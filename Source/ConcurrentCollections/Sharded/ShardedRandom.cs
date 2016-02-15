using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentCollections.Sharded
{
    public class ShardedRandom
    {
        private Shard[] shards;

        public ShardedRandom(int shardCount)
            :this(shardCount, Environment.TickCount)
        {
        }

        public ShardedRandom(int shardCount, int seed)
        {
            shards = new Shard[shardCount];
            for (int i = 0; i < shardCount; i++)
            {
                unchecked
                {
                    shards[i] = new Shard(seed + i);
                }
            }
        }

        private Shard GetShard()
        {
            while (true)
                for (int i = 0; i < shards.Length; i++)
                    if (shards[i].spinLock.TryLock())
                        return shards[i];
        }

        #region next (int)
        /// <summary>
        /// Returns the next nonnegative random number
        /// </summary>
        /// <returns></returns>
        public int Next()
        {
            return Transaction(GetShard(), NextTransactionMax, 0, Int32.MaxValue);
        }

        private int NextTransaction(int foo, int bar, Random r)
        {
            return r.Next();
        }

        /// <summary>
        /// returns the next random value
        /// </summary>
        /// <param name="maxValue">The exclusive upperbound</param>
        /// <returns></returns>
        public int Next(int maxValue)
        {
            return Transaction(GetShard(), NextTransactionMax, 0, maxValue);
        }

        private int NextTransactionMax(int foo, int maxValue, Random r)
        {
            return r.Next(maxValue);
        }

        /// <summary>
        /// Gets the next random number
        /// </summary>
        /// <param name="minValue">the inclusive lower bound</param>
        /// <param name="maxValue">the exclusive upper bound</param>
        /// <returns></returns>
        public int Next(int minValue, int maxValue)
        {
            return Transaction(GetShard(), NextTransactionMinMax, minValue, maxValue);
        }

        private int NextTransactionMinMax(int minValue, int maxValue, Random r)
        {
            return r.Next(minValue, maxValue);
        }

        private delegate int TransactionAction(int parameterA, int parameterB, Random s);
        private int Transaction(Shard s, TransactionAction action, int parameterA, int parameterB)
        {
            try
            {
                return action(parameterA, parameterB, s.random);
            }
            finally
            {
                s.spinLock.Unlock();
            }
        }
        #endregion

        public void NextBytes(byte[] b)
        {
            Shard s = GetShard();
            try
            {
                s.random.NextBytes(b);
            }
            finally
            {
                s.spinLock.Unlock();
            }
        }

        private class Shard
        {
            public readonly Random random;
            public readonly SpinLock spinLock = new SpinLock();

            public Shard(int seed)
            {
                random = new Random(seed);
            }
        }
    }
}
