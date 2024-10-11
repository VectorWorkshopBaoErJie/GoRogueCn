using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 在指定的布尔网格视图中查找不同的区域，并使用指定的标签将它们添加到项目列表中。
    /// </summary>
    [PublicAPI]
    public class AreaFinder : GenerationStep
    {
        /// <summary>
        /// 与用于查找区域的网格视图相关联的可选标签。
        /// </summary>
        public readonly string? GridViewComponentTag;

        /// <summary>
        /// 与用于存储此算法找到的区域的组件相关联的可选标签。
        /// </summary>
        public readonly string? AreasComponentTag;

        /// <summary>
        /// 用于确定两个位置是否位于同一区域的邻接方法。
        /// </summary>
        public AdjacencyRule AdjacencyMethod = AdjacencyRule.Cardinals;

        /// <summary>
        /// 创建一个新的AreaFinder生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="AreaFinder"/></param>
        /// <param name="gridViewComponentTag">
        /// 与用于查找区域的网格视图相关联的可选标签。
        /// 默认为"WallFloor"。
        /// </param>
        /// <param name="areasComponentTag">
        /// 与用于存储此算法找到的区域的组件相关联的可选标签。
        /// 默认为"Areas"。
        /// </param>
        public AreaFinder(string? name = null, string? gridViewComponentTag = "WallFloor",
                          string? areasComponentTag = "Areas")
            : base(name, (typeof(IGridView<bool>), gridViewComponentTag))
        {
            AreasComponentTag = areasComponentTag;
            GridViewComponentTag = gridViewComponentTag;
        }

        /// <inheritdoc/>
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Get/create required components
            var gridView = context.GetFirst<IGridView<bool>>(GridViewComponentTag); // Known to succeed because required
            var areas = context.GetFirstOrNew(() => new ItemList<Area>(), AreasComponentTag);

            // Use MapAreaFinder to find unique areas and record them in the correct component
            areas.AddRange(MapAreaFinder.MapAreasFor(gridView, AdjacencyMethod), Name);

            yield break;
        }
    }
}
