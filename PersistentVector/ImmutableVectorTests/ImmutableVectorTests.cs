using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentVectors;
using System.Diagnostics;

namespace ImmutableVectorTests
{
    [TestClass]
    public class ImmutableVectorTests
    {
        private void TestVectorImplementation(Func<IEnumerable<int>, IVector<int>> getVector, string name)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            TimeWithMessage(ms => string.Format("{0}: Standard Battery: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 1000; it++)
                {
                    var vec1 = getVector(new[] { 1, 2, 3, 4, 5 });

                    Assert.AreEqual(vec1.Tail, getVector(new[] { 2, 3, 4, 5 }));
                    Assert.AreEqual(vec1.Popped, getVector(new[] { 1, 2, 3, 4 }));
                    Assert.AreEqual(vec1.Append(7), getVector(new[] { 1, 2, 3, 4, 5, 7 }));
                    Assert.AreEqual(vec1.Cons(5), getVector(new[] { 5, 1, 2, 3, 4, 5 }));
                    Assert.AreEqual(vec1.Update(2, 6), getVector(new[] { 1, 2, 6, 4, 5 }));

                    Assert.AreEqual(vec1[3], 4);
                    Assert.AreNotEqual(vec1[2], 5);
                    Assert.AreEqual(vec1.Concat(vec1), getVector(new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 }));
                    Assert.AreEqual(getVector(Enumerable.Range(1, 1000))[500], 501);
                    Assert.AreEqual(
                        getVector(Enumerable.Range(1, 1000)).Map(i => i * 3),
                        getVector(Enumerable.Range(1, 1000).Select(i => i * 3)));
                    Assert.AreEqual(
                        getVector(Enumerable.Range(1, 1000)).Filter(i => i % 2 == 0),
                        getVector(Enumerable.Range(1, 1000).Where(i => i % 2 == 0)));
                    Assert.AreEqual(
                        getVector(Enumerable.Range(1, 1000)).Foldl((acc, el) => acc + el, 0),
                        Enumerable.Range(1, 1000).Aggregate(0, (acc, el) => acc + el));
                    Assert.AreEqual(
                        getVector(Enumerable.Range(1, 1000)).Foldr((acc, el) => acc + el, 0),
                        Enumerable.Range(1, 1000).Aggregate(0, (acc, el) => acc + el));
                    Assert.AreEqual(
                        getVector(Enumerable.Range(1, 1000)).Reduce((acc, el) => acc + el),
                        Enumerable.Range(1, 1000).Aggregate((acc, el) => acc + el));
                }
            });

            //TimeWithMessage(ms => string.Format("{0}: Prepending: {1} ms", name, ms), () =>
            //{
            //    for (int it = 0; it < 1; it++)
            //    {
            //        var vec = getVector(Enumerable.Empty<int>());
            //        foreach (var number in Enumerable.Range(1, 100000))
            //            vec = vec.Cons(number);
            //    }
            //});

            var vec = getVector(Enumerable.Range(1, 100000));

            TimeWithMessage(ms => string.Format("{0}: Iterating: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 10; it++)
                {
                    int count = 0;
                    foreach (var number in vec)
                        count++;
                }
            });

            TimeWithMessage(ms => string.Format("{0}: Random Access: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 10; it++)
                {
                    int count = 0;
                    for (int i = 0; i < vec.Length; i++)
                        count += vec[i];
                }
            });

        }

        private void TimeWithMessage(Func<double, string> message, Action toTime)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            toTime();
            stopwatch.Stop();
            Console.WriteLine(message(stopwatch.ElapsedMilliseconds));
        }

        [TestMethod]
        public void TestAppendable()
        {
            TestVectorImplementation(Vector.Appendable, "Appendable");
        }
        [TestMethod]
        public void TestPrependable()
        {
            TestVectorImplementation(Vector.Prependable, "Prependable");
        }
        [TestMethod]
        public void TestArrayBacked()
        {
            TestVectorImplementation(Vector.ArrayListBacked, "ArrayListBacked");
        }
    }
}
