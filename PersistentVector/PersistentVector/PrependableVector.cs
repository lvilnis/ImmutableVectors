using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    // Port of Clojure's PersistentVector (by Rich Hickey) by way of Daniel Spiewak's Scala version
    internal class PrependableImmutableVector<T> : IVector<T>
    {
        private readonly int m_TailOffset;
        private readonly int m_Shift;
        private readonly int m_Length;
        private readonly object[] m_Root;
        private readonly object[] m_Tail;

        private PrependableImmutableVector(int length, int shift, object[] root, object[] tail)
        {
            m_TailOffset = length - tail.Length;
            m_Length = length;
            m_Shift = shift;
            m_Root = root;
            m_Tail = tail;
        }

        public PrependableImmutableVector() : this(0, 5, new object[0], new object[0]) { }

        public T this[int untranslatedIndex]
        {
            get
            {
                // HACK just using the pre-existing Appendable code and reversing the indices...
                int i = m_Length - untranslatedIndex - 1;
                if (i >= 0 && i < m_Length)
                {
                    if (i >= m_TailOffset)
                        return (T)m_Tail[i & 0x01f];

                    var arr = m_Root;
                    for (int level = m_Shift; level > 0; level -= 5)
                        arr = (object[])arr[(i >> level) & 0x01f];

                    return (T)arr[i & 0x01f];
                }

                throw new Exception("Index out of bounds!");
            }
        }

        public PrependableImmutableVector<T> Update(int untranslatedIndex, T obj)
        {
            // HACK just using the pre-existing Appendable code and reversing the indices...
            int i = m_Length - untranslatedIndex - 1;
            if (i >= 0 && i < m_Length)
            {
                if (i >= m_TailOffset)
                {
                    var newTail = new object[m_Tail.Length];
                    Array.Copy(m_Tail, 0, newTail, 0, m_Tail.Length);
                    newTail[i & 0x01f] = obj;

                    return new PrependableImmutableVector<T>(m_Length, m_Shift, m_Root, newTail);
                }

                return new PrependableImmutableVector<T>(m_Length, m_Shift, UpdateIndex(m_Shift, m_Root, i, obj), m_Tail);
            }

            throw new Exception("Index out of bounds!");
        }

        public PrependableImmutableVector<T> Prepend(T obj)
        {
            if (m_Tail.Length < 32)
            {
                var newTail = new object[m_Tail.Length + 1];
                Array.Copy(m_Tail, 0, newTail, 0, m_Tail.Length);
                newTail[m_Tail.Length] = obj;

                return new PrependableImmutableVector<T>(m_Length + 1, m_Shift, m_Root, newTail);
            }

            object expansion;
            var newRoot = PushTail(m_Shift - 5, m_Root, m_Tail, out expansion);

            var newShift = m_Shift;

            if (expansion != null)
            {
                newRoot = NewArray(newRoot, expansion);
                newShift += 5;
            }

            return new PrependableImmutableVector<T>(m_Length + 1, newShift, newRoot, NewArray(obj));
        }

        public PrependableImmutableVector<T> Pop()
        {
            if (m_Length == 0)
            {
                throw new Exception("Can't pop empty vector");
            }
            else if (m_Length == 1)
            {
                return new PrependableImmutableVector<T>();
            }
            else if (m_Tail.Length > 1)
            {
                var newTail = new object[m_Tail.Length - 1];
                Array.Copy(m_Tail, 0, newTail, 0, newTail.Length);

                return new PrependableImmutableVector<T>(m_Length - 1, m_Shift, m_Root, newTail);
            }
            else
            {
                object pTail;
                var newRoot = PopTail(m_Shift - 5, m_Root, null, out pTail);

                var newShift = m_Shift;

                if (newRoot == null)
                    newRoot = new object[0];

                if (m_Shift > 5 && newRoot.Length == 1)
                {
                    newRoot = (object[])newRoot[0];
                    newShift -= 5;
                }

                return new PrependableImmutableVector<T>(m_Length - 1, newShift, newRoot, (object[])pTail);
            }
        }

        #region Private helpers

        private object[] PopTail(int shift, object[] arr, object pTail, out object newPTail)
        {
            if (shift > 0)
            {
                object subPTail;
                var newChild = PopTail(shift - 5, (object[])arr[arr.Length - 1], pTail, out subPTail);

                if (newChild != null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, 0, ret, 0, arr.Length);

                    ret[arr.Length - 1] = newChild;

                    newPTail = subPTail;
                    return ret;
                }

                newPTail = subPTail;
            }
            else if (shift == 0)
            {
                newPTail = arr[arr.Length - 1];
            }
            else
            {
                newPTail = pTail;
            }

            // contraction
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
            Array.Copy(elems, 0, back, 0, back.Length);

            return back;
        }

        private object[] PushTail(int level, object[] arr, object[] tailNode, out object expansion)
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
                    Array.Copy(arr, 0, ret, 0, arr.Length);

                    ret[arr.Length - 1] = newInnerChild;

                    expansion = null;
                    return ret;
                }

                newChild = subExpansion;
            }

            // expansion
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
            Array.Copy(arr, 0, ret, 0, arr.Length);

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

        public IEnumerable<T> GetRightToLeftEnumeration()
        {
            // Specialize Enumeration to different tree heights gives a ~3x improvement over just indexing
            int depth = (int)Math.Ceiling(Math.Log(m_Length, 32));

            if (depth == 2)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        yield return (T)arr1[i2];
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
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            yield return (T)arr2[i3];
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
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                yield return (T)arr3[i4];
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
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                {
                                    yield return (T)arr4[i5];
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
                                    var arr5 = (object[])arr4[i5];
                                    for (int i6 = 0; i6 < arr5.Length; i6++)
                                    {
                                        yield return (T)arr5[i6];
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
                                        var arr6 = (object[])arr5[i6];
                                        for (int i7 = 0; i7 < arr6.Length; i7++)
                                        {
                                            yield return (T)arr6[i7];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < m_Tail.Length; i++)
                yield return (T)m_Tail[i];
        }

        public IEnumerable<T> GetLeftToRightEnumeration()
        {
            // Specialize Enumeration to different tree heights gives a ~3x improvement over just indexing
            // jeepers the backwards enumerator takes 134 lines of code!!
            int depth = (int)Math.Ceiling(Math.Log(m_Length, 32));

            for (int i = m_Tail.Length - 1; i >= 0; i--)
                yield return (T)m_Tail[i];

            if (depth == 2)
            {
                for (int i1 = m_Root.Length - 1; i1 >= 0; i1--)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = arr1.Length - 1; i2 >= 0; i2--)
                    {
                        yield return (T)arr1[i2];
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
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = arr2.Length - 1; i3 >= 0; i3--)
                        {
                            yield return (T)arr2[i3];
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
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = arr3.Length - 1; i4 >= 0; i4--)
                            {
                                yield return (T)arr3[i4];
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
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = arr4.Length - 1; i5 >= 0; i5--)
                                {
                                    yield return (T)arr4[i5];
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
                                    var arr5 = (object[])arr4[i5];
                                    for (int i6 = arr5.Length - 1; i6 >= 0; i6--)
                                    {
                                        yield return (T)arr5[i6];
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
                                        var arr6 = (object[])arr5[i6];
                                        for (int i7 = arr6.Length - 1; i7 >= 0; i7--)
                                        {
                                            yield return (T)arr6[i7];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public PrependableImmutableVector<T> Concat(IEnumerable<T> other)
        {
            var newVector = new PrependableImmutableVector<T>();
            foreach (var item in other.Reverse())
                newVector = newVector.Prepend(item);
            foreach (var item in GetRightToLeftEnumeration())
                newVector = newVector.Prepend(item);
            return newVector;
        }

        public PrependableImmutableVector<T> Filter(Func<T, bool> pred)
        {
            // prepending to a new vector seems dumb... 
            var matching = new PrependableImmutableVector<T>();
            foreach (var item in GetRightToLeftEnumeration())
                if (pred(item))
                    matching = matching.Prepend(item);
            return matching;
        }

        public PrependableImmutableVector<A> FlatMap<A>(Func<T, IEnumerable<A>> mapper)
        {
            var mapped = new PrependableImmutableVector<A>();
            foreach (var item in GetRightToLeftEnumeration())
                foreach (var newElement in mapper(item).Reverse())
                    mapped = mapped.Prepend(newElement);
            return mapped;
        }

        public PrependableImmutableVector<A> Map<A>(Func<T, A> mapper)
        {
            var mapped = new PrependableImmutableVector<A>();
            foreach (var item in GetRightToLeftEnumeration())
                mapped = mapped.Prepend(mapper(item));
            return mapped;
        }

        public PrependableImmutableVector<Tuple<T, A>> Zip<A>(IVector<A> that)
        {
            var zipped = new PrependableImmutableVector<Tuple<T, A>>();

            // This enumerator crap should be faster than the indexing version
            var thisEnumerator = this.GetRightToLeftEnumeration().GetEnumerator();
            var thatEnumerator = that.FastRightToLeftEnumeration.GetEnumerator();

            while (thisEnumerator.MoveNext() && thatEnumerator.MoveNext())
                zipped = zipped.Prepend(Tuple.Create(thisEnumerator.Current, thatEnumerator.Current));

            return zipped;
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
            get { return Pop(); }
        }

        IVector<T> IVector<T>.Cons(T item)
        {
            return Prepend(item);
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
            get
            {
                // is there like a windowed / projection way to get fast slice?
                var newVec = new PrependableImmutableVector<T>();
                var enumerator = GetRightToLeftEnumeration().GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                    newVec = newVec.Prepend(enumerator.Current);
                return newVec;
            }
        }

        IVector<T> IVector<T>.Append(T newItem)
        {
            var newVec = new PrependableImmutableVector<T>();
            newVec = newVec.Prepend(newItem);
            foreach (var item in GetRightToLeftEnumeration())
                newVec = newVec.Prepend(item);
            return newVec;
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
    }
}
