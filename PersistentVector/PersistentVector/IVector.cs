using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PersistentVector
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

        // Concatable. Need a fast version of this, really bad
        IVector<T> Concat(IEnumerable<T> items);

        // "Contract" idea: if we allocate a whole new array to do an operation
        // should we have a version of the operation that returns just the array?
        // Also could add SliceEnumerator to efficiently grab segments
        T[] SliceToArray(int start, int length);
        IVector<T> Slice(int start, int length);
        
        // HOFs and such
        // Probably can pull some/all of these out to extn methods
        IVector<T> Filter(Func<T, bool> pred);
        IVector<U> Map<U>(Func<T, U> mapper);
        IVector<U> FlatMap<U>(Func<T, IEnumerable<U>> mapper);
        TAcc Foldl<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed);
        TAcc Foldr<TAcc>(Func<TAcc, T, TAcc> accumulator, TAcc seed);
        T Reduce(Func<T, T, T> accumulator);
        IVector<Tuple<T, U>> Zip<U>(IVector<U> that);

        IEnumerable<T> FastRightToLeftEnumeration { get; }

        T[] FastToArray();

        // Kind of weird little hack so we can make xtn methods return
        // same type of array as their argument...
        IVector<U> New<U>(params U[] items);

        // NOTE: I'm probably going to have to add more memory-conservative operations
        // or just use more virtualizing IVector implementations
    }
}
