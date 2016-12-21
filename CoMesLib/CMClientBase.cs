using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.InteropServices;
using System.Globalization;

namespace CorporateMessengerLibrary
{
    public enum ClientState
    {
        Connected,
        MarkedToKill,
        Error,
        Disconnected,
        Connecting
    }


    public class CMClientBase : IDisposable
    {
        [Serializable]
        protected class CimCredentials
        {
            public bool   IsLoggedIn { get; set; }
            public string UserName   { get; set; }
            public string Domain     { get; set; }
            public byte[] Password   { get; set; }
        }


        public TcpClient Tcp { get; protected set; }

        public NetworkStream CStream { get; protected set; }

        public ClientState State { get; protected set; }

        //public Exception exception { get; set; }

        //protected CMUser User { get; set; }

        private RSAParameters clientPublicKey;
        private RSACryptoServiceProvider cryptoProvider;

        public DateTime LastActivity { get; private set; }

        private Queue<CMMessage> OutMessageQueue = new Queue<CMMessage>();
        private Queue<CMMessage> InMessageQueue = new Queue<CMMessage>();

        private SortedList<string, QueryMessage> QueryMessages = new SortedList<string,QueryMessage>();

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler Disconnecting;
        public event ErrorEventHandler ConnectionError;
        public event EventHandler NewMessage;

        protected delegate void DelegateConnectTo(string server, int port);


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="tcp">Подключившийся клиент</param>
        public CMClientBase(TcpClient tcp)
            : this()
        {
            this.Tcp = tcp;
            CStream = this.Tcp.GetStream();
            this.Tcp.NoDelay = true;
            State = ClientState.Connected;
            //OnConnected();
        }

        public CMClientBase()
        {
            LastActivity = DateTime.Now;
            State = ClientState.Disconnected;
            cryptoProvider = new RSACryptoServiceProvider();
        }



        protected virtual void OnConnected()
        {
            if (Connected != null)
                Connected(this, new EventArgs());
        }
        protected virtual void OnConnectionError(Exception connectionException)
        {
            //exception = e;

            if (ConnectionError != null)
                ConnectionError(this, new ErrorEventArgs(connectionException));
        }
        protected virtual void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }
        protected virtual void OnDisconnecting()
        {
            if (Disconnecting != null)
                Disconnecting(this, new EventArgs());
        }
        protected virtual void OnNewMessage()
        {
            if (NewMessage != null)
                NewMessage(this, new EventArgs());
        }



        public void Disconnect()
        {
            if (State != ClientState.Disconnected)
            {
                OnDisconnecting();
                State = ClientState.Disconnected;
                lock (Tcp)
                {
                    CStream.Close();
                    Tcp.Close();

                    Tcp = null;
                    CStream = null;
                }
                OnDisconnected();
            }
        }


