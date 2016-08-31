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

namespace COMessengerClient.StartScreen
{
     public class StartScreenViewModel : DependencyObject
    {
        private StartScreenView view;
        //private StartScreenModel model;

        public CMClientClient Client { get; set; }

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
            view.MainGrid.Children.Add(new LogOnScreen());

            App.ThisApp.ListOfConversations.CollectionChanged += (collection, args) => { Client.ViewModel.ConnectionStatus = args.Action.ToString() + ": " + ((ClientPeer)args.NewItems[0]).Peer.DisplayName; };


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
                    //ChangeMainContent(new Conversation.ConversationView());

                    view.MainGrid.Children.Clear();

                    view.ConversationsGrid.SetValue(Grid.ColumnProperty, 0);

                    view.MainGrid.Children.Add(view.ConversationsGrid);


                    //view.TabControl.SetValue(Grid.ColumnProperty, 0);
                    //view.TabControl.Style = App.ThisApp.Resources["FlatTabControl"] as Style;
                    //view.TabControl.ItemContainerStyle = App.ThisApp.Resources["FlatTabItems"] as Style;

                    //view.MainGrid.Children.Add(view.TabControl);

                    ContactList.ContactListView conList = new ContactList.ContactListView();

                    conList.SetValue(Grid.ColumnProperty, 1);

                    view.MainGrid.Children.Add(conList);
                    
                }));
            };

            Client.AuthorizationError +=
                (client, args) =>
                {
                    string ErrorMessage = String.Empty;

                    switch ((ErrorKind)args.ErrorMessage.Message)
                    {
                        case ErrorKind.UserNotFound:
                            ErrorMessage =
                                String.Format(provider: CultureInfo.CurrentUICulture,
                                              format:   App.ThisApp.Locally.LocaleStrings["Person {0} is not registered on server {1}"],
                                              args:     new Object[]{String.IsNullOrEmpty(Client.Domain) ? Client.Login : Client.Domain + "\\" + Client.Login,
                                                                     Client.Server});
                            break;

                        case ErrorKind.UserNotPresented:
                            throw new InvalidOperationException("Получен ответ от сервера об отсутствии контекста пользователя");

                        case ErrorKind.WrongPassword:
                            ErrorMessage =
                                String.Format(provider: CultureInfo.CurrentUICulture,
                                              format:   App.ThisApp.Locally.LocaleStrings["{1}: Wrong password for user {0}"],
                                              args:     new Object[]{String.IsNullOrEmpty(Client.Domain)  ? Client.Login : Client.Domain + "\\" + Client.Login,
                                                        Client.Server});
                            break;

                        case ErrorKind.DomainCouldNotBeContacted:
                            ErrorMessage =
                                String.Format(provider: CultureInfo.CurrentUICulture,
                                              format:   App.ThisApp.Locally.LocaleStrings["Domain {0} couldn't be contacted"],
                                              args:     new Object[]{Client.Domain});
                            break;


                    }



                    Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error during authorization occurred - {0}"], ErrorMessage);
                };

            //model.StartClient();
        }
    }
}
