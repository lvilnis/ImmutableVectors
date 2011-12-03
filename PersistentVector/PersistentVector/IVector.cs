using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PersistentVectors
{
    public interface IVector<T> : IEnumerable<T>, IEquatable<IVector<T>>
    {
        int Length { get; }

        // Indexable      
        T this[int i] { get; }
        IVector<T> Update(int index, T newVal);

        // Appendable
        T End { get; }
        IVector<T> Popped { get; }
        IVector<T> Append(T item);

        // Prependable
        T Head { get; }
        IVector<T> Tail { get; }
        IVector<T> Cons(T item);

        // Concatable
        IVector<T> Concat(IEnumerable<T> items);

        // HOFs and such
        // Probably can pull some/all of these out to extn methods
        // with casts to specialize implementations for ArrayLists
        IVector<T> Filter(Func<T, bool> pred);
        IVector<U> Map<U>(Func<T, U> mapper);
        IVector<U> FlatMap<U>(Func<T, IEnumerable<U>> mapper);
        TAcc Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed);
        TAcc Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed);
        T Reduce(Func<T, T, T> accumulator);
        IVector<Tuple<T, U>> Zip<U>(IVector<U> that);

        IEnumerable<T> FastRightToLeftEnumeration { get; }

        // need to add Slice / Window
    }
}
