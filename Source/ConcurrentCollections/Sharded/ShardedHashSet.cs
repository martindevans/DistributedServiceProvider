using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentCollections.Sharded
{
    /// <summary>
    /// A hashet which allows concurrent access
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShardedHashSet<T>
        :ShardedCollection<T, HashSet<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedHashSet&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="shardCount">The number of shards to use (more allows higher amounts of concurrency, but makes queries more expensive)</param>
        public ShardedHashSet(int shardCount)
            :base(shardCount)
        {

        }
    }
}