        /// <summary>
        /// Обработать очереди
        /// </summary>
        public void ProcessQueue()
        {
            if (State == ClientState.Connected)
            {
                if (OutMessageQueue.Count > 0)
                    Send(OutMessageQueue.Dequeue());
            }

            if (State == ClientState.Connected)
            {
                if (Tcp.Available > 0)
                {
                    CMMessage tmp = ReadMessage();

                    if (tmp != null)
                    {
//#if DEBUG
//                        if (tmp.Kind == MessageKind.RoutedMessage)
//                        {
//                            Trace.WriteLine("{0}: Получено сообщение", DateTime.Now.ToString("HH:mm:ss.ffff"));
//                            Console.WriteLine("{0}: Получено сообщение", DateTime.Now.ToString("HH:mm:ss.ffff"));
//                        }
//#endif
                        InMessageQueue.Enqueue(tmp);
                        OnNewMessage();
                    }

                }
            }
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="mes">Сообщение</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Не передавать литералы в качестве локализованных параметров", MessageId = "System.Console.WriteLine(System.String,System.Object)")]
        private void Send(CMMessage mes)
        {
            if (State == ClientState.Connected)
            {
                try
                {
                    //StreamWriter writer = new StreamWriter(stream);
                    lock (Tcp)
                    {
                         Serializator.Formatter.Serialize(CStream, mes);
                    }

                    LastActivity = DateTime.Now;
                    //Console.WriteLine("Отправлено {0} :", DateTime.Now.TimeOfDay.TotalSeconds);

#if DEBUG
                    if (mes.Kind == MessageKind.RoutedMessage)
                    {
                        Console.WriteLine("{0}: Отправлено сообщение", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture));
                    }
                    else if (mes.Kind == MessageKind.Ping)
                    {
                        Console.WriteLine("{0}: Ping sent", Tcp.Client.RemoteEndPoint);
                    }
                    else if (mes.Kind == MessageKind.Disconnect)
                    {
                        Console.WriteLine("{0}: Disconnect sent", Tcp.Client.RemoteEndPoint);
                    }

#endif
                    //Сообщение, ожидающее ответа
                    if (mes.Kind == MessageKind.Query)
                    {
                        QueryMessage query = mes.Message as QueryMessage;

                        QueryMessages.Add(query.MessageId, query);

                        //Если ответа не придет - вызываем назначенный обработчик и удаляемся из списка ожидающих ответа
                        query.Timer = new Timer(callback: (me) =>
                                                            {
                                                                QueryMessage timedOutQuery = me as QueryMessage;

                                                                Trace.WriteLine("Timeout of " + timedOutQuery.MessageId + " fired");

                                                                if (timedOutQuery.TimeoutAction != null)
                                                                    timedOutQuery.TimeoutAction.BeginInvoke(null, null);

                                                                QueryMessages.Remove(timedOutQuery.MessageId);
                                                            },
                                                state: query,
                                                dueTime: TimeSpan.FromSeconds(60),       //Таймаут 60 секунд
                                                period: TimeSpan.FromMilliseconds(-1)); //-1 не повторять
                    }


                }
                catch (System.IO.IOException e)
                {
                    State = ClientState.MarkedToKill;

                    OnConnectionError(e);
                    Disconnect();

                }
                //catch (Exception)
                //{
                //    state = ClientState.Error;
                //    throw;
                //}
            }
            else
            {
                throw new InvalidOperationException("Подключение не установлено");
            }

        }

        /// <summary>
        /// Проверка существования канала
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Не передавать литералы в качестве локализованных параметров", MessageId = "System.Console.WriteLine(System.String,System.Object,System.Object)")]
        public void CheckAlive()
        {
            CMMessage ping = new CMMessage();

            ping.Kind = MessageKind.Ping;

            try
            {
                PutOutMessage(ping);
                //state = ClientState.Connected;
            }
            catch (System.IO.IOException e)
            {
                State = ClientState.MarkedToKill;
                Console.WriteLine("{0} lost connection ({1})", Tcp.Client.RemoteEndPoint.ToString(), e.Message);
            }
        }


