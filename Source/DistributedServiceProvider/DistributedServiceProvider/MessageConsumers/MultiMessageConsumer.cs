using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;

namespace DistributedServiceProvider.MessageConsumers
{
    public abstract class MultiMessageConsumer
        :MessageConsumer
    {
        private Dictionary<byte, Action<Contact, byte[]>> processors = new Dictionary<byte, Action<Contact, byte[]>>();

        public MultiMessageConsumer(Guid consumerId)
            :base(consumerId)
        {
        }

        protected void BindProcessors(Dictionary<byte, Action<Contact, byte[]>> processors)
        {
            foreach (var item in processors)
            {
                this.processors[item.Key] = item.Value;
            }
        }

        public override void Deliver(Contact source, byte[] message)
        {
            byte flag = message[0];
            Action<Contact, byte[]> processor = processors[flag];

            if (processor != null)
                processor(source, message.Skip(1).ToArray());
        }

        protected void Send(Contact destination, Guid consumerId, byte flag, byte[] data, bool reliable = true, bool ordered = true, int channel = 1)
        {
            byte[] packet = new byte[data.Length + 1];
            packet[0] = flag;
            Array.ConstrainedCopy(data, 0, packet, 1, data.Length);

            destination.Send(RoutingTable.LocalContact, consumerId, packet, reliable, ordered, channel);
        }
    }
}
