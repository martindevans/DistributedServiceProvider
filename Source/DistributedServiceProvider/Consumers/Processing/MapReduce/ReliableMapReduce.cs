using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Consumers.Processing.MapReduce
{
    /// <summary>
    /// An implementation of map reduce which is designed to work on reliable networks where nodes do not drop out or send malicious data
    /// </summary>
    /// <typeparam name="InKey">The type of key passed into Map</typeparam>
    /// <typeparam name="InData">The type of data passed into Map</typeparam>
    /// <typeparam name="OutKey">The type out key output from Map and input to reduce</typeparam>
    /// <typeparam name="MidData">The type of data output from Map and input to reduce</typeparam>
    /// <typeparam name="OutData">The type of data output from reduce</typeparam>
    public abstract class ReliableMapReduce<InKey, InData, OutKey, MidData, OutData>
        :MessageConsumer
    {
        #region fields
        private DistributedRoutingTable table;

        [LinkedConsumer(Callback.GUID_STRING)]
        public Callback callback;
        #endregion

        #region construction/setup
        public ReliableMapReduce(Guid taskId)
            :base(taskId)
        {
            Serializer.PrepareSerializer<InKeyContainer>();
            Serializer.PrepareSerializer<OutKeyContainer>();
            Serializer.PrepareSerializer<InDataContainer>();
            Serializer.PrepareSerializer<OutDataContainer>();
            Serializer.PrepareSerializer<MidDataContainer>();
            Serializer.PrepareSerializer<OutDataContainer>();
            Serializer.PrepareSerializer<RunMapOnKey>();
            Serializer.PrepareSerializer<EmitIntermediatePacket>();
            Serializer.PrepareSerializer<MapStageComplete>();
            Serializer.PrepareSerializer<ReduceResult>();
        }

        protected override void OnRegisteredToTable(DistributedRoutingTable table)
        {
            this.table = table;
            base.OnRegisteredToTable(table);
        }
        #endregion

        #region abstract things
        /// <summary>
        /// Given a piece of input data, output a set of output data points
        /// </summary>
        /// <param name="key">The key of the input data</param>
        /// <param name="data">The data to be processed</param>
        /// <returns></returns>
        protected abstract IEnumerable<KeyValuePair<OutKey, MidData>> Map(InKey key, InData data);

        /// <summary>
        /// Give all the datapoints with a given key, generate a single output value
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dataPoints">The data points.</param>
        /// <returns>the reduce output value of all input data for this key</returns>
        protected abstract OutData Reduce(OutKey key, IEnumerable<MidData> dataPoints);

        /// <summary>
        /// Called on each reducer peer. This will send the output to the root peer so that it appears in the output dictionary. Override if you require alternative handling out outputs
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        protected virtual void StoreOutput(OutKey key, OutData data, Contact rootPeer)
        {
            using (MemoryStream m = new MemoryStream())
            {
                m.WriteByte((byte)PacketFlag.ReduceResult);
                Serializer.SerializeWithLengthPrefix<ReduceResult>(m, new ReduceResult()
                {
                    Key = new OutKeyContainer() { Key = key },
                    Data = new OutDataContainer() { Data = data },
                }, PrefixStyle.Base128);

                rootPeer.Send(table.LocalContact, ConsumerId, m.ToArray());
            }
        }

        /// <summary>
        /// Enumerate all the keys for the input data
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<InKey> GenerateKeys();

        /// <summary>
        /// Fetches the data assosciated with this key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected abstract InData FetchData(InKey key);

        /// <summary>
        /// Transforms an input key into an identifier
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected abstract Identifier512 TransformInputKey(InKey key);

        /// <summary>
        /// Transform an output key into an identifier
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected abstract Identifier512 TransformOutputKey(OutKey key);
        #endregion

        protected IDictionary<OutKey, OutData> RunTask()
        {
            var callbacks = new List<KeyValuePair<InKey, Callback.WaitToken>>();

            HashSet<Contact> mappers = new HashSet<Contact>();

            foreach (var inputKey in GenerateKeys())
                callbacks.Add(new KeyValuePair<InKey, Callback.WaitToken>(inputKey, RemoteIssueMap(inputKey, mappers)));

            //keep retrying until all "Map" stages have run and successfully made a callback
            for (int i = callbacks.Count - 1; i >= 0; i--)
            {
                if (callbacks[i].Value.Wait(-1))
                {
                    callback.FreeToken(callbacks[i].Value);
                    callbacks.RemoveAt(i);
                }
                else
                {
                    throw new NotImplementedException("This mapper died before making a callback");
                }
            }

            if (callbacks.Count != 0)
                throw new NotImplementedException("Not all callbacks have been handled");

            //all map stages have run
            //tell all the mappers that all mappers have run, they will all wait on their assosciated reducers and return once done
            List<Callback.WaitToken> tokens = new List<Callback.WaitToken>();
            foreach (var mapper in mappers)
            {
                using (MemoryStream m = new MemoryStream())
                {
                    Callback.WaitToken token = callback.AllocateToken();
                    tokens.Add(token);

                    m.WriteByte((byte)PacketFlag.CoordinatorToMap_MapStageFinished);
                    Serializer.SerializeWithLengthPrefix<MapStageComplete>(m, new MapStageComplete()
                    {
                        CallbackId = token.Id,
                        RootTaskPeer = table.LocalIdentifier
                    }, PrefixStyle.Base128);

                    mapper.Send(table.LocalContact, ConsumerId, m.ToArray());
                }
            }

            //Wait for all the mappers to call back that all of their reducers have finished
            foreach (var token in tokens)
            {
                token.Wait(-1);
                callback.FreeToken(token);
            }

            //at this point, all reducers have sent their final values back here, now we just need to return them
            return reduceResults;
        }

        public override void Deliver(Contact source, byte[] message)
        {
            PacketFlag flag = (PacketFlag)message[0];
            using (MemoryStream mStream = new MemoryStream(message, 1, message.Length - 1, false))
            {
                switch (flag)
                {
                    case PacketFlag.RunMapOnKey:
                        LocalIssueMap(source, Serializer.DeserializeWithLengthPrefix<RunMapOnKey>(mStream, PrefixStyle.Base128));
                        break;
                    case PacketFlag.EmitIntermediatePacket:
                        LocalReceiveEmit(source, Serializer.DeserializeWithLengthPrefix<EmitIntermediatePacket>(mStream, PrefixStyle.Base128));
                        break;
                    case PacketFlag.CoordinatorToMap_MapStageFinished:
                        LocalHandleMapsFinished(source, Serializer.DeserializeWithLengthPrefix<MapStageComplete>(mStream, PrefixStyle.Base128));
                        break;
                    case PacketFlag.MapToReducer_MapStageFinished:
                        TryRunReducer(source, Serializer.DeserializeWithLengthPrefix<MapStageComplete>(mStream, PrefixStyle.Base128));
                        break;
                    case PacketFlag.ReduceResult:
                        AddReduceResult(source, Serializer.DeserializeWithLengthPrefix<ReduceResult>(mStream, PrefixStyle.Base128));
                        break;
                    default:
                        throw new ArgumentException("Unknown packet flag");
                }
            }
        }

        private ConcurrentDictionary<OutKey, OutData> reduceResults = new ConcurrentDictionary<OutKey, OutData>();
        private void AddReduceResult(Contact source, ReduceResult reduceResult)
        {
            bool alreadyContained = true;

            reduceResults.GetOrAdd(reduceResult.Key.Key, (p) =>
                {
                    alreadyContained = false;
                    return reduceResult.Data.Data;
                });

            if (alreadyContained)
                throw new ArgumentException("Received two results for one reducer key");
        }

        private HashSet<Contact> myReducers = new HashSet<Contact>();
        private void LocalHandleMapsFinished(Contact source, MapStageComplete packet)
        {
            List<Callback.WaitToken> tokens = new List<Callback.WaitToken>();

            foreach (var reducer in myReducers)
            {
                Callback.WaitToken t = callback.AllocateToken();
                tokens.Add(t);

                using(MemoryStream m = new MemoryStream())
                {
                    m.WriteByte((byte)PacketFlag.MapToReducer_MapStageFinished);
                    Serializer.SerializeWithLengthPrefix<MapStageComplete>(m, new MapStageComplete()
                    {
                        CallbackId = t.Id,
                        RootTaskPeer = packet.RootTaskPeer,
                    }, PrefixStyle.Base128);

                    reducer.Send(table.LocalContact, ConsumerId, m.ToArray());
                }
            }

            foreach (var t in tokens)
            {
                t.Wait(-1);
                callback.FreeToken(t);
            }

            callback.SendResponse(table.LocalContact, source, packet.CallbackId, new byte[] { 1 });
        }

        private ConcurrentDictionary<OutKey, ConcurrentBag<MidData>> emitsReceivedForReduction = new ConcurrentDictionary<OutKey, ConcurrentBag<MidData>>();
        private void LocalReceiveEmit(Contact source, EmitIntermediatePacket packet)
        {
            ConcurrentBag < MidData > l = emitsReceivedForReduction.GetOrAdd(packet.Key.Key, (p) => new ConcurrentBag<MidData>());
             
            l.Add(packet.Data.Data);
        }

        private bool reduceDone = false;
        private void TryRunReducer(Contact source, MapStageComplete mapStageComplete)
        {
            if (!reduceDone)
            {
                reduceDone = true;

                foreach (var item in emitsReceivedForReduction)
                {
                    OutData result = Reduce(item.Key, item.Value);
                    StoreOutput(item.Key, result, table.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(mapStageComplete.RootTaskPeer).First());
                }
            }

            //send callback to the mapper which sent this message
            callback.SendResponse(table.LocalContact, source, mapStageComplete.CallbackId, new byte[] { 1 });
        }

        private void LocalIssueMap(Contact source, RunMapOnKey rmok)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            {
                //run this map, and emit intermediate results to reducers
                Parallel.ForEach(Map(rmok.Key.Key, FetchData(rmok.Key.Key)), (item) =>
                //foreach (var item in Map(rmok.Key.Key, FetchData(rmok.Key.Key)))
                {
                    EmitIntermediate(TransformOutputKey(item.Key).GetHashedKey(), item.Key, item.Value);
                }
                );
            }
            timer.Stop();

            callback.SendResponse(table.LocalContact, source, rmok.WaitTokenHandle, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(timer.ElapsedMilliseconds)));
        }

        private void EmitIntermediate(Identifier512 reducerId, OutKey key, MidData data)
        {
            var closest = table.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(reducerId).First();
            lock (myReducers)
            {
                myReducers.Add(closest);
            }

            using (MemoryStream m = new MemoryStream())
            {
                m.WriteByte((byte)PacketFlag.EmitIntermediatePacket);

                Serializer.SerializeWithLengthPrefix<EmitIntermediatePacket>(m, new EmitIntermediatePacket()
                    {
                        Key = new OutKeyContainer() { Key = key },
                        Data = new MidDataContainer() { Data = data },
                    }
                    , PrefixStyle.Base128
                );

                closest.Send(table.LocalContact, ConsumerId, m.ToArray());
            }
        }

        private Callback.WaitToken RemoteIssueMap(InKey key, HashSet<Contact> mappers)
        {
            Callback.WaitToken wait = callback.AllocateToken();

            Identifier512 k = TransformInputKey(key).GetHashedKey();

            using(MemoryStream m = new MemoryStream())
            {
                m.WriteByte((byte)PacketFlag.RunMapOnKey);

                Serializer.SerializeWithLengthPrefix<RunMapOnKey>(m,
                    new RunMapOnKey()
                    {
                        Key = new InKeyContainer() { Key = key },
                        WaitTokenHandle = wait.Id
                    }
                    , PrefixStyle.Base128
                );

                var closest = table.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(k).First();
                closest.Send(table.LocalContact, ConsumerId, m.ToArray());
                mappers.Add(closest);
            }

            return wait;
        }

        #region containers
        [ProtoContract]
        private class InKeyContainer
        {
            [ProtoMember(1)]
            public InKey Key
            {
                get;
                set;
            }
        }

        [ProtoContract]
        private class InDataContainer
        {
            [ProtoMember(1)]
            public InData Key
            {
                get;
                set;
            }
        }

        [ProtoContract]
        private class OutKeyContainer
        {
            [ProtoMember(1)]
            public OutKey Key
            {
                get;
                set;
            }
        }

        [ProtoContract]
        private class MidDataContainer
        {
            [ProtoMember(1)]
            public MidData Data
            {
                get;
                set;
            }
        }

        [ProtoContract]
        private class OutDataContainer
        {
            [ProtoMember(1)]
            public OutData Data
            {
                get;
                set;
            }
        }
        #endregion

        #region packets
        private enum PacketFlag
            :byte
        {
            RunMapOnKey,
            EmitIntermediatePacket,
            CoordinatorToMap_MapStageFinished,
            MapToReducer_MapStageFinished,
            ReduceResult,
        }

        [ProtoContract]
        private class RunMapOnKey
        {
            [ProtoMember(1)]
            public InKeyContainer Key
            {
                get;
                set;
            }

            [ProtoMember(2)]
            public long WaitTokenHandle
            {
                get;
                set;
            }
        }

        [ProtoContract]
        private class EmitIntermediatePacket
        {
            [ProtoMember(1)]
            public OutKeyContainer Key;

            [ProtoMember(2)]
            public MidDataContainer Data;
        }

        [ProtoContract]
        private class MapStageComplete
        {
            [ProtoMember(1)]
            public long CallbackId;

            [ProtoMember(2)]
            public Identifier512 RootTaskPeer;
        }

        [ProtoContract]
        private class ReduceResult
        {
            [ProtoMember(1)]
            public OutKeyContainer Key;

            [ProtoMember(2)]
            public OutDataContainer Data;
        }
        #endregion
    }
}
