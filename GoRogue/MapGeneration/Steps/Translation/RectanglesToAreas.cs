using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.Steps.Translation
{
    /// <summary>
    /// “转换”步骤，该步骤接收一个 <see cref="ItemList{TItem}" /> 作为输入，并将其转换为 <see cref="ItemList{Area}" />。
    /// 可以选择从上下文中移除 <see cref="ItemList{Rectangle}" />。
    /// 所需的上下文组件：
    /// - <see cref="ItemList{Rectangle}" />（标签 <see cref="RectanglesComponentTag" />）：要转换为区域的矩形列表
    /// 添加/使用的上下文组件：
    /// - <see cref="ItemList{Area}" />（标签 <see cref="AreasComponentTag" />）：要添加表示矩形的区域的区域列表。如果不存在，将会创建。
    /// </summary>
    [PublicAPI]
    public class RectanglesToAreas : GenerationStep
    {
        /// <summary>
        /// 必须与用于存储结果区域的组件相关联的标签。
        /// </summary>
        public readonly string AreasComponentTag;

        /// <summary>
        /// 必须与用作输入矩形的组件相关联的标签。
        /// </summary>
        public readonly string RectanglesComponentTag;

        /// <summary>
        /// 是否从上下文中移除输入的矩形列表。默认为false。
        /// </summary>
        public bool RemoveSourceComponent;

        /// <summary>
        /// 创建一个新的步骤，用于将 <see cref="SadRogue.Primitives.Rectangle" /> 列表转换为 <see cref="SadRogue.Primitives.Area" /> 列表。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为 <see cref="RectanglesToAreas" />。</param>
        /// <param name="rectanglesComponentTag">必须与用作输入矩形的组件相关联的标签。</param>
        /// <param name="areasComponentTag">必须与用于存储结果区域的组件相关联的标签。</param>
        public RectanglesToAreas(string? name, string rectanglesComponentTag, string areasComponentTag)
            : base(name, (typeof(ItemList<Rectangle>), rectanglesComponentTag))
        {
            RectanglesComponentTag = rectanglesComponentTag;
            AreasComponentTag = areasComponentTag;
        }

        /// <summary>
        /// 创建一个名为 <see cref="RectanglesToAreas" /> 的新步骤，用于将 <see cref="SadRogue.Primitives.Rectangle" /> 列表转换为 <see cref="SadRogue.Primitives.Area" /> 列表。
        /// </summary>
        /// <param name="rectanglesComponentTag">必须与用作输入矩形的组件相关联的标签。</param>
        /// <param name="areasComponentTag">必须与用于存储结果区域的组件相关联的标签。</param>
        public RectanglesToAreas(string rectanglesComponentTag, string areasComponentTag)
            : this(null, rectanglesComponentTag, areasComponentTag)
        { }

        /// <inheritdoc />
        protected override IEnumerator<object> OnPerform(GenerationContext context)
        {
            // Get required components; guaranteed to exist because enforced by required components list
            var rectangles = context.GetFirst<ItemList<Rectangle>>(RectanglesComponentTag);

            // Get/create output component as needed
            var areas = context.GetFirstOrNew(() => new ItemList<Area>(), AreasComponentTag);

            if (RemoveSourceComponent)
                context.Remove(rectangles);

            foreach (var rect in rectangles.Items)
            {
                var area = new Area { rect.Positions() };
                areas.Add(area, Name);
            }

            yield break;
        }
    }
}
