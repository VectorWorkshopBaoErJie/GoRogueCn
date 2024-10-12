using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration.Steps.Translation
{
    /// <summary>
    /// 将一个项目列表追加到另一个项目列表上，并可选择从上下文中移除被追加的那个列表。
    /// 所需的上下文组件：
    /// - <see cref="ItemList{TItem}" />（标签 <see cref="BaseListTag" />）：作为基础的列表，其他列表将被追加到这个列表上
    /// - <see cref="ItemList{TItem}" />（标签 <see cref="ListToAppendTag" />）：其项目将被追加到基础列表上的列表。
    ///   如果 <see cref="RemoveAppendedComponent" /> 为真，此组件将从上下文中移除。
    /// </summary>
    /// <typeparam name="TItem">被追加的列表中的项目类型。</typeparam>
    [PublicAPI]
    public class AppendItemLists<TItem> : GenerationStep
        where TItem : notnull
    {
        /// <summary>
        /// 必须附加到将有其他列表的项目追加到其上的组件的标签。
        /// </summary>
        public readonly string BaseListTag;

        /// <summary>
        /// 必须附加到将其项目追加到基础列表的组件上的标签。
        /// </summary>
        public readonly string ListToAppendTag;

        /// <summary>
        /// 在将带有 <see cref="ListToAppendTag" /> 标签的组件的项目添加到基础列表后，是否移除该组件。默认为 false。
        /// </summary>
        public bool RemoveAppendedComponent;

        /// <summary>
        /// 创建一个新的追加列表的生成组件。
        /// </summary>
        /// <param name="name">此组件的名称。</param>
        /// <param name="baseListTag">
        /// 必须附加到将有其他列表的项目追加到其上的组件的标签。
        /// </param>
        /// <param name="listToAppendTag">
        /// 必须附加到将其项目追加到基础列表的组件上的标签。
        /// </param>
        public AppendItemLists(string? name, string baseListTag, string listToAppendTag)
            : base(name, (typeof(ItemList<TItem>), baseListTag), (typeof(ItemList<TItem>), listToAppendTag))
        {
            BaseListTag = baseListTag;
            ListToAppendTag = listToAppendTag;

            // Check here since the tags are given in the constructor
            if (BaseListTag == ListToAppendTag)
                throw new InvalidConfigurationException(this, nameof(BaseListTag),
                    $"An ItemList cannot be appended to itself, so the base tag must be different than the {nameof(ListToAppendTag)}.");
        }

        /// <summary>
        /// 创建一个新的用于追加列表的生成组件。
        /// </summary>
        /// <param name="baseListTag">
        /// 必须附加到将接收其他列表项目的组件上的标签。
        /// </param>
        /// <param name="listToAppendTag">
        /// 必须附加到将其项目追加到基础列表的组件上的标签。
        /// </param>
        public AppendItemLists(string baseListTag, string listToAppendTag)
            : this(null, baseListTag, listToAppendTag)
        { }

        /// <inheritdoc />
        protected override IEnumerator<object> OnPerform(GenerationContext context)
        {
            // Get required components; guaranteed to exist because enforced by required components list
            var baseList = context.GetFirst<ItemList<TItem>>(BaseListTag);
            var listToAppend = context.GetFirst<ItemList<TItem>>(ListToAppendTag);

            // Iterate over each individual position and add to original list, so we keep the original generation step that created it with it.
            foreach (var item in listToAppend.Items)
                baseList.Add(item, listToAppend.ItemToStepMapping[item]);

            yield break;
        }
    }
}
