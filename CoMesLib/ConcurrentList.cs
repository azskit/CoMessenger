using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CorporateMessengerLibrary
{
    public class ConcurrentList<T> : IEnumerable<T>
    {
        private List<T> internalList;

        public ConcurrentList()
        {
            internalList = new List<T>();
        }

        public void Add(T item)
        {
            lock(internalList)
                internalList.Add(item);
        }

        public void RemoveAll(Predicate<T> predicate)
        {
            lock (internalList)
                internalList.RemoveAll(predicate);
        }


        public IEnumerator<T> GetEnumerator()
        {
            List<T> copyOfInternalList = null;

            lock (internalList)
                copyOfInternalList = new List<T>(internalList);

            return copyOfInternalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
