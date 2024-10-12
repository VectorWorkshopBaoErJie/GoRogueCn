using System;
using System.Collections.Generic;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    // TODO: 添加邻接规则支持（3面墙假设为曼哈顿距离）
    /// <summary>
    /// 搜索不会通向任何地方的隧道（例如，被3面墙包围的），并从地图中删除它们。
    /// 所需的上下文组件：
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <description>Default Tag</description>
    ///     </listheader>
    ///     <item>
    ///         <term>
    ///             <see cref="ItemList{TItem}" />
    ///         </term>
    ///         <description>"Tunnels"</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="SadRogue.Primitives.GridViews.ISettableGridView{T}" /> where T is bool</term>
    ///         <description>"WallFloor"</description>
    ///     </item>
    /// </list>
    /// 添加的上下文组件：
    /// - 无
    /// </summary>
    /// <remarks>
    /// 此算法会遍历带有给定标签的 <see cref="ItemList{Area}" /> 上下文组件中的所有地图区域。
    /// 对于每个区域，它会扫描死胡同
    /// （即根据给定的 “WallFloor” 组件，被 3 面墙包围的位置）。对于每个死胡同，如果它当前不是，
    /// 并且之前也没有被选为“保存”的，基于百分比检查，算法会继续填充它。它会从相应的
    /// 区域中移除死胡同位置，并将 “WallFloor” 地图中的该位置设置为 true。
    /// 它以这种方式进行，直到找不到更多（未保存的）死胡同，或者达到给定的最大迭代次数，
    /// 然后继续处理 ItemList 中的下一个区域，直到处理完所有区域。
    /// </remarks>
    [PublicAPI]
    public class TunnelDeadEndTrimming : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与记录隧道区域的组件相关联，本算法将从这个区域中修剪死胡同。
        /// </summary>
        public readonly string? TunnelsComponentTag;

        /// <summary>
        /// 可选的标签，必须与用于设置由此算法更改的图块的墙壁/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 每个区域寻找死胡同的最大遍历次数。默认为无穷大。
        /// </summary>
        public int MaxTrimIterations = -1;

        /// <summary>
        /// 用于百分比检查的随机数生成器。默认为<see cref="GlobalRandom.DefaultRNG" />。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;
        public readonly string? TunnelsComponentTag;

        /// <summary>
        /// 可选的标签，该标签必须与用于设置由本算法更改的图块的墙壁/地板状态的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 每个区域寻找死胡同的最大遍历次数。默认为无穷大。
        /// </summary>
        public int MaxTrimIterations = -1;

        /// <summary>
        /// 用于百分比检查的随机数生成器。默认为<see cref="GlobalRandom.DefaultRNG" />。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 死胡同被保留的百分比几率（100中的几率）。默认为40。
        /// </summary>
        public float SaveDeadEndChance = 40f;

        /// <summary>
        /// 创建一个新的去除死胡同的生成步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="TunnelDeadEndTrimming" />。</param>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于设置由本算法更改的图块的墙壁/地板状态的组件相关联。默认为"WallFloor"。
        /// </param>
        /// <param name="tunnelsComponentTag">
        /// 可选的标签，必须与用于记录代表隧道的区域的组件相关联，该算法将从这些隧道中修剪死胡同。默认为"Tunnels"。
        /// </param>
        public TunnelDeadEndTrimming(string? name = null, string? wallFloorComponentTag = "WallFloor",
                                     string? tunnelsComponentTag = "Tunnels")
            : base(name, (typeof(ISettableGridView<bool>), wallFloorComponentTag),
                (typeof(ItemList<Area>), tunnelsComponentTag))
        {
            WallFloorComponentTag = wallFloorComponentTag;
            TunnelsComponentTag = tunnelsComponentTag;
        }

        /// <inheritdoc />
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Validate configuration
            if (SaveDeadEndChance > 100)
                throw new InvalidConfigurationException(this, nameof(SaveDeadEndChance),
                    "The value must be a valid percent (between 0 and 100).");

            if (MaxTrimIterations < -1)
                throw new InvalidConfigurationException(this, nameof(MaxTrimIterations),
                    "The value must be 0 or above, or -1 for no iteration limit.");

            // Get required components; guaranteed to exist because enforced by required components list
            var wallFloor = context.GetFirst<ISettableGridView<bool>>(WallFloorComponentTag);
            var tunnels = context.GetFirst<ItemList<Area>>(TunnelsComponentTag);

            // For each area, find dead ends up to the maximum number of iterations and prune them, unless they're saved
            foreach (var area in tunnels.Items)
            {
                HashSet<Point> safeDeadEnds = new HashSet<Point>();
                HashSet<Point> deadEnds = new HashSet<Point>();

                var iteration = 1;
                while (MaxTrimIterations == -1 || iteration <= MaxTrimIterations)
                {
                    foreach (var point in area)
                        foreach (var direction in AdjacencyRule.Cardinals.DirectionsOfNeighborsClockwise())
                        {
                            var neighbor = point + direction;

                            if (wallFloor[neighbor])
                            {
                                var oppositeNeighborDir = direction + 4;
                                var found = false;

                                // If we get here, source direction is a floor, opposite direction
                                // should be wall
                                if (!wallFloor[point + oppositeNeighborDir])
                                    // Check for a wall pattern in the map. Where X is a wall,
                                    // checks the appropriately rotated version of:
                                    // XXX
                                    // X X
                                    found = oppositeNeighborDir.Type switch
                                    {
                                        Direction.Types.Up => !wallFloor[point + Direction.UpLeft] &&
                                                              !wallFloor[point + Direction.UpRight] &&
                                                              !wallFloor[point + Direction.Left] &&
                                                              !wallFloor[point + Direction.Right],

                                        Direction.Types.Down => !wallFloor[point + Direction.DownLeft] &&
                                                                !wallFloor[point + Direction.DownRight] &&
                                                                !wallFloor[point + Direction.Left] &&
                                                                !wallFloor[point + Direction.Right],

                                        Direction.Types.Right => !wallFloor[point + Direction.UpRight] &&
                                                                 !wallFloor[point + Direction.DownRight] &&
                                                                 !wallFloor[point + Direction.Up] &&
                                                                 !wallFloor[point + Direction.Down],

                                        Direction.Types.Left => !wallFloor[point + Direction.UpLeft] &&
                                                                !wallFloor[point + Direction.DownLeft] &&
                                                                !wallFloor[point + Direction.Up] &&
                                                                !wallFloor[point + Direction.Down],

                                        _ => throw new Exception(
                                            "Cannot occur since original neighbor direction was a cardinal.")
                                    };

                                // If we found a dead end and it's not already safe, then add it to the list
                                if (found && !safeDeadEnds.Contains(point))
                                    deadEnds.Add(point);

                                break; // Even if it is already saved, we know it's a dead end so we can stop processing this point
                            }
                        }

                    // No dead ends to process
                    if (deadEnds.Count == 0)
                        break;

                    // Process cancel chance for each dead end
                    foreach (var point in deadEnds)
                        if (RNG.PercentageCheck(SaveDeadEndChance))
                            safeDeadEnds.Add(point);

                    // Remove newly cancelled dead ends
                    deadEnds.ExceptWith(safeDeadEnds);

                    // Fill in all the selected dead ends on the wall-floor map
                    foreach (var point in deadEnds)
                        wallFloor[point] = false;

                    // Remove dead ends from the list of points in the tunnel
                    area.Remove(deadEnds);

                    // Clear dead ends for next pass and record the completed iteration
                    deadEnds.Clear();
                    iteration++;
                }

                yield return null;
            }
        }
    }
}
