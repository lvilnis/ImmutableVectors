using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVectors
{
    // Port of Clojure's PersistentVector (by Rich Hickey) by way of Daniel Spiewak's Scala version
    public class AppendableImmutableVector<T> : IVector<T>
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
                    var level = m_Shift;

                    while (level > 0)
                    {
                        arr = (object[])arr[(i >> level) & 0x01f];
                        level -= 5;
                    }

                    return (T)arr[i & 0x01f];
                }

                throw new Exception("Index out of bounds!");
            }
            set
            {
                throw new NotSupportedException();
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

                return new AppendableImmutableVector<T>(m_Length, m_Shift, DoAssoc(m_Shift, m_Root, i, obj), m_Tail);

            }
            if (i == m_Length)
            {
                return Append(obj);
            }

            throw new Exception();
        }

        public AppendableImmutableVector<T> Concat(IEnumerable<T> other)
        {
            var currentVector = this;
            foreach (var item in other)
                currentVector = currentVector.Append(item);
            return currentVector;
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

            var pushResults = PushTail(m_Shift - 5, m_Root, m_Tail);
            var newRoot = pushResults.Item1;
            var expansion = pushResults.Item2;

            var newShift = m_Shift;

            if (expansion != null)
            {
                newRoot = array(newRoot, expansion);
                newShift += 5;
            }

            return new AppendableImmutableVector<T>(m_Length + 1, newShift, newRoot, array(obj));
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
                var popResults = PopTail(m_Shift - 5, m_Root, null);
                var newRoot = popResults.Item1;
                var pTail = popResults.Item2;

                var newShift = m_Shift;

                if (newRoot == null)
                {
                    newRoot = new object[0];
                }

                if (m_Shift > 5 && newRoot.Length == 1)
                {
                    newRoot = (object[])newRoot[0];
                    newShift -= 5;
                }

                return new AppendableImmutableVector<T>(m_Length - 1, newShift, newRoot, (object[])pTail);
            }
        }

        public AppendableImmutableVector<T> Filter(Func<T, bool> pred)
        {
            // won't this lead to nlogn iteration?
            // can't we just grab everything and iterate over it in O(n)?
            // also, appending shit to a new vector seems dumb... 
            var matching = new AppendableImmutableVector<T>();
            for (int i = 0; i < m_Length; i++)
            {
                var el = this[i];
                if (pred(el))
                    matching = matching.Append(el);
            }
            return matching;
        }

        public AppendableImmutableVector<A> FlatMap<A>(Func<T, IEnumerable<A>> mapper)
        {
            var mapped = new AppendableImmutableVector<A>();
            for (int i = 0; i < m_Length; i++)
            {
                var el = this[i];
                foreach (var newElement in mapper(el))
                    mapped = mapped.Append(newElement);
            }
            return mapped;
        }

        public AppendableImmutableVector<A> Map<A>(Func<T, A> mapper)
        {
            var mapped = new AppendableImmutableVector<A>();
            for (int i = 0; i < m_Length; i++)
                mapped = mapped.Append(mapper(this[i]));
            return mapped;
        }

        public AppendableImmutableVector<Tuple<T, A>> Zip<A>(IVector<A> that)
        {
            var zipped = new AppendableImmutableVector<Tuple<T, A>>();
            for (int i = 0; i < Math.Min(this.m_Length, that.Length); i++)
                zipped = zipped.Append(Tuple.Create(this[i], that[i]));
            return zipped;
        }

        #region Private helpers

        private Tuple<object[], object> PopTail(int shift, object[] arr, object pTail)
        {
            object newPTail = null;

            if (shift > 0)
            {
                var popResults = PopTail(shift - 5, (object[])arr[arr.Length - 1], pTail);
                var newChild = popResults.Item1;
                var subPTail = popResults.Item2;

                if (newChild != null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, 0, ret, 0, arr.Length);

                    ret[arr.Length - 1] = newChild;

                    return Tuple.Create(ret, subPTail);
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
                return Tuple.Create((object[])null, newPTail);
            }
            else
            {
                var ret = new object[arr.Length - 1];
                Array.Copy(arr, 0, ret, 0, ret.Length);

                return Tuple.Create(ret, newPTail);
            }
        }

        private object[] array(params object[] elems)
        {
            var back = new object[elems.Length];
            Array.Copy(elems, 0, back, 0, back.Length);

            return back;
        }

        private Tuple<object[], object> PushTail(int level, object[] arr, object[] tailNode)
        {
            object newChild = null;

            if (level == 0)
            {
                newChild = tailNode;
            }
            else
            {
                var pushResults = PushTail(level - 5, (object[])arr[arr.Length - 1], tailNode);
                var newChild2 = pushResults.Item1;
                var subExpansion = pushResults.Item2;

                if (subExpansion == null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, 0, ret, 0, arr.Length);

                    ret[arr.Length - 1] = newChild2;

                    return Tuple.Create(ret, (object)null);
                }

                newChild = subExpansion;
            }

            // expansion
            if (arr.Length == 32)
            {
                return Tuple.Create(arr, (object)array(newChild));
            }
            else
            {
                var ret = new object[arr.Length + 1];
                Array.Copy(arr, 0, ret, 0, arr.Length);
                ret[arr.Length] = newChild;

                return Tuple.Create(ret, (object)null);
            }
        }

        private object[] DoAssoc(int level, object[] arr, int i, T obj)
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
                ret[subidx] = DoAssoc(level - 5, (object[])arr[subidx], i, obj);
            }

            return ret;
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
            for (int i = m_Length; i >= 0; i--)
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
