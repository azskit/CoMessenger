using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using CorporateMessengerLibrary;
using COMessengerClient.CustomControls;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;

namespace COMessengerClient.StartScreen
{
     public class StartScreenViewModel : DependencyObject
    {
        private StartScreenView view;

        ContactList.ContactListView conList;

        //private StartScreenModel model;

        public CMClientClient Client { get; set; }

        private LogOnScreen logOnScreen = new LogOnScreen();

        //public StartScreenViewModel()
        //{
        //}

        //public void ChangeMainContent(UIElement newContent)
        //{
        //    view.MainGrid.Children.Clear();

        //    view.MainGrid.Children.Add(newContent);
        //}

        public StartScreenViewModel(Window viewParameter)
        {
            view = viewParameter as StartScreenView;
            Client = App.ThisApp.Client;
            //model = new StartScreenModel(this);
        }

        internal void OnLoad()
        {
            view.MainGrid.Children.Add(logOnScreen);

            //App.ThisApp.ListOfConversations.CollectionChanged += (collection, args) => { Client.ViewModel.ConnectionStatus = args.Action.ToString() + ": " + ((ClientPeer)args.NewItems[0]).Peer.DisplayName; };
             
            Client.Connecting += 
                (clients, args) => 
                {
                    logOnScreen.isBusy = true;
                };

            Client.ConnectionError +=
            (client, args) =>
            {
                Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error occurred - {0}"], Client.exception.Message);
            };

            App.ThisApp.Client.AuthorizationSuccess +=
            (client, args) =>
            {


                App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentCulture, App.ThisApp.Locally.LocaleStrings["Authorization success"]);

                    logOnScreen.Visibility = Visibility.Collapsed;

                    //view.MainGrid.Children.Clear();

                    //view.ConversationsGrid.SetValue(Grid.ColumnProperty, 0);

                    //view.MainGrid.Children.Add(view.ConversationsGrid);

                    view.Title = args.LoggedUser.DisplayName;

                    logOnScreen.isBusy = false;
                }));


            };

            App.ThisApp.Client.ContactListLoaded +=
            (client, args) =>
            {
                if (conList == null)
                {

                    App.ThisApp.Dispatcher.Invoke(new Action(() =>
                    {
                        GridSplitter splitter = new GridSplitter();
                        splitter.Width = 20;
                        splitter.ShowsPreview = true;
                        splitter.VerticalAlignment = VerticalAlignment.Stretch;
                        splitter.HorizontalAlignment = HorizontalAlignment.Center;
                        splitter.Background = new SolidColorBrush(Colors.Transparent);

                        splitter.SetValue(Grid.ColumnProperty, 1);
                        view.MainGrid.Children.Add(splitter);


                        conList = new ContactList.ContactListView();

                        conList.SetValue(Grid.ColumnProperty, 2);

                        view.MainGrid.Children.Add(conList);

                        //System.Diagnostics.Trace.WriteLine(Client.ViewModel.ConnectionStatus + " " + DateTime.Now.ToString("HH:mm:ss.ffff") + " Contact list showed");
                        //Client.ViewModel.ConnectionStatus = Client.ViewModel.ConnectionStatus + " " + DateTime.Now.ToString("HH:mm:ss.ffff") + " Contact list showed";
                    }));
                }
            };

            Client.AuthorizationError +=
                (client, args) =>
                {
                    OnAuthorizationError(args);
                };

            Client.Disconnected += (a, b) => 
            {
                foreach (ClientPeer peer in App.ThisApp.ListOfConversations)
	            {
                    if (peer.Peer.Type == PeerType.Person)
                    {
                        peer.Peer.State = PeerStatus.Offline;
                        peer.UpdatePeer();
                    }
	            }

                logOnScreen.isBusy = false;

            };

            Client.ConnectionError += (a,b)=>{logOnScreen.isBusy = false;};

            //model.StartClient();
        }

        private void OnAuthorizationError(AuthorizationErrorEventArgs args)
        {
            logOnScreen.isBusy = false;

            string ErrorMessage = String.Empty;

            switch ((ErrorKind)args.ErrorMessage.Message)
            {
                case ErrorKind.UserNotFound:
                    ErrorMessage =
                        String.Format(provider: CultureInfo.CurrentUICulture,
                                      format: App.ThisApp.Locally.LocaleStrings["User {0} is not registered on server {1}"],
                                      args: new Object[]{String.IsNullOrEmpty(Client.Domain) ? Client.Login : Client.Domain + "\\" + Client.Login,
                                                                     Client.Server});
                    break;

                case ErrorKind.UserNotPresented:
                    throw new InvalidOperationException("Получен ответ от сервера об отсутствии контекста пользователя");

                case ErrorKind.WrongPassword:
                    ErrorMessage =
                        String.Format(provider: CultureInfo.CurrentUICulture,
                                      format: App.ThisApp.Locally.LocaleStrings["{1}: Wrong password for user {0}"],
                                      args: new Object[]{String.IsNullOrEmpty(Client.Domain)  ? Client.Login : Client.Domain + "\\" + Client.Login,
                                                        Client.Server});
                    break;

                case ErrorKind.DomainCouldNotBeContacted:
                    ErrorMessage =
                        String.Format(provider: CultureInfo.CurrentUICulture,
                                      format: App.ThisApp.Locally.LocaleStrings["Domain {0} couldn't be contacted"],
                                      args: new Object[] { Client.Domain });
                    break;


            }



            Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error during authorization occurred - {0}"], ErrorMessage);
        }
    }
}
