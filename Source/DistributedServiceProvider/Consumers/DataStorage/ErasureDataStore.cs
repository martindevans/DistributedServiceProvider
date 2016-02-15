using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using DigitalFountain;
using System.IO;
using System.Security.Cryptography;
using ProtoBuf;
using System.Threading.Tasks;
using System.Threading;
using DistributedServiceProvider.Base.Extensions;

namespace Consumers.DataStorage
{
    public class ErasureDataStore
        : MessageConsumer, IDataStore
    {
        IDataStore basicStore;

        public ErasureDataStore(Guid consumerId, IDataStore baseStore)
            :base(consumerId)
        {
            basicStore = baseStore;

            Serializer.PrepareSerializer<PacketChunk>();
        }

        /// <summary>
        /// Puts the specified data indeed by the specified key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <param name="blocks">The number of blocks to split the data into</param>
        /// <param name="multiples">The number of times more blocks than data there should be stored in the network</param>
        public void Put(Identifier512 key, byte[] data, int blockSize, float additionalMultiples)
        {
            Identifier512 rootKey = (Identifier512)key.Clone();

            Fountain f = new Fountain(DateTime.Now.Millisecond, data, blockSize);

            //while (!Parallel.For(0, (int)(f.BlockCount * additionalMultiples + 1), (i) =>
            for (int i = 0; i < (int)(f.BlockCount * additionalMultiples + 1); i++)
            {
                Packet p;
                lock (f) { p = f.CreatePacket(); }

                basicStore.Put(CalculateKeyForIndex(key, i), PacketToBinary(p, rootKey, f.BlockSize, f.BlockCount));
            }
            //).IsCompleted) { Thread.Sleep(10); }
        }

        public void Put(Identifier512 key, byte[] data)
        {
            Put(key, data, 1, 2);
        }

        private Identifier512 CalculateKeyForIndex(Identifier512 rootKey, int index)
        {
            return (rootKey + index).GetHashedKey();
        }

        private byte[] PacketToBinary(Packet p, Identifier512 rootKey, int blockSize, int blockCount)
        {
            using (MemoryStream m = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix<PacketChunk>(m, new PacketChunk(rootKey, p, blockSize, blockCount), PrefixStyle.Base128);

                return m.ToArray();
            }
        }

        public byte[] Get(Identifier512 key, int timeout)
        {
            HashSet<Identifier512> missingChunks = new HashSet<Identifier512>();

            Bucket bucket = GetData(key, timeout, missingChunks);

            return RegenerateData(key, bucket, missingChunks);
        }

        private byte[] RegenerateData(Identifier512 key, Bucket bucket, HashSet<Identifier512> missingChunks)
        {
            if (bucket.IsComplete)
            {
                byte[] data = bucket.GetData();

                Fountain f = new Fountain(DateTime.Now.Millisecond, data, bucket.BlockSize);
                foreach (var missing in missingChunks)
                    basicStore.Put(missing, PacketToBinary(f.CreatePacket(), key, f.BlockSize, f.BlockCount));

                return data;
            }
            else
                throw new IncompleteDownloadException("Could not complete download");
        }

        private Bucket GetData(Identifier512 key, int timeout, HashSet<Identifier512> missingChunks)
        {
            Bucket bucket = null;

            foreach (var chunk in FetchChunks(key, timeout, missingChunks, 100))
            {
                if (bucket == null)
                    bucket = new Bucket(chunk.BlockSize, chunk.BlockCount);

                if (bucket.AddPacket(new Packet(chunk.packetSeed, chunk.packetData)))
                    break;
            }
            return bucket;
        }

        private IEnumerable<PacketChunk> FetchChunks(Identifier512 rootKey, int timeout, ICollection<Identifier512> missingChunks, int maxConsecutiveFailures)
        {
            int i = 0;

            int consecutiveFailures = 0;

            do
            {
                using (MemoryStream m = new MemoryStream())
                {
                    PacketChunk chunk = null;
                    Identifier512 key = CalculateKeyForIndex(rootKey, i);
                    i++;

                    try
                    {
                        var c = basicStore.Get(key, timeout);

                        m.Write(c, 0, c.Length);
                        m.Position = 0;

                        chunk = Serializer.DeserializeWithLengthPrefix<PacketChunk>(m, PrefixStyle.Base128);

                        if (chunk.rootKey != rootKey)
                            break;
                    }
                    catch (TimeoutException)
                    {
                        if (missingChunks != null)
                            missingChunks.Add(key);

                        Console.WriteLine("Timeout getting block " + rootKey + " + " + (i - 1));
                    }

                    if (chunk != null)
                        yield return chunk;
                    else
                        consecutiveFailures++;
                }
            } while (consecutiveFailures <= maxConsecutiveFailures);

            if (consecutiveFailures > maxConsecutiveFailures)
                throw new IncompleteDownloadException("Suffered too many consecutive failures");
        }

        public override void Deliver(Contact source, byte[] message)
        {
            throw new InvalidOperationException();
        }

        [ProtoContract]
        private class PacketChunk
        {
            [ProtoMember(1)]
            public int packetSeed;

            [ProtoMember(2)]
            public byte[] packetData;

            [ProtoMember(3)]
            public Identifier512 rootKey;

            [ProtoMember(4)]
            public int BlockSize;

            [ProtoMember(5)]
            public int BlockCount;

            public PacketChunk(Identifier512 rootKey, Packet packet, int blockSize, int blockCount)
            {
                this.rootKey = rootKey;
                this.packetSeed = packet.PacketSeed;
                this.packetData = packet.Data;
                BlockSize = blockSize;
                BlockCount = blockCount;
            }

            public PacketChunk()
            {

            }
        }

        public class IncompleteDownloadException
            :Exception
        {
            public IncompleteDownloadException(string msg)
                :base(msg)
            {

            }
        }
    }
}
