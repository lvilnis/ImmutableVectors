using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    public static class VectorLinqExtensionOverloads
    {
        // WARNING: Using vectors with LINQ will evaluate strictly unlike with IEnumerable

        public static IVector<U> Select<T, U>(this IVector<T> vec, Func<T, U> mapper)
        {
            return vec.Map(mapper);
        }

        public static IVector<V> SelectMany<T, U, V>(this IVector<T> vec, Func<T, IVector<U>> from, Func<T, U, V> select)
        {
            var resultList = new List<V>(vec.Length);
            foreach (var outer in vec)
                foreach (var inner in from(outer))
                    resultList.Add(select(outer, inner));

            return vec.New(resultList.ToArray());
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
            return vec.New(elements);
        }

        public static IVector<T> OrderBy<T>(this IVector<T> vec)
        {
            var elements = vec.ToArray();
            Array.Sort(elements);
            return vec.New(elements);
        }

        public static IVector<T> OrderByDescending<T, U>(this IVector<T> vec, Func<T, U> keySelector)
        {
            var elements = vec.ToArray();
            var keys = vec.Map(keySelector).ToArray();
            Array.Sort(keys, elements);
            Array.Reverse(elements);
            return vec.New(elements);
        }

        public static IVector<T> OrderByDescending<T, U>(this IVector<T> vec)
        {
            var elements = vec.ToArray();
            Array.Sort(elements);
            Array.Reverse(elements);
            return vec.New(elements);
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

        public static T ElementAt<T>(this IVector<T> vec, int idx)
        {
            return vec[idx];
        }

        public static T ElementAtOrDefault<T>(this IVector<T> vec, int idx)
        {
            return idx > vec.Length - 1 ? default(T) : vec[idx];
        }

        public static int Count<T>(this IVector<T> vec)
        {
            return vec.Length;
        }

        // is my fold even faster than the .net fold?? Probably not. Need to measure...
        public static TAcc Aggregate<T, TAcc>(this IVector<T> vec, TAcc seed, Func<TAcc, T, TAcc> combiner)
        {
            return vec.Foldl(combiner, seed);
        }

        // try doing skip, take etc?

        public static T Aggregate<T>(this IVector<T> vec, Func<T, T, T> combiner)
        {
            return vec.Reduce(combiner);
        }

        // Probably don't need to implement first, single, etc...

        public static T LastOrDefault<T>(this IVector<T> vec)
        {
            return vec.Length > 0 ? vec.End : default(T);
        }

        public static T FirstOrDefault<T>(this IVector<T> vec)
        {
            return vec.Length > 0 ? vec.Head : default(T);
        }

        public static T SingleOrDefault<T>(this IVector<T> vec)
        {
            return vec.Length == 1 ? vec.Head : default(T);
        }

        // Should specialize max, min, sum and other folds to use for loops over the array?
        // What about all, any, etc... Though, the default implementations for all this
        // are probably the same as I would do (just enumerate and combine)

        public static T Last<T>(this IVector<T> vec)
        {
            return vec.Length > 0 ? vec.End : Throw<T>("Vector must be non-empty to use Last!");
        }

        public static T First<T>(this IVector<T> vec)
        {
            return vec.Length > 0 ? vec.Head : Throw<T>("Vector must be non-empty to use First!");
        }

        public static T Single<T>(this IVector<T> vec)
        {
            return vec.Length == 1 ? vec.Head : Throw<T>("Vector must have exactly one item to use Single!");
        }

        private static T Throw<T>(string message)
        {
            throw new Exception(message);
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
