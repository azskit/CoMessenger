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

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для LogOnScreen.xaml
    /// </summary>
    public partial class LogOnScreen : UserControl
    {
        public LogOnScreen()
        {
            InitializeComponent();
        }

        public bool isBusy 
        {
            get { return BusyIndicator.Visibility == System.Windows.Visibility.Visible; }
            set
            {
                if (value == true)
                {
                    App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BusyIndicator.Visibility = Visibility.Visible;
                    }));
                }
                else
                {
                    App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BusyIndicator.Visibility = Visibility.Hidden;
                    }));
                }
            }
        }
    }
}
