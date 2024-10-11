using System.Collections.Generic;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 使用元胞自动机平滑算法对给定地图上的区域进行平滑处理。
    /// </summary>
    [PublicAPI]
    public class CellularAutomataAreaGeneration : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与用于设置由此算法更改的瓦片的墙壁/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 用于确定由此算法生成的唯一区域的邻接规则。
        /// </summary>
        public AdjacencyRule AreaAdjacencyRule = AdjacencyRule.Cardinals;

        /// <summary>
        /// 基于元胞自动机的平滑算法执行的总次数。
        /// 建议范围在[2, 10]。
        /// </summary>
        public int TotalIterations = 7;

        /// <summary>
        /// 在切换到更标准的最近邻版本之前，更可能导致“分解”大面积区域的元胞自动机平滑变体的运行总次数。
        /// 必须小于或等于<see cref="TotalIterations"/>。建议范围在[2, 7]。
        /// </summary>
        public int CutoffBigAreaFill = 4;

        /// <summary>
        /// 创建一个基于元胞自动机的新区域生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为 <see cref="CellularAutomataAreaGeneration" />。</param>
        ///
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地板/墙壁状态的地图视图组件相关联。默认为 "WallFloor"。
        /// </param>
        public CellularAutomataAreaGeneration(string? name = null, string? wallFloorComponentTag = "WallFloor")
            : base(name)
        {
            WallFloorComponentTag = wallFloorComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (CutoffBigAreaFill > TotalIterations)
                throw new InvalidConfigurationException(this, nameof(CutoffBigAreaFill),
                    $"The value must be less than or equal to the value of {nameof(TotalIterations)}.");

            // Get or create/add a wall-floor context component
            var wallFloorContext = context.GetFirstOrNew<ISettableGridView<bool>>(
                () => new ArrayView<bool>(context.Width, context.Height),
                WallFloorComponentTag);

            // Create a new array map to use in the smoothing algorithms to temporarily store old values.
            // Allocating it here instead of in the smoothing minimizes allocations.
            var oldMap = new ArrayView<bool>(wallFloorContext.Width, wallFloorContext.Height);

            // Iterate over the generated values, smoothing them with the appropriate algorithm
            for (int i = 0; i < TotalIterations; i++)
            {
                CellAutoSmoothingAlgo(wallFloorContext, oldMap, i < CutoffBigAreaFill);
                yield return null;
            }

            // Fill to a rectangle to ensure the resulting areas are enclosed
            foreach (var pos in wallFloorContext.Bounds().PerimeterPositions())
                wallFloorContext[pos] = false;
        }

        private static void CellAutoSmoothingAlgo(ISettableGridView<bool> map, ArrayView<bool> oldMap, bool bigAreaFill)
        {
            // Record current state of the map so we can compare to it to determine nearest walls
            oldMap.ApplyOverlay(map);

            // Iterate over inner square only to avoid messing with outer walls
            foreach (var pos in map.Bounds().Expand(-1, -1).Positions())
            {
                if (CountWallsNear(oldMap, pos, 1) >= 5 || bigAreaFill && CountWallsNear(oldMap, pos, 2) <= 2)
                    map[pos] = false;
                else
                    map[pos] = true;
            }
        }

        private static int CountWallsNear(ArrayView<bool> map, Point centerPos, int distance)
        {
            int count = 0;

            foreach (var pos in Radius.Square.PositionsInRadius(centerPos, distance))
                if (map.Contains(pos) && pos != centerPos && !map[pos])
                    count += 1;

            return count;
        }
    }
}
