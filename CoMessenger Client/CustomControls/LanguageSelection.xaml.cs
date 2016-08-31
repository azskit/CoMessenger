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

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для LanguageSelection.xaml
    /// </summary>
    public partial class LanguageSelection : UserControl
    {
        public LanguageSelection()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.UserCultureUIInfo = ((ListBox)sender).SelectedItem as CultureInfo;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            LanguageList.IsOpen = true;
        }
    }
}
