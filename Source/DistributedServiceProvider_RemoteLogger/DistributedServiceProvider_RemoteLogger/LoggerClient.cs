using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using ProtoBuf;

namespace DistributedServiceProvider_RemoteLogger
{
    public static class LoggerClient
    {
        private static object connectionLock = new object();
        private static DistributionConnection connection;

        /// <summary>
        /// Begins the specified hostname.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <exception cref="SocketException">Thrown if a connection to the remote logger could not be established</exception>
        public static void Begin(string hostname, bool justDebug, bool toFile=false)
        {
            lock (connectionLock)
            {
                if (connection != null)
                    throw new InvalidOperationException("Cannot create a new connection when one already exists");

                connection = new TcpConnection(hostname, Setup.PORT);
                DistributedPipes.RegisterConnection(connection);
            }

            Setup.SetUpEncoders(justDebug, toFile);
        }

        public static void End()
        {
            lock (connectionLock)
            {
                if (connection != null)
                {
                    DistributedPipes.WaitForSendMessages();
                    DistributedPipes.UnregisterConnection(connection);
                    connection = null;
                }
            }
        }
    }
}
