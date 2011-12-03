using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVectors
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

        public PrependableImmutableVector<T> Concat(IEnumerable<T> other)
        {
            var newVector = new PrependableImmutableVector<T>();
            foreach (var item in other.Reverse())
                newVector = newVector.Prepend(item);
            for (int i = m_Length - 1; i >= 0; i--)
                newVector = newVector.Prepend(this[i]);
            return newVector;
        }

        public PrependableImmutableVector<T> Filter(Func<T, bool> pred)
        {
            // won't this lead to nlogn iteration?
            // can't we just grab everything and iterate over it in O(n)?
            // also, appending shit to a new vector seems dumb... 
            var matching = new PrependableImmutableVector<T>();
            for (int i = m_Length - 1; i >= 0; i--)
            {
                var el = this[i];
                if (pred(el))
                    matching = matching.Prepend(el);
            }
            return matching;
        }

        public PrependableImmutableVector<A> FlatMap<A>(Func<T, IEnumerable<A>> mapper)
        {
            var mapped = new PrependableImmutableVector<A>();
            for (int i = m_Length - 1; i >= 0; i--)
            {
                var el = this[i];
                foreach (var newElement in mapper(el).Reverse())
                    mapped = mapped.Prepend(newElement);
            }
            return mapped;
        }

        public PrependableImmutableVector<A> Map<A>(Func<T, A> mapper)
        {
            var mapped = new PrependableImmutableVector<A>();
            for (int i = m_Length - 1; i >= 0; i--)
                mapped = mapped.Prepend(mapper(this[i]));
            return mapped;
        }

        public PrependableImmutableVector<Tuple<T, A>> Zip<A>(IVector<A> that)
        {
            var zipped = new PrependableImmutableVector<Tuple<T, A>>();
            for (int i = Math.Min(this.m_Length, that.Length) - 1; i >= 0; i--)
                zipped = zipped.Prepend(Tuple.Create(this[i], that[i]));
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
            for (int i = 0; i < m_Length; i++)
                yield return this[i];
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
                // this doesn't have to be this slow... check out the windowed VectorProjection version??
                var newVec = new PrependableImmutableVector<T>();
                for (int i = m_Length - 2; i >= 0; i--)
                    newVec = newVec.Prepend(this[i]);
                return newVec;
            }
        }

        IVector<T> IVector<T>.Append(T item)
        {
            var newVec = new PrependableImmutableVector<T>();
            newVec = newVec.Prepend(item);
            for (int i = m_Length - 1; i >= 0; i--)
                newVec = newVec.Prepend(this[i]);
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
            for (int i = m_Length - 1; i >= 0; i--)
                currentValue = accumulator(currentValue, this[i]);
            return currentValue;
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            var seed = this[0];
            for (int i = 1; i < m_Length; i++)
                seed = accumulator(seed, this[i]);
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
