using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider;
using Consumers.Processing.MapReduce.Samples;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.MessageConsumers;
using Consumers.DataStorage;
using System.IO;
using System.Net.Sockets;
using ProtoBuf;
using LoggerMessages;
using System.Security.Cryptography;
using System.Threading;

namespace DEMO_MapReduce
{
    class Program
    {
        static void Main(string[] args)
        {
            HandleInput("help");

            while (true)
                if (HandleInput(Console.ReadLine()))
                    break;
        }

        private static bool HandleInput(string input)
        {
            switch (input.ToLower())
            {
                case "help":
                    Console.WriteLine("Type:");
                    Console.WriteLine("\t\"help\" for this helpful message");
                    Console.WriteLine("\t\"sort\" to demonstrate sorting 5000 random integers");
                    Console.WriteLine("\t\"wordcount\" to demonstrate distributed word occurence counting");
                    Console.WriteLine("\t\"datastore\" to interactively put and get data from a datastore");
                    Console.WriteLine("\t\"playback\" to begine playing back a prerecorded logfile");
                    Console.WriteLine("\t\"quit\" to quit");
                    break;
                case "sort":
                    var n = CreateNetwork(100);
                    Sort(n);
                    break;
                case "wordcount":
                    var n2 = CreateNetwork(100);
                    WordCount(n2);
                    break;
                case "datastore":
                    //BeginLogging("localhost");
                    var n3 = CreateNetwork(100);
                    DataStore(n3);
                    Console.WriteLine("\tExited datastore mode");
                    //LoggerClient.End();
                    break;
                case "playback":
                    LogfilePlayback();
                    Console.WriteLine("\tExited playback mode");
                    break;
                case "quit":
                    return true;
                default:
                    Console.WriteLine("Unknown command " + input);
                    break;
            }

            return false;
        }

        private static void RegisterConsumerOnNetwork<T>(IEnumerable<DistributedRoutingTable> network, Func<DistributedRoutingTable, T> factory) where T : MessageConsumer
        {
            foreach (var n in network)
                n.RegisterConsumer(factory(n));
        }

        private static List<DistributedRoutingTable> CreateNetwork(int count)
        {
            Console.WriteLine("\tCreating a virtual network (" + count + " nodes)");

            Func<DistributedRoutingTable, Contact> contactFactory = drt => new LocalContact(drt);

            Guid networkId = Guid.NewGuid();
            Configuration config = new Configuration();

            var tables = new List<DistributedRoutingTable>();

            for (int i = 0; i < count; i++)
                tables.Add(new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config));

            for (int i = 0; i < tables.Count; i++)
                tables[i].Bootstrap(tables.Where(a => a != tables[i]).Select(a => a.LocalContact));

            return tables;
        }

        #region map/reduce
        private static void Sort(List<DistributedRoutingTable> network)
        {
            Console.WriteLine("\tCreating Data");
            List<int> data = new List<int>();
            for (int i = 0; i < 5000; i++)
                data.Add(i * 500000);

            Console.WriteLine("\tShuffling Data");
            Random r = new Random();
            for (int i = 0; i < 100000; i++)
            {
                int index = r.Next(data.Count);
                int tmp = data[index];
                data.RemoveAt(index);
                data.Insert(r.Next(data.Count), tmp);
            }

            Console.WriteLine("\tSetting up sort operation");
            Guid taskId = Guid.NewGuid();
            RegisterConsumerOnNetwork(network, d => new Sort(taskId, data.ToArray(), 100));

            Console.WriteLine("\tSorting");
            var sorted = network[0].GetConsumer<Sort>(taskId).Run().ToArray();

            for (int i = 0; i < sorted.Length; i++)
            {
                if (i > 0 && sorted[i] < sorted[i - 1])
                {
                    Console.WriteLine("Incorrect Sort!");
                    break;
                }
                Console.WriteLine(sorted[i]);
            }
        }

