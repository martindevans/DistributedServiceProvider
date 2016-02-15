using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Blocking
{
    public class BlockingQueue<T>
    {
        private Queue<T> myQueue;
        private volatile bool open = true;

        public int Count
        {
            get
            {
                lock (myQueue)
                {
                    if (open)
                        return myQueue.Count;
                    else
                        throw new InvalidOperationException("Queue closed");
                }
            }
        }

        public bool isClosed
        {
            get
            {
                lock (myQueue)
                {
                    return !open;
                }
            }
        }

        #region constructor
        public BlockingQueue()
        {
            myQueue = new Queue<T>();
        }

        public BlockingQueue(int capacity)
        {
            myQueue = new Queue<T>(capacity);
        }

        public BlockingQueue(IEnumerable<T> collection)
        {
            myQueue = new Queue<T>(collection);
        }
        #endregion

        #region enqueue
        public void Enqueue(T item)
        {
            lock (myQueue)
            {
                myQueue.Enqueue(item);
                Monitor.Pulse(myQueue);
            }
        }
        #endregion

        #region dequeue
        public T Dequeue(TimeSpan t)
        {
            return Dequeue(t.Milliseconds);
        }

        public T Dequeue(int milliseconds)
        {
            lock (myQueue)
            {
                while (myQueue.Count == 0)
                    if (!Monitor.Wait(myQueue, milliseconds))
                    {
                        throw new TimeoutException("wait timed out");
                    }

                if (open)
                    return myQueue.Dequeue();
                else
                    throw new InvalidOperationException("Queue closed");
            }
        }

        public T Dequeue()
        {
            return Dequeue(Timeout.Infinite);
        }
        #endregion

        #region destructor
        ~BlockingQueue()
        {
            lock (myQueue)
            {
                if (open)
                    close();
            }
        }

        public void close()
        {
            lock (myQueue)
            {
                open = false;
                Monitor.PulseAll(myQueue);
            }
        }
        #endregion
    }
}
