using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace COMessengerClient
{
    [Serializable]
    public class DuplicateKeyException : Exception
    {
        public DuplicateKeyException() { }
        public DuplicateKeyException(string message) : base(message) { }
        public DuplicateKeyException(string message, Exception inner) : base(message, inner) { }
        protected DuplicateKeyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    public class IndexedObservableCollection<TKey, TValue> : ObservableCollection<TValue>
    {
        private SortedList<TKey, int> index = new SortedList<TKey,int>();

        public TValue this[TKey key] 
        {
            get
            {
                int idx;

                if (index.TryGetValue(key, out idx))
                    return this[idx];
                else
                    throw new KeyNotFoundException();
            }

            set
            {
                int idx;

                if (index.TryGetValue(key, out idx))
                {
                    this[idx] = value;
                    //base.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Replace));
                }
                else
                    throw new KeyNotFoundException();
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (index.ContainsKey(key))
                throw new DuplicateKeyException();
            else
            {
                index.Add(key, base.Count);
                base.Add(value);
            }
        }

        public int IndexOf(TKey key)
        {
            return index[key];
        }

        internal bool ContainsKey(TKey key)
        {
            return index.ContainsKey(key);
        }

        internal void RaiseCollectionChanged(TValue oldobject, TValue newobject, int idx)
        {
            base.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Replace, oldobject, newobject, idx));
        }

        internal void RaiseCollectionChanged()
        {
            base.OnCollectionChanged( new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }
}
