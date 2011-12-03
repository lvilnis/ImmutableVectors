using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentVectors;

namespace ImmutableVectorTests
{
    [TestClass]
    public class ImmutableVectorTests
    {
        [TestMethod]
        public void TestAppendable()
        {
            var vec1 = Vector.Appendable(1, 2, 3, 4, 5);

            Assert.AreEqual(vec1.Tail, Vector.Appendable(2, 3, 4, 5));
            Assert.AreEqual(vec1.Popped, Vector.Appendable(1, 2, 3, 4));
            Assert.AreEqual(vec1[3], 4);
            Assert.AreNotEqual(vec1[2], 5);
            Assert.AreEqual(vec1.Concat(vec1), Vector.Appendable(1, 2, 3, 4, 5, 1, 2, 3, 4, 5));
            Assert.AreEqual(Vector.Appendable(Enumerable.Range(1, 1000))[500], 501);
            Assert.AreEqual(
                Vector.Appendable(Enumerable.Range(1, 1000)).Map(i => i * 3),
                Vector.Appendable(Enumerable.Range(1, 1000).Select(i => i * 3)));
        }
        [TestMethod]
        public void TestPrependable()
        {
            var vec1 = Vector.Prependable(1, 2, 3, 4, 5);

            Assert.AreEqual(vec1.Tail, Vector.Prependable(2, 3, 4, 5));
            Assert.AreEqual(vec1.Popped, Vector.Prependable(1, 2, 3, 4));
            Assert.AreEqual(vec1[3], 4);
            Assert.AreNotEqual(vec1[2], 5);
            Assert.AreEqual(vec1.Concat(vec1), Vector.Prependable(1, 2, 3, 4, 5, 1, 2, 3, 4, 5));
            Assert.AreEqual(Vector.Prependable(Enumerable.Range(1, 1000))[500], 501);
            Assert.AreEqual(
                Vector.Prependable(Enumerable.Range(1, 1000)).Map(i => i * 3),
                Vector.Prependable(Enumerable.Range(1, 1000).Select(i => i * 3)));
        }
        [TestMethod]
        public void TestArrayBacked()
        {
            var vec1 = Vector.ArrayListBacked(1, 2, 3, 4, 5);

            Assert.AreEqual(vec1.Tail, Vector.ArrayListBacked(2, 3, 4, 5));
            Assert.AreEqual(vec1.Popped, Vector.ArrayListBacked(1, 2, 3, 4));
            Assert.AreEqual(vec1[3], 4);
            Assert.AreNotEqual(vec1[2], 5);
            Assert.AreEqual(vec1.Concat(vec1), Vector.ArrayListBacked(1, 2, 3, 4, 5, 1, 2, 3, 4, 5));
            Assert.AreEqual(Vector.ArrayListBacked(Enumerable.Range(1, 1000))[500], 501);
            Assert.AreEqual(
                Vector.ArrayListBacked(Enumerable.Range(1, 1000)).Map(i => i * 3),
                Vector.ArrayListBacked(Enumerable.Range(1, 1000).Select(i => i * 3)));
        }
    }
}
