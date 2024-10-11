using System;
using System.Collections.Generic;
using System.Text;
using GoRogue.SenseMapping.Sources;
using JetBrains.Annotations;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogue.SenseMapping
{
    /// <summary>
    /// 一个包含网格视图和自定义调整大小函数的结构，用于将该网格视图调整为新的大小。
    /// </summary>
    /// <remarks>
    /// <see cref="Resizer"/> 接收所需的新宽度/高度和旧的网格视图，并必须返回适当大小的新网格视图。
    /// 它可以返回一个新对象或一个现有对象；在任何情况下，结果都将在调整大小时分配给感知图的结果视图。
    /// 调整大小函数还必须确保，当返回新的网格视图时，必须将所有值设置为 0.0。
    /// 如果底层数据结构被重新分配，从而单元格被隐式清除，这可以避免执行清除操作。
    ///
    /// 该结构还提供了一个 <see cref="ArrayViewResizer"/>，如果 <see cref="ResultView"/> 是 ArrayView，它是一个高效的调整器。
    /// </remarks>
    [PublicAPI]
    public readonly struct CustomResultViewWithResize
    {
        /// <summary>
        /// 要使用的初始结果视图。
        /// </summary>
        public readonly ISettableGridView<double> ResultView;

        /// <summary>
        /// 要使用的调整器函数。有关此函数的约束，请参阅 <see cref="CustomResultViewWithResize"/> 类描述。
        /// </summary>
        public readonly Func<int, int, ISettableGridView<double>, ISettableGridView<double>> Resizer;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="resultView">最初要使用的结果视图。</param>
        /// <param name="resizer">必须清除、调整大小并返回结果视图的调整器函数。</param>
        public CustomResultViewWithResize(ISettableGridView<double> resultView,
            Func<int, int, ISettableGridView<double>, ISettableGridView<double>> resizer)
        {
            ResultView = resultView;
            Resizer = resizer;
        }

        /// <summary>
        /// 当使用的网格视图是ArrayView时，适合用作<see cref="CustomResultViewWithResize.Resizer"/>的数组调整大小函数。
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="currentView">当前视图</param>
        /// <returns>根据需要重新分配/清除的数组视图。</returns>
        public static ISettableGridView<double> ArrayViewResizer(int width, int height, ISettableGridView<double> currentView)
        {
            var current = (ArrayView<double>)currentView;

            // 无需调整整个结构的大小；只需获取内部数组并清空，以避免重新分配内存
            if (width * height == current.Count)
            {
                var newView = new ArrayView<double>(current, width);
                newView.Clear();
                return newView;
            }

            return new ArrayView<double>(width, height);
        }
    }

    /// <summary>
    /// 便于创建<see cref="ISenseMap"/>接口自定义实现的基类。
    /// </summary>
    /// <remarks>
    /// 此类实现了正确实现<see cref="ISenseMap"/>所需的大部分样板代码，确保实现者只需实现功能和属性的最小子集。
    ///
    /// 实现者应实现<see cref="OnCalculate"/>来执行所有感知源的传播计算，并将其聚合到<see cref="ResultView"/>中。
    /// 值得注意的是，实现者不应调用<see cref="Reset"/>，也不应执行任何等效的功能，并且不应触发<see cref="Recalculated"/>
    /// 或<see cref="SenseMapReset"/>事件。所有这些都由调用OnCalculate的<see cref="Calculate"/>函数处理。
    ///
    /// 实现者可以指定一个自定义网格视图作为结果使用，并且必须在构造函数中提供一个调整大小的函数。
    /// 这允许感知地图在透明度视图更改大小时调整结果视图的大小。通常，数组视图以及作为调整器的
    /// <see cref="CustomResultViewWithResize.ArrayViewResizer"/>就足够了。
    ///
    /// 最后，实现者必须实现<see cref="CurrentSenseMap"/>、<see cref="NewlyInSenseMap"/>和<see cref="NewlyOutOfSenseMap"/>
    /// 可枚举项。这允许实现者控制其跟踪方法。
    /// </remarks>
    [PublicAPI]
    public abstract class SenseMapBase : ISenseMap
    {
        /// <summary>
        /// 用于记录结果的实际网格视图。通过<see cref="ResultView"/>以只读方式公开。
        /// </summary>
        /// </summary>
        protected ISettableGridView<double> ResultViewBacking;

        /// <summary>
        /// 如果在计算调用之间阻力视图的大小发生变化，则使用此函数来调整ResultView的大小。
        /// 该函数应执行任何必要的操作，并返回一个适当大小的网格视图。
        ///
        /// 该函数必须返回一个所有值都设置为0.0的视图，该视图具有给定的宽度和高度。
        /// </summary>
        protected Func<int, int, ISettableGridView<double>, ISettableGridView<double>> ResultViewResizer;

        /// <inheritdoc/>
        public event EventHandler? Recalculated;

        /// <inheritdoc/>
        public event EventHandler? SenseMapReset;

        /// <inheritdoc/>
        public IGridView<double> ResistanceView { get; }

        /// <inheritdoc />
        public IGridView<double> ResultView => ResultViewBacking;

        private readonly List<ISenseSource> _senseSources;
        /// <inheritdoc />
        public IReadOnlyList<ISenseSource> SenseSources => _senseSources.AsReadOnly();

        /// <inheritdoc />
        public abstract IEnumerable<Point> CurrentSenseMap { get; }

        /// <inheritdoc />
        public abstract IEnumerable<Point> NewlyInSenseMap { get; }

        /// <inheritdoc />
        public abstract IEnumerable<Point> NewlyOutOfSenseMap { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="resistanceView">用于计算的阻力图。</param>
        /// <param name="resultViewAndResizer">
        /// SenseMap计算结果存储的视图，以及一个根据需要调整其大小的方法。
        ///
        /// 如果未指定，将使用ArrayView作为结果视图，并且调整大小函数将根据需要分配一个新的适当大小的ArrayView。这应该足以满足大多数用例。
        ///
        /// 此函数必须返回一个所有值都设置为0.0的视图，该视图具有给定的宽度和高度。
        /// </param>
        protected SenseMapBase(IGridView<double> resistanceView, CustomResultViewWithResize? resultViewAndResizer = null)
        {
            resultViewAndResizer ??= new CustomResultViewWithResize(
                new ArrayView<double>(resistanceView.Width, resistanceView.Height),
                CustomResultViewWithResize.ArrayViewResizer);

            ResistanceView = resistanceView;
            _senseSources = new List<ISenseSource>();
            ResultViewBacking = resultViewAndResizer.Value.ResultView;
            ResultViewResizer = resultViewAndResizer.Value.Resizer;
        }

        /// <inheritdoc />
        public IReadOnlySenseMap AsReadOnly() => this;

        /// <inheritdoc/>
        public void AddSenseSource(ISenseSource senseSource)
        {
            _senseSources.Add(senseSource);
            senseSource.SetResistanceMap(ResistanceView);
        }

        /// <inheritdoc/>
        public void RemoveSenseSource(ISenseSource senseSource)
        {
            _senseSources.Remove(senseSource);
            senseSource.SetResistanceMap(null);
        }

        /// <inheritdoc/>
        public void Calculate()
        {
            Reset();

            OnCalculate();
            Recalculated?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            if (ResistanceView.Width != ResultViewBacking.Width || ResistanceView.Height != ResultViewBacking.Height)
                ResultViewBacking = ResultViewResizer(ResistanceView.Width, ResistanceView.Height, ResultViewBacking);
            else
                ResultViewBacking.Clear();

            SenseMapReset?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 对所有源执行<see cref="ISenseSource.CalculateLight"/>，并将它们的结果聚合到<see cref="ResultViewBacking"/>中。
        /// </summary>
        /// <remarks>
        /// 自定义实现应该实现此函数以执行其计算；Calculate函数首先调用reset，然后调用此函数，自动触发相关事件。
        /// </remarks>
        protected abstract void OnCalculate();

        // ReSharper disable once MethodOverloadWithOptionalParameter
        /// <summary>
        /// 自定义用于表示地图的字符的ToString方法。
        /// </summary>
        /// <param name="normal">用于SenseMap中不存在的任何位置的字符。</param>
        /// <param name="center">
        /// 用于任何作为源中心点的位置的字符。
        /// </param>
        /// <param name="sourceValue">
        /// 用于任何在源范围内但非中心点的位置的字符。
        /// </param>
        /// <returns>使用指定字符的SenseMap的字符串表示形式。</returns>
        public string ToString(char normal = '-', char center = 'C', char sourceValue = 'S')
        {
            var result = new StringBuilder();

            for (var y = 0; y < ResistanceView.Height; y++)
            {
                for (var x = 0; x < ResistanceView.Width; x++)
                {
                    if (ResultView[x, y] > 0.0)
                        result.Append(IsACenter(x, y) ? center : sourceValue);
                    else
                        result.Append(normal);

                    result.Append(' ');
                }

                result.Append('\n');
            }

            return result.ToString();
        }

        /// <summary>
        /// 返回一个字符串表示形式的地图，其中SenseMap中不存在的任何位置都由'-'字符表示，
        /// 任何作为某些源中心的位置都由'C'字符表示，任何具有非零值但不是中心的位置都由'S'表示。
        /// </summary>
        /// <returns>SenseMap的多行字符串表示形式。</returns>
        public override string ToString() => ToString();

        /// <summary>
        /// 返回一个字符串表示形式的地图，其中包含SenseMap中的实际值，这些值四舍五入到给定的小数位数。
        /// </summary>
        /// <param name="decimalPlaces">要四舍五入到的小数位数。</param>
        /// <returns>
        /// 地图的字符串表示形式，四舍五入到给定的小数位数。
        /// </returns>
        public string ToString(int decimalPlaces)
            => ResultView.ExtendToString(elementStringifier: obj
                => obj.ToString("0." + "0".Multiply(decimalPlaces)));

        private bool IsACenter(int x, int y)
        {
            foreach (var source in _senseSources)
                if (source.Position.X == x && source.Position.Y == y)
                    return true;

            return false;
        }

    }
}
