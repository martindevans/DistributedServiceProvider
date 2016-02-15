using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using System.IO;
using ProtoBuf;
using System.Collections.Concurrent;

namespace Consumers.DataStorage
{
    public class KeyValuePairStore
        : MessageConsumer, IDataStore
    {
        #region fields
        ConcurrentDictionary<Identifier512, byte[]> localData = new ConcurrentDictionary<Identifier512, byte[]>();

        [LinkedConsumer(Callback.GUID_STRING)]
        public Callback Callback;
        #endregion

        public KeyValuePairStore(Guid consumerId)
            :base(consumerId)
        {
            Serializer.PrepareSerializer<PutRequest>();
            Serializer.PrepareSerializer<GetRequest>();
        }

        public void Put(Identifier512 key, byte[] data)
        {
            var closest = RoutingTable.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(key).First();

            using(MemoryStream mStream = new MemoryStream())
            {
                mStream.WriteByte((byte)PacketFlag.PutRequest);

                Serializer.SerializeWithLengthPrefix<PutRequest>(mStream, new PutRequest(key, data), PrefixStyle.Base128);

                closest.Send(RoutingTable.LocalContact, ConsumerId, mStream.ToArray());
            }
        }

        public void Delete(Identifier512 key)
        {
            Put(key, null);
        }

        public byte[] Get(Identifier512 key, int timeout)
        {
            byte[] localData = GetData(key);
            if (localData != null)
                return localData;

            var token = Callback.AllocateToken();

            GetRequest request = new GetRequest(key, token.Id);

            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.WriteByte((byte)PacketFlag.GetRequest);
                Serializer.SerializeWithLengthPrefix<GetRequest>(mStream, request, PrefixStyle.Base128);

                var remote = RoutingTable.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(key).First();
                remote.Send(RoutingTable.LocalContact, ConsumerId, mStream.ToArray());
            }

            if (!token.Wait(timeout))
                throw new TimeoutException();

            return token.Response;
        }

        public override void Deliver(Contact source, byte[] message)
        {
            using (MemoryStream mStream = new MemoryStream(message))
            {
                switch ((PacketFlag)mStream.ReadByte())
                {
                    case PacketFlag.PutRequest:
                        {
                            lock (localData)
                            {
                                PutRequest r = Serializer.DeserializeWithLengthPrefix<PutRequest>(mStream, PrefixStyle.Base128);
                                if (r.Data == null)
                                {
                                    byte[] b;
                                    localData.TryRemove(r.Key, out b);
                                }
                                else
                                    localData[r.Key] = r.Data;
                                break;
                            }
                        }
                    case PacketFlag.GetRequest:
                        {
                            GetRequest r = Serializer.DeserializeWithLengthPrefix<GetRequest>(mStream, PrefixStyle.Base128);

                            Callback.SendResponse(RoutingTable.LocalContact, source, r.TokenId, GetData(r.Key));

                            break;
                        }
                    default:
                        break;
                }
            }
        }

        private byte[] GetData(Identifier512 key)
        {
            lock (localData)
            {
                byte[] b = null;
                localData.TryGetValue(key, out b);

                return b;
            }
        }

        #region packets
        private enum PacketFlag
            :byte
        {
            PutRequest,
            GetRequest,
        }

        [ProtoContract]
        private class PutRequest
        {
            [ProtoMember(1)]
            public Identifier512 Key;

            [ProtoMember(2)]
            public byte[] Data;

            public PutRequest(Identifier512 key, byte[] data)
            {
                Key = key;
                Data = data;
            }

            public PutRequest()
            {

            }
        }

        [ProtoContract]
        private class GetRequest
        {
            [ProtoMember(1)]
            public Identifier512 Key;

            [ProtoMember(2)]
            public long TokenId;

            public GetRequest(Identifier512 key, long tokenId)
            {
                Key = key;
                TokenId = tokenId;
            }

            public GetRequest()
            {

            }
        }
        #endregion
    }
}
