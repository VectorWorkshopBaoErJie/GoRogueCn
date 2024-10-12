using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// 实现了一种选择算法，该算法选择给定<see cref="SadRogue.Primitives.Area" />实例的边界框中心点作为连接点。
    /// </summary>
    [PublicAPI]
    public class CenterBoundsConnectionPointSelector : IConnectionPointSelector
    {
        /// <inheritdoc />
        public AreaConnectionPointPair SelectConnectionPoints(IReadOnlyArea area1, IReadOnlyArea area2)
            => new AreaConnectionPointPair(area1.Bounds.Center, area2.Bounds.Center);
    }
}
