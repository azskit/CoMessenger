using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
//using System.Windows.Data;
//using System.Collections.ObjectModel;
using CorporateMessengerLibrary;
using System.Collections.ObjectModel;

namespace COMessengerClient.ContactList
{


    class ContactListViewModel : INotifyPropertyChanged
    {
        private ICollectionView participants;

        public ICollectionView Participants
        {
            get { return participants; }
        }

        private string newRoomName;

        public string NewRoomName
        {
            get { return newRoomName; }
            set
            {

                if (value != newRoomName)
                {
                    newRoomName = value;
                    OnPropertyChanged("NewRoomName");
                }
            }
        }
        


        public ContactListViewModel()
        {
            if (App.ThisApp != null)
            {
                participants = new System.Windows.Data.CollectionViewSource { Source = App.ThisApp.ListOfConversations }.View;

                ((ListCollectionView)Participants).CustomSort = new ClientPeerComparer() as System.Collections.IComparer;
            }
        }

        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public event PropertyChangedEventHandler PropertyChanged;


    }
}
