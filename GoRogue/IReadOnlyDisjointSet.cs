using System;
using JetBrains.Annotations;

namespace GoRogue
{
    /// <summary>
    /// <see cref="DisjointSet"/> 的只读表示。
    /// </summary>
    [PublicAPI]
    public interface IReadOnlyDisjointSet
    {

        /// <summary>
        /// 当两个集合合并为一个时触发。参数提供合并的两个集合的ID。
        /// </summary>
        public event EventHandler<JoinedEventArgs>? SetsJoined;

        /// <summary>
        /// 不同集合的数量。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 返回包含ID为<paramref name="objectID" />的集合的父集合的ID，并在搜索完成时执行路径压缩。
        /// </summary>
        /// <param name="objectID">要搜索的对象的ID。</param>
        /// <returns>给定对象的父对象的ID。</returns>
        int Find(int objectID);

        /// <summary>
        /// 如果由给定ID指定的对象位于同一集合中，则返回true。
        /// </summary>
        /// <param name="objectID1">第一个对象的ID。</param>
        /// <param name="objectID2">第二个对象的ID。</param>
        /// <returns>如果由给定ID指定的对象位于同一集合中，则为true；否则为false。</returns>
        bool InSameSet(int objectID1, int objectID2);
    }

    /// <summary>
    /// <see cref="DisjointSet{T}"/> 的只读表示。
    /// </summary>
    [PublicAPI]
    public interface IReadOnlyDisjointSet<T>
    {
        /// <summary>
        /// 当两个集合合并为一个集合时触发。参数提供被合并的两个集合。
        /// </summary>
        public event EventHandler<JoinedEventArgs<T>>? SetsJoined;

        /// <summary>
        /// 不同集合的数量。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 返回包含<paramref name="item" />的集合的父级，并在搜索完成时执行路径压缩。
        /// </summary>
        /// <param name="item">要搜索的对象。</param>
        /// <returns>给定对象的父级。</returns>
        T Find(T item);

        /// <summary>
        /// 如果指定的两个对象位于同一集合中，则返回true。
        /// </summary>
        /// <param name="item1">第一个要检查的对象。</param>
        /// <param name="item2">第二个要检查的对象。</param>
        /// <returns>如果两个对象位于同一集合中，则为true；否则为false。</returns>
        bool InSameSet(T item1, T item2);
    }
}
