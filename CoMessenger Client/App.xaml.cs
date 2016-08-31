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
using System.Reflection;
using System.Diagnostics;
using COMessengerClient.CustomControls;
//using System.Windows.Media.Imaging;
//using System.Drawing;

[assembly: CLSCompliant(true)]
namespace COMessengerClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string StorageCatalog { get; private set; }
        public static App ThisApp { get; set; }
        public CMClientClient Client { get; set; }
        public CoMessengerUser CurrentUser { get; set; }
        public ClientPeer CurrentPeer { get; set; }
        public LocalizationUI Locally { get; set; }

        public SoundManager Sound { get; private set; }

        public SearchListWindow SearchWindow { get; set; }

        public static string Company { get; private set; }
        public static string Product { get; private set; }
        public static string Version { get; private set; }

        public static string Home { get; private set; }
        
        public static Stopwatch sw = new Stopwatch();

        //public List<ConversationView> ListOfConversations { get; set; }

        //SortedSet<ConversationModel> asdf;

        private IndexedObservableCollection<string, ClientPeer> listOfConversations = new IndexedObservableCollection<string, ClientPeer>();

        public IndexedObservableCollection<string, ClientPeer> ListOfConversations
        {
            get { return listOfConversations; }
        }

        public static ClientPeer FoundPeer(string peerId)
        {
            if (!App.ThisApp.ListOfConversations.ContainsKey(peerId))
            {
                ClientPeer Unknown = new ClientPeer();

                Unknown.UpdatePeer(new PersonPeer()
                    {
                        DisplayName = "Неизвестный отправитель (пользователь удален)",
                        PeerID = peerId, 
                        Type = PeerType.Person, 
                        State = PeerStatus.Deleted                        
                    });

                return Unknown;
            }
            else
                return App.ThisApp.ListOfConversations[peerId];
        }

        public IndexedHistoryManager History;




        public App()
        {



            ThisApp = (App)App.Current;
            Locally = new LocalizationUI();
            Client = new CMClientClient();

            Company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                        Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false))
                       .Company;

            Product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(
                        Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute), false))
                       .Product;

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Home = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Company, Product);

            StorageCatalog = System.IO.Path.Combine(App.Home, "Storage");

            Sound = new SoundManager();

            //При авторизации открываем файл истории
            Client.AuthorizationSuccess +=
                            (client, args) =>
                            {
                                History = new IndexedHistoryManager
                                            (storageCatalog: System.IO.Path.Combine(App.StorageCatalog, App.ThisApp.CurrentUser.UserId),
                                             keepConnection: true);
                            };

            //При отключении - закрываем
            //upd нет, не закрываем, так как связь может прерваться временно. Закрываем командой SignOut
            //Client.Disconnected += (client, args) => {History = null;};


            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
            {
                MessageBox.Show("Global exception: " + args.ExceptionObject.ToString());
            };


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

            if (Client != null && Client.state == ClientState.Connected)
                Client.PutOutMessage(new CMMessage() { Kind = MessageKind.Disconnect });

            Client = null;

            base.OnExit(e);
            

            //if (ListenerThread != null) ListenerThread.Abort();
        }


        public static double DpiYScalingFactor { get; set; }

        public static double DpiXScalingFactor { get; set; }
    }
}
