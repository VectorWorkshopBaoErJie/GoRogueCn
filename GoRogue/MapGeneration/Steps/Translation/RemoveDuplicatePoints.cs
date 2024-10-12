using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.Steps.Translation
{
    /// <summary>
    /// 从一个区域列表中移除所有也出现在另一个列表中的任何区域的所有点。
    /// 所需的上下文组件：
    /// - <see cref="ItemList{TItem}" />（标签 <see cref="UnmodifiedAreaListTag" />）：不会修改的区域列表，但将作为从另一个列表中区域移除点的基础。
    /// - <see cref="ItemList{Area}" />（标签 <see cref="ModifiedAreaListTag" />）：将要修改的区域列表；此列表中的所有区域都将移除也出现在另一个列表区域中的任何点。如果某个区域最终没有剩余的点，它将被从列表中移除。
    /// </summary>
    /// <remarks>
    /// 此组件将从修改后的区域列表中的任何区域中移除所有也出现在一个或多个未修改区域列表中的位置。如果某个区域被修改后没有剩余的点，它将被完全从列表中移除。
    /// 这确保了这两个列表不包含任何相互重叠的位置。
    /// </remarks>
    [PublicAPI]
    public class RemoveDuplicatePoints : GenerationStep
    {
        /// <summary>
        /// 必须与用作移除重复项的区域列表的组件相关联的标签。
        /// </summary>
        public readonly string ModifiedAreaListTag;

        /// <summary>
        /// 必须与用作未修改区域列表的组件相关联的标签。
        /// </summary>
        public readonly string UnmodifiedAreaListTag;

        /// <summary>
        /// 创建一个新的区域重复点移除步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。</param>
        /// <param name="unmodifiedAreaListTag">必须与用作未修改区域列表的组件相关联的标签。</param>
        /// <param name="modifiedAreaListTag">
        /// 必须与用作移除重复项的区域列表的组件相关联的标签。
        /// </param>
        public RemoveDuplicatePoints(string? name, string unmodifiedAreaListTag, string modifiedAreaListTag)
            : base(name, (typeof(ItemList<Area>), unmodifiedAreaListTag), (typeof(ItemList<Area>), modifiedAreaListTag))
        {
            UnmodifiedAreaListTag = unmodifiedAreaListTag;
            ModifiedAreaListTag = modifiedAreaListTag;

            // Validate here because it was given in the constructor
            if (ModifiedAreaListTag == UnmodifiedAreaListTag)
                throw new InvalidConfigurationException(this, nameof(ModifiedAreaListTag),
                    $"The value must be different than the value of {nameof(UnmodifiedAreaListTag)}.");
        }

        /// <summary>
        /// 创建一个名为 <see cref="RemoveDuplicatePoints" /> 的新区域重复点移除步骤。
        /// </summary>
        /// <param name="unmodifiedAreaListTag">必须与用作未修改区域列表的组件相关联的标签。</param>
        /// <param name="modifiedAreaListTag">
        /// 必须与用作移除重复项的区域列表的组件相关联的标签。
        /// </param>
        public RemoveDuplicatePoints(string unmodifiedAreaListTag, string modifiedAreaListTag)
            : this(null, unmodifiedAreaListTag, modifiedAreaListTag)
        { }

        /// <inheritdoc />
        protected override IEnumerator<object> OnPerform(GenerationContext context)
        {
            // 获取必需的组件；由于必需组件列表的强制要求，这些组件一定存在
            var areaList1 = context.GetFirst<ItemList<Area>>(ModifiedAreaListTag);
            var areaList2 = context.GetFirst<ItemList<Area>>(UnmodifiedAreaListTag);

            // 缓存 area1List 中任何区域的所有位置
            var areaList1Positions = new HashSet<Point>();
            foreach (var area in areaList1.Items)
                foreach (var point in area)
                    areaList1Positions.Add(point);

            // 从第二个列表的区域中移除任何已经存在于第一个列表中的位置
            foreach (var area in areaList2.Items)
                area.Remove(pos => areaList1Positions.Contains(pos));

            // 移除现在不包含任何位置的区域
            areaList2.Remove(area => area.Count == 0);

            yield break;
        }
    }
}
