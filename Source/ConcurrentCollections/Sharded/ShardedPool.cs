using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Sharded
{
    /// <summary>
    /// A pool for items, items can be added to the pool, and removed from the pool asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShardedPool<T> where T : new()
    {
        private KeyValuePair<SpinLock, HashSet<T>>[] sets;
        private int count = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedPool&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="shardCount">The number of shards to use</param>
        public ShardedPool(int shardCount)
        {
            sets = new KeyValuePair<SpinLock, HashSet<T>>[shardCount];
            for (int i = 0; i < shardCount; i++)
            {
                sets[i] = new KeyValuePair<SpinLock, HashSet<T>>(new SpinLock(), new HashSet<T>());
            }
        }

        /// <summary>
        /// Adds an item to the pool
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            int i = 0;
            while (true)
            {
                try
                {
                    if (sets[i].Key.TryLock())
                    {
                        sets[i].Value.Add(item);
                        Interlocked.Increment(ref count);
                        return;
                    }
                }
                finally
                {
                    if (sets[i].Key.IsOwned)
                        sets[i].Key.Unlock();
                }
            }
        }

        /// <summary>
        /// Gets an instance from the pool, either reusing an item or creating a new one
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            if (count == 0)
                return new T();

            int i = 0;
            while (true)
            {
                try
                {
                    if (sets[i].Key.TryLock())
                    {
                        throw new NotImplementedException();
                    }
                }
                finally
                {
                    if (sets[i].Key.IsOwned)
                        sets[i].Key.Unlock();
                }
            }
        }
    }
}
