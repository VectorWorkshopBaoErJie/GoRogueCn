using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.PointHashers;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 一个具有任意边和角数量的区域
    /// </summary>
    [PublicAPI]
    public class PolygonArea : IReadOnlyArea, IMatchable<PolygonArea>
    {
        #region Properties/Fields
        /// <summary>
        /// 此多边形的顶点
        /// </summary>
        public IReadOnlyList<Point> Corners => _corners.AsReadOnly();
        private readonly List<Point> _corners;

        /// <summary>
        /// 多边形的外部点
        /// </summary>
        public IReadOnlyMultiArea OuterPoints => _outerPoints;
        private readonly MultiArea _outerPoints;

        /// <summary>
        /// 多边形的内部点
        /// </summary>
        public IReadOnlyArea InnerPoints => _innerPoints;
        private readonly Area _innerPoints;

        private readonly MultiArea _points;

        /// <summary>
        /// 使用哪种线条绘制算法
        /// </summary>
        public readonly Lines.Algorithm LineAlgorithm;

        /// <inheritdoc/>
        public Rectangle Bounds => _points.Bounds;

        /// <inheritdoc/>
        public int Count => _points.Count;

        /// <inheritdoc/>
        public bool UseIndexEnumeration => _points.UseIndexEnumeration;

        /// <inheritdoc/>
        public Point this[int index] => _points[index];

        /// <summary>
        /// 多边形的最左侧X值
        /// </summary>
        public int Left => Bounds.MinExtentX;

        /// <summary>
        /// 多边形的最右侧X值
        /// </summary>
        public int Right => Bounds.MaxExtentX;

        /// <summary>
        /// 多边形的最顶部Y值
        /// </summary>
        public int Top => Direction.YIncreasesUpward ? Bounds.MaxExtentY : Bounds.MinExtentY;

        /// <summary>
        ///多边形的最底部Y值
        /// </summary>
        public int Bottom => Direction.YIncreasesUpward ? Bounds.MinExtentY : Bounds.MaxExtentY;

        /// <summary>
        /// 这个多边形有多宽
        /// </summary>
        public int Width => Bounds.Width;

        /// <summary>
        /// 这个多边形有多高
        /// </summary>
        public int Height => Bounds.Height;

        /// <summary>
        /// 这个多边形的中心点
        /// </summary>
        /// <remarks>不保证中心点位于多边形内部</remarks>
        public Point Center => Bounds.Center;

        /// <summary>
        /// 如果提供的位置是这个多边形的一个角，则返回true
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>布尔值</returns>
        public bool IsCorner(Point position) => Corners.Contains(position);

        /// <summary>
        /// 返回一个描述区域角点位置的字符串。
        /// </summary>
        public override string ToString()
        {
            var answer = new StringBuilder("PolygonArea: ");
            foreach (var corner in _corners)
                answer.Append($"{corner} => ");
            return answer.ToString();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 使用提供的点作为角点创建一个新的多边形
        /// </summary>
        /// <param name="corners">多边形的每个角点，这些点将被复制到一个新列表中</param>
        /// <param name="algorithm">使用哪种线算法</param>
        /// <exception cref="ArgumentException">必须有3个或更多角点；算法必须产生有序的线段。</exception>
        public PolygonArea(IEnumerable<Point> corners, Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
            : this(corners.ToList(), algorithm) { }

        /// <summary>
        /// 使用提供的点作为角点创建一个新的多边形
        /// </summary>
        /// <param name="corners">这个多边形的角点</param>
        /// <param name="algorithm">使用哪种线算法</param>
        /// <exception cref="ArgumentException">必须有3个或更多角点；算法必须产生有序的线段。</exception>
        public PolygonArea(ref List<Point> corners, Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
            : this(corners, algorithm) { }

        /// <summary>
        /// 返回一个新的多边形区域，其角点位于提供的点上。
        /// </summary>
        /// <param name="algorithm">使用哪种画线算法</param>
        /// <param name="corners">这个多边形的角点</param>
        /// <exception cref="ArgumentException">必须有3个或更多角点；算法必须产生有序的线段。</exception>
        public PolygonArea(Lines.Algorithm algorithm, params Point[] corners)
            : this(corners, algorithm) { }

        /// <summary>
        /// 使用DDA算法生成线段，并返回一个新多边形，其角点位于提供的点上
        /// </summary>
        /// <param name="corners">多边形的角点</param>
        /// <exception cref="ArgumentException">必须有3个或更多角点；算法必须产生有序的线段。</exception>
        public PolygonArea(params Point[] corners) : this(corners, Lines.Algorithm.Bresenham) { }

        private PolygonArea(List<Point> corners, Lines.Algorithm algorithm)
        {
            _corners = corners;
            LineAlgorithm = algorithm;
            CheckCorners();
            _outerPoints = new MultiArea();

            // Draw corners
            DrawFromCorners();
            // Create proper inner points area
            var (minExtent, maxExtent) = _outerPoints.Bounds;
            _innerPoints = new Area(new KnownRangeHasher(minExtent, maxExtent));
            // Determine inner points based on outer points and bounds
            SetInnerPoints();

            _points = new MultiArea { _outerPoints, _innerPoints };
        }
        #endregion

        #region Private Initialization Functions

        private void CheckCorners() => CheckCorners(_corners.Count);
        private static void CheckCorners(int corners)
        {
            if (corners < 3)
                throw new ArgumentException("Polygons must have 3 or more sides to be representable in 2 dimensions");
        }

        // 从每个角点绘制到下一个角点的线段
        private void DrawFromCorners()
        {
            // TODO: 由于在创建之前已知每个边界的范围，因此更改这些区域使用的哈希算法可能也很有用；
            // 然而，计算本身确实需要一些时间；需要进行测试。
            // 无论如何，在创建过程中实现的大部分性能提升都是通过SetInnerPoints中缓存外部点来实现的。
            for (int i = 0; i < _corners.Count - 1; i++)
                _outerPoints.Add(new Area(Lines.GetLine(_corners[i], _corners[i + 1], LineAlgorithm)));

            _outerPoints.Add(new Area(Lines.GetLine(_corners[^1], _corners[0], LineAlgorithm)));
        }

        // 使用奇偶规则来确定我们是否在区域内，并相应地填充InnerPoints
        private void SetInnerPoints()
        {
            // 计算边界并缓存外部点，以便我们可以高效地检查任意点是否为外部点。
            var bounds = _outerPoints.Bounds;
            var outerPointsSet =
                new HashSet<Point>(_outerPoints, new KnownRangeHasher(bounds.MinExtent, bounds.MaxExtent));

            // 顶部和底部行永远不可能包含内部点，所以跳过它们。
            for (int y = bounds.MinExtentY + 1; y < bounds.MaxExtentY; y++)
            {
                var lineIndicesEncountered = new HashSet<int>();

                // 必须包含MinExtentX，以便能够准确计算遇到的线段数量。
                // 不需要MaxExtentX，因为没有内部点的值可以等于或大于它。
                for (int x = bounds.MinExtentX; x < bounds.MaxExtentX; x++)
                {
                    var curPoint = new Point(x, y);

                    // 如果我们找到一个外部点，我们必须将其计为在此y线上可见
                    if (outerPointsSet.Contains(curPoint))
                    {
                        // 添加包含我们找到的所有边界线段。每个点可能是1或2个边界线段的一部分（角点在2个中）。
                        // 然而，请注意，仅仅检查当前点是否为角点并不足以知道它是仅属于1个还是2个线段，
                        // 因为如果两个边界之间的角度极小（由于在线性网格上表示线段的不精确性），非角点也可能属于2个线段。
                        for (int i = 0; i < _outerPoints.SubAreas.Count; i++)
                        {
                            var boundary = _outerPoints.SubAreas[i];
                            if (!boundary.Contains(curPoint)) continue;

                            // 当且仅当线段包含一个y值小于当前点y值的点时，我们才必须计算遇到的线段。
                            // 根据线段的定义，这样的点必须出现在两个端点之一。
                            if (boundary[0].Y < y || boundary[^1].Y < y)
                                lineIndicesEncountered.Add(i);
                        }
                    }
                    // 否则，一个点是内部点，当且仅当扫描线在到达当前点的过程中穿过了奇数个外部边界
                    else
                    {
                        if (lineIndicesEncountered.Count % 2 == 1)
                            _innerPoints.Add(curPoint);
                    }
                }
            }
        }
        #endregion

        #region Static Creation Methods
        /// <summary>
        /// 根据GoRogue.Rectangle创建一个新的多边形。
        /// </summary>
        /// <param name="rectangle">矩形</param>
        /// <param name="algorithm">用于查找边界的线段绘制算法。</param>
        /// <exception cref="ArgumentException">必须有3个或更多角；算法必须产生有序的线段。</exception>
        /// <returns>一个矩形形状的新多边形</returns>
        public static PolygonArea Rectangle(Rectangle rectangle, Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
            => new PolygonArea(algorithm, rectangle.MinExtent, (rectangle.MaxExtentX, rectangle.MinExtentY), rectangle.MaxExtent,
                (rectangle.MinExtentX, rectangle.MaxExtentY));

        /// <summary>
        /// 创建一个平行四边形形状的新多边形。
        /// </summary>
        /// <param name="origin">平行四边形的起点。</param>
        /// <param name="width">平行四边形的宽度。</param>
        /// <param name="height">平行四边形的高度。</param>
        /// <param name="fromTop">平行四边形是从起点开始向下-右延伸还是向上-右延伸</param>
        /// <param name="algorithm">用于查找边界的线段绘制算法。</param>
        /// <exception cref="ArgumentException">必须有3个或更多角；算法必须产生有序的线段。</exception>
        /// <returns>一个平行四边形形状的新多边形</returns>
        public static PolygonArea Parallelogram(Point origin, int width, int height, bool fromTop = false,
                                                Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
        {
            if (fromTop && Direction.YIncreasesUpward)
                height *= -1;

            else if (!fromTop && !Direction.YIncreasesUpward)
                height *= -1;


            Point p1 = origin;
            Point p2 = origin + new Point(width, 0);
            Point p3 = origin + new Point(width + Math.Abs(height), height);
            Point p4 = origin + new Point(Math.Abs(height), height);

            return new PolygonArea(algorithm, p1, p2, p3, p4);
        }

        /// <summary>
        /// 创建一个各边等长的多边形
        /// </summary>
        /// <param name="center">这个多边形的中心点</param>
        /// <param name="numberOfSides">这个多边形上的边和角的数量</param>
        /// <param name="radius">中心点到每个角的期望距离</param>
        /// <exception cref="ArgumentException">必须有3个或更多的角；算法必须产生有序的线段。</exception>
        /// <param name="algorithm">使用哪种线段绘制算法</param>
        /// <returns></returns>
        public static PolygonArea RegularPolygon(Point center, int numberOfSides, double radius, Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
        {
            CheckCorners(numberOfSides);

            var corners = new List<Point>(numberOfSides);
            var increment = 360.0 / numberOfSides;

            for (int i = 0; i < numberOfSides; i++)
            {
                var theta = SadRogue.Primitives.MathHelpers.ToRadian(i * increment);
                var corner = new PolarCoordinate(radius, theta).ToCartesian();
                corner += center;
                corners.Add(corner);
            }

            return new PolygonArea(ref corners, algorithm);
        }

        /// <summary>
        /// 创建一个新的星形多边形
        /// </summary>
        /// <param name="center">星形的中心点</param>
        /// <param name="points">这个星形有多少个顶点</param>
        /// <param name="outerRadius">中心点到星形尖端的距离</param>
        /// <param name="innerRadius">中心点到星形凹点（腋窝处）的距离</param>
        /// <param name="algorithm">使用哪种线段绘制算法</param>
        /// <exception cref="ArgumentException">星形必须拥有3个或更多的顶点；算法必须是有序的；内半径和外半径必须是正数</exception>
        public static PolygonArea RegularStar(Point center, int points, double outerRadius, double innerRadius,
                                              Lines.Algorithm algorithm = Lines.Algorithm.Bresenham)
        {
            CheckCorners(points);

            if (outerRadius < 0)
                throw new ArgumentException("outerRadius must be positive.");
            if (innerRadius < 0)
                throw new ArgumentException("innerRadius must be positive.");

            points *= 2;
            var corners = new List<Point>(points);
            var increment = 360.0 / points;

            for (int i = 0; i < points; i++)
            {
                var radius = i % 2 == 0 ? outerRadius : innerRadius;
                var theta = SadRogue.Primitives.MathHelpers.ToRadian(i * increment);
                var corner = new PolarCoordinate(radius, theta).ToCartesian();
                corner += center;
                corners.Add(corner);
            }

            return new PolygonArea(ref corners, algorithm);
        }
        #endregion

        #region IReadOnlyArea Implementation

        /// <inheritdoc/>
        public bool Matches(IReadOnlyArea? other)
        {
            if (other is PolygonArea p) return Matches(p);
            return _points.Matches(other);
        }

        /// <inheritdoc/>
        public IEnumerator<Point> GetEnumerator() => _points.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _points.GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(IReadOnlyArea area) => _points.Contains(area);

        /// <inheritdoc/>
        public bool Contains(Point position) => _points.Contains(position);

        /// <inheritdoc/>
        public bool Contains(int positionX, int positionY) => _points.Contains(positionX, positionY);

        /// <inheritdoc/>
        public bool Intersects(IReadOnlyArea area) => _points.Intersects(area);
        #endregion

        #region IMatchable Implementation

        /// <summary>
        /// 比较多边形以确保它们由相同的角定义，从而表示相同的区域。
        /// </summary>
        /// <param name="other">要比较的另一个多边形</param>
        /// <returns>如果多边形表示相同的区域，则为True；否则为false。</returns>
        public bool Matches(PolygonArea? other)
        {
            if (other is null) return false;
            if (other.Corners.Count != Corners.Count) return false;

            // 寻找起始点。注意：
            //    - 每个角点在Corners列表中只能出现一次，以符合闭合多边形的定义
            //    - 如果两个多边形包含完全相同且顺序一致的角点，则它们是等价的，
            //      但是起始点是独立的
            //        - [(0, 1), (5, 0), (1, 2)] == [(5, 0), (1, 2), (0, 1)]
            //        - [(0, 1), (5, 0), (1, 2)] != [(5, 0), (0, 1), (1, 2)]
            Point start = Corners[0];
            int size = Corners.Count;
            int otherStartIdx = -1;
            for (int i = 0; i < size; i++)
                if (other.Corners[i].Matches(start))
                {
                    otherStartIdx = i;
                    break;
                }

            if (otherStartIdx == -1) return false;

            // 从起始点开始比较以确保顺序有效。thisIdx的起始点不需要取模，
            // 因为要构成一个有效的多边形，大小必须至少为3。同样，我们知道两个角点列表的长度是相同的，
            // 所以我们可以简单地将thisIdx增加1，并保证它不会超出索引范围。
            int thisIdx = 1;
            for (int otherIdx = (otherStartIdx + 1) % size; otherIdx != otherStartIdx; otherIdx = (otherIdx + 1) % size)
            {
                if (!other.Corners[otherIdx].Matches(Corners[thisIdx])) return false;
                thisIdx += 1;
            }

            return true;
        }
        #endregion

        #region Transformation

        /// <summary>
        /// 将多边形向指定方向移动。
        /// </summary>
        /// <param name="dx">要沿X轴移动的值</param>
        /// <param name="dy">要沿Y轴移动的值</param>
        /// <returns></returns>
        public PolygonArea Translate(int dx, int dy)
            => Translate(new Point(dx, dy));

        /// <summary>
        /// 将多边形向指定方向移动。
        /// </summary>
        /// <param name="delta">要沿X轴和Y轴移动的量。</param>
        /// <returns>一个新的、已移动的多边形区域</returns>
        public PolygonArea Translate(Point delta)
        {
            var corners = new List<Point>(_corners.Count);
            for (int i = 0; i < Corners.Count; i++)
            {
                corners.Add(Corners[i] + delta);
            }

            return new PolygonArea(ref corners);
        }

        /// <summary>
        /// 围绕中心旋转多边形。
        /// </summary>
        /// <param name="degrees">旋转的角度</param>
        /// <returns>一个新的、已旋转的多边形区域</returns>
        public PolygonArea Rotate(double degrees) => Rotate(degrees, Center);

        /// <summary>
        /// 围绕原点旋转多边形
        /// </summary>
        /// <param name="degrees">旋转的角度</param>
        /// <param name="origin">围绕其旋转的点</param>
        /// <returns>一个新的、已旋转的多边形区域</returns>
        public PolygonArea Rotate(double degrees, Point origin)
        {
            degrees = MathHelpers.WrapAround(degrees, 360);

            var corners = new List<Point>(_corners.Count);
            for (int i = 0; i < Corners.Count; i++)
            {
                corners.Add(Corners[i].Rotate(degrees, origin));
            }

            return new PolygonArea(ref corners);
        }

        /// <summary>
        /// 围绕X轴水平翻转
        /// </summary>
        /// <param name="x">围绕哪个值进行翻转。</param>
        /// <returns>一个新的、已翻转的多边形区域</returns>
        public PolygonArea FlipHorizontal(int x)
        {
            var corners = new List<Point>(_corners.Count);
            for (int i = 0; i < Corners.Count; i++)
            {
                corners.Add((Corners[i] - (x, 0)) * (-1, 1) + (x, 0));
            }

            return new PolygonArea(ref corners);
        }

        /// <summary>
        /// 围绕Y轴垂直翻转
        /// </summary>
        /// <param name="y">围绕哪个值进行翻转。</param>
        public PolygonArea FlipVertical(int y)
        {
            var corners = new List<Point>(_corners.Count);
            for (int i = 0; i < Corners.Count; i++)
            {
                corners.Add((Corners[i] - (0, y)) * (1, -1) + (0, y));
            }

            return new PolygonArea(ref corners);
        }

        /// <summary>
        /// 相对于一条对角线，交换多边形的X和Y值
        /// </summary>
        /// <param name="x">与用于转置的对角线相交的任意点的X值</param>
        /// <param name="y">与用于转置的对角线相交的任意点的Y值</param>
        /// <returns>一个新的多边形区域</returns>
        public PolygonArea Transpose(int x, int y)
            => Transpose((x, y));

        /// <summary>
        /// 相对于一条对角线，交换多边形的X和Y值
        /// </summary>
        /// <param name="xy">与用于转置的对角线相交的任意点</param>
        /// <returns>一个新的多边形区域</returns>
        public PolygonArea Transpose(Point xy)
        {
            var corners = new List<Point>(_corners.Count);
            for (int i = 0; i < Corners.Count; i++)
            {
                var corner = Corners[i];
                corner -= xy;
                corner = (corner.Y, corner.X);
                corner += xy;
                corners.Add(corner);
            }

            return new PolygonArea(ref corners);
        }
        #endregion
    }
}
