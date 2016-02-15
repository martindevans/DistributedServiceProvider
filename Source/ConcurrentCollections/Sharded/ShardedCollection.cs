using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace ConcurrentCollections.Sharded
{
    /// <summary>
    /// A generic sharded collection
    /// </summary>
    /// <typeparam name="T">the type of item to store in the collection</typeparam>
    /// <typeparam name="C">the type of collection to use</typeparam>
    public class ShardedCollection<T, C>
        :ICollection<T>,
        IShardedEnumerable<T>,
        ICollection
        where C : ICollection<T>, new()
    {
        #region fields
        public int ShardCount
        {
            get
            {
                return shardlookup.Length;
            }
        }
        protected KeyValuePair<SpinLock, C>[] shardlookup;
        #endregion

        #region constructor
        public ShardedCollection(int shardCount)
        {
            if (shardCount <= 0)
                throw new ArgumentException("Must be more than 0 shards");
            shardlookup = new KeyValuePair<SpinLock, C>[shardCount];
            for (int i = 0; i < shardCount; i++)
            {
                shardlookup[i] = new KeyValuePair<SpinLock, C>(new SpinLock(), new C());
            }
        }
        #endregion

        #region test
#if DEBUG
        public static void Test(int size)
        {
            ShardedCollection<int, HashSet<int>> myset = new ShardedCollection<int, HashSet<int>>(5);

            myset.Add(1);
            if (!myset.Contains(1))
                throw new Exception("item should be in the set");

            for (int i = 0; i < size; i++)
            {
                myset.Add(i);
            }
            for (int i = 0; i < size; i++)
            {
                if (!myset.Contains(i))
                    throw new Exception("item should be in the set");
            }

            HashSet<int> c = new HashSet<int>();
            foreach (var item in myset)
            {
                if (!c.Add(item))
                    throw new Exception("Not added to the hashet");
            }
            for (int i = 0; i < size; i++)
            {
                if (!c.Remove(i))
                    throw new Exception("Item not in set!");
            }
            if (c.Count != 0)
                throw new Exception("Set not empty");

            for (int i = 0; i < size; i++)
            {
                if (!myset.Remove(i))
                    throw new Exception("item should be in the set and available for removal");
            }

            for (int i = 0; i < size; i++)
            {
                if (myset.Contains(i))
                    throw new Exception("item should not be in the set");
            }
        }
#endif
        #endregion

        #region transactions
        protected C BeginTransactionGetShard(T value)
        {
            return shardlookup[BeginTransaction(value)].Value;
        }

        /// <summary>
        /// Begins a transaction on this item, and returns a hash which can be used for more efficient ending of the transaction
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public int BeginTransaction(T value)
        {
            int hash = CalculateHash(value);
            KeyValuePair<SpinLock, C> shard = shardlookup[hash];
            shard.Key.Lock();
            return hash;
        }

        /// <summary>
        /// Ends the transaction based on this value
        /// </summary>
        /// <param name="value">The value.</param>
        public void EndTransaction(T value)
        {
            KeyValuePair<SpinLock, C> shard = shardlookup[CalculateHash(value)];
            shard.Key.Unlock();
        }

        /// <summary>
        /// Ends a transaction which was started with BeginTransactionGetHash
        /// </summary>
        /// <param name="hash">The hash.</param>
        public void EndHashTransaction(int hash)
        {
            shardlookup[hash].Key.Unlock();
        }

        /// <summary>
        /// Performs an action inside a transaction on a sharded set
        /// </summary>
        /// <param name="value">The value to operate on</param>
        /// <param name="collection">the collection to operate on</param>
        /// <returns>a value to be returned from the transaction</returns>
        public delegate R TransactionAction<R>(T value);

        /// <summary>
        /// Acquires a lock on the given value, and then operates a transaction on that value
        /// </summary>
        /// <typeparam name="R">the type of the return value</typeparam>
        /// <param name="txn">the method to run in this transaction</param>
        /// <param name="value">the value to operate on</param>
        /// <returns>a value, returned from the transaction</returns>
        public R Transaction<R>(TransactionAction<R> txn, T value)
        {
            int hash = BeginTransaction(value);
            try
            {
                return txn(value);
            }
            finally
            {
                EndHashTransaction(hash);
            }
        }

        protected delegate R ShardAwareTransactionAction<R>(T value, C collection);

        protected R ShardAwareTransaction<R>(ShardAwareTransactionAction<R> txn, T value)
        {
            KeyValuePair<SpinLock, C> shard = shardlookup[CalculateHash(value)];
            try
            {
                shard.Key.Lock();
                return txn(value, shard.Value);
            }
            finally
            {
                shard.Key.Unlock();
            }
        }

        /// <summary>
        /// Performs an action on a shard in a transaction
        /// </summary>
        /// <returns>True, if the loop should terminate, otherwise false</returns>
        public delegate bool LoopTransaction<A>(C collection, A arg);

        /// <summary>
        /// Loops over all shards and perform an action on them each in turn
        /// </summary>
        /// <param name="txn">The transaction to run</param>
        /// <returns>True, if the loop was terminated early, otherwise false</returns>
        public bool Loop<A>(LoopTransaction<A> txn, A arg)
        {
            for (int i = 0; i < shardlookup.Length; i++)
            {
                try
                {
                    shardlookup[i].Key.Lock();
                    if (txn(shardlookup[i].Value, arg))
                        return true;
                }
                finally
                {
                    shardlookup[i].Key.Unlock();
                }
            }
            return false;
        }
        #endregion

        protected virtual int CalculateHash(T value)
        {
            return Math.Abs(value.GetHashCode()) % ShardCount;
        }

        #region ICollection<T> Members
        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public void Add(T item)
        {
            ShardAwareTransaction<bool>(UnlockedAdd, item);
        }

        private bool UnlockedAdd(T value, C set)
        {
            set.Add(value);
            return true;
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            for (int i = 0; i < shardlookup.Length; i++)
            {
                try
                {
                    shardlookup[i].Key.Lock();
                    shardlookup[i].Value.Clear();
                }
                finally
                {
                    shardlookup[i].Key.Unlock();
                }
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
            return ShardAwareTransaction<bool>(UnlockedContains, item);
        }

        private bool UnlockedContains(T item, C set)
        {
            return set.Contains(item);
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
            for (int i = 0; i < shardlookup.Length; i++)
            {
                try
                {
                    shardlookup[i].Key.Lock();
                    shardlookup[i].Value.CopyTo(array, arrayIndex);
                    arrayIndex += shardlookup[i].Value.Count;
                }
                finally
                {
                    shardlookup[i].Key.Unlock();
                }
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
                int count = 0;
                for (int i = 0; i < shardlookup.Length; i++)
                {
                    try
                    {
                        shardlookup[i].Key.Lock();
                        count += shardlookup[i].Value.Count;
                    }
                    finally
                    {
                        shardlookup[i].Key.Unlock();
                    }
                }
                return count;
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
            return ShardAwareTransaction<bool>(UnlockedRemove, item);
        }

        private bool UnlockedRemove(T item, C set)
        {
            return set.Remove(item);
        }
        #endregion

        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            return new ShardedEnumerator<T, ShardedCollection<T, C>>(this);
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<T>).GetEnumerator();
        }
        #endregion

        #region IShardedEnumerable<T> Members
        int IShardedEnumerable<T>.ShardCount
        {
            get { return ShardCount; }
        }

        void IShardedEnumerable<T>.LockShard(int i)
        {
            shardlookup[i].Key.Lock();
        }

        void IShardedEnumerable<T>.UnlockShard(int i)
        {
            shardlookup[i].Key.Unlock();
        }

        IEnumerator<T> IShardedEnumerable<T>.GetShardEnumerator(int i)
        {
            return shardlookup[i].Value.GetEnumerator();
        }
        #endregion

        #region IEnumerable<T> Members
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return (this as ShardedCollection<T, C>).GetEnumerator();
        }
        #endregion

        #region ICollection Members
        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.-or- <paramref name="index"/> is equal to or greater than the length of <paramref name="array"/>.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>. </exception>
        /// <exception cref="T:System.ArgumentException">The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>. </exception>
        public void CopyTo(Array array, int index)
        {
            this.CopyTo(array as T[], index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.</returns>
        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        public override string ToString()
        {
            return "Count = " + Count;
        }
    }
}
