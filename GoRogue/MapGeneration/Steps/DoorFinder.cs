using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 查找构成门道的矩形房间中开放墙壁的位置。
    /// </summary>
    [PublicAPI]
    public class DoorFinder : GenerationStep
    {
        /// <summary>
        /// 必须与用于查找房间墙壁开口的网格视图相关联的可选标签。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 可选标签，必须与用于存储它所生成门位置的矩形房间的组件相关联。
        /// </summary>
        public readonly string? RoomsComponentTag;

        /// <summary>
        /// 可选标签，必须与创建/用于记录此算法找到的门位置的组件相关联。
        /// </summary>
        public readonly string? DoorsListComponentTag;

        /// <summary>
        /// 创建一个寻门生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为 <see cref="DoorFinder"/></param>
        /// <param name="wallFloorComponentTag">
        /// 可选标签，必须与用于查找房间墙壁是否开放的网格视图相关联。
        /// 默认为 "WallFloor"。
        /// </param>
        /// <param name="roomsComponentTag">
        /// 可选标签，必须与用于存储此算法为其查找开口的矩形房间的组件相关联。
        /// 默认为 "Rooms"。
        /// </param>
        /// <param name="doorsListComponentTag">
        /// 可选标签，必须与创建/用于记录此算法找到的门位置的组件相关联。
        /// 默认为 "Doors"。
        /// </param>
        public DoorFinder(string? name = null, string? wallFloorComponentTag = "WallFloor",
                          string? roomsComponentTag = "Rooms", string? doorsListComponentTag = "Doors")
            : base(name,
                (typeof(IGridView<bool>), wallFloorComponentTag),
                                      (typeof(ItemList<Rectangle>), roomsComponentTag))
        {
            WallFloorComponentTag = wallFloorComponentTag;
            RoomsComponentTag = roomsComponentTag;
            DoorsListComponentTag = doorsListComponentTag;
        }

        /// <inheritdoc/>
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Get required components; guaranteed to exist because enforced by required components list
            var wallFloor = context.GetFirst<IGridView<bool>>(WallFloorComponentTag);
            var roomsList = context.GetFirst<ItemList<Rectangle>>(RoomsComponentTag);

            // Get/create doors component
            var doorsList = context.GetFirstOrNew(() => new DoorList(), DoorsListComponentTag);

            // Go through each room and add door locations for it
            foreach (var room in roomsList.Items)
            {
                foreach (var perimeterPos in room.Expand(1, 1).PerimeterPositions())
                    if (wallFloor[perimeterPos])
                        doorsList.AddDoor(Name, room, perimeterPos);

                yield return null;
            }
        }
    }
}
