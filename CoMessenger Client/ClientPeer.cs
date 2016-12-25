using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using COMessengerClient.Conversation;
using CorporateMessengerLibrary;
using System.Linq;

namespace COMessengerClient
{

    public class PeerViewModel
    {

        private ClientPeer parent;

        public PeerViewModel(ClientPeer theParent)
        {
            parent = theParent;
        }

        private bool hasUnreadMessages = false;

        public bool HasUnreadMessages
        {
            get { return hasUnreadMessages; }
            set
            {
                //System.Diagnostics.Trace.WriteLine("hasUnreadMessage = " + value, "hasUnreadMessage");
                hasUnreadMessages = value;
                parent.OnPropertyChanged("ViewModel");
            }
        }

        public Color StatusColor
        {
            get
            {
                if (parent.Peer != null)
                {
                    if (parent.Peer.PeerType == PeerType.Room)
                    {

                        RoomPeer room = parent.Peer as RoomPeer;

                        if (room.Participants.Contains(App.ThisApp.CurrentPeer.Peer as PersonPeer, PersonPeer.EqualityComparer))
                            return Color.FromArgb(0x80, 0x00, 0x00, 0xFF); //Blue
                        else
                            return Color.FromArgb(0x80, 0x80, 0x80, 0x80); //Grey
                    }
                    else
                    {
                        if (((PersonPeer)parent.Peer).State == PeerStatus.Online)
                            return Color.FromArgb(0x80, 0x90, 0xEE, 0x90); //LightGreen
                        else if (((PersonPeer)parent.Peer).State == PeerStatus.Offline)
                            return Color.FromArgb(0x80, 0xFF, 0x00, 0x00); //Red
                        else
                            return Colors.Transparent;
                    }
                }
                else
                    return Colors.Transparent;
            }
        }

        public string Description
        {
            get
            {
                if (parent.Peer.PeerType == PeerType.Room)
                    return "Комната";
                else
                    return ((PersonPeer)parent.Peer).State == PeerStatus.Online ? "В сети" : "Не в сети";
            }
        }

        public bool HasAvatar { get { return parent.Peer != null && parent.Peer.Avatar != null; } }

        private ICollectionView participants;
        public ICollectionView Participants
        {
            get
            {
                if (participants != null)
                    return participants;

                if (!(parent.Peer.PeerType == PeerType.Room))
                {
                    return null;
                }

                participants = new System.Windows.Data.CollectionViewSource { Source = App.ThisApp.ListOfConversations }.View;

                Predicate<object> filter = (user) => ((RoomPeer)parent.Peer).Participants.Contains<PersonPeer>(((ClientPeer)user).Peer as PersonPeer, PersonPeer.EqualityComparer);

                participants.Filter = filter;
                
                //participants.CollectionChanged += (a, b) => { System.Windows.MessageBox.Show("updated"); };
                //TODO допилить, чтобы фильтр применялся не каждый раз, а только при изменениях в комнате
                App.ThisApp.ListOfConversations.CollectionChanged += 
                    (a, b) => 
                    { 
                        if (b.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && b.NewItems.Contains(parent))
                            participants.Filter = filter; 
                    };

                return participants;

            }
        }
    }

    public class ClientPeer : INotifyPropertyChanged
    {
        private Peer peer;
        public bool StatusChanged { get; set; }

        public void UpdatePeer()
        {
            OnPropertyChanged("Peer");
            OnPropertyChanged("ViewModel");
        }

        public void UpdatePeer(Peer newPeer)
        {
            if (newPeer == null)
                throw new ArgumentNullException("newPeer");

            if (peer == null)
                peer = newPeer;
            else
            {
                peer.Avatar = newPeer.Avatar;
                peer.DisplayName = newPeer.DisplayName;

                if (peer.State != newPeer.State)
                {
                    StatusChanged = true;
                    peer.State = newPeer.State;
                }

                if (peer.PeerType == PeerType.Room)
                {
                    //((RoomPeer)peer).Participants = ((RoomPeer)newPeer).Participants;

                    RoomPeer Room = peer as RoomPeer;

                    Room.Participants.Clear();
                    Room.Participants.AddRange(((RoomPeer)newPeer).Participants);

                }
            }

            UpdatePeer();
        }

        public Peer Peer
        {
            get { return peer; }
            /*
            private set
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

                    //iHistory = new IndexedHistoryManager
                    //    (storageCatalog: System.IO.Path.Combine(App.StorageCatalog, App.ThisApp.CurrentUser.UserId),
                    //     keepConnection: true);
                }
            }
            */
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }


        //public HistoryManager History { get; set; }
        //public IndexedHistoryManager iHistory { get; set; }

        //private ConversationView view;

        //public ConversationView View
        //{
        //    get
        //    {
        //        return view;
        //    }
        //}

        public ConversationView View { get; set; }

        internal void InitView()
        {
            if (View == null)
            {
                App.ThisApp.Dispatcher.Invoke(new Action(() =>
                {
                    View = new ConversationView(App.ThisApp.MainWindow, this) { Visibility = System.Windows.Visibility.Hidden };
                }));
            }
        }


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

            if (View == null) InitView();

            //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Сохраняем сообщение");

            //Сохраняем долбаное сообщение
            if (Peer.PeerType == PeerType.Person)
                routedMessage.PreviousMessageId = routedMessage.PreviousMessageId = App.ThisApp.History.GetLastMessageBetween(routedMessage.Sender, routedMessage.Receiver, routedMessage.SendTime);

            App.ThisApp.History.Save(routedMessage);

            //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Сообщение сохранено");
             

            ConversationModel.NewMessageHandler(routedMessage, View);

            OnPropertyChanged("ViewModel");

        }

        //дерьмо какое-то, непомню зачем написал
        //public void ProcessHistoryQuery(HistoryQuery query)
        //{
        //    if (query == null)
        //        throw new ArgumentNullException("answer");

        //    if (query.Content.Count > 0)
        //    {
        //        App.ThisApp.History.SaveMessages(query.Content);

        //        if (App.ThisApp.History.OnHistoryQueryProcessed != null)
        //        {
        //            App.ThisApp.History.OnHistoryQueryProcessed();
        //            App.ThisApp.History.OnHistoryQueryProcessed = null;
        //        }
        //    }
        //}

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
            if (x.Peer.PeerType != y.Peer.PeerType)
                return x.Peer.PeerType == PeerType.Room ? -1 : 1;

            //Если два собеседника
            if (x.Peer.PeerType == PeerType.Person)
            {
                //Если разные статусы, то приоритет у тех кто онлайн
                if (((PersonPeer)x.Peer).State != ((PersonPeer)y.Peer).State)
                    return ((PersonPeer)x.Peer).State == PeerStatus.Online ? -1 : 1;
            }

            //Во всех остальных случаях сортируем по алфавиту
            //return x.Peer.DisplayName.CompareTo(y.Peer.DisplayName);
            return String.Compare(x.Peer.DisplayName, y.Peer.DisplayName, StringComparison.CurrentCulture);
        }
    }
}