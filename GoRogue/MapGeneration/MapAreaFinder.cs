using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 该类旨在计算和生成一个区域列表，代表地图上每个唯一相连的区域。
    /// </summary>
    /// <remarks>
    /// 该类接收一个 <see cref="SadRogue.Primitives.GridViews.IGridView{T}" />，其中给定位置的值为 true 表示它应该是地图区域的一部分，
    /// 而 false 表示它不应该是任何地图区域的一部分。在一个经典的 Roguelike 地牢示例中，这可能是一个“可行走性”视图，
    /// 其中地板返回 true 值，而墙壁返回 false 值。
    /// </remarks>
    [PublicAPI]
    public class MapAreaFinder
    {
        private bool[,]? _visited;
        private Direction[] _adjacentDirs = null!;

        private AdjacencyRule _adjacencyMethod;
        /// <summary>
        /// 用于确定网格连接性的方法。
        /// </summary>
        public AdjacencyRule AdjacencyMethod
        {
            get => _adjacencyMethod;
            set
            {
                _adjacencyMethod = value;
                _adjacentDirs = _adjacencyMethod.DirectionsOfNeighbors().ToArray();
            }
        }

        /// <summary>
        /// 用于所创建区域的点哈希算法。如果设置为null，将使用默认的点哈希算法。
        /// </summary>
        public IEqualityComparer<Point>? PointHasher;

        /// <summary>
        /// 网格视图，指示哪些单元格应被视为地图区域的一部分，哪些不应被视为地图区域的一部分。
        /// </summary>
        public IGridView<bool> AreasView;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="areasView">
        /// 网格视图，指示哪些单元格应被视为地图区域的一部分，哪些不应被视为地图区域的一部分。
        /// </param>
        /// <param name="adjacencyMethod">用于确定网格连接性的方法。</param>
        /// <param name="pointHasher">
        /// 用于所创建区域的点哈希算法。如果设置为null，将使用默认的点哈希算法。
        /// </param>
        public MapAreaFinder(IGridView<bool> areasView, AdjacencyRule adjacencyMethod, IEqualityComparer<Point>? pointHasher = null)
        {
            AreasView = areasView;
            _visited = null;
            AdjacencyMethod = adjacencyMethod;
            PointHasher = pointHasher;
        }

        /// <summary>
        /// 便利函数，用于创建一个MapAreaFinder实例，并返回该实例的<see cref="MapAreas" />函数的结果。
        /// 此函数适用于那些MapAreaFinder实例永远不会被重复使用的情况。
        /// </summary>
        /// <param name="map">
        /// 网格视图，指示哪些单元格应被视为地图区域的一部分，哪些不应被视为地图区域的一部分。
        /// </param>
        /// <param name="adjacencyMethod">用于确定网格连接性的方法。</param>
        /// <param name="pointHasher">
        /// 用于所创建区域的点哈希算法。如果设置为null，将使用默认的点哈希算法。
        /// </param>
        /// <returns>一个IEnumerable，包含每个（唯一的）地图区域。</returns>
        public static IEnumerable<Area> MapAreasFor(IGridView<bool> map, AdjacencyRule adjacencyMethod, IEqualityComparer<Point>? pointHasher = null)
        {
            var areaFinder = new MapAreaFinder(map, adjacencyMethod, pointHasher);
            return areaFinder.MapAreas();
        }

        /// <summary>
        /// 便利函数，用于创建一个MapAreaFinder实例，并返回该实例的<see cref="FillFrom(Point, bool)" />函数的结果。
        /// 此函数适用于MapAreaFinder实例永远不会被重复使用的情况。
        /// </summary>
        /// <param name="map">
        /// 网格视图，指示哪些单元格应被视为地图区域的一部分，哪些不应被视为地图区域的一部分。
        /// </param>
        /// <param name="adjacencyMethod">用于确定网格连接性的方法。</param>
        /// <param name="position">开始填充的起始位置。</param>
        /// <param name="pointHasher">
        /// 用于所创建区域的点哈希算法。如果设置为null，将使用默认的点哈希算法。
        /// </param>
        /// <returns>一个IEnumerable，包含每个（唯一的）地图区域。</returns>
        public static Area? FillFrom(IGridView<bool> map, AdjacencyRule adjacencyMethod, Point position,
                                     IEqualityComparer<Point>? pointHasher = null)
        {
            var areaFinder = new MapAreaFinder(map, adjacencyMethod, pointHasher);
            return areaFinder.FillFrom(position);
        }

        /// <summary>
        /// 计算地图区域列表，返回每个唯一的地图区域。
        /// </summary>
        /// <param name="clearVisited">
        /// 在查找区域之前，是否将所有单元格重置为未访问状态。已访问的位置不能包含在任何结果区域中。
        /// </param>
        /// <returns>一个IEnumerable，包含每个（唯一的）地图区域。</returns>
        public IEnumerable<Area> MapAreas(bool clearVisited = true)
        {
            CheckAndResetVisited(clearVisited);

            for (var x = 0; x < AreasView.Width; x++)
                for (var y = 0; y < AreasView.Height; y++)
                {
                    // Don't bother with a function call or any allocation, because the starting point isn't valid
                    // (either can't be in any area, or already in another one found).
                    var position = new Point(x, y);
                    if (AreasView[position] && !_visited![position.X, position.Y])
                        yield return Visit(new Point(x, y));
                }
        }

        /// <summary>
        /// 计算并返回一个区域，该区域代表与给定起始点相连的所有点。
        /// </summary>
        /// <param name="position">起始位置。</param>
        /// <param name="clearVisited">
        /// 在查找区域之前，是否将所有单元格重置为未访问状态。已访问的位置不能包含在结果区域中。
        /// </param>
        /// <returns>
        /// 一个区域，代表与给定起始点相连的所有点；如果从该点出发没有有效区域，则返回null。
        /// </returns>
        public Area? FillFrom(Point position, bool clearVisited = true)
        {
            if (!AreasView[position] || _visited != null && _visited[position.X, position.Y])
                return null;

            CheckAndResetVisited(clearVisited);

            return Visit(position);
        }

        /// <summary>
        /// 将所有位置重置为“未访问”。如果区域查找算法将重置标志设置为true，则会自动调用此方法。
        /// </summary>
        public void ResetVisitedPositions()
        {
            if (_visited == null || _visited.GetLength(1) != AreasView.Height || _visited.GetLength(0) != AreasView.Width)
                _visited = new bool[AreasView.Width, AreasView.Height];
            else
                Array.Clear(_visited, 0, _visited.Length);
        }

        private void CheckAndResetVisited(bool canClearVisited)
        {
            if (canClearVisited)
                ResetVisitedPositions();
            else if (_visited == null) // Allocate
                _visited = new bool[AreasView.Width, AreasView.Height];
            else if (_visited.GetLength(1) != AreasView.Height || _visited.GetLength(0) != AreasView.Width)
                throw new ArgumentException(
                    "Fill algorithm not set to clear visited, but the map view size has changed since it was allocated.", nameof(canClearVisited));
        }

        /// <summary>
        /// 访问指定的位置，并返回一个表示所有连接点的区域。
        /// </summary>
        /// <param name="position">访问的起始位置。</param>
        /// <returns>表示与起始位置相连的所有点的区域。</returns>
        /// <remarks>
        /// 此函数使用基于栈的方法执行连接点的深度优先搜索（DFS）。
        /// 它假定_visited数组已初始化且不为null，这是由调用此方法的公共函数所确保的。
        /// 如果点相邻（基于_adjacentDirs）且在AreasView的范围内，则认为它们是连接的。
        /// 已访问的点和不属于任何mapArea的点将被跳过。
        /// </remarks>
        private Area Visit(Point position)
        {
            // NOTE: This function can safely assume that _visited is NOT null, as this is enforced
            // by every public function that calls this one.

            var stack = new Stack<Point>();
            var area = new Area(PointHasher);
            stack.Push(position);

            while (stack.Count != 0)
            {
                position = stack.Pop();
                // Already visited, or not part of any mapArea (eg. visited since it was added via another path)
                if (_visited![position.X, position.Y] || !AreasView[position])
                    continue;

                area.Add(position);
                _visited[position.X, position.Y] = true;

                for (int i = 0; i < _adjacentDirs.Length; i++)
                {
                    var c = position + _adjacentDirs[i];

                    // Out of bounds, thus not actually a neighbor
                    if (c.X < 0 || c.Y < 0 || c.X >= AreasView.Width || c.Y >= AreasView.Height)
                        continue;

                    if (AreasView[c] && !_visited[c.X, c.Y])
                        stack.Push(c);
                }
            }

            return area;
        }
    }
}
