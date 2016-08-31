using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System;

namespace COMessengerClient.CustomControls
{
    public class VersionViewModel : DependencyObject
    {


        public bool IsCurrent
        {
            get { return (bool)GetValue(IsCurrentProperty); }
            set { SetValue(IsCurrentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCurrent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCurrentProperty =
            DependencyProperty.Register("IsCurrent", typeof(bool), typeof(VersionViewModel), new UIPropertyMetadata(false));



        public int Version { get; set; }
        public DateTime ChangeTime { get; set; }
    }

    class MessageHeaderViewModel : INotifyPropertyChanged
    {
        public ICollectionView Versions { get; private set; }



        public bool HasMultipleVersion
        {
            get { return versionsCollection.Count >= 2 ? true : false; }
        }

        private ObservableCollection<VersionViewModel> versionsCollection;


        public MessageHeaderViewModel()
        {
            versionsCollection = new ObservableCollection<VersionViewModel>();

            versionsCollection.Add(new VersionViewModel() { IsCurrent = true });
            versionsCollection.Add(new VersionViewModel() { IsCurrent = false });
            versionsCollection.Add(new VersionViewModel() { IsCurrent = false });

            Versions = new System.Windows.Data.CollectionViewSource { Source = versionsCollection }.View;
        }

        public MessageHeaderViewModel(ObservableCollection<VersionViewModel> source)
        {
            versionsCollection = source;

            Versions = new System.Windows.Data.CollectionViewSource { Source = versionsCollection }.View;

            versionsCollection.CollectionChanged += (a, b) => { OnPropertyChanged("HasMultipleVersion"); };
        }

        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }

}
