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

        //public CMClientClient Client { get; set; }

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
            //Client = App.ThisApp.Client;
            //model = new StartScreenModel(this);
        }

        internal void OnLoad()
        {
            view.MainGrid.Children.Add(logOnScreen);

            logOnScreen.SetValue(Grid.ColumnSpanProperty, 3);


            ConnectionManager.Client.AuthorizationSuccess +=
            (client, args) =>
            {


                App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ConnectionManager.Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentCulture, App.ThisApp.Locally.LocaleStrings["Authorization success"]);

                    logOnScreen.Visibility = Visibility.Collapsed;

                    //view.Title = args.LoggedUser.DisplayName;

                    logOnScreen.IsBusy = false;
                }));


            };

            ConnectionManager.Client.ContactListLoaded +=
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
        }

    }
}
