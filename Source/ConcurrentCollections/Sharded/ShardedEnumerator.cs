using System;
using System.Collections.Generic;
using System.Text;

namespace ConcurrentCollections.Sharded
{
    public class ShardedEnumerator<T, C>
        : IEnumerator<T>
        where C : IShardedEnumerable<T>
    {
        C collection;

        private bool lockHeld = false;
        int shard = 0;
        IEnumerator<T> currentEnumerator;

        public ShardedEnumerator(C c)
        {
            collection = c;
        }

        public T Current
        {
            get
            {
                if (!lockHeld)
                    AcquireEnumeratorLock();
                return currentEnumerator.Current;
            }
        }

        public void Dispose()
        {
            if (lockHeld)
                ReleaseLock();
            if (currentEnumerator != null)
                currentEnumerator.Dispose();
            currentEnumerator = null;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

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
                if (shard < collection.ShardCount)
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

        public void Reset()
        {
            if (lockHeld)
                ReleaseLock();
            if (currentEnumerator != null)
                currentEnumerator.Dispose();
            shard = 0;
        }

        private void AcquireEnumeratorLock()
        {
            collection.LockShard(shard);
            lockHeld = true;
            currentEnumerator = collection.GetShardEnumerator(shard);
        }

        private void ReleaseLock()
        {
            collection.UnlockShard(shard);
            lockHeld = false;
        }
    }
}
