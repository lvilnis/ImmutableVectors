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

        T[] FastToArray();

        // need to add Slice / Window
    }

    public static class VectorLinqExtensionOverloads
    {
        // WARNING: Using vectors with LINQ will evaluate strictly unlike with IEnumerable
        // Also, we still need a VectorProjection class that lazily maps vector elements

        public static IVector<U> Select<T, U>(this IVector<T> vec, Func<T, U> mapper)
        {
            return vec.Map(mapper);
        }

        public static IVector<U> SelectMany<T, U>(this IVector<T> vec, Func<T, IVector<U>> mapper)
        {
            return vec.FlatMap(mapper);
        }

        public static IVector<T> Where<T>(this IVector<T> vec, Func<T, bool> pred)
        {
            return vec.Filter(pred);
        }

        public static IVector<T> OrderBy<T, U>(this IVector<T> vec, Func<T, U> keySelector)
        {
            var elements = vec.ToArray();
            var keys = vec.Map(keySelector).ToArray();
            Array.Sort(keys, elements);
            return Vector.Appendable(elements);
        }

        public static IVector<T> OrderByDescending<T, U>(this IVector<T> vec, Func<T, U> keySelector)
        {
            var elements = vec.ToArray();
            var keys = vec.Map(keySelector).ToArray();
            Array.Sort(keys, elements);
            Array.Reverse(elements);
            return Vector.Appendable(elements);
        }

        public static IEnumerable<T> Reverse<T>(this IVector<T> vec)
        {
            return vec.FastRightToLeftEnumeration;
        }

        public static T[] ToArray<T>(this IVector<T> vec)
        {
            return vec.FastToArray();
        }

        public static IList<T> ToList<T>(this IVector<T> vec)
        {
            return new List<T>(vec.FastToArray());
        }

        // Probably don't need to implement first and single...

        public static T FirstOrDefault<T>(this IVector<T> vec)
        {
            return vec.Length > 0 ? vec.Head : default(T);
        }

        public static T SingleOrDefault<T>(this IVector<T> vec)
        {
            return vec.Length == 1 ? vec.Head : default(T);
        }
    }

    public static class VectorExtensions
    {
        public static IVector<T> ToVector<T>(this IEnumerable<T> items)
        {
            return Vector.Appendable(items.ToArray());
        }
    }
}
