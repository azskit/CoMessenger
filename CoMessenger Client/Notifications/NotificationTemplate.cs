using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

namespace COMessengerClient.Notifications
{
    public partial class NotificationTemplate : ResourceDictionary
    {
        public NotificationTemplate()
        {
            InitializeComponent();

            //Storyboard disappearing = Resources["Disappearing"] as Storyboard;
        }

        public void CloseButtonClick(object sender, EventArgs e)
        {
            MessageBox.Show("Disappeared!");
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null) window.Close();

        }

        public void DesappearingCompleted(object sender, EventArgs e)
        {
            MessageBox.Show("Disappeared!");
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window != null) window.Close();
        }

    }
}
