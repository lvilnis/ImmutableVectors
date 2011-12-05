using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    public class VectorProjection<S, T> : IVector<T>
    {
        private Func<S, T> m_MapFunction;
        private IVector<S> m_WrappedVector;
        // Don't memoize results of map for now
        public VectorProjection(IVector<S> wrappedVector, Func<S, T> mapFunction)
        {
            m_WrappedVector = wrappedVector;
            m_MapFunction = mapFunction;
        }

        int IVector<T>.Length
        {
            get { return m_WrappedVector.Length; }
        }

        T IVector<T>.this[int i]
        {
            get { return m_MapFunction(m_WrappedVector[i]); }
        }

        IVector<T> IVector<T>.Update(int index, T newVal)
        {
            // WARNING: updating a VectorProjection will strictly map the entire vector
            return m_WrappedVector.Map(m_MapFunction).Update(index, newVal);
        }

        T IVector<T>.End
        {
            get { return m_MapFunction(m_WrappedVector.End); }
        }

        IVector<T> IVector<T>.Popped
        {
            get { return new VectorProjection<S, T>(m_WrappedVector.Popped, m_MapFunction); }
        }

        IVector<T> IVector<T>.Append(T item)
        {
            return m_WrappedVector.Map(m_MapFunction).Append(item);
        }

        T IVector<T>.Head
        {
            get { return m_MapFunction(m_WrappedVector.Head); }
        }

        IVector<T> IVector<T>.Tail
        {
            get { return new VectorProjection<S, T>(m_WrappedVector.Tail, m_MapFunction); }
        }

        IVector<T> IVector<T>.Cons(T item)
        {
            return m_WrappedVector.Map(m_MapFunction).Cons(item);
        }

        IVector<T> IVector<T>.Concat(IEnumerable<T> items)
        {
            return m_WrappedVector.Map(m_MapFunction).Concat(items);
        }

        // a bunch of these operations could be slightly faster by fusing the maps
        // or even fusing the loops in the more general case of filter, foldl, etc. Not a bad idea....

        IVector<T> IVector<T>.Filter(Func<T, bool> pred)
        {
            return m_WrappedVector.Map(m_MapFunction).Filter(pred);
        }

        IVector<U> IVector<T>.Map<U>(Func<T, U> mapper)
        {
            return new VectorProjection<S, U>(m_WrappedVector, el => mapper(m_MapFunction(el)));
        }

        IVector<U> IVector<T>.FlatMap<U>(Func<T, IEnumerable<U>> mapper)
        {
            return m_WrappedVector.Map(m_MapFunction).FlatMap(mapper);
        }

        TAcc IVector<T>.Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            return m_WrappedVector.Foldl((acc, el) => accumulator(acc, m_MapFunction(el)), seed);
        }

        TAcc IVector<T>.Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            return m_WrappedVector.Foldr((acc, el) => accumulator(acc, m_MapFunction(el)), seed);
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            return m_WrappedVector.Map(m_MapFunction).Reduce(accumulator);
        }

        IVector<Tuple<T, U>> IVector<T>.Zip<U>(IVector<U> that)
        {
            return m_WrappedVector.Map(m_MapFunction).Zip(that);
        }

        IEnumerable<T> IVector<T>.FastRightToLeftEnumeration
        {
            get { return m_WrappedVector.FastRightToLeftEnumeration.Select(m_MapFunction); }
        }

        T[] IVector<T>.FastToArray()
        {
            // this could be very slightly faster by ToArray-ing then mapping, but probs not worth it
            return m_WrappedVector.Map(m_MapFunction).ToArray();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_WrappedVector.Select(m_MapFunction).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        bool IEquatable<IVector<T>>.Equals(IVector<T> other)
        {
            // This is nasty because to really compare properly we need to pull the whole collection
            // through the map. Seems like a good case for memoizing or something....
            return m_WrappedVector.Map(m_MapFunction).Equals(other);
        }

        IVector<U> IVector<T>.New<U>(params U[] items)
        {
            // punt for now...
            return Vector.Appendable(items);
        }
    }
}
