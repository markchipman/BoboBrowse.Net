﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Facets.Impl;
    using Lucene.Net.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ListMerger
    {
        public class MergedIterator<T> : IEnumerator<T>
        {
            private class IteratorNode
            {
                public IEnumerator<T> m_iterator;
                public T m_curVal;

                public IteratorNode(IEnumerator<T> iterator)
                {
                    m_iterator = iterator;
                    m_curVal = default(T);
                }

                public bool Fetch()
                {
                    if (m_iterator.MoveNext())
                    {
                        m_curVal = m_iterator.Current;
                        return true;
                    }
                    m_curVal = default(T);
                    return false;
                }
            }

            private readonly MergedQueue m_queue;
            private T m_current;

            private MergedIterator(int length, IComparer<T> comparer)
            {
                m_queue = new MergedQueue(length, comparer);
            }

            private class MergedQueue : PriorityQueue<IteratorNode>
            {
                private readonly IComparer<T> comparer;

                public MergedQueue(int length, IComparer<T> comparer)
                    : base(length)
                {
                    this.comparer = comparer;
                }

                protected override bool LessThan(IteratorNode a, IteratorNode b)
                {
                    return (comparer.Compare(a.m_curVal, b.m_curVal) < 0);
                }
            }

            public MergedIterator(IList<IEnumerator<T>> iterators, IComparer<T> comparer)
                : this(iterators.Count, comparer)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch()) m_queue.Add(ctx); // NOTE: This was InsertWithOverflow in codeplex version
                }
            }

            public MergedIterator(IEnumerator<T>[] iterators, IComparer<T> comparer)
                : this(iterators.Length, comparer)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch()) m_queue.Add(ctx); // NOTE: This was InsertWithOverflow in codeplex version
                }
            }

            //public virtual bool HasNext()
            //{
            //    return _queue.Size() > 0;
            //}

            //public virtual T Next()
            //{
            //    IteratorNode ctx = (IteratorNode)_queue.Top;
            //    T val = ctx._curVal;
            //    if (ctx.Fetch())
            //    {
            //        _queue.UpdateTop();
            //    }
            //    else
            //    {
            //        _queue.Pop();
            //    }
            //    return val;
            //}

            //public virtual void Remove()
            //{
            //    throw new NotSupportedException();
            //}

            public T Current
            {
                get { return m_current; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return m_current; }
            }

            public bool MoveNext()
            {
                if (m_queue.Count > 0)
                {
                    IteratorNode ctx = (IteratorNode)m_queue.Top;
                    T val = ctx.m_curVal;
                    if (ctx.Fetch())
                    {
                        m_queue.UpdateTop();
                    }
                    else
                    {
                        m_queue.Pop();
                    }
                    this.m_current = val;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        private ListMerger()
        {
        }

        public static MergedIterator<T> MergeLists<T>(IEnumerator<T>[] iterators, IComparer<T> comparer)
        {
            return new MergedIterator<T>(iterators, comparer);
        }

        public static MergedIterator<T> MergeLists<T>(IList<IEnumerator<T>> iterators, IComparer<T> comparer)
        {
            return new MergedIterator<T>(iterators, comparer);
        }

        public static IList<T> MergeLists<T>(int offset, int count, IEnumerator<T>[] iterators, IComparer<T> comparer)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparer));
        }

        public static IList<T> MergeLists<T>(int offset, int count, IList<IEnumerator<T>> iterators, IComparer<T> comparer)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparer));
        }

        private static IList<T> MergeLists<T>(int offset, int count, MergedIterator<T> mergedIter)
        {
            if (count == 0) return new List<T>();
            for (int c = 0; c < offset && mergedIter.MoveNext(); c++)
            {
                var x = mergedIter.Current;
            }

            List<T> mergedList = new List<T>();

            for (int c = 0; c < count && mergedIter.MoveNext(); c++)
            {
                mergedList.Add(mergedIter.Current);
            }

            return mergedList;
        }

        public static IComparer<BrowseFacet> FACET_VAL_COMPARER = new FacetValComparer();

        private class FacetValComparer : IComparer<BrowseFacet>
        {
            public int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                int ret = string.CompareOrdinal(o1.Value, o2.Value);
                if (o1.Value.StartsWith("-") && o2.Value.StartsWith("-"))
                {
                    ret = -ret;
                }
                return ret;
            }

            public static IDictionary<string, IFacetAccessible> MergeSimpleFacetContainers(IEnumerable<IDictionary<string, IFacetAccessible>> subMaps, BrowseRequest req)
            {
                Dictionary<string, Dictionary<object, BrowseFacet>> counts = new Dictionary<string, Dictionary<object, BrowseFacet>>();
                foreach (Dictionary<string, IFacetAccessible> subMap in subMaps)
                {
                    foreach (KeyValuePair<string, IFacetAccessible> entry in subMap)
                    {
                        Dictionary<object, BrowseFacet> count = counts[entry.Key];
                        if (count == null)
                        {
                            count = new Dictionary<object, BrowseFacet>();
                            counts.Add(entry.Key, count);
                        }
                        foreach (BrowseFacet facet in entry.Value.GetFacets())
                        {
                            string val = facet.Value;
                            BrowseFacet oldValue = count[val];
                            if (oldValue == null)
                            {
                                count.Add(val, new BrowseFacet(val, facet.FacetValueHitCount));
                            }
                            else
                            {
                                oldValue.FacetValueHitCount = oldValue.FacetValueHitCount + facet.FacetValueHitCount;
                            }
                        }
                    }
                }

                Dictionary<string, IFacetAccessible> mergedFacetMap = new Dictionary<string, IFacetAccessible>();
                foreach (string facet in counts.Keys)
                {
                    FacetSpec fs = req.GetFacetSpec(facet);

                    FacetSpec.FacetSortSpec sortSpec = fs.OrderBy;

                    IComparer<BrowseFacet> comparer;
                    if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(sortSpec))
                    {
                        comparer = FACET_VAL_COMPARER;
                    }
                    else if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(sortSpec))
                    {
                        comparer = FacetHitcountComparerFactory.FACET_HITS_COMPARER;
                    }
                    else
                    {
                        comparer = fs.CustomComparerFactory.NewComparer();
                    }

                    Dictionary<object, BrowseFacet> facetValueCounts = counts[facet];
                    BrowseFacet[] facetArray = facetValueCounts.Values.ToArray();
                    Array.Sort(facetArray, comparer);

                    int numToShow = facetArray.Length;
                    if (req != null)
                    {
                        FacetSpec fspec = req.GetFacetSpec(facet);
                        if (fspec != null)
                        {
                            int maxCount = fspec.MaxCount;
                            if (maxCount > 0)
                            {
                                numToShow = Math.Min(maxCount, numToShow);
                            }
                        }
                    }

                    BrowseFacet[] facets;
                    if (numToShow == facetArray.Length)
                    {
                        facets = facetArray;
                    }
                    else
                    {
                        facets = new BrowseFacet[numToShow];
                        Array.Copy(facetArray, 0, facets, 0, numToShow);
                    }

                    MappedFacetAccessible mergedFacetAccessible = new MappedFacetAccessible(facets);
                    mergedFacetMap.Add(facet, mergedFacetAccessible);
                }
                return mergedFacetMap;
            }
        }
    }
}
