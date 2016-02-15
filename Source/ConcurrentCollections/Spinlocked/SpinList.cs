using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentCollections.Spinlocked
{
    public class SpinList<T>
        :IList<T>
    {
        private SpinLock listLock = new SpinLock();
        private List<T> list;

        public SpinList(int initialLength)
        {
            list = new List<T>(initialLength);
        }

        public SpinList()
        {
            list = new List<T>();
        }

        #region IList<T> Members
        public int IndexOf(T item)
        {
            try
            {
                listLock.Lock();
                return list.IndexOf(item);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public void Insert(int index, T item)
        {
            try
            {
                listLock.Lock();
                list.Insert(index, item);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public void RemoveAt(int index)
        {
            try
            {
                listLock.Lock();
                list.RemoveAt(index);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    listLock.Lock();
                    return list[index];
                }
                finally
                {
                    listLock.Unlock();
                }
            }
            set
            {
                try
                {
                    listLock.Lock();
                    list[index] = value;
                }
                finally
                {
                    listLock.Unlock();
                }
            }
        }
        #endregion

        #region ICollection<T> Members
        public void Add(T item)
        {
            try
            {
                listLock.Lock();
                list.Add(item);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public void Clear()
        {
            try
            {
                listLock.Lock();
                list.Clear();
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                listLock.Lock();
                return list.Contains(item);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                listLock.Lock();
                list.CopyTo(array, arrayIndex);
            }
            finally
            {
                listLock.Unlock();
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    listLock.Lock();
                    return list.Count;
                }
                finally
                {
                    listLock.Unlock();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            try
            {
                listLock.Lock();
                return list.Remove(item);
            }
            finally
            {
                listLock.Unlock();
            }
        }
        #endregion

        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
