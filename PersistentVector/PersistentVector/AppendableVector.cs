using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    // Port of Clojure's PersistentVector (by Rich Hickey) by way of Daniel Spiewak's Scala version

    // DISCLAIMER: this code is horribly factored, it shares tons of copy-paste with the prependable version. But, I
    // haven't had time to make them share code reasonably yet...  Also, there's tons of super-ugly
    // manually-inlined code that dispatches on tree depth. It's pretty fast though!

    public class AppendableImmutableVector<T> : IVector<T>
    {
        private readonly int m_TailOffset;
        private readonly int m_Shift;
        private readonly int m_Length;
        private readonly object[] m_Root;
        private readonly T[] m_Tail;

        private AppendableImmutableVector(int length, int shift, object[] root, T[] tail)
        {
            m_TailOffset = length - tail.Length;
            m_Length = length;
            m_Shift = shift;
            m_Root = root;
            m_Tail = tail;
        }

        public AppendableImmutableVector() : this(0, 5, new object[0], new T[0]) { }

        public T this[int i]
        {
            get
            {
                if (i >= 0 && i < m_Length)
                {
                    if (i >= m_TailOffset)
                        return (T)m_Tail[i & 0x01f];

                    var arr = m_Root;
                    for (int level = m_Shift; level > 5; level -= 5)
                        arr = (object[])arr[(i >> level) & 0x01f];

                    var leafArr = (T[])arr[(i >> 5) & 0x01f];

                    return leafArr[i & 0x01f];
                }

                throw new Exception("Index out of bounds!");
            }
        }

        public AppendableImmutableVector<T> Update(int i, T obj)
        {
            if (i >= 0 && i < m_Length)
            {
                if (i >= m_TailOffset)
                {
                    var newTail = new T[m_Tail.Length];
                    Array.Copy(m_Tail, newTail, m_Tail.Length);
                    newTail[i & 0x01f] = obj;

                    return new AppendableImmutableVector<T>(m_Length, m_Shift, m_Root, newTail);
                }

                return new AppendableImmutableVector<T>(m_Length, m_Shift, UpdateIndex(m_Shift, m_Root, i, obj), m_Tail);
            }

            throw new Exception("Index out of bounds!");
        }

        public AppendableImmutableVector<T> Append(T obj)
        {
            if (m_Tail.Length < 32)
            {
                var newTail = new T[m_Tail.Length + 1];
                Array.Copy(m_Tail, newTail, m_Tail.Length);
                newTail[m_Tail.Length] = obj;

                return new AppendableImmutableVector<T>(m_Length + 1, m_Shift, m_Root, newTail);
            }

            object expansion;
            var newRoot = PushTail(m_Shift - 5, m_Root, m_Tail, out expansion);

            var newShift = m_Shift;

            if (expansion != null)
            {
                newRoot = NewArray(newRoot, expansion);
                newShift += 5;
            }

            return new AppendableImmutableVector<T>(m_Length + 1, newShift, newRoot, new T[] { obj });
        }

        public AppendableImmutableVector<T> Pop()
        {
            if (m_Length == 0)
            {
                throw new Exception("Can't pop empty vector");
            }
            else if (m_Length == 1)
            {
                return new AppendableImmutableVector<T>();
            }
            else if (m_Tail.Length > 1)
            {
                var newTail = new T[m_Tail.Length - 1];
                Array.Copy(m_Tail, newTail, newTail.Length);

                return new AppendableImmutableVector<T>(m_Length - 1, m_Shift, m_Root, newTail);
            }
            else
            {
                object pTail;
                var newRoot = PopTail(m_Shift - 5, m_Root, out pTail);

                var newShift = m_Shift;

                if (newRoot == null)
                    newRoot = new object[0];

                if (m_Shift > 5 && newRoot.Length == 1)
                {
                    newRoot = (object[])newRoot[0];
                    newShift -= 5;
                }

                return new AppendableImmutableVector<T>(m_Length - 1, newShift, newRoot, (T[])pTail);
            }
        }

        #region Private helpers

        private object[] PopTail(int shift, object[] arr, out object pTail)
        {
            if (shift == 0)
            {
                pTail = arr[arr.Length - 1];
            }
            else
            {
                var newChild = PopTail(shift - 5, (object[])arr[arr.Length - 1], out pTail);

                if (newChild != null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, ret, arr.Length);

                    ret[arr.Length - 1] = newChild;

                    return ret;
                }
            }

            // Contraction
            if (arr.Length == 1)
            {
                return null;
            }
            else
            {
                var ret = new object[arr.Length - 1];
                Array.Copy(arr, 0, ret, 0, ret.Length);

                return ret;
            }
        }

        private object[] NewArray(params object[] elems)
        {
            var back = new object[elems.Length];
            Array.Copy(elems, back, back.Length);

            return back;
        }

        private object[] PushTail(int level, object[] arr, T[] tailNode, out object expansion)
        {
            object newChild = null;

            if (level == 0)
            {
                newChild = tailNode;
            }
            else
            {
                object subExpansion;
                var newInnerChild = PushTail(level - 5, (object[])arr[arr.Length - 1], tailNode, out subExpansion);

                if (subExpansion == null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, ret, arr.Length);

                    ret[arr.Length - 1] = newInnerChild;

                    expansion = null;
                    return ret;
                }

                newChild = subExpansion;
            }

            // Expansion
            if (arr.Length == 32)
            {
                expansion = NewArray(newChild);
                return arr;
            }
            else
            {
                var ret = new object[arr.Length + 1];
                Array.Copy(arr, 0, ret, 0, arr.Length);
                ret[arr.Length] = newChild;

                expansion = null;
                return ret;
            }
        }

        private object[] UpdateIndex(int level, object[] arr, int i, T obj)
        {
            var ret = new object[arr.Length];
            Array.Copy(arr, ret, arr.Length);

            if (level == 0)
            {
                ret[i & 0x01f] = obj;
            }
            else
            {
                var subidx = (i >> level) & 0x01f;
                ret[subidx] = UpdateIndex(level - 5, (object[])arr[subidx], i, obj);
            }

            return ret;
        }

        #endregion

        #region Collection functions

        public IEnumerable<T> GetLeftToRightEnumeration()
        {
            // Specialize Enumeration to different tree heights gives a ~3x improvement over just indexing
            int depth = GetDepthFromLength(m_Length - m_Tail.Length);

            if (depth == 2)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (T[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        yield return arr1[i2];
                    }
                }
            }
            else if (depth == 3)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (T[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            yield return arr2[i3];
                        }
                    }
                }
            }
            else if (depth == 4)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (T[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                yield return arr3[i4];
                            }
                        }
                    }
                }
            }
            else if (depth == 5)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                var arr4 = (T[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                {
                                    yield return arr4[i5];
                                }
                            }
                        }
                    }
                }
            }
            else if (depth == 6)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                {
                                    var arr5 = (T[])arr4[i5];
                                    for (int i6 = 0; i6 < arr5.Length; i6++)
                                    {
                                        yield return arr5[i6];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (depth == 7)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                {
                                    var arr5 = (object[])arr4[i5];
                                    for (int i6 = 0; i6 < arr5.Length; i6++)
                                    {
                                        var arr6 = (T[])arr5[i6];
                                        for (int i7 = 0; i7 < arr6.Length; i7++)
                                        {
                                            yield return arr6[i7];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < m_Tail.Length; i++)
                yield return m_Tail[i];
        }

        public IEnumerable<T> GetRightToLeftEnumeration()
        {
            // Specialize Enumeration to different tree heights gives a ~3x improvement over just indexing
            // jeepers the backwards enumerator takes 134 lines of code!!
            int depth = GetDepthFromLength(m_Length - m_Tail.Length);

            for (int i = m_Tail.Length - 1; i >= 0; i--)
                yield return m_Tail[i];

            if (depth == 2)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (T[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        yield return arr1[i2];
                    }
                }
            }
            else if (depth == 3)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        var arr2 = (T[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            yield return arr2[i3];
                        }
                    }
                }
            }
            else if (depth == 4)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            var arr3 = (T[])arr2[i3];
                            for (int i4 = arr3.Length - 1; i4 >= 0; i4--)
                            {
                                yield return arr3[i4];
                            }
                        }
                    }
                }
            }
            else if (depth == 5)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = arr3.Length - 1; i4 >= 0; i4--)
                            {
                                var arr4 = (T[])arr3[i4];
                                for (int i5 = arr4.Length - 1; i5 >= 0; i5--)
                                {
                                    yield return arr4[i5];
                                }
                            }
                        }
                    }
                }
            }
            else if (depth == 6)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = arr3.Length - 1; i4 >= 0; i4--)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = arr4.Length - 1; i5 >= 0; i5--)
                                {
                                    var arr5 = (T[])arr4[i5];
                                    for (int i6 = arr5.Length - 1; i6 >= 0; i6--)
                                    {
                                        yield return arr5[i6];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (depth == 7)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = arr3.Length - 1; i4 >= 0; i4--)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = arr4.Length - 1; i5 >= 0; i5--)
                                {
                                    var arr5 = (object[])arr4[i5];
                                    for (int i6 = arr5.Length - 1; i6 >= 0; i6--)
                                    {
                                        var arr6 = (T[])arr5[i6];
                                        for (int i7 = arr6.Length - 1; i7 >= 0; i7--)
                                        {
                                            yield return arr6[i7];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public AppendableImmutableVector<T> Concat(IEnumerable<T> other)
        {
            // should make a dedicated Concat routine that avoids some of the
            // repeated copy-on-writes since those intermediate results are never seen
            var currentVector = this;
            foreach (var item in other)
                currentVector = currentVector.Append(item);
            return currentVector;
        }

        public AppendableImmutableVector<T> Filter(Func<T, bool> pred)
        {
            // Using intermediate arrays/lists seems to give a 20-35% improvement over repeated appends in the average case (filtering out evens)
            // Probably slower for filtering down small sets - which might be what we should be optimizing more
            var matching = new List<T>(m_Length);
            foreach (var item in this)
                if (pred(item))
                    matching.Add(item);
            return new AppendableImmutableVector<T>(matching.ToArray());
        }

        public AppendableImmutableVector<A> FlatMap<A>(Func<T, IEnumerable<A>> mapper)
        {
            var mapped = new List<A>(m_Length);
            foreach (var item in this)
                foreach (var newElement in mapper(item))
                    mapped.Add(newElement);
            return new AppendableImmutableVector<A>(mapped.ToArray());
        }

        public AppendableImmutableVector<A> Map<A>(Func<T, A> mapper)
        {
            var mapped = new A[m_Length];
            int i = 0;
            foreach (var item in this)
                mapped[i++] = mapper(item);
            return new AppendableImmutableVector<A>(mapped);
        }

        public AppendableImmutableVector<Tuple<T, A>> Zip<A>(IVector<A> that)
        {
            var resultArray = new Tuple<T, A>[Math.Min(m_Length, that.Length)];

            // This enumerator crap should be faster than the indexing version
            var thisEnumerator = this.GetLeftToRightEnumeration().GetEnumerator();
            var thatEnumerator = that.GetEnumerator();

            int i = 0;
            while (thisEnumerator.MoveNext() && thatEnumerator.MoveNext())
                resultArray[i++] = Tuple.Create(thisEnumerator.Current, thatEnumerator.Current);

            return new AppendableImmutableVector<Tuple<T, A>>(resultArray);
        }

        #endregion

        #region IVector<T> members

        T IVector<T>.this[int i]
        {
            get { return this[i]; }
        }

        int IVector<T>.Length
        {
            get { return m_Length; }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetLeftToRightEnumeration().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        T IVector<T>.Head
        {
            get { return this[0]; }
        }

        IVector<T> IVector<T>.Tail
        {
            get
            {
                var newArr = new T[m_Length - 1];
                var enumerator = this.GetLeftToRightEnumeration().GetEnumerator();
                enumerator.MoveNext();
                int i = 0;
                while (enumerator.MoveNext())
                    newArr[i++] = enumerator.Current;
                return new PrependableImmutableVector<T>(newArr);
            }
        }

        IVector<T> IVector<T>.Cons(T newItem)
        {
            // is there like a windowed / projection way to get fast slice?
            var newArr = new T[m_Length + 1];
            newArr[0] = newItem;
            int i = 1;
            foreach (var item in this)
                newArr[i++] = item;
            return new PrependableImmutableVector<T>(newArr);
        }

        IVector<T> IVector<T>.Update(int index, T newVal)
        {
            return this.Update(index, newVal);
        }

        T IVector<T>.End
        {
            get { return this[m_Length - 1]; }
        }

        IVector<T> IVector<T>.Popped
        {
            get { return Pop(); }
        }

        IVector<T> IVector<T>.Append(T item)
        {
            return Append(item);
        }

        IVector<T> IVector<T>.Concat(IEnumerable<T> items)
        {
            return Concat(items);
        }

        IVector<T> IVector<T>.Filter(Func<T, bool> pred)
        {
            return Filter(pred);
        }

        IVector<U> IVector<T>.Map<U>(Func<T, U> mapper)
        {
            return Map(mapper);
        }

        IVector<U> IVector<T>.FlatMap<U>(Func<T, IEnumerable<U>> mapper)
        {
            return FlatMap(mapper);
        }

        IVector<Tuple<T, U>> IVector<T>.Zip<U>(IVector<U> that)
        {
            return Zip(that);
        }

        TAcc IVector<T>.Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            var currentValue = seed;
            foreach (var item in this)
                currentValue = accumulator(currentValue, item);
            return currentValue;
        }

        TAcc IVector<T>.Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            var currentValue = seed;
            foreach (var item in GetRightToLeftEnumeration())
                currentValue = accumulator(currentValue, item);
            return currentValue;
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            var enumerator = ((IEnumerable<T>)this).GetEnumerator();
            enumerator.MoveNext();
            var seed = enumerator.Current;
            while (enumerator.MoveNext())
                seed = accumulator(seed, enumerator.Current);
            return seed;
        }

        IEnumerable<T> IVector<T>.FastRightToLeftEnumeration
        {
            get { return GetRightToLeftEnumeration(); }
        }

        T[] IVector<T>.FastToArray()
        {
            // TODO manual inlining for all 7 depths?? Or can we iterate through these things faster in some other way?

            var outArray = new T[m_Length];
            int outputIndex = 0;

            for (int i = 0; i < m_TailOffset; i += 32)
            {
                var arr = m_Root;
                for (int level = m_Shift; level > 5; level -= 5)
                    arr = (object[])arr[(i >> level) & 0x01f];

                var leafArr = (T[])arr[(i >> 5) & 0x01f];

                Array.Copy(leafArr, 0, outArray, outputIndex, 32);

                outputIndex += 32;
            }

            Array.Copy(m_Tail, 0, outArray, outputIndex, m_Tail.Length);

            return outArray;
        }

        T[] IVector<T>.SliceToArray(int start, int length)
        {
            // If we had fast append _and_ prepend we could share leafarrays between copies..
            // Add optimization for slices starting from front or back to re-use leafarrays??
            // Good idea. Could also name this popN and skipN or something.
            int frontLength = start % 32;
            int backLength = length % 32;

            throw new NotImplementedException();
        }

        IVector<T> IVector<T>.Slice(int start, int length)
        {
            throw new NotImplementedException();
        }

        IVector<U> IVector<T>.New<U>(params U[] items)
        {
            return new AppendableImmutableVector<U>(items);
        }

        #endregion

        public bool Equals(IVector<T> other)
        {
            return other == null ? false : other.SequenceEqual(this);
        }

        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (other is IVector<T>) return Equals((IVector<T>)other);
            else return false;
        }

        #region Embarassingly huge inlined constructors

        public AppendableImmutableVector(T[] items)
        {
            // FIXME this code is insanely ugly, please rewrite it to not be so damn awful

            int tailLength = (items.Length - 1) % 32 + 1;
            int depth = GetDepthFromLength(items.Length - tailLength);

            int numLeafArrays = (int)Math.Ceiling(items.Length / 32f) - 1;
            int numLevel1Parents = (int)Math.Ceiling(numLeafArrays / 32f);
            int numLevel2Parents = (int)Math.Ceiling(numLevel1Parents / 32f);
            int numLevel3Parents = (int)Math.Ceiling(numLevel2Parents / 32f);
            int numLevel4Parents = (int)Math.Ceiling(numLevel3Parents / 32f);
            int numLevel5Parents = (int)Math.Ceiling(numLevel4Parents / 32f);

            int inputIndex = 0;

            if (depth == 0)
            {
                m_Root = new object[0];
            }
            else if (depth == 2)
            {
                var level1Parent = new object[numLeafArrays];
                for (int i1 = 0; i1 < numLeafArrays; i1++)
                {
                    var leafArray = new T[32];
                    Array.Copy(items, inputIndex, leafArray, 0, 32);
                    level1Parent[i1] = leafArray;
                    inputIndex += 32;
                }
                m_Root = level1Parent;
            }
            else if (depth == 3)
            {
                var level2Parent = new object[numLevel1Parents];
                for (int i2 = 0; i2 < numLevel1Parents; i2++)
                {
                    var level1Parent = new object[i2 == numLevel1Parents - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                    for (int i1 = 0; i1 < level1Parent.Length; i1++)
                    {
                        var leafArray = new T[32];
                        Array.Copy(items, inputIndex, leafArray, 0, 32);
                        level1Parent[i1] = leafArray;
                        inputIndex += 32;
                    }
                    level2Parent[i2] = level1Parent;
                }
                m_Root = level2Parent;
            }
            else if (depth == 4)
            {
                var level3Parent = new object[numLevel2Parents];

                for (int i3 = 0; i3 < numLevel2Parents; i3++)
                {
                    var level2Parent = new object[i3 == numLevel2Parents - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                    for (int i2 = 0; i2 < level2Parent.Length; i2++)
                    {
                        var level1Parent = new object[i3 == numLevel2Parents - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                        if (level1Parent.Length == 0) level1Parent = new object[32];
                        for (int i1 = 0; i1 < level1Parent.Length; i1++)
                        {
                            var leafArray = new T[32];
                            Array.Copy(items, inputIndex, leafArray, 0, 32);
                            level1Parent[i1] = leafArray;
                            inputIndex += 32;
                        }
                        level2Parent[i2] = level1Parent;
                    }
                    level3Parent[i3] = level2Parent;
                }

                m_Root = level3Parent;
            }
            else if (depth == 5)
            {
                var level4Parent = new object[numLevel3Parents];

                for (int i4 = 0; i4 < numLevel3Parents; i4++)
                {
                    var level3Parent = new object[i4 == numLevel3Parents - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                    for (int i3 = 0; i3 < level3Parent.Length; i3++)
                    {
                        var level2Parent = new object[i4 == numLevel3Parents - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                        for (int i2 = 0; i2 < level2Parent.Length; i2++)
                        {
                            var level1Parent = new object[i4 == numLevel3Parents - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                            for (int i1 = 0; i1 < level1Parent.Length; i1++)
                            {
                                var leafArray = new T[32];
                                Array.Copy(items, inputIndex, leafArray, 0, 32);
                                level1Parent[i1] = leafArray;
                                inputIndex += 32;
                            }
                            level2Parent[i2] = level1Parent;
                        }
                        level3Parent[i3] = level2Parent;
                    }
                    level4Parent[i4] = level3Parent;
                }

                m_Root = level4Parent;
            }
            else if (depth == 6)
            {
                var level5Parent = new object[numLevel4Parents];

                for (int i5 = 0; i5 < numLevel4Parents; i5++)
                {
                    var level4Parent = new object[i5 == numLevel4Parents - 1 ? (numLevel3Parents - 1) % 32 + 1 : 32];
                    for (int i4 = 0; i4 < level4Parent.Length; i4++)
                    {
                        var level3Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                        for (int i3 = 0; i3 < level3Parent.Length; i3++)
                        {
                            var level2Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                            for (int i2 = 0; i2 < level2Parent.Length; i2++)
                            {
                                var level1Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                                for (int i1 = 0; i1 < level1Parent.Length; i1++)
                                {
                                    var leafArray = new T[32];
                                    Array.Copy(items, inputIndex, leafArray, 0, 32);
                                    level1Parent[i1] = leafArray;
                                    inputIndex += 32;
                                }
                                level2Parent[i2] = level1Parent;
                            }
                            level3Parent[i3] = level2Parent;
                        }
                        level4Parent[i4] = level3Parent;
                    }
                    level5Parent[i5] = level4Parent;
                }

                m_Root = level5Parent;
            }
            else if (depth == 7)
            {
                var level6Parent = new object[numLevel5Parents];

                for (int i6 = 0; i6 < numLevel5Parents; i6++)
                {
                    var level5Parent = new object[i6 == numLevel5Parents - 1 ? (numLevel4Parents - 1) % 32 + 1 : 32];
                    for (int i5 = 0; i5 < level5Parent.Length; i5++)
                    {
                        var level4Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 ? (numLevel3Parents - 1) % 32 + 1 : 32];
                        for (int i4 = 0; i4 < level4Parent.Length; i4++)
                        {
                            var level3Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                            for (int i3 = 0; i3 < level3Parent.Length; i3++)
                            {
                                var level2Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                                for (int i2 = 0; i2 < level2Parent.Length; i2++)
                                {
                                    var level1Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                                    for (int i1 = 0; i1 < level1Parent.Length; i1++)
                                    {
                                        var leafArray = new T[32];
                                        Array.Copy(items, inputIndex, leafArray, 0, 32);
                                        level1Parent[i1] = leafArray;
                                        inputIndex += 32;
                                    }
                                    level2Parent[i2] = level1Parent;
                                }
                                level3Parent[i3] = level2Parent;
                            }
                            level4Parent[i4] = level3Parent;
                        }
                        level5Parent[i5] = level4Parent;
                    }
                    level6Parent[i6] = level5Parent;
                }

                m_Root = level6Parent;
            }

            m_Tail = new T[tailLength];
            Array.Copy(items, inputIndex, m_Tail, 0, tailLength);

            m_Length = items.Length;
            m_Shift = Math.Max(5, (depth - 1) * 5);
            m_TailOffset = m_Length - m_Tail.Length;
        }

        #endregion

        private static int GetDepthFromLength(int p)
        {
            const int zeroThresh = 0;
            const int oneThresh = 32;
            const int twoThresh = 32 << 5;
            const int threeThresh = 32 << 10;
            const int fourThresh = 32 << 15;
            const int fiveThresh = 32 << 20;
            const int sixThresh = 32 << 25;
            const int sevenThresh = 32 << 30;

            if (p <= zeroThresh) return 0;
            if (p <= twoThresh) return 2;
            if (p <= threeThresh) return 3;
            if (p <= fourThresh) return 4;
            if (p <= fiveThresh) return 5;
            if (p <= sixThresh) return 6;

            return 7;
        }
    }
}
