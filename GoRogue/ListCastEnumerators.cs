using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue
{
    /// <summary>
    /// 一个自定义枚举器，它遍历一个列表并将其对象转换为给定的类型。
    ///
    /// 所有对象_必须_是指定的类型，否则迭代器将无法正常工作。
    /// </summary>
    /// <remarks>
    /// 这个类型是一个结构体，因此在foreach循环中使用时，比返回IEnumerable<T>的函数或使用System.LINQ扩展（如Where）要高效得多。
    ///
    /// 否则，它基本上具有将列表公开为<see cref="IEnumerable{T}"/>的相同特性；
    /// 因此，如果您需要将项目公开为IEnumerable之类的类型，并且这些项目在内部存储为列表，那么这是一个不错的选择。
    /// 此类型确实实现了IEnumerable，因此可以直接与需要它的函数（例如，System.LINQ）一起使用。
    /// 但是，由于迭代器的装箱，这将降低性能。
    /// </remarks>
    /// <typeparam name="TBase">列表中存储的项目的类型。</typeparam>
    /// <typeparam name="TItem">列表中项目的实际类型。</typeparam>
    [PublicAPI]
    public struct ListCastEnumerator<TBase, TItem> : IEnumerator<TItem>, IEnumerable<TItem>
        where TItem : TBase
    {
        private List<TBase>.Enumerator _enumerator;
        private TItem _current;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="list">要迭代的列表。</param>
        public ListCastEnumerator(List<TBase> list)
        {
            _enumerator = list.GetEnumerator();
            _current = default!;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext()) return false;

            _current = (TItem)_enumerator.Current!;
            return true;
        }

        /// <inheritdoc/>
        public TItem Current => _current;

        object? IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            ((IEnumerator)_enumerator).Reset();
        }

        /// <summary>
        /// 返回此枚举器。
        /// </summary>
        /// <returns>此枚举器。</returns>
        public ListCastEnumerator<TBase, TItem> GetEnumerator() => this;

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;
    }

    /// <summary>
    /// 一个与<see cref="ListCastEnumerator{TBase, TItem}"/>类似的结构，但适用于<see cref="IReadOnlyList{T}"/>。它的速度不如<see cref="ListCastEnumerator{TBase, TItem}"/>快，
    /// 但仍然比使用 IReadOnlyList 的典型 Enumerable 实现要快。仅当由于所处理的类型而无法使用<see cref="ListCastEnumerator{TBase, TItem}"/>时，才应使用它；
    /// 否则，它们具有相同的特性。
    ///
    /// 所有对象_必须_为指定的类型，否则迭代器将无法正常工作。
    /// </summary>
    /// <typeparam name="TBase">列表中存储的项的类型。</typeparam>
    /// <typeparam name="TItem">列表中项的实际类型。</typeparam>
    [PublicAPI]
    public struct ReadOnlyListCastEnumerator<TBase, TItem> : IEnumerator<TItem>, IEnumerable<TItem>
        where TItem : TBase
    {
        private ReadOnlyListEnumerator<TBase> _enumerator;
        private TItem _current;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="list">要遍历的列表。</param>
        public ReadOnlyListCastEnumerator(IReadOnlyList<TBase> list)
        {
            _enumerator = new ReadOnlyListEnumerator<TBase>(list);
            _current = default!;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext()) return false;

            _current = (TItem)_enumerator.Current!;
            return true;
        }

        /// <inheritdoc/>
        public TItem Current => _current;

        object? IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            ((IEnumerator)_enumerator).Reset();
        }

        /// <summary>
        /// 返回此枚举器。
        /// </summary>
        /// <returns>此枚举器。</returns>
        public ReadOnlyListCastEnumerator<TBase, TItem> GetEnumerator() => this;

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
