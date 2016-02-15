using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ConcurrentCollections.Sharded
{
    public interface IShardedEnumerable<T>
        :IEnumerable<T>, IEnumerable
    {
        int ShardCount
        {
            get;
        }

        void LockShard(int i);

        void UnlockShard(int i);

        IEnumerator<T> GetShardEnumerator(int i);
    }
}
