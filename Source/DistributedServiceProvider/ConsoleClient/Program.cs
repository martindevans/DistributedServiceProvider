using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider;
using System.Threading;
using System.Net.Sockets;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using System.IO;
using Consumers;
using System.Reflection;
using System.Net;
using Consumers.DataStorage;
using System.Threading.Tasks;
using DistributedServiceProvider.MessageConsumers;
using ProtoBuf;
using LoggerMessages;

namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Enter IP address of the logging terminal");
            BeginLogging("localhost");

            Func<DistributedRoutingTable, Contact> contactFactory = drt =>
                {
                    return new LocalContact(drt);
                };

            Guid networkId = Guid.NewGuid();
            Guid taskId = Guid.NewGuid();
            Configuration config = new Configuration()
            {
                LookupTimeout = 2,
            };

            List<DistributedRoutingTable> tables = new List<DistributedRoutingTable>();

            Console.WriteLine("Creating network");
            while (tables.Count < 100)
            {
                Identifier512 id = Identifier512.NewIdentifier();
                tables.Add(new DistributedRoutingTable(id, contactFactory, networkId, config));
            }

            Console.WriteLine("Bootstrapping...");
            Random r = new Random();
            for (int i = 0; i < tables.Count; i++)
            {
                if (i % 100 == 0)
                    Console.WriteLine("Bootstrap " + i);

                tables[i].Bootstrap(
                    tables[r.Next(tables.Count)].LocalContact,
                    tables[r.Next(tables.Count)].LocalContact,
                    tables[r.Next(tables.Count)].LocalContact);
            }

            StreamWriter w = new StreamWriter(new BufferedStream(File.Create("Log with failures.csv")));

            Console.WriteLine("Doing initial lookups");
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Initial " + i);
                DoSomeLookups(tables, 100);
            }

            int dead = 0;
            int killStep = 500;
            int lookups = 1000;
            int totalLookups = 0;

            while (true)
            //for (int i = 0; i < tables.Count / 4 * 3; i += killStep)
            {
                for (int x = 0; x < killStep; x++)
                {
                    var c = (tables[r.Next(tables.Count)].LocalContact as LocalContact);
                    if (!c.IsDead)
                    {
                        c.IsDead = true;
                        dead++;
                        AddPeer(config, contactFactory, networkId, tables, r);
                    }
                }

                var counts = DoSomeLookups(tables, lookups);

                int max = counts.Max();
                int min = counts.Min();
                float avg = counts.Aggregate(0, (a, b) => a + b) / (float)counts.Count();
                totalLookups += lookups;
                string s = tables.Count + "," + dead + "," + min + "," + avg + "," + max + "," + totalLookups;
                w.WriteLine(s);
                Console.WriteLine(s);

                w.Flush();
            }
        }

        private static void AddPeer(Configuration config, Func<DistributedRoutingTable, Contact> contactFactory, Guid networkId, IList<DistributedRoutingTable> tables, Random r)
        {
            Identifier512 id = Identifier512.NewIdentifier();
            DistributedRoutingTable t = new DistributedRoutingTable(id, contactFactory, networkId, config);

            t.Bootstrap(
                tables[r.Next(tables.Count)].LocalContact,
                tables[r.Next(tables.Count)].LocalContact,
                tables[r.Next(tables.Count)].LocalContact);

            tables.Add(t);
        }

        private static IEnumerable<int> DoSomeLookups(List<DistributedRoutingTable> tables, int count)
        {
            List<int> counts = new List<int>();

            Random r = new Random();
            for (int i = 0; i < count; i++)
            {
                DistributedRoutingTable a = tables[r.Next(tables.Count)];
                DistributedRoutingTable b = tables[r.Next(tables.Count)];

                Action<int> iterationCount = (its) => { counts.Add(its); };

                if (a != b)
                {
                    GetClosestNodes.ClosestResults results = a.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(b.LocalIdentifier) as GetClosestNodes.ClosestResults;

                    counts.Add(results.Iterations);
                }
            }

            return counts;
        }

        private static void EndLogging()
        {
            //LoggerClient.End();
        }

        private static void BeginLogging(string loggerIp)
        {
            //try
            //{
            //    LoggerClient.Begin(loggerIp, false, true);
            //    Console.WriteLine("Logging connection established");
            //}
            //catch (SocketException)
            //{
            //    Console.WriteLine("Could not establish a logging connection");
            //}
        }
    }
}
