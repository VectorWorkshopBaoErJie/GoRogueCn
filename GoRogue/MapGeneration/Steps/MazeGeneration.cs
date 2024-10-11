using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 在地图的墙壁区域中使用爬行器生成迷宫，这些爬行器会在地图上穿行并雕刻隧道。
    /// 所需的上下文组件：
    /// - 无
    /// 添加/使用的上下文组件：
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ContextComponents.ItemList{Area}" />
    ///         </term>
    ///         <description>"Tunnels"</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}" /> where T is bool</term>
    ///         <description>"WallFloor"</description>
    ///     </item>
    /// </list>
    /// 对于这两个组件，如果存在现有组件，则使用它们；如果不存在，则添加新组件。
    /// </summary>
    /// <remarks>
    /// 此生成步骤会生成迷宫，并将创建的隧道添加到带有适当标签（如果已指定）的<see cref="ContextComponents.ItemList{Area}" />上下文组件
    /// 中，该组件位于<see cref="GenerationContext" />上。如果不存在此类组件，则会创建一个。它还会在地图的"WallFloor"地图视图上下文组件中
    /// 将隧道内的所有位置设置为true。如果GenerationContext具有现有的"WallFloor"上下文组件，则使用该组件。如果没有，则会创建一个
    /// <see cref="SadRogue.Primitives.GridViews.ArrayView{T}" />（其中T为bool类型），并将其添加到地图上下文中，其宽度/高度与
    /// <see cref="GenerationContext.Width" />/<see cref="GenerationContext.Height" />相匹配。
    /// </remarks>
    [PublicAPI]
    public class MazeGeneration : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与用于存储此算法生成的隧道的组件相关联。
        /// </summary>
        public readonly string? TunnelsComponentTag;

        /// <summary>
        /// 可选的标签，必须与用于设置由此算法更改的瓦片的墙壁/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 在100中，每步增加多少爬虫改变方向的机会。一旦它改变方向，就会重置为0并按此数量增加。默认为10。
        /// </summary>
        public ushort CrawlerChangeDirectionImprovement = 10;

        /// <summary>
        /// 用于迷宫生成的随机数生成器。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 创建一个新的迷宫生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="MazeGeneration" />。</param>
        /// <param name="tunnelsComponentTag">
        /// 可选的标签，必须与用于存储算法创建的隧道/迷宫的组件相关联。默认为"Tunnels"。
        /// </param>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地板/墙壁状态的地图视图组件相关联。默认为"WallFloor"。
        /// </param>
        public MazeGeneration(string? name = null, string? tunnelsComponentTag = "Tunnels",
                              string? wallFloorComponentTag = "WallFloor")
            : base(name)
        {
            TunnelsComponentTag = tunnelsComponentTag;
            WallFloorComponentTag = wallFloorComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (CrawlerChangeDirectionImprovement > 100)
                throw new InvalidConfigurationException(this, nameof(CrawlerChangeDirectionImprovement),
                    "The value must be a valid percent (between 0 and 100).");

            // Logic implemented from http://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/

            // Get or create/add a wall-floor context component
            var wallFloorContext = context.GetFirstOrNew<ISettableGridView<bool>>(
                () => new ArrayView<bool>(context.Width, context.Height),
                WallFloorComponentTag
            );

            // Get or create/add a tunnel list context component
            var tunnelList = context.GetFirstOrNew(
                () => new ItemList<Area>(),
                TunnelsComponentTag
            );

            // Record spaces we've crawled to introduce changes.
            int spacesCrawled = 0;


            var crawlers = new List<Crawler>();
            var empty = FindEmptySquare(wallFloorContext, RNG);

            while (empty != Point.None)
            {
                var crawler = new Crawler();
                crawlers.Add(crawler);
                crawler.MoveTo(empty);
                var startedCrawler = true;
                ushort percentChangeDirection = 0;

                while (crawler.Path.Count != 0)
                {
                    // Dig this position
                    wallFloorContext[crawler.CurrentPosition] = true;

                    // Get valid directions (basically is any position outside the map or not?
                    var points = AdjacencyRule.Cardinals.NeighborsClockwise(crawler.CurrentPosition).ToArray();
                    var directions = AdjacencyRule.Cardinals.DirectionsOfNeighborsClockwise(Direction.None).ToList();

                    var validDirections = new bool[4];

                    // Rule out any valid directions based on their position. Only process cardinals, do not use diagonals
                    for (var i = 0; i < 4; i++)
                        validDirections[i] = IsPointWallsExceptSource(wallFloorContext, points[i], directions[i] + 4);

                    // If not a new crawler, exclude where we came from
                    if (!startedCrawler)
                        validDirections[directions.IndexOf(crawler.Facing + 4)] = false;

                    // Do we have any valid direction to go?
                    if (validDirections[0] || validDirections[1] || validDirections[2] || validDirections[3])
                    {
                        int index;

                        // Are we just starting this crawler? OR Is the current crawler facing
                        // direction invalid?
                        if (startedCrawler || validDirections[directions.IndexOf(crawler.Facing)] == false)
                        {
                            // Just get anything
                            index = GetDirectionIndex(validDirections, RNG);
                            crawler.Facing = directions[index];
                            percentChangeDirection = 0;
                            startedCrawler = false;
                        }
                        else
                        {
                            // Increase probability we change direction
                            percentChangeDirection += CrawlerChangeDirectionImprovement;

                            if (RNG.PercentageCheck(percentChangeDirection))
                            {
                                index = GetDirectionIndex(validDirections, RNG);
                                crawler.Facing = directions[index];
                                percentChangeDirection = 0;
                            }
                            else
                                index = directions.IndexOf(crawler.Facing);
                        }

                        crawler.MoveTo(points[index]);
                        spacesCrawled++;
                    }
                    else
                    {
                        crawler.Backtrack();
                        spacesCrawled++;
                    }

                    if (spacesCrawled >= 10)
                    {
                        yield return null;
                        spacesCrawled = 0;
                    }
                }

                if (spacesCrawled > 0)
                {
                    yield return null;
                    spacesCrawled = 0;
                }

                empty = FindEmptySquare(wallFloorContext, RNG);
            }

            // Add appropriate items to the tunnels list
            tunnelList.AddRange(crawlers.Select(c => c.AllPositions).Where(a => a.Count != 0), Name);
        }

        private static Point FindEmptySquare(IGridView<bool> map, IEnhancedRandom rng)
        {
            // Try random positions first
            // TODO: Port to retries option
            for (var i = 0; i < 100; i++)
            {
                var location = rng.RandomPosition(map, false);

                if (IsPointConsideredEmpty(map, location))
                    return location;
            }

            // Start looping through every single one
            for (var i = 0; i < map.Width * map.Height; i++)
            {
                var location = Point.FromIndex(i, map.Width);

                if (IsPointConsideredEmpty(map, location))
                    return location;
            }

            return Point.None;
        }

        private static int GetDirectionIndex(bool[] validDirections, IEnhancedRandom rng)
        {
            // 10 tries to find random ok valid
            var randomSuccess = false;
            var tempDirectionIndex = 0;

            for (var randomCounter = 0; randomCounter < 10; randomCounter++)
            {
                tempDirectionIndex = rng.NextInt(4);
                if (!validDirections[tempDirectionIndex]) continue;

                randomSuccess = true;
                break;
            }

            if (randomSuccess) return tempDirectionIndex;

            // Couldn't find an active valid, so just run through each one
            if (validDirections[0])
                tempDirectionIndex = 0;
            else if (validDirections[1])
                tempDirectionIndex = 1;
            else if (validDirections[2])
                tempDirectionIndex = 2;
            else
                tempDirectionIndex = 3;

            return tempDirectionIndex;
        }

        // TODO: Create random position function that has a fallback for if random fails after max retries
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointConsideredEmpty(IGridView<bool> map, Point location)
            => !IsPointMapEdge(map, location) && // exclude outer ridge of map
               location.X % 2 != 0 && location.Y % 2 != 0 && // check is odd number position
               IsPointSurroundedByWall(map, location) && // make sure is surrounded by a wall.
               !map[location]; // The location is a wall

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointMapEdge(IGridView<bool> map, Point location, bool onlyEdgeTest = false)
        {
            if (onlyEdgeTest)
                return location.X == 0 || location.X == map.Width - 1 || location.Y == 0 ||
                       location.Y == map.Height - 1;
            return location.X <= 0 || location.X >= map.Width - 1 || location.Y <= 0 || location.Y >= map.Height - 1;
        }

        private static bool IsPointSurroundedByWall(IGridView<bool> map, Point location)
        {
            var points = AdjacencyRule.EightWay.Neighbors(location);

            var mapBounds = map.Bounds();
            foreach (var point in points)
            {
                if (!mapBounds.Contains(point))
                    return false;

                if (map[point])
                    return false;
            }

            return true;
        }

        private static bool IsPointWallsExceptSource(IGridView<bool> map, Point location, Direction sourceDirection)
        {
            // exclude the outside of the map
            var mapInner = map.Bounds().Expand(-1, -1);

            if (!mapInner.Contains(location))
                // Shortcut out if this location is part of the map edge.
                return false;

            // Get map indexes for all surrounding locations
            var index = AdjacencyRule.EightWay.DirectionsOfNeighborsClockwise().ToArray();

            Direction[] skipped;

            if (sourceDirection == Direction.Right)
                skipped = new[] { sourceDirection, Direction.UpRight, Direction.DownRight };
            else if (sourceDirection == Direction.Left)
                skipped = new[] { sourceDirection, Direction.UpLeft, Direction.DownLeft };
            else if (sourceDirection == Direction.Up)
                skipped = new[] { sourceDirection, Direction.UpRight, Direction.UpLeft };
            else
                skipped = new[] { sourceDirection, Direction.DownRight, Direction.DownLeft };

            foreach (var direction in index)
            {
                if (skipped[0] == direction || skipped[1] == direction || skipped[2] == direction)
                    continue;

                if (!map.Bounds().Contains(location + direction) || map[location + direction])
                    return false;
            }

            return true;
        }

        private class Crawler
        {
            public readonly Area AllPositions = new Area();
            public readonly Stack<Point> Path = new Stack<Point>();
            public Point CurrentPosition = new Point(0, 0);
            public Direction Facing = Direction.Up;

            public void Backtrack()
            {
                if (Path.Count != 0)
                    CurrentPosition = Path.Pop();
            }

            public void MoveTo(Point position)
            {
                Path.Push(position);
                AllPositions.Add(position);
                CurrentPosition = position;
            }
        }
    }
}
