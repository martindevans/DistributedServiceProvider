using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistributedServiceProvider;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using Consumers;
using Consumers.DataStorage;

namespace TestProject
{
    /// <summary>
    /// Summary description for BasicStorage
    /// </summary>
    [TestClass]
    public class Storage
    {
        [TestMethod]
        public void KeyValuePairStore()
        {
            List<DistributedRoutingTable> network = CreateNetwork();

            Guid consumerId = Guid.NewGuid();
            for (int i = 0; i < network.Count; i++)
                network[i].RegisterConsumer(new KeyValuePairStore(consumerId));

            Identifier512 key = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            byte[] data = new byte[] { 1 };

            network[1].GetConsumer<KeyValuePairStore>(consumerId).Put(key, data);

            var result = network[2].GetConsumer<KeyValuePairStore>(consumerId).Get(key, 1000);

            for (int i = 0; i < result.Length; i++)
                Assert.AreEqual(result[i], data[i]);

            network[3].GetConsumer<KeyValuePairStore>(consumerId).Delete(key);

            result = network[4].GetConsumer<KeyValuePairStore>(consumerId).Get(key, 1000);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void ErasureStore()
        {
            List<DistributedRoutingTable> network = CreateNetwork();

            Guid baseStorageId = Guid.NewGuid();
            Guid consumerId = Guid.NewGuid();
            for (int i = 0; i < network.Count; i++)
            {
                var kvps = new KeyValuePairStore(baseStorageId);
                network[i].RegisterConsumer(kvps);
                network[i].RegisterConsumer(new ErasureDataStore(consumerId, kvps));
            }

            Identifier512 key = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            network[1].GetConsumer<ErasureDataStore>(consumerId).Put(key, data, 1, 2);

            var result = network[2].GetConsumer<ErasureDataStore>(consumerId).Get(key, 1000);

            for (int i = 0; i < result.Length; i++)
                Assert.AreEqual(result[i], data[i]);
        }

        private List<DistributedRoutingTable> CreateNetwork()
        {
            Func<DistributedRoutingTable, Contact> contactFactory = drt =>
            {
                return new LocalContact(drt);
            };

            Guid networkId = Guid.NewGuid();
            Configuration config = new Configuration();
            DistributedRoutingTable table1 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);
            DistributedRoutingTable table2 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);
            DistributedRoutingTable table3 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);
            DistributedRoutingTable table4 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);
            DistributedRoutingTable table5 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);

            table1.Bootstrap(table2.LocalContact);
            table2.Bootstrap(table3.LocalContact);
            table3.Bootstrap(table4.LocalContact);
            table4.Bootstrap(table5.LocalContact);
            table5.Bootstrap(table1.LocalContact);

            return new List<DistributedRoutingTable>(new[] { table1, table2, table3, table4, table5 });
        }
    }
}
