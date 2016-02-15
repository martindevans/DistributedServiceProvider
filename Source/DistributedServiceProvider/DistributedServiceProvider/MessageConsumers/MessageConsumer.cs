using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;

namespace DistributedServiceProvider.MessageConsumers
{
    /// <summary>
    /// A place where messages from the network are consumed
    /// </summary>
    public abstract class MessageConsumer
    {
        /// <summary>
        /// The table which this consumer is registered to
        /// </summary>
        public DistributedRoutingTable RoutingTable
        {
            get;
            private set;
        }

        /// <summary>
        /// The Id of this consumer
        /// </summary>
        public Guid ConsumerId { get; private set; }

        public MessageConsumer(Guid consumerId)
        {
            ConsumerId = consumerId;
        }

        /// <summary>
        /// Deliver a message encoded into the given byte array
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="source">The source of the message</param>
        /// <returns>The response to return</returns>
        public abstract void Deliver(Contact source, byte[] message);

        protected internal virtual void OnRegisteredToTable(DistributedRoutingTable table)
        {
            RoutingTable = table;
        }

        protected internal virtual void OnUnregisteredFromTable(DistributedRoutingTable table)
        {
            RoutingTable = null;
        }
    }
}
