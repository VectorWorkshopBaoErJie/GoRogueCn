using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ContextComponents
{
    /// <summary>
    /// 房间墙壁上的开口列表，按它们所在的侧面进行分类。通常通过
    /// <see cref="DoorList" /> 创建。
    /// </summary>
    [PublicAPI]
    public class RoomDoors : IEnumerable<ItemStepPair<Point>>
    {
        private readonly RectangleEdgePositionsList _positionsList;
        private readonly Dictionary<Point, string> _doorToStepMapping;


        /// <summary>
        /// 为给定的房间创建一个新的门列表。
        /// </summary>
        /// <param name="room">要追踪其门的房间。</param>
        public RoomDoors(Rectangle room)
        {
            _positionsList = new RectangleEdgePositionsList(room.Expand(1, 1));
            _doorToStepMapping = new Dictionary<Point, string>();
        }

        /// <summary>
        /// 房间顶部墙壁上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> TopDoors => _positionsList.TopPositions;

        /// <summary>
        /// 房间右侧墙壁上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> RightDoors => _positionsList.RightPositions;

        /// <summary>
        /// 房间底部墙壁上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> BottomDoors => _positionsList.BottomPositions;

        /// <summary>
        /// 房间左侧墙壁上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> LeftDoors => _positionsList.LeftPositions;

        /// <summary>
        /// 正在追踪其门的房间。
        /// </summary>
        public Rectangle Room => _positionsList.Rectangle.Expand(-1, -1);

        /// <summary>
        /// 包含正在追踪其门的房间的外墙的矩形。
        /// </summary>
        public Rectangle RoomWithOuterWalls => _positionsList.Rectangle;

        /// <summary>
        /// 房间所有墙壁上的所有门的位置，没有重复的位置。
        /// </summary>
        public IEnumerable<Point> Doors => _positionsList.Positions;

        /// <summary>
        /// 一个将门与记录/创建它们的生成步骤相关联的字典。
        /// </summary>
        public IReadOnlyDictionary<Point, string> DoorToStepMapping => _doorToStepMapping.AsReadOnly();

        /// <summary>
        /// 检索给定侧面的只读门列表。指定的方向必须是基本方位（东、南、西、北）。
        /// </summary>
        /// <param name="side">要获取门的侧面。</param>
        /// <returns>给定侧面的只读门列表。</returns>
        public IReadOnlyList<Point> this[Direction side] => _positionsList[side];

        /// <summary>
        /// 将给定的位置添加到适当的门列表中。
        /// </summary>
        /// <param name="generationStepName">正在添加门的生成步骤的名称。</param>
        /// <param name="doorPosition">要添加的位置。</param>
        public void AddDoor(string generationStepName, Point doorPosition)
        {
            _positionsList.Add(doorPosition);
            _doorToStepMapping[doorPosition] = generationStepName;
        }

        /// <summary>
        /// 将给定的位置添加到适当的门列表中。
        /// </summary>
        /// <param name="generationStepName">正在添加门的生成步骤的名称。</param>
        /// <param name="doorPositions">要添加的位置。</param>
        public void AddDoors(string generationStepName, params Point[] doorPositions)
            => AddDoors(generationStepName, (IEnumerable<Point>)doorPositions);

        /// <summary>
        /// 将给定的位置集合添加到适当的门列表中。
        /// </summary>
        /// <param name="generationStepName">正在添加门的生成步骤的名称。</param>
        /// <param name="doorPositions">要添加的位置集合。</param>
        public void AddDoors(string generationStepName, IEnumerable<Point> doorPositions)
        {
            foreach (var pos in doorPositions)
            {
                _positionsList.Add(pos);
                _doorToStepMapping[pos] = generationStepName;
            }
        }

        /// <summary>
        /// 获取所有记录的门以及添加它们的步骤的枚举器。
        /// </summary>
        /// <returns/>
        public IEnumerator<ItemStepPair<Point>> GetEnumerator()
        {
            foreach (var door in Doors)
                yield return new ItemStepPair<Point>(door, _doorToStepMapping[door]);
        }

        /// <summary>
        /// 获取所有记录的门以及添加它们的步骤的非泛型枚举器。
        /// </summary>
        /// <returns/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
