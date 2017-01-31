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

            //Storyboard disappearing = Resources["Disappearing"] as Storyboard;
        }

        public void CloseButtonClick(object sender, EventArgs e)
        {
            //MessageBox.Show("Disappeared!");
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null && OpenedNotifications.Contains(window))
            {
                OpenedNotifications.Remove(window);
                window.Close();
            }
        }

        public void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null && OpenedNotifications.Contains(window))
            {
                OpenedNotifications.Remove(window);
                window.Close();
            }
            //MessageBox.Show("Window closed!");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;

            Timer timer = new Timer();
            timer.AutoReset = true;

            timer.Interval = 500;
            DateTime startTime = DateTime.Now;

            

            //Storyboard disappearing = Template.Resources["Disappearing"] as Storyboard;
            Storyboard disappearing = window.Template.Resources["Disappearing"] as Storyboard;

            //disappearing.Clone().Completed += (c, d) => { popups.Remove(this); Close(); };
            //try
            //{
            //    disappearing.Completed += (c, d) => { popups.Remove(this); Close(); };
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}

            //Clock clock = disappearing.CreateClock(true);

            //clock.

            timer.Elapsed += (a, b) =>
            {
                //Dispatcher.BeginInvoke(new Action(() =>
                //{
                //    Timer.Text = (disappearing.GetCurrentState(this)).ToString();
                //}));
                if ((b.SignalTime - startTime) > TimeSpan.FromSeconds(5))
                {
                    timer.Stop();

                    //if (disappearing.GetCurrentState == ClockState.)
                    window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //disappearing.CreateClock().Completed += (c, d) => { popups.Remove(this); Close(); };
                        //disappearing.CreateClock()
                        disappearing.Begin(window, window.Template, true);
                    }));


                }
            };
            timer.Start();

            //Останавливаем оповещение при наведении мыши
            window.MouseEnter += (a, b) =>
            {
                disappearing.Stop(window);
                timer.Stop();
            };

            //После выхода запускаем таймер заново
            window.MouseLeave += (a, b) =>
            {
                startTime = DateTime.Now;
                timer.Start();
            };
        }
    }
}
