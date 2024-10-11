using System.Collections.Generic;
using GoRogue.MapGeneration.ConnectionPointSelectors;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.MapGeneration.TunnelCreators;
using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.Steps
{
    /// <summary>
    /// 通过将每个指定区域连接到另一个随机区域，或按照指定的特定顺序连接区域，来连接地图上的区域。
    /// </summary>
    [PublicAPI]
    public class OrderedMapAreaConnection : GenerationStep
    {
        /// <summary>
        /// 可选的标签，必须与此算法连接的地图区域所用的组件相关联。
        /// </summary>
        public readonly string? AreasComponentTag;

        /// <summary>
        /// 可选的标签，必须与创建/用于存储此连接方法所创建的隧道的组件相关联。
        /// </summary>
        public readonly string? TunnelsComponentTag;

        /// <summary>
        /// 可选的标签，必须与此算法更改的瓦片的墙壁/地板状态设置所用的组件相关联。
        /// </summary>
        public readonly string? WallFloorComponentTag;

        /// <summary>
        /// 要使用的区域连接策略。并非所有方法都适用于具有凹形区域的地图
        /// ——有关详细信息，请参阅相应类的文档。
        /// </summary>
        public IConnectionPointSelector ConnectionPointSelector = new RandomConnectionPointSelector();

        /// <summary>
        /// 在连接区域之前是否随机化区域的顺序。如果为 false，则区域将
        /// 连接到由<see cref="AreasComponentTag"/>指定的列表中的下一个区域。
        /// </summary>
        public bool RandomizeOrder;

        /// <summary>
        /// 要使用的隧道创建策略。默认为使用<see cref="GlobalRandom.DefaultRNG"/>的<see cref="HorizontalVerticalTunnelCreator"/>。
        /// </summary>
        public ITunnelCreator TunnelCreator = new HorizontalVerticalTunnelCreator();

        /// <summary>
        /// 用于随机化或房间顺序（如果启用了随机化）的RNG。
        /// </summary>
        public IEnhancedRandom RNG = GlobalRandom.DefaultRNG;

        /// <summary>
        /// 创建一个新的有序区域连接步骤。
        /// </summary>
        /// <param name="name">生成步骤的名称。默认为<see cref="OrderedMapAreaConnection"/>。</param>
        /// <param name="wallFloorComponentTag">
        /// 可选的标签，必须与用于存储/设置地板/墙壁状态的地图视图组件相关联。默认为"WallFloor"。
        /// </param>
        /// <param name="areasComponentTag">
        /// 可选的标签，必须与通过此算法连接的地图区域所用的组件相关联。默认为"Areas"。
        /// </param>
        /// <param name="tunnelsComponentTag">
        /// 可选的标签，必须与创建/用于存储通过此连接方法创建的隧道的组件相关联。默认为"Tunnels"。
        /// </param>
        public OrderedMapAreaConnection(string? name = null, string? wallFloorComponentTag = "WallFloor",
                                        string? areasComponentTag = "Areas", string? tunnelsComponentTag = "Tunnels")
            : base(name, (typeof(ISettableGridView<bool>), wallFloorComponentTag),
                (typeof(ItemList<Area>), areasComponentTag))
        {
            WallFloorComponentTag = wallFloorComponentTag;
            AreasComponentTag = areasComponentTag;
            TunnelsComponentTag = tunnelsComponentTag;
        }

        /// <inheritdoc/>
        protected override IEnumerator<object?> OnPerform(GenerationContext context)
        {
            // Get required components; guaranteed to exist because enforced by required components list
            var areasToConnectOriginal = context.GetFirst<ItemList<Area>>(AreasComponentTag);
            var wallFloor = context.GetFirst<ISettableGridView<bool>>(WallFloorComponentTag);

            // Get/create tunnel component
            var tunnels = context.GetFirstOrNew(() => new ItemList<Area>(), TunnelsComponentTag);

            // Randomize order of connected areas if we need to
            IReadOnlyList<Area> areasToConnect;
            if (RandomizeOrder)
            {
                var list = new List<Area>(areasToConnectOriginal.Items);
                RNG.Shuffle(list);
                areasToConnect = list;
            }
            else
                areasToConnect = areasToConnectOriginal.Items;

            // Connect each area to the next one in the list
            for (int i = 1; i < areasToConnect.Count; i++)
            {
                var (point1, point2) =
                    ConnectionPointSelector.SelectConnectionPoints(areasToConnect[i], areasToConnect[i - 1]);

                var tunnel = TunnelCreator.CreateTunnel(wallFloor, point1, point2);
                tunnels.Add(tunnel, Name);

                yield return null;
            }
        }
    }
}
