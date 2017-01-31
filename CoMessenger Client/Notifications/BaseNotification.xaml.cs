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

        public BaseNotification()
        {
            InitializeComponent();
            var primaryMonitorArea = SystemParameters.WorkArea;
            Left = primaryMonitorArea.Right - Width - 10;
            Top = primaryMonitorArea.Bottom - Height - 10;

            //Поднимаем все уже созданные оповещения чтобы освободить место для нового
            NotificationTemplate.OpenedNotifications.ForEach((a) =>
            {
                DoubleAnimation liftUp = a.Resources["LiftUp"] as DoubleAnimation;

                liftUp.To = a.Top - a.Height - 5;

                a.BeginAnimation(TopProperty, liftUp);
            });

            //Storyboard disappearing = Template.Resources["Disappearing"] as Storyboard;

            //disappearing.CreateClock().Completed += (c, d) => { popups.Remove(this); Close(); };




            //IsVisibleChanged += (a, b) => { MessageBox.Show("New visibility = " + IsVisible.ToString()); };



            NotificationTemplate.OpenedNotifications.Add(this);
        }

        public void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
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
