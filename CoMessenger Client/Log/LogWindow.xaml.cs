using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace COMessengerClient.Log
{
    /// <summary>
    /// Логика взаимодействия для LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        public void Add(string title, string text)
        {
            App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
            {

                LogTextBox.AppendText(String.Format(CultureInfo.CurrentCulture, "{0} {1}: {2}\r\n", DateTime.Now.ToString(CultureInfo.CurrentCulture), title, text));
            }));
        }
    }
}
