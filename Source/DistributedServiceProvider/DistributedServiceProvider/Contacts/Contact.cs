using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using System.Threading;
using ProtoBuf.Meta;

namespace DistributedServiceProvider.Contacts
{
    /// <summary>
    /// A contact point for a remote routing table
    /// </summary>
    [ProtoContract, ProtoInclude(3, typeof(LocalContact)), ProtoInclude(4, typeof(ProxyContact))]
    public abstract class Contact
    {
        /// <summary>
        /// The identifier of the remote routing table
        /// </summary>
        [ProtoMember(1)]
        public Identifier512 Identifier;

        [ProtoMember(2)]
        private byte[] networkIdBytes;

        /// <summary>
        /// The id of the network which the routing table oeprates on
        /// </summary>
        public Guid NetworkId
        {
            get
            {
                return new Guid(networkIdBytes);
            }
            set
            {
                networkIdBytes = value.ToByteArray();
            }
        }

        public abstract DistributedRoutingTable Table
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Contact"/> class.
        /// </summary>
        /// <param name="identifier">The identifier of the DistributedRoutingTable this contact represents</param>
        /// <param name="networkId">The network id.</param>
        public Contact(Identifier512 identifier, Guid networkId)
        {
            Identifier = identifier;
            networkIdBytes = networkId.ToByteArray();
        }

        protected Contact()
        {

        }

        /// <summary>
        /// Sends a message to the consumer with the given Id
        /// </summary>
        /// <param name="consumerId">The consumer id.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response fromthe remote consumer, or null if there was no response</returns>
        public abstract void Send(Contact source, Guid consumerId, byte[] message, bool reliable = true, bool ordered = true, int channel = 1);

        /// <summary>
        /// Pings this instance.
        /// </summary>
        /// <returns>The response time, or Timespan.MaxValue if it timed out</returns>
        public abstract TimeSpan Ping(Contact source, TimeSpan timeout);
    }
}
