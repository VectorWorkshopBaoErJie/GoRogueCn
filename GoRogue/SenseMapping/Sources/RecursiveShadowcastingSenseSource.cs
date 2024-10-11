using System;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.SenseMapping.Sources
{
    /// <summary>
    /// 一种感知源，它使用递归阴影投射算法执行其扩散计算。
    /// </summary>
    /// <remarks>
    /// 阻力图上任何非完全阻挡（例如，其值小于源的<see cref="ISenseSource.Intensity"/>）的位置都被视为完全透明，
    /// 因为此阴影投射实现是一种开关算法。
    ///
    /// 此计算速度更快，但显然不支持部分阻力。当你只需要粗略的光线近似值，或者你的阻力图本身就是开关类型时，它可能很有用。
    /// </remarks>
    [PublicAPI]
    public class RecursiveShadowcastingSenseSource : SenseSourceBase
    {
        /// <summary>
        /// 创建一个向所有方向向外扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是在区域完全无阻碍的情况下，源值将散发的最大距离。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或是可隐式转换为<see cref="Distance"/>的类型，例如<see cref="Radius"/>）。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RecursiveShadowcastingSenseSource(Point position, double radius, Distance distanceCalc, double intensity = 1)
            : base(position, radius, distanceCalc, intensity)
        { }

        /// <summary>
        /// 创建一个向所有方向向外扩散的源。
        /// </summary>
        /// <param name="positionX">
        /// 源在地图上位置的X值。
        /// </param>
        /// <param name="positionY">
        /// 源在地图上位置的Y值。
        /// </param>
        /// <param name="radius">
        /// 源的最大半径——这是在区域完全无阻碍的情况下，源值将散发的最大距离。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或是可隐式转换为<see cref="Distance"/>的类型，例如<see cref="Radius"/>）。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RecursiveShadowcastingSenseSource(int positionX, int positionY, double radius, Distance distanceCalc, double intensity = 1)
            : base(positionX, positionY, radius, distanceCalc, intensity)
        { }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的圆锥体内扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是在区域完全无阻碍的情况下，源值将散发的最大距离。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或是可隐式转换为<see cref="Distance"/>的类型，例如<see cref="Radius"/>）。
        /// </param>
        /// <param name="angle">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体的最外侧中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体中包含的完整弧段——
        /// 圆锥体中心线的两侧各包含<paramref name="angle"/> / 2度。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RecursiveShadowcastingSenseSource(Point position, double radius, Distance distanceCalc, double angle, double span, double intensity = 1)
            : base(position, radius, distanceCalc, angle, span, intensity)
        { }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的圆锥体内扩散的源。
        /// </summary>
        /// <param name="positionX">源在地图上所在位置的x值。</param>
        /// <param name="positionY">源在地图上所在位置的y值。</param>
        /// <param name="radius">
        /// 源的最大半径——这是在区域完全无阻碍的情况下，源值将散发的最大距离。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或是可隐式转换为<see cref="Distance"/>的类型，例如<see cref="Radius"/>）。
        /// </param>
        /// <param name="angle">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体的最外侧中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体中包含的完整弧段——
        /// 圆锥体中心线的两侧各包含<paramref name="angle"/> / 2度。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RecursiveShadowcastingSenseSource(int positionX, int positionY, double radius, Distance distanceCalc, double angle, double span, double intensity = 1)
            : base(positionX, positionY, radius, distanceCalc, angle, span, intensity)
        { }

        /// <summary>
        /// 通过递归阴影投射执行扩散计算。
        /// </summary>
        public override void OnCalculate()
        {
            if (IsAngleRestricted)
            {
                var angle = AngleInternal * SadRogue.Primitives.MathHelpers.DegreePctOfCircle;
                var span = Span * SadRogue.Primitives.MathHelpers.DegreePctOfCircle;

                ShadowCast(1, 1.0, 0.0, 0, 1, 1, 0, angle, span);
                ShadowCast(1, 1.0, 0.0, 1, 0, 0, 1, angle, span);

                ShadowCast(1, 1.0, 0.0, 0, -1, 1, 0, angle, span);
                ShadowCast(1, 1.0, 0.0, -1, 0, 0, 1, angle, span);

                ShadowCast(1, 1.0, 0.0, 0, -1, -1, 0, angle, span);
                ShadowCast(1, 1.0, 0.0, -1, 0, 0, -1, angle, span);

                ShadowCast(1, 1.0, 0.0, 0, 1, -1, 0, angle, span);
                ShadowCast(1, 1.0, 0.0, 1, 0, 0, -1, angle, span);
            }
            else
                for (var i = 0; i < AdjacencyRule.Diagonals.DirectionsOfNeighborsCache.Length; i++)
                {
                    var d = AdjacencyRule.Diagonals.DirectionsOfNeighborsCache[i];

                    ShadowCast(1, 1.0, 0.0, 0, d.DeltaX, d.DeltaY, 0, 0, 0);
                    ShadowCast(1, 1.0, 0.0, d.DeltaX, 0, 0, d.DeltaY, 0, 0);
                }
        }

        private void ShadowCast(int row, double start, double end, int xx, int xy, int yx, int yy, double angle, double span)
        {
            double newStart = 0;
            if (start < end)
                return;

            var blocked = false;
            for (var distance = row; distance <= Radius && distance < 2 * Size && !blocked; distance++)
            {
                var deltaY = -distance;
                for (var deltaX = -distance; deltaX <= 0; deltaX++)
                {
                    var currentX = Center + deltaX * xx + deltaY * xy;
                    var currentY = Center + deltaX * yx + deltaY * yy;
                    // TODO: Is this round correct for negative coords?
                    var gCurrentX = Position.X - (int)Radius + currentX;
                    var gCurrentY = Position.Y - (int)Radius + currentY;
                    double leftSlope = (deltaX - 0.5f) / (deltaY + 0.5f);
                    double rightSlope = (deltaX + 0.5f) / (deltaY - 0.5f);

                    if (!(gCurrentX >= 0 && gCurrentY >= 0 && gCurrentX < ResistanceView!.Width && gCurrentY < ResistanceView.Height) ||
                        start < rightSlope)
                        continue;

                    if (end > leftSlope)
                        break;

                    var deltaRadius = DistanceCalc.Calculate(deltaX, deltaY);
                    var inSpan = true;
                    if (IsAngleRestricted)
                    {
                        var at2 = Math.Abs(
                            angle - MathHelpers.ScaledAtan2Approx(currentY - Center, currentX - Center));
                        inSpan = at2 <= span * 0.5 || at2 >= 1.0 - span * 0.5;
                    }
                    if (deltaRadius <= Radius && inSpan)
                    {
                        var bright = Intensity - Decay * deltaRadius;
                        ResultViewBacking[currentX, currentY] = bright;
                    }

                    if (blocked) // Previous cell was blocked
                        if (ResistanceView![gCurrentX, gCurrentY] >= Intensity) // Hit a wall...
                            newStart = rightSlope;
                        else
                        {
                            blocked = false;
                            start = newStart;
                        }
                    else
                        if (ResistanceView![gCurrentX, gCurrentY] >= Intensity && distance < Radius) // Wall within FOV
                        {
                            blocked = true;
                            ShadowCast(distance + 1, start, leftSlope, xx, xy, yx, yy, angle, span);
                            newStart = rightSlope;
                        }
                }
            }
        }
    }
}
