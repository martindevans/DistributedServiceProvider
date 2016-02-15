using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using System.IO;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.MessageConsumers;
using System.Threading;

namespace peerTube
{
    public class UdpFactory
        : ProxyContact.Factory
    {
        #region serialisation
        private Dictionary<IPEndPoint, KeyValuePair<ProxyContact, UdpProxy>> proxies = new Dictionary<IPEndPoint, KeyValuePair<ProxyContact, UdpProxy>>();

        public int Type
        {
            get { return "Udp".GetHashCode(); }
        }

        public ProxyContact.IProxy Deserialise(byte[] data, ProxyContact contact)
        {
            var d = Serializer.Deserialize<UdpProxy>(new MemoryStream(data));

            proxies[d.EndPoint] = new KeyValuePair<ProxyContact, UdpProxy>(contact, d);

            return d;
        }

        public ProxyContact Construct(IPEndPoint iPEndPoint, Guid networkId, Identifier512 remoteId)
        {
            lock (proxies)
            {
                if (!proxies.ContainsKey(iPEndPoint))
                {
                    var proxy = new UdpProxy() { EndPoint = iPEndPoint };
                    var kvp = new KeyValuePair<ProxyContact, UdpProxy>(new ProxyContact(this.Type, proxy), proxy);
                    proxies[iPEndPoint] = kvp;

                    kvp.Key.NetworkId = networkId;
                    kvp.Key.Identifier = remoteId;
                }

                return proxies[iPEndPoint].Key;
            }
        }

        public byte[] Serialise(ProxyContact.IProxy proxy)
        {
            MemoryStream m = new MemoryStream();
            Serializer.Serialize<UdpProxy>(m, (UdpProxy)proxy);

            return m.ToArray();
        }
        #endregion

        #region communication
        Callback callback;
        DistributedRoutingTable table;
        volatile bool continueEating = true;
        UdpClient udpClient;
        public readonly int ListenPort;

        public UdpFactory(int port)
        {
            ListenPort = port;
            udpClient = new UdpClient(port);
            udpClient.DontFragment = false;
            udpClient.AllowNatTraversal(true);
        }

        public void Begin(DistributedRoutingTable table)
        {
            this.table = table;
            callback = table.GetConsumer<Callback>(Callback.CONSUMER_ID);

            BeginReceive(CreateEatPacket(table));
        }

        public void Close()
        {
            continueEating = false;
            udpClient.Close();
        }

        private void BeginReceive(Action<IAsyncResult> eatPacket)
        {
            bool success = false;
            while (continueEating && !success)
            {
                try
                {
                    udpClient.BeginReceive(a =>
                    {
                        eatPacket(a);

                        BeginReceive(eatPacket);
                    }, null);

                    success = true;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                        Thread.Sleep(100);
                    else
                        Console.WriteLine(e);
                }
            }
        }

        private Action<IAsyncResult> CreateEatPacket(DistributedRoutingTable routingTable)
        {
            return a =>
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, ListenPort);

                    var m = new MemoryStream(udpClient.EndReceive(a, ref ep));

                    ProxyContact source = Serializer.DeserializeWithLengthPrefix<ProxyContact>(m, PrefixStyle.Base128);

                    if (m.ReadByte() == 0)
                    {
                        routingTable.DeliverPing(source);

                        BinaryReader r = new BinaryReader(m);

                        callback.SendResponse(routingTable.LocalContact, source, r.ReadInt64(), new byte[] { 1 });
                    }
                    else
                    {
                        try
                        {
                            BinaryReader r = new BinaryReader(m);

                            Guid consumer = new Guid(r.ReadBytes(16));

                            byte[] buffer = new byte[BitConverter.ToInt32(r.ReadBytes(4), 0)];
                            m.Read(buffer, 0, buffer.Length);

                            ThreadPool.QueueUserWorkItem(_ => routingTable.Deliver(source, consumer, buffer));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            };
        }

        public void Send(byte[] dgram, IPEndPoint endpoint, bool synchronous=true)
        {
            Action send = 
            () =>
            {
                lock (udpClient)
                {
                    udpClient.Send(dgram, dgram.Length, endpoint);
                }
            };

            if (synchronous)
                send();
            else
                ThreadPool.QueueUserWorkItem(a => send());
        }

        public Callback.WaitToken SendPing(ProxyContact source, UdpProxy destination, bool synchronous=true)
        {
            var t = callback.AllocateToken();

            MemoryStream m = new MemoryStream();
            Serializer.SerializeWithLengthPrefix<ProxyContact>(m, source, PrefixStyle.Base128);

            m.WriteByte(0);

            BinaryWriter writer = new BinaryWriter(m);
            writer.Write(t.Id);

            Send(m.ToArray(), destination.EndPoint, synchronous);

            return t;
        }

        public void PingResponse(Contact source, Callback.WaitToken token)
        {
            callback.FreeToken(token);
            table.DeliverPing(source);
        }
        #endregion
    }
}
