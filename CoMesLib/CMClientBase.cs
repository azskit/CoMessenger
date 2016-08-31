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
        public TcpClient tcp;

        public NetworkStream cStream;

        public ClientState state;

        public Exception exception { get; set; }

        public CoMessengerUser user;

        private RSAParameters clientPublicKey;
        private RSACryptoServiceProvider cryptoProvider;

        public DateTime LastActivity = DateTime.Now;

        private Queue<CMMessage> OutMessageQueue = new Queue<CMMessage>();
        private Queue<CMMessage> InMessageQueue = new Queue<CMMessage>();

        private SortedList<string, QueryMessage> QueryMessages = new SortedList<string,QueryMessage>();

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler Disconnecting;
        public event ErrorEventHandler ConnectionError;
        public event EventHandler NewMessage;

        public delegate void DelegateConnectTo(string server, int port);


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="p_tcp">Подключившийся клиент</param>
        public CMClientBase(TcpClient p_tcp)
            : this()
        {
            tcp = p_tcp;
            cStream = tcp.GetStream();
            tcp.NoDelay = true;
            state = ClientState.Connected;
            OnConnected();
        }

        public CMClientBase()
        {
            state = ClientState.Disconnected;
            cryptoProvider = new RSACryptoServiceProvider();
        }



        protected virtual void OnConnected()
        {
            if (Connected != null)
                Connected(this, new EventArgs());
        }
        protected virtual void OnConnectionError(Exception e)
        {
            exception = e;

            if (ConnectionError != null)
                ConnectionError(this, new ErrorEventArgs(e));
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
            if (state != ClientState.Disconnected)
            {
                OnDisconnecting();
                state = ClientState.Disconnected;
                lock (cStream)
                {
                    cStream.Close();
                    tcp.Close();

                    tcp = null;
                    cStream = null;
                }
                OnDisconnected();
            }
        }


        /// <summary>
        /// Обработать очереди
        /// </summary>
        public void ProcessQueue()
        {
            if (state == ClientState.Connected)
            {
                if (OutMessageQueue.Count > 0)
                    Send(OutMessageQueue.Dequeue());
            }

            if (state == ClientState.Connected)
            {
                if (tcp.Available > 0)
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
        private void Send(CMMessage mes)
        {
            if (state == ClientState.Connected)
            {
                try
                {
                    //StreamWriter writer = new StreamWriter(stream);
                    lock (cStream)
                    {
                         Serializator.fmt.Serialize(cStream, mes);
                    }

                    LastActivity = DateTime.Now;
                    //Console.WriteLine("Отправлено {0} :", DateTime.Now.TimeOfDay.TotalSeconds);

#if DEBUG
                    if (mes.Kind == MessageKind.RoutedMessage)
                    {
                        Console.WriteLine("{0}: Отправлено сообщение", DateTime.Now.ToString("HH:mm:ss.ffff"));
                    }
                    else if (mes.Kind == MessageKind.Ping)
                    {
                        Console.WriteLine("{0}: Ping sent", tcp.Client.RemoteEndPoint);
                    }
                    else if (mes.Kind == MessageKind.Disconnect)
                    {
                        Console.WriteLine("{0}: Disconnect sent", tcp.Client.RemoteEndPoint);
                    }

#endif
                    //Сообщение, ожидающее ответа
                    if (mes.Kind == MessageKind.Query)
                    {
                        QueryMessage query = mes.Message as QueryMessage;

                        QueryMessages.Add(query.MessageID, query);

                        //Если ответа не придет - вызываем назначенный обработчик и удаляемся из списка ожидающих ответа
                        query.Timer = new Timer(callback: (me) =>
                                                            {
                                                                QueryMessage timedOutQuery = me as QueryMessage;

                                                                Trace.WriteLine("Timeout of " + timedOutQuery.MessageID + " fired");

                                                                if (timedOutQuery.TimeoutAction != null)
                                                                    timedOutQuery.TimeoutAction.BeginInvoke(null, null);

                                                                QueryMessages.Remove(timedOutQuery.MessageID);
                                                            },
                                                state: query,
                                                dueTime: TimeSpan.FromSeconds(60),       //Таймаут 60 секунд
                                                period: TimeSpan.FromMilliseconds(-1)); //-1 не повторять
                    }


                }
                catch (System.IO.IOException e)
                {
                    state = ClientState.MarkedToKill;

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
                state = ClientState.MarkedToKill;
                Console.WriteLine("{0} lost connection ({1})", tcp.Client.RemoteEndPoint.ToString(), e.Message);
            }
        }


        /// <summary>
        /// Прочесть сообщение из потока
        /// </summary>
        /// <returns>Сообщение</returns>
        private CMMessage ReadMessage()
        {
            CMMessage newmes = null;
            try
            {
                lock (cStream)
                {
                    //msgsize = cStream.Position;
                    newmes = Serializator.fmt.Deserialize(cStream) as CMMessage;
                    //msgsize = cStream.Position - msgsize;
                }

                LastActivity = DateTime.Now;
#if DEBUG
                if (newmes.Kind == MessageKind.RoutedMessage)
                {
                    //byte[] bytes = ((RoutedMessage)newmes.Message).Message as byte[];
                    //Console.WriteLine("{0}: Прочитано сообщение размером {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), bytes != null ? bytes.Length : -1);
                    Console.WriteLine("{0}: Прочитано сообщение {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), ((RoutedMessage)newmes.Message).MessageID);
                }
                else if (newmes.Kind == MessageKind.Ping)
                {
                    Console.WriteLine("{0}: Ping received", tcp.Client.RemoteEndPoint );
                }
#endif
                //Дождались ответа на сообщение, ожидающее ответа :) Вызываем обработчик
                if (newmes.Kind == MessageKind.Answer)
                {
                    QueryMessage answer = newmes.Message as QueryMessage;

                    QueryMessage initialQuery = QueryMessages[answer.MessageID];

                    initialQuery.Timer.Dispose(); //остановить таймер

                    if (initialQuery.SuccessAction != null)
                        initialQuery.SuccessAction.BeginInvoke(answer, null, null);

                    QueryMessages.Remove(answer.MessageID);
                }

            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                state = ClientState.MarkedToKill;

                OnConnectionError(e);
                Disconnect();

            }
            catch (IOException e)
            {
                state = ClientState.MarkedToKill;

                OnConnectionError(e);
                Disconnect();

            }
            catch (Exception)
            {
                state = ClientState.Error;
                throw;
            }

//#if DEBUG
//            if (newmes!= null && newmes.Kind == MessageKind.RoutedMessage)
//                Debug.WriteLine("{0}: message read", DateTime.Now.ToString("HH:mm:ss.ffff"));
//#endif
            return newmes;
        }

        public CMMessage GetInMessage()
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
            Dispose();
        }

        public void Dispose()
        {
            if (cStream != null)
                cStream.Close();

            if (tcp != null)
                tcp.Close();

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

            if (KeyMessage.Kind != MessageKind.CryptoKey)
                throw new InvalidOperationException("Ожидался криптоключ вообще то...");

            clientPublicKey = (RSAParameters)KeyMessage.Message;
        }

        protected byte[] CryptSomething(object something)
        {
            //Зашифровать вонючее сообщение
            using (MemoryStream toEncrypt = new MemoryStream())
            {
                Serializator.fmt.Serialize(toEncrypt, something);
                
                cryptoProvider.ImportParameters(clientPublicKey);
                return cryptoProvider.Encrypt(toEncrypt.ToArray(), true);
            }
        }

        public object DecryptSomething(byte[] encrypted)
        {
            using (MemoryStream toEncrypt = new MemoryStream(cryptoProvider.Decrypt(encrypted, true)))
            {
                return Serializator.fmt.Deserialize(toEncrypt);
            }
        }

        protected byte[] CryptPassword(SecureString password)
        {
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
