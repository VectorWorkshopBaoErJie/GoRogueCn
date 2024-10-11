using System;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.SenseMapping.Sources
{
    /// <summary>
    /// 表示感知地图可用的源的接口。
    /// </summary>
    [PublicAPI]
    public interface ISenseSource
    {
        /// <summary>
        /// 表示感知地图计算结果的网格视图。
        /// </summary>
        IGridView<double> ResultView { get; }

        /// <summary>
        /// 源在地图上的位置。
        /// </summary>
        ref Point Position { get; }

        /// <summary>
        /// 源的最大半径——这是源值散发的最大距离，前提是区域完全无阻碍。
        /// 更改此设置将触发底层数组的重新调整大小（重新分配）。
        /// </summary>
        double Radius { get; set; }

        /// <summary>
        /// 每单位距离感知源值的减少量。自动计算为<see cref="Intensity"/>和<see cref="Radius"/>的乘积。
        /// </summary>
        public double Decay { get; }

        /// <summary>
        /// 用于确定半径形状的距离计算（或可隐式转换为<see cref="Distance"/>的类型，如<see cref="SadRogue.Primitives.Radius"/>）。
        /// </summary>
        Distance DistanceCalc { get; set; }

        /// <summary>
        /// 此源是否已启用。如果在调用<see cref="ISenseMap.Calculate"/>时禁用源，则源不计算值，并有效地假定为“关闭”。
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 是否限制从此源传播的值到某个角度和范围。
        /// </summary>
        bool IsAngleRestricted { get; set; }

        /// <summary>
        /// 要传播的源的起始值。默认为1.0。
        /// </summary>
        double Intensity { get; set; }

        /// <summary>
        /// 如果<see cref="IsAngleRestricted"/>为true，则表示从源起点到由源计算值形成的圆锥体的最外中心点的线的角度（以度为单位）。
        /// 0度指向上方，角度增加则顺时针移动（像指南针一样）。
        /// 否则，这将是0.0度。
        /// </summary>
        double Angle { get; set; }

        /// <summary>
        /// 如果<see cref="IsAngleRestricted"/>为true，
        /// 则表示由源计算值形成的圆锥体的完整弧度的角度（以度为单位）。否则，它将是360度。
        /// </summary>
        double Span { get; set; }

        /// <summary>
        /// 用于执行计算的阻力图。
        /// </summary>
        /// <remarks>
        /// 感知图实现将在计算之前将其设置为感知图的阻力图。这可以通过<see cref="SetResistanceMap"/>进行设置，但除非您正在创建自定义感知图实现，否则不应这样做。
        /// </remarks>
        IGridView<double>? ResistanceView { get; }

        /// <summary>
        /// 当源的半径改变时触发。
        /// </summary>
        event EventHandler? RadiusChanged;

        /// <summary>
        /// 如果源已启用，则通过首先清除现有计算的结果，然后通过调用<see cref="OnCalculate"/>重新进行计算，来执行光照计算。
        /// </summary>
        void CalculateLight();

        /// <summary>
        /// 执行实际的传播计算。
        /// </summary>
        void OnCalculate();

        /// <summary>
        /// 仅应从SenseMap或等效实现中调用。设置源用于计算的阻力图。
        /// </summary>
        /// <param name="resMap">阻力图参数</param>
        void SetResistanceMap(IGridView<double>? resMap);
    }
}