        private static void WordCount(List<DistributedRoutingTable> network)
        {
            Guid storeId = Guid.NewGuid();
            RegisterConsumerOnNetwork(network, d => new KeyValuePairStore(storeId));

            Guid taskId = Guid.NewGuid();
            RegisterConsumerOnNetwork(network, d => new WordCount(taskId, d.GetConsumer<KeyValuePairStore>(storeId)));

            IDictionary<string, int> result;

            Console.WriteLine("Use default string? y/n");
            if (Console.ReadLine().ToLower() == "y")
            {
                var defaultString = "A distributed hash table (DHT) is a class of a decentralized distributed system that provides a lookup service similar to a hash table; " + 
                    "(key, value) pairs are stored in a DHT, and any participating node can efficiently retrieve the value associated with a given key. Responsibility for " + 
                    "maintaining the mapping from keys to values is distributed among the nodes, in such a way that a change in the set of participants causes a minimal amount" +
                    "of disruption. This allows a DHT to scale to extremely large numbers of nodes and to handle continual node arrivals, departures, and failures." + 
                    "DHTs form an infrastructure that can be used to build more complex services, such as anycast, cooperative Web caching, distributed file systems, domain name" + 
                    "services, instant messaging, multicast, and also peer-to-peer file sharing and content distribution systems. Notable distributed networks that use DHTs include" +
                    "BitTorrent's distributed tracker, the Coral Content Distribution Network, the Kad network, the Storm botnet, and YaCy.";
                Console.WriteLine(defaultString);
                result = network[0].GetConsumer<WordCount>(taskId).Run(defaultString, 4);
            }
            else
            {
                Console.WriteLine("Enter a string");
                string s = Console.ReadLine();
                int chunkSize = Math.Max(s.Length / 100, 2);
                result = network[0].GetConsumer<WordCount>(taskId).Run(s, chunkSize);
            }

            foreach (var item in result.OrderBy(a => a.Value))
                Console.WriteLine(item.Key + " => " + item.Value);
        }
        #endregion

        #region datastore
        private static void DataStore(List<DistributedRoutingTable> network)
        {
            Console.WriteLine("\tSetting up datastore");
            Guid storeId = Guid.NewGuid();
            Guid erasureId = Guid.NewGuid();
            RegisterConsumerOnNetwork(network, d => new KeyValuePairStore(storeId));
            RegisterConsumerOnNetwork(network, d => new ErasureDataStore(erasureId, d.GetConsumer<KeyValuePairStore>(storeId)));

            KeyValuePairStore store = network[0].GetConsumer<KeyValuePairStore>(storeId);
            ErasureDataStore erasure = network[0].GetConsumer<ErasureDataStore>(erasureId);

            Console.Clear();

            while (!HandleDatastoreInput(store, erasure, Console.ReadLine()));
        }

