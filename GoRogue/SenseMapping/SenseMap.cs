using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoRogue.SenseMapping.Sources;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.SenseMapping
{
    /// <summary>
    /// <see cref="ISenseMap"/>的实现，以一种适用于许多典型用例的方式实现了所需的字段/方法。
    /// </summary>
    /// <remarks>
    /// 此<see cref="ISenseMap"/>实现通过使用一对哈希映射来跟踪当前（和之前）计算调用中非零的位置，从而实现了可枚举。这提供了相对高效的实现，应适用于各种用例。
    ///
    /// 默认情况下，计算首先通过调用所有源的<see cref="ISenseSource.CalculateLight"/>函数来执行。
    /// 如果有多个源且<see cref="ParallelCalculate"/>属性设置为true，则通过 Parallel.ForEach 循环并行执行此操作。
    /// 通常，即使在 2 个感官源的情况下，并行化计算也有显著的好处；但是，请随意使用此标志根据您的用例进行调整。
    ///
    /// 所有计算完成后，<see cref="OnCalculate"/>实现将获取每个源的结果视图，并将其复制到<see cref="IReadOnlySenseMap.ResultView"/>属性的相应部分。
    /// 这是按顺序完成的，以避免重叠源带来的任何问题。值是通过简单地将当前值和新值相加来聚合的。
    ///
    /// 如果您想自定义值的聚合方式，可以自定义<see cref="ApplySenseSourceToResult"/>函数。
    /// 此函数用于将指定感官源的结果视图应用到结果视图上。如果您只是想更改聚合方法，则可以复制粘贴该函数并更改执行聚合的行；
    /// 出于性能原因，聚合方法本身不作为单独的函数提供。您也可以重写此函数以自定义执行聚合的顺序/方法。
    ///
    /// 大多数其他自定义将需要重写<see cref="OnCalculate"/>函数。
    ///
    /// 您还可以直接实现该接口或从<see cref="SenseMapBase"/>继承来创建自己的<see cref="ISenseSource"/>实现。
    /// 例如，如果您想避免在可枚举实现中使用哈希集合，这可能是最佳选择。
    /// </remarks>
    [PublicAPI]
    public class SenseMap : SenseMapBase
    {
        /// <summary>
        /// 一个哈希集合，包含最近一次计算结果中非零值的位置。
        /// </summary>
        /// <remarks>
        /// 这个哈希集合是<see cref="NewlyInSenseMap"/>、<see cref="NewlyOutOfSenseMap"/>以及<see cref="CurrentSenseMap"/>的后备结构。
        /// 在<see cref="OnCalculate"/>期间，执行新计算之前会清除此值。
        ///
        /// 通常，只有在重写<see cref="OnCalculate"/>时才需要与这个哈希集合交互；在这种情况下，如果不调用此类的实现，则需要自己执行清除操作。
        ///
        /// 为了保留在类启动时传递给它的任何哈希器的使用，建议不要完全重新分配这个结构。
        /// 请参阅<see cref="OnCalculate"/>，了解一种管理这个结构和<see cref="PreviousSenseMapBacking"/>的方法，该方法不涉及重新分配。
        /// </remarks>
        protected HashSet<Point> CurrentSenseMapBacking;
        /// <inheritdoc />
        public override IEnumerable<Point> CurrentSenseMap => CurrentSenseMapBacking;

        /// <summary>
        /// 一个哈希集合，包含前一次计算结果中具有非零值的位置。
        /// </summary>
        /// <remarks>
        /// 这个哈希集合是<see cref="NewlyInSenseMap"/>和<see cref="NewlyOutOfSenseMap"/>的后备结构。
        /// 
        /// 通常，只有在重写<see cref="OnCalculate"/>时才需要与这个哈希集合交互；在这种情况下，如果不调用此类的实现，
        /// 则需要在执行新计算之前确保此集合被适当地设置。
        ///
        /// 为了保留在类启动时传递给它的任何哈希器的使用，建议不要完全重新分配这个结构。
        /// 请参阅<see cref="OnCalculate"/>，了解一种不涉及重新分配的管理这个结构和<see cref="CurrentSenseMapBacking"/>的方法。
        /// </remarks>
        protected HashSet<Point> PreviousSenseMapBacking;

        /// <summary>
        /// 是否并行计算每个感知源的扩散算法。如果仅添加了一个源，则此设置无效。
        /// </summary>
        /// <remarks>
        /// 当此设置为true时，将通过多个线程并行调用<see cref="ISenseSource.CalculateLight"/>。
        /// 将使用Parallel.ForEach，这将启用线程池的使用。
        /// 
        /// 在任何情况下，感知源的默认实现总是具有它们自己的结果视图，它们在这些视图上执行它们的计算，
        /// 因此无需担心源重叠的问题。这不会影响从感知源的本地结果视图到感知映射的值的复制，
        /// 在默认实现中，该复制过程始终是顺序的。
        /// </remarks>
        public bool ParallelCalculate { get; set; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="resistanceView">用于计算的阻力视图。</param>
        /// <param name="resultViewAndResizer">
        /// 用于存储感知地图计算结果的视图，以及一个按需调整其大小的方法。
        ///
        /// 如果未指定或为 null ，则将为结果视图使用 ArrayView ，并且调整大小函数将按需分配适当大小的新 ArrayView 。这对于大多数用例应该足够了。
        ///
        /// 调整大小函数必须返回一个具有给定宽度和高度的视图，且其所有值都设置为0.0。
        /// </param>
        /// <param name="parallelCalculate">是否使用 Parallel.ForEach 并行计算感知源。如果仅添加了一个源，则此设置无效。</param>
        /// <param name="hasher">用于哈希集合中点的哈希算法。默认为 Points 的默认哈希算法。</param>
        public SenseMap(IGridView<double> resistanceView, CustomResultViewWithResize? resultViewAndResizer = null,
            bool parallelCalculate = true, IEqualityComparer<Point>? hasher = null)
            : base(resistanceView, resultViewAndResizer)
        {
            ParallelCalculate = parallelCalculate;
            hasher ??= EqualityComparer<Point>.Default;

            PreviousSenseMapBacking = new HashSet<Point>(hasher);
            CurrentSenseMapBacking = new HashSet<Point>(hasher);
        }

        /// <inheritdoc />
        public override IEnumerable<Point> NewlyInSenseMap => CurrentSenseMapBacking.Where(pos => !PreviousSenseMapBacking.Contains(pos));

        /// <inheritdoc />
        public override IEnumerable<Point> NewlyOutOfSenseMap => PreviousSenseMapBacking.Where(pos => !CurrentSenseMapBacking.Contains(pos));

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();

            // 循环使用当前和之前的哈希集合，以避免重新分配内部缓冲区
            (PreviousSenseMapBacking, CurrentSenseMapBacking) = (CurrentSenseMapBacking, PreviousSenseMapBacking);
            CurrentSenseMapBacking.Clear();
        }

        /// <inheritdoc />
        protected override void OnCalculate()
        {
            // 超过1个感知源的情况下，并行执行似乎能带来显著的好处
            if (SenseSources.Count > 1 && ParallelCalculate)
                Parallel.ForEach(SenseSources, senseSource => { senseSource.CalculateLight(); });
            else
                foreach (var senseSource in SenseSources)
                    senseSource.CalculateLight();

            // 将源数据刷新到实际的senseMap中
            foreach (var senseSource in SenseSources)
                ApplySenseSourceToResult(senseSource);
        }

        /// <summary>
        /// 接收给定的源并将其值应用到<see cref="SenseMapBase.ResultViewBacking"/>的适当子区域。
        /// 将任何最终具有非0值的位置添加到<see cref="CurrentSenseMapBacking"/>哈希集中。
        /// </summary>
        /// <remarks>
        /// 如果您需要控制聚合函数（例如，执行除将值相加以外的其他操作），或者您想以不同的方式将感知源计算的结果应用到感知图上，请重写此方法。
        /// </remarks>
        /// <param name="source">要应用的源。</param>
        protected virtual void ApplySenseSourceToResult(ISenseSource source)
        {
            // 根据位置约束，计算实际的半径边界
            var minX = Math.Min((int)source.Radius, source.Position.X);
            var minY = Math.Min((int)source.Radius, source.Position.Y);
            var maxX = Math.Min((int)source.Radius, ResistanceView.Width - 1 - source.Position.X);
            var maxY = Math.Min((int)source.Radius, ResistanceView.Height - 1 - source.Position.Y);

            // 使用半径边界来推断全局坐标系的最小值
            var gMin = source.Position - new Point(minX, minY);

            // 使用半径边界来推算实际绘制的局部光照坐标系的最小和最大边界
            var lMin = new Point((int)source.Radius - minX, (int)source.Radius - minY);
            var lMax = new Point((int)source.Radius + maxX, (int)source.Radius + maxY);

            for (var xOffset = 0; xOffset <= lMax.X - lMin.X; xOffset++)
                for (var yOffset = 0; yOffset <= lMax.Y - lMin.Y; yOffset++)
                {
                    // 适当偏移局部/当前位置，并更新光照贴图
                    var c = new Point(xOffset, yOffset);
                    var gCur = gMin + c;
                    var lCur = lMin + c;

                    // 此处不进行空值检查，因为在添加源时会设置ResistanceView，所以除非有人不当修改了不应修改的值，
                    // 否则不会发生空引用。而且，添加检查会影响性能。
                    ResultViewBacking[gCur.X, gCur.Y] += source.ResultView[lCur.X, lCur.Y];
                    if (ResultViewBacking[gCur.X, gCur.Y] > 0.0)
                        CurrentSenseMapBacking.Add(gCur);
                }
        }
    }
}
