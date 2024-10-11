using System;
using System.Text;
using GoRogue.Components;
using GoRogue.Components.ParentAware;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 当一个区域的面积发生变化时触发的事件。
    /// </summary>
    [PublicAPI]
    public class RegionAreaChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 之前的区域面积
        /// </summary>
        public readonly PolygonArea OldValue;

        /// <summary>
        /// 我们更新到的区域面积
        /// </summary>
        public readonly PolygonArea NewValue;

        /// <summary>
        /// 当一个区域的面积发生变化时的事件参数
        /// </summary>
        /// <param name="oldValue">区域面积的前一个值</param>
        /// <param name="newValue">区域面积的新值</param>
        public RegionAreaChangedEventArgs(PolygonArea oldValue, PolygonArea newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    /// <summary>
    /// 地图上具有任意形状和大小的四边区域
    /// </summary>
    [PublicAPI]
    public class Region : IObjectWithComponents
    {
        /// <summary>
        /// 这个区域的面积
        /// </summary>
        public PolygonArea Area
        {
            get => _area;
            set
            {
                if (value == _area) return;

                var oldValue = _area;
                _area = value;
                AreaChanged?.Invoke(this, new RegionAreaChangedEventArgs(oldValue, value));
            }
        }
        private PolygonArea _area;

        /// <summary>
        /// 当面积发生变化时触发。
        /// </summary>
        public EventHandler<RegionAreaChangedEventArgs>? AreaChanged;

        /// <inheritdoc/>
        public IComponentCollection GoRogueComponents { get; }

        /// <summary>
        /// 使用提供的面积返回一个新的区域
        /// </summary>
        /// <param name="area">这个区域的面积</param>
        /// <param name="components">这个区域的组件集合</param>
        public Region(PolygonArea area, IComponentCollection? components = null)
        {
            _area = area;
            GoRogueComponents = components ?? new ComponentCollection();
            GoRogueComponents.ParentForAddedComponents = this;
        }

        /// <summary>
        /// 返回一个字符串，详细描述区域的角落位置。
        /// </summary>
        public override string ToString()
        {
            var answer = new StringBuilder("Region with ");
            answer.Append($"{GoRogueComponents.Count} components and the following ");
            answer.Append(Area);
            return answer.ToString();
        }
    }
}
