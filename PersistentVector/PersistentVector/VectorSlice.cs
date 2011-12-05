using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    // TBD finish implementing all these methods, add efficient versions of HOFs and
    // such that avoid consing up intermediate vectors and look directly at the sliced array
    public class VectorSlice<T> : IVector<T>
    {
        private IVector<T> m_WrappedVector;
        private int m_Start;
        private int m_Length;

        public VectorSlice(IVector<T> wrappedVector, int start, int length)
        {
            if (start + length > wrappedVector.Length)
                throw new Exception("Slice is too big!");
            if (length < 0)
                throw new Exception("Length of slice must be non-negative!");
            if (start < 0 || start >= wrappedVector.Length)
                throw new Exception("Start of slice must be valid index!");

            m_WrappedVector = wrappedVector;
            m_Start = start;
            m_Length = length;
        }

        int IVector<T>.Length
        {
            get { return m_Length; }
        }

        T IVector<T>.this[int i]
        {
            get
            {
                if (0 <= i && i < m_Length)
                    return m_WrappedVector[m_Start + i];

                throw new Exception("Invalid index!");
            }
        }

        // Need to write a custom fast Slice method as an extension of FastToArray
        // That only copies the necessary items. Should be pretty easy just get outline mod32

        IVector<T> IVector<T>.Update(int index, T newVal)
        {
            return new VectorSlice<T>(m_WrappedVector.Update(index, newVal), m_Start, m_Length);
        }

        T IVector<T>.End
        {
            get { return m_WrappedVector[m_Start + m_Length - 1]; }
        }

        // TBD: special case these if slice extends to either end?
        // But then we might force copy if its not the favored end...

        IVector<T> IVector<T>.Popped
        {
            get { return m_WrappedVector.Slice(m_Start, m_Length - 1); }
        }

        IVector<T> IVector<T>.Append(T item)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Append(item);
        }

        T IVector<T>.Head
        {
            get { return m_WrappedVector[m_Start]; }
        }

        IVector<T> IVector<T>.Tail
        {
            get { return m_WrappedVector.Slice(m_Start + 1, m_Length - 1); }
        }

        IVector<T> IVector<T>.Cons(T item)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Cons(item);
        }

        // still need a fast concat that takes a whole array...
        // not that complicated just a big pain to write

        IVector<T> IVector<T>.Concat(IEnumerable<T> items)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Concat(items);
        }

        IVector<T> IVector<T>.Filter(Func<T, bool> pred)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Filter(pred);
        }

        IVector<U> IVector<T>.Map<U>(Func<T, U> mapper)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Map(mapper);
        }

        IVector<U> IVector<T>.FlatMap<U>(Func<T, IEnumerable<U>> mapper)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).FlatMap(mapper);
        }

        TAcc IVector<T>.Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Foldl(accumulator, seed);
        }

        TAcc IVector<T>.Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Foldr(accumulator, seed);
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Reduce(accumulator);
        }

        IVector<Tuple<T, U>> IVector<T>.Zip<U>(IVector<U> that)
        {
            return m_WrappedVector.Slice(m_Start, m_Length).Zip(that);
        }

        IEnumerable<T> IVector<T>.FastRightToLeftEnumeration
        {
            get { throw new NotImplementedException(); }
        }

        T[] IVector<T>.FastToArray()
        {
            // this could be very slightly faster by ToArray-ing then mapping, but probs not worth it
            return m_WrappedVector.SliceToArray(m_Start, m_Length);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return (IEnumerator<T>)m_WrappedVector.Slice(m_Start, m_Length).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        bool IEquatable<IVector<T>>.Equals(IVector<T> other)
        {
            // This is nasty because to really compare properly we need to pull the whole collection
            // through the map. Seems like a good case for memoizing or something....
            return m_WrappedVector.Slice(m_Start, m_Length).Equals(other.ToArray());
        }

        IVector<U> IVector<T>.New<U>(params U[] items)
        {
            // punt for now...
            return Vector.Appendable(items);
        }

        T[] IVector<T>.SliceToArray(int start, int length)
        {
            throw new NotImplementedException();
        }

        IVector<T> IVector<T>.Slice(int start, int length)
        {
            throw new NotImplementedException();
        }
    }
}
