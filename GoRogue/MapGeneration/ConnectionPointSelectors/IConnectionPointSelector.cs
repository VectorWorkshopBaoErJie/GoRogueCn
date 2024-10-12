using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// 用于实现选择连接位置的算法，以便连接两个给定的区域。
    /// </summary>
    [PublicAPI]
    public interface IConnectionPointSelector
    {
        /// <summary>
        /// 实现算法。返回一对位置——一个在<paramref name="area1"/>中使用，另一个在<paramref name="area2"/>中使用。
        /// </summary>
        /// <param name="area1">第一个要连接的<see cref="SadRogue.Primitives.Area"/>。</param>
        /// <param name="area2">第二个要连接的<see cref="SadRogue.Primitives.Area"/>。</param>
        /// <returns>
        /// 一对用于连接的位置（每个<see cref="SadRogue.Primitives.Area"/>中一个）。
        /// </returns>
        AreaConnectionPointPair SelectConnectionPoints(IReadOnlyArea area1, IReadOnlyArea area2);
    }
}