        private static bool HandleDatastoreInput(KeyValuePairStore store, ErasureDataStore erasure, string input)
        {
            try
            {
                switch (input.Split(' ')[0])
                {
                    case "help":
                        Console.WriteLine("Type:");
                        Console.WriteLine("\t\"help\" for this helpful message");
                        Console.WriteLine("\t\"put [int/key] [string/data]\" to put the given string into the datastore with the given integer key");
                        Console.WriteLine("\t\"get [int/key]\" to get the data with the given key from the datastore");
                        Console.WriteLine("\t\"put_erasure [int/key] [string/data]\" to put the given string into the erasure datastore with the given integer key");
                        Console.WriteLine("\t\"get_erasure [int/key]\" to get the data with the given key from the erasure data store");
                        Console.WriteLine("\t\"get_erasure_block_hash [int/root] [int/index]\" to fetch the erasure block with the given rootKey");
                        Console.WriteLine("\t\"quit\" to return to the main menu");
                        break;
                    case "quit":
                        return true;
                    case "put":
                        {
                            int key = int.Parse(input.Split(' ')[1]);
                            string data = input.Split(' ').Skip(2).Aggregate((a, b) => a + " " + b);
                            store.Put(Identifier512.CreateKey(key), Encoding.ASCII.GetBytes(data));
                            Console.WriteLine("\tDone");
                            break;
                        }
                    case "get":
                        {
                            int key = int.Parse(input.Split(' ')[1]);
                            string output = Encoding.ASCII.GetString(store.Get(Identifier512.CreateKey(key), -1));
                            Console.WriteLine("\t" + output);
                            break;
                        }
                    case "get_erasure_block_hash":
                        {
                            Identifier512 root = Identifier512.CreateKey(int.Parse(input.Split(' ')[1]));
                            int index = int.Parse(input.Split(' ')[2]);
                            var output = store.Get((root + index).GetHashedKey(), -1);

                            MD5 hasher = MD5.Create();
                            var hash = hasher.ComputeHash(output);

                            Console.WriteLine("\t" + hash.Select(a => a.ToString()).Aggregate((a, b) => a.ToString() + b.ToString()));
                            break;
                        }
                    case "put_erasure":
                        {
                            int key = int.Parse(input.Split(' ')[1]);
                            string data = input.Split(' ').Skip(2).Aggregate((a, b) => a + " " + b);
                            erasure.Put(Identifier512.CreateKey(key), Encoding.ASCII.GetBytes(data), 1, 5);
                            Console.WriteLine("\tDone");
                            break;
                        }
                    case "get_erasure":
                        {
                            int key = int.Parse(input.Split(' ')[1]);
                            string output = Encoding.ASCII.GetString(erasure.Get(Identifier512.CreateKey(key), -1));
                            Console.WriteLine("\t" + output);
                            break;
                        }
                    default:
                        Console.WriteLine("Unknown command " + input);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
        #endregion

        #region logfile playback
        public static void LogfilePlayback()
        {
            if (!Directory.Exists("logs"))
                Console.WriteLine("\tNo logfile folder found, create \"logs\" folder and put some files in it");

            Console.WriteLine("\tSelect logfile:");
            string[] files = Directory.EnumerateFiles("logs").ToArray();
            for (int i = 0; i < files.Length; i++)
                Console.WriteLine("\t[" + i + "] " + files[i]);

            string file = files[int.Parse(Console.ReadLine())];
            BufferedStream stream = new BufferedStream(File.OpenRead(file));

            Console.WriteLine("\tConnecting to visualiser");
            if (!BeginLogging("localhost"))
                return;

            while (!HandleLogfileInput(stream)) ;
        }

        private static bool HandleLogfileInput(Stream file)
        {
            var input = Console.ReadLine();
            switch (input.ToLower().Split(' ')[0])
            {
                case "help":
                    Console.WriteLine("\t\"help\" display this helpful message");
                    Console.WriteLine("\t\"step [int/steps]\" proceed a certain number of steps");
                    Console.WriteLine("\t\"quit\" return to normal mode");
                    break;
                case "step":
                    int steps = int.Parse(input.Split(' ')[1]);
                    for (int i = 0; i < steps; i++)
                    {
                        if (file.Length == file.Position)
                        {
                            Console.WriteLine("\tPlayback Complete");
                            break;
                        }
                        var m = Serializer.DeserializeWithLengthPrefix<BaseMessage>(file, PrefixStyle.Base128);
                        Console.WriteLine(m.ToString());
                        //m.Send();
                        Thread.Sleep(10);
                    }
                    break;
                case "quit":
                    return true;
            }
            return false;
        }

        private static bool BeginLogging(string loggerIp)
        {
            //try
            //{
            //    LoggerClient.Begin(loggerIp, false);
            //    Console.WriteLine("\tLogging connection established");
            //}
            //catch (SocketException)
            //{
            //    Console.WriteLine("\tCould not establish a logging connection");
            //    return false;
            //}
            //return true;
            return false;
        }
        #endregion
    }
}
