using System.Collections.Generic;
using System.Linq;
using GoRogue.MapGeneration.ConnectionPointSelectors;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.MapGeneration.TunnelCreators;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 通过将每个区域连接到其最近的相邻区域来连接地图上的区域，区域之间的距离基于指定的连接点选择器来确定。
    /// 所需的上下文组件：
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ItemList{Area}" />
    ///         </term>
    ///         <description>"Areas"</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}" /> where T is bool</term>
    ///         <description>"WallFloor"</description>
    ///     </item>
    /// </list>
    /// Context Components Added/Used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ItemList{Area}" />
    ///         </term>
    ///         <description>"Tunnels"</description>
    ///     </item>
    /// </list>
    /// 对于隧道组件，如果存在合适的组件，则使用现有组件；否则，将添加一个新组件。
    /// </summary>
    /// <remarks>
    /// 此生成步骤的输入是一个带有“Areas”标签（默认）的<see cref="ItemList{Area}" />上下文组件，其中包含要连接的区域，
    /// 以及一个“WallFloor”地图视图上下文组件，该组件指示地图上每个位置的墙壁/地板状态。
    /// 然后，它会连接列表中的地图区域，并在此过程中生成隧道。在“WallFloor”组件中，构成生成隧道的每个位置都设置为“true”。
    /// 此外，代表每个创建的隧道的<see cref="SadRogue.Primitives.Area" />会被添加到带有“Tunnels”标签（默认）的
    /// <see cref="ItemList{Area}" />上下文组件中。
    /// 如果为结果隧道存在具有指定标签的合适组件，则将这些区域添加到该组件中。否则，将创建一个新组件。
    /// 区域之间通过在每个区域与其最近的相邻区域之间绘制隧道来连接，基于给定<see cref="ConnectionPointSelector"/>选择的点之间的距离。
    /// 在每个区域中选择的实际连接点，以及在这些区域之间绘制隧道的方法，都可以通过<see cref="ConnectionPointSelector" />
    /// 和<see cref="TunnelCreator" />参数进行自定义。
    /// </remarks>
    [PublicAPI]
    public class ClosestMapAreaConnection : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与用于存储此算法连接的地图区域的组件相关联。
        /// </summary>
        public readonly string? AreasComponentTag;

        /// <summary>
        /// 可选的标签，必须与创建/用于存储此连接方法创建的隧道的组件相关联。
        /// </summary>
        public readonly string? TunnelsComponentTag;

        /// <summary>
        /// 可选的标签，必须与用于设置此算法更改的瓦片的墙壁/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 要使用的区域连接策略。并非所有方法都适用于具有凹形区域的地图
        /// -- 详见相应类文档以获取详细信息。
        /// </summary>
        public IConnectionPointSelector ConnectionPointSelector = new RandomConnectionPointSelector();

        /// <summary>
        /// 定义距离/邻居的距离计算方式。
        /// </summary>
        public Distance DistanceCalc = Distance.Manhattan;

        /// <summary>
        /// 要使用的隧道创建策略。默认为使用基本邻接规则的<see cref="DirectLineTunnelCreator" />。
        /// </summary>
        public ITunnelCreator TunnelCreator = new DirectLineTunnelCreator(Distance.Manhattan);

        private List<MultiArea>? _multiAreas;

        /// <summary>
        /// 创建一个新的最近区域连接步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="ClosestMapAreaConnection" />。</param>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地板/墙壁状态的地图视图组件相关联。默认为"WallFloor"。
        /// </param>
        /// <param name="areasComponentTag">
        /// 可选的标签，必须与用于存储此算法连接的地图区域的组件相关联。默认为"Areas"。
        /// </param>
        /// <param name="tunnelsComponentTag">
        /// 可选的标签，必须与创建/用于存储此连接方法创建的隧道的组件相关联。默认为"Tunnels"。
        /// </param>
        public ClosestMapAreaConnection(string? name = null, string? wallFloorComponentTag = "WallFloor",
                                        string? areasComponentTag = "Areas", string? tunnelsComponentTag = "Tunnels")
            : base(name, (typeof(ISettableGridView<bool>), wallFloorComponentTag),
                (typeof(ItemList<Area>), areasComponentTag))
        {
            WallFloorComponentTag = wallFloorComponentTag;
            AreasComponentTag = areasComponentTag;
            TunnelsComponentTag = tunnelsComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Get required components; guaranteed to exist because enforced by required components list
            var areasToConnect = context.GetFirst<ItemList<Area>>(AreasComponentTag);
            var wallFloor = context.GetFirst<ISettableGridView<bool>>(WallFloorComponentTag);

            // Get/create tunnel component
            var tunnels = context.GetFirstOrNew(() => new ItemList<Area>(), TunnelsComponentTag);

            // Create set of multi-areas for the areas we're joining, and a disjoint set based off of them
            _multiAreas = new List<MultiArea>(areasToConnect.Select(a => new MultiArea(a.Item)));
            var ds = new DisjointSet(_multiAreas.Count);
            ds.SetsJoined += DSOnSetsJoined;

            while (ds.Count > 1) // Haven't unioned all sets into one
                for (var i = 0; i < _multiAreas.Count; i++)
                {
                    // We finished early
                    if (ds.Count == 1) break;

                    // Make sure we operate on the parent set (since it contains the true points
                    int iParent = ds.Find(i);

                    // Find nearest area (area calculated based on point selector, and return selected connection points)
                    var (iClosest, area1Position, area2Position) = FindNearestMapArea(_multiAreas, DistanceCalc, ConnectionPointSelector, iParent, ds);

                    // Create a tunnel between the two points
                    var tunnel = TunnelCreator.CreateTunnel(wallFloor, area1Position, area2Position);
                    tunnels.Add(tunnel, Name);

                    // Mark the sets as unioned in the disjoint set
                    ds.MakeUnion(iParent, iClosest);

                    yield return null; // One stage per connection
                }
        }

        private void DSOnSetsJoined(object? sender, JoinedEventArgs e)
        {
            // 可以忽略可空性不匹配，因为事件处理程序仅在multiAreas初始化后在OnPerform中使用。
            _multiAreas![e.LargerSetID].AddRange(_multiAreas![e.SmallerSetID].SubAreas);
        }

        private static (int areaIndex, Point area1Position, Point area2Position) FindNearestMapArea(
            IReadOnlyList<IReadOnlyArea> mapAreas, Distance distanceCalc, IConnectionPointSelector pointSelector,
            int mapAreaIndex, DisjointSet ds)
        {
            // Record minimum distance and pair of points found based on selection
            int closestIndex = mapAreaIndex;
            double closestDistance = double.MaxValue;
            AreaConnectionPointPair closestPointPair = (Point.None, Point.None);

            for (var i = 0; i < mapAreas.Count; i++)
            {
                // Don't check against ourselves or anything in our set
                if (i == mapAreaIndex)
                    continue;

                if (ds.InSameSet(i, mapAreaIndex))
                    continue;

                // Ensure we operate on the parent of the neighbor (which cannot be our parent due to the checks
                // above), since the parents are the only areas that have the "true" list of points
                int iParentNeighbor = ds.Find(i);

                // Select connection points to check between the two areas
                var currentPointPair = pointSelector.SelectConnectionPoints(mapAreas[mapAreaIndex],
                    mapAreas[iParentNeighbor]);

                // Calculate distance between the selected connection points
                double distance = distanceCalc.Calculate(currentPointPair.Area1Position, currentPointPair.Area2Position);
                if (distance < closestDistance)
                {
                    closestIndex = iParentNeighbor;
                    closestDistance = distance;
                    closestPointPair = currentPointPair;
                }
            }

            // Return index, along with connection points found
            return (closestIndex, closestPointPair.Area1Position, closestPointPair.Area2Position);
        }
    }
}
