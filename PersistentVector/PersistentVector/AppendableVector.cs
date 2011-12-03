using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVectors
{
    // Port of Clojure's PersistentVector (by Rich Hickey) by way of Daniel Spiewak's Scala version
    internal class AppendableImmutableVector<T> : IVector<T>
    {
        private readonly int m_TailOffset;
        private readonly int m_Shift;
        private readonly int m_Length;
        private readonly object[] m_Root;
        private readonly object[] m_Tail;

        private AppendableImmutableVector(int length, int shift, object[] root, object[] tail)
        {
            m_TailOffset = length - tail.Length;
            m_Length = length;
            m_Shift = shift;
            m_Root = root;
            m_Tail = tail;
        }

        public AppendableImmutableVector() : this(0, 5, new object[0], new object[0]) { }

        public T this[int i]
        {
            get
            {
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

        public AppendableImmutableVector<T> Update(int i, T obj)
        {
            if (i >= 0 && i < m_Length)
            {
                if (i >= m_TailOffset)
                {
                    var newTail = new object[m_Tail.Length];
                    Array.Copy(m_Tail, 0, newTail, 0, m_Tail.Length);
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
                var newTail = new object[m_Tail.Length + 1];
                Array.Copy(m_Tail, 0, newTail, 0, m_Tail.Length);
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

            return new AppendableImmutableVector<T>(m_Length + 1, newShift, newRoot, NewArray(obj));
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
                var newTail = new object[m_Tail.Length - 1];
                Array.Copy(m_Tail, 0, newTail, 0, newTail.Length);

                return new AppendableImmutableVector<T>(m_Length - 1, m_Shift, m_Root, newTail);
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

                return new AppendableImmutableVector<T>(m_Length - 1, newShift, newRoot, (object[])pTail);
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

        public AppendableImmutableVector<T> Concat(IEnumerable<T> other)
        {
            var currentVector = this;
            foreach (var item in other)
                currentVector = currentVector.Append(item);
            return currentVector;
        }

        public AppendableImmutableVector<T> Filter(Func<T, bool> pred)
        {
            // appending to a new vector is dumb... 
            // we need shortcuts to build from arrays, all this copy on write is deadly
            var matching = new AppendableImmutableVector<T>();
            foreach (var item in this)
                if (pred(item))
                    matching = matching.Append(item);
            return matching;
        }

        public AppendableImmutableVector<A> FlatMap<A>(Func<T, IEnumerable<A>> mapper)
        {
            var mapped = new AppendableImmutableVector<A>();
            foreach (var item in this)
                foreach (var newElement in mapper(item))
                    mapped = mapped.Append(newElement);
            return mapped;
        }

        public AppendableImmutableVector<A> Map<A>(Func<T, A> mapper)
        {
            var mapped = new AppendableImmutableVector<A>();
            foreach (var item in this)
                mapped = mapped.Append(mapper(item));
            return mapped;
        }

        public AppendableImmutableVector<Tuple<T, A>> Zip<A>(IVector<A> that)
        {
            var zipped = new AppendableImmutableVector<Tuple<T, A>>();

            if (this.m_Length == 0 || that.Length == 0)
                return zipped;

            // This enumerator crap should be faster than the indexing version
            var thisEnumerator = ((IEnumerable<T>)this).GetEnumerator();
            var thatEnumerator = that.GetEnumerator();

            zipped = zipped.Append(Tuple.Create(thisEnumerator.Current, thatEnumerator.Current));

            while (thisEnumerator.MoveNext() && thatEnumerator.MoveNext())
                zipped = zipped.Append(Tuple.Create(thisEnumerator.Current, thatEnumerator.Current));

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
            // Specializing Enumeration to different tree heights gives a ~3x improvement over just indexing
            int depth = (int)Math.Ceiling(Math.Log(m_Length, 32));

            if (depth == 2)
            {
                foreach (var arr1 in m_Root)
                    foreach (var el in (object[])arr1)
                        yield return (T)el;
            }
            else if (depth == 3)
            {
                foreach (var arr1 in m_Root)
                    foreach (var arr2 in (object[])arr1)
                        foreach (var el in (object[])arr2)
                            yield return (T)el;
            }
            else if (depth == 4)
            {
                foreach (var arr1 in m_Root)
                    foreach (var arr2 in (object[])arr1)
                        foreach (var arr3 in (object[])arr2)
                            foreach (var el in (object[])arr3)
                                yield return (T)el;
            }
            else if (depth == 5)
            {
                foreach (var arr1 in m_Root)
                    foreach (var arr2 in (object[])arr1)
                        foreach (var arr3 in (object[])arr2)
                            foreach (var arr4 in (object[])arr3)
                                foreach (var el in (object[])arr4)
                                    yield return (T)el;
            }
            else if (depth == 6)
            {
                foreach (var arr1 in m_Root)
                    foreach (var arr2 in (object[])arr1)
                        foreach (var arr3 in (object[])arr2)
                            foreach (var arr4 in (object[])arr3)
                                foreach (var arr5 in (object[])arr4)
                                    foreach (var el in (object[])arr5)
                                        yield return (T)el;
            }
            else if (depth == 7)
            {
                foreach (var arr1 in m_Root)
                    foreach (var arr2 in (object[])arr1)
                        foreach (var arr3 in (object[])arr2)
                            foreach (var arr4 in (object[])arr3)
                                foreach (var arr5 in (object[])arr4)
                                    foreach (var arr6 in (object[])arr5)
                                        foreach (var el in (object[])arr6)
                                            yield return (T)el;
            }

            foreach (var tlItem in m_Tail)
                yield return (T)tlItem;
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
                // this doesn't have to be this slow... check out the windowed VectorProjection version??
                return new AppendableImmutableVector<T>().Concat(this.Skip(1));
            }
        }

        IVector<T> IVector<T>.Cons(T item)
        {
            return new AppendableImmutableVector<T>().Append(item).Concat(this);
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
            // would using backwards enumerator be faster?
            // Good idea: make all vectors have forwards and backwards enumerators!!!
            for (int i = m_Length - 1; i >= 0; i--)
                currentValue = accumulator(currentValue, this[i]);
            return currentValue;
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            var enumerator = ((IEnumerable<T>)this).GetEnumerator();
            var seed = enumerator.Current;
            while (enumerator.MoveNext())
                seed = accumulator(seed, enumerator.Current);
            return seed;
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
