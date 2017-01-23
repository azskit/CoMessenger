using System;
using System.Collections.Generic;
using System.Windows;
using CorporateMessengerLibrary;
using System.Globalization;
using COMessengerClient.Conversation;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//using System.Windows.Media.Imaging;
//using System.Drawing;

[assembly: CLSCompliant(true)]
namespace COMessengerClient
{
    public class PeerViewModel
    {
        private ClientPeer parent;

        public PeerViewModel(ClientPeer theParent)
        {
            parent = theParent;
        }

        //public string MessagesInQueue
        //{
        //    get
        //    {
        //        if (parent.IncomeMessages.Count > 0)
        //            return parent.IncomeMessages.Count.ToString();
        //        else
        //            return String.Empty;
        //    }
        //}


        private bool hasUnreadMessages = false;

        public bool HasUnreadedMessages
        {
            get { return hasUnreadMessages; }
            set 
            { 
                hasUnreadMessages = value;
                parent.OnPropertyChanged("ViewModel");
            }
        }

        public Color StatusColor
        {
            get
            {
                if (HasUnreadedMessages)
                    return Colors.Orange;


                if (parent.Peer != null)
                {
                    if (parent.Peer.Type == PeerType.Room)
                        return Colors.Gray;
                    else
                    {
                        if (((PersonPeer)parent.Peer).State == PersonStatus.Online)
                            return Colors.LightGreen;
                        else if (((PersonPeer)parent.Peer).State == PersonStatus.Offline)
                            return Colors.Red;
                        else
                            return Colors.Transparent;
                    }
                }
                else
                    return Colors.Transparent;
            }
        }

        //public string Status
        //{
        //    get
        //    {
        //        if (parent.Peer.Type == PeerType.Room)
        //            return "GROUP";
        //        else
        //            return ((PersonPeer)parent.Peer).State.ToString();
        //    }
        //}

        public bool HasAvatar { get { return parent.Peer != null && parent.Peer.Avatar != null; } }

    }

    public class ClientPeer : INotifyPropertyChanged
    {
        private Peer peer;
        public bool StatusChanged { get; set; }

        public Peer Peer
        { 
            get { return peer;}
            set
            {
                if (peer != value)
                {

                    if (peer != null && value != null && !(value is RoomPeer) && !(((PersonPeer)peer).State == ((PersonPeer)value).State))
                    {
                        peer = value;
                        StatusChanged = true;
                        OnPropertyChanged("Peer");
                        OnPropertyChanged("ViewModel");
                    }
                    else
                    {
                        peer = value;
                        OnPropertyChanged("Peer");
                        OnPropertyChanged("ViewModel");
                    }

                    History = new HistoryManager(App.StorageCatalog, App.ThisApp.CurrentUser.UserId, Peer.PeerID);
                }
            }
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }


        public HistoryManager History { get; set; }

        private ConversationView view;

        public ConversationView View 
        {
            get
            {
                if (view == null)
                {
                    //Читаем историю сообщений за сегодня


                    List<HistoryEntry> historyList = History.LoadHistory(DateTime.Now);

                    App.ThisApp.Dispatcher.Invoke(new Action(() =>
                       {
                           view = new ConversationView(App.ThisApp.MainWindow, this);
                       }));

                    if (historyList.Count != 0)
                    {
                        App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            historyList.ForEach(historyEntry =>
                                {
                                    ConversationModel.AddNewMessage
                                        (type:    historyEntry.Type,                                                     //Тип (входящее/исходящее)
                                         blocks:  ConversationModel.ExtractBlocks(historyEntry.routedMessage),           //Тело сообщения
                                         conView: view,                                                                  //Окно беседы
                                         peer:    App.ThisApp.ListOfConversations[historyEntry.routedMessage.Sender],    //Собеседник, отправлявший сообщение
                                         time:    historyEntry.routedMessage.SendTime);                                  //Время отправки
                                });

                        }));
                    }


                }

                return view;
            }
        }


        //private static BinaryFormatter fmt = new BinaryFormatter();

        private PeerViewModel viewModel;

        public PeerViewModel ViewModel
        {
            get { return viewModel; }
            set { viewModel = value; }
        }

        public ClientPeer()
        {
            viewModel = new PeerViewModel(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ProcessMessage(RoutedMessage routedMessage)
        {
            if (routedMessage == null)
                throw new ArgumentNullException("routedMessage");

            //Сохраняем долбаное сообщение
            History.Save(new HistoryEntry(){routedMessage = routedMessage, Type = MessageType.Income});

            ConversationModel.NewMessageHandler(routedMessage, View);

            OnPropertyChanged("ViewModel");

        }

    }

    public class ClientPeerComparer : Comparer<ClientPeer>
    {
        public override int Compare(ClientPeer x, ClientPeer y)
        {
            if (x == null)
                throw new ArgumentNullException("x");

            if (y == null)
                throw new ArgumentNullException("y");

            //Разные типы - у комнат приоритет
            if (x.Peer.Type != y.Peer.Type)
                return x.Peer.Type == PeerType.Room ? -1 : 1;
            
            //Если два собеседника
            if (x.Peer.Type == PeerType.Person)
            {
                //Если разные статусы, то приоритет у тех кто онлайн
                if (((PersonPeer)x.Peer).State != ((PersonPeer)y.Peer).State)
                    return ((PersonPeer)x.Peer).State == PersonStatus.Online ? -1 : 1;
            }
                      
            //Во всех остальных случаях сортируем по алфавиту
            //return x.Peer.DisplayName.CompareTo(y.Peer.DisplayName);
            return String.Compare(x.Peer.DisplayName, y.Peer.DisplayName, StringComparison.CurrentCulture);
        }
    }



    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string StorageCatalog = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Storage");
        public static App ThisApp { get; set; }
        public CMClientClient Client { get; set; }
        public CoMessengerUser CurrentUser { get; set; }
        public ClientPeer CurrentPeer { get; set; }
        public LocalizationUI Locally { get; set; }
        //public List<ConversationModel> ListOfConversations { get; set; }

        //SortedSet<ConversationModel> asdf;

        private IndexedObservableCollection<string, ClientPeer> listOfConversations = new IndexedObservableCollection<string, ClientPeer>();

        public IndexedObservableCollection<string, ClientPeer> ListOfConversations
        {
            get { return listOfConversations; }
        }

        public App()
        {
            ThisApp = (App)App.Current;
            Locally = new LocalizationUI();
            Client = new CMClientClient();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            global::COMessengerClient.Properties.Settings.Default.PropertyChanged += (settings, eventargs) =>
            {
                if (eventargs.PropertyName.Equals("UserCultureUIInfo"))
                    Locally.Load(global::COMessengerClient.Properties.Settings.Default.UserCultureUIInfo);
            };

            if (global::COMessengerClient.Properties.Settings.Default.UserCultureUIInfo == CultureInfo.InvariantCulture)
                global::COMessengerClient.Properties.Settings.Default.UserCultureUIInfo = CultureInfo.CurrentUICulture;

            Locally.Load(global::COMessengerClient.Properties.Settings.Default.UserCultureUIInfo);

            Client.Connected +=
                    (client, args) =>
                    {
                        Client.IsProcessing = true;
                        Client.IsStayAlive = true;
                    };

        }


        protected override void OnExit(ExitEventArgs e)
        {
            COMessengerClient.Properties.Settings.Default.Save();

            Client = null;

            base.OnExit(e);
            

            //if (ListenerThread != null) ListenerThread.Abort();
        }

    }
}
