﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVectors
{
    public static class Vector
    {
        public static IVector<T> Appendable<T>(params T[] items)
        {
            return new AppendableImmutableVector<T>().Concat(items);
        }
        public static IVector<T> Appendable<T>(IList<T> items)
        {
            return new AppendableImmutableVector<T>().Concat(items);
        }
        public static IVector<T> Appendable<T>(IEnumerable<T> items)
        {
            return new AppendableImmutableVector<T>().Concat(items);
        }

        public static IVector<T> Prependable<T>(params T[] items)
        {
            return new PrependableImmutableVector<T>().Concat(items);
        }
        public static IVector<T> Prependable<T>(IList<T> items)
        {
            return new PrependableImmutableVector<T>().Concat(items);
        }
        public static IVector<T> Prependable<T>(IEnumerable<T> items)
        {
            return new PrependableImmutableVector<T>().Concat(items);
        }

        public static IVector<T> ArrayListBacked<T>(params T[] items)
        {
            return new ArrayListBackedVector<T>(items);
        }
        public static IVector<T> ArrayListBacked<T>(IList<T> items)
        {
            return new ArrayListBackedVector<T>(items);
        }
        public static IVector<T> ArrayListBacked<T>(IEnumerable<T> items)
        {
            return new ArrayListBackedVector<T>(items.ToList());
        }

        public static IVector<T> Deque<T>(params T[] items)
        {
            throw new NotImplementedException("I haven't used 2-3 finger trees to make a deque yet! Quit hasslin' me! Note to self: use 2-3 finger trees to make a deque.");
        }
        public static IVector<T> Deque<T>(IList<T> items)
        {
            throw new NotImplementedException("I haven't used 2-3 finger trees to make a deque yet! Quit hasslin' me! Note to self: use 2-3 finger trees to make a deque.");
        }
        public static IVector<T> Deque<T>(IEnumerable<T> items)
        {
            throw new NotImplementedException("I haven't used 2-3 finger trees to make a deque yet! Quit hasslin' me! Note to self: use 2-3 finger trees to make a deque.");
        }
    }
}
