using System;
using GoRogue.SenseMapping.Sources;
using JetBrains.Annotations;

namespace GoRogue.SenseMapping
{
    /// <summary>
    /// 用于计算代表感官（声音、光线等）的地图，或通常可以建模为源通过具有不同程度传播阻力的地图传播的任何事物的接口。
    /// </summary>
    /// <remarks>
    /// 如果您正在寻找此接口的现有实现以供使用，请参阅<see cref="SenseMap"/>。如果您想实现自己的接口，
    /// 可以考虑使用<see cref="SenseMapBase"/>作为基类，因为它可以大大简化实现。
    /// 
    /// 此接口基于具有一个或多个<see cref="ISenseSource"/>实例的概念，这些实例能够使用某种算法将源传播通过地图。
    /// 因此，地图和每个源都使用double类型的网格视图作为其地图表示，其中每个double值代表该位置对源值通过的“阻力”。
    /// 值必须大于等于0.0，其中0.0表示位置对源值的传播没有阻力，而更大的值表示更大的阻力。
    /// 这个阻力的规模是任意的，并且与您的源的<see cref="ISenseSource.Intensity" />有关。
    ///
    /// 除了0.0表示单元格没有阻力的约束之外，接口/API本身对强度和阻力值的定义没有严格的限制。
    /// 在GoRogue中，这些接口的默认实现将阻力视图值视为源在通过该单元格传播时从其剩余强度中减去的值；
    /// 因此，当源通过给定位置传播时，会从源的值中减去等于该位置阻力值的量（加上正常的距离衰减）。
    /// 但是，如果需要其他方法，可以实现自定义的<see cref="ISenseSource"/>；有关详细信息，请参阅该接口的文档。
    ///
    /// 通常，使用涉及通过调用<see cref="ISenseMap.Calculate" />函数对所有感官源执行和聚合计算，
    /// 然后通过<see cref="IReadOnlySenseMap.ResultView"/>访问结果。如果没有源传播到该位置，则这些值将为0.0，
    /// 而大于0.0的值则表示传播到该位置的组合源的强度。
    /// </remarks>
    [PublicAPI]
    public interface ISenseMap : IReadOnlySenseMap
    {
        /// <summary>
        /// 当SenseMap重新计算时触发。
        /// </summary>
        event EventHandler? Recalculated;

        /// <summary>
        /// 在计算新的SenseMap之前，当现有的SenseMap被重置时触发。
        /// </summary>
        event EventHandler? SenseMapReset;

        /// <summary>
        /// 将给定的源添加到源列表中。如果在下次调用<see cref="Calculate" />时，
        /// 该源已设置其<see cref="ISenseSource.Enabled" />标志，那么它将被计为一个源。
        /// </summary>
        /// <param name="senseSource">要添加的源。</param>
        void AddSenseSource(ISenseSource senseSource);

        /// <summary>
        /// 从源列表中移除给定的源。通常，如果源从地图中永久移除，则使用此方法。
        /// 对于临时禁用，通常应使用<see cref="ISenseSource.Enabled" />标志。
        /// </summary>
        /// <remarks>
        /// 在下次调用<see cref="Calculate" />之前，此感官源负责的源值不会从感官输出值中移除。
        /// </remarks>
        /// <param name="senseSource">要移除的源。</param>
        public void RemoveSenseSource(ISenseSource senseSource);

        /// <summary>
        /// 计算地图。对于源列表中的每个已启用的源，它都会计算源的传播，并将它们全部组合在感官地图的输出中。
        /// </summary>
        public void Calculate();

        /// <summary>
        /// 通过擦除当前记录的结果值来重置给定的感官地图。
        /// </summary>
        /// <remarks>
        /// 调用此函数后，<see cref="IReadOnlySenseMap.ResultView"/>中的任何值都将为0。
        /// 此外，<see cref="IReadOnlySenseMap.CurrentSenseMap"/>将为空。
        /// </remarks>
        public void Reset();
    }
}
