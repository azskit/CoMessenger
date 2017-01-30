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


            popups.ForEach((a) =>
            {
                DoubleAnimation liftUp = a.Resources["LiftUp"] as DoubleAnimation;

                liftUp.To = a.Top - a.Height - 5;

                a.BeginAnimation(TopProperty, liftUp);
            });

            popups.Add(this);
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            //Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            //if (window != null) window.Close();
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer timer = new Timer();
            timer.AutoReset = true;

            timer.Interval = 500;
            DateTime startTime = DateTime.Now;

            Storyboard disappearing = Resources["Disappearing"] as Storyboard;

            //disappearing.FillBehavior = FillBehavior.Stop;
            //disappearing.Completed += (c, d) => { popups.Remove(this); Close(); };

            timer.Elapsed += (a, b) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Timer.Text = (b.SignalTime - startTime).ToString();
                }));
                if ((b.SignalTime - startTime) > TimeSpan.FromSeconds(5))
                {
                    timer.Stop();


                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        disappearing.Begin(this, true);
                        //BeginStoryboard(disappearing);
                    }));

                    
                }
            };
            timer.Start();

            MouseEnter += (a, b) => 
            {
                
                disappearing.Stop(this);
                timer.Stop();
            };
            MouseLeave += (a, b) => { startTime = DateTime.Now; timer.Start(); };
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Close();
        }
    }

    public static class BaseNotificationHandlers
    {


        
        public static void Disappeared(this BaseNotificationResources res, object sender, EventArgs e)
        {
            MessageBox.Show("Disappeared!");
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null) window.Close();

        }
    }

}
