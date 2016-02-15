using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ConcurrentPipes.Distributed;

namespace DistributedServiceProvider_RemoteLogger
{
    public static class LoggerServer
    {
        private static HashSet<TcpConnection> connections = new HashSet<TcpConnection>();
        private static Thread acceptThread;

        private static TcpListener listener;

        public static void Begin()
        {
            Setup.SetUpEncoders(false, false);

            listener = new TcpListener(IPAddress.Any, Setup.PORT);
            listener.Start();

            acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            acceptThread.Start();
        }

        static bool accept = true;
        private static void AcceptLoop()
        {
            while (accept)
            {
                var asyncResult = listener.BeginAcceptTcpClient((a) => { }, null);

                while (!asyncResult.IsCompleted)
                    if (!accept)
                        return;

                lock (connections)
                {
                    var c = new TcpConnection(listener.EndAcceptTcpClient(asyncResult));
                    DistributedPipes.RegisterConnection(c);

                    Console.WriteLine("New connection from " + c.Ip);

                    connections.Add(c);
                    foreach (var conn in connections.Where(a => !a.IsConnected))
                    {
                        connections.Remove(conn);
                        DistributedPipes.UnregisterConnection(conn);
                    }
                }
            }

            Console.WriteLine("Dead");
        }

        public static void End()
        {
            accept = false;
            acceptThread.Join();

            lock (connections)
            {
                foreach (var connection in connections)
                    DistributedPipes.UnregisterConnection(connection);
            }
        }
    }
}
