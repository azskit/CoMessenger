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
using CorporateMessengerLibrary;
using System.Windows.Media.Animation;

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для UserListBox.xaml
    /// </summary>
    public partial class UserListBox : UserControl
    {
        public UserListBox()
        {
            InitializeComponent();
        }

        private void listViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;

            OnClientPeerDoubleClicked(item.Content as ClientPeer);
        }

        //При обновлении статуса пира ищем панельку, его содержащую
        private void StatusUpdated(object sender, DataTransferEventArgs e)
        {
            Grid elem = TryFindParent<Grid>(((DependencyObject)sender)) as Grid;

            OnStatusUpdated(elem);
        }

        //Панелька только загрузилась - 
        // возможно это потому что контакт только что вошел в сеть, нужно проверить и при необходимости подсветить
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid elem = sender as Grid;

            OnStatusUpdated(elem);
        }

        private static void OnStatusUpdated(Grid elem)
        {
            if (elem.DataContext != null)
            {
                ClientPeer peer = elem.DataContext as ClientPeer;

                if (peer.StatusChanged)
                {
                    AnimateStatus(elem, ((PersonPeer)peer.Peer).State);
                    peer.StatusChanged = false;
                }
            }
        }

        private static void AnimateStatus(Panel panel, PeerStatus newStatus)
        {
            if (newStatus == PeerStatus.Online)
            {
                panel.BeginStoryboard(App.ThisApp.Resources["OnlineHighlight"] as Storyboard);
            }
            else if (newStatus == PeerStatus.Offline)
            {
                panel.BeginStoryboard(App.ThisApp.Resources["OfflineHighlight"] as Storyboard);
            }
        }

        public event EventHandler<ClientPeerDoubleClickedEventArgs> ClientPeerDoubleClicked;

        protected void OnClientPeerDoubleClicked(ClientPeer clickedClientPeer)
        {
            if (ClientPeerDoubleClicked != null)
                ClientPeerDoubleClicked(this, new ClientPeerDoubleClickedEventArgs(clickedClientPeer));
        }

        //private void SolidColorBrush_Changed(object sender, EventArgs e)
        //{
        //    StackPanel elem = TryFindParent<StackPanel>(((DependencyObject)sender)) as StackPanel;

        //    if (elem.DataContext != null)
        //    {
        //        ClientPeer RoomID = elem.DataContext as ClientPeer;

        //        if (RoomID.StatusChanged)
        //            AnimateStatus(elem, ((PersonPeer)RoomID.Peer).State);
        //    }
        //}

        /// <summary>
        /// Finds a parent of a given control/item on the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of Parent</typeparam>
        /// <param name="child">Child whose parent is queried</param>
        /// <returns>Returns the first parent item that matched the type (T), if no match found then it will return null</returns>
        public static T TryFindParent<T>(DependencyObject child)
        where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return TryFindParent<T>(parentObject);
        }

    }

    public class ClientPeerDoubleClickedEventArgs : EventArgs
    {
        public ClientPeer ClickedClientPeer { get; set; }
        public ClientPeerDoubleClickedEventArgs(ClientPeer clickedClientPeer)
        {
            ClickedClientPeer = clickedClientPeer;
        }
    }




}
