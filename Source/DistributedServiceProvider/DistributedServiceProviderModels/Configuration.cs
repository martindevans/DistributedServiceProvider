using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ProtoBuf;
using System.Diagnostics;

namespace DistributedServiceProvider.Base
{
    /// <summary>
    /// The configuration of the network
    /// </summary>
    [ProtoContract]
    public class Configuration
    {
        [ProtoMember(1)]
        private int bucketSize = 20;
        /// <summary>
        /// Gets or sets the target size ofthe contact buckets
        /// </summary>
        /// <value>The size of the bucket.</value>
        public int BucketSize
        {
            get
            {
                if (!(bucketSize > 0))
                    throw new ArgumentException("Bucket size must be > 0");
                return bucketSize;
            }
            set
            {
                Interlocked.Exchange(ref bucketSize, value);
            }
        }

        [ProtoMember(2)]
        private long pingTimeoutTicks = TimeSpan.FromSeconds(2).Ticks;
        /// <summary>
        /// Gets or sets the default ping timeout.
        /// </summary>
        /// <value>The ping timeout.</value>
        public TimeSpan PingTimeout
        {
            get
            {
                return new TimeSpan(pingTimeoutTicks);
            }
            set
            {
                Interlocked.Exchange(ref pingTimeoutTicks, value.Ticks);
            }
        }

        [ProtoMember(3)]
        private int lookupTimeout = 3000;
        /// <summary>
        /// Gets or sets the timeout to use for individual lookups in the iterative lookup operation
        /// </summary>
        /// <value>The lookup timeout.</value>
        public int LookupTimeout
        {
            get
            {
                return lookupTimeout;
            }
            set
            {
                Interlocked.Exchange(ref lookupTimeout, value);
            }
        }

        [ProtoMember(4)]
        private int lookupConcurrency = 5;
        /// <summary>
        /// Gets the maximum number of nodes which may be held as potential candidates in the iterative lookup operation
        /// </summary>
        public int LookupConcurrency
        {
            get
            {
                if (!(lookupConcurrency > 0))
                    throw new ArgumentException("Lookup concurrency must be > 0");
                return lookupConcurrency;
            }
            set
            {
                Interlocked.Exchange(ref lookupConcurrency, value);
            }
        }

        [ProtoMember(5)]
        private long bucketRefreshPeriod = TimeSpan.FromMinutes(10).Ticks;
        /// <summary>
        /// Gets or sets the period of time between refreshes
        /// </summary>
        /// <value>The bucket refresh period.</value>
        public TimeSpan BucketRefreshPeriod
        {
            get
            {
                return TimeSpan.FromTicks(bucketRefreshPeriod);
            }
            set
            {
                Interlocked.Exchange(ref bucketRefreshPeriod, value.Ticks);
            }
        }

#if DEBUG
        [ProtoMember(6)]
        private int updateRoutingTable = 1;
        /// <summary>
        /// Gets or sets a value indicating whether the routing table should be updated.
        /// Turning off routing table updates will cause the network to stagnate very quickly and is almost always a BAD IDEA
        /// </summary>
        /// <value><c>true</c> if should update routing table; otherwise, <c>false</c>.</value>
        public bool UpdateRoutingTable
        {
            get
            {
                return updateRoutingTable == 1;
            }
            set
            {
                Interlocked.Exchange(ref updateRoutingTable, value ? 1 : 0);
            }
        }
#endif
    }
}
