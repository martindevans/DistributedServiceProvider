using System.Threading.Tasks;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Base.Extensions;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.MessageConsumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DistributedServiceProvider
{
    /// <summary>
    /// Provides services to route around a distributed system
    /// </summary>
    public class DistributedRoutingTable
    {
        #region fields
        /// <summary>
        /// The identifier of this routing table
        /// </summary>
        public readonly Identifier512 LocalIdentifier;

        /// <summary>
        /// The Id of the network this routing table operates upon
        /// </summary>
        public readonly Guid NetworkId;

        /// <summary>
        /// The configuration of this node
        /// </summary>
        public readonly Configuration Configuration;

        private ReaderWriterLockSlim consumerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private Dictionary<Guid, MessageConsumer> consumers = new Dictionary<Guid, MessageConsumer>();

        private ContactCollection contacts;

        /// <summary>
        /// The contact for this routing table
        /// </summary>
        public readonly Contact LocalContact;

        /// <summary>
        /// The callback service to mediate long running conversations
        /// </summary>
        public Callback MessageCallback;
        private GetClosestNodes getClosest;

        public int ContactCount
        {
            get
            {
                return contacts.Count;
            }
        }
        #endregion

        #region construction
        /// <summary>
        /// Create a new DistributedRoutingTable
        /// </summary>
        /// <param name="localIdentifier">The identifier of this node, or null to autogenerate one</param>
        /// <param name="createLocalContact">A factory function which creates a contact for this table</param>
        /// <param name="networkId">the ID of the network this routing table is part of</param>
        /// <param name="configuration">The configuration of this node</param>
        public DistributedRoutingTable(Identifier512 localIdentifier, Func<DistributedRoutingTable, Contact> createLocalContact, Guid networkId, Configuration configuration)
        {
            LocalIdentifier = localIdentifier ?? Identifier512.NewIdentifier();
            NetworkId = networkId;
            Configuration = configuration;
            LocalContact = createLocalContact(this);

            contacts = new ContactCollection(this);

            //Register internal consumers
            RegisterConsumer(MessageCallback = new Callback());
            RegisterConsumer(getClosest = new GetClosestNodes(contacts, MessageCallback));

#if DEBUG
            //new DRTConstructed(localIdentifier, networkId, configuration).Send();
#endif
        }
        #endregion

        #region bootstrap
        /// <summary>
        /// Bootstrap this node onto the network using the given set of known initial nodes
        /// </summary>
        /// <param name="initialContacts">The initial contacts.</param>
        public void Bootstrap(params Contact[] initialContacts)
        {
            Bootstrap(initialContacts as IEnumerable<Contact>);
        }

        public void Bootstrap(IEnumerable<Contact> initialContacts)
        {
            //Add initial contacts
            Parallel.ForEach(initialContacts.Where(a => a != null), c => {
                if (c.Ping(LocalContact, Configuration.PingTimeout) < TimeSpan.MaxValue)
                    contacts.Update(c);
            });

            //Lookup selfto populate local buckets
            GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(LocalIdentifier).ForEach((c) => { contacts.Update(c); });

            contacts.RefreshFarBuckets(true);

            #if DEBUG
            //new ClearQueries().Send();
            #endif
        }
        #endregion

        #region message consuming
        /// <summary>
        /// Registers a consumer with this routing table
        /// </summary>
        /// <param name="consumer">The consumer.</param>
        /// <exception cref="ArgumentException">Thrown if the network Ids do not match</exception>
        public MessageConsumer RegisterConsumer(MessageConsumer consumer)
        {
            try
            {
                consumerLock.EnterWriteLock();
                consumers[consumer.ConsumerId] = consumer;

                AutoLink(consumer);

                consumer.OnRegisteredToTable(this);
            }
            finally
            {
                consumerLock.ExitWriteLock();
            }

            return consumer;
        }

        private void AutoLink(MessageConsumer consumer)
        {
            foreach (var field in 
                consumer
                .GetType()
                .GetFields()
                .Where(a => a.GetCustomAttributes(true).Any(b => b.GetType().IsAssignableFrom(typeof(LinkedConsumerAttribute))))
                .Select(a => new KeyValuePair<LinkedConsumerAttribute, FieldInfo>((a.GetCustomAttributes(true).Where(b => b.GetType() == typeof(LinkedConsumerAttribute)).First() as LinkedConsumerAttribute), a)))
            {
                try
                {
                    var c = consumers.Where(a => a.Value.GetType().IsAssignableFrom(field.Value.FieldType));
                    var d = c.Where(a => a.Key == field.Key.Id);
                    MessageConsumer msgConsumer = null;

                    if (d.Count() == 0 && !field.Key.Optional)
                    {
                        var cons = field.Value.FieldType.GetConstructor(new Type[] { typeof(Guid) });
                        if (cons != null)
                            msgConsumer = RegisterConsumer((MessageConsumer)cons.Invoke(new object[] { field.Key.Id }));
                    }
                    else
                        msgConsumer = d.First().Value;

                    if (msgConsumer == null)
                        throw new InvalidOperationException("No such consumer exists for linking");

                    field.Value.SetValue(consumer, msgConsumer);
                }
                catch (InvalidOperationException e)
                {
                    if (!field.Key.Optional)
                        throw new InvalidOperationException("No such consumer exists for linking " + field.Value.FieldType);
                }
            }
        }

        /// <summary>
        /// Removes the consumer.
        /// </summary>
        /// <param name="consumer">The consumer.</param>
        /// <returns>true, if the consumer was removed, false if it was not found</returns>
        public bool RemoveConsumer(MessageConsumer consumer)
        {
            try
            {
                consumerLock.EnterWriteLock();

                consumer.OnUnregisteredFromTable(this);

                return consumers.Remove(consumer.ConsumerId);
            }
            finally
            {
                consumerLock.ExitWriteLock();
            }
        }

        public T GetConsumer<T>(Guid guid) where T : MessageConsumer
        {
            var c = consumers[guid];

            return (T)c;
        }

        public T GetConsumer<T>(Guid g, Func<T> create) where T : MessageConsumer
        {
            MessageConsumer c;
            if (!consumers.TryGetValue(g, out c))
                RegisterConsumer(c = create());

            return (T)c;
        }

        /// <summary>
        /// Delivers the specified message to the correct consumer
        /// </summary>
        /// <param name="source">The source of this message, may be null</param>
        /// <param name="consumerId">The consumer id.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response to return to the sender</returns>
        public void Deliver(Contact source, Guid consumerId, byte[] message)
        {
            DeliverPing(source);

            try
            {
                consumerLock.EnterReadLock();
                consumers[consumerId].Deliver(source, message);
            }
            finally
            {
                consumerLock.ExitReadLock();
            }
        }
        #endregion

        /// <summary>
        /// Delivers a ping
        /// </summary>
        /// <param name="c">The contact who sent the ping</param>
        public void DeliverPing(Contact c)
        {
            if (c == null)
                return;

            if (c.Table == null)
                c.Table = this;

            contacts.Update(c);
        }

        /// <summary>
        /// Refresh contacts collection, dropping dead contacts
        /// </summary>
        /// <param name="force">true, if the refresh must go ahead, false if the collection should only go ahead if the refresh timeout has been achieved</param>
        public void Refresh(bool force)
        {
            contacts.Refresh(force);
        }

        public override string ToString()
        {
            return LocalIdentifier.ToString();
        }
    }
}
