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

namespace CorporateMessengerLibrary
{
    public static class CMClientCommands
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand ToggleConnection = RegisterNewCommand("ToggleConnection", "ToggleConnection", ToggleConnection_Executed, ToggleConnection_CanExecute);

        private static void ToggleConnection_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var client = COMessengerClient.App.ThisApp.Client as CMClientClient;

            //args.CanExecute = (client.state == ClientState.Connected || client.state == ClientState.Disconnected);
            e.CanExecute = (client.state != ClientState.Connecting && !String.IsNullOrWhiteSpace(client.ViewModel.UserLogin));
        }

        private static void ToggleConnection_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var client = COMessengerClient.App.ThisApp.Client as CMClientClient;
            if (client.state == ClientState.Disconnected || client.state == ClientState.Error)
            {
                client.AsynchronousConnectTo("localhost", 13000);
            }
            else if (client.state == ClientState.Connected)
            {
                client.Disconnect();
            }
            else
            {
                throw new InvalidOperationException("Неверное состояние подключения");
            }
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

            COMessengerClient.Properties.Settings.Default.PropertyChanged += 
                (sender, args) => 
                {
                    if (args.PropertyName == "UseCurrentWindowsAccount")
                        IsBuiltIn = !COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount;
                };

        }

        public string ButtonName
        {
            get { return (string)GetValue(ButtonNameProperty); }
            set 
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetValue(ButtonNameProperty, value);
                    }));
            }
        }

        // Using a DependencyProperty as the backing store for ButtonName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonNameProperty =
            DependencyProperty.Register("ButtonName", typeof(string), typeof(CMClientClientViewModel), new UIPropertyMetadata("Ошыбка"));



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
                string tmp = String.Empty;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                   tmp = (string)GetValue(UserLoginProperty);
                }));


                return tmp;
                //return (string)GetValue(UserLoginProperty); 
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




        public bool IsBuiltIn
        {
            get { return (bool)GetValue(IsBuiltInProperty); }
            set { SetValue(IsBuiltInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsBuiltIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsBuiltInProperty = 
    DependencyProperty.Register("IsBuiltIn", typeof(bool), typeof(CMClientClientViewModel), new UIPropertyMetadata((bool)true));

        


        public string UserPassword
        {
            get { return (string)GetValue(UserPasswordProperty); }
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

        



    }

    public class AuthorizationSuccessEventArgs : EventArgs
    {
        public CoMessengerUser LoggedUser { get; set; }

        public AuthorizationSuccessEventArgs(CoMessengerUser loggedUser)
        {
            LoggedUser = loggedUser;
        }
    }

    public class CMClientClient : CMClientBase
    {
        private Thread QueueProcessThread;
        private Thread StayAliveThread;

        private AutoResetEvent NewMessageEvent = new AutoResetEvent(false);
        private Task NewMessageTask;


        //public delegate void AuthorizationSuccessEventHandler(object sender, AuthorizationSuccessEventArgs args);
        public event EventHandler<AuthorizationSuccessEventArgs> AuthorizationSuccess;

        //public CMClientClientViewModel ViewModel = new CMClientClientViewModel();

        public CMClientClientViewModel ViewModel { get; set; }

        private void UpdateStatus(string newStatus)
        {
            ViewModel.ConnectionStatus = newStatus;
        }

        public CMClientClient()
        {
            ViewModel = new CMClientClientViewModel();

            

            UpdateStatus(App.ThisApp.Locally.LocaleStrings["Disconnected"]);
        }

        public void ConnectTo(string server, int port)
        {
            if (state == ClientState.Disconnected || state == ClientState.Error)
            {
                state = ClientState.Connecting;

                if (tcp == null)
                {
                    tcp = new TcpClient();
                }

                if (tcp.Connected)
                    throw new InvalidOperationException("Подключение уже установлено");

                try
                {
                    UpdateStatus(String.Format(CultureInfo.CurrentCulture, "Подключение к {0}:{1}", server, port.ToString(CultureInfo.InvariantCulture)));
                    tcp.Connect(server, port);
                    cStream = tcp.GetStream();
                    state = ClientState.Connected;
                    ViewModel.ButtonName = "Отключиться";
                    UpdateStatus(String.Format(CultureInfo.CurrentCulture, "Соединен с {0}:{1}", server, port.ToString(CultureInfo.InvariantCulture)));

                    OnConnection();
                }
                catch (SocketException e)
                {
                    state = ClientState.Error;
                    ViewModel.ButtonName = "Подключиться";
                    UpdateStatus( CustomUtilites.formatException(e) );

                    OnConnectionError(e);

                }
                //catch (Exception)
                //{
                //    state = ClientState.Error;
                //    ViewModel.ButtonName = "Подключиться";
                //    throw;
                //}
            }
        }

        public IAsyncResult AsynchronousConnectTo(string server, int port)
        {
            DelegateConnectTo connect_delegate = new DelegateConnectTo(this.ConnectTo);

            return connect_delegate.BeginInvoke(server, port, new AsyncCallback((a) => { }), new Object());
        }

        protected override void OnDisconnection()
        {
            IsStayAlive = false;
            IsProcessing = false;
            UpdateStatus(App.ThisApp.Locally.LocaleStrings["Disconnected"]);
            ViewModel.ButtonName = "Подключиться";

            if (!NewMessageTask.IsCompleted)
            {
                //Отпускаем задачу с паузы
                NewMessageEvent.Set();
                //и ждем пока она закончится
                NewMessageTask.Wait();
            }

            base.OnDisconnection();
        }

        protected override void OnNewMessage()
        {
            NewMessageEvent.Set();

            base.OnNewMessage();
        }

        protected override void OnConnection()
        {
            NewMessageTask = new Task(NewMessageHandler);
            NewMessageTask.Start();

            //Сперва отправляем наш логин/пароль

            CMMessage AuthorizationMessage = new CMMessage();

            AuthorizationMessage.Kind = MessageKind.Authorization;

            //Если используем учетку текущего пользователя Windows
            if (COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount)
            {
                AuthorizationMessage.Message = new WindowsUser()
                {
                    UserId = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,
                };
            }
                //Если в поле "Логин" введена учетка с доменом
            else if (ViewModel.UserLogin.Contains("@") || ViewModel.UserLogin.Contains("\\"))
            {
                AuthorizationMessage.Message = new WindowsUser()
                {
                    UserId = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,
                };
            }



            App.ThisApp.Client.PutOutMessage(new CMMessage()
            {
                Kind = MessageKind.Authorization,
                Message = new BuiltInUser()
                {
                    Login = App.ThisApp.Client.ViewModel.UserLogin,
                    PasswordHash = MD5Helper.CreateMD5(App.ThisApp.Client.ViewModel.UserPassword)
                }
            });

            base.OnConnection();
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
                    COMessengerClient.Properties.Settings.Default.UserPassword = App.ThisApp.Client.ViewModel.UserPassword;
                }));
            }

            if (AuthorizationSuccess != null)
                AuthorizationSuccess(this, args);
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

#if DEBUG
                Debug.WriteLine("{0}: Получен сигнал о новом сообщении", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));
#endif

                while (InMessagesCount > 0)
                {
                    CMMessage NewCMMessage = GetInMessage();
#if DEBUG
                    Debug.WriteLine("{0}: message dequeued", DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture));
#endif
                    switch (NewCMMessage.Kind)
                    {
                        case MessageKind.Ping: break;
                        case MessageKind.Text:
                        case MessageKind.RichText:


                            App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    App.ThisApp.ListOfConversations.Find(a => a != null).NewMessageHandler(NewCMMessage);
                                }));
                            break;

                        //Успешная авторизация
                        case MessageKind.Authorization:

                            OnAuthorizationSuccess( new AuthorizationSuccessEventArgs((CoMessengerUser)NewCMMessage.Message));

                            break;

                        //Ошибка при авторизации
                        case MessageKind.AuthorizationError:

                            MessageBox.Show((string)NewCMMessage.Message);
                            App.ThisApp.Client.Disconnect();

                            break;

                        //Требуется новый пароль
                        case MessageKind.AuthorizationNewPassword:

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

                            break;

                        default: break;
                    }

                }
            }
        }
    }
}
