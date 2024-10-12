using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ContextComponents
{
    /// <summary>
    /// 添加到生成上下文组件中的项以及添加该项的步骤名称。
    /// </summary>
    /// <typeparam name="TItem">存储在配对中的项的类型。</typeparam>
    [DataContract]
    [PublicAPI]
    public readonly struct ItemStepPair<TItem> : IEquatable<ItemStepPair<TItem>>, IMatchable<ItemStepPair<TItem>>
        where TItem : notnull
    {
        /// <summary>
        /// 项。
        /// </summary>
        [DataMember] public readonly TItem Item;

        /// <summary>
        /// 与创建该项的步骤相关联的名称。
        /// </summary>
        [DataMember] public readonly string Step;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="item">项。</param>
        /// <param name="step">步骤名称。</param>
        public ItemStepPair(TItem item, string step)
        {
            Item = item;
            Step = step;
        }

        /// <summary>
        /// 返回一个字符串，表示项和添加该项的步骤名称。
        /// </summary>
        /// <returns>表示项和步骤名称的字符串。</returns>
        [Pure]
        public override string ToString() => $"{Item}: {Step}";

        #region Tuple Compatibility

        /// <summary>
        /// 支持C#解构语法。
        /// </summary>
        /// <param name="item">输出的项。</param>
        /// <param name="step">输出的步骤名称。</param>
        public void Deconstruct(out TItem item, out string step)
        {
            item = Item;
            step = Step;
        }

        /// <summary>
        /// 将ItemStepPair隐式转换为等效的元组。
        /// </summary>
        /// <param name="pair">要转换的ItemStepPair对象。</param>
        /// <returns>等效的元组。</returns>
        public static implicit operator (TItem item, string step)(ItemStepPair<TItem> pair) => pair.ToTuple();

        /// <summary>
        /// 将元组隐式转换为等效的ItemStepPair。
        /// </summary>
        /// <param name="tuple">要转换的元组。</param>
        /// <returns>转换后的ItemStepPair对象。</returns>
        public static implicit operator ItemStepPair<TItem>((TItem item, string step) tuple) => FromTuple(tuple);

        /// <summary>
        /// 将配对转换为等效的元组。
        /// </summary>
        /// <returns>等效的元组。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (TItem item, string step) ToTuple() => (Item, Step);

        /// <summary>
        /// 将元组转换为等效的ItemStepPair。
        /// </summary>
        /// <param name="tuple">要转换的元组。</param>
        /// <returns>转换后的ItemStepPair对象。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("ReSharper", "CA1000")] // Must be static to comply with implicit operator rules
        public static ItemStepPair<TItem> FromTuple((TItem item, string step) tuple)
            => new ItemStepPair<TItem>(tuple.item, tuple.step);

        #endregion

        #region EqualityComparison

        /// <summary>
        /// 如果给定的配对具有相同的项且这些项是由相同的步骤生成的，则为 true；否则为 false。
        /// </summary>
        /// <param name="other">要与当前对象进行比较的另一个 ItemStepPair。</param>
        /// <returns>一个布尔值，表示两个 ItemStepPair 是否具有相同的项和步骤。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ItemStepPair<TItem> other) => Item.Equals(other.Item) && Step == other.Step;

        /// <summary>
        /// 如果给定的配对具有相同的项且这些项是由相同的步骤生成的，则为 true；否则为 false。
        /// </summary>
        /// <param name="other">要与当前对象进行匹配检查的另一个 ItemStepPair。</param>
        /// <returns>一个布尔值，表示两个 ItemStepPair 是否匹配。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(ItemStepPair<TItem> other) => Equals(other);

        /// <summary>
        /// 如果给定的对象是一个 ItemStepPair，且它具有一个相同的项，该项是由相同的步骤生成的，则为 true；
        /// 否则为 false。
        /// </summary>
        /// <param name="obj">要与当前对象进行比较的对象。</param>
        /// <returns>一个布尔值，表示给定的对象是否与当前 ItemStepPair 具有相同的项和步骤。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is ItemStepPair<TItem> pair && Equals(pair);

        /// <summary>
        /// 基于配对的所有字段返回一个哈希码。
        /// </summary>
        /// <returns>配对的哈希码。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Item.GetHashCode() ^ Step.GetHashCode();

        /// <summary>
        /// 如果给定的配对具有相同的组件和标签，则为 true；否则为 false。
        /// </summary>
        /// <param name="left">要比较的第一个 ItemStepPair。</param>
        /// <param name="right">要比较的第二个 ItemStepPair。</param>
        /// <returns>一个布尔值，表示两个 ItemStepPair 是否具有相同的组件和标签。</returns>
        public static bool operator ==(ItemStepPair<TItem> left, ItemStepPair<TItem> right) => left.Equals(right);

        /// <summary>
        /// 如果给定的配对具有不同的组件或/和标签，则为 true；否则为 false。
        /// </summary>
        /// <param name="left">要比较的第一个 ItemStepPair。</param>
        /// <param name="right">要比较的第二个 ItemStepPair。</param>
        /// <returns>一个布尔值，表示两个 ItemStepPair 是否具有不同的组件或/和标签。</returns>
        public static bool operator !=(ItemStepPair<TItem> left, ItemStepPair<TItem> right) => !(left == right);

        #endregion
    }
}
