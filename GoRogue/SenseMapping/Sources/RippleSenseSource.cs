using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.SenseMapping.Sources
{
    /// <summary>
    /// 源值从其源位置扩散的不同 Ripple 算法类型。
    /// </summary>
    [PublicAPI]
    public enum RippleType
    {
        /// <summary>
        /// 通过从源位置推送值来进行计算。源值会稍微绕过角落扩散。
        /// </summary>
        Regular,

        /// <summary>
        /// 类似于<see cref="Regular" />，但具有不同的扩散机制。值像烟雾或水一样绕过边缘扩散，
        /// 但在绕过边缘时保持向起始位置卷曲的趋势。
        /// </summary>
        Loose,

        /// <summary>
        /// 类似于<see cref="Regular" />，但值只稍微绕过角落扩散。
        /// </summary>
        Tight,

        /// <summary>
        /// 类似于<see cref="Regular" />，但值会大量绕过角落扩散。
        /// </summary>
        VeryLoose
    }

    /// <summary>
    /// 使用“ripple”算法执行其扩散计算的感知源。
    /// </summary>
    /// <remarks>
    /// 值从中心向外扩散，随着距离的增加而减小，并根据它们遇到的每个单元格的阻力进行调整。
    ///
    /// 提供了算法的几种变体，它们产生略有不同的扩散趋势。有关详细信息，请参阅<see cref="RippleType"/>值的文档。
    /// </remarks>
    [PublicAPI]
    public class RippleSenseSource : SenseSourceBase
    {
        /// <summary>
        /// 正在使用的ripple算法的变体。请参阅<see cref="Sources.RippleType"/>值的文档以了解每个变体的描述。
        /// </summary>
        public RippleType RippleType { get; set; }

        private BitArray _nearLight;
        // 预分配的列表，以避免重新分配小数组
        private readonly List<Point> _neighbors;
        private readonly Queue<Point> _dq = new Queue<Point>();

        /// <summary>
        /// 创建一个向所有方向扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或可隐式转换为<see cref="Distance" />的类型，如<see cref="Radius" />）。
        /// </param>
        /// <param name="rippleType">要使用的ripple算法的变体。请参阅<see cref="Sources.RippleType"/>值的文档以了解每个变体的描述。</param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RippleSenseSource(Point position, double radius, Distance distanceCalc,
            RippleType rippleType = RippleType.Regular, double intensity = 1)
            : base(position, radius, distanceCalc, intensity)
        {
            RippleType = rippleType;

            _nearLight = new BitArray(Size * Size);
            RadiusChanged += OnRadiusChanged;

            // Stores max of 8 neighbors
            _neighbors = new List<Point>(8);
        }

        /// <summary>
        /// 创建一个向所有方向扩散的源。
        /// </summary>
        /// <param name="positionX">
        /// 源在地图上位置的X值。
        /// </param>
        /// <param name="positionY">
        /// 源在地图上位置的Y值。
        /// </param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或可隐式转换为<see cref="Distance" />的类型，如<see cref="Radius" />）。
        /// </param>
        /// <param name="rippleType">要使用的ripple算法的变体。请参阅<see cref="Sources.RippleType"/>值的文档以了解每个变体的描述。</param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RippleSenseSource(int positionX, int positionY, double radius, Distance distanceCalc,
            RippleType rippleType = RippleType.Regular, double intensity = 1)
            : base(positionX, positionY, radius, distanceCalc, intensity)
        {
            RippleType = rippleType;

            _nearLight = new BitArray(Size * Size);
            RadiusChanged += OnRadiusChanged;

            // Stores max of 8 neighbors
            _neighbors = new List<Point>(8);
        }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的圆锥体内扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或可隐式转换为<see cref="Distance" />的类型，如<see cref="Radius" />）。
        /// </param>
        /// <param name="angle">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体的最外中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体中所包含的全弧——
        /// <paramref name="angle" /> / 2度包含在圆锥体中心线的任一侧。
        /// </param>
        /// <param name="rippleType">要使用的ripple算法的变体。请参阅<see cref="Sources.RippleType"/>值的文档以了解每个变体的描述。</param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RippleSenseSource(Point position, double radius, Distance distanceCalc, double angle, double span,
            RippleType rippleType = RippleType.Regular, double intensity = 1)
            : base(position, radius, distanceCalc, angle, span, intensity)
        {
            RippleType = rippleType;

            _nearLight = new BitArray(Size * Size);
            RadiusChanged += OnRadiusChanged;

            // Stores max of 8 neighbors
            _neighbors = new List<Point>(8);
        }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的圆锥体内扩散的源。
        /// </summary>
        /// <param name="positionX">源在地图上位置的X值。</param>
        /// <param name="positionY">源在地图上位置的Y值。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算（或可隐式转换为<see cref="Distance" />的类型，如<see cref="Radius" />）。
        /// </param>
        /// <param name="angle">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体的最外中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以度为单位的角度，该角度指定由源值形成的圆锥体中所包含的全弧——
        /// <paramref name="angle" /> / 2度包含在圆锥体中心线的任一侧。
        /// </param>
        /// <param name="rippleType">要使用的波纹算法的变体。请参阅<see cref="Sources.RippleType"/>值的文档以了解每个变体的描述。</param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        public RippleSenseSource(int positionX, int positionY, double radius, Distance distanceCalc, double angle,
            double span, RippleType rippleType = RippleType.Regular, double intensity = 1)
            : base(positionX, positionY, radius, distanceCalc, angle, span, intensity)
        {
            RippleType = rippleType;
            _nearLight = new BitArray(Size * Size);
            RadiusChanged += OnRadiusChanged;

            // Stores max of 8 neighbors
            _neighbors = new List<Point>(8);
        }

        /// <inheritdoc/>
        public override void OnCalculate()
        {
            if (IsAngleRestricted)
            {
                var angle = AngleInternal * SadRogue.Primitives.MathHelpers.DegreePctOfCircle;
                var span = Span * SadRogue.Primitives.MathHelpers.DegreePctOfCircle;
                DoRippleFOV(RippleValue(RippleType), angle, span);
            }
            else
                DoRippleFOV(RippleValue(RippleType), 0, 0);
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            _nearLight.SetAll(false);
        }

        private void OnRadiusChanged(object? sender, EventArgs e)
        {
            _nearLight = new BitArray(Size * Size);
        }

        private void DoRippleFOV(int ripple, double angle, double span)
        {
            //Queue<Point> dq = new Queue<Point>();
            _dq.Enqueue(new Point(Center, Center)); // Add starting point
            while (_dq.Count != 0)
            {
                var p = _dq.Dequeue();

                if (ResultViewBacking[p.X, p.Y] <= 0 || _nearLight[p.ToIndex(Size)])
                    continue; // Nothing left to spread!

                for (int i = 0; i < AdjacencyRule.EightWay.DirectionsOfNeighborsCache.Length; i++)
                {
                    var dir = AdjacencyRule.EightWay.DirectionsOfNeighborsCache[i];

                    var x2 = p.X + dir.DeltaX;
                    var y2 = p.Y + dir.DeltaY;
                    var globalX2 = Position.X - (int)Radius + x2;
                    var globalY2 = Position.Y - (int)Radius + y2;

                    // Null-forgiving is fine; OnCalculate cannot be called with a null ResistanceView
                    if (globalX2 < 0 || globalX2 >= ResistanceView!.Width || globalY2 < 0 ||
                        globalY2 >= ResistanceView.Height || // Bounds check
                        DistanceCalc.Calculate(Center, Center, x2, y2) > Radius
                       ) // +1 covers starting tile at least
                        continue;

                    if (IsAngleRestricted)
                    {
                        var at2 = Math.Abs(angle - MathHelpers.ScaledAtan2Approx(y2 - Center, x2 - Center));
                        if (at2 > span * 0.5 && at2 < 1.0 - span * 0.5)
                            continue;
                    }
                    

                    var surroundingLight = NearRippleLight(x2, y2, globalX2, globalY2, ripple);
                    if (ResultViewBacking[x2, y2] < surroundingLight)
                    {
                        ResultViewBacking[x2, y2] = surroundingLight;
                        if (ResistanceView[globalX2, globalY2] < Intensity) // Not a wall (fully blocking)
                            _dq.Enqueue(new Point(x2,
                                y2)); // Need to redo neighbors, since we just changed this entry's light.
                    }
                }
            }
        }

        private double NearRippleLight(int x, int y, int globalX, int globalY, int rippleNeighbors)
        {
            if (x == Center && y == Center)
                return Intensity;

            for (int dirIdx = 0; dirIdx < AdjacencyRule.EightWay.DirectionsOfNeighborsCache.Length; dirIdx++)
            {
                var di = AdjacencyRule.EightWay.DirectionsOfNeighborsCache[dirIdx];

                var x2 = x + di.DeltaX;
                var y2 = y + di.DeltaY;

                // Out of bounds
                if (x2 < 0 || y2 < 0 || x2 >= ResultViewBacking.Width || y2 >= ResultViewBacking.Height)
                    continue;

                var globalX2 = Position.X - (int)Radius + x2;
                var globalY2 = Position.Y - (int)Radius + y2;

                // Null forgiving because this can only be called from OnCalculate, and ResistanceView cannot be null when that function
                // is called; adding a check would cost performance unnecessarily
                if (globalX2 >= 0 && globalX2 < ResistanceView!.Width && globalY2 >= 0 && globalY2 < ResistanceView.Height)
                {
                    var tmpDistance = DistanceCalc.Calculate(Center, Center, x2, y2);
                    int idx = 0;

                    // Find where to insert the new element
                    int count = _neighbors.Count;
                    for (; idx < count && idx < rippleNeighbors; idx++)
                    {
                        var c = _neighbors[idx];
                        var testDistance = DistanceCalc.Calculate(Center, Center, c.X, c.Y);
                        if (tmpDistance < testDistance)
                            break;
                    }
                    // No point in inserting it after this point, it'd never be counted anyway.  Otherwise, if we're kicking
                    // an existing element off the end, we'll just remove it to prevent shifting it down pointlessly
                    if (idx < rippleNeighbors)
                    {
                        if (count >= rippleNeighbors)
                            _neighbors.RemoveAt(rippleNeighbors - 1);
                        _neighbors.Insert(idx, new Point(x2, y2));
                    }
                }
            }

            if (_neighbors.Count == 0)
                return 0;

            int maxNeighborIdx = Math.Min(_neighbors.Count, rippleNeighbors);

            double curLight = 0;
            int lit = 0, indirects = 0;
            for (int neighborIdx = 0; neighborIdx < maxNeighborIdx; neighborIdx++)
            {
                var (pointX, pointY) = _neighbors[neighborIdx];

                var gpx = Position.X - (int)Radius + pointX;
                var gpy = Position.Y - (int)Radius + pointY;

                if (ResultViewBacking[pointX, pointY] > 0)
                {
                    lit++;
                    if (_nearLight[Point.ToIndex(pointX, pointY, Size)])
                        indirects++;

                    var dist = DistanceCalc.Calculate(x, y, pointX, pointY);
                    var resistance = ResistanceView![gpx, gpy];
                    if (gpx == Position.X && gpy == Position.Y)
                        resistance = 0.0;

                    curLight = Math.Max(curLight, ResultViewBacking[pointX, pointY] - dist * Decay - resistance);
                }
            }

            if (ResistanceView![globalX, globalY] >= Intensity || indirects >= lit)
                _nearLight[Point.ToIndex(x, y, Size)] = true;

            _neighbors.Clear();
            return curLight;
        }

        private static int RippleValue(RippleType type)
        {
            return type switch
            {
                RippleType.Regular => 2,
                RippleType.Loose => 3,
                RippleType.Tight => 1,
                RippleType.VeryLoose => 6,
                _ => RippleValue(RippleType.Regular)
            };
        }
    }
}
