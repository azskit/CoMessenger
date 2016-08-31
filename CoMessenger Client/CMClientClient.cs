using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using System.Windows;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
using COMessengerClient;
using System.Text.RegularExpressions;
using COMessengerClient.Conversation;
using System.Windows.Documents;
using System.IO;
using System.Security;
using System.Net;
using COMessengerClient.Credentials;

namespace CorporateMessengerLibrary
{


    public class CMClientClientViewModel : DependencyObject
    {
        public CMClientClientViewModel()
        {
            //UserLogin = COMessengerClient.Properties.Settings.Default.UserLogin;
            //UserPassword = COMessengerClient.Properties.Settings.Default.UserPassword;
        }

        public string ConnectionStatus
        {
            get 
            {
                string tmp = "";
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    tmp = (string)GetValue(ConnectionStatusProperty);
                }));

                return tmp; 
            }
            set
            { 
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetValue(ConnectionStatusProperty, value);
                    }));
            }
        }

        // Using a DependencyProperty as the backing store for ConnectionStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectionStatusProperty =
            DependencyProperty.Register("ConnectionStatus", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata(String.Empty));
    }

    public class AuthorizationSuccessEventArgs : EventArgs
    {
        public CoMessengerUser LoggedUser { get; set; }

        public AuthorizationSuccessEventArgs(CoMessengerUser loggedUser)
        {
            LoggedUser = loggedUser;
        }
    }

    public class AuthorizationErrorEventArgs : EventArgs
    {
        public CMMessage ErrorMessage { get; set; }

        public AuthorizationErrorEventArgs(CMMessage errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
    public class ConnectingEventArgs : EventArgs
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public bool CanConnect { get; set; }

        public ConnectingEventArgs(string server, int port)
        {
            Server = server;
            Port = port;
            CanConnect = true;
        }
    }

    public class CMClientClient : CMClientBase
    {
        private Thread QueueProcessThread;
        private Thread StayAliveThread;

        private AutoResetEvent NewMessageEvent = new AutoResetEvent(false);
        private Task NewMessageTask;

        public event EventHandler<AuthorizationSuccessEventArgs> AuthorizationSuccess;
        public event EventHandler<AuthorizationErrorEventArgs> AuthorizationError;
        public event EventHandler<ConnectingEventArgs> Connecting;
        public event EventHandler ContactListLoaded;

        public CMClientClientViewModel ViewModel { get; set; }

        public string Login { get; set; }
        public string Domain { get; set; }
        //public SecureString Password { get; set; }
        private NetworkCredential credentials;
        public string       Server   { get; set; }
        public int          Port     { get; set; }


        public CMClientClient()
        {
            ViewModel = new CMClientClientViewModel();
        }

        public void ConnectTo(string server, int port)
        {
            if (state == ClientState.Disconnected || state == ClientState.Error || state == ClientState.MarkedToKill)
            {
                this.Server = server;
                this.Port = port;

                OnConnecting(new ConnectingEventArgs(server, port));

                //if (!canConnect)
                //    return;

                state = ClientState.Connecting;

                if (tcp == null)
                {
                    tcp = new TcpClient();
                }

                if (tcp.Connected)
                    throw new InvalidOperationException("Подключение уже установлено");

                try
                {
                    tcp.Connect(server, port);
                    tcp.NoDelay = true;
                    cStream = tcp.GetStream();
                    state = ClientState.Connected;

                    SendKey();

                    WaitKey();

                    OnConnected();
                }
                catch (ArgumentOutOfRangeException e)
                {
                    state = ClientState.Error;
                    OnConnectionError(e);
                }
                catch (SocketException e)
                {
                    state = ClientState.Error;
                    OnConnectionError(e);
                }
            }
        }

        public IAsyncResult AsynchronousConnectTo(string server, int port, AsyncCallback callback)
        {
            DelegateConnectTo connect_delegate = new DelegateConnectTo(this.ConnectTo);

            return connect_delegate.BeginInvoke(server, port, callback, new Object());
        }

        protected override void OnDisconnected()
        {
            IsStayAlive = false;
            IsProcessing = false;

            if (NewMessageTask != null && !NewMessageTask.IsCompleted && Task.CurrentId != NewMessageTask.Id)
            {
                //Отпускаем задачу с паузы
                NewMessageEvent.Set();
                //и ждем пока она закончится
                NewMessageTask.Wait();
            }

            base.OnDisconnected();
        }
        
        protected override void OnNewMessage()
        {
            NewMessageEvent.Set();

            base.OnNewMessage();
        }

        protected override void OnConnected()
        {
            NewMessageTask = new Task(NewMessageHandler);
            NewMessageTask.Start();

            //Сперва представимся
            Authorizate();

            base.OnConnected();
        }

        private void Authorizate()
        {

            credentials = CredentialFormModel.GetCredentials();
            this.Login = credentials.UserName;
            this.Domain = credentials.Domain;

            CMMessage AuthorizationMessage = new CMMessage();

            AuthorizationMessage.Kind = MessageKind.Authorization;

            //Если используем учетку текущего пользователя Windows, логин и пароль нам не интересен
            if (COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount)
            {
                this.Login = Environment.UserName;
                this.Domain = Environment.UserDomainName;

                AuthorizationMessage.Message = new CoMessengerUser()
                {
                    UserId = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,
                    IsBuiltIn = false
                };
            }
            //Если в поле "Логин" введена учетка с доменом
            else
            {
                //Доменная авторизация
                if (!String.IsNullOrEmpty(credentials.Domain))
                {
                    AuthorizationMessage.Message = new CoMessengerUser()
                    {
                        Login = credentials.UserName,
                        Domain = credentials.Domain,
                        EncryptedPassword = CryptPassword(credentials.SecurePassword)
                    };
                }
                //Встроенная авторизация
                else
                {
                    AuthorizationMessage.Message = new CoMessengerUser()
                    {
                        Login = credentials.UserName,
                        EncryptedPassword = CryptPassword(credentials.SecurePassword)
                    };
                }
            }

            App.ThisApp.Client.PutOutMessage(AuthorizationMessage);
        }

        protected virtual void OnAuthorizationSuccess(AuthorizationSuccessEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            App.ThisApp.CurrentUser = args.LoggedUser;

            //Если вручную вводили пароль, то сохраним данные для следующего входа
            if (!COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount)
            {
                CredentialFormModel.SaveCredentials(credentials);
            }

            if (AuthorizationSuccess != null)
                AuthorizationSuccess(this, args);
        }

        protected virtual void OnAuthorizationError(AuthorizationErrorEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            Disconnect();

            if (AuthorizationError != null)
                AuthorizationError(this, args);
        }

        protected virtual void OnConnecting(ConnectingEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            if (Connecting != null)
                Connecting(this, args);

            //canConnect = args.CanConnect;
        }

        protected virtual void OnContactListLoaded(EventArgs args)
        {

            if (ContactListLoaded != null)
                ContactListLoaded(this, args);
        }


        private void QueueProcess()
        {
            while (state == ClientState.Connected)
            {
                ProcessQueue();

                Thread.Sleep(1);
            }
        }

        private bool _IsProcessing;

        public bool IsProcessing
        {
            get { return _IsProcessing; }
            set {
                if (value != _IsProcessing)
                {
                    if (value)
                    {
                        QueueProcessThread = new Thread(new ThreadStart(QueueProcess));
                        QueueProcessThread.IsBackground = true;
                        QueueProcessThread.Name = "QueueProcess";

                        QueueProcessThread.Start();
                    }

                    _IsProcessing = value;
                }
                }
        }


        private void StayAlive()
        {
            while (_IsStayAlive && state == ClientState.Connected)
            {
                if (LastActivity.AddMilliseconds(15000) < DateTime.Now)
                    CheckAlive();

                Thread.Sleep(1000);
            }
        }

        private bool _IsStayAlive;

        public bool IsStayAlive
        {
            get { return _IsStayAlive; }
            set {
                if (value != _IsStayAlive)
                {
                    _IsStayAlive = value;

                    if (value)
                    {
                        StayAliveThread = new Thread(new ThreadStart(StayAlive));
                        StayAliveThread.IsBackground = true;
                        StayAliveThread.Name = "StayAlive";

                        StayAliveThread.Start();
                    }

                }
            }
        }

        /// <summary>
        /// Обработчик новых сообщений
        /// </summary>
        private void NewMessageHandler()
        {
            while (state == ClientState.Connected)
            {
                NewMessageEvent.WaitOne();

                //Заканчиваем работу
                if (state != ClientState.Connected)
                    return;

//#if DEBUG
//                Debug.WriteLine("{0}: Получен сигнал о новом сообщении", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));
//#endif

                while (InMessagesCount > 0)
                {
                    CMMessage NewCMMessage = GetInMessage();
//#if DEBUG
//                    Debug.WriteLine("{0}: searchResult dequeued", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));
//#endif
                    switch (NewCMMessage.Kind)
                    {
                        case MessageKind.Ping: break;

                        case MessageKind.RoutedMessage:

                            //App.ThisApp.Client.ViewModel.ConnectionStatus = App.ThisApp.Client.ViewModel.ConnectionStatus + " Получено обратно через:" + App.sw.ElapsedMilliseconds;

                            RoutedMessage routedMessage = (RoutedMessage)NewCMMessage.Message;

                            RoutedMessagesHandler(routedMessage);


                            break;

                        //Успешная авторизация
                        case MessageKind.Authorization:

                            //Trace.WriteLine("{0}: Авторизация успешна", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            OnAuthorizationSuccess( new AuthorizationSuccessEventArgs((CoMessengerUser)NewCMMessage.Message));

                            break;

                        //Ошибка при авторизации
                        case MessageKind.AuthorizationError:

                            OnAuthorizationError(new AuthorizationErrorEventArgs(NewCMMessage));

                            break;

                        //Требуется новый пароль
                        case MessageKind.AuthorizationNewPassword:

                            throw new InvalidOperationException("Требования смены пароля");

                            /*
                            App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                            {

                                var NewPasswordPrompt =
                                    new COMessengerClient.CustomControls.InputBox(
                                        text: App.ThisApp.Locally.LocaleStrings["Type in new password"],
                                        caption: App.ThisApp.Locally.LocaleStrings["Set new password"]);

                                //Пользователь ввел пароль - отправляем (даже если пароль пустой)
                                if (NewPasswordPrompt.ShowDialog() == true)
                                {
                                    App.ThisApp.Client.PutOutMessage(new CMMessage()
                                    {
                                        Kind = MessageKind.AuthorizationNewPassword,
                                        SearchResult = MD5Helper.CreateMD5(NewPasswordPrompt.Text)
                                    });
                                }
                                //Пользователь отказался от ввода - если не авторизованы, то отключаемся
                                else if (App.ThisApp.CurrentUser == null)
                                {
                                    App.ThisApp.Client.Disconnect();
                                }
                                //Если авторизованы, ниче не делаем
                                else
                                {
                                }
                            }));
                            */
                            //break;

                        case MessageKind.UsersList:

                            //Trace.WriteLine("{0}: Список пользователей", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            //List<CoMessengerUser>  NewUsersList = (List<CoMessengerUser>)NewCMMessage.SearchResult;

                            //RecieversList.Values.OfType<ServerRoomPeer>().ToList<RoomPeer>();
                            List<RoomPeer> Rooms = (List<RoomPeer>)NewCMMessage.Message;

                            //Stopwatch sw = new Stopwatch();

                            //sw.Start();

                           

                            Rooms.ForEach(Room =>
                                {
                                    App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                                    {

                                        //Добавляем комнату
                                        AddOrUpdatePeer(Room);

                                        //Добавляем всех пользователей
                                        if (Room.IsMainRoom)
                                        {
                                            Room.Participants.ForEach(Person => AddOrUpdatePeer(Person));
                                            //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Contact list loaded");
                                            //ViewModel.ConnectionStatus = DateTime.Now.ToString("HH:mm:ss.ffff") + " Contact list loaded";
                                        }
                                    }), System.Windows.Threading.DispatcherPriority.Background);
                                });




                                OnContactListLoaded(new EventArgs());

                            //sw.Stop();
                            //    App.ThisApp.Client.ViewModel.ConnectionStatus = "Контакт лист загружен за " + sw.ElapsedMilliseconds;
                            //    sw.Reset();

                                
                                break;

                        case MessageKind.UpdatePeer:
                            
                            //Trace.WriteLine("{0}: Новый пользователь", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            //CoMessengerUser updatedPeer = (CoMessengerUser)NewCMMessage.SearchResult;
                            Peer updatedPeer = NewCMMessage.Message as Peer;


                            App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddOrUpdatePeer(updatedPeer);

                                //Trace.WriteLine("{0}: Пользователь обработан", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            }));
                            break;

                        //case MessageKind.Answer:

                        //    QueryMessage answer = NewCMMessage.Message as QueryMessage;

                        //    HistoryQuery query = NewCMMessage.Message as HistoryQuery;

                        //    ClientPeer room = App.ThisApp.ListOfConversations[query.PeerID];

                        //    room.ProcessHistoryQuery(query);


                        //    break;

                        case MessageKind.Disconnect:

                            ViewModel.ConnectionStatus = "Disconnected by server";

                            Disconnect();
                            break;

                        default: break;
                    }

                }
            }
        }

        private static void AddOrUpdatePeer(Peer peer)
        {
            //App.ThisApp.Dispatcher.Invoke(new Action(() =>
            //{

            lock (App.ThisApp.ListOfConversations)
            {


                //Есть ли такой пользователь в списке?
                if (App.ThisApp.ListOfConversations.ContainsKey(peer.PeerID))
                {
                    App.ThisApp.ListOfConversations[peer.PeerID].UpdatePeer(peer);
                    App.ThisApp.ListOfConversations.RaiseCollectionChanged(App.ThisApp.ListOfConversations[peer.PeerID], App.ThisApp.ListOfConversations[peer.PeerID], App.ThisApp.ListOfConversations.IndexOf(peer.PeerID));
                    //App.ThisApp.ListOfConversations.RaiseCollectionChanged();
                    //App.ThisApp.ListOfConversations[RoomID.PeerID] = new ClientPeer() { Peer = RoomID };
                }
                else
                {
                    ClientPeer NewPeer = new ClientPeer();

                    NewPeer.UpdatePeer(peer);

                    App.ThisApp.ListOfConversations.Add(peer.PeerID, NewPeer);
                }
            }
            if (App.ThisApp.CurrentUser.UserId == peer.PeerID)
                App.ThisApp.CurrentPeer = App.ThisApp.ListOfConversations[peer.PeerID];
            //}));
        }
        
        private static void RoutedMessagesHandler(RoutedMessage routedMessage)
        {

            //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Ищем получателя сообщения");

            ClientPeer RECEIVER, Sender;

            Sender  = App.FoundPeer(routedMessage.Sender);

            if (!App.ThisApp.ListOfConversations.ContainsKey(routedMessage.Receiver))
                return; //Игнорируем
            else
                RECEIVER = App.ThisApp.ListOfConversations[routedMessage.Receiver];

            //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Получатель найден");
            if (RECEIVER.Peer.Type == PeerType.Room)
            {
                //lock (Receiver)
                //{
                    RECEIVER.ProcessMessage(routedMessage);
                //}
                
            }
            else if (routedMessage.Receiver == App.ThisApp.CurrentUser.UserId)
            {

                //lock (Receiver)
                //{
                Sender.ProcessMessage(routedMessage);
                //}
            }
            else
                throw new InvalidOperationException("Кажется это сообщение не для нас!");


        }
    }
}
