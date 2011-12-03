using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    internal class ArrayListBackedVector<T> : IVector<T>
    {
        private readonly IList<T> m_List;
        public ArrayListBackedVector(IList<T> list)
        {
            m_List = list;
        }

        int IVector<T>.Length
        {
            get { return m_List.Count; }
        }

        T IVector<T>.this[int i]
        {
            get { return m_List[i]; }
        }

        IVector<T> IVector<T>.Update(int index, T newVal)
        {
            var newList = new List<T>(m_List.Count);
            for (int i = 0; i < m_List.Count; i++)
                newList.Add(i == index ? newVal : m_List[i]);
            return new ArrayListBackedVector<T>(newList);
        }

        T IVector<T>.End
        {
            get { return m_List[m_List.Count - 1]; }
        }

        IVector<T> IVector<T>.Popped
        {
            get
            {
                var newList = new List<T>(m_List.Count - 1);
                for (int i = 0; i < m_List.Count - 1; i++)
                    newList.Add(m_List[i]);
                return new ArrayListBackedVector<T>(newList);
            }
        }

        IVector<T> IVector<T>.Append(T newItem)
        {
            var newList = new List<T>(m_List.Count + 1);
            foreach (var item in m_List)
                newList.Add(item);
            newList.Add(newItem);
            return new ArrayListBackedVector<T>(newList);
        }

        T IVector<T>.Head
        {
            get { return m_List[0]; }
        }

        IVector<T> IVector<T>.Tail
        {
            get
            {
                var newList = new List<T>(m_List.Count - 1);
                for (int i = 1; i < m_List.Count; i++)
                    newList.Add(m_List[i]);
                return new ArrayListBackedVector<T>(newList);
            }
        }

        IVector<T> IVector<T>.Cons(T newItem)
        {
            var newList = new List<T>(m_List.Count + 1);
            newList.Add(newItem);
            foreach (var item in m_List)
                newList.Add(item);
            return new ArrayListBackedVector<T>(newList);
        }

        IVector<T> IVector<T>.Concat(IEnumerable<T> newItems)
        {
            var newList = new List<T>(m_List.Count);
            foreach (var item in m_List)
                newList.Add(item);
            foreach (var newItem in newItems)
                newList.Add(newItem);
            return new ArrayListBackedVector<T>(newList);
        }

        IVector<T> IVector<T>.Filter(Func<T, bool> pred)
        {
            var newList = new List<T>();
            foreach (var item in m_List)
                if (pred(item))
                    newList.Add(item);
            return new ArrayListBackedVector<T>(newList);
        }

        IVector<U> IVector<T>.Map<U>(Func<T, U> mapper)
        {
            var newList = new List<U>(m_List.Count);
            foreach (var item in m_List)
                newList.Add(mapper(item));
            return new ArrayListBackedVector<U>(newList);
        }

        IVector<U> IVector<T>.FlatMap<U>(Func<T, IEnumerable<U>> mapper)
        {
            var newList = new List<U>();
            foreach (var input in m_List)
                foreach (var item in mapper(input))
                    newList.Add(item);
            return new ArrayListBackedVector<U>(newList);
        }

        IVector<Tuple<T, U>> IVector<T>.Zip<U>(IVector<U> that)
        {
            int newLength = Math.Min(m_List.Count, that.Length);
            var newList = new List<Tuple<T, U>>(newLength);
            for (int i = 0; i < newLength; i++)
                newList.Add(Tuple.Create(m_List[i], that[i]));
            return new ArrayListBackedVector<Tuple<T, U>>(newList);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        TAcc IVector<T>.Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            var currentAcc = seed;
            foreach (var item in m_List)
                currentAcc = accumulator(currentAcc, item);
            return currentAcc;
        }

        TAcc IVector<T>.Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed)
        {
            var currentAcc = seed;
            for (int i = m_List.Count - 1; i >= 0; i--)
                currentAcc = accumulator(currentAcc, m_List[i]);
            return currentAcc;
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            var currentAcc = m_List[0];
            for (int i = 1; i < m_List.Count; i++)
                currentAcc = accumulator(currentAcc, m_List[i]);
            return currentAcc;
        }

        IEnumerable<T> IVector<T>.FastRightToLeftEnumeration
        {
            get
            {
                for (int i = m_List.Count - 1; i >= 0; i--)
                    yield return m_List[i];
            }
        }

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
