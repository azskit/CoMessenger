using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace COMessengerClient.Notifications
{
    public partial class NotificationTemplate : ResourceDictionary
    {
        public static List<Window> OpenedNotifications = new List<Window>();

        public NotificationTemplate()
        {
            InitializeComponent();
        }

        public void CloseButtonClick(object sender, EventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null && OpenedNotifications.Contains(window))
            {
                window.Close();
            }
        }

        public void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null && OpenedNotifications.Contains(window))
            {
                window.Close();
            }
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window window = sender as Window;

            //window.Owner = App.ThisApp.MainWindow;

            //window.Owner = HidingHelper; // Okey, this will result to disappear icon for main window.
            //HidingHelper.Hide(); // Hide helper window just in case


            //Window window = ((FrameworkElement)sender).TemplatedParent as Window;

            window.Closing += (a, b) => { OpenedNotifications.Remove(window); };

            var primaryMonitorArea = SystemParameters.WorkArea;
            window.Left = primaryMonitorArea.Right - window.Width - 10;
            window.Top = primaryMonitorArea.Bottom - window.Height - 10;

            OpenedNotifications.ForEach((a) =>
            {
                DoubleAnimation liftUp = a.Template.Resources["LiftUp"] as DoubleAnimation;

                liftUp.To = a.Top - window.Height - 5;

                a.BeginAnimation(Window.TopProperty, liftUp);
            });

            OpenedNotifications.Add(window);

            //Timer timer = new Timer();
            //timer.AutoReset = true;

            //timer.Interval = 500;
            //DateTime startTime = DateTime.Now;



            ////Storyboard disappearing = Template.Resources["Disappearing"] as Storyboard;
            //Storyboard disappearing = window.Template.Resources["Disappearing"] as Storyboard;

            ////disappearing.Clone().Completed += (c, d) => { popups.Remove(this); Close(); };
            ////try
            ////{
            ////    disappearing.Completed += (c, d) => { popups.Remove(this); Close(); };
            ////}
            ////catch (Exception ex)
            ////{
            ////    MessageBox.Show(ex.Message);
            ////}

            ////Clock clock = disappearing.CreateClock(true);

            ////clock.

            //timer.Elapsed += (a, b) =>
            //{
            //    //Dispatcher.BeginInvoke(new Action(() =>
            //    //{
            //    //    Timer.Text = (disappearing.GetCurrentState(this)).ToString();
            //    //}));
            //    if ((b.SignalTime - startTime) > TimeSpan.FromSeconds(5))
            //    {
            //        timer.Stop();

            //        //if (disappearing.GetCurrentState == ClockState.)
            //        window.Dispatcher.BeginInvoke(new Action(() =>
            //        {
            //            //disappearing.CreateClock().Completed += (c, d) => { popups.Remove(this); Close(); };
            //            //disappearing.CreateClock()
            //            disappearing.Begin(window, window.Template, true);
            //        }));


            //    }
            //};
            //timer.Start();

            ////Останавливаем оповещение при наведении мыши
            //window.MouseEnter += (a, b) =>
            //{
            //    disappearing.Stop(window);
            //    timer.Stop();
            //};

            ////После выхода запускаем таймер заново
            //window.MouseLeave += (a, b) =>
            //{
            //    startTime = DateTime.Now;
            //    timer.Start();
            //};
        }
    }
}
