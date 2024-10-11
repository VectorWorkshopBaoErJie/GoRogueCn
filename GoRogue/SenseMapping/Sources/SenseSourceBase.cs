using System;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.SenseMapping.Sources
{
    /// <summary>
    /// 一个用于创建<see cref="ISenseSource"/>实现的基类，它将实现简化为主要实现<see cref="OnCalculate"/>函数。
    /// </summary>
    /// <remarks>
    /// 此类使用ArrayView作为<see cref="ResultView"/>，以便高效地启用通用功能。此处自定义视图的使用场景应该相对有限，
    /// 因为视图必须是可设置和可调整大小的；如果需要自定义实现，您可以直接实现<see cref="ISenseSource"/>。
    /// </remarks>
    [PublicAPI]
    public abstract class SenseSourceBase : ISenseSource
    {
        /// <summary>
        /// 结果视图的大小（例如，其宽度和高度）；为提高效率和便利性而进行缓存。
        /// </summary>
        protected int Size;

        /// <summary>
        /// 将作为结果视图中心点的坐标，即中心点是 (Center, Center)。
        /// </summary>
        /// <remarks>
        /// 这等同于 Size / 2；但是，由于此计算频繁执行，因此为提高性能和便利性而进行了缓存。
        /// </remarks>
        protected int Center;

        // 这里声明为不为null是因为它在Radius设置器中进行了初始化，但我们无法访问MemberNotNull属性。
        /// <summary>
        /// 用于记录结果的结果视图。
        /// </summary>
        protected ArrayView<double> ResultViewBacking = null!;
        /// <inheritdoc/>
        public IGridView<double> ResultView => ResultViewBacking;

        /// <inheritdoc/>
        public double Decay { get; private set; }

        private Point _position;
        /// <inheritdoc/>
        public ref Point Position => ref _position;

        private double _radius;
        /// <inheritdoc/>
        public double Radius
        {
            get => _radius;
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException(nameof(Radius),
                        "Radius for a SenseSource must be greater than 0.");

                var newRadius = Math.Max(1, value);
                if (newRadius.Equals(_radius)) return;

                _radius = newRadius;
                // 这里可以向下取整，因为欧几里得距离形状总是包含在切比雪夫距离形状内
                Size = (int)_radius * 2 + 1;
                // 任何数乘以2都是偶数，加1就是奇数。例如，半径3，3*2 = 6，+1 = 7。7/2=3，这被用作数组中间的索引，所以数学计算是有效的
                Center = Size / 2;
                ResultViewBacking = new ArrayView<double>(Size, Size);

                Decay = _intensity / (_radius + 1);

                RadiusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public event EventHandler? RadiusChanged;

        /// <inheritdoc/>
        public Distance DistanceCalc { get; set; }

        /// <inheritdoc/>
        public bool Enabled { get; set; }

        /// <inheritdoc/>
        public bool IsAngleRestricted { get; set; }

        private double _intensity;
        /// <inheritdoc/>
        public double Intensity
        {
            get => _intensity;

            set
            {
                if (value < 0.0)
                    throw new ArgumentOutOfRangeException(nameof(Intensity),
                        "Intensity for sense source cannot be set to less than 0.0.");


                _intensity = value;
                Decay = _intensity / (_radius + 1);
            }
        }

        /// <summary>
        /// <see cref="Angle"/>的值，但顺时针偏移了90度；即，0度指向右边而不是上方。这个值通常在实际的光线计算中效果更好（因为其定义更贴近单位圆）。
        /// </summary>
        protected double AngleInternal;

        /// <inheritdoc/>
        public double Angle
        {
            get => IsAngleRestricted ? MathHelpers.WrapAround(AngleInternal + 90, 360.0) : 0.0;
            set
            {
                // 将内部角度偏移，使0度指向右边而不是上方
                AngleInternal = value - 90;

                // 将角度包裹到适当的度数范围内
                if (AngleInternal > 360.0 || AngleInternal < 0)
                    AngleInternal = MathHelpers.WrapAround(AngleInternal, 360.0);
            }
        }

        private double _span;
        /// <inheritdoc/>
        public double Span
        {
            get => IsAngleRestricted ? _span : 360.0;
            set
            {
                if (value < 0.0 || value > 360.0)
                    throw new ArgumentOutOfRangeException(nameof(Span), "SenseSource Span must be in range [0, 360]");

                _span = value;

                IsAngleRestricted = !_span.Equals(360.0);
            }
        }

        /// <inheritdoc/>
        public IGridView<double>? ResistanceView { get; private set; }
        /// <summary>
        /// 创建一个向所有方向扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或可隐式转换为<see cref="Distance"/>的类型，例如<see cref="SadRogue.Primitives.Radius"/>）。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        protected SenseSourceBase(Point position, double radius, Distance distanceCalc,
            double intensity = 1.0)
        {
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Sense source radius cannot be 0");

            if (intensity < 0)
                throw new ArgumentOutOfRangeException(nameof(intensity),
                    "Sense source intensity cannot be less than 0.0.");

            Position = position;
            Radius = radius; // Arrays are initialized by this setter
            DistanceCalc = distanceCalc;

            ResistanceView = null;
            Enabled = true;

            IsAngleRestricted = false;
            Intensity = intensity;
        }

        /// <summary>
        /// 创建一个向所有方向扩散的源。
        /// </summary>
        /// <param name="positionX">
        /// 源在地图上位置的X值。
        /// </param>
        /// <param name="positionY">
        /// 源在地图上位置的Y值。
        /// </param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或可隐式转换为<see cref="Distance"/>的类型，例如<see cref="SadRogue.Primitives.Radius"/>）。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        protected SenseSourceBase(int positionX, int positionY, double radius, Distance distanceCalc,
            double intensity = 1.0)
            : this(new Point(positionX, positionY), radius, distanceCalc, intensity)
        { }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的锥体内扩散的源。
        /// </summary>
        /// <param name="position">源在地图上的位置。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或可隐式转换为<see cref="Distance"/>的类型，例如<see cref="SadRogue.Primitives.Radius"/>）。
        /// </param>
        /// <param name="angle">
        /// 以角度为单位，指定由源值形成的锥体的最外中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以角度为单位，指定由源值形成的锥体所包含的全弧——锥体中心线两侧各包含<paramref name="angle" /> / 2度。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        protected SenseSourceBase(Point position, double radius, Distance distanceCalc, double angle,
                           double span, double intensity = 1.0)
            : this(position, radius, distanceCalc, intensity)
        {
            if (span < 0.0 || span > 360.0)
                throw new ArgumentOutOfRangeException(nameof(span),
                    "Span used to initialize a sense source must be in range [0, 360]");

            Angle = angle;
            // This also sets IsAngleRestricted appropriately.
            Span = span;
        }

        /// <summary>
        /// 构造函数。创建一个仅在由给定角度和跨度定义的锥体内扩散的源。
        /// </summary>
        /// <param name="positionX">源在地图上位置的X值。</param>
        /// <param name="positionY">源在地图上位置的Y值。</param>
        /// <param name="radius">
        /// 源的最大半径——这是源值将散发的最大距离，前提是区域完全无阻碍。
        /// </param>
        /// <param name="distanceCalc">
        /// 用于确定半径形状的距离计算方式（或可隐式转换为<see cref="Distance"/>的类型，例如<see cref="SadRogue.Primitives.Radius"/>）。
        /// </param>
        /// <param name="angle">
        /// 以角度为单位，指定由源值形成的锥体的最外中心点。0度指向右侧。
        /// </param>
        /// <param name="span">
        /// 以角度为单位，指定由源值形成的锥体所包含的全弧——锥体中心线两侧各包含<paramref name="angle" /> / 2度。
        /// </param>
        /// <param name="intensity">源的起始强度值。默认为1.0。</param>
        protected SenseSourceBase(int positionX, int positionY, double radius, Distance distanceCalc,
            double angle, double span, double intensity = 1.0)
            : this(new Point(positionX, positionY), radius, distanceCalc, angle, span, intensity)
        { }

        /// <summary>
        /// 重置计算状态，以便可以开始新的一组计算。
        /// </summary>
        protected virtual void Reset()
        {
            ResultViewBacking.Clear();
            ResultViewBacking[Center, Center] = _intensity; // source light is center, starts out at our intensity
        }

        /// <inheritdoc/>
        public void CalculateLight()
        {
            if (!Enabled) return;

            if (ResistanceView == null)
                throw new InvalidOperationException(
                    "Attempted to calculate the light of a sense map without a resistance map.  This is almost certainly a bug in the implementation of the sense map.");

            Reset();
            OnCalculate();
        }

        /// <inheritdoc/>
        public abstract void OnCalculate();


        /// <inheritdoc/>
        public void SetResistanceMap(IGridView<double>? resMap) => ResistanceView = resMap;

        /// <summary>
        /// 返回此 SenseSource 配置的字符串表示形式。
        /// </summary>
        /// <returns>此 SenseSource 配置的字符串表示形式。</returns>
        public override string ToString()
            => $"Enabled: {Enabled}, Type: {GetType().Name}, Radius Mode: {(Radius)DistanceCalc}, Position: {Position}, Radius: {Radius}";
    }
}
