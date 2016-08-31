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

namespace COMessengerClient.CustomControls
{

    public class VersionSelectedEventArgs: EventArgs
    {
        public VersionViewModel Version;

    }

    public partial class EditMessagePanel : UserControl
    {
        public EditMessagePanel()
        {
            InitializeComponent();
        }

        public event EventHandler<VersionSelectedEventArgs> VersionSelected;

        private void OnVersionSelected(VersionViewModel clickedClientPeer)
        {
            if (VersionSelected != null)
                VersionSelected(this, new VersionSelectedEventArgs() { Version = clickedClientPeer });
        }
        private void OnTogglerClicked(object sender, MouseButtonEventArgs e)
        {
            VersionViewModel version = ((Border)sender).DataContext as VersionViewModel;

            if (!version.IsCurrent)
            {
                
                OnVersionSelected(version);
            }

        }
    }
}
