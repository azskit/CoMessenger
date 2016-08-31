using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace COMessengerClient.Credentials
{
    /// <summary>
    /// Interaction logic for CredentialForm.xaml
    /// </summary>
    public partial class CredentialForm : UserControl
    {
        private CredentialFormViewModel viewModel;

        public CredentialForm()
        {
            InitializeComponent();


            viewModel = CredentialFormModel.ViewModel;

            CredentialFormModel.FillViewModel(this.PasswordBoxElement);

            DataContext = viewModel;

            //CredentialFormModel.FillViewModel(DataContext as CredentialFormViewModel, PasswordBoxElement);

            //string login = Properties.Settings.Default.UserLogin;
            //string domain = Properties.Settings.Default.UserDomain;
            //System.Net.NetworkCredential 
            //Коль домена нет, то суем логин
            //if (String.IsNullOrEmpty(domain))
            //    LoginTextBox.Text = login;
            //else
            //    LoginTextBox.Text = string.Format(CultureInfo.InvariantCulture, App.ThisApp.Locally.LocaleStrings["{0}\\{1}"], domain, login);
        }

        //PasswordBox не поддерживает биндинг поля Password - поэтому обновляем ViewModel сами
        private void PasswordBoxElement_PasswordChanged(object sender, RoutedEventArgs e)
        {
            viewModel.Password = PasswordBoxElement.SecurePassword;
        }
    }
}
