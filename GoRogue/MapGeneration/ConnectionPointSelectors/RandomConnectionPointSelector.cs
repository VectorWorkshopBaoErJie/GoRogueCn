using GoRogue.Random;
using JetBrains.Annotations;
using SadRogue.Primitives;
using ShaiRandom.Generators;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// 实现了一个选择算法，该算法简单地从给定区域的位置列表中随机选择点，使用指定的随机数生成器（RNG），如果给定null，则使用默认随机数生成器。
    /// </summary>
    [PublicAPI]
    public class RandomConnectionPointSelector : IConnectionPointSelector
    {
        private readonly IEnhancedRandom _rng;

        /// <summary>
        /// 构造函数。指定要使用的随机数生成器（RNG），如果要使用默认RNG，则为null。
        /// </summary>
        /// <param name="rng">要使用的随机数生成器（RNG），如果要使用默认RNG，则为null。</param>
        public RandomConnectionPointSelector(IEnhancedRandom? rng = null) => _rng = rng ?? GlobalRandom.DefaultRNG;

        /// <inheritdoc />
        public AreaConnectionPointPair SelectConnectionPoints(IReadOnlyArea area1, IReadOnlyArea area2)
            => new AreaConnectionPointPair(_rng.RandomElement(area1), _rng.RandomElement(area2));
    }
}
