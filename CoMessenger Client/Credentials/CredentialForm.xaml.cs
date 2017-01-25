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
        }

        //PasswordBox не поддерживает биндинг поля Password - поэтому обновляем ViewModel сами
        private void PasswordBoxElement_PasswordChanged(object sender, RoutedEventArgs e)
        {
            viewModel.Password = PasswordBoxElement.SecurePassword;
        }

        private void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !String.IsNullOrWhiteSpace(LoginTextBox.Text))
                PasswordBoxElement.Focus();
        }

        private void PasswordBoxElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && CMClientCommands.SignInCommand.CanExecute(this, null))
                CMClientCommands.SignInCommand.Execute(this, null);
        }
    }
}
