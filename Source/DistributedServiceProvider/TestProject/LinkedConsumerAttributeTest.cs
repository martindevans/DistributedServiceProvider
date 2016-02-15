using DistributedServiceProvider.MessageConsumers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;

namespace TestProject
{
    
    
    /// <summary>
    ///This is a test class for LinkedConsumerAttributeTest and is intended
    ///to contain all LinkedConsumerAttributeTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LinkedConsumerAttributeTest
    {
        /// <summary>
        ///A test for LinkedConsumerAttribute Constructor
        ///</summary>
        [TestMethod()]
        public void LinkedConsumerAttributeConstructorTest()
        {
            Foo f = new Foo(Guid.NewGuid());

            Bar b = new Bar(new Guid(Foo.guidString));

            DistributedRoutingTable drt = new DistributedRoutingTable(Identifier512.NewIdentifier(), (a) => new LocalContact(a), Guid.NewGuid(), new Configuration());
            drt.RegisterConsumer(b);
            drt.RegisterConsumer(f);

            Assert.AreEqual(f.Bar, b);
            Assert.IsNotNull(f.Bar2);
        }

        private class Foo
            :MessageConsumer
        {
            public const string guidString = "7a5eeaf1-243e-4495-a796-ea49aa287cb6";
            public const string guidString2 = "93b8d121-71ac-440e-9495-7bf144b0f2be";

            [LinkedConsumer(guidString)]
            public Bar Bar;

            [LinkedConsumer(guidString2)]
            public Bar Bar2;

            public Foo(Guid g)
                :base(g)
            {

            }

            public override void Deliver(DistributedServiceProvider.Contacts.Contact source, byte[] message)
            {
                throw new NotImplementedException();
            }
        }

        private class Bar
            : MessageConsumer
        {
            public Bar(Guid g)
                : base(g)
            {

            }

            public override void Deliver(DistributedServiceProvider.Contacts.Contact source, byte[] message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
