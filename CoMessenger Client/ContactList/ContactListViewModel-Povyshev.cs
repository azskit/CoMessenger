using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
//using System.Windows.Data;
//using System.Collections.ObjectModel;
using CorporateMessengerLibrary;

namespace COMessengerClient.ContactList
{
	

    class ContactListViewModel
    {
        private IList<ClientPeer> participants;

        public IList<ClientPeer> Participants
        {
            get { return participants; }
        }

        public ContactListViewModel()
        {
            //participants = CollectionViewSource.GetDefaultView(App.ThisApp.ListOfConversations.Values.ToList());

            participants = App.ThisApp.ListOfConversations.Values;

            //((ListCollectionView)participants).CustomSort = new ClientPeerComparer() as System.Collections.IComparer;

        }


    }
}
