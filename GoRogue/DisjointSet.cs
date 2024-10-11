using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue
{
    /// <summary>
    /// 提供有关<see cref="DisjointSet.SetsJoined"/>事件的数据。
    /// </summary>
    [PublicAPI]
    public class JoinedEventArgs : EventArgs
    {
        /// <summary>
        /// 合并后成为新父集合的集合ID。
        /// </summary>
        public readonly int LargerSetID;

        /// <summary>
        /// 合并后成为新子集合的集合ID。
        /// </summary>
        public readonly int SmallerSetID;

        /// <summary>
        /// 初始化<see cref="JoinedEventArgs"/>类的新实例。
        /// </summary>
        /// <param name="largerSetID">合并后成为新父集合的集合ID。</param>
        /// <param name="smallerSetID">合并后成为新子集合的集合ID。</param>
        public JoinedEventArgs(int largerSetID, int smallerSetID)
        {
            LargerSetID = largerSetID;
            SmallerSetID = smallerSetID;
        }
    }

    /// <summary>
    /// <see cref="DisjointSet{T}.SetsJoined"/> 事件的事件参数。
    /// </summary>
    [PublicAPI]
    public class JoinedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 两个已连接集合中较大的一个；将成为新的父集合。
        /// </summary>
        public readonly T LargerSet;

        /// <summary>
        /// 两个已连接集合中较小的一个；将成为新的子集合。
        /// </summary>
        public readonly T SmallerSet;

        /// <summary>
        /// 构造函数，用于初始化已连接的集合参数。
        /// </summary>
        /// <param name="largerSet">两个已连接集合中较大的一个；将成为新的父集合。</param>
        /// <param name="smallerSet">两个已连接集合中较小的一个；将成为新的子集合。</param>
        public JoinedEventArgs(T largerSet, T smallerSet)
        {
            LargerSet = largerSet;
            SmallerSet = smallerSet;
        }
    }

    /// <summary>
    /// 不相交集合数据结构的基本表示。
    /// </summary>
    /// <remarks>
    /// 出于优化原因，此不相交集合的实现不使用泛型，而是持有整数值，这些整数值将恰好是范围
    /// [0, num_items_in_set - 1] 内的所有整数值。因此，您需要为打算添加的对象分配适当的ID，并适当地映射它们。
    /// </remarks>
    [Serializable]
    [PublicAPI]
    public class DisjointSet : IReadOnlyDisjointSet
    {
        private readonly int[] _parents;
        private readonly int[] _sizes;

        /// <inheritdoc />
        public event EventHandler<JoinedEventArgs>? SetsJoined;

        /// <summary>
        /// 构造函数。不相交集合将包含范围 [0, <paramref name="size" /> - 1] 内的所有值。
        /// </summary>
        /// <param name="size">不相交集合的（最大）大小。</param>
        public DisjointSet(int size)
        {
            Count = size;
            _parents = new int[size];
            _sizes = new int[size];

            for (var i = 0; i < size; i++)
            {
                _parents[i] = i;
                _sizes[i] = 1;
            }
        }

        /// <inheritdoc />
        public int Count { get; private set; }

        /// <inheritdoc />
        public int Find(int obj)
        {
            // 查找基础父节点，并进行路径压缩
            if (obj != _parents[obj])
                _parents[obj] = Find(_parents[obj]);

            return _parents[obj];
        }

        /// <inheritdoc />
        public bool InSameSet(int obj1, int obj2) => Find(obj1) == Find(obj2); // In same set; same parent

        /// <summary>
        /// 返回不相交集合的只读表示。
        /// </summary>
        /// <returns>不相交集合的只读表示。</returns>
        public IReadOnlyDisjointSet AsReadOnly() => this;

        /// <summary>
        /// 对包含两个指定ID对象的集合执行并集操作。执行此操作后，
        /// 包含这两个指定对象的集合中的每个元素都将成为一个更大集合的一部分。
        /// </summary>
        /// <remarks>如果这两个元素已经在同一个集合中，则不会执行任何操作。</remarks>
        /// <param name="obj1" />
        /// <param name="obj2" />
        public void MakeUnion(int obj1, int obj2)
        {
            var i = Find(obj1);
            var j = Find(obj2);

            if (i == j) return; // 两个元素已经在同一个集合中；具有相同的父节点

            // 总是将较小的集合附加到较大的集合上
            if (_sizes[i] <= _sizes[j])
            {
                _parents[i] = j;
                _sizes[j] += _sizes[i];
                SetsJoined?.Invoke(this, new JoinedEventArgs(j, i));
            }
            else
            {
                _parents[j] = i;
                _sizes[i] += _sizes[j];
                SetsJoined?.Invoke(this, new JoinedEventArgs(i, j));
            }

            Count--;
        }

        /// <summary>
        /// 返回 DisjointSet 的字符串表示形式，显示集合中父元素和所有元素的 ID。
        /// </summary>
        /// <returns>DisjointSet 的字符串表示形式。</returns>
        public override string ToString() => ExtendToString(i => i.ToString());

        /// <summary>
        /// 返回 DisjointSet 的字符串表示形式，显示集合中的父元素和所有元素。
        /// 给定的函数用于为每个元素生成字符串。
        /// </summary>
        /// <returns>DisjointSet 的字符串表示形式。</returns>
        public string ExtendToString(Func<int, string> elementStringifier)
        {
            var values = new Dictionary<int, List<int>>();

            for (var i = 0; i < _parents.Length; i++)
            {
                var parentOf = FindNoCompression(i);
                if (!values.ContainsKey(parentOf))
                    values[parentOf] = new List<int>
                    {
                        parentOf // 父元素是每个子列表中的第一个元素
                    };

                if (parentOf != i) // 我们已经添加了父元素，所以不要重复添加
                    values[parentOf].Add(i);
            }

            return values.ExtendToString("", valueStringifier: obj => obj.ExtendToString(elementStringifier: elementStringifier), kvSeparator: ": ",
                pairSeparator: "\n", end: "");
        }

        // 用于确保 ToString 方法不会影响后续操作的性能
        private int FindNoCompression(int obj)
        {
            while (_parents[obj] != obj)
                obj = _parents[obj];

            return obj;
        }
    }

    /// <summary>
    /// <see cref="DisjointSet"/>的一个更易使用（但效率较低）的变体。此版本接受类型为T的实际对象，
    /// 并自动为您管理ID。
    /// </summary>
    /// <remarks>
    /// 这个集合结构与<see cref="DisjointSet"/>实际上完全一样，但是它接受类型T而不是ID。
    /// 为了效率起见，它仍然需要在创建集合时知道元素的数量。
    /// </remarks>
    /// <typeparam name="T">集合中元素的类型。</typeparam>
    [PublicAPI]
    public class DisjointSet<T> : IReadOnlyDisjointSet<T>
        where T : notnull
    {
        private readonly DisjointSet _idSet;
        private readonly Dictionary<T, int> _indices;
        private readonly T[] _items;

        /// <inheritdoc />
        public event EventHandler<JoinedEventArgs<T>>? SetsJoined;

        /// <inheritdoc />
        public int Count => _idSet.Count;

        /// <summary>
        /// 创建一个由给定项组成的新的不相交集合。每个项都将是其自己的唯一集合。
        /// </summary>
        /// <remarks>
        /// 这些项将通过字典映射到范围 [0, <paramref name="items" />.Length - 1] 内的ID，
        /// 其中键使用指定的比较器进行哈希，或者如果没有指定比较器，则使用默认的哈希函数。
        /// </remarks>
        /// <param name="items">要放置在不相交集合中的项。</param>
        /// <param name="comparer">用于哈希项时可选的比较器。</param>
        public DisjointSet(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
        {
            _items = items.ToArray();
            _indices = new Dictionary<T, int>(_items.Length, comparer ?? EqualityComparer<T>.Default);
            _idSet = new DisjointSet(_items.Length);

            // Create a mapping from item to index
            for (int i = 0; i < _items.Length; i++)
                _indices[_items[i]] = i;

            _idSet.SetsJoined += IDSetOnSetsJoined;
        }

        /// <inheritdoc />
        public T Find(T item)
        {
            int parentID = _idSet.Find(_indices[item]);
            return _items[parentID];
        }

        /// <inheritdoc />
        public bool InSameSet(T item1, T item2)
            => _idSet.InSameSet(_indices[item1], _indices[item2]);

        /// <summary>
        /// 返回不相交集合的只读表示。
        /// </summary>
        /// <returns>不相交集合的只读表示。</returns>
        public IReadOnlyDisjointSet<T> AsReadOnly() => this;

        /// <summary>
        /// 对包含两个指定对象的集合执行并集操作。执行此操作后，
        /// 包含这两个指定对象的集合中的每个元素都将成为一个更大集合的一部分。
        /// </summary>
        /// <remarks>如果这两个元素已经位于同一个集合中，则不会执行任何操作。</remarks>
        /// <param name="item1" />
        /// <param name="item2" />
        public void MakeUnion(T item1, T item2)
            => _idSet.MakeUnion(_indices[item1], _indices[item2]);

        /// <summary>
        /// 返回 DisjointSet、父节点以及它们集合中所有元素的字符串表示形式。
        /// 使用元素的默认 ToString 方法来生成字符串。
        /// </summary>
        /// <returns>DisjointSet 的字符串表示形式。</returns>
        public override string ToString()
            => _idSet.ExtendToString(i => _items[i].ToString() ?? "null");

        /// <summary>
        /// 返回不相交集合（DisjointSet）的字符串表示形式，展示父节点以及它们集合中的所有元素。
        /// 使用给定的函数来为每个元素生成字符串。
        /// </summary>
        /// <returns>不相交集合的字符串表示形式。</returns>
        public string ExtendToString(Func<T, string> elementStringifier)
            => _idSet.ExtendToString(i => elementStringifier(_items[i]));

        #region Event Synchronization
        // 当内部事件被触发时，通过找到与给定ID对应的对象来触发公共事件。
        private void IDSetOnSetsJoined(object? sender, JoinedEventArgs e)
            => SetsJoined?.Invoke(this, new JoinedEventArgs<T>(_items[e.LargerSetID], _items[e.SmallerSetID]));
        #endregion
    }
}
