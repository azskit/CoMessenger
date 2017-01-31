using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace COMessengerClient.Notifications
{
    /// <summary>
    /// Interaction logic for BaseNotification.xaml
    /// </summary>
    public partial class BaseNotification : Window
    {
        public static List<BaseNotification> popups = new List<BaseNotification>();

        public BaseNotification()
        {
            InitializeComponent();
            var primaryMonitorArea = SystemParameters.WorkArea;
            Left = primaryMonitorArea.Right - Width - 10;
            Top = primaryMonitorArea.Bottom - Height - 10;

            //Поднимаем все уже созданные оповещения чтобы освободить место для нового
            popups.ForEach((a) =>
            {
                DoubleAnimation liftUp = a.Resources["LiftUp"] as DoubleAnimation;

                liftUp.To = a.Top - a.Height - 5;

                a.BeginAnimation(TopProperty, liftUp);
            });

            //Storyboard disappearing = Template.Resources["Disappearing"] as Storyboard;

            //disappearing.CreateClock().Completed += (c, d) => { popups.Remove(this); Close(); };


            

            //IsVisibleChanged += (a, b) => { MessageBox.Show("New visibility = " + IsVisible.ToString()); };



            popups.Add(this);
        }

        public void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer timer = new Timer();
            timer.AutoReset = true;

            timer.Interval = 500;
            DateTime startTime = DateTime.Now;

            Storyboard disappearing = Template.Resources["Disappearing"] as Storyboard;

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
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //disappearing.CreateClock().Completed += (c, d) => { popups.Remove(this); Close(); };
                        //disappearing.CreateClock()
                        disappearing.Begin(this, Template, true);
                    }));

                    
                }
            };
            timer.Start();

            //Останавливаем оповещение при наведении мыши
            MouseEnter += (a, b) => 
            {
                disappearing.Stop(this);
                timer.Stop();
            };

            //После выхода запускаем таймер заново
            MouseLeave += (a, b) => 
            {
                startTime = DateTime.Now;
                timer.Start();
            };
        }
        //private void Storyboard_Completed(object sender, EventArgs e)
        //{
        //    Close();
        //}
    }

    //public static class BaseNotificationHandlers
    //{


        
    //    public static void Disappeared(this BaseNotificationResources res, object sender, EventArgs e)
    //    {
    //        MessageBox.Show("Disappeared!");
    //        Window window = ((FrameworkElement)sender).TemplatedParent as Window;
    //        if (window != null) window.Close();

    //    }
    //}

}
