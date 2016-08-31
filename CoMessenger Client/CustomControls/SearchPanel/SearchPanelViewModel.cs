using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using COMessengerClient.Conversation;
using System.Windows.Data;
using System.Windows.Media;

namespace COMessengerClient.CustomControls.SearchPanel
{
    public class SearchPanelViewModel : INotifyPropertyChanged
    {
        public SearchPanelViewModel(ConversationView conView)
        {
            conversation = conView;
            foundMessages = new ObservableCollection<SearchResultItem>();

            foundMessagesView = CollectionViewSource.GetDefaultView(FoundMessages);

            //((ListCollectionView)FoundMessages).CustomSort = new ClientPeerComparer() as System.Collections.IComparer;

        }

        private ConversationView conversation;

        public ConversationView Conversation
        {
            get { return conversation; }
            set
            {
                if (value != conversation)
                {
                    conversation = value;
                    OnPropertyChanged("Conversation");
                }
            }
        }

        private ICollectionView foundMessagesView;

        public ICollectionView FoundMessagesView
        {
            get { return foundMessagesView; }
        }

        private ObservableCollection<SearchResultItem> foundMessages;

        public ObservableCollection<SearchResultItem> FoundMessages
        {
            get { return foundMessages; }
            //set
            //{
            //    if (value != foundMessages)
            //    {
            //        foundMessages = value;
            //        OnPropertyChanged("FoundMessages");
            //    }
            //}
        }


        private string status;
        public string Status
        {
            get { return status; }
            set
            {
                if (value != status)
                {
                    status = value;
                    OnPropertyChanged("Status");
                }
            }
        }



        #region Индикатор прогресса поиска
        private int totalMaximum = 0;
        public int TotalMaximum
        {
            get { return totalMaximum; }
            set
            {
                if (value != totalMaximum)
                {
                    totalMaximum = value;
                    OnPropertyChanged("TotalMaximum");
                }
            }
        }

        private int totalValue = 0;
        public int TotalValue
        {
            get { return totalValue; }
            set
            {
                if (value != totalValue)
                {
                    totalValue = value;
                    OnPropertyChanged("TotalValue");
                }
            }
        }

        private int currentMaximum = 0;
        public int CurrentMaximum
        {
            get { return currentMaximum; }
            set
            {
                if (value != currentMaximum)
                {
                    currentMaximum = value;
                    OnPropertyChanged("CurrentMaximum");
                }
            }
        }

        private int currentValue = 0;
        public int CurrentValue
        {
            get { return currentValue; }
            set
            {
                if (value != currentValue)
                {
                    currentValue = value;
                    OnPropertyChanged("CurrentValue");
                }
            }
        }
        #endregion

        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        

    }

    public class SearchResultItem
    {
        public MessageForeground Message { get; set; }
        public string Text {get;set;}
        public string Header { get; set; }
        public DateTime UncMessageTime { get; set; }
        public string LocalMessageTime { get { return UncMessageTime.ToLocalTime().ToString(Properties.Settings.Default.UserCultureUIInfo); } }
        public SolidColorBrush ItemColor { get {if (Message == null) return new SolidColorBrush(Colors.Gray); else return new SolidColorBrush(Colors.Black); } }
    }
}
