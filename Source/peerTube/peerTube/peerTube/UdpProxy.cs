using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;
using System.IO;
using DistributedServiceProvider.Base;
using System.Diagnostics;

namespace peerTube
{
    [ProtoContract]
    public class UdpProxy
        : ProxyContact.IProxy
    {
        [ProtoMember(1)]
        public int Port
        {
            get;
            private set;
        }

        [ProtoMember(2)]
        byte[] addressBytes;

        public IPEndPoint EndPoint
        {
            get
            {
                return new IPEndPoint(new IPAddress(addressBytes), Port);
            }
            set
            {
                Port = value.Port;
                addressBytes = value.Address.GetAddressBytes();
            }
        }

        internal UdpProxy()
        {

        }

        public TimeSpan Ping(Contact source, TimeSpan timeout)
        {
            ProxyContact proxySource = (ProxyContact)source;

            MemoryStream m = new MemoryStream();
            Serializer.SerializeWithLengthPrefix<Contact>(m, source, PrefixStyle.Base128);

            m.WriteByte(0);

            var waitToken = Game1.UdpFactory.SendPing(proxySource, this, true);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            if (waitToken.Wait((int)timeout.TotalMilliseconds))
                return timer.Elapsed;

            return TimeSpan.MaxValue;
        }

        public void Send(Contact source, Guid consumerId, byte[] message, bool reliable, bool ordered, int channel)
        {
            MemoryStream m = new MemoryStream();
            Serializer.SerializeWithLengthPrefix<Contact>(m, source, PrefixStyle.Base128);

            m.WriteByte(1);

            m.Write(consumerId.ToByteArray(), 0, 16);

            m.Write(BitConverter.GetBytes(message.Length), 0, 4);
            m.Write(message, 0, message.Length);

            try
            {
                Game1.UdpFactory.Send(m.ToArray(), EndPoint);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
        }

        public void Bind(Action deliverPing, Action<Guid, byte[]> deliverData)
        {
            
        }

        public override bool Equals(object obj)
        {
            UdpProxy other = obj as UdpProxy;
            if (other == null)
                return false;

            return other.EndPoint.Equals(EndPoint);
        }

        public override int GetHashCode()
        {
            return EndPoint.GetHashCode();
        }

        public override string ToString()
        {
            return EndPoint.ToString();
        }
    }
}
