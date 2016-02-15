using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DistributedServiceProvider.Base;
using System.Diagnostics;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.MessageConsumers;
using LoggerMessages;

namespace DistributedServiceProvider
{
    class ContactBucket
        :IEnumerable<Contact>
    {
        #region fields
        public readonly DistributedRoutingTable DistributedRoutingTable;
        public Configuration Configuration { get { return DistributedRoutingTable.Configuration; } }
        public Contact LocalContact { get { return DistributedRoutingTable.LocalContact; } }

        private object refreshLock = new object();
        private DateTime lastRefresh = DateTime.MinValue;

        private ReaderWriterLockSlim contactsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private RecentlyUsedQueue<Contact> contactsQueue = new RecentlyUsedQueue<Contact>();
        private LinkedList<Contact> contactsStack = new LinkedList<Contact>();

        public int Count
        {
            get
            {
                try
                {
                    contactsLock.EnterReadLock();
                    return contactsStack.Count;
                }
                finally
                {
                    contactsLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the amount of remaining space.
        /// </summary>
        /// <value>The remaining space.</value>
        public int RemainingSpace
        {
            get
            {
                contactsLock.EnterReadLock();
                try
                {
                    return Configuration.BucketSize - contactsQueue.Count;
                }
                finally
                {
                    contactsLock.ExitReadLock();
                }
            }
        }

        public readonly int CommonBitsCount;
        #endregion

        #region construction
        public ContactBucket(DistributedRoutingTable drt, int commonBitsCount)
        {
            this.DistributedRoutingTable = drt;
            this.CommonBitsCount = commonBitsCount;
        }
        #endregion

        /// <summary>
        /// Updates the given contact to the front of the most recently used queue
        /// </summary>
        /// <param name="source">The source.</param>
        internal void Update(Contact source)
        {
            try
            {
                contactsLock.EnterWriteLock();

                if (RemainingSpace <= 0)
                    Trim();
                if (RemainingSpace > 0)
                    if (contactsQueue.Use(source))
                        contactsStack.AddFirst(source);
            }
            finally
            {
                contactsLock.ExitWriteLock();
            }

            SendState();
        }

        /// <summary>
        /// Trims the contacts set until it meets the size requirements
        /// </summary>
        private void Trim()
        {
            try
            {
                contactsLock.EnterWriteLock();

                //remove least recently used nodes which do not respond to ping
                ISet<Contact> dead = new HashSet<Contact>(
                    contactsQueue                               //enumeration from least->most recently used
                    .Where(a => a.Ping(LocalContact, Configuration.PingTimeout) == TimeSpan.MaxValue)  //select dead nodes
                    .Take(-RemainingSpace));                    //take only as many as we need

                foreach (var c in dead)
                {
                    contactsQueue.Remove(c);
                    contactsStack.Remove(c);
                }

                //remove the newest nodes added to the collection until the collection is small enough
                while (RemainingSpace < 0)
                {
                    contactsQueue.Remove(contactsStack.First.Value);
                    contactsStack.RemoveFirst();
                }
            }
            finally
            {
                contactsLock.ExitWriteLock();
            }
        }

        #region IEnumerable
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Contact> GetEnumerator()
        {
            List<Contact> contacts = new List<Contact>();

            try
            {
                contactsLock.EnterReadLock();

                contacts.AddRange(contactsQueue);
                return contacts.GetEnumerator();
            }
            finally
            {
                contactsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<Contact>).GetEnumerator();
        }
        #endregion

        #region refresh
        public void Refresh(bool force)
        {
            if (Monitor.TryEnter(refreshLock))
            {
                try
                {
                    if (force || DateTime.Now - lastRefresh > Configuration.BucketRefreshPeriod)
                    {
                        lastRefresh = DateTime.Now;

                        GetClosestNodes getClosest = DistributedRoutingTable.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID);
                        foreach (var item in getClosest.GetClosestContacts(CreateRefreshId()))
                            item.Ping(LocalContact, Configuration.PingTimeout);
                    }
                }
                finally
                {
                    Monitor.Exit(refreshLock);
                }
            }

            SendState();
        }

        private int refreshIdInt = int.MaxValue / 2;
        private Identifier512 CreateRefreshId()
        {
            int[] idInts = LocalContact.Identifier.GetInts().ToArray();

            int commonInts = CommonBitsCount / 32;
            int additionalBits = CommonBitsCount - commonInts * 32;
            Debug.Assert(additionalBits <= 32);

            int r = Interlocked.Increment(ref refreshIdInt);

            //this is the int which needs to be divided on a bitwise basis
            //ie. the first "additionalBits" bits need to be kept, the rest need to be replaced
            idInts[commonInts] = (idInts[commonInts] & int.MaxValue >> additionalBits) | (r & int.MaxValue >> (32 - additionalBits));

            //these are the ints which simply need replacing
            for (int i = commonInts + 1; i < idInts.Length; i++)
                idInts[i] = Interlocked.Increment(ref refreshIdInt);

            return new Identifier512(idInts);
        }
        #endregion

        public override string ToString()
        {
            return "Count = " + Count;
        }

        private void SendState()
        {
#if DEBUG
            //new BucketState()
            //{
            //    Identifiers = this.Select(a => a.Identifier).ToArray(),
            //    LocalId = LocalContact.Identifier,
            //    Index = (ushort)CommonBitsCount,
            //}.Send();
#endif
        }
    }
}
