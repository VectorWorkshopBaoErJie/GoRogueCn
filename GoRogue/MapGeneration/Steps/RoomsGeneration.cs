using System;
using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 在地图中雕刻出不会重叠的房间。生成的房间不会与自身或地图中的任何现有开放区域重叠。
    /// 
    /// <b>所需的上下文组件：</b> 无
    /// 
    /// <b>添加/使用的上下文组件：</b>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <term>Default Tag</term>
    ///         <term>Description</term>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ContextComponents.ItemList{Rectangle}">ItemList&lt;Rectangle&gt;</see>
    ///         </term>
    ///         <term>"Rooms"</term>
    ///         <term>A list of <see cref="Rectangle"/> instances which denote the rooms that were created.</term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}">ISettableGridView&lt;bool&gt;</see></term>
    ///         <term>"WallFloor"</term>
    ///         <term>A grid view of boolean values the size of the map where "true" indicates a tile is passable, and "false" indicates it is not.</term>
    ///     </item>
    /// </list>
    /// 
    /// 对于这两个组件，如果存在现有组件，则使用它们；如果不存在，则添加新组件。
    /// </summary>
    /// <remarks>
    /// 此生成步骤会生成房间，并将生成的房间添加到<see cref="GenerationContext"/>中具有给定标签的
    /// <see cref="ContextComponents.ItemList{Rectangle}">ItemList&lt;Rectangle&gt;</see>上下文组件中。
    /// 如果这样的组件不存在，则会创建一个新组件。它还会在具有给定标签的地图上下文的网格视图中将内部位置设置为true。
    /// 如果GenerationContext具有带有适当标签的现有网格视图上下文组件，则使用该组件。如果没有，则会创建一个
    /// <see cref="SadRogue.Primitives.GridViews.ArrayView{T}">ArrayView&lt;bool&gt;</see>并将其添加到地图上下文中，
    /// 其宽度/高度与<see cref="GenerationContext.Width"/>/<see cref="GenerationContext.Height"/>相匹配。
    /// </remarks>
    [PublicAPI]
    public class RoomsGeneration : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与用于存储此算法生成的房间的组件相关联。
        /// </summary>
        public readonly string? RoomsComponentTag;

        /// <summary>
        /// 可选的标签，必须与用于设置此算法更改的瓦片的墙/地面状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 在完全放弃生成某个房间之前，重新生成无法放置在有效位置的房间的最大次数。默认为10次。
        /// </summary>
        public int MaxCreationAttempts = 10;

        /// <summary>
        /// 在放弃并重新生成房间之前，尝试将房间放置在地图中而不与其他房间相交的最大次数。默认为10次。
        /// </summary>
        public int MaxPlacementAttempts = 10;

        /// <summary>
        /// 要生成的最大房间数量。默认为10。
        /// </summary>
        public int MaxRooms = 10;

        /// <summary>
        /// 要生成的最小房间数量。默认为4。
        /// </summary>
        public int MinRooms = 4;

        /// <summary>
        /// 用于房间创建/放置的随机数生成器。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 允许的房间最大尺寸。向上取整为奇数。默认为7。
        /// </summary>
        public int RoomMaxSize = 7;

        /// <summary>
        /// 允许的房间最小尺寸。向上取整为奇数。默认为3。
        /// </summary>
        public int RoomMinSize = 3;

        /// <summary>
        /// 房间宽度与基本生成尺寸的比率。默认为1.0。
        /// </summary>
        public float RoomSizeRatioX = 1f;

        /// <summary>
        /// 房间高度与基本生成尺寸的比率。默认为1.0。
        /// </summary>
        public float RoomSizeRatioY = 1f;


        /// <summary>
        /// 创建一个新的房间生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为 <see cref="RoomsGeneration" />。</param>
        /// <param name="roomsComponentTag">
        /// 可选的标签，必须与用于存储房间的组件相关联。默认为 "Rooms"。
        /// </param>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地面/墙壁状态的地图视图组件相关联。默认为 "WallFloor"。
        /// </param>
        public RoomsGeneration(string? name = null, string? roomsComponentTag = "Rooms",
                               string? wallFloorComponentTag = "WallFloor")
            : base(name)
        {
            RoomsComponentTag = roomsComponentTag;
            WallFloorComponentTag = wallFloorComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (MinRooms > MaxRooms)
                throw new InvalidConfigurationException(this, nameof(MinRooms),
                    $"The value must be less than or equal to the value of {nameof(MaxRooms)}.");

            if (RoomMinSize > RoomMaxSize)
                throw new InvalidConfigurationException(this, nameof(RoomMinSize),
                    $"The value must be less than or equal to the value of ${nameof(RoomMaxSize)}.");

            if (RoomSizeRatioX <= 0f)
                throw new InvalidConfigurationException(this, nameof(RoomSizeRatioX),
                    "The value must be greater than 0.");

            if (RoomSizeRatioY <= 0f)
                throw new InvalidConfigurationException(this, nameof(RoomSizeRatioY),
                    "The value must be greater than 0.");

            // Get or create/add a wall-floor context component
            var wallFloorContext = context.GetFirstOrNew<ISettableGridView<bool>>(
                () => new ArrayView<bool>(context.Width, context.Height),
                WallFloorComponentTag
            );

            // Determine how many rooms to generate
            var roomCounter = RNG.NextInt(MinRooms, MaxRooms + 1);

            // Get or create/add a rooms context component
            var roomsContext = context.GetFirstOrNew(
                () => new ItemList<Rectangle>(roomCounter),
                RoomsComponentTag
            );

            // Try to place all the rooms
            while (roomCounter != 0)
            {
                var tryCounterCreate = MaxCreationAttempts;
                var placed = false;

                // Attempt to create the room until either we reach max attempts or we create and place a room in a valid location
                while (tryCounterCreate != 0)
                {
                    var roomSize = RNG.NextInt(RoomMinSize, RoomMaxSize + 1);
                    var width =
                        (int)(roomSize * RoomSizeRatioX); // This helps with non square fonts. So rooms don't look odd
                    var height = (int)(roomSize * RoomSizeRatioY);

                    // When accounting for font ratios, these adjustments help prevent all rooms
                    // having the same looking square format
                    var adjustmentBase = roomSize / 4;

                    if (adjustmentBase != 0)
                    {
                        var adjustment = RNG.NextInt(-adjustmentBase, adjustmentBase + 1);
                        var adjustmentChance = RNG.NextInt(0, 2);

                        if (adjustmentChance == 0)
                            width += (int)(adjustment * RoomSizeRatioX);
                        else if (adjustmentChance == 1)
                            height += (int)(adjustment * RoomSizeRatioY);
                    }

                    width = Math.Max(RoomMinSize, width);
                    height = Math.Max(RoomMinSize, height);

                    // Keep room interior odd, helps with placement + tunnels around the outside.
                    if (width % 2 == 0)
                        width += 1;

                    if (height % 2 == 0)
                        height += 1;

                    var roomInnerRect = new Rectangle(0, 0, width, height);

                    var tryCounterPlace = MaxPlacementAttempts;

                    // Try to place the room we've created until either it doesn't intersect any other rooms, or we reach max retries (in which case, we will scrap the room entirely, create a new one, and try again)
                    while (tryCounterPlace != 0)
                    {
                        int xPos = 0, yPos = 0;

                        // Generate the rooms at odd positions, to make door/tunnel placement easier
                        while (xPos % 2 == 0)
                            xPos = RNG.NextInt(3, wallFloorContext.Width - roomInnerRect.Width - 3);
                        while (yPos % 2 == 0)
                            yPos = RNG.NextInt(3, wallFloorContext.Height - roomInnerRect.Height - 3);

                        // Record a rectangle for the inner and outer bounds of the room we've created
                        roomInnerRect = roomInnerRect.WithPosition(new Point(xPos, yPos));
                        var roomBounds = roomInnerRect.Expand(3, 3);

                        // Check if the room intersects with any floor tile on the map already.  We do it this way instead of checking against only the rooms list
                        // to ensure that if some other map generation step placed things before we did, we don't intersect those.
                        var intersected = false;
                        foreach (var point in roomBounds.Positions())
                            if (wallFloorContext[point])
                            {
                                intersected = true;
                                break;
                            }

                        // If we intersected floor tiles, try to place the room again
                        if (intersected)
                        {
                            tryCounterPlace--;
                            continue;
                        }

                        // Once we place it in a valid location, update the wall/floor context, and add the room to the list of rooms.
                        foreach (var point in roomInnerRect.Positions())
                            wallFloorContext[point] = true;

                        placed = true;
                        roomsContext.Add(roomInnerRect, Name);
                        break;
                    }

                    if (placed)
                    {
                        yield return null;
                        break;
                    }

                    tryCounterCreate--;
                }

                roomCounter--;
            }
        }
    }
}
