using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider;
using DistributedServiceProvider.MessageConsumers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    static class TestUtilities
    {
        public static void TestCallbackLeak(IEnumerable<DistributedRoutingTable> tables)
        {
            foreach (var item in tables)
                Assert.AreEqual(0, item.GetConsumer<Callback>(Callback.CONSUMER_ID).TokenCount);
        }

        public static void TestCallbackLeak(params DistributedRoutingTable[] tables)
        {
            TestCallbackLeak(tables as IEnumerable<DistributedRoutingTable>);
        }
    }
}
