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
using System.ComponentModel;
using COMessengerClient.Conversation;
using CorporateMessengerLibrary.Messaging;

namespace COMessengerClient.GroupConversation
{
    /// <summary>
    /// Логика взаимодействия для GroupConversationView.xaml
    /// </summary>
    public partial class GroupConversationView : UserControl
    {
        public GroupConversationView()
        {
            InitializeComponent();
        }

        public GroupConversationView(RoomPeer group):this()
        {
            if (group == null)
                throw new ArgumentNullException("group");

            GroupConversationViewModel viewmodel = new GroupConversationViewModel(group);

            //MainGrid.Children.Add(new ConversationView(App.ThisApp.MainWindow, group.PeerID));
            //Conversation.PeerId = group.PeerID;
            this.DataContext = viewmodel;
        }
    }
}
