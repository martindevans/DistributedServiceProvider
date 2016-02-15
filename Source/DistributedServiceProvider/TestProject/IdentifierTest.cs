using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;

namespace TestProject
{
    [TestClass]
    public class IdentifierTest
    {
        [TestMethod]
        public void Construction()
        {
            Identifier512 a = new Identifier512(1, 2, 3, 4, 5, 6, 7, 8, 8, 10, 11, 12, 13, 14, 15, 16);
            Identifier512 b = new Identifier512(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 8, 10, 11, 12, 13, 14, 15, 16 });

            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void Distance()
        {
            var a = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            var b = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
            var c = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            Assert.IsTrue(Identifier512.Distance(a, c) > Identifier512.Distance(b, c));
        }

        [TestMethod]
        public void DistanceReversability()
        {
            var a = new Identifier512(new int[] { 0, 2 });
            var b = new Identifier512(new int[] { 0, 1 });
            var a2b = Identifier512.Distance(a, b);
            var b2a = Identifier512.Distance(b, a);

            Assert.AreEqual(a2b, b2a);
        }

        [TestMethod]
        public void AddInteger()
        {
            var a = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            var b = a + 1;
            var c = b + 1;

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(c, a);
            Assert.IsTrue(a < b);
            Assert.IsTrue(b < c);
        }

        [TestMethod]
        public void Ordering()
        {
            var a = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            var b = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
            var c = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            var d = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4);

            Assert.IsTrue(a < b);
            Assert.IsTrue(a < c);
            Assert.IsTrue(a < d);
            Assert.IsFalse(a < a);
            Assert.IsFalse(a > a);

            Assert.IsTrue(b > a);
            Assert.IsTrue(b < c);
            Assert.IsTrue(b < d);
            Assert.IsFalse(b < b);
            Assert.IsFalse(b > b);

            Assert.IsTrue(c > a);
            Assert.IsTrue(c > b);
            Assert.IsTrue(c < d);
            Assert.IsFalse(c < c);
            Assert.IsFalse(c > c);

            Assert.IsTrue(d > a);
            Assert.IsTrue(d > b);
            Assert.IsTrue(d > c);
            Assert.IsFalse(d < d);
            Assert.IsFalse(d > d);
        }

        [TestMethod]
        public void ReverseTriangleInequality()
        {
            var a = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            var b = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
            var c = new Identifier512(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            var AToB = Identifier512.Distance(a, b);
            var AToC = Identifier512.Distance(a, c);
            var BToC = Identifier512.Distance(b, c);
            var BToA = Identifier512.Distance(b, a);
            var CToB = Identifier512.Distance(c, b);
            var CToA = Identifier512.Distance(c, a);

            Assert.IsTrue(AToC >= Identifier512.Distance(AToB, BToC));
            Assert.IsTrue(AToB >= Identifier512.Distance(AToC, CToB));
            Assert.IsTrue(BToC >= Identifier512.Distance(BToA, AToC));
            Assert.IsTrue(BToA >= Identifier512.Distance(BToC, CToA));
            Assert.IsTrue(CToA >= Identifier512.Distance(CToB, BToA));
            Assert.IsTrue(CToB >= Identifier512.Distance(CToA, AToB));
        }
    }
}
