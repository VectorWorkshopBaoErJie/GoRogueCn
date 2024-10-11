using System.Collections.Generic;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 使用true/false值随机填充一个布尔型<see cref="SadRogue.Primitives.GridViews.IGridView{T}"/>。如果不存在，则使用给定的标签创建一个网格视图。
    /// </summary>
    [PublicAPI]
    public class RandomViewFill : GenerationStep
    {
        /// <summary>
        /// 必须与设置随机值的网格视图相关联的可选标签。
        /// </summary>
        public readonly string? GridViewComponentTag;

        /// <summary>
        /// 用于填充视图的随机数生成器。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 表示在地图最初随机填充时，给定单元格成为地面单元格的百分比几率。
        /// </summary>
        public float FillProbability = 40f;

        /// <summary>
        /// 是否在随机填充时排除周边点。
        /// </summary>
        public bool ExcludePerimeterPoints = true;

        /// <summary>
        ///  在暂停之前填充多少个方格。默认为不暂停（0）。
        /// </summary>
        public uint FillsBetweenPauses;

        /// <summary>
        /// 创建一个新步骤，用于向地图视图应用随机值。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="RandomViewFill" />。</param>
        /// <param name="gridViewComponentTag">
        /// 必须与设置随机值的网格视图相关联的可选标签。默认为"WallFloor"。
        /// </param>
        public RandomViewFill(string? name = null, string? gridViewComponentTag = "WallFloor")
            : base(name)
        {
            GridViewComponentTag = gridViewComponentTag;
        }

        /// <inheritdoc/>
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (FillProbability > 100)
                throw new InvalidConfigurationException(this, nameof(FillProbability),
                    "The value must be a valid percent (between 0 and 100).");

            // Get or create/add a grid view context component to fill
            var gridViewContext = context.GetFirstOrNew<ISettableGridView<bool>>(
                () => new ArrayView<bool>(context.Width, context.Height),
                GridViewComponentTag);

            // Determine positions to fill based on exclusion settings
            var positionsRect = ExcludePerimeterPoints
                ? gridViewContext.Bounds().Expand(-1, -1)
                : gridViewContext.Bounds();

            // Fill each position with a random value
            uint squares = 0;
            foreach (var position in positionsRect.Positions())
            {
                gridViewContext[position] = RNG.PercentageCheck(FillProbability);
                squares++;
                if (FillsBetweenPauses != 0 && squares == FillsBetweenPauses)
                {
                    squares = 0;
                    yield return null;
                }
            }

            // Pause one last time if we need
            if (FillsBetweenPauses != 0 && squares != 0)
                yield return null;
        }
    }
}
