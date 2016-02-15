using System;
using System.Collections.Generic;
using System.Text;

namespace ConcurrentCollections.Spinlocked
{
    /// <summary>
    /// A linked list, all methods are threadsafe due to using a spinlock
    /// </summary>
    /// <typeparam name="T">the type of this linked list</typeparam>
    public class SpinLinkedList<T>
    {
        private LinkedList<T> myList;
        private SpinLock myLock = new SpinLock();

        public SpinLinkedList()
        {
            myList = new LinkedList<T>();
        }

        public SpinLinkedList(IEnumerable<T> collection)
        {
            myList = new LinkedList<T>(collection);
        }

        public void Clear()
        {
            try
            {
                myLock.Lock();
            }
            finally
            {
                myLock.Unlock();
            }
        }

        public void AddFirst(T item)
        {
            TryAddFirst(item, 0);
        }

        public bool TryAddFirst(T item, uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                myList.AddFirst(item);
                myLock.Unlock();
                return true;
            }
            else
                return false;
        }

        public void AddLast(T item)
        {
            TryAddLast(item, 0);
        }

        public bool TryAddLast(T item, uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                myList.AddLast(item);
                myLock.Unlock();
                return true;
            }
            else
                return false;
        }

        public bool Remove(T item)
        {
            return TryRemove(item, 0);
        }

        public bool TryRemove(T item, uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                bool ret = myList.Remove(item);
                myLock.Unlock();
                return ret;
            }
            return false;
        }

        public bool Contains(T item)
        {
            return TryContains(item, 0);
        }

        public bool TryContains(T item, uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                bool ret = myList.Contains(item);
                myLock.Unlock();
                return ret;
            }
            return false;
        }

        public void CopyTo(T[] array, int index)
        {
            TryCopyTo(array, index, 0);
        }

        public bool TryCopyTo(T[] array, int index, uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                myList.CopyTo(array, index);
                myLock.Unlock();
                return true;
            }
            else
                return false;
        }

        public void RemoveFirst()
        {
            TryRemoveFirst(0);
        }

        public bool TryRemoveFirst(uint timeout)
        {
            if (myLock.TryLock(TimeSpan.FromMilliseconds(timeout)))
            {
                myList.RemoveFirst();
                myLock.Unlock();
                return true;
            }
            else
                return false;
        }
    }
}
