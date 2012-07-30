/////////////////////////////////////////////////////////////////////
// File Name               : IntervalTree.cs
//      Created            : 24 7 2012   23:20
//      Author             : Costin S
//
/////////////////////////////////////////////////////////////////////
#define TREE_WITH_PARENT_POINTERS

namespace IntervalTree
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Interval
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Interval<T> where T : IComparable<T>
    {
        #region C'tor
        
        public Interval(T start, T end)
            :this()
        {
            if (start.CompareTo(end) >= 0)
            {
                throw new ArgumentException("the start value of the interval must be smaller than the end value. null interval are not allowed");
            }

            this.Start = start;
            this.End = end;
        }

        #endregion

        #region Public Methods

        public T Start;
        public T End;

        /// <summary>
        /// Determines if two intervals overlap (i.e. if this interval starts before the other ends and it finishes after the other starts)
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>
        ///   <c>true</c> if the specified other is overlapping; otherwise, <c>false</c>.
        /// </returns>
        public bool OverlapsWith(Interval<T> other)
        {            
            return (this.Start.CompareTo(other.End) < 0 && this.End.CompareTo(other.Start) > 0);
        }        

        public override string ToString()
        {
            return "[" + Start.ToString() + "," + End.ToString() + "]";
        }
        
        #endregion
    }

    /// <summary>
    /// Interval Tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IntervalTree<T, TypeValue> where T : IComparable<T>
    {        
        #region Properties

        private IntervalNode Root;
        internal int Count;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalTree&lt;T, TypeValue&gt;"/> class.
        /// </summary>
        public IntervalTree()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalTree&lt;T, TypeValue&gt;"/> class.
        /// </summary>
        /// <param name="elems">The elems.</param>
        public IntervalTree(IEnumerable<KeyValuePair<Interval<T>, TypeValue>> elems)
        {
            if (elems != null)
            {
                foreach (var elem in elems)
                {
                    Add(elem.Key, elem.Value);
                }
            }
        }

        #endregion

        #region Delegates

        /// <summary>
        /// visitor delegate
        /// </summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="level">The level.</param>
        private delegate void VisitNodeHandler<TNode>(TNode node, int level);

        #endregion        
               
        #region Methods

        /// <summary>
        /// Adds the specified interval.
        /// </summary>
        /// <param name="arg">The arg.</param>
        public void Add(T x, T y, TypeValue value)
        {
            Add(new Interval<T>(x, y), value);
        }

        /// <summary>
        /// Adds the specified arg.
        /// </summary>
        /// <param name="arg">The arg.</param>
        public void Add(Interval<T> interval, TypeValue value)
        {
            bool wasAdded = false;
            bool wasSuccessful = false;

            this.Root = IntervalNode.Add(this.Root, interval, value, ref wasAdded, ref wasSuccessful);
            if (this.Root != null)
            {
                IntervalNode.ComputeMax(this.Root);
            }

            if (wasSuccessful)
            {
                this.Count++;
            }
        }

        /// <summary>
        /// Deletes the interval starting at x.
        /// </summary>
        /// <param name="arg">The arg.</param>
        public void Delete(Interval<T> arg)
        {
            if (Root != null)
            {
                bool wasDeleted = false;
                bool wasSuccessful = false;

                Root = IntervalNode.Delete(Root, arg, ref wasDeleted, ref wasSuccessful);
                if (this.Root != null)
                {
                    IntervalNode.ComputeMax(this.Root);
                }

                if (wasSuccessful)
                {
                    this.Count--;
                }
            }
        }        

        /// <summary>
        /// Searches for all intervals overlapping the one specified as an argument.
        /// </summary>
        /// <param name="toFind">To find.</param>
        /// <param name="list">The list.</param>
        public void GetIntervalsOverlappingWith(Interval<T> toFind, ref List<KeyValuePair<Interval<T>, TypeValue>> list)
        {
            if (this.Root != null)
            {
                this.Root.GetIntervalsOverlappingWith(toFind, ref list);
            }
        }

        /// <summary>
        /// Searches for all intervals overlapping the one specified as an argument.
        /// </summary>
        /// <param name="toFind">To find.</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<Interval<T>, TypeValue>> GetIntervalsOverlappingWith(Interval<T> toFind)
        {
            return (Root != null) ? Root.GetIntervalsOverlappingWith(toFind) : null;
        }

        /// <summary>
        /// Searches the specified arg.
        /// </summary>
        /// <param name="arg">The arg.</param>
        /// <returns></returns>
        public List<KeyValuePair<Interval<T>, TypeValue>> GetIntervalsStartingAt(T arg)
        {
            return IntervalNode.GetIntervalsStartingAt(Root, arg);
        }

#if TREE_WITH_PARENT_POINTERS
        /// <summary>
        /// Gets the collection of keys (ascending order)
        /// </summary>
        public IEnumerable<Interval<T>> Keys
        {
            get
            {
                if (this.Root == null)
                    yield break;

                var p = IntervalNode.FindMin(this.Root);
                while (p != null)
                {
                    yield return p.Interval;

                    foreach (var rangeNode in p.GetRange())
                    {
                        yield return rangeNode.Key;
                    }

                    p = p.Successor();
                }
            }
        }

        /// <summary>
        /// Gets the collection of values (ascending order)
        /// </summary>
        public IEnumerable<TypeValue> Values
        {
            get
            {
                if (this.Root == null)
                    yield break;

                var p = IntervalNode.FindMin(this.Root);
                while (p != null)
                {
                    yield return p.Value;

                    foreach (var rangeNode in p.GetRange())
                    {
                        yield return rangeNode.Value;
                    }

                    p = p.Successor();
                }
            }
        }

#endif        

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.Root = null;
            this.Count = 0;
        }

        public void Print()
        {
            this.Visit((node, level) =>
            {
                Console.Write(new string(' ', 2 * level));
                Console.WriteLine("{0}", "[" + node.Interval.Start.ToString() + "," + node.Interval.End.ToString() + "]." + (node.Max));
            });
        }

        /// <summary>
        /// Visit_inorders the specified visitor. Defined for debugging purposes only
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        private void Visit(VisitNodeHandler<IntervalNode> visitor)
        {
            if (Root != null)
            {
                Root.Visit(visitor, 0);
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// node class
        /// </summary>
        /// <typeparam name="TElem">The type of the elem.</typeparam>
        private class IntervalNode
        {
            #region Properties

            private int Balance;
            private IntervalNode Left;
            private IntervalNode Right;
            public Interval<T> Interval;
            public TypeValue Value;
            private List<KeyValuePair<T, TypeValue>> Range;
            public T Max;

#if TREE_WITH_PARENT_POINTERS
            private IntervalNode Parent;
#endif

            #endregion

            #region Methods

            /// <summary>
            /// Adds the specified elem.
            /// </summary>
            /// <param name="elem">The elem.</param>
            /// <param name="data">The data.</param>
            /// <returns></returns>
            public static IntervalNode Add(IntervalNode elem, Interval<T> interval, TypeValue value, ref bool wasAdded, ref bool wasSuccessful)
            {
                if (elem == null)
                {
                    elem = new IntervalNode { Left = null, Right = null, Balance = 0, Interval = interval, Value = value, Max = interval.End };
                    wasAdded = true;
                    wasSuccessful = true;
                }
                else
                {
                    int compareResult = interval.Start.CompareTo(elem.Interval.Start);
                    IntervalNode newChild = null;
                    if (compareResult < 0)
                    {
                        newChild = Add(elem.Left, interval, value, ref wasAdded, ref wasSuccessful);
                        if (elem.Left != newChild)
                        {
                            elem.Left = newChild;
#if TREE_WITH_PARENT_POINTERS
                            newChild.Parent = elem;
#endif
                        }

                        if (wasAdded)
                        {
                            elem.Balance--;

                            if (elem.Balance == 0)
                            {
                                wasAdded = false;
                            }
                            else if (elem.Balance == -2)
                            {
                                if (elem.Left.Balance == 1)
                                {
                                    int elemLeftRightBalance = elem.Left.Right.Balance;

                                    elem.Left = RotateLeft(elem.Left);
                                    elem = RotateRight(elem);

                                    elem.Balance = 0;
                                    elem.Left.Balance = elemLeftRightBalance == 1 ? -1 : 0;
                                    elem.Right.Balance = elemLeftRightBalance == -1 ? 1 : 0;
                                }

                                else if (elem.Left.Balance == -1)
                                {
                                    elem = RotateRight(elem);
                                    elem.Balance = 0;
                                    elem.Right.Balance = 0;
                                }
                                wasAdded = false;
                            }
                        }
                    }
                    else if (compareResult > 0)
                    {
                        newChild = Add(elem.Right, interval, value, ref wasAdded, ref wasSuccessful);
                        if (elem.Right != newChild)
                        {
                            elem.Right = newChild;
#if TREE_WITH_PARENT_POINTERS
                            newChild.Parent = elem;
#endif
                        }

                        if (wasAdded)
                        {
                            elem.Balance++;
                            if (elem.Balance == 0)
                            {
                                wasAdded = false;
                            }
                            else if (elem.Balance == 2)
                            {
                                if (elem.Right.Balance == -1)
                                {
                                    int elemRightLeftBalance = elem.Right.Left.Balance;

                                    elem.Right = RotateRight(elem.Right);
                                    elem = RotateLeft(elem);

                                    elem.Balance = 0;
                                    elem.Left.Balance = elemRightLeftBalance == 1 ? -1 : 0;
                                    elem.Right.Balance = elemRightLeftBalance == -1 ? 1 : 0;
                                }

                                else if (elem.Right.Balance == 1)
                                {
                                    elem = RotateLeft(elem);

                                    elem.Balance = 0;
                                    elem.Left.Balance = 0;
                                }
                                wasAdded = false;
                            }
                        }
                    }
                    else
                    {
                        // we allow multiple values per key
                        if (elem.Range == null)
                        {
                            elem.Range = new List<KeyValuePair<T, TypeValue>>();
                        }

                        //always store the max Y value in the node itself .. store the Range list in decreasing order
                        if (interval.End.CompareTo(elem.Interval.End) > 0)
                        {
                            elem.Range.Insert(0, new KeyValuePair<T, TypeValue>(elem.Interval.End, elem.Value));
                            elem.Interval = interval;
                            elem.Value = value;
                        }
                        else
                        {
                            for (int i = 0; i < elem.Range.Count; i++)
                            {
                                if (interval.End.CompareTo(elem.Range[i].Key) >= 0)
                                {
                                    elem.Range.Insert(i, new KeyValuePair<T, TypeValue>(interval.End, value));

                                    break;
                                }
                            }
                        }

                        wasSuccessful = true;
                    }
                    ComputeMax(elem);
                }

                return elem;
            }

            /// <summary>
            /// Searches the specified subtree.
            /// </summary>
            /// <param name="subtree">The subtree.</param>
            /// <param name="data">The data.</param>
            /// <returns></returns>
            public static IntervalNode Search(IntervalNode subtree, Interval<T> data)
            {
                if (subtree != null)
                {
                    int compareResult = data.Start.CompareTo(subtree.Interval.Start);

                    if (compareResult < 0)
                    {
                        return Search(subtree.Left, data);
                    }
                    else if (compareResult > 0)
                    {
                        return Search(subtree.Right, data);
                    }
                    else
                    {
                        return subtree;
                    }
                }
                else return null;
            }

            /// <summary>
            /// Computes the max.
            /// </summary>
            /// <param name="node">The node.</param>
            public static void ComputeMax(IntervalNode node)
            {
                T maxRange = node.Interval.End;

                if (node.Left == null && node.Right == null)
                {
                    node.Max = maxRange;
                }
                else if (node.Left == null)
                {
                    node.Max = (maxRange.CompareTo(node.Right.Max) >= 0) ? maxRange : node.Right.Max;
                }
                else if (node.Right == null)
                {
                    node.Max = (maxRange.CompareTo(node.Left.Max) >= 0) ? maxRange : node.Left.Max;
                }
                else
                {
                    T leftMax = node.Left.Max;
                    T rightMax = node.Right.Max;

                    if (leftMax.CompareTo(rightMax) >= 0)
                    {
                        node.Max = maxRange.CompareTo(leftMax) >= 0 ? maxRange : leftMax;
                    }
                    else
                    {
                        node.Max = maxRange.CompareTo(rightMax) >= 0 ? maxRange : rightMax;
                    }
                }
            }

            /// <summary>
            /// Rotates lefts this instance.
            /// Assumes that this.Right != null
            /// </summary>
            /// <returns></returns>
            private static IntervalNode RotateLeft(IntervalNode node)
            {
                var right = node.Right;
                Debug.Assert(node.Right != null);

                var rightLeft = right.Left;

                node.Right = rightLeft;
                ComputeMax(node);

#if TREE_WITH_PARENT_POINTERS
                var parent = node.Parent;
                if (rightLeft != null)
                {
                    rightLeft.Parent = node;
                }
#endif
                right.Left = node;
                ComputeMax(right);

#if TREE_WITH_PARENT_POINTERS
                node.Parent = right;
                if (parent != null)
                {
                    if (parent.Left == node)
                        parent.Left = right;
                    else
                        parent.Right = right;

                }
                right.Parent = parent;
#endif

                return right;

            }

            /// <summary>
            /// Rotates right this instance.
            /// Assumes that (this.Left != null)
            /// </summary>
            /// <returns></returns>
            private static IntervalNode RotateRight(IntervalNode node)
            {
                var left = node.Left;
                Debug.Assert(node.Left != null);

                var leftRight = left.Right;
                node.Left = leftRight;
                ComputeMax(node);

#if TREE_WITH_PARENT_POINTERS
                var parent = node.Parent;
                if (leftRight != null)
                {
                    leftRight.Parent = node;
                }
#endif
                left.Right = node;
                ComputeMax(left);

#if TREE_WITH_PARENT_POINTERS
                node.Parent = left;
                if (parent != null)
                {
                    if (parent.Left == node)
                        parent.Left = left;
                    else
                        parent.Right = left;

                }
                left.Parent = parent;
#endif
                return left;
            }

            /// <summary>
            /// Finds the min.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <returns></returns>
            public static IntervalNode FindMin(IntervalNode node)
            {
                while (node != null && node.Left != null)
                {
                    node = node.Left;
                }
                return node;
            }

            /// <summary>
            /// Finds the max.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <returns></returns>
            public static IntervalNode FindMax(IntervalNode node)
            {
                while (node != null && node.Right != null)
                {
                    node = node.Right;
                }
                return node;
            }

            public IEnumerable<KeyValuePair<Interval<T>, TypeValue>> GetRange()
            {
                if (this.Range != null)
                {
                    foreach (var value in this.Range)
                    {
                        var kInterval = new Interval<T>(this.Interval.Start, value.Key);
                        yield return new KeyValuePair<Interval<T>, TypeValue>(kInterval, value.Value);
                    }
                }
                else
                {
                    yield break;
                }
            }

#if TREE_WITH_PARENT_POINTERS

            /// <summary>
            /// Succeeds this instance.
            /// </summary>
            /// <returns></returns>
            public IntervalNode Successor()
            {
                if (this.Right != null)
                    return FindMin(this.Right);
                else
                {
                    var p = this;
                    while (p.Parent != null && p.Parent.Right == p)
                        p = p.Parent;
                    return p.Parent;
                }
            }

            /// <summary>
            /// Precedes this instance.
            /// </summary>
            /// <returns></returns>
            public IntervalNode Predecesor()
            {
                if (this.Left != null)
                    return FindMax(this.Left);
                else
                {
                    var p = this;
                    while (p.Parent != null && p.Parent.Left == p)
                        p = p.Parent;
                    return p.Parent;
                }
            }
#endif

            /// <summary>
            /// Deletes the specified node.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <param name="arg">The arg.</param>
            /// <returns></returns>
            public static IntervalNode Delete(IntervalNode node, Interval<T> arg, ref bool wasDeleted, ref bool wasSuccessful)
            {
                int cmp = arg.Start.CompareTo(node.Interval.Start);
                IntervalNode newChild = null;

                if (cmp < 0)
                {
                    if (node.Left != null)
                    {
                        newChild = Delete(node.Left, arg, ref wasDeleted, ref wasSuccessful);
                        if (node.Left != newChild)
                        {
                            node.Left = newChild;
                        }

                        if (wasDeleted)
                        {
                            node.Balance++;
                        }
                    }
                }
                else if (cmp == 0)
                {
                    int position = -1;

                    // find the exact interval to delete based on the Y value.. consider changing this code
                    if (arg.End.CompareTo(node.Interval.End) == 0)
                    {
                        position = 0;
                    }
                    else
                    {
                        if (node.Range != null && node.Range.Count > 0)
                        {
                            for (int k = 0; k < node.Range.Count; k++)
                            {
                                if (arg.End.CompareTo(node.Range[k].Key) == 0)
                                {
                                    position = k + 1;
                                }
                            }
                        }
                    }

                    // couldn't find the interval in the tree, throw an exception
                    if (position == -1)
                    {
                        throw new ArgumentOutOfRangeException("arg", "cannot delete the specified interval. invalid argument.");
                    }

                    if (position > 0)
                    {
                        // we're counting the value stored in the node.Value as position 0, all values stored in Range represent position + 1, position + 2, ...etc
                        if (node.Range != null && position - 1 < node.Range.Count)
                        {
                            node.Range.RemoveAt(position - 1);

                            if (node.Range.Count == 0)
                            {
                                node.Range = null;
                            }

                            wasSuccessful = true;
                        }
                    }
                    else if (position == 0 && node.Range != null && node.Range.Count > 0)
                    {
                        node.Interval = new Interval<T>(node.Interval.Start, node.Range[0].Key);
                        node.Value = node.Range[0].Value;

                        node.Range.RemoveAt(0);
                        if (node.Range.Count == 0)
                        {
                            node.Range = null;
                        }

                        wasSuccessful = true;
                    }
                    else
                    {
                        if (node.Left != null && node.Right != null)
                        {
                            var min = FindMin(node.Right);

                            var interval = node.Interval;
                            node.Swap(min);

                            wasDeleted = false;

                            newChild = Delete(node.Right, interval, ref wasDeleted, ref wasSuccessful);
                            if (node.Right != newChild)
                            {
                                node.Right = newChild;
                            }

                            if (wasDeleted)
                            {
                                node.Balance--;
                            }
                        }
                        else if (node.Left == null)
                        {
                            wasDeleted = true;
                            wasSuccessful = true;
                            return node.Right;
                        }
                        else
                        {
                            wasDeleted = true;
                            wasSuccessful = true;
                            return node.Left;
                        }
                    }
                }
                else
                {
                    if (node.Right != null)
                    {
                        newChild = Delete(node.Right, arg, ref wasDeleted, ref wasSuccessful);
                        if (node.Right != newChild)
                        {
                            node.Right = newChild;
                        }

                        if (wasDeleted)
                        {
                            node.Balance--;
                        }
                    }
                }
                ComputeMax(node);

                if (wasDeleted)
                {
                    if (node.Balance == 1 || node.Balance == -1)
                    {
                        wasDeleted = false;
                        return node;
                    }
                    else if (node.Balance == -2)
                    {
                        if (node.Left.Balance == 1)
                        {
                            int leftRightBalance = node.Left.Right.Balance;

                            node.Left = RotateLeft(node.Left);
                            node = RotateRight(node);

                            node.Balance = 0;
                            node.Left.Balance = (leftRightBalance == 1) ? -1 : 0;
                            node.Right.Balance = (leftRightBalance == -1) ? 1 : 0;
                        }
                        else if (node.Left.Balance == -1)
                        {
                            node = RotateRight(node);
                            node.Balance = 0;
                            node.Right.Balance = 0;
                        }
                        else if (node.Left.Balance == 0)
                        {
                            node = RotateRight(node);
                            node.Balance = 1;
                            node.Right.Balance = -1;

                            wasDeleted = false;
                        }
                    }
                    else if (node.Balance == 2)
                    {
                        if (node.Right.Balance == -1)
                        {
                            int rightLeftBalance = node.Right.Left.Balance;

                            node.Right = RotateRight(node.Right);
                            node = RotateLeft(node);

                            node.Balance = 0;
                            node.Left.Balance = (rightLeftBalance == 1) ? -1 : 0;
                            node.Right.Balance = (rightLeftBalance == -1) ? 1 : 0;
                        }
                        else if (node.Right.Balance == 1)
                        {
                            node = RotateLeft(node);
                            node.Balance = 0;
                            node.Left.Balance = 0;
                        }
                        else if (node.Right.Balance == 0)
                        {
                            node = RotateLeft(node);
                            node.Balance = -1;
                            node.Left.Balance = 1;

                            wasDeleted = false;
                        }
                    }
                }
                return node;
            }

            private void Swap(IntervalNode node)
            {
                var dataInterval = this.Interval;
                var dataValue = this.Value;
                var dataRange = this.Range;

                this.Interval = node.Interval;
                this.Value = node.Value;
                this.Range = node.Range;

                node.Interval = dataInterval;
                node.Value = dataValue;
                node.Range = dataRange;
            }

            /// <summary>
            /// Returns all intervals beginning at the specified start value
            /// </summary>
            /// <param name="subtree">The subtree.</param>
            /// <param name="data">The data.</param>
            /// <returns></returns>
            public static List<KeyValuePair<Interval<T>, TypeValue>> GetIntervalsStartingAt(IntervalNode subtree, T start)
            {
                if (subtree != null)
                {
                    int compareResult = start.CompareTo(subtree.Interval.Start);
                    if (compareResult < 0)
                    {
                        return GetIntervalsStartingAt(subtree.Left, start);
                    }
                    else if (compareResult > 0)
                    {
                        return GetIntervalsStartingAt(subtree.Right, start);
                    }
                    else
                    {
                        var result = new List<KeyValuePair<Interval<T>, TypeValue>>();
                        result.Add(new KeyValuePair<Interval<T>, TypeValue>(subtree.Interval, subtree.Value));

                        if (subtree.Range != null)
                        {
                            foreach (var value in subtree.Range)
                            {
                                var kInterval = new Interval<T>(start, value.Key);
                                result.Add(new KeyValuePair<Interval<T>, TypeValue>(kInterval, value.Value));
                            }
                        }

                        return result;
                    }
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Searches for all intervals in this subtree that are overlapping the argument interval.
            /// </summary>
            /// <param name="toFind">To find.</param>
            /// <param name="list">The list.</param>
            public void GetIntervalsOverlappingWith(Interval<T> toFind, ref List<KeyValuePair<Interval<T>, TypeValue>> list)
            {
                if (toFind.End.CompareTo(this.Interval.Start) <= 0)
                {
                    ////toFind ends before subtree.Interval begins, prune the right subtree
                    if (this.Left != null)
                    {
                        this.Left.GetIntervalsOverlappingWith(toFind, ref list);
                    }
                }
                else if (toFind.Start.CompareTo(this.Max) >= 0)
                {
                    ////toFind begins after the subtree.Max ends, prune the left subtree
                    if (this.Right != null)
                    {
                        this.Right.GetIntervalsOverlappingWith(toFind, ref list);
                    }
                }
                else
                {
                    //// search the left subtree
                    if (this.Left != null)
                    {
                        this.Left.GetIntervalsOverlappingWith(toFind, ref list);
                    }

                    if (this.Interval.OverlapsWith(toFind))
                    {
                        if (list == null)
                        {
                            list = new List<KeyValuePair<Interval<T>, TypeValue>>();
                        }

                        list.Add(new KeyValuePair<Interval<T>, TypeValue>(this.Interval, this.Value));
                    }

                    if (this.Range != null && this.Range.Count > 0)
                    {
                        for (int k = 0; k < this.Range.Count; k++)
                        {
                            var kInterval = new Interval<T>(this.Interval.Start, this.Range[k].Key);
                            if (kInterval.OverlapsWith(toFind))
                            {
                                if (list == null)
                                {
                                    list = new List<KeyValuePair<Interval<T>, TypeValue>>();
                                }
                                list.Add(new KeyValuePair<Interval<T>, TypeValue>(kInterval, this.Range[k].Value));
                            }
                        }
                    }

                    //// search the right subtree
                    if (this.Right != null)
                    {
                        this.Right.GetIntervalsOverlappingWith(toFind, ref list);
                    }
                }
            }

            /// <summary>
            /// Gets all intervals in this subtree that are overlapping the argument interval.
            /// </summary>
            /// <param name="toFind">To find.</param>
            /// <returns></returns>
            public IEnumerable<KeyValuePair<Interval<T>, TypeValue>> GetIntervalsOverlappingWith(Interval<T> toFind)
            {
                if (toFind.End.CompareTo(this.Interval.Start) <= 0)
                {
                    //toFind ends before subtree.Interval begins, prune the right subtree
                    if (this.Left != null)
                    {
                        foreach (var value in this.Left.GetIntervalsOverlappingWith(toFind))
                        {
                            yield return value;
                        }
                    }
                }
                else if (toFind.Start.CompareTo(this.Max) >= 0)
                {
                    //toFind begins after the subtree.Max ends, prune the left subtree
                    if (this.Right != null)
                    {
                        foreach (var value in this.Right.GetIntervalsOverlappingWith(toFind))
                        {
                            yield return value;
                        }
                    }
                }
                else
                {
                    if (this.Left != null)
                    {
                        foreach (var value in this.Left.GetIntervalsOverlappingWith(toFind))
                        {
                            yield return value;
                        }
                    }

                    if (this.Interval.OverlapsWith(toFind))
                    {
                        yield return new KeyValuePair<Interval<T>, TypeValue>(this.Interval, this.Value);
                    }

                    if (this.Range != null && this.Range.Count > 0)
                    {
                        foreach (var value in this.Range)
                        {
                            var kInterval = new Interval<T>(this.Interval.Start, value.Key);

                            if (kInterval.OverlapsWith(toFind))
                            {
                                yield return new KeyValuePair<Interval<T>, TypeValue>(kInterval, value.Value);
                            }
                        }
                    }


                    if (this.Right != null)
                    {
                        foreach (var value in this.Right.GetIntervalsOverlappingWith(toFind))
                        {
                            yield return value;
                        }
                    }
                }
            }

            public void Visit(VisitNodeHandler<IntervalNode> visitor, int level)
            {
                if (this.Left != null)
                {
                    this.Left.Visit(visitor, level + 1);
                }

                visitor(this, level);

                if (this.Right != null)
                {
                    this.Right.Visit(visitor, level + 1);
                }
            }

            #endregion
        }

        #endregion
    }
}
