using System;
using System.Threading;

namespace ConcurrentCollections
{
    public class SpinLock
    {
        private Thread owner;
        private int recursion;

        public bool IsLocked
        {
            get
            {
                return recursion > 0;
            }
        }
        public bool IsOwned
        {
            get
            {
                return owner == Thread.CurrentThread;
            }
        }

        /// <summary>
        /// Enters the lock. The calling thread will spin wait until it gains ownership of the lock.
        /// </summary>
        public void Lock()
        {
            while (!TryLock()) ;
        }

        /// <summary>
        /// Tries to enter the lock.
        /// </summary>
        /// <returns><c>true</c> if the lock was successfully taken; else <c>false</c>.</returns>
        public bool TryLock()
        {
            // get the current thead
            var caller = Thread.CurrentThread;

            // early out: return if the current thread already has ownership.
            if (owner == caller)
            {
                Interlocked.Increment(ref recursion);
                return true;
            }

            // try to take the lock, if the current owner is null.
            bool success = Interlocked.CompareExchange(ref owner, caller, null) == null;
            if (success)
                Interlocked.Increment(ref recursion);
            return success;
        }

        /// <summary>
        /// Tries to enter the lock.
        /// Fails after the specified time has elapsed without aquiring the lock.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns><c>true</c> if the lock was successfully taken; else <c>false</c>.</returns>
        public bool TryLock(TimeSpan timeout)
        {
            DateTime start = DateTime.Now;

            while (!TryLock())
            {
                if (DateTime.Now - start > timeout)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Exits the lock. This allows other threads to take ownership of the lock.
        /// </summary>
        public void Unlock()
        {
            // get the current thread.
            var caller = Thread.CurrentThread;

            if (caller == owner)
            {
                Interlocked.Decrement(ref recursion);
                if (recursion == 0)
                    owner = null;
            }
            else
                throw new InvalidOperationException("Exit cannot be called by a thread which does not currently own the lock.");
        }
    }
}
