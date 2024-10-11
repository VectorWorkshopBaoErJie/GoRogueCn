using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.MapGeneration.TunnelCreators
{
    /// <summary>
    /// 用于实现在可步行性地图上两个位置之间创建隧道的算法的接口。
    /// </summary>
    [PublicAPI]
    public interface ITunnelCreator
    {
        /// <summary>
        /// 实现该算法，在两个点之间创建隧道（确保这两个点之间存在一条设置为true的位置路径）。
        /// </summary>
        /// <param name="map">要在其上创建隧道的网格。</param>
        /// <param name="tunnelStart">要连接的起始位置。</param>
        /// <param name="tunnelEnd">要连接的结束位置。</param>
        /// <returns>包含隧道中所有点的区域。</returns>
        Area CreateTunnel(ISettableGridView<bool> map, Point tunnelStart, Point tunnelEnd);

        /// <summary>
        /// 实现该算法，在两个点之间创建隧道（确保这两个点之间存在一条设置为true的位置路径）。
        /// </summary>
        /// <param name="map">要在其上创建隧道的网格。</param>
        /// <param name="startX">要连接的起始位置的X值。</param>
        /// <param name="startY">要连接的起始位置的Y值。</param>
        /// <param name="endX">要连接的结束位置的X值。</param>
        /// <param name="endY">要连接的结束位置的Y值。</param>
        /// <returns>包含隧道中所有点的区域。</returns>
        Area CreateTunnel(ISettableGridView<bool> map, int startX, int startY, int endX, int endY);
    }
}
