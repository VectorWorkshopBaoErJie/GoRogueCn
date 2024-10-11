using System;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.TunnelCreators
{
    /// <summary>
    /// 实现了一个隧道创建算法，该算法将两点之间的直线设置为可通行。在使用<see cref="SadRogue.Primitives.Distance.Manhattan" />的情况下，
    /// 通过<see cref="SadRogue.Primitives.Lines.Algorithm.Orthogonal" />算法计算该直线。否则，使用
    /// <see cref="SadRogue.Primitives.Lines.Algorithm.Bresenham" />来计算该直线。
    /// </summary>
    [PublicAPI]
    public class DirectLineTunnelCreator : ITunnelCreator
    {
        private readonly AdjacencyRule _adjacencyRule;
        private readonly bool _doubleWideVertical;

        /// <summary>
        /// 构造函数。采用要使用的距离计算方式，该方式决定了是使用<see cref="SadRogue.Primitives.Lines.Algorithm.Orthogonal" />
        /// 还是<see cref="SadRogue.Primitives.Lines.Algorithm.Bresenham" />来创建隧道。
        /// </summary>
        /// <param name="adjacencyRule">
        /// 创建隧道时要遵守的邻接方法。不能是对角线。
        /// </param>
        /// <param name="doubleWideVertical">是否将垂直隧道创建为2格宽。</param>
        public DirectLineTunnelCreator(AdjacencyRule adjacencyRule, bool doubleWideVertical = true)
        {
            if (adjacencyRule == AdjacencyRule.Diagonals)
                throw new ArgumentException("Cannot use diagonal adjacency to create tunnels", nameof(adjacencyRule));
            _adjacencyRule = adjacencyRule;
            _doubleWideVertical = doubleWideVertical;
        }

        /// <inheritdoc />
        public Area CreateTunnel(ISettableGridView<bool> map, Point start, Point end)
        {
            var lineAlgorithm = _adjacencyRule == AdjacencyRule.Cardinals
                ? Lines.Algorithm.Orthogonal
                : Lines.Algorithm.Bresenham;
            var area = new Area();

            var previous = Point.None;
            foreach (var pos in Lines.GetLine(start, end, lineAlgorithm))
            {
                map[pos] = true;
                area.Add(pos);
                // Previous cell, and we're going vertical, go 2 wide so it looks nicer Make sure not
                // to break rectangles (less than last index)!
                if (_doubleWideVertical && previous != Point.None && pos.Y != previous.Y && pos.X + 1 < map.Width - 1)
                {
                    var wideningPos = pos + (1, 0);
                    map[wideningPos] = true;
                    area.Add(wideningPos);
                }

                previous = pos;
            }

            return area;
        }

        /// <inheritdoc />
        public Area CreateTunnel(ISettableGridView<bool> map, int startX, int startY, int endX, int endY)
            => CreateTunnel(map, new Point(startX, startY), new Point(endX, endY));
    }
}
