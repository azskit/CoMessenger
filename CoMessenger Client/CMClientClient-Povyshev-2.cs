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

namespace CorporateMessengerLibrary
{
    public static class CMClientCommands
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")] //Ругается мол пишите рид онли, а поля в типе то все равно менять можно. А фиголи
        public static readonly RoutedUICommand SignInCommand = RegisterNewCommand("SignInCommand", "SignInCommand", SignIn_Executed, SignIn_CanExecute);
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand SignOutCommand = RegisterNewCommand("SignOutCommand", "SignOutCommand", SignOut_Executed, SignOut_CanExecute);

        private static void SignOut_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var client = COMessengerClient.App.ThisApp.Client as CMClientClient;

            e.CanExecute = (client.state == ClientState.Connected);
        }

        private static void SignOut_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            App.ThisApp.Client.Disconnect();
            App.ThisApp.Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentCulture, App.ThisApp.Locally.LocaleStrings["Disconnected"]);
        }

        private static void SignIn_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var client = COMessengerClient.App.ThisApp.Client as CMClientClient;
                            
            e.CanExecute = (
                //Состояние - не подключен
                client.state != ClientState.Connecting && client.state != ClientState.Connected

                //Вход либо с текущей учеткой, либо если задан логин
             && (COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount || !String.IsNullOrWhiteSpace(client.ViewModel.UserLogin) )

                //Заполнены сервер и порт в настройках
             && !String.IsNullOrWhiteSpace(COMessengerClient.Properties.Settings.Default.Server)
             && !String.IsNullOrWhiteSpace(COMessengerClient.Properties.Settings.Default.Port)
                           );
        }

        private static void SignIn_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var client = COMessengerClient.App.ThisApp.Client as CMClientClient;

            string server = COMessengerClient.Properties.Settings.Default.Server;
            int port = Int32.Parse(COMessengerClient.Properties.Settings.Default.Port, CultureInfo.InvariantCulture);

            client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Connecting to {0}:{1}"], server, port.ToString(CultureInfo.InvariantCulture));
            client.AsynchronousConnectTo(server, port, new AsyncCallback((a) =>
            {
                if (client.state == ClientState.Connected)
                    client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Connected to {0}:{1}"], server, port.ToString(CultureInfo.InvariantCulture));
                else if (client.state == ClientState.Error)
                    client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error occurred during connection to {0}:{1} - {2}"], server, port.ToString(CultureInfo.InvariantCulture), client.exception.Message);
            }));

        }

        private static RoutedUICommand RegisterNewCommand(string commandDescription, string commandName, ExecutedRoutedEventHandler execHandler, CanExecuteRoutedEventHandler canExecHandler)
        {
            RoutedUICommand NewCommand = new RoutedUICommand(commandDescription, commandName, typeof(Window));

            CommandManager.RegisterClassCommandBinding(type: typeof(Window),
                                                        commandBinding: new CommandBinding(command: NewCommand,
                                                                                            executed: new ExecutedRoutedEventHandler(execHandler),
                                                                                            canExecute: new CanExecuteRoutedEventHandler(canExecHandler)));

            return NewCommand;
        }

    }


    public class CMClientClientViewModel : DependencyObject
    {
        public CMClientClientViewModel()
        {
            UserLogin = COMessengerClient.Properties.Settings.Default.UserLogin;
            UserPassword = COMessengerClient.Properties.Settings.Default.UserPassword;

            //COMessengerClient.Properties.Settings.Default.PropertyChanged += 
            //    (sender, args) => 
            //    {
            //        if (args.PropertyName == "UseCurrentWindowsAccount")
            //            IsBuiltIn = !COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount;
            //    };

        }

        //public string ButtonName
        //{
        //    get { return (string)GetValue(ButtonNameProperty); }
        //    set 
        //    {
        //        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        //            {
        //                SetValue(ButtonNameProperty, value);
        //            }));
        //    }
        //}

        // Using a DependencyProperty as the backing store for ButtonName.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty ButtonNameProperty =
        //    DependencyProperty.Register("ButtonName", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata("Ошыбка"));



        public string ConnectionStatus
        {
            get { return (string)GetValue(ConnectionStatusProperty); }
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



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
        public string UserLogin
        {
            get 
            {
                return (string)GetValue(UserLoginProperty); 
            }
            set
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetValue(UserLoginProperty, value);
                    }));
            }
        }

        // Using a DependencyProperty as the backing store for UserLogin.  This enables animation, styling, binding, etc...
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
        public static readonly DependencyProperty UserLoginProperty =
            DependencyProperty.Register("UserLogin", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata(String.Empty));



        public string UserDomain
        {
            get { return (string)GetValue(UserDomainProperty); }
            set { SetValue(UserDomainProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserDomain.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserDomainProperty = 
    DependencyProperty.Register("UserDomain", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata(String.Empty));




    //    public bool IsBuiltIn
    //    {
    //        get { return (bool)GetValue(IsBuiltInProperty); }
    //        set { SetValue(IsBuiltInProperty, value); }
    //    }

    //    // Using a DependencyProperty as the backing store for IsBuiltIn.  This enables animation, styling, binding, etc...
    //    public static readonly DependencyProperty IsBuiltInProperty = 
    //DependencyProperty.Register("IsBuiltIn", typeof(bool), typeof(CMClientClientViewModel), new UIPropertyMetadata((bool)true));

        


        public string UserPassword
        {
            get 
            {
                    return (string)GetValue(UserPasswordProperty);
            }
            set
            {

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetValue(UserPasswordProperty, value);
                    }));
            }        
        }

        // Using a DependencyProperty as the backing store for UserPassword.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserPasswordProperty =
            DependencyProperty.Register("UserPassword", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata(String.Empty));

        public object GetPropertyValueThroughDispatcher(DependencyProperty dp)
        {
            return Dispatcher.Invoke(new Func<object>(() => { return GetValue(dp); }));
        }



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

        public CMClientClientViewModel ViewModel { get; set; }

        public string Login   { get; set; }
        public string Domain  { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int    Port { get; set; }


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
                    cStream = tcp.GetStream();
                    state = ClientState.Connected;

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
            //UpdateStatus(App.ThisApp.Locally.LocaleStrings["Disconnected"]);
            //ViewModel.ButtonName = "Подключиться";

            if (!NewMessageTask.IsCompleted && Task.CurrentId != NewMessageTask.Id)
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
                this.Login    = (string)App.ThisApp.Client.ViewModel.GetPropertyValueThroughDispatcher(CMClientClientViewModel.UserLoginProperty);
                this.Domain   = (string)App.ThisApp.Client.ViewModel.GetPropertyValueThroughDispatcher(CMClientClientViewModel.UserDomainProperty);
                this.Password = (string)App.ThisApp.Client.ViewModel.GetPropertyValueThroughDispatcher(CMClientClientViewModel.UserPasswordProperty);

                //Доменная авторизация
                if (!String.IsNullOrEmpty(Domain))
                {
                    AuthorizationMessage.Message = new CoMessengerUser()
                    {
                        Login = Login,
                        PasswordHash = Password,
                        Domain = Domain,
                        IsBuiltIn = false
                    };
                }
                //Встроенная авторизация
                else
                {
                    AuthorizationMessage.Message = new CoMessengerUser()
                    {
                        Login = Login,
                        PasswordHash = MD5Helper.CreateMD5(Password),
                        IsBuiltIn = true
                    };
                }

                
            }

            App.ThisApp.Client.PutOutMessage(AuthorizationMessage);

            base.OnConnected();
        }

        protected virtual void OnAuthorizationSuccess(AuthorizationSuccessEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            App.ThisApp.CurrentUser = args.LoggedUser;

            //Если вручную вводили пароль, то сохраним данные для следующего входа
            if (!COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount)
            {
                App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                {
                    COMessengerClient.Properties.Settings.Default.UserLogin = App.ThisApp.Client.ViewModel.UserLogin;
                    COMessengerClient.Properties.Settings.Default.UserDomain = App.ThisApp.Client.ViewModel.UserDomain;
                    COMessengerClient.Properties.Settings.Default.UserPassword = App.ThisApp.Client.ViewModel.UserPassword;
                }));
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
                    if (value)
                    {
                        StayAliveThread = new Thread(new ThreadStart(StayAlive));
                        StayAliveThread.IsBackground = true;
                        StayAliveThread.Name = "StayAlive";

                        StayAliveThread.Start();
                    }

                    _IsStayAlive = value;
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
//                    Debug.WriteLine("{0}: message dequeued", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));
//#endif
                    switch (NewCMMessage.Kind)
                    {
                        case MessageKind.Ping: break;

                        case MessageKind.RoutedMessage:

                            RoutedMessage routedMessage = (RoutedMessage)NewCMMessage.Message;

                            RoutedMessagesHandler(routedMessage);


                            break;

                        //Успешная авторизация
                        case MessageKind.Authorization:

                            Trace.WriteLine("{0}: Авторизация успешна", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

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
                                        Message = MD5Helper.CreateMD5(NewPasswordPrompt.Text)
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

                        case MessageKind.Rooms:

                            Trace.WriteLine("{0}: Список пользователей", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            List<RoomPeer> Rooms = (List<RoomPeer>)NewCMMessage.Message;

                            Rooms.ForEach(Room =>
                                {
                                    //Добавляем комнату
                                    AddOrUpdateUser(Room);

                                    //Добавляем всех пользователей
                                    if (Room.IsMainRoom)
                                        Room.Participants.ForEach(Person => AddOrUpdateUser(Person));
                                });

                            break;

                        case MessageKind.Person:
                            
                            Trace.WriteLine("{0}: Новый пользователь", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            //CoMessengerUser NewUser = (CoMessengerUser)NewCMMessage.Message;
                            PersonPeer NewUser = (PersonPeer)NewCMMessage.Message;


                            //App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                            //{
                                AddOrUpdateUser(NewUser);

                                Trace.WriteLine("{0}: Пользователь обработан", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            //}));
                            break;

                        case MessageKind.Groups:
                            /*
                            Trace.WriteLine("{0}: Получен список групп", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            List<CoMessengerGroup> groups = (List<CoMessengerGroup>)NewCMMessage.Message;

                            App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                groups.ForEach(group => 
                                    {
                                        //Есть ли такой пользователь в списке?
                                        int groupindex = App.ThisApp.GroupList.IndexOf(group);

                                        if (groupindex >= 0)
                                            App.ThisApp.GroupList[groupindex] = group;
                                        else
                                            App.ThisApp.GroupList.Add(group);
                                    });

                                Trace.WriteLine("{0}: Список групп обработан", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));

                            }));
                            */
                            break;

                        default: break;
                    }

                }
            }
        }

        private static void AddOrUpdateUser(Peer peer)
        {
            App.ThisApp.Dispatcher.Invoke(new Action(() =>
            {
                //Есть ли такой пользователь в списке?
                if (App.ThisApp.ListOfConversations.ContainsKey(peer.PeerID))
                {
                    App.ThisApp.ListOfConversations[peer.PeerID].Peer = peer;
                    App.ThisApp.ListOfConversations.RaiseCollectionChanged(App.ThisApp.ListOfConversations[peer.PeerID], App.ThisApp.ListOfConversations[peer.PeerID], App.ThisApp.ListOfConversations.IndexOf(peer.PeerID));
                    //App.ThisApp.ListOfConversations[peer.PeerID] = new ClientPeer() { Peer = peer };
                }
                else
                    App.ThisApp.ListOfConversations.Add(peer.PeerID, new ClientPeer()
                    {
                        Peer = peer
                    }
                    );

                if (App.ThisApp.CurrentUser.UserId == peer.PeerID)
                    App.ThisApp.CurrentPeer = App.ThisApp.ListOfConversations[peer.PeerID];
            }));
        }
        
        private static void RoutedMessagesHandler(RoutedMessage routedMessage)
        {
            if (routedMessage.Receiver != App.ThisApp.CurrentUser.UserId)
                throw new InvalidOperationException("Кажется это сообщение не для нас!");

            if (!App.ThisApp.ListOfConversations.ContainsKey(routedMessage.Sender))
                throw new InvalidOperationException("Неизвестный отправитель!");

            lock (App.ThisApp.ListOfConversations[routedMessage.Sender])
            {
                App.ThisApp.ListOfConversations[routedMessage.Sender].ProcessMessage(routedMessage);
            }

        }
    }
}
