using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Sharded
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShardedList<T>
        :IList<T>
    {
        #region fields
        public int ShardSize
        {
            get;
            private set;
        }

        public int ShardCount
        {
            get
            {
                return shards.Count;
            }
        }

        private int topShard = 0; //the shard which holds the end of the list

        private ReaderWriterLockSlim creationLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<Shard> shards;
        #endregion

        #region constructor
        public ShardedList(int shardSize)
            :this(shardSize, 1) { }

        public ShardedList(int shardSize, int initialShardCount)
        {
            this.ShardSize = shardSize;

            shards = new List<ShardedList<T>.Shard>(initialShardCount);
            for (int i = 0; i < initialShardCount; i++)
            {
                CreateShard();
            }
        }
        #endregion

        #region sharding
        private void CreateShard()
        {
            Shard s = new ShardedList<T>.Shard(shards.Count * ShardSize) { data = new T[ShardSize], shardLock = new SpinLock() };
            shards.Add(s);
        }

        private class Shard
        {
            public SpinLock shardLock;
            public T[] data;
            public int NextItem
            {
                get;
                private set;
            }

            public int FirstItem;

            public Shard(int first)
            {
                FirstItem = first;
            }

            public void Add(T item)
            {
                if (IsFull())
                    throw new IndexOutOfRangeException();
                data[NextItem] = item;
                NextItem++;
            }

            public bool IsFull()
            {
                return NextItem >= data.Length;
            }

            public bool IsEmpty()
            {
                return NextItem == 0;
            }

            public void Clear()
            {
                NextItem = 0;
            }

            public int IndexOf(T item)
            {
                for (int i = 0; i < NextItem; i++)
                {
                    if (data[i].Equals(item))
                        return i;
                }
                return -1;
            }

            public void RemoveAt(int index)
            {
                if (index < NextItem)
                {
                    for (int i = index; i < NextItem && i < data.Length - 1; i++)
                    {
                        data[i] = data[i + 1];
                    }
                    NextItem--;
                }
                else
                    throw new IndexOutOfRangeException("ShardedList.Shard.RemoveAt");
            }
        }

        private Shard GetShardForIndex(int index)
        {
            int id = GetShardIdForIndex(index);
            if (id >= ShardCount)
                return null;
            else
                return shards[id];
        }

        private int GetShardIdForIndex(int index)
        {
            return index / ShardSize;
        }

        /// <summary>
        /// Remove all currently unused shards to cut memory usage to a minimum
        /// </summary>
        public void TrimExcess()
        {
            try
            {
                creationLock.EnterWriteLock();
                int upper = topShard+1;
                int count = shards.Count - upper;
                if (count > 0)
                    shards.RemoveRange(upper, count);
                shards.TrimExcess();
            }
            finally
            {
                creationLock.ExitWriteLock();
            }
        }
        #endregion

        #region IList<T> Members
        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            try
            {
                creationLock.EnterReadLock();
                for (int i = 0; i < shards.Count; i++)
                {
                    try
                    {
                        shards[i].shardLock.Lock();
                        int innerIndex = shards[i].IndexOf(item);
                        if (innerIndex != -1)
                            return i * ShardSize + innerIndex;
                    }
                    finally
                    {
                        shards[i].shardLock.Unlock();
                    }
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
        public void Insert(int index, T item)
        {
            try
            {
                creationLock.EnterUpgradeableReadLock();
                Shard s = GetShardForIndex(index);
                try
                {
                    s.shardLock.Lock();
                    InsertInitialLockHeld(index, item);
                }
                finally
                {
                    s.shardLock.Unlock();
                }
            }
            finally
            {
                creationLock.ExitUpgradeableReadLock();
            }
        }

        private void InsertInitialLockHeld(int index, T item)
        {
            int id = GetShardIdForIndex(index);
            int nextId = id + 1;
            Shard shard = shards[id];
            Shard nextShard = (nextId >= shards.Count ? null : shards[nextId]);

            index -= shard.FirstItem;

            bool extraInUse = false;
            T extra = default(T);
            if (shard.NextItem == shard.data.Length)
            {
                extraInUse = true;
                extra = shard.data[shard.data.Length - 1];
            }
            for (int i = shard.NextItem - 1; i > index; i--)
            {
                shard.data[i] = shard.data[i - 1];
            }
            shard.data[index] = item;
            if (extraInUse)
            {
                if (nextShard == null)
                {
                    try
                    {
                        creationLock.EnterWriteLock();
                        CreateShard();
                        nextShard = shards[nextId];
                    }
                    finally
                    {
                        creationLock.ExitWriteLock();
                    }
                }
                try
                {
                    nextShard.shardLock.Lock();
                    InsertInitialLockHeld(nextShard.FirstItem, extra);
                }
                finally
                {
                    nextShard.shardLock.Unlock();
                }
            }
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
        public void RemoveAt(int index)
        {
            try
            {
                creationLock.EnterReadLock();

                int firstId = GetShardIdForIndex(index);
                int upperId = 0;

                try
                {
                    for (int i = firstId; i <= topShard; i++)
                    {
                        upperId = i;

                        Shard s = shards[i];
                        s.shardLock.Lock();
                        s.RemoveAt(index - s.FirstItem);

                        if (i + 1 <= topShard)
                        {
                            shards[i + 1].shardLock.Lock();
                            s.Add(shards[i + 1].data[0]);
                            index = shards[i + 1].FirstItem;
                            s.shardLock.Unlock();
                        }
                        else if (i == topShard && i != 0 && shards[topShard].IsEmpty())
                            topShard--;
                    }
                }
                finally
                {
                    for (int i = firstId + 1; i < upperId; i++)
                    {
                        if (shards[i].shardLock.IsOwned)
                            shards[i].shardLock.Unlock();
                    }
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        /// <value></value>
        public T this[int index]
        {
            get
            {
                try
                {
                    creationLock.EnterReadLock();
                    return GetShardForIndex(index).data[index % ShardSize];
                }
                finally
                {
                    creationLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    creationLock.EnterReadLock();
                    shards[index / ShardSize].data[index % ShardSize] = value;
                }
                finally
                {
                    creationLock.ExitReadLock();
                }
            }
        }
        #endregion

        #region ICollection<T> Members
        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public void Add(T item)
        {
            try
            {
                creationLock.EnterUpgradeableReadLock();
                Shard s = shards[topShard];
                try
                {
                    s.shardLock.Lock();
                    if (s.IsFull())
                    {
                        try
                        {
                            creationLock.EnterWriteLock();
                            CreateShard();
                            topShard++;
                        }
                        finally
                        {
                            creationLock.ExitWriteLock();
                        }
                        s.shardLock.Unlock();

                        s = shards[topShard];

                        s.shardLock.Lock();
                        if (s.IsFull())
                            throw new Exception("New shard is full as soon as it is created!");
                        s.Add(item);
                    }
                    else
                    {
                        s.Add(item);
                    }
                }
                finally
                {
                    s.shardLock.Unlock();
                }
            }
            finally
            {
                creationLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            try
            {
                creationLock.EnterWriteLock();
                topShard = 0;
                shards[0].Clear();
            }
            finally
            {
                creationLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return (IndexOf(item) != -1);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.-or-<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array.Rank != 1)
                throw new ArgumentException("Array must be single dimensional");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("destination array is not big enough");
            try
            {
                creationLock.EnterReadLock();
                for (int i = 0; i < ShardCount; i++)
                {
                    try
                    {
                        shards[i].shardLock.Lock();
                        for (int j = 0; j < shards[i].data.Length; j++)
                        {
                            array[arrayIndex++] = shards[i].data[j];
                        }
                    }
                    finally
                    {
                        shards[i].shardLock.Unlock();
                    }
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
        public int Count
        {
            get
            {
                if (topShard < 0)
                    return 0;
                return topShard * ShardSize + shards[topShard].NextItem;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool Remove(T item)
        {
            try
            {
                creationLock.EnterReadLock();
                for (int i = 0; i < shards.Count; i++)
                {
                    try
                    {
                        shards[i].shardLock.Lock();
                        int index = shards[i].IndexOf(item);
                        if (index != -1)
                        {
                            RemoveAt(index + i * ShardSize);
                            return true;
                        }
                    }
                    finally
                    {
                        shards[i].shardLock.Unlock();
                    }
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
            return false;
        }
        #endregion

        #region IEnumerable<T> Members
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new ShardListEnumerator(this);
        }

        private class ShardListEnumerator
            : IEnumerator<T>
        {
            ShardedList<T> myList;

            private bool lockHeld = false;
            int shard = 0;
            IEnumerator<T> currentEnumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShardedList&lt;T&gt;.ShardListEnumerator"/> class.
            /// </summary>
            /// <param name="list">The list.</param>
            public ShardListEnumerator(ShardedList<T> list)
            {
                myList = list;
            }

            #region IEnumerator<T> Members
            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <value></value>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            public T Current
            {
                get
                {
                    if (!lockHeld)
                        AcquireEnumeratorLock();
                    return currentEnumerator.Current;
                }
            }
            #endregion

            #region IDisposable Members
            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (lockHeld)
                    ReleaseLock();
                if (currentEnumerator != null)
                    currentEnumerator.Dispose();
                currentEnumerator = null;
            }
            #endregion

            #region IEnumerator Members
            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <value></value>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext()
            {
                if (!lockHeld)
                    AcquireEnumeratorLock();
                if (currentEnumerator.MoveNext())
                    return true;
                else
                {
                    ReleaseLock();
                    shard++;
                    if (shard < myList.ShardCount)
                    {
                        AcquireEnumeratorLock();
                        return MoveNext();
                    }
                    else
                    {
                        currentEnumerator.Dispose();
                        currentEnumerator = null;
                        return false;
                    }
                }
            }

            private void ReleaseLock()
            {
                myList.shards[shard].shardLock.Unlock();
                lockHeld = false;
            }

            private void AcquireEnumeratorLock()
            {
                myList.shards[shard].shardLock.Lock();
                lockHeld = true;
                currentEnumerator = (myList.shards[shard].data as IEnumerable<T>).GetEnumerator();
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
            {
                if (lockHeld)
                    ReleaseLock();
                if (currentEnumerator != null)
                    currentEnumerator.Dispose();
                shard = 0;
            }

            #endregion
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<T>).GetEnumerator();
        }
        #endregion

        public delegate bool LoopTransaction<I>(int index, T item, ShardedList<T> collection, I arg);

        public void Loop<I>(LoopTransaction<I> txn, int start, int end, I arg)
        {
            if (start > Count)
                return;
            if (end != -1 && end < start)
                throw new ArgumentException("End must come after start");
            try
            {
                creationLock.EnterReadLock();
                for (int i = 0; i < shards.Count; i++)
                {
                    if (shards[i].FirstItem < start)
                        continue;
                    try
                    {
                        shards[i].shardLock.Lock();
                        for (int index = 0; index < shards[i].NextItem; index++)
                        {
                            int globalIndex = shards[i].FirstItem + index;
                            if (end != -1 && end >= globalIndex)
                                return;
                            if (txn.Invoke(globalIndex, shards[i].data[index], this, arg))
                                return;
                        }
                    }
                    finally
                    {
                        shards[i].shardLock.Unlock();
                    }
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
        }

        /// <summary>
        /// An action to perform inside a transaction
        /// </summary>
        public delegate O TransactionAction<I, O>(int key, ShardedList<T> collection, params I[] args);

        /// <summary>
        /// Obtains a lock on a specified key, and then performs a transaction delegate.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="txn">The transaction to perform.</param>
        /// <returns></returns>
        public O Transaction<I, O>(TransactionAction<I, O> txn, int index, params I[] args)
        {
            try
            {
                creationLock.EnterReadLock();
                Shard s = GetShardForIndex(index);
                try
                {
                    s.shardLock.Lock();
                    return txn.Invoke(index, this, args);
                }
                finally
                {
                    s.shardLock.Unlock();
                }
            }
            finally
            {
                creationLock.ExitReadLock();
            }
        }

        #region Test
#if DEBUG
        public static void Test(int size)
        {
            Console.WriteLine("Creating a concurrent list");
            ShardedList<int> myList = new ShardedList<int>(2, 1);
            if (myList.creationLock.IsWriteLockHeld || myList.creationLock.IsReadLockHeld)
                throw new Exception("Hanging lock");

            Console.WriteLine("Adding some items to the list");
            myList.Add(1);
            myList.Add(2);
            myList.Add(3);
            myList.Add(4);
            myList.Add(5);
            myList.Add(6);
            myList.Add(7);
            myList.Add(8);
            myList.Add(9);
            myList.Add(11);

            if (myList.Contains(0))
                throw new Exception("Incorrect containment result");

            Console.WriteLine("Checking the count is correct");
            if (myList.Count != 10)
                throw new Exception("Incorrect count!");

            Console.WriteLine("Iterating through the entire list to check values and enumeration");
            int i = 1;
            foreach (var item in myList)
            {
                if (item != i++)
                    throw new Exception("Incorrect value");
                if (i == 10) i++;
            }

            Console.WriteLine("Removing an item");
            myList.Remove(11);
            if (myList.Count != 9)
                throw new Exception("Incorrect count!");

            Console.WriteLine("Adding a new item, back into the space now vacated by the removed item");
            myList.Add(10);
            if (myList.Count != 10)
                throw new Exception("Incorrect count!");

            if (myList.ShardCount != 5)
                throw new Exception("Created a shard when there was an empty slot after removal!");

            i = 1;
            foreach (var item in myList)
            {
                if (item != i++)
                    throw new Exception("Incorrect value");
            }

            Console.WriteLine("Checking searching for the index of a value");
            if (myList.IndexOf(4) != 3)
                throw new Exception("Incorrect index searching");

            Console.WriteLine("Testing containment tests");
            if (myList.Contains(11))
                throw new Exception("Incorrect containment test");
            if (!myList.Contains(10))
                throw new Exception("Incorrect containment test");

            Console.WriteLine("Testing copying to an external array");
            int[] array = new int[10];
            myList.CopyTo(array, 0);
            for (i = 0; i < array.Length; i++)
            {
                if (array[i] != i + 1)
                    throw new Exception("Incorrect copying");
            }

            Console.WriteLine("Inserting an item");
            myList.Insert(0, 0);

            Console.WriteLine("Iterating through list to check iteration and insertion");
            for (i = 0; i < myList.Count; i++)
            {
                if (myList[i] != i)
                    throw new Exception("Incorrect value");
            }

            if (myList.Remove(11))
                throw new Exception("Removed an item not in the collection");
            if (!myList.Remove(9))
                throw new Exception("Did not remove an item which should have been removable");
            if (!myList.Remove(8))
                throw new Exception("Did not remove an item which should have been removable");
            if (!myList.Remove(7))
                throw new Exception("Did not remove an item which should have been removable");
            if (!myList.Remove(6))
                throw new Exception("Did not remove an item which should have been removable");
            if (!myList.Remove(5))
                throw new Exception("Did not remove an item which should have been removable");
            if (!myList.Remove(4))
                throw new Exception("Did not remove an item which should have been removable");

            myList.TrimExcess();

            if (myList.ShardCount != 2)
                throw new Exception("Incorrect shard count after trimming");

            if (myList.Count != 4)
                throw new Exception("Incorrect count");

            for (i = 4; i < 6; i++)
                myList.Add(i);
            for (i = 0; i < myList.Count; i++)
            {
                if (myList[i] != i)
                    throw new Exception("Incorrect value");
            }

            myList.RemoveAt(0);
            if (myList.Count != 5)
                throw new Exception("Incorrect count");

            for (i = 0; i < myList.Count; i++)
            {
                if (myList[i] != i + 1)
                    throw new Exception("Incorrect value");
            }

            for (i = 6; i < 8; i++)
                myList.Add(i);
            for (i = 0; i < myList.Count; i++)
            {
                if (myList[i] != i + 1)
                    throw new Exception("Incorrect value");
            }
        }
#endif
        #endregion
    }
}
