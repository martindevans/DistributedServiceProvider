using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistributedServiceProvider.Base;
using DistributedServiceProvider;
using DistributedServiceProvider.Contacts;
using System.IO;
using DistributedServiceProvider.MessageConsumers;

namespace TestProject
{
    [TestClass]
    public class RoutingTest
    {
        [TestCleanup]
        public void TestClean()
        {
            LocalContact.Clear();
        }

        private void FindTable(DistributedRoutingTable start, DistributedRoutingTable end)
        {
            var closest = start.GetConsumer<GetClosestNodes>(GetClosestNodes.GUID).GetClosestContacts(end.LocalIdentifier).First();
            if (closest.NetworkId != end.NetworkId) throw new Exception("Incorrect network GUID");
            if (closest.Identifier != end.LocalIdentifier) throw new Exception("Incorrect table");
        }

        [TestMethod]
        public void TwoNodeNetwork()
        {
            Func<DistributedRoutingTable, Contact> contactFactory = drt =>
            {
                return new LocalContact(drt);
            };

            Guid networkId = Guid.NewGuid();
            Configuration config = new Configuration();
            DistributedRoutingTable table1 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);
            DistributedRoutingTable table2 = new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config);

            table1.Bootstrap(table2.LocalContact);

#if DEBUG
            config.UpdateRoutingTable = false;
#endif

            FindTable(table1, table2);
            FindTable(table2, table1);

            TestUtilities.TestCallbackLeak(table1, table2);
        }

        [TestMethod]
        public void ThreeNodeNetwork()
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

            table1.Bootstrap(table2.LocalContact);
            table2.Bootstrap(table3.LocalContact);

#if DEBUG
            config.UpdateRoutingTable = false;
#endif

            FindTable(table1, table1);
            FindTable(table1, table2);
            FindTable(table1, table3);

            FindTable(table2, table1);
            FindTable(table2, table2);
            FindTable(table2, table3);

            FindTable(table3, table1);
            FindTable(table3, table2);
            FindTable(table3, table3);

            TestUtilities.TestCallbackLeak(table1, table2, table3);
        }

        [TestMethod]
        public void FiveNodeRing()
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

#if DEBUG
            config.UpdateRoutingTable = false;
#endif

            FindTable(table1, table1);
            FindTable(table1, table2);
            FindTable(table1, table3);
            FindTable(table1, table4);
            FindTable(table1, table5);

            TestUtilities.TestCallbackLeak(table1, table2, table3, table4, table5);
        }

        [TestMethod]
        public void RingBootstrappedNetwork()
        {
            List<DistributedRoutingTable> tables = new List<DistributedRoutingTable>();

            Func<DistributedRoutingTable, Contact> contactFactory = drt =>
            {
                return new LocalContact(drt);
            };

            Guid networkId = Guid.NewGuid();
            Configuration config = new Configuration();
            for (int i = 0; i < 50; i++)
                tables.Add(new DistributedRoutingTable(Identifier512.NewIdentifier(), contactFactory, networkId, config));

            for (int i = 0; i < tables.Count; i++)
                tables[i].Bootstrap(tables[(i + 1) % tables.Count].LocalContact);

#if DEBUG
            config.UpdateRoutingTable = false;
#endif

            FindTable(tables[0], tables[tables.Count / 2]);
            FindTable(tables[tables.Count / 4], tables[tables.Count / 4 * 3]);

            TestUtilities.TestCallbackLeak(tables);
        }
    }
}
