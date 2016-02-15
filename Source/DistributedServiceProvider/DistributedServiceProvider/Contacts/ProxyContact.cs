using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using ProtoBuf;

namespace DistributedServiceProvider.Contacts
{
    [ProtoContract]
    public class ProxyContact
        :Contact
    {
        #region fields
        public override DistributedRoutingTable Table
        {
            get;
            set;
        }

        [ProtoMember(1)]
        private int factoryKey;
        private Factory factory
        {
            get
            {
                return factories[factoryKey];
            }
        }

        [ProtoMember(2)]
        private byte[] serialised;

        public IProxy ProxyInstance
        {
            get;
            private set;
        }

        Action deliverPing;
        Action<Guid, byte[]> deliverData;
        #endregion

        public ProxyContact()
        {

        }

        public ProxyContact(int factory, IProxy proxy)
        {
            deliverData = (a, b) => Table.Deliver(this, a, b);
            deliverPing = () => Table.DeliverPing(this);

            this.factoryKey = factory;
            this.ProxyInstance = proxy;

            if (proxy != null)
                proxy.Bind(deliverPing, deliverData);
        }

        #region proxy serialisation
        [ProtoBeforeSerialization]
        private void OnSerialisation()
        {
            serialised = factory.Serialise(ProxyInstance);
        }

        [ProtoAfterDeserialization]
        private void OnDeserialisation()
        {
            ProxyInstance = factory.Deserialise(serialised, this);
            ProxyInstance.Bind(deliverPing, deliverData);
        }
        #endregion

        #region standard contact stuff
        public override TimeSpan Ping(Contact source, TimeSpan timeout)
        {
            return ProxyInstance.Ping(source, timeout);
        }

        public override void Send(Contact source, Guid consumerId, byte[] message, bool reliable = true, bool ordered = true, int channel = 1)
        {
            ProxyInstance.Send(source, consumerId, message, reliable, ordered, channel);
        }
        #endregion

        public override int GetHashCode()
        {
            return ProxyInstance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var proxyContact = obj as ProxyContact;
            if (proxyContact != null)
                return proxyContact.Identifier.Equals(Identifier) && proxyContact.NetworkId.Equals(NetworkId);

            if (obj.GetType().Equals(ProxyInstance.GetType()))
                return ProxyInstance.Equals(obj);

            return base.Equals(obj);
        }

        public override string ToString()
        {
            return "Proxy:" + ProxyInstance.ToString();
        }

        #region static factory registration
        private static ConcurrentDictionary<int, Factory> factories = new ConcurrentDictionary<int, Factory>();
        public static void RegisterFactory(Factory factory)
        {
            factories.AddOrUpdate(factory.Type, factory, (a, b) => factory);
        }
        #endregion

        #region proxy interfaces
        public interface Factory
        {
            int Type
            {
                get;
            }

            IProxy Deserialise(byte[] data, ProxyContact contact);

            byte[] Serialise(IProxy proxy);
        }

        public interface IProxy
        {
            TimeSpan Ping(Contact source, TimeSpan timeout);

            void Send(Contact source, Guid consumerId, byte[] message, bool reliable, bool ordered, int channel);

            void Bind(Action deliverPing, Action<Guid, byte[]> deliverData);
        }
        #endregion
    }
}
