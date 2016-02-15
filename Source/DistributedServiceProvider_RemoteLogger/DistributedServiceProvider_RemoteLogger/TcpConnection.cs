using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConcurrentPipes.Distributed;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;

namespace DistributedServiceProvider_RemoteLogger
{
    public class TcpConnection : DistributionConnection, IDisposable
    {
        TcpClient client;
        NetworkStream stream;
        BinaryReader reader;
        BinaryWriter writer;

        public EndPoint Ip
        {
            get
            {
                return client.Client.RemoteEndPoint;
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (client)
                {
                    return client.Connected;
                }
            }
        }

        public TcpConnection(string hostname, int port)
            :this(new TcpClient(hostname, port))
        {
        }

        public TcpConnection(TcpClient client)
        {
            this.client = client;

            writer = new BinaryWriter(new BufferedStream(stream = client.GetStream()));
            reader = new BinaryReader(client.GetStream());
        }

        /// <summary>
        /// Receive some byte data from the other end
        /// </summary>
        /// <param name="target"></param>
        /// <param name="start"></param>
        /// <param name="maxlength"></param>
        /// <returns></returns>
        public override int Receive(byte[] target, int start, int maxlength)
        {
            lock (client)
            {
                if (stream.DataAvailable)
                {
                    int length = reader.ReadInt32();
                    if (length > maxlength)
                        throw new IndexOutOfRangeException("Not enough space to decode packet");

                    int read = 0;
                    while (read < length)
                    {
                        int r = reader.Read(target, start + read, length - read);
                        read += r;
                    }

                    return length;
                }
                else
                    return 0;
            }
        }

        private int biggestPacket;
        public int BiggestPacket
        {
            get
            {
                return biggestPacket;
            }
        }

        /// <summary>
        /// Send this data to the other end
        /// </summary>
        /// <param name="data"></param>
        /// <param name="option"></param>
        public override void Send(ArraySegment<byte> data, Transmission option)
        {
            lock (client)
            {
                try
                {
                    Interlocked.Exchange(ref biggestPacket, data.Count);
                    writer.Write(data.Count);
                    writer.Write(data.Array, data.Offset, data.Count);

                    writer.Flush();
                }
                catch (IOException)
                {
                    Console.Error.WriteLine("IO Exception in TCP connection");
                }
            }
        }

        public override void Flush()
        {
            lock (client)
            {
                writer.Flush();
                stream.Flush();
            }
        }

        public void Dispose()
        {
            Flush();

            reader.Dispose();
            writer.Dispose();

            client.Close();
        }
    }
}
