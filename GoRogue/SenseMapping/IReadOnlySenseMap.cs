using System.Collections.Generic;
using GoRogue.SenseMapping.Sources;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.SenseMapping
{
    /// <summary>
    /// <see cref="ISenseMap" />的只读接口。
    /// </summary>
    [PublicAPI]
    public interface IReadOnlySenseMap
    {
        /// <summary>
        /// 当前“在”感知地图中的位置的 IEnumerable ，例如所有值不为 0.0 的位置。
        /// </summary>
        IEnumerable<Point> CurrentSenseMap { get; }

        /// <summary>
        /// 在最近一次Calculate调用时，在感知地图中具有非零值，但在上一次Calculate调用后不具有非零值的位置的IEnumerable。
        /// </summary>
        IEnumerable<Point> NewlyInSenseMap { get; }

        /// <summary>
        /// 在最近一次Calculate调用时，不在感知地图中具有非零值，但在上一次Calculate调用后具有非零值的位置的IEnumerable。
        /// </summary>
        IEnumerable<Point> NewlyOutOfSenseMap { get; }

        /// <summary>
        /// 当前被视为感知地图一部分的所有源的只读列表。
        /// 其中一些可能将其<see cref="ISenseSource.Enabled" />标志设置为false，
        /// 因此当调用Calculate时，这些可能会计数，也可能不会。
        /// </summary>
        IReadOnlyList<ISenseSource> SenseSources { get; }

        /// <summary>
        /// 用于执行计算的阻力地图。
        /// </summary>
        public IGridView<double> ResistanceView { get; }

        /// <summary>
        /// 感知地图计算结果的视图
        /// </summary>
        public IGridView<double> ResultView { get; }

        /// <summary>
        ///  返回感知地图的只读表示。
        /// </summary>
        /// <returns>此感知地图对象作为<see cref="IReadOnlySenseMap" />。</returns>
        public IReadOnlySenseMap AsReadOnly();
    }
}
