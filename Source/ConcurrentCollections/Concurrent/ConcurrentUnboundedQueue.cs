using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Concurrent
{
    /// <summary>
    /// A queue of unbounded size. Allows concurrent adding and removing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentUnboundedQueue<T>
    {
        //TODO: Write the concurrent unbounded queue

        Queue<T> tempQueue = new Queue<T>();

        /// <summary>
        /// Gets number of items currently in the Queue
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get
            {
                lock (tempQueue)
                {
                    return tempQueue.Count;   
                }
            }
        }

        /// <summary>
        /// Adds an item to the back of the queue
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item)
        {
            lock (tempQueue)
            {
                tempQueue.Enqueue(item);
            }
        }

        /// <summary>
        /// Removes an item from the front of the queue
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there are no items in the queue</exception>
        /// <returns></returns>
        public T Dequeue()
        {
            lock (tempQueue)
            {
                T a = tempQueue.Dequeue();
                return a;
            }
        }

        /// <summary>
        /// Attempts to dequeue an item
        /// </summary>
        /// <param name="item">The item from the front of the queue</param>
        /// <returns>True, if an item was returned, False if the queue is empty and no item was returned</returns>
        public bool TryDequeue(out T item)
        {
            lock (tempQueue)
            {
                try
                {
                    item = Dequeue();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    item = default(T);
                    return false;
                }
            }
        }

        #region test
#if DEBUG
        public static void Test(int size)
        {
            ConcurrentUnboundedQueue<int> queue = new ConcurrentUnboundedQueue<int>();

            for (int i = 0; i < size; i++)
            {
                queue.Enqueue(i);
            }
            if (queue.Count != size)
                throw new Exception("Incorrecty count");
            for (int i = 0; i < size; i++)
            {
                if (queue.Dequeue() != i)
                    throw new Exception("Incorrect value");
            }

            if (queue.Count != 0)
                throw new Exception("queue is not empty");

            bool except = false;
            try
            {
                queue.Dequeue();
            }
            catch (InvalidOperationException)
            {
                except = true;
            }
            if (!except)
                throw new Exception("Dequeue from an empty queue  did not throw an excepetion");

            for (int i = 0; i < size; i++)
            {
                queue.Enqueue(i);
            }
            if (queue.Count != size)
                throw new Exception("Incorrect count");
            for (int i = 0; i < size; i++)
            {
                int val;
                if (!queue.TryDequeue(out val))
                    throw new Exception("No items availble to dequeue");
                if (val != i)
                    throw new Exception("Incorrect value dequeued");
            }

            if (queue.Count != 0)
                throw new Exception("queue is not empty");
        }
#endif
        #endregion
    }
}
