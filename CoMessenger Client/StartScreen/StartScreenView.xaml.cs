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
        //StartScreenViewModel viewmodel;

        internal StartScreenViewModel ViewModel { get; set; }

        private Window optionsWindow;

        public StartScreenView()
        {
            InitializeComponent();
            ViewModel = new StartScreenViewModel();
            this.DataContext = ViewModel;

            App.ThisApp.MainWindow = this;
            ContactList.StartScreen = this;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            ViewModel.OnLoad();

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


        // minimize to system tray when applicaiton is closed
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }


            if (Properties.Settings.Default.HideOnClose)
            {
                e.Cancel = true;

                this.Hide();
            }


            base.OnClosing(e);
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.ThisApp.Shutdown();
        }
    }
}
