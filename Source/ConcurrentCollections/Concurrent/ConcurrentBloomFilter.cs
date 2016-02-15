using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Concurrent
{
    /// <summary>
    /// A bloom filter, items can be added to the collection and then the filter can be queried for containment. False positivies are possible but false negatives are not
    /// </summary>
    public class ConcurrentCountingBloomFilter
    {
        #region fields
        private Int32 largestIndex;
        /// <summary>
        /// the remaining capacity of this filter
        /// </summary>
        public UInt32 Capacity
        {
            get
            {
                return (UInt32)(Int32.MaxValue - filter[largestIndex] - 10);
            }
        }

        private Int32[] filter;

        private int set;
        #endregion

        #region constructor
        /// <summary>
        /// Construct a new bloom filter
        /// </summary>
        /// <param name="size">the size of the filter, larger will return less false positives</param>
        /// <param name="set">the number of lines set per item, more will return less false positives but make the set degrade faster</param>
        public ConcurrentCountingBloomFilter(int size, int set)
        {
            filter = new int[size];
            for (int i = 0; i < size; i++)
            {
                filter[i] = int.MinValue;
            }
            this.set = set;
        }
        #endregion

        #region add
        /// <summary>
        /// Add an item to the collection
        /// </summary>
        /// <param name="o"></param>
        public unsafe void Add<T>(T o)
        {
            if (Capacity == 0)
                throw new InvalidOperationException("Filter has no remaining capacity");

            uint* myPos = stackalloc uint[set];
            GetPositions(myPos, o);
            for (int i = 0; i < set; i++)
            {
                int a = Interlocked.Increment(ref filter[myPos[i]]);
                Transacted.Transaction(ExchangeLargest, ref largestIndex, (int)myPos[i]);
            }
        }

        private bool ExchangeLargest(int value, int newValue)
        {
            return filter[newValue] > filter[value];
        }
        #endregion

        #region remove
        /// <summary>
        /// remove an item from the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns>True, if item was removed, otherwise false if this item could not possibly be in this set</returns>
        public unsafe bool Remove<T>(T o)
        {
            uint* myPos = stackalloc uint[set];
            GetPositions(myPos, o);
            for (int i = 0; i < set; i++)
            {
                if (Transacted.Transaction(ExchangeValid, ref filter[myPos[i]]) == int.MinValue)
                {
                    //this transaction is invalid, undo the part which has already been done
                    for (int u = 0; u < i; u++)
                    {
                        Interlocked.Increment(ref filter[myPos[u]]);
                    }
                    return false;
                }
            }
            return true;
        }

        private int ExchangeValid(int value, int[] args, out bool ex)
        {
            ex = value != int.MinValue;
            return value - 1;
        }
        #endregion

        #region queries
        /// <summary>
        /// Checks if the given item has been added to the collection\n
        /// FALSE POSITIVES are possible.\n
        /// FALSE NEGATIVES are NOT possible.\n
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public unsafe bool Contains<T>(T o)
        {
            uint* myPos = stackalloc uint[set];
            GetPositions(myPos, o);
            for (int i = 0; i < set; i++)
            {
                if (filter[(myPos[i])] == int.MinValue)
                    return false;
            }
            return true;
        }
        #endregion

        public void Union(ConcurrentCountingBloomFilter other)
        {
            if (other.filter.Length != filter.Length)
                throw new ArgumentException("Other filter must have the same size!");
            if (other.set != set)
                throw new ArgumentException("Other filter must set the same number of lines!");

            for (int i = 0; i < other.filter.Length; i++)
            {
                Interlocked.Add(ref filter[i], other.filter[i]);
            }
        }

        #region helpers
        private unsafe void GetPositions(uint* array, object o)
        {
            int hashcode = o.GetHashCode();
            uint uHash = *((uint*)&hashcode);
            for (int i = 0; i < set; i++)
            {
                (array[i]) = Random(uHash, (uint)filter.Length);
                uHash = Random(uHash, uint.MaxValue);
            }
        }
        #endregion

        #region test
#if DEBUG
        public static unsafe void Test(int size)
        {
            ConcurrentCountingBloomFilter filter = new ConcurrentCountingBloomFilter(100, 5);
            Console.WriteLine("Testing concurrent bloom filter");

            if (filter.Contains(new object()))
                throw new Exception("False positive on an empty set!");

            object a = new object();

            uint* indices = stackalloc uint[5];
            filter.GetPositions(indices, a);
            uint* indices2 = stackalloc uint[5];
            filter.GetPositions(indices2, a);
            for (int i = 0; i < 5; i++)
            {
                if ((indices[i]) != (indices2[i]))
                    throw new Exception("Incorrect index!");
            }
            Console.WriteLine("Correctly generated hash positions");

            filter.Add(a);
            Console.WriteLine("Added an item");
            if (!filter.Contains(a))
                throw new Exception("False negative!");
            Console.WriteLine("Successfully tested for set membership");

            object b = new object();
            filter.Add(b);
            Console.WriteLine("Added an item");
            if (!filter.Contains(b))
                throw new Exception("False negative!");

            filter.Remove(b);
            Console.WriteLine("removed an item");
            if (filter.Contains(b))
                Console.WriteLine("False positive");

            if (filter.Remove(new object()))
                throw new Exception("Removed an item which could not be in the set");

            if (!filter.Contains(a))
                throw new Exception("False negative!");

            List<object> objects = new List<object>();
            for (int i = 0; i < size; i++)
            {
                objects.Add(new object());
            }

            Console.WriteLine("Adding many items to bloom filter");
            DateTime start = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                var z = objects[i];
                filter.Add(z);
                if (!filter.Contains(z))
                    throw new Exception("False negative!");
            }
            TimeSpan t = DateTime.Now - start;
            Console.WriteLine("success, " + t.TotalMilliseconds / ((float)size) + "ms per add");

            Console.WriteLine("removing many items");
            start = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                var z = objects[i];
                if (!filter.Remove(z))
                    throw new Exception("Removal failed");
            }
            t = DateTime.Now - start;
            Console.WriteLine("success, " + t.TotalMilliseconds / ((float)size) + "ms per removal");
        }
#endif
        #endregion

        #region random number generation
        const double REAL_UNIT_INT = 1.0 / ((double)int.MaxValue + 1.0);
        const uint U = 273326509 >> 19;

        public uint Random(uint seed, uint upperBound)
        {
            uint x = seed;
            uint w = 273326509;

            uint t = (x ^ (x << 11));

			// The explicit int cast before the first multiplication gives better performance.
			return (uint)((REAL_UNIT_INT * (int)(0x7FFFFFFF & (w = (w ^ U) ^ (t ^ (t >> 8))))) * upperBound);
        }
        #endregion

    }
}
