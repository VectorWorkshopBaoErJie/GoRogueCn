using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 矩形周长上任意数量的位置的任意列表。通常用于在某些地图生成步骤中表示一系列
    /// 门或房间的边缘。
    /// </summary>
    [PublicAPI]
    public class RectangleEdgePositionsList : IEnumerable<Point>
    {
        private readonly List<Point> _bottomPositions;

        private readonly List<Point> _leftPositions;

        private readonly HashSet<Point> _positions;

        private readonly List<Point> _rightPositions;
        private readonly List<Point> _topPositions;

        /// <summary>
        /// 存储了其边缘位置的矩形。
        /// </summary>
        public readonly Rectangle Rectangle;


        /// <summary>
        /// 为给定的矩形创建一个空的周长列表。
        /// </summary>
        /// <param name="rectangle">该结构为其存储周长位置的矩形。</param>
        public RectangleEdgePositionsList(Rectangle rectangle)
        {
            Rectangle = rectangle;
            _topPositions = new List<Point>();
            _rightPositions = new List<Point>();
            _bottomPositions = new List<Point>();
            _leftPositions = new List<Point>();
            _positions = new HashSet<Point>();
        }

        /// <summary>
        /// 矩形顶部边缘上的位置。
        /// </summary>
        public IReadOnlyList<Point> TopPositions => _topPositions.AsReadOnly();

        /// <summary>
        /// 矩形右侧墙上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> RightPositions => _rightPositions.AsReadOnly();

        /// <summary>
        /// 矩形底部墙上的门的位置。
        /// </summary>
        public IReadOnlyList<Point> BottomPositions => _bottomPositions.AsReadOnly();

        /// <summary>
        /// 矩形左侧墙上的门的位置。（注意：原注释中写成了“bottom wall”，这里已更正为“left wall”）
        /// </summary>
        public IReadOnlyList<Point> LeftPositions => _leftPositions.AsReadOnly();

        /// <summary>
        /// 正在存储的位置，没有重复的位置。
        /// </summary>
        public IEnumerable<Point> Positions => _positions;

        /// <summary>
        /// 检索给定边上的已存储位置的只读列表。指定的方向必须是基本方位。
        /// </summary>
        /// <param name="side">要获取已存储位置的边。</param>
        /// <returns>给定边上的已存储位置的只读列表。</returns>
        public IReadOnlyList<Point> this[Direction side]
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            => side.Type switch
            {
                Direction.Types.Up => TopPositions,
                Direction.Types.Right => RightPositions,
                Direction.Types.Down => BottomPositions,
                Direction.Types.Left => LeftPositions,
                _ => throw new ArgumentException("Side of a room must be a cardinal direction.", nameof(side))
            };

        /// <summary>
        /// 将给定的位置添加到相应的位置列表中。
        /// </summary>
        /// <param name="perimeterPosition">要添加的位置。</param>
        public void Add(Point perimeterPosition) => AddRange(perimeterPosition);

        /// <summary>
        /// 将给定的位置们添加到相应的位置列表中。
        /// </summary>
        /// <param name="perimeterPositions">要添加的位置们。</param>
        public void AddRange(params Point[] perimeterPositions)
            => AddRange((IEnumerable<Point>)perimeterPositions);

        /// <summary>
        /// 将给定的位置集合添加到相应的位置列表中。
        /// </summary>
        /// <param name="perimeterPositions">要添加的位置集合。</param>
        public void AddRange(IEnumerable<Point> perimeterPositions)
        {
            foreach (var pos in perimeterPositions)
            {
                bool top = Rectangle.IsOnSide(pos, Direction.Up);
                bool right = Rectangle.IsOnSide(pos, Direction.Right);
                bool down = Rectangle.IsOnSide(pos, Direction.Down);
                bool left = Rectangle.IsOnSide(pos, Direction.Left);

                // Not directly on perimeter of rectangle
                if (!(top || right || down || left))
                    throw new ArgumentException(
                        $"Positions added to a {nameof(RectangleEdgePositionsList)} must be on one of the edges of the rectangle.",
                        nameof(perimeterPositions));

                // Allowed but it won't record it multiple times
                if (_positions.Contains(pos))
                    continue;

                // Add to collection of positions and appropriate sub-lists
                _positions.Add(pos);

                if (top)
                    _topPositions.Add(pos);

                if (right)
                    _rightPositions.Add(pos);

                if (down)
                    _bottomPositions.Add(pos);

                if (left)
                    _leftPositions.Add(pos);
            }
        }

        /// <summary>
        /// 从数据结构中移除给定的位置。
        /// </summary>
        /// <param name="perimeterPosition">要移除的位置。</param>
        public void Remove(Point perimeterPosition) => RemoveRange(perimeterPosition);

        /// <summary>
        /// 从数据结构中移除给定的多个位置。
        /// </summary>
        /// <param name="perimeterPositions">要移除的多个位置。</param>
        public void RemoveRange(params Point[] perimeterPositions)
            => RemoveRange((IEnumerable<Point>)perimeterPositions);

        /// <summary>
        /// 从数据结构中移除给定的位置集合。
        /// </summary>
        /// <param name="perimeterPositions">要移除的位置集合。</param>
        public void RemoveRange(IEnumerable<Point> perimeterPositions)
        {
            foreach (var pos in perimeterPositions)
            {
                if (!_positions.Contains(pos))
                    throw new ArgumentException(
                        $"Tried to remove a position from a ${nameof(RectangleEdgePositionsList)} that was not present.");

                // Remove from collection of positions and appropriate sub-lists
                _positions.Remove(pos);

                if (Rectangle.IsOnSide(pos, Direction.Up))
                    _topPositions.Remove(pos);

                if (Rectangle.IsOnSide(pos, Direction.Right))
                    _rightPositions.Remove(pos);

                if (Rectangle.IsOnSide(pos, Direction.Down))
                    _bottomPositions.Remove(pos);

                if (Rectangle.IsOnSide(pos, Direction.Left))
                    _leftPositions.Remove(pos);
            }
        }

        /// <summary>
        /// 返回数据结构中是否包含给定的位置。
        /// </summary>
        /// <param name="position" />
        /// <returns>数据结构中是否包含指定的位置。</returns>
        public bool Contains(Point position) => _positions.Contains(position);

        /// <summary>
        /// 获取数据结构中所有位置的枚举器。
        /// </summary>
        /// <returns/>
        public IEnumerator<Point> GetEnumerator() => _positions.GetEnumerator();

        /// <summary>
        /// 获取数据结构中所有位置的枚举器。
        /// </summary>
        /// <returns/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
