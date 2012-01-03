using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVector
{
    public class DequeVector<T> : IVector<T>
    {
        private readonly short m_Shift;
        private readonly int m_Length;
        private readonly object[] m_Root;

        private readonly T[] m_RightTail;
        private readonly T[] m_LeftTail;

        private DequeVector(int length, short shift, object[] root, T[] leftTail, T[] rightTail)
        {
            m_Length = length;
            m_Shift = shift;
            m_Root = root;
            m_LeftTail = leftTail;
            m_RightTail = rightTail;
        }

        public DequeVector() : this(0, 5, new object[0], new T[0], new T[0]) { }

        public T this[int i]
        {
            get
            {
                if (i >= 0 && i < m_Length)
                {
                    if (i < m_LeftTail.Length)
                        return m_LeftTail[i];

                    if (i >= m_Length - m_RightTail.Length)
                        return m_RightTail[i & 0x01f];

                    i -= m_LeftTail.Length;

                    var arr = m_Root;
                    for (int level = m_Shift; level > 5; level -= 5)
                        arr = (object[])arr[(i >> level) & 0x01f];

                    var leafArr = (T[])arr[(i >> 5) & 0x01f];

                    return leafArr[i & 0x01f];
                }

                throw new Exception("Index out of bounds!");
            }
        }

        // All the normal operations work the same as before, with idx - m_LeftTail.Length
        // except if update happens in the left tail, which we do with copy-on write

        private IEnumerable<T[]> GetLeftToRightBodyBlockEnumeration() // this returns lefttail, inner blocks, righttail
        {
            // Specialize Enumeration to different tree heights gives a ~3x improvement over just indexing
            int depth = GetDepthFromLength(m_Length - m_LeftTail.Length - m_RightTail.Length);

            //   yield return m_LeftTail;

            if (depth == 2)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                    yield return (T[])m_Root[i1];
            }
            else if (depth == 3)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                        yield return (T[])arr1[i2];
                }
            }
            else if (depth == 4)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                            yield return (T[])arr2[i3];
                    }
                }
            }
            else if (depth == 5)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                                yield return (T[])arr3[i4];
                        }
                    }
                }
            }
            else if (depth == 6)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                    yield return (T[])arr4[i5];
                            }
                        }
                    }
                }
            }
            else if (depth == 7)
            {
                for (int i1 = 0; i1 < m_Root.Length; i1++)
                {
                    var arr1 = (object[])m_Root[i1];
                    for (int i2 = 0; i2 < arr1.Length; i2++)
                    {
                        var arr2 = (object[])arr1[i2];
                        for (int i3 = 0; i3 < arr2.Length; i3++)
                        {
                            var arr3 = (object[])arr2[i3];
                            for (int i4 = 0; i4 < arr3.Length; i4++)
                            {
                                var arr4 = (object[])arr3[i4];
                                for (int i5 = 0; i5 < arr4.Length; i5++)
                                {
                                    var arr5 = (object[])arr4[i5];
                                    for (int i6 = 0; i6 < arr5.Length; i6++)
                                        yield return (T[])arr5[i6];
                                }
                            }
                        }
                    }
                }
            }

            //    yield return m_RightTail;
        }

        // member to make the prependable version work actually for real and fill right-to-left instead of storing things
        // in reversed order, so that we can share data between appendable and prependable without copying anything but the intermediate nodes

        public IVector<T> Update(int i, T obj)
        {
            if (i >= 0 && i < m_Length)
            {
                if (i >= m_Length - m_RightTail.Length)
                {
                    var newTail = new T[m_RightTail.Length];
                    Array.Copy(m_RightTail, newTail, m_RightTail.Length);
                    newTail[i & 0x01f] = obj;

                    return new DequeVector<T>(m_Length, m_Shift, m_Root, m_LeftTail, newTail);
                }
                else if (i < m_LeftTail.Length)
                {
                    var newTail = new T[m_LeftTail.Length];
                    Array.Copy(m_LeftTail, newTail, m_LeftTail.Length);
                    newTail[i & 0x01f] = obj;

                    return new DequeVector<T>(m_Length, m_Shift, m_Root, newTail, m_RightTail);
                }

                return new DequeVector<T>(m_Length, m_Shift, UpdateIndex(m_Shift, m_Root, i, obj), m_LeftTail, m_RightTail);
            }

            throw new Exception("Index out of bounds!");
        }

        public DequeVector<T> Append(T obj)
        {
            if (m_Length < 32)
            {
                var newLeftTail = new T[m_LeftTail.Length + 1];
                Array.Copy(m_LeftTail, newLeftTail, m_LeftTail.Length);
                newLeftTail[m_LeftTail.Length] = obj;

                return new DequeVector<T>(m_Length + 1, m_Shift, m_Root, newLeftTail, m_RightTail);
            }
            else if (m_RightTail.Length < 32)
            {
                var newTail = new T[m_RightTail.Length + 1];
                Array.Copy(m_RightTail, newTail, m_RightTail.Length);
                newTail[m_RightTail.Length] = obj;

                return new DequeVector<T>(m_Length + 1, m_Shift, m_Root, m_LeftTail, newTail);
            }

            object expansion;
            var newRoot = PushTail(m_Shift - 5, m_Root, m_RightTail, out expansion);

            var newShift = m_Shift;

            if (expansion != null)
            {
                newRoot = new object[] { newRoot, expansion };
                newShift += 5;
            }

            return new DequeVector<T>(m_Length + 1, newShift, newRoot, m_LeftTail, new T[] { obj });
        }

        public DequeVector<T> Pop()
        {
            if (m_Length == 0)
            {
                throw new Exception("Can't pop empty vector");
            }
            else if (m_Length == 1)
            {
                return new DequeVector<T>();
            }
            else if (m_Length < 33)
            {
                var newLeftTail = new T[m_LeftTail.Length - 1];
                Array.Copy(m_LeftTail, newLeftTail, newLeftTail.Length);

                return new DequeVector<T>(m_Length - 1, m_Shift, m_Root, newLeftTail, m_RightTail);
            }
            else if (m_RightTail.Length > 1)
            {
                var newTail = new T[m_RightTail.Length - 1];
                Array.Copy(m_RightTail, newTail, newTail.Length);

                return new DequeVector<T>(m_Length - 1, m_Shift, m_Root, m_LeftTail, newTail);
            }
            else
            {
                object pTail;
                var newRoot = PopTail(m_Shift - 5, m_Root, out pTail);

                var newShift = m_Shift;

                if (newRoot == null)
                    newRoot = new object[0];

                if (m_Shift > 5 && newRoot.Length == 1)
                {
                    newRoot = (object[])newRoot[0];
                    newShift -= 5;
                }

                return new DequeVector<T>(m_Length - 1, newShift, newRoot, m_LeftTail, (T[])pTail);
            }
        }

        public DequeVector<T> Prepend(T newVal)
        {
            if (m_LeftTail.Length < 32)
            {
                var newLeftTail = new T[m_LeftTail.Length + 1];
                newLeftTail[0] = newVal;
                Array.Copy(m_LeftTail, 0, newLeftTail, 1, m_LeftTail.Length);
                return new DequeVector<T>(m_Length + 1, m_Shift, m_Root, newLeftTail, m_RightTail);
            }
            else
            {
                // push left tail down - done with a full copy

                var newLeftTail = new T[1];
                newLeftTail[0] = newVal;
                return new DequeVector<T>(newLeftTail, GetLeftToRightBodyBlockEnumeration(), m_RightTail, m_Length + 1);
            }
        }

        public DequeVector<T> PopFront()
        {
            if (m_RightTail.Length == m_Length)
            {
                return new DequeVector<T>(new T[0], Enumerable.Empty<T[]>(), m_RightTail.Skip(1).ToArray(), m_Length - 1);
            }

            var bodyBlocks = GetLeftToRightBodyBlockEnumeration();
            var newLeftTail = m_LeftTail.Length > 0 ? m_LeftTail.Skip(1).ToArray() : bodyBlocks.First().Skip(1).ToArray();
            var newBodyBlocks = m_LeftTail.Length > 0 ? bodyBlocks : bodyBlocks.Skip(1);
            return new DequeVector<T>(newLeftTail, newBodyBlocks, m_RightTail, m_Length - 1);
        }

        #region Private helpers

        private object[] PopTail(int shift, object[] arr, out object pTail)
        {
            if (shift == 0)
            {
                pTail = arr[arr.Length - 1];
            }
            else
            {
                var newChild = PopTail(shift - 5, (object[])arr[arr.Length - 1], out pTail);

                if (newChild != null)
                {
                    var ret = new object[arr.Length];
                    Array.Copy(arr, ret, arr.Length);

                    ret[arr.Length - 1] = newChild;

                    return ret;
                }
            }

            // Contraction
            if (arr.Length == 1)
            {
                return null;
            }
            else
            {
                var ret = new object[arr.Length - 1];
                Array.Copy(arr, ret, ret.Length);

                return ret;
            }
        }

        private object[] PushTail(int level, object[] arr, T[] tailNode, out object expansion)
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
                    Array.Copy(arr, ret, arr.Length);

                    ret[arr.Length - 1] = newInnerChild;

                    expansion = null;
                    return ret;
                }

                newChild = subExpansion;
            }

            // Expansion
            if (arr.Length == 32)
            {
                expansion = new object[] { newChild };
                return arr;
            }
            else
            {
                var ret = new object[arr.Length + 1];
                Array.Copy(arr, ret, arr.Length);
                ret[arr.Length] = newChild;

                expansion = null;
                return ret;
            }
        }

        private object[] UpdateIndex(int level, object[] arr, int i, T obj)
        {
            var ret = new object[arr.Length];
            Array.Copy(arr, ret, arr.Length);

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

        // add iterators to enumerate blocks left-to-right and right-to-left, and include optional start/end indices for slice-a-roonie

        #region Embarassingly huge inlined constructors

        // gonna leave out the array-based ctor for now and see if we can do everything in a way that recycles the actual leaf data arrays...

        public DequeVector(T[] leftTail, IEnumerable<T[]> innerBlocks, T[] rightTail, int length)
        {
            CtorHelper(leftTail, innerBlocks, rightTail, length, out m_Shift, out m_Length, out m_Root, out m_RightTail, out m_LeftTail);
        }

        private static void CtorHelper(T[] leftTail, IEnumerable<T[]> innerBlocks, T[] rightTail, int length,
            out short m_Shift, out   int m_Length, out   object[] m_Root, out      T[] m_RightTail, out    T[] m_LeftTail)
        {
            int rightTailLength = rightTail.Length;
            int leftTailLength = leftTail.Length;
            int innerBlocksValuesLength = length - leftTail.Length - rightTail.Length;
            int depth = GetDepthFromLength(innerBlocksValuesLength);

            int numLeafArrays = innerBlocksValuesLength / 32; // this must be an integer because all the blocks have length 32
            int numLevel1Parents = (int)Math.Ceiling(numLeafArrays / 32f);
            int numLevel2Parents = (int)Math.Ceiling(numLevel1Parents / 32f);
            int numLevel3Parents = (int)Math.Ceiling(numLevel2Parents / 32f);
            int numLevel4Parents = (int)Math.Ceiling(numLevel3Parents / 32f);
            int numLevel5Parents = (int)Math.Ceiling(numLevel4Parents / 32f);

            // copy the leftTail first

            m_LeftTail = new T[leftTailLength];
            Array.Copy(leftTail, m_LeftTail, leftTail.Length);

            var innerBlocksEnumerator = innerBlocks.GetEnumerator();

            if (depth == 0)
            {
                m_Root = new object[0];
            }
            else if (depth == 2)
            {
                var level1Parent = new object[numLeafArrays];
                for (int i1 = 0; i1 < numLeafArrays; i1++)
                {
                    innerBlocksEnumerator.MoveNext();
                    level1Parent[i1] = innerBlocksEnumerator.Current;
                }
                m_Root = level1Parent;
            }
            else if (depth == 3)
            {
                var level2Parent = new object[numLevel1Parents];
                for (int i2 = 0; i2 < numLevel1Parents; i2++)
                {
                    var level1Parent = new object[i2 == numLevel1Parents - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                    for (int i1 = 0; i1 < level1Parent.Length; i1++)
                    {
                        innerBlocksEnumerator.MoveNext();
                        level1Parent[i1] = innerBlocksEnumerator.Current;
                    }
                    level2Parent[i2] = level1Parent;
                }
                m_Root = level2Parent;
            }
            else if (depth == 4)
            {
                var level3Parent = new object[numLevel2Parents];

                for (int i3 = 0; i3 < numLevel2Parents; i3++)
                {
                    var level2Parent = new object[i3 == numLevel2Parents - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                    for (int i2 = 0; i2 < level2Parent.Length; i2++)
                    {
                        var level1Parent = new object[i3 == numLevel2Parents - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                        if (level1Parent.Length == 0) level1Parent = new object[32];
                        for (int i1 = 0; i1 < level1Parent.Length; i1++)
                        {
                            innerBlocksEnumerator.MoveNext();
                            level1Parent[i1] = innerBlocksEnumerator.Current;
                        }
                        level2Parent[i2] = level1Parent;
                    }
                    level3Parent[i3] = level2Parent;
                }

                m_Root = level3Parent;
            }
            else if (depth == 5)
            {
                var level4Parent = new object[numLevel3Parents];

                for (int i4 = 0; i4 < numLevel3Parents; i4++)
                {
                    var level3Parent = new object[i4 == numLevel3Parents - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                    for (int i3 = 0; i3 < level3Parent.Length; i3++)
                    {
                        var level2Parent = new object[i4 == numLevel3Parents - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                        for (int i2 = 0; i2 < level2Parent.Length; i2++)
                        {
                            var level1Parent = new object[i4 == numLevel3Parents - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                            for (int i1 = 0; i1 < level1Parent.Length; i1++)
                            {
                                innerBlocksEnumerator.MoveNext();
                                level1Parent[i1] = innerBlocksEnumerator.Current;
                            }
                            level2Parent[i2] = level1Parent;
                        }
                        level3Parent[i3] = level2Parent;
                    }
                    level4Parent[i4] = level3Parent;
                }

                m_Root = level4Parent;
            }
            else if (depth == 6)
            {
                var level5Parent = new object[numLevel4Parents];

                for (int i5 = 0; i5 < numLevel4Parents; i5++)
                {
                    var level4Parent = new object[i5 == numLevel4Parents - 1 ? (numLevel3Parents - 1) % 32 + 1 : 32];
                    for (int i4 = 0; i4 < level4Parent.Length; i4++)
                    {
                        var level3Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                        for (int i3 = 0; i3 < level3Parent.Length; i3++)
                        {
                            var level2Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                            for (int i2 = 0; i2 < level2Parent.Length; i2++)
                            {
                                var level1Parent = new object[i5 == numLevel4Parents - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                                for (int i1 = 0; i1 < level1Parent.Length; i1++)
                                {
                                    innerBlocksEnumerator.MoveNext();
                                    level1Parent[i1] = innerBlocksEnumerator.Current;
                                }
                                level2Parent[i2] = level1Parent;
                            }
                            level3Parent[i3] = level2Parent;
                        }
                        level4Parent[i4] = level3Parent;
                    }
                    level5Parent[i5] = level4Parent;
                }

                m_Root = level5Parent;
            }
            else // if (depth == 7)
            {
                var level6Parent = new object[numLevel5Parents];

                for (int i6 = 0; i6 < numLevel5Parents; i6++)
                {
                    var level5Parent = new object[i6 == numLevel5Parents - 1 ? (numLevel4Parents - 1) % 32 + 1 : 32];
                    for (int i5 = 0; i5 < level5Parent.Length; i5++)
                    {
                        var level4Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 ? (numLevel3Parents - 1) % 32 + 1 : 32];
                        for (int i4 = 0; i4 < level4Parent.Length; i4++)
                        {
                            var level3Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 ? (numLevel2Parents - 1) % 32 + 1 : 32];
                            for (int i3 = 0; i3 < level3Parent.Length; i3++)
                            {
                                var level2Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 ? (numLevel1Parents - 1) % 32 + 1 : 32];
                                for (int i2 = 0; i2 < level2Parent.Length; i2++)
                                {
                                    var level1Parent = new object[i6 == numLevel5Parents - 1 && i5 == level5Parent.Length - 1 && i4 == level4Parent.Length - 1 && i3 == level3Parent.Length - 1 && i2 == level2Parent.Length - 1 ? (numLeafArrays - 1) % 32 + 1 : 32];
                                    for (int i1 = 0; i1 < level1Parent.Length; i1++)
                                    {
                                        innerBlocksEnumerator.MoveNext();
                                        level1Parent[i1] = innerBlocksEnumerator.Current;
                                    }
                                    level2Parent[i2] = level1Parent;
                                }
                                level3Parent[i3] = level2Parent;
                            }
                            level4Parent[i4] = level3Parent;
                        }
                        level5Parent[i5] = level4Parent;
                    }
                    level6Parent[i6] = level5Parent;
                }

                m_Root = level6Parent;
            }

            m_RightTail = new T[rightTailLength];
            Array.Copy(rightTail, m_RightTail, rightTail.Length);

            m_Length = length;
            m_Shift = (short)Math.Max(5, (depth - 1) * 5);
        }

        public DequeVector(IEnumerable<T> elements)
        {
            var enumerator = elements.GetEnumerator();

            var leftTail = new List<T>();
            int elemCount = 0;
            int blockCount = 0;

            var bodyBlocks = new List<T[]>();
            while (enumerator.MoveNext())
            {
                if (elemCount < 32)
                {
                    leftTail.Add(enumerator.Current);
                    elemCount++;
                }
                else
                {
                    blockCount = 0;
                    var block = new T[32];
                    block[blockCount++] = enumerator.Current;
                    while (blockCount < 32 && enumerator.MoveNext())
                    {
                        block[blockCount++] = enumerator.Current;
                    }
                    bodyBlocks.Add(block);
                    elemCount += blockCount;
                }
            }

            if (bodyBlocks.Count == 0)
            {
                CtorHelper(leftTail.ToArray(), new T[0][], new T[0], elemCount, out m_Shift, out m_Length, out m_Root, out m_RightTail, out m_LeftTail);
            }
            else if (bodyBlocks.Count == 1)
            {
                CtorHelper(leftTail.ToArray(), new T[0][], bodyBlocks.Single().Take(blockCount).ToArray(), elemCount, out m_Shift, out m_Length, out m_Root, out m_RightTail, out m_LeftTail);
            }
            else
            {
                var rightTail = bodyBlocks.Last().Take(blockCount).ToArray();
                bodyBlocks.RemoveAt(bodyBlocks.Count - 1);
                CtorHelper(leftTail.ToArray(), bodyBlocks, rightTail, elemCount, out m_Shift, out m_Length, out m_Root, out m_RightTail, out m_LeftTail);
            }
        }

        #endregion

        private static int GetDepthFromLength(int p)
        {
            const int zeroThresh = 0;
            const int oneThresh = 32;
            const int twoThresh = 32 << 5;
            const int threeThresh = 32 << 10;
            const int fourThresh = 32 << 15;
            const int fiveThresh = 32 << 20;
            const int sixThresh = 32 << 25;
            const int sevenThresh = 32 << 30;

            if (p <= zeroThresh) return 0;
            if (p <= twoThresh) return 2;
            if (p <= threeThresh) return 3;
            if (p <= fourThresh) return 4;
            if (p <= fiveThresh) return 5;
            if (p <= sixThresh) return 6;

            return 7;
        }

        int IVector<T>.Length
        {
            get { return m_Length; }
        }

        T IVector<T>.this[int i]
        {
            get { return this[i]; }
        }

        IVector<T> IVector<T>.Update(int index, T newVal)
        {
            return Update(index, newVal);
        }

        T IVector<T>.End
        {
            get { return this[m_Length - 1]; }
        }

        IVector<T> IVector<T>.Popped
        {
            get
            {
                return Pop();
            }
        }

        IVector<T> IVector<T>.Append(T item)
        {
            return Append(item);
        }

        T IVector<T>.Head
        {
            get { return this[0]; }
        }

        // write the intial implementations really fast in linq and then come back later and speed em up! correctness over  performance you dumb asshole
        // thats what cost you so much time last time around is that u optimized everythign as u went along instead of actually _understanding_
        // what it was u were doing, writing a quick LINQ-based version adn then optimizzing afterwards.
        // its not ur fault tho, ur just not that smart and data structures are hard for you. now that u realize this trie is easy you should be able
        // to pwn the crap out of it using the "know what youre trying to do" technique. patent pending.

        IVector<T> IVector<T>.Tail
        {
            get
            {
                return PopFront();
            }
        }

        IVector<T> IVector<T>.Cons(T item)
        {

            var bodyBlocks = GetLeftToRightBodyBlockEnumeration();
            var newLeftTail = m_LeftTail.Length == 32 ? new T[] { item } : new T[] { item }.Concat(m_LeftTail).ToArray();
            var newBodyBlocks = m_LeftTail.Length == 32 ? new T[][] { m_LeftTail }.Concat(bodyBlocks) : bodyBlocks;
            return new DequeVector<T>(newLeftTail, newBodyBlocks, m_RightTail, m_Length + 1);
        }

        IVector<T> IVector<T>.Concat(IEnumerable<T> items)
        {
            // should make a dedicated Concat routine that avoids some of the
            // repeated copy-on-writes since those intermediate results are never seen
            var currentVector = (IVector<T>)this;
            foreach (var item in items)
                currentVector = currentVector.Append(item);
            return currentVector;
        }

        T[] IVector<T>.SliceToArray(int start, int length)
        {
            throw new NotImplementedException();
        }

        IVector<T> IVector<T>.Slice(int start, int length)
        {
            throw new NotImplementedException();

            if (start < m_LeftTail.Length)
            {
                // arg do this   
            }
        }

        IVector<T> IVector<T>.Filter(Func<T, bool> pred)
        {
            var matching = new List<T>(m_Length);
            foreach (var item in this)
                if (pred(item))
                    matching.Add(item);
            return new DequeVector<T>(matching.ToArray());
        }

        IVector<U> IVector<T>.Map<U>(Func<T, U> mapper)
        {
            var mapped = new U[m_Length];
            int i = 0;
            foreach (var item in this)
                mapped[i++] = mapper(item);
            return new DequeVector<U>(mapped);
        }

        IVector<U> IVector<T>.FlatMap<U>(Func<T, IEnumerable<U>> mapper)
        {
            var mapped = new List<U>(m_Length);
            foreach (var item in this)
                foreach (var newElement in mapper(item))
                    mapped.Add(newElement);
            return new DequeVector<U>(mapped.ToArray());
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
            foreach (var item in GetRightToLeftEnumeration())
                currentValue = accumulator(currentValue, item);
            return currentValue;
        }

        private IEnumerable<T> GetLeftToRightEnumeration()
        {
            foreach (var item in m_LeftTail)
                yield return item;
            foreach (var block in GetLeftToRightBodyBlockEnumeration())
                foreach (var item in block)
                    yield return item;
            foreach (var item in m_RightTail)
                yield return item;
        }

        private IEnumerable<T> GetRightToLeftEnumeration()
        {
            return GetLeftToRightEnumeration().Reverse();
        }

        T IVector<T>.Reduce(Func<T, T, T> accumulator)
        {
            var enumerator = ((IEnumerable<T>)this).GetEnumerator();
            enumerator.MoveNext();
            var seed = enumerator.Current;
            while (enumerator.MoveNext())
                seed = accumulator(seed, enumerator.Current);
            return seed;
        }

        IVector<Tuple<T, U>> IVector<T>.Zip<U>(IVector<U> that)
        {
            var resultArray = new Tuple<T, U>[Math.Min(m_Length, that.Length)];

            // This enumerator crap should be faster than the indexing version
            var thisEnumerator = this.GetEnumerator();
            var thatEnumerator = that.GetEnumerator();

            int i = 0;
            while (thisEnumerator.MoveNext() && thatEnumerator.MoveNext())
                resultArray[i++] = Tuple.Create(thisEnumerator.Current, thatEnumerator.Current);

            return new DequeVector<Tuple<T, U>>(resultArray);
        }

        IEnumerator<T> GetEnumerator()
        {
            return GetLeftToRightEnumeration().GetEnumerator();
        }

        IEnumerable<T> IVector<T>.FastRightToLeftEnumeration
        {
            get { return this.GetRightToLeftEnumeration(); }
        }

        T[] IVector<T>.FastToArray()
        {
            return this.GetLeftToRightEnumeration().ToArray();
        }

        IVector<U> IVector<T>.New<U>(params U[] items)
        {
            return new DequeVector<U>(items);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
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
