using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;
using CorporateMessengerLibrary;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Globalization;
using COMessengerClient.Conversation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

[assembly: CLSCompliant(true)]
namespace COMessengerClient
{

    public class ClientPeer
    {
        public Peer Peer{ get; set; }
        public ConversationView View { get; set; }

        private Queue<RoutedMessage> incomeMessages = new Queue<RoutedMessage>();

        public Queue<RoutedMessage> IncomeMessages
        {
            get { return incomeMessages; }
        }

        public string Status
        {
            get
            {
                if (Peer.Type == PeerType.Room)
                    return "GROUP";
                else
                    return ((PersonPeer)Peer).State.ToString();
            }
        }

        //public int IndexInCollection { get; set; }

    }

    public class ClientPeerComparer : Comparer<ClientPeer>
    {
        public override int Compare(ClientPeer x, ClientPeer y)
        {
            //if (x.Status == y.Status)
            return x.Peer.DisplayName.CompareTo(y.Peer.DisplayName);
            ////else if (x.Status == PersonStatus.Online)
            //    return -1;
            //else if (y.Status == PersonStatus.Online)
            //    return 1;
            //else
            //    return x.DisplayName.CompareTo(y.DisplayName);
        }
    }



    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static App ThisApp { get; set; }
        public CMClientClient Client { get; set; }
        public CoMessengerUser CurrentUser { get; set; }
        public LocalizationUI Locally { get; set; }
        //public List<ConversationModel> ListOfConversations { get; set; }

        //SortedSet<ConversationModel> asdf;

        private SortedObservableList<string, ClientPeer> listOfConversations = new SortedObservableList<string, ClientPeer>();

        public SortedObservableList<string, ClientPeer> ListOfConversations
        {
            get { return listOfConversations; }
        }

        //private Observablecollection<CoMessengerUser> userslist = new ObservableCollection<CoMessengerUser>();

        //public ObservableCollection<CoMessengerUser> UsersList
        //{
        //    get { return userslist; }
        //}

        //private ObservableCollection<CoMessengerGroup> groupList = new ObservableCollection<CoMessengerGroup>();

        //public ObservableCollection<CoMessengerGroup> GroupList
        //{
        //    get { return groupList; }
        //}
        
        

        //private static Thread ListenerThread = null;

        public App()
        {
            ThisApp = (App)App.Current;
            Locally = new LocalizationUI();
            Client = new CMClientClient();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            //GroupList.CollectionChanged += (collection, args) =>
            //    {
            //        if (args.Action == NotifyCollectionChangedAction.Add)
            //        {
            //            foreach (CoMessengerGroup newGroup in args.NewItems)
            //            {
            //                ListOfConversations.Add(newGroup.GroupID, new Peer(){Receiver = newGroup});
            //            }
            //        }
            //        else if (args.Action == NotifyCollectionChangedAction.Remove)
            //        {
            //            foreach (CoMessengerGroup deletedGroup in args.NewItems)
            //            {
            //                ListOfConversations.Remove(deletedGroup.GroupID);
            //            }
            //        }
            //    };

            //UsersList.CollectionChanged += (collection, args) =>
            //    {
            //        if (args.Action == NotifyCollectionChangedAction.Add)
            //        {
            //            int i = args.NewStartingIndex;

            //            foreach (CoMessengerUser newUser in args.NewItems)
            //            {
            //                ListOfConversations.Add(newUser.UserId, new Peer() { Receiver = newUser, IndexInCollection = i++ });
            //            }
            //        }
            //        else if (args.Action == NotifyCollectionChangedAction.Remove)
            //        {
            //            foreach (CoMessengerUser deletedUser in args.NewItems)
            //            {
            //                ListOfConversations.Remove(deletedUser.UserId);
            //            }
            //        }
            //    };


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
