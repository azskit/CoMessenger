using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System;

namespace COMessengerClient.StartScreen
{
    /// <summary>
    /// Interaction logic for StartScreenView.xaml
    /// </summary>
    public partial class StartScreenView : Window
    {
        StartScreenViewModel viewmodel;

        private Window optionsWindow;

        //private TabControl tabControl = new TabControl();

        //public TabControl TabControl
        //{
        //    get { return tabControl; }
        //    set { tabControl = value; }
        //} 

        //private Grid conversationsGrid = new Grid();

        //public Grid ConversationsGrid
        //{
        //    get { return conversationsGrid; }
        //    set { conversationsGrid = value; }
        //}

        private System.Windows.Forms.NotifyIcon m_notifyIcon;

        public StartScreenView()
        {
            InitializeComponent();
            viewmodel = new StartScreenViewModel(this);
            this.DataContext = viewmodel;

            m_notifyIcon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.BalloonTipTitle = "Зоголовок сообщения";
            m_notifyIcon.BalloonTipText = "Появляется когда мы помещаем иконку в трэй";

            m_notifyIcon.Text = "Это у нас пишется если мы наведем мышку на нашу иконку в трэее";
            //m_notifyIcon.Icon = new System.Drawing.Icon(typeof(Control), "Resources\\Icons\\TrayOnline.ico"); ;

            System.IO.Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/online3.ico")).Stream;
            m_notifyIcon.Icon = new System.Drawing.Icon(iconStream);

            m_notifyIcon.Click += (a, b) => { WindowState = System.Windows.WindowState.Normal;};
            m_notifyIcon.Visible = true; 

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            viewmodel.OnLoad();

            App.DpiYScalingFactor = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M22;
            App.DpiXScalingFactor = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;
        }

        private void ShowOptions(object sender, RoutedEventArgs e)
        {
            if (optionsWindow == null)
            {

                optionsWindow = new SettingsPanel.SettingsWindow();

                optionsWindow.Closed += (window, args) => { this.optionsWindow = null; };

                optionsWindow.Show();
            }
            else
            {
                optionsWindow.Activate();
            }

        }

    }
}
