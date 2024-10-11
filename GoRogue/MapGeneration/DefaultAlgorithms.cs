using System.Collections.Generic;
using GoRogue.MapGeneration.ConnectionPointSelectors;
using GoRogue.MapGeneration.Steps;
using GoRogue.MapGeneration.Steps.Translation;
using GoRogue.MapGeneration.TunnelCreators;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 一组函数，返回预定义的生成步骤系列，用于生成特定类型的地图。如需更自定义的地图生成，请参阅<see cref="Steps"/>中各个步骤的文档，
    /// 并使用AddStep将它们添加到生成器中。
    /// </summary>
    /// <remarks>
    /// 这些算法提供了一种快速生成地图的方式，并演示了如何一起使用生成步骤。请随意查看源代码，并将一个或多个生成步骤复制到自定义生成器中。
    /// </remarks>
    [PublicAPI]
    public static class DefaultAlgorithms
    {
        /// <summary>
        /// 通过在地图上随机确定房间的大小和位置，然后使用基本隧道将它们连接起来，从而生成一个基本的地下城。
        /// </summary>
        /// <param name="rng">用于地图生成的随机数生成器。默认为<see cref="GlobalRandom.DefaultRNG" />。</param>
        /// <param name="minRooms">地图上生成的最小房间数。默认为4。</param>
        /// <param name="maxRooms">地图上生成的最大房间数。默认为10。</param>
        /// <param name="roomMinSize">生成房间的最小允许尺寸。向上取整为奇数。默认为3。</param>
        /// <param name="roomMaxSize">生成房间的最大允许尺寸。向上取整为奇数。默认为7。</param>
        /// <param name="roomSizeRatioX">生成房间的宽度与高度的比例。默认为1.0。</param>
        /// <param name="roomSizeRatioY">生成房间的高度与宽度的比例。默认为1.0。</param>
        /// <param name="maxCreationAttempts">
        /// 在完全放弃生成某个房间之前，重新生成因无法放置在有效位置而失败的房间的最大次数。默认为10。
        /// </param>
        /// <param name="maxPlacementAttempts">
        /// 在放弃并重新生成某个房间之前，尝试在地图上放置房间而不产生交集的最大次数。默认为10。
        /// </param>
        /// <param name="connectionPointSelector">
        /// 要使用的区域连接策略。默认为<see cref="CenterBoundsConnectionPointSelector"/>。</param>
        /// <param name="tunnelCreator">
        /// 要使用的隧道创建策略。默认为使用给定随机数生成器的<see cref="HorizontalVerticalTunnelCreator"/>。</param>
        /// <returns>一组地图生成步骤，用于生成带有相互连接的矩形房间的地图。</returns>
        public static IEnumerable<GenerationStep> BasicRandomRoomsMapSteps(IEnhancedRandom? rng = null, int minRooms = 4,
                                                                      int maxRooms = 10, int roomMinSize = 3,
                                                                      int roomMaxSize = 7, float roomSizeRatioX = 1f,
                                                                      float roomSizeRatioY = 1f,
                                                                      int maxCreationAttempts = 10,
                                                                      int maxPlacementAttempts = 10,
                                                                      IConnectionPointSelector? connectionPointSelector = null,
                                                                      ITunnelCreator? tunnelCreator = null
                                                                      )
        {
            rng ??= GlobalRandom.DefaultRNG;
            tunnelCreator ??= new HorizontalVerticalTunnelCreator(rng);
            connectionPointSelector ??= new CenterBoundsConnectionPointSelector();

            // 1. 生成矩形房间
            yield return new RoomsGeneration
            {
                RNG = rng,
                MinRooms = minRooms,
                MaxRooms = maxRooms,
                RoomMinSize = roomMinSize,
                RoomMaxSize = roomMaxSize,
                RoomSizeRatioX = roomSizeRatioX,
                RoomSizeRatioY = roomSizeRatioY,
                MaxCreationAttempts = maxCreationAttempts,
                MaxPlacementAttempts = maxPlacementAttempts
            };

            // 2. 将上一步给出的房间矩形转换为区域，以便我们可以运行连接算法
            yield return new RectanglesToAreas("Rooms", "RoomAreas");

            // 3. 使用指定的隧道创建方法/连接点选择器随机连接房间
            yield return new OrderedMapAreaConnection(areasComponentTag: "RoomAreas")
            {
                ConnectionPointSelector = connectionPointSelector,
                RandomizeOrder = true,
                RNG = rng,
                TunnelCreator = tunnelCreator
            };

            // 4. 查找并记录应在生成的房间中放置门的位置。
            yield return new DoorFinder();
        }

        /// <summary>
        /// 基于以下流程生成一个地下城地图：
        /// http://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/。
        /// </summary>
        /// <param name="rng">用于地图生成的随机数生成器。默认为<see cref="GlobalRandom.DefaultRNG" />。</param>
        /// <param name="minRooms">在地图上生成的最小房间数量。默认为4。</param>
        /// <param name="maxRooms">在地图上生成的最大房间数量。默认为10。</param>
        /// <param name="roomMinSize">生成房间的最小允许尺寸。向上取整为奇数。默认为3。</param>
        /// <param name="roomMaxSize">生成房间的最大允许尺寸。向上取整为奇数。默认为7。</param>
        /// <param name="roomSizeRatioX">生成房间的宽度与高度的比例。默认为1.0。</param>
        /// <param name="roomSizeRatioY">生成房间的高度与宽度的比例。默认为1.0。</param>
        /// <param name="maxCreationAttempts">
        /// 在放弃完全生成该房间之前，重新生成无法放置在有效位置的房间的最大次数。默认为10。
        /// </param>
        /// <param name="maxPlacementAttempts">
        /// 在放弃并重新生成该房间之前，尝试将房间放置在地图上而不相交的最大次数。默认为10。
        /// </param>
        /// <param name="crawlerChangeDirectionImprovement">
        /// 在迷宫生成过程中，每一步改变爬虫方向的机会增加多少（百分比）。一旦它改变方向，就会重置为0并按此数量增加。
        /// 默认为10。
        /// </param>
        /// <param name="minSidesToConnect">每个房间连接到迷宫的最小侧面数。默认为1。</param>
        /// <param name="maxSidesToConnect">每个房间连接到迷宫的最大侧面数。默认为4。</param>
        /// <param name="cancelSideConnectionSelectChance">
        /// 取消选择侧面连接到迷宫的机率（每个房间百分比）。默认为50。
        /// </param>
        /// <param name="cancelConnectionPlacementChance">
        /// 在房间的给定侧面放置一个门后，取消在该侧面放置另一个门的机率（百分比）。默认为70。
        /// </param>
        /// <param name="cancelConnectionPlacementChanceIncrease">
        /// 每次在房间的给定侧面放置门时，<paramref name="cancelConnectionPlacementChance" />值将增加的数量。默认为10。
        /// </param>
        /// <param name="saveDeadEndChance">
        /// 在死胡同修剪过程中保留死胡同的机率（百分比）。默认为40。
        /// </param>
        /// <param name="maxTrimIterations">
        /// 在死胡同修剪过程中，每次查找死胡同的最大通过次数。默认为无穷大。
        /// </param>
        /// <returns>一组地图生成步骤，生成一个由矩形房间和隧道迷宫连接的地图。</returns>
        public static IEnumerable<GenerationStep> DungeonMazeMapSteps(IEnhancedRandom? rng = null, int minRooms = 4,
                                                                      int maxRooms = 10, int roomMinSize = 3,
                                                                      int roomMaxSize = 7, float roomSizeRatioX = 1f,
                                                                      float roomSizeRatioY = 1f,
                                                                      int maxCreationAttempts = 10,
                                                                      int maxPlacementAttempts = 10,
                                                                      ushort crawlerChangeDirectionImprovement = 10,
                                                                      ushort minSidesToConnect = 1,
                                                                      ushort maxSidesToConnect = 4,
                                                                      ushort cancelSideConnectionSelectChance = 50,
                                                                      ushort cancelConnectionPlacementChance = 70,
                                                                      ushort cancelConnectionPlacementChanceIncrease =
                                                                          10, ushort saveDeadEndChance = 40,
                                                                      int maxTrimIterations = -1)
        {
            rng ??= GlobalRandom.DefaultRNG;

            // 1. 生成矩形房间
            yield return new RoomsGeneration
            {
                RNG = rng,
                MinRooms = minRooms,
                MaxRooms = maxRooms,
                RoomMinSize = roomMinSize,
                RoomMaxSize = roomMaxSize,
                RoomSizeRatioX = roomSizeRatioX,
                RoomSizeRatioY = roomSizeRatioY,
                MaxCreationAttempts = maxCreationAttempts,
                MaxPlacementAttempts = maxPlacementAttempts
            };

            // 2. 在房间之间的空间中生成迷宫
            yield return new MazeGeneration
            {
                RNG = rng,
                CrawlerChangeDirectionImprovement = crawlerChangeDirectionImprovement
            };

            // 3. 确保所有的迷宫都连接成一个迷宫。 
            // 
            // 这个连接步骤默认会连接标签为"Areas"的列表中的区域，但由于我们想要连接迷宫生成步骤留下的隧道（该步骤将其区域添加到了"Tunnels"组件中）， 
            // 因此我们适当地更改了这个标签。同样地，ClosestMapAreaConnection不能从同一区域中获取并存储其结果，所以我们给它一个不同的标签。
            yield return new
                ClosestMapAreaConnection(areasComponentTag: "Tunnels", tunnelsComponentTag: "MazeConnections")
            {
                ConnectionPointSelector = new ClosestConnectionPointSelector(Distance.Manhattan),
                TunnelCreator = new HorizontalVerticalTunnelCreator(rng)
            };

            // 4. 为了使所有隧道都在一个组件中，将MazeConnections添加到隧道中，减去任何重叠的点
            yield return new RemoveDuplicatePoints("Tunnels", "MazeConnections");
            yield return new AppendItemLists<Area>("Tunnels", "MazeConnections") { RemoveAppendedComponent = true };

            // 5. 打开房间的墙壁，将它们连接到迷宫
            yield return new RoomDoorConnection
            {
                RNG = rng,
                MinSidesToConnect = minSidesToConnect,
                MaxSidesToConnect = maxSidesToConnect,
                CancelSideConnectionSelectChance = cancelSideConnectionSelectChance,
                CancelConnectionPlacementChance = cancelConnectionPlacementChance,
                CancelConnectionPlacementChanceIncrease = cancelConnectionPlacementChanceIncrease
            };

            // 6. 修剪迷宫的死胡同，以降低迷宫的密度
            yield return new TunnelDeadEndTrimming
            {
                RNG = rng,
                SaveDeadEndChance = saveDeadEndChance,
                MaxTrimIterations = maxTrimIterations
            };
        }

        /// <summary>
        /// 使用此处的细胞自动机算法生成类似洞穴的地图：
        /// http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels。
        /// </summary>
        /// <param name="rng">要使用的随机数生成器。默认为<see cref="GlobalRandom.DefaultRNG"/>。</param>
        /// <param name="fillProbability">
        /// 表示在地图最初随机填充时，给定单元格成为地面单元格的百分比几率。建议在范围[40, 60]内。
        /// </param>
        /// <param name="totalIterations">
        /// 基于细胞自动机的平滑算法执行的总次数。建议在范围[2, 10]内。
        /// </param>
        /// <param name="cutoffBigAreaFill">
        /// 在切换到更标准的最近邻版本之前，运行更可能导致“分解”大面积区域的细胞自动机平滑变体的总次数。
        /// 必须小于或等于<paramref name="totalIterations"/>。建议在范围[2, 7]内。
        /// </param>
        /// <param name="distanceCalculation">
        /// 用于确定距离/邻居的距离计算，以便确定唯一区域并连接它们。默认为<see cref="SadRogue.Primitives.Distance.Manhattan"/>。
        /// </param>
        /// <param name="connectionPointSelector">
        /// 要使用的区域连接策略。并非所有方法都适用于具有凹面区域的地图——请参阅相应类的文档以获取详细信息。
        /// 默认为使用<see cref="RandomConnectionPointSelector"/>。
        /// </param>
        /// <param name="tunnelCreationMethod">
        /// 要使用的隧道创建策略。默认为具有给定距离的邻接规则的<see cref="DirectLineTunnelCreator"/>。
        /// </param>
        /// <returns>使用细胞自动机算法生成类似洞穴的地图的一组地图生成步骤。</returns>
        public static IEnumerable<GenerationStep> CellularAutomataGenerationSteps(IEnhancedRandom? rng = null,
                                                                                  ushort fillProbability = 40,
                                                                                  int totalIterations = 7,
                                                                                  int cutoffBigAreaFill = 4,
                                                                                  Distance? distanceCalculation = null,
                                                                                  IConnectionPointSelector? connectionPointSelector = null,
                                                                                  ITunnelCreator? tunnelCreationMethod = null)
        {
            rng ??= GlobalRandom.DefaultRNG;
            Distance dist = distanceCalculation ?? Distance.Manhattan;
            connectionPointSelector ??= new RandomConnectionPointSelector(rng);
            tunnelCreationMethod ??= new DirectLineTunnelCreator(dist);

            // 1. 随机使用墙壁/地面填充地图
            yield return new RandomViewFill
            {
                FillProbability = fillProbability,
                RNG = rng
            };

            // 2. 使用细胞自动机算法平滑地图形成区域
            yield return new CellularAutomataAreaGeneration
            {
                AreaAdjacencyRule = dist,
                TotalIterations = totalIterations,
                CutoffBigAreaFill = cutoffBigAreaFill,
            };

            // 3. 查找平滑生成的所有唯一区域并记录它们
            yield return new AreaFinder
            {
                AdjacencyMethod = dist
            };

            // 4. 通过将每个区域连接到其最近的邻居来连接此算法生成的区域
            yield return new ClosestMapAreaConnection
            {
                ConnectionPointSelector = connectionPointSelector,
                DistanceCalc = dist,
                TunnelCreator = tunnelCreationMethod
            };
        }

        /// <summary>
        /// 生成一个简单的地图，该地图是由墙壁包围的空心矩形。
        /// </summary>
        /// <returns>一组地图生成步骤，用于生成由墙壁包围的空心矩形组成的简单地图。</returns>
        public static IEnumerable<GenerationStep> RectangleMapSteps()
        {
            yield return new RectangleGenerator();
        }
    }
}
