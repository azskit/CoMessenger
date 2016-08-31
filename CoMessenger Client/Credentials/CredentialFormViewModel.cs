using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.ComponentModel;
using System.Globalization;

namespace COMessengerClient.Credentials
{
    public class CredentialFormViewModel : INotifyPropertyChanged, IDisposable
    {
        private string userName;

        public string UserName
        {
            get { return userName; }
            set 
            {
                if (value != userName)
                {
                    userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        private string userNameWithDomain;

        public string UserNameWithDomain
        {
            get 
            {
                if (userNameWithDomain != null)
                    return userNameWithDomain;

                return !String.IsNullOrEmpty(domain) ? String.Format(CultureInfo.CurrentCulture, App.ThisApp.Locally.LocaleStrings["{0}\\{1}"], domain, userName) : userName; 
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                string DomainUser = (string)value;

                string[] retValues = new string[2];

                if (DomainUser.Contains("\\"))
                {
                    retValues = DomainUser.Split('\\');
                    Domain = retValues[0];
                    UserName = retValues[1];
                }
                else if (DomainUser.Contains("@"))
                {
                    retValues = DomainUser.Split('@');
                    UserName = retValues[0];
                    Domain = retValues[1];
                }
                else
                {
                    UserName = DomainUser;
                    Domain = String.Empty;
                }

                userNameWithDomain = value;
            }
        }


        private string domain;

        public string Domain
        {
            get { return domain; }
            set
            {
                if (value != domain)
                {
                    domain = value;
                    OnPropertyChanged("Domain");
                }
            }
        }

        private SecureString password = new SecureString();

        public SecureString Password
        {
            get { return password; }
            set
            {
                if (value != password)
                {
                    password = value;
                    OnPropertyChanged("Password");
                }
            }
        }


        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                password.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }
}
