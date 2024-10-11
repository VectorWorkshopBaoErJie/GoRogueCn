using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 一个实现了<see cref="IReadOnlyMultiArea"/>接口的类，该类从多个“子区域”中派生其区域。
    /// </summary>
    [PublicAPI]
    public class MultiArea : IReadOnlyMultiArea
    {
        private readonly List<IReadOnlyArea> _subAreas;

        /// <inheritdoc/>
        public IReadOnlyList<IReadOnlyArea> SubAreas => _subAreas.AsReadOnly();

        // TODO: 修改为在Rectangle的ExpandToFit函数中
        /// <summary>
        /// 包含每个子区域中每个位置的最小可能矩形。
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                if (_subAreas.Count == 0) return Rectangle.Empty;

                var firstBounds = _subAreas[0].Bounds;
                int minX = firstBounds.MinExtentX;
                int minY = firstBounds.MinExtentY;
                int maxX = firstBounds.MaxExtentX;
                int maxY = firstBounds.MaxExtentY;

                for (int i = 1; i < _subAreas.Count; i++)
                {
                    var currentBounds = _subAreas[i].Bounds;
                    if (minX > currentBounds.MinExtentX) minX = currentBounds.MinExtentX;
                    if (minY > currentBounds.MinExtentY) minY = currentBounds.MinExtentY;
                    if (maxX < currentBounds.MaxExtentX) maxX = currentBounds.MaxExtentX;
                    if (maxY < currentBounds.MaxExtentY) maxY = currentBounds.MaxExtentY;
                }

                return new Rectangle(new Point(minX, minY), new Point(maxX, maxY));
            }
        }

        /// <summary>
        /// 此区域所有子区域中位置的总数。
        /// </summary>
        public int Count => _subAreas.Sum(area => area.Count);

        /// <inheritdoc/>
        public bool UseIndexEnumeration => false;

        /// <summary>
        /// 以类似于列表的方式从区域（通过其子区域）返回位置。
        /// </summary>
        /// <remarks>
        /// 索引方案将索引0视为<see cref="SubAreas"/>中第一个子区域的索引0。
        /// 索引在该子区域的所有点中以递增顺序进行，然后滚动到下一个子区域。
        /// 例如，索引[SubAreas[0].Count]实际上是第二个子区域（即SubAreas[1][0]）的索引0。
        /// </remarks>
        /// <param name="index">要检索的位置的索引。</param>
        public Point this[int index]
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _subAreas.Count; i++)
                {
                    var area = _subAreas[i];
                    if (sum + area.Count > index)
                        return area[index - sum];

                    sum += area.Count;
                }

                throw new ArgumentOutOfRangeException(nameof(index), "Index given is not valid.");
            }
        }

        /// <summary>
        /// 创建一个没有点/子区域的区域。
        /// </summary>
        public MultiArea()
        {
            _subAreas = new List<IReadOnlyArea>();
        }

        /// <summary>
        /// 创建一个只包含给定子区域的MultiArea。
        /// </summary>
        /// <param name="area">要添加的子区域。</param>
        public MultiArea(IReadOnlyArea area)
            : this(area.Yield())
        { }

        /// <summary>
        /// 创建一个由给定子区域组成的MultiArea。
        /// </summary>
        /// <param name="areas">要添加的子区域。</param>
        public MultiArea(IEnumerable<IReadOnlyArea> areas) => _subAreas = new List<IReadOnlyArea>(areas);

        /// <summary>
        /// 将给定的子区域添加到MultiArea中。
        /// </summary>
        /// <param name="subArea">要添加的子区域。</param>
        public void Add(IReadOnlyArea subArea) => _subAreas.Add(subArea);

        /// <summary>
        /// 将给定的子区域添加到MultiArea中。
        /// </summary>
        /// <param name="subAreas">要添加的子区域。</param>
        public void AddRange(IEnumerable<IReadOnlyArea> subAreas) => _subAreas.AddRange(subAreas);

        /// <summary>
        /// 清除MultiArea中的所有子区域。
        /// </summary>
        public void Clear() => _subAreas.Clear();

        /// <summary>
        /// 从MultiArea中移除给定的子区域。
        /// </summary>
        /// <param name="subArea">要移除的子区域。</param>
        public void Remove(IReadOnlyArea subArea) => _subAreas.Remove(subArea);

        // TODO: 在基础库中将此方法制作为扩展方法或默认接口方法；这是从Area中复制粘贴的。
        /// <summary>
        /// 进行相等性比较。如果两个区域包含完全相同的点，则返回true。
        /// </summary>
        /// <param name="other"/>
        /// <returns>如果两个区域包含完全相同的点，则返回true，否则返回false。</returns>
        public bool Matches(IReadOnlyArea? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            // Quick checks that can short-circuit a function that would otherwise require looping over all points
            if (Count != other.Count)
                return false;

            if (Bounds != other.Bounds)
                return false;

            foreach (Point pos in this)
                if (!other.Contains(pos))
                    return false;

            return true;
        }

        /// <summary>
        /// 返回一个枚举器，用于遍历所有子区域中的所有位置。
        /// </summary>
        /// <returns>一个枚举器，用于遍历所有子区域中的所有位置。</returns>
        public IEnumerator<Point> GetEnumerator()
        {
            foreach (var area in _subAreas)
            {
                foreach (var point in area)
                    yield return point;
            }
        }

        /// <summary>
        /// 返回一个枚举器，用于遍历所有子区域中的所有位置。
        /// </summary>
        /// <returns>一个枚举器，用于遍历所有子区域中的所有位置。</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 返回给定的区域是否完全包含在此区域的子区域总和之内。
        /// </summary>
        /// <param name="area">要检查的区域。</param>
        /// <returns>
        /// 如果给定区域的所有点都包含在一个或多个子区域内，则为true，否则为false。
        /// </returns>
        public bool Contains(IReadOnlyArea area)
        {
            foreach (var pos in area)
            {
                // Try to find this point in one of this area's sub-areas
                bool found = false;
                for (var i = 0; i < _subAreas.Count; i++)
                {
                    var subarea = _subAreas[i];
                    if (subarea.Contains(pos))
                    {
                        found = true;
                        break;
                    }
                }

                // If we can't find this point in any sub-area, then the summation of the subareas does NOT contain
                // the area in question.
                if (!found)
                    return false;
            }

            // All points were found in at least one sub-area, so by definition, the summation of the subareas contains
            // the area in question.
            return true;
        }

        /// <summary>
        /// 确定给定的位置是否被认为位于此区域的子区域之一内。
        /// </summary>
        /// <param name="position">要检查的位置。</param>
        /// <returns>如果指定的位置位于子区域之一内，则为true，否则为false。</returns>
        public bool Contains(Point position)
        {
            for (int i = 0; i < _subAreas.Count; i++)
            {
                var subArea = _subAreas[i];
                if (subArea.Contains(position))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 确定给定的位置是否被认为位于此区域的子区域之一内。
        /// </summary>
        /// <param name="positionX">要检查的位置的 X 值。</param>
        /// <param name="positionY">要检查的位置的 Y 值。</param>
        /// <returns>如果指定的位置位于子区域之一内，则为 true，否则为 false。</returns>
        public bool Contains(int positionX, int positionY)
        {
            for (int i = 0; i < _subAreas.Count; i++)
            {
                var subArea = _subAreas[i];
                if (subArea.Contains(positionX, positionY))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 返回给定的地图区域是否与当前区域的任何子区域相交。如果您打算根据此返回值确定/使用确切的交集，
        /// 最好改为调用 <see cref="Area.GetIntersection"/>，并检查结果中的位置数量（如果无交集则为0）。
        /// </summary>
        /// <param name="area">要检查的区域。</param>
        /// <returns>如果给定区域与当前区域的某个子区域相交，则为true，否则为false。</returns>
        public bool Intersects(IReadOnlyArea area)
        {
            for (int i = 0; i < _subAreas.Count; i++)
            {
                var subArea = _subAreas[i];
                if (subArea.Intersects(area))
                    return true;
            }

            return false;
        }
    }
}
