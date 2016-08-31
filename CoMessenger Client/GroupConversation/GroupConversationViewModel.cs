using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using CorporateMessengerLibrary;
using System.Windows.Data;
using System.ComponentModel;

namespace COMessengerClient.GroupConversation
{
    class GroupConversationViewModel
    {
        private ICollectionView participants;

        public ICollectionView Participants
        {
            get { return participants; }
        }

        public GroupConversationViewModel(RoomPeer group)
        {
            participants = new CollectionViewSource { Source = App.ThisApp.ListOfConversations }.View;

            Participants.Filter = (user) => group.Participants.Contains(((ClientPeer)user).Peer); //  ((ClientPeer)user).Peer. == UserStatus.Online;
        }
    }
}
