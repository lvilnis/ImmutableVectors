using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PersistentVector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace PersistentVectorTests
{
    [TestClass]
    public class PersistentVectorTests
    {
        public void RunTests()
        {
            TestAppendable();
            TestPrependable();
            TestHOFs();
            TestArrayBacked();
            TestFastConstructors();
            TestFastToArray();
        }

        private void TestVectorImplementation(Func<IEnumerable<int>, IVector<int>> getVector, string name)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            TimeWithMessage(ms => string.Format("{0}: Standard Battery: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 1000; it++)
                {
                    var vecInner = getVector(new[] { 1, 2, 3, 4, 5 });

                    Assert.AreEqual(vecInner.Tail, getVector(new[] { 2, 3, 4, 5 }));
                    Assert.AreEqual(vecInner.Popped, getVector(new[] { 1, 2, 3, 4 }));
                    Assert.AreEqual(vecInner.Append(7), getVector(new[] { 1, 2, 3, 4, 5, 7 }));
                    Assert.AreEqual(vecInner.Cons(5), getVector(new[] { 5, 1, 2, 3, 4, 5 }));
                    Assert.AreEqual(vecInner.Update(2, 6), getVector(new[] { 1, 2, 6, 4, 5 }));

                    Assert.AreEqual(vecInner[3], 4);
                    Assert.AreNotEqual(vecInner[2], 5);
                    Assert.AreEqual(vecInner.Concat(vecInner), getVector(new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 }));
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

            TimeWithMessage(ms => string.Format("{0}: Prepending: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 1; it++)
                {
                    var emptyVec = getVector(Enumerable.Empty<int>());
                    foreach (var number in Enumerable.Range(1, 100000))
                        emptyVec = emptyVec.Cons(number);
                }
            });

            TimeWithMessage(ms => string.Format("{0}: Appending: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 1; it++)
                {
                    var emptyVec = getVector(Enumerable.Empty<int>());
                    foreach (var number in Enumerable.Range(1, 100000))
                        emptyVec = emptyVec.Append(number);
                }
            });

            var vec = getVector(Enumerable.Range(1, 100000));

            TimeWithMessage(ms => string.Format("{0}: Iterating: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 50; it++)
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

            var vec1 = getVector(Enumerable.Range(1, 1000));
            var vec2 = getVector(Enumerable.Range(1000, 1000));
            TimeWithMessage(ms => string.Format("{0}: Zip and Map: {1} ms", name, ms), () =>
            {
                for (int it = 0; it < 500; it++)
                {
                    Assert.AreEqual(vec1.Zip(vec2), vec1.Map(i => Tuple.Create(i, i + 999)));
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
        public void TestHOFs()
        {
            IVector<int> vec1 = new AppendableImmutableVector<int>(Enumerable.Range(0, 1000000).ToArray());
            IVector<int> vec2 = new PrependableImmutableVector<int>(Enumerable.Range(0, 1000000).ToArray());

            TimeWithMessage(ms => string.Format("Map (appendable): {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    vec1.Map(el => el * 3 - 45 + 3 * 26);
                }
            });

            TimeWithMessage(ms => string.Format("Map (prependable): {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    vec2.Map(el => el * 3 - 45 + 3 * 26);
                }
            });

            TimeWithMessage(ms => string.Format("Filter (appendable): {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    vec1.Filter(el => el % 2 == 0);
                }
            });

            TimeWithMessage(ms => string.Format("Filter (prependable): {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    vec2.Filter(el => el % 2 == 0);
                }
            });
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
            //      TestVectorImplementation(Vector.ArrayListBacked, "ArrayListBacked");
        }

        [TestMethod]
        public void TestFastConstructors()
        {
            //IVector<int> vec1 = new AppendableImmutableVector<int>(Enumerable.Range(0, 1000000).ToArray());
            //IVector<int> vec2 = new AppendableImmutableVector<int>().Concat(Enumerable.Range(0, 1000000));
            //Assert.AreEqual(vec1, vec2);

            var arr = Enumerable.Range(1, 1000000).ToArray();
            TimeWithMessage(ms => string.Format("Fast Constructor: {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var vec = new AppendableImmutableVector<int>(arr);
                }
            });

            TimeWithMessage(ms => string.Format("Slow Constructor: {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var vec = new AppendableImmutableVector<int>().Concat(arr);
                }
            });

            TimeWithMessage(ms => string.Format("ArrayList (for comparison): {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var list = new List<int>();
                    foreach (var item in arr)
                        list.Add(item);
                }
            });

            // test depth of 1
            IVector<int> vec1 = new AppendableImmutableVector<int>(Enumerable.Range(0, 12).ToArray());
            Assert.AreEqual(vec1.ToArray().Length, 12);

            // test depth of 2
            IVector<int> vec2 = new AppendableImmutableVector<int>(Enumerable.Range(0, 120).ToArray());
            Assert.AreEqual(vec2.ToArray().Length, 120);

            // test depth of 3
            IVector<int> vec3 = new AppendableImmutableVector<int>(Enumerable.Range(0, 1200).ToArray());
            Assert.AreEqual(vec3.ToArray().Length, 1200);

            // test depth of 4
            IVector<int> vec4 = new AppendableImmutableVector<int>(Enumerable.Range(0, 40000).ToArray());
            Assert.AreEqual(vec4.ToArray().Length, 40000);

            // test depth of 5
            IVector<int> vec5 = new AppendableImmutableVector<int>(Enumerable.Range(0, 1200000).ToArray());
            Assert.AreEqual(vec5.ToArray().Length, 1200000);

            //// test depth of 6
            //IVector<int> vec6 = new AppendableImmutableVector<int>(Enumerable.Range(0, 40000000).ToArray());
            //Assert.AreEqual(vec6.ToArray().Length, 40000000);

            //// test depth of 7 - this will probably crash a computer since this requires several gigabytes of memory sooooo
            //IVector<int> vec7 = new AppendableImmutableVector<int>(Enumerable.Range(0, 1100000000).ToArray());
            //Assert.AreEqual(vec7.Length, 1100000000);
        }

        [TestMethod]
        public void TestFastToArray()
        {
            IVector<int> vec = new AppendableImmutableVector<int>(Enumerable.Range(0, 1000000).ToArray());

            TimeWithMessage(ms => string.Format("Fast ToArray: {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var resultArray = vec.FastToArray();
                    Assert.AreEqual(resultArray.Length, vec.Length);
                }
            });

            TimeWithMessage(ms => string.Format("Slow ToArray: {0}", ms), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var resultArray = vec.ToArray();
                    Assert.AreEqual(resultArray.Length, vec.Length);
                }
            });
        }
    }
}
