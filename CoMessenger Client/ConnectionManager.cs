using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using CorporateMessengerLibrary;

namespace COMessengerClient
{
    public static class ConnectionManager
    {
        public static CMClientClient Client { get; set; }




        public static void InitClient()
        {
            Client = new CMClientClient();

            Client.AuthorizationSuccess += OnAuthorizationSuccess;
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisonnected;
            Client.ConnectionError += OnConnectionError;
            Client.AuthorizationError += OnAuthorizationError;
        }

        private static void OnConnectionError(object sender, System.IO.ErrorEventArgs e)
        {
            //Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error occurred - {0}"], e.GetException().Message);

            string title = App.ThisApp.Locally.LocaleStrings["Connection Error"];
            string text = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error occurred - {0}"], e.GetException().Message);


            App.Log.Add(
                title: title,
                text: text
                );

            App.TrayIcon.ShowBalloonTip(
                timeout: 5,
                tipTitle: title,
                tipText: text,
                tipIcon: System.Windows.Forms.ToolTipIcon.Error);

        }

        public static void Shutdown()
        {
            if (Client != null && Client.State == ClientState.Connected)
                Client.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.Disconnect });

            Client = null;
        }

        private static void OnConnected(object sender, EventArgs e)
        {
            App.TrayIcon.ShowBalloonTip(5, null, String.Format(CultureInfo.CurrentCulture, "Connected to {0}", ConnectionManager.Client.Server), System.Windows.Forms.ToolTipIcon.Info);
        }

        private static void OnDisonnected(object sender, EventArgs e)
        {
            foreach (ClientPeer peer in App.ThisApp.ListOfConversations)
            {
                if (peer.Peer.PeerType == PeerType.Person)
                {
                    peer.Peer.State = PeerStatus.Offline;
                    peer.UpdatePeer();
                }
            }

            App.TrayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/TrayRed.ico")).Stream);
            App.TrayIcon.ShowBalloonTip(5, null, String.Format(CultureInfo.CurrentCulture, "Disconnected"), System.Windows.Forms.ToolTipIcon.Info);

        }


        private static void OnAuthorizationSuccess(object sender, AuthorizationSuccessEventArgs e)
        {
            //Открываем историю
            App.ThisApp.History.InitHistoryManager(
                         storageCatalog: System.IO.Path.Combine(App.StorageCatalog, App.ThisApp.CurrentUserId),
                         keepConnection: true);

            App.TrayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/TrayGreen.ico")).Stream);

            App.TrayIcon.ShowBalloonTip(5, null, String.Format(CultureInfo.CurrentCulture, "Authorization successful"), System.Windows.Forms.ToolTipIcon.Info);
        }

        private static void OnAuthorizationError(object sender, AuthorizationErrorEventArgs args)
        {
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

            //Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error during authorization occurred - {0}"], ErrorMessage);

            App.Log.Add(
                        title: App.ThisApp.Locally.LocaleStrings["Authorization Error"],
                        text: String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error during authorization occurred - {0}"], ErrorMessage)
                        );


        }


        //private void OnAuthorizationSuccess()


    }
}
