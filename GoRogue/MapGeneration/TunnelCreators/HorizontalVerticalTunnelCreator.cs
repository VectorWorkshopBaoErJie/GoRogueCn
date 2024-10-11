using System;
using System.Collections.Generic;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.TunnelCreators
{
    /// <summary>
    /// 实现了一个隧道创建算法，该算法创建的隧道会先进行所有必要的垂直移动，然后再进行水平移动，
    /// 或者反过来（取决于随机数生成器）。
    /// </summary>
    [PublicAPI]
    public class HorizontalVerticalTunnelCreator : ITunnelCreator
    {
        private readonly IEnhancedRandom _rng;

        /// <summary>
        /// 创建一个新的隧道生成器。
        /// </summary>
        /// <param name="rng">用于移动选择的随机数生成器。</param>
        public HorizontalVerticalTunnelCreator(IEnhancedRandom? rng = null) => _rng = rng ?? GlobalRandom.DefaultRNG;

        /// <inheritdoc />
        public Area CreateTunnel(ISettableGridView<bool> map, Point tunnelStart, Point tunnelEnd)
        {
            var tunnel = new Area();

            if (_rng.NextBool())
            {
                tunnel.Add(CreateHTunnel(map, tunnelStart.X, tunnelEnd.X, tunnelStart.Y));
                tunnel.Add(CreateVTunnel(map, tunnelStart.Y, tunnelEnd.Y, tunnelEnd.X));
            }
            else
            {
                tunnel.Add(CreateVTunnel(map, tunnelStart.Y, tunnelEnd.Y, tunnelStart.X));
                tunnel.Add(CreateHTunnel(map, tunnelStart.X, tunnelEnd.X, tunnelEnd.Y));
            }

            return tunnel;
        }

        /// <inheritdoc />
        public Area CreateTunnel(ISettableGridView<bool> map, int startX, int startY, int endX, int endY)
            => CreateTunnel(map, new Point(startX, startY), new Point(endX, endY));

        private static IEnumerable<Point> CreateHTunnel(ISettableGridView<bool> map, int xStart, int xEnd, int yPos)
        {
            for (var x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); ++x)
            {
                map[x, yPos] = true;
                yield return new Point(x, yPos);
            }
        }

        private static IEnumerable<Point> CreateVTunnel(ISettableGridView<bool> map, int yStart, int yEnd, int xPos)
        {
            for (var y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); ++y)
            {
                map[xPos, y] = true;
                yield return new Point(xPos, y);
            }
        }
    }
}
