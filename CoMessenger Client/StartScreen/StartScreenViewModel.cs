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
using System.ComponentModel;
using COMessengerClient.Tools;

namespace COMessengerClient.StartScreen
{
    internal class StartScreenViewModel : NotifyPropertyChanged
    {
        private bool isLogonMode = true;

        public bool IsLogonMode
        {
            get { return isLogonMode; }
            set
            {
                isLogonMode = value;
                OnPropertyChanged("IsLogonMode");
            }
        }

        private string title = String.Empty;

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                OnPropertyChanged("Title");
        }
        }



        internal void OnLoad()
        {
            Title = App.Product;

            ConnectionManager.Client.AuthorizationSuccess +=
            (client, args) =>
            {
                Title = args.LoggedUser.DisplayName;
            };

            ConnectionManager.Client.ContactListLoaded +=
            (client, args) =>
            {
                IsLogonMode = false;
            };
        }

    }
}
