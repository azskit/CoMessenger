using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CorporateMessengerLibrary.Collections
{
    /// <summary>
    /// Обертка для SortedList, позволяющая не выбрасывать исключение в случае если указанного элемента нет в коллекции, а возвращать TKey
    /// </summary>
    /// <typeparam name="T">Тип ключа и значения</typeparam>
    public class BackedSortedList<T>
    {
        private SortedList<T, T> back = new SortedList<T, T>();

        public T this[T key]
        {
            get
            {
                T value;

                if (back.TryGetValue(key, out value))
                    return value;
                else
                    return key;
            }
            //set
            //{
            //    back[key] = value;
            //}
        }

        public void Add(T key, T value)
        {
            back.Add(key, value);
        }

        public int Count { get { return back.Count; } }

        public void Clear()
        {
            back.Clear();
        }
    }
}
