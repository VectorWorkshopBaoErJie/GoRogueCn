using System.Collections.Generic;
using System.Linq;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 选择并打开矩形房间的墙壁，以将它们连接到相邻的开放空间（通常是迷宫/隧道）。
    /// 所需组件：
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ItemList{TItem}" />
    ///         </term>
    ///         <description>"Rooms"</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}" /> where T is bool</term>
    ///         <description>"WallFloor"</description>
    ///     </item>
    /// </list>
    /// Components Added/Used:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="DoorList" />
    ///         </term>
    ///         <description>"Doors"</description>
    ///     </item>
    /// </list>
    /// 对于DoorsList组件，如果存在合适的组件，则使用现有组件；否则，将添加一个新组件。
    /// </summary>
    /// <remarks>
    /// 此算法遍历<see cref="ItemList{Rectangle}" />上下文组件中指定的每个房间，并选择随机数量的侧面来放置连接（在指定参数范围内）。
    /// 对于每个侧面，它然后从该侧面的有效连接点中随机选择，并通过在“WallFloor”地图视图中将其值设置为true来开辟选定位置，
    /// 并将其添加到与<see cref="DoorList" />上下文组件中相应房间关联的门列表中。
    /// 它会继续在一侧选择连接点，直到<see cref="CancelConnectionPlacementChance" />成功。
    /// 每次选择一个点时，<see cref="CancelConnectionPlacementChance" />都会增加<see cref="CancelConnectionPlacementChanceIncrease" />。
    /// 该算法永远不会选择一侧的两个相邻点作为连接点。同样，它也永远不会打破地图的边缘。
    /// 如果地图上下文中存在具有正确标签的现有<see cref="DoorList" />组件，则该组件用于记录生成的门；否则，将创建一个新组件。
    /// </remarks>
    [PublicAPI]
    public class RoomDoorConnection : GenerationStep
    {
        /// <summary>
        /// Optional tag that must be associated with the component created/used to record the locations of doors created by this
        /// algorithm.
        /// </summary>
        public readonly string? DoorsListComponentTag;

        /// <summary>
        /// 可选标签，必须与包含此算法所连接房间的组件相关联。
        /// </summary>
        public readonly string? RoomsComponentTag;

        /// <summary>
        /// 可选标签，必须与用于设置此算法更改的瓦片的墙/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 在放置一扇门后（每侧），有1/100的概率取消在该侧继续放置门。默认为70。
        /// </summary>
        public ushort CancelConnectionPlacementChance = 70;

        /// <summary>
        /// 每次放置门时（每侧），按此数量增加<see cref="CancelConnectionPlacementChance" />的值。默认为10。
        /// </summary>
        public ushort CancelConnectionPlacementChanceIncrease = 10;

        /// <summary>
        /// 对于每个房间，有1/100的概率取消选择待处理的侧面。默认为50。
        /// </summary>
        public ushort CancelSideConnectionSelectChance = 50;

        /// <summary>
        /// 每个房间要处理的最大侧面数。默认为4。
        /// </summary>
        public ushort MaxSidesToConnect = 4;

        /// <summary>
        /// 每个房间要处理的最小侧面数。默认为1。
        /// </summary>
        public ushort MinSidesToConnect = 1;

        /// <summary>
        /// 用于连接的随机数生成器。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 创建一个新的迷宫生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="RoomDoorConnection" />。</param>
        /// <param name="roomsComponentTag">
        /// 可选标签，必须与包含此算法所连接房间的组件相关联。默认为"Rooms"。
        /// </param>
        /// <param name="wallFloorComponentTag">
        /// 可选标签，必须与用于设置此算法更改的瓦片的墙/地板状态的组件相关联。默认为"WallFloor"。
        /// </param>
        /// <param name="doorsListComponentTag">
        /// 可选标签，必须与创建/用于记录此算法创建的门的位置的组件相关联。默认为"Doors"。
        /// </param>
        public RoomDoorConnection(string? name = null, string? roomsComponentTag = "Rooms",
                                  string? wallFloorComponentTag = "WallFloor", string? doorsListComponentTag = "Doors")
            : base(name, (typeof(ItemList<Rectangle>), roomsComponentTag),
                (typeof(ISettableGridView<bool>), wallFloorComponentTag))
        {
            RoomsComponentTag = roomsComponentTag;
            WallFloorComponentTag = wallFloorComponentTag;
            DoorsListComponentTag = doorsListComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (MaxSidesToConnect > 4 || MaxSidesToConnect <= 0)
                throw new InvalidConfigurationException(this, nameof(MaxSidesToConnect),
                    "The value must be in range [1, 4].");

            if (MinSidesToConnect > MaxSidesToConnect)
                throw new InvalidConfigurationException(this, nameof(MinSidesToConnect),
                    $"The value must be less than or equal to {nameof(MaxSidesToConnect)}.");

            if (CancelSideConnectionSelectChance > 100)
                throw new InvalidConfigurationException(this, nameof(CancelSideConnectionSelectChance),
                    "The value must be a valid percent (between 0 and 100).");

            if (CancelConnectionPlacementChance > 100)
                throw new InvalidConfigurationException(this, nameof(CancelConnectionPlacementChance),
                    "The value must be a valid percent (between 0 and 100).");

            if (CancelConnectionPlacementChanceIncrease > 100)
                throw new InvalidConfigurationException(this, nameof(CancelConnectionPlacementChanceIncrease),
                    "The value must be a valid percent (between 0 and 100).");

            // Get required components; guaranteed to exist because enforced by required components list
            var rooms = context.GetFirst<ItemList<Rectangle>>(RoomsComponentTag);
            var wallFloor = context.GetFirst<ISettableGridView<bool>>(WallFloorComponentTag);

            // Get rectangle of inner map bounds (the entire map except for the outer box that must remain all walls
            var innerMap = wallFloor.Bounds().Expand(-1, -1);

            // Get/create doors list component.
            var doorsList = context.GetFirstOrNew(() => new DoorList(), DoorsListComponentTag);

            /*
			- Get all valid points along a side
			- if point count for side is > 0
			  - mark side for placement
			- if total sides marked > max
			  - loop total sides > max
				- randomly remove side
			- if total sides marked > min
			  - loop sides
				- CHECK side placement cancel check OK
				  - un-mark side
				- if total sides marked == min
				  -break loop
			- Loop sides
			  - Loop points
				- If point passes availability (no already chosen point next to point)
				  - CHECK point placement OK
					- Add point to list
			*/

            foreach (var room in rooms.Items)
            {
                // Holds positions that are valid options to carve out as doors to this room.
                // We're recording wall positions, and the room rectangle is only interior, so
                // we expand by one so we can store positions that are walls
                var validPositions = new RectangleEdgePositionsList(room.Expand(1, 1));

                foreach (var pos in room.Expand(1, 1).PerimeterPositions())
                    if (wallFloor[pos])
                        throw new RegenerateMapException("RoomDoorConnection was given rooms that already had doors.");

                // For each side, add any valid carving positions
                foreach (var side in AdjacencyRule.Cardinals.DirectionsOfNeighbors())
                    foreach (var sidePosition in room.PositionsOnSide(side))
                    {
                        var wallPoint = sidePosition + side; // Calculate point of wall next to the current position
                        var testPoint =
                            wallPoint + side; // Keep going in that direction to see where an opening here would lead

                        // If this opening hasn't been carved out already, wouldn't lead to the edge of the map, and WOULD lead to a walkable tile,
                        // then it's a valid location for us to choose to carve a door
                        if (!wallFloor[wallPoint] && innerMap.Contains(testPoint) && wallFloor[testPoint])
                            validPositions.Add(wallPoint);
                    }

                // Any side with at least one valid carving position is a valid side to select to start
                var validSides = AdjacencyRule.Cardinals.DirectionsOfNeighbors()
                    .Where(side => validPositions[side].Count > 0).ToList();

                // If the total sides we can select from is greater than the maximum amount of sides we are allowed to select per room,
                // then we must randomly remove sides until we are within the max parameter
                while (validSides.Count > MaxSidesToConnect)
                    validSides.RemoveAt(RNG.RandomIndex(validSides));

                // If there are some extra sides that we could remove and still stay within the minimum sides parameter,
                // then check the side cancellation chance and remove if needed.
                if (validSides.Count > MinSidesToConnect)
                {
                    var sidesRemoved = 0;
                    for (var i = 0; i < validSides.Count; i++)
                    {
                        if (RNG.PercentageCheck(CancelSideConnectionSelectChance))
                        {
                            // Since None couldn't be a valid side to begin with, we just use it as a marker for deletion to avoid modifying while iterating
                            validSides[i] = Direction.None;
                            sidesRemoved++;
                        }

                        // We can't remove any more sides without violating minimum parameter, so stop checking sides for cancellation
                        if (validSides.Count - sidesRemoved == MinSidesToConnect)
                            break;
                    }

                    validSides.RemoveAll(side => side == Direction.None);
                }

                foreach (var side in validSides)
                {
                    var currentCancelPlacementChance = CancelConnectionPlacementChance;
                    var selectedAPoint = false;
                    // While there are still points to connect
                    while (validPositions[side].Count > 0)
                    {
                        // Select a position from the list
                        var newConnectionPoint = RNG.RandomElement(validPositions[side]);
                        validPositions.Remove(newConnectionPoint);

                        // If point is by two valid walls, we'll carve it.  This might not be the case if we happened to select the point next to it
                        // previously
                        if (AdjacencyRule.Cardinals.Neighbors(newConnectionPoint)
                            .Count(pos => wallFloor.Contains(pos) && !wallFloor[pos]) >= 2)
                        {
                            doorsList.AddDoor(Name, room, newConnectionPoint);
                            wallFloor[newConnectionPoint] = true;
                            selectedAPoint = true;
                        }

                        // In either case, as long as we have at least one point selected on this side, we'll run the cancel chance to see if we're
                        // cancelling connection placement, and increase the chance of cancelling next iteration as needed
                        if (selectedAPoint)
                        {
                            if (RNG.PercentageCheck(currentCancelPlacementChance))
                                break;

                            currentCancelPlacementChance += CancelConnectionPlacementChanceIncrease;
                        }

                        yield return null;
                    }
                }
            }
        }
    }
}
