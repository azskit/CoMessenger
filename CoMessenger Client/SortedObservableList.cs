using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;

namespace COMessengerClient
{
    public class SortedObservableList<TKey, TValue> : SortedList<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public TValue this[TKey key] 
        {
            get { return base[key]; }
            set { base[key] = value; } 
        }
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

    }
}
