using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// 实现了一种选择算法，该算法在给定的<see cref="SadRogue.Primitives.Area" />实例中选择彼此最接近的两个点。
    /// </summary>
    [PublicAPI]
    public class ClosestConnectionPointSelector : IConnectionPointSelector
    {
        /// <summary>
        /// 用于确定接近度的距离计算方式。
        /// </summary>
        public readonly Distance DistanceCalculation;

        /// <summary>
        /// 创建一个新的点选择器。
        /// </summary>
        /// <param name="distanceCalculation">用于确定接近度的距离计算方式。</param>
        public ClosestConnectionPointSelector(Distance distanceCalculation)
            => DistanceCalculation = distanceCalculation;

        /// <inheritdoc />
        public AreaConnectionPointPair SelectConnectionPoints(
            IReadOnlyArea area1, IReadOnlyArea area2)
        {
            var c1 = Point.None;
            var c2 = Point.None;
            var minDist = double.MaxValue;

            foreach (var point1 in area1)
                foreach (var point2 in area2)
                {
                    var distance = DistanceCalculation.Calculate(point1, point2);
                    if (distance < minDist)
                    {
                        c1 = point1;
                        c2 = point2;
                        minDist = distance;
                    }
                }

            return new AreaConnectionPointPair(c1, c2);
        }
    }
}