        /// <summary>
        /// Прочесть сообщение из потока
        /// </summary>
        /// <returns>Сообщение</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Не передавать литералы в качестве локализованных параметров", MessageId = "System.Console.WriteLine(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Не передавать литералы в качестве локализованных параметров", MessageId = "System.Console.WriteLine(System.String,System.Object,System.Object)")]
        private CMMessage ReadMessage()
        {
            CMMessage newmes = null;
            try
            {
                lock (Tcp)
                {
                    //msgsize = cStream.Position;
                    newmes = Serializator.Formatter.Deserialize(CStream) as CMMessage;
                    //msgsize = cStream.Position - msgsize;
                }

                LastActivity = DateTime.Now;
#if DEBUG
                if (newmes.Kind == MessageKind.RoutedMessage)
                {
                    //byte[] bytes = ((RoutedMessage)newmes.Message).Message as byte[];
                    //Console.WriteLine("{0}: Прочитано сообщение размером {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), bytes != null ? bytes.Length : -1);
                    Console.WriteLine("{0}: Прочитано сообщение {1}", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture), ((RoutedMessage)newmes.Message).MessageId);
                }
                else if (newmes.Kind == MessageKind.Ping)
                {
                    Console.WriteLine("{0}: Ping received", Tcp.Client.RemoteEndPoint );
                }
#endif
                //Дождались ответа на сообщение, ожидающее ответа :) Вызываем обработчик
                if (newmes.Kind == MessageKind.Answer)
                {
                    QueryMessage answer = newmes.Message as QueryMessage;

                    QueryMessage initialQuery = QueryMessages[answer.MessageId];

                    initialQuery.Timer.Dispose(); //остановить таймер

                    if (initialQuery.SuccessAction != null)
                        initialQuery.SuccessAction.BeginInvoke(answer, null, null);

                    QueryMessages.Remove(answer.MessageId);
                }

            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                State = ClientState.MarkedToKill;

                OnConnectionError(e);
                Disconnect();

            }
            catch (IOException e)
            {
                State = ClientState.MarkedToKill;

                OnConnectionError(e);
                Disconnect();

            }
            catch (Exception)
            {
                State = ClientState.Error;
                throw;
            }

//#if DEBUG
//            if (newmes!= null && newmes.Kind == MessageKind.RoutedMessage)
//                Debug.WriteLine("{0}: message read", DateTime.Now.ToString("HH:mm:ss.ffff"));
//#endif
            return newmes;
        }

        public CMMessage RetrieveInMessage()
        {
            return InMessageQueue.Dequeue();
        }

        public void PutOutMessage(CMMessage mes)
        {
            OutMessageQueue.Enqueue(mes);
        }

        public int InMessagesCount
        {
            get { return InMessageQueue.Count; }
        }

        public int OutMessagesCount
        {
            get { return OutMessageQueue.Count; }
        }

        ~CMClientBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (CStream != null)
                    CStream.Close();

                if (Tcp != null)
                    Tcp.Close();

                if (cryptoProvider != null)
                    cryptoProvider.Dispose();
            }
        }

        public void SendKey()
        {
            Send(new CMMessage()
            {
                Kind = MessageKind.CryptoKey,
                Message = cryptoProvider.ExportParameters(false)
            });
        }

        public void WaitKey()
        {
            CMMessage KeyMessage = ReadMessage();

            if (KeyMessage != null)
            {
                if (KeyMessage.Kind != MessageKind.CryptoKey)
                    throw new InvalidOperationException("Ожидался криптоключ вообще то...");

                clientPublicKey = (RSAParameters)KeyMessage.Message;
            }
        }

        protected byte[] CryptSomething(object something)
        {
            //Зашифровать вонючее сообщение
            using (MemoryStream toEncrypt = new MemoryStream())
            {
                Serializator.Formatter.Serialize(toEncrypt, something);
                
                cryptoProvider.ImportParameters(clientPublicKey);
                return cryptoProvider.Encrypt(toEncrypt.ToArray(), true);
            }
        }

        public object DecryptSomething(byte[] encrypted)
        {
            using (MemoryStream toEncrypt = new MemoryStream(cryptoProvider.Decrypt(encrypted, true)))
            {
                return Serializator.Formatter.Deserialize(toEncrypt);
            }
        }

        protected byte[] CryptPassword(SecureString password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            cryptoProvider.ImportParameters(clientPublicKey);
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(password);

                byte[] bytes = new byte[password.Length * 2];
                for (int i=0; i < password.Length * 2; i++)
                {
                    bytes.SetValue(Marshal.ReadByte(valuePtr, i), i); 
                }
                return cryptoProvider.Encrypt(bytes, true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        
        }

        public string DecryptPassword(byte[] encrypted)
        {
            return Encoding.Unicode.GetString(cryptoProvider.Decrypt(encrypted, true));
        }
    }

}
