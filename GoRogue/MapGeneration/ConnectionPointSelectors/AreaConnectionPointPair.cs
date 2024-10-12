using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// 一对不同区域中的点，这些点已被一个<see cref="IConnectionPointSelector"/>选为连接点。
    /// </summary>
    [DataContract]
    [PublicAPI]
    public struct AreaConnectionPointPair : IEquatable<AreaConnectionPointPair>, IMatchable<AreaConnectionPointPair>
    {
        /// <summary>
        /// 预期组件的类型。
        /// </summary>
        [DataMember] public readonly Point Area1Position;

        /// <summary>
        /// 预期与指定类型的组件相关联的标签。
        /// </summary>
        [DataMember] public readonly Point Area2Position;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="area1Position">区域1的位置。</param>
        /// <param name="area2Position">区域2的位置。</param>
        public AreaConnectionPointPair(Point area1Position, Point area2Position)
        {
            Area1Position = area1Position;
            Area2Position = area2Position;
        }

        /// <summary>
        /// 返回一个表示这两个点的字符串。
        /// </summary>
        /// <returns>表示两个点的字符串。</returns>
        public override string ToString() => $"{Area1Position} <-> {Area2Position}";

        #region Tuple Compatibility

        /// <summary>
        /// 支持C#的解构语法。
        /// </summary>
        /// <param name="area1Position">区域1的位置输出参数。</param>
        /// <param name="area2Position">区域2的位置输出参数。</param>
        public void Deconstruct(out Point area1Position, out Point area2Position)
        {
            area1Position = Area1Position;
            area2Position = Area2Position;
        }

        /// <summary>
        /// 隐式地将AreaConnectionPointPair转换为等效的元组。
        /// </summary>
        /// <param name="pair">要转换的AreaConnectionPointPair对象。</param>
        /// <returns>等效的元组。</returns>
        public static implicit operator (Point area1Position, Point area2Position)(AreaConnectionPointPair pair)
            => pair.ToTuple();

        /// <summary>
        /// 隐式地将元组转换为等效的AreaConnectionPointPair。
        /// </summary>
        /// <param name="tuple">要转换的元组。</param>
        /// <returns>等效的AreaConnectionPointPair对象。</returns>
        public static implicit operator AreaConnectionPointPair((Point area1Position, Point area2Position) tuple)
            => FromTuple(tuple);

        /// <summary>
        /// 将配对转换为等效的元组。
        /// </summary>
        /// <returns>等效的元组。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Point area1Position, Point area2Position) ToTuple() => (Area1Position, Area2Position);

        /// <summary>
        /// 将元组转换为等效的AreaConnectionPointPair。
        /// </summary>
        /// <param name="tuple">要转换的元组。</param>
        /// <returns>等效的AreaConnectionPointPair对象。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AreaConnectionPointPair FromTuple((Point area1Position, Point area2Position) tuple)
            => new AreaConnectionPointPair(tuple.area1Position, tuple.area2Position);
        #endregion

        #region Equality Comparison

        /// <summary>
        /// 如果给定的配对包含相同的点，则为True；否则为False。
        /// </summary>
        /// <param name="other">要与当前对象进行比较的配对。</param>
        /// <returns>一个布尔值，表示两个配对是否相等。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AreaConnectionPointPair other)
            => Area1Position == other.Area1Position && Area2Position == other.Area2Position;

        /// <summary>
        /// 如果给定的配对包含相同的点，则为True；否则为False。
        /// </summary>
        /// <param name="other">要与当前配对进行比较的另一个配对。</param>
        /// <returns>一个布尔值，表示两个配对是否匹配。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(AreaConnectionPointPair other) => Equals(other);

        /// <summary>
        /// 如果给定的对象是AreaConnectionPointPair并且具有相同的点，则为True；否则为False。
        /// </summary>
        /// <param name="obj">要与当前对象进行比较的对象。</param>
        /// <returns>一个布尔值，表示两个对象是否相等。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is AreaConnectionPointPair pair && Equals(pair);

        /// <summary>
        /// 基于配对的所有字段返回一个哈希码。
        /// </summary>
        /// <returns>配对的哈希码。</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Area1Position.GetHashCode() ^ Area2Position.GetHashCode();

        /// <summary>
        /// 如果给定的两个配对包含相同的点，则为True；否则为False。
        /// </summary>
        /// <param name="left">要比较的第一个配对。</param>
        /// <param name="right">要比较的第二个配对。</param>
        /// <returns>一个布尔值，表示两个配对是否相同。</returns>
        public static bool operator ==(AreaConnectionPointPair left, AreaConnectionPointPair right)
            => left.Equals(right);

        /// <summary>
        /// 如果给定的两个配对的第一个点和第二个点分别不同，则为True；否则为False。
        /// </summary>
        /// <param name="left">要比较的第一个配对。</param>
        /// <param name="right">要比较的第二个配对。</param>
        /// <returns>一个布尔值，表示两个配对是否不同。</returns>
        public static bool operator !=(AreaConnectionPointPair left, AreaConnectionPointPair right) => !(left == right);
        #endregion
    }
}
