using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base.Extensions;
using System.Threading;
using LoggerMessages;

namespace DistributedServiceProvider
{
    class ContactCollection
    {
        #region fields
        /// <summary>
        /// The root identifier to build this collection around
        /// </summary>
        public Identifier512 LocalIdentifier
        {
            get
            {
                return LocalContact.Identifier;
            }
        }

        public readonly Contact LocalContact;

        public readonly DistributedRoutingTable DistributedRoutingTable;

        /// <summary>
        /// The Id of the network this collection operates upon
        /// </summary>
        public readonly Guid NetworkId;

        public readonly Configuration Configuration;

        private ContactBucket[] buckets = new ContactBucket[Identifier512.BIT_LENGTH];

        public int Count
        {
            get
            {
                return buckets.Where(a => a != null).Select(a => a.Count).Aggregate((a, b) => a + b);
            }
        }
        #endregion

        #region construction
        public ContactCollection(DistributedRoutingTable drt)
        {
            LocalContact = drt.LocalContact;
            NetworkId = drt.NetworkId;
            Configuration = drt.Configuration;
            this.DistributedRoutingTable = drt;

            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = new ContactBucket(drt, i);
        }
        #endregion

        /// <summary>
        /// Moves the specified contact to the top of the most recently used queue
        /// </summary>
        /// <param name="source">The source.</param>
        internal void Update(Contact source)
        {
#if DEBUG
            if (Configuration.UpdateRoutingTable)
            {
#endif
                if (source == null)
                    return;
                if (source.Identifier == LocalIdentifier)
                    return;
                if (source.NetworkId != NetworkId)
                    throw new ArgumentException("Network Id of contact and ContactCollection must be the same");

                buckets[Identifier512.CommonPrefixLength(source.Identifier, LocalIdentifier)].Update(source);
#if DEBUG
            }
#endif
        }

        /// <summary>
        /// Refresh all buckets
        /// </summary>
        /// <param name="force"></param>
        public void Refresh(bool force)
        {
            foreach (var bucket in buckets)
                bucket.Refresh(force);
        }

        /// <summary>
        /// Refreshes the all the buckets with a smaller similar prefix length than the largest non empty bucket
        /// </summary>
        /// <param name="force">if set to <c>true</c> force the bucket to refresh no matter if it has been used recently.</param>
        public void RefreshFarBuckets(bool force)
        {
            bool refresh = false;
            for (int i = buckets.Length - 1; i >= 0; i--)
            {
                if (buckets[i].Count > 0)
                    refresh = refresh || buckets[i].Count > 0;

                if (refresh)
                    buckets[i].Refresh(force);
            }
        }

        /// <summary>
        /// Returns an enumeration of all nodes in ascending order of distance from the given identifier
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        public IEnumerable<Contact> ClosestNodes(Identifier512 identifier)
        {
            int mid = Identifier512.CommonPrefixLength(identifier, LocalIdentifier);

            if (mid != Identifier512.BIT_LENGTH)
                foreach (var contact in buckets[mid].OrderWithComparer(new ContactComparer(identifier)))
                    yield return contact;
            else
                yield return LocalContact;

            //loop through buckets, moving up and down from mid concatenating the two buckets either side of mid and returning them in order of distance
            List<Contact> contacts = new List<Contact>();
            bool moreLow = true;
            bool moreHigh = true;
            for (int i = 1; moreHigh || moreLow; i++)
            {
                int indexHigh = mid + i;
                int indexLow = mid - i;

                contacts.Clear();

                if (indexHigh >= buckets.Length)
                    moreHigh = false;
                else
                    contacts.AddRange(buckets[indexHigh]);

                if (indexLow < 0)
                    moreLow = false;
                else
                    contacts.AddRange(buckets[indexLow]);

                foreach (var contact in contacts.OrderWithComparer(new ContactComparer(identifier)))
                    yield return contact;
            }
        }
    }
}
