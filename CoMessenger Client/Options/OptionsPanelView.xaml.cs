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

namespace COMessengerClient.SettingsPanel
{
    /// <summary>
    /// Логика взаимодействия для SettingsPanelView.xaml
    /// </summary>
    public partial class SettingsPanelView : UserControl
    {
        public SettingsPanelView()
        {
            InitializeComponent();

            if (!String.IsNullOrEmpty(Properties.Settings.Default.Server) && !String.IsNullOrEmpty(Properties.Settings.Default.Port))
                ServerPort.Text = String.Format(CultureInfo.InvariantCulture, App.ThisApp.Locally.LocaleStrings["{0}:{1}"], Properties.Settings.Default.Server, Properties.Settings.Default.Port);
        }
    }
}
