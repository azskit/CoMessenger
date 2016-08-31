using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Globalization;
using System.Security;
using System.Net;
using System.Runtime.InteropServices;

namespace COMessengerClient.Credentials
{
    public static class CredentialFormModel
    {
        private static CredentialFormViewModel viewModel;
        
        public static CredentialFormViewModel ViewModel
        {
            get
            {
                if (viewModel != null)
                    return viewModel;

                viewModel = new CredentialFormViewModel();

                return viewModel;
            }
        }
        
        internal static void FillViewModel(System.Windows.Controls.PasswordBox pbox)
        {
            viewModel.Domain = Properties.Settings.Default.UserDomain;
            viewModel.UserName = Properties.Settings.Default.UserLogin;

            if (Properties.Settings.Default.SavePassword)
            {
                byte[] entropy = { 20, 54, 9, 1, 22, 85 };
                byte[] encryptedPassword;
                byte[] decryptedPassword;

                string credentialsFile = Path.Combine(App.Home, "credentials.dat");
                if (File.Exists(credentialsFile))
                {
                    encryptedPassword = File.ReadAllBytes(credentialsFile);
                    decryptedPassword = ProtectedData.Unprotect(encryptedPassword, entropy, DataProtectionScope.CurrentUser);

                    pbox.Password = "********";
                    viewModel.Password.Clear();

                    foreach (char passwordchar in Encoding.Unicode.GetChars(decryptedPassword))
                    {
                        viewModel.Password.AppendChar(passwordchar);
                    }
                }
            }
        }

        internal static void SaveCredentials(NetworkCredential credentials)
        {
            Properties.Settings.Default.UserDomain = credentials.Domain;
            Properties.Settings.Default.UserLogin = credentials.UserName;


            if (Properties.Settings.Default.SavePassword)
            {
                string credentialsFile = Path.Combine(App.Home, "credentials.dat");

                Directory.CreateDirectory(App.Home);

                byte[] entropy = { 20, 54, 9, 1, 22, 85 };
                byte[] encryptedPassword;
                byte[] decryptedPassword;

                IntPtr valuePtr = IntPtr.Zero;
                try
                {
                    valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(credentials.SecurePassword);

                    decryptedPassword = new byte[credentials.SecurePassword.Length * 2];
                    for (int i = 0; i < credentials.SecurePassword.Length * 2; i++)
                    {
                        decryptedPassword.SetValue(Marshal.ReadByte(valuePtr, i), i);
                    }
                    encryptedPassword = ProtectedData.Protect(decryptedPassword, entropy, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(credentialsFile, encryptedPassword);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
                }
            }
        }

        internal static NetworkCredential GetCredentials()
        {
            return new NetworkCredential(viewModel.UserName, viewModel.Password, viewModel.Domain);
        }


    }
}
