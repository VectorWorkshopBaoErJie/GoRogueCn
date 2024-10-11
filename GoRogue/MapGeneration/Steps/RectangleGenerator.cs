using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 生成一个非常简单的地图，该地图完全由地面组成，外围有单层厚的墙壁轮廓。
    ///
    /// 所需的上下文组件：
    /// - 无
    /// 添加/使用的上下文组件：
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}" /> where T is bool</term>
    ///         <description>"WallFloor"</description>
    ///     </item>
    /// </list>
    ///
    /// 如果存在现有的墙-地面组件，则使用它；否则，添加一个新的组件。
    /// </summary>
    /// <remarks>
    /// 这个生成步骤简单地将地图变成一个巨大的矩形房间。在给定的标签的地图上下文的地图视图中，它将内部位置设置为
    /// true，将外边缘点设置为 false。如果 GenerationContext 具有现有的地图视图上下文组件，则使用该组件。
    /// 如果没有，则创建一个 <see cref="SadRogue.Primitives.GridViews.ArrayView{T}" />（其中 T 是 bool 类型）并将其添加到地图上下文中，
    /// 其宽度/高度与 <see cref="GenerationContext.Width" />/<see cref="GenerationContext.Height" /> 相匹配。
    /// </remarks>
    [PublicAPI]
    public class RectangleGenerator : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与用于设置此算法更改的图块的墙壁/地面状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 创建一个新的矩形地图生成步骤。
        /// </summary>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地面/墙壁状态的地图视图组件相关联。默认为 "WallFloor"。
        /// </param>
        public RectangleGenerator(string? wallFloorComponentTag = "WallFloor")
        {
            WallFloorComponentTag = wallFloorComponentTag;
        }

        /// <inheritdoc/>
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Get or create/add a wall-floor context component
            var wallFloorContext = context.GetFirstOrNew<ISettableGridView<bool>>(
                () => new ArrayView<bool>(context.Width, context.Height),
                WallFloorComponentTag
            );

            var innerBounds = wallFloorContext.Bounds().Expand(-1, -1);
            foreach (var position in wallFloorContext.Positions())
                wallFloorContext[position] = innerBounds.Contains(position);

            // No stages as its a simple rectangle generator
            yield break;
        }
    }
}
