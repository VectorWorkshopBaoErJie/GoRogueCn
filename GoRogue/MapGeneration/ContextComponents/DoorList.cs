using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ContextComponents
{
    /// <summary>
    /// 一个由地图生成组件生成/添加的房间及其入口/出口点的列表，该列表跟踪哪个生成步骤创建/记录了哪个开口。
    /// </summary>
    [PublicAPI]
    public class DoorList
    {
        private readonly Dictionary<Rectangle, RoomDoors> _doorsPerRoom;

        /// <summary>
        /// 创建一个新的门管理器上下文组件。
        /// </summary>
        public DoorList()
        {
            _doorsPerRoom = new Dictionary<Rectangle, RoomDoors>();
        }

        /// <summary>
        /// 一个将房间与其门列表相关联的字典。
        /// </summary>
        public IReadOnlyDictionary<Rectangle, RoomDoors> DoorsPerRoom => _doorsPerRoom.AsReadOnly();

        /// <summary>
        /// 在给定房间的给定位置记录一个新的开口。
        /// </summary>
        /// <param name="generationStepName">记录门位置的生成步骤的名称。</param>
        /// <param name="room">门所在的房间。</param>
        /// <param name="doorPosition">要添加的门的位置。</param>
        public void AddDoor(string generationStepName, Rectangle room, Point doorPosition)
            => AddDoors(generationStepName, room, doorPosition);

        /// <summary>
        /// 在给定房间的给定位置记录新的开口。
        /// </summary>
        /// <param name="generationStepName">记录门位置的生成步骤的名称。</param>
        /// <param name="room">门所在的房间。</param>
        /// <param name="doorPositions">要添加的门的位置。</param>
        public void AddDoors(string generationStepName, Rectangle room, params Point[] doorPositions)
            => AddDoors(generationStepName, room, (IEnumerable<Point>)doorPositions);

        /// <summary>
        /// 在给定房间的给定位置记录新的开口。
        /// </summary>
        /// <param name="generationStepName">记录门位置的生成步骤的名称。</param>
        /// <param name="room">门所在的房间。</param>
        /// <param name="doorPositions">要添加的门的位置的集合。</param>
        public void AddDoors(string generationStepName, Rectangle room, IEnumerable<Point> doorPositions)
        {
            foreach (var door in doorPositions)
            {
                if (!_doorsPerRoom.ContainsKey(room))
                    _doorsPerRoom.Add(room, new RoomDoors(room));

                // Add door to list with the generation step that created it
                _doorsPerRoom[room].AddDoor(generationStepName, door);
            }
        }
    }
}
