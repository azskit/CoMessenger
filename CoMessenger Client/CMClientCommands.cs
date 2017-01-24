using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CorporateMessengerLibrary;
using System.Globalization;
using System.Windows;
using CorporateMessengerLibrary.CimProtocol;

namespace COMessengerClient
{
    public static class CMClientCommands
    {

        public static readonly RoutedUICommand SignInCommand = RegisterNewCommand("SignInCommand", "SignInCommand", SignIn_Executed, SignIn_CanExecute);

        public static readonly RoutedUICommand SignOutCommand = RegisterNewCommand("SignOutCommand", "SignOutCommand", SignOut_Executed, SignOut_CanExecute);

        private static void SignOut_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            //var client = COMessengerClient.App.ThisApp.Client as CMClientClient;

            e.CanExecute = (ConnectionManager.Client.State == ClientState.Connected);
        }

        private static void SignOut_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            ConnectionManager.Client.Disconnect();
            App.ThisApp.History.Close();

            if (App.ThisApp.MainWindow != null)
            {
                ((StartScreen.StartScreenView)App.ThisApp.MainWindow).ViewModel.IsLogonMode = true;
                ((StartScreen.StartScreenView)App.ThisApp.MainWindow).ViewModel.Title = App.Product;
                App.ThisApp.ListOfConversations.Clear();
                ((StartScreen.StartScreenView)App.ThisApp.MainWindow).ConversationsHost.Children.Clear();


            }

            ConnectionManager.Client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentCulture, App.ThisApp.Locally.LocaleStrings["Disconnected"]);
        }

        private static void SignIn_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var client = ConnectionManager.Client as CMClientClient;

            e.CanExecute = (
                //Состояние - не подключен
                client.State != ClientState.Connecting && client.State != ClientState.Connected

                //Вход либо с текущей учеткой, либо если задан логин
             && (    COMessengerClient.Properties.Settings.Default.UseCurrentWindowsAccount
                 || !String.IsNullOrWhiteSpace(COMessengerClient.Credentials.CredentialFormModel.GetCredentials().UserName))

                //Заполнены сервер и порт в настройках
             && !String.IsNullOrWhiteSpace(COMessengerClient.Properties.Settings.Default.Server)
             && !String.IsNullOrWhiteSpace(COMessengerClient.Properties.Settings.Default.Port)
                           );
        }

        private static void SignIn_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            //Credentials.CredentialForm form = e.Parameter as Credentials.CredentialForm;

            //HandleSecureString(form.PasswordBoxElement.SecurePassword);

            var client = ConnectionManager.Client as CMClientClient;

            string server = COMessengerClient.Properties.Settings.Default.Server;
            int port = Int32.Parse(COMessengerClient.Properties.Settings.Default.Port, CultureInfo.InvariantCulture);

            client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Connecting to {0}:{1}"], server, port.ToString(CultureInfo.InvariantCulture));
            client.AsynchronousConnectTo(server, port, new AsyncCallback((a) =>
            {
                //if (client.State == ClientState.Connected)
                //    client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Connected to {0}:{1}"], server, port.ToString(CultureInfo.InvariantCulture));
                //else if (client.State == ClientState.Error)
                //    client.ViewModel.ConnectionStatus = String.Format(CultureInfo.CurrentUICulture, App.ThisApp.Locally.LocaleStrings["Error occurred during connection to {0}:{1} - {2}"], server, port.ToString(CultureInfo.InvariantCulture), client.exception.Message);
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

}
