using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 当达到地图生成的最大重试次数时抛出的异常。
    /// </summary>
    [PublicAPI]
    public class MapGenerationFailedException : Exception
    {
        /// <summary>
        /// 创建没有消息的地图生成失败异常。
        /// </summary>
        public MapGenerationFailedException()
        { }

        /// <summary>
        /// 使用自定义消息创建地图生成失败异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        public MapGenerationFailedException(string message)
            : base(message)
        { }

        /// <summary>
        /// 使用自定义消息和内部异常创建地图生成失败异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        /// <param name="innerException">内部异常。</param>
        public MapGenerationFailedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    /// <summary>
    /// 地图生成器，它将一系列<see cref="GenerationStep"/>实例应用于<see cref="GenerationContext"/>以生成地图。
    /// </summary>
    [PublicAPI]
    public class Generator
    {
        private readonly List<GenerationStep> _generationSteps;

        /// <summary>
        /// 此<see cref="Generator"/>正在生成的地图的上下文。
        /// </summary>
        public readonly GenerationContext Context;

        /// <summary>
        /// 创建一个生成器，该生成器将用于生成给定宽/高的地图。
        /// </summary>
        /// <param name="width">生成的地图的宽度。</param>
        /// <param name="height">生成的地图的高度。</param>
        public Generator(int width, int height)
        {
            Context = new GenerationContext(width, height);
            _generationSteps = new List<GenerationStep>();
        }

        /// <summary>
        /// 用于生成地图的步骤。
        /// </summary>
        public IReadOnlyList<GenerationStep> GenerationSteps => _generationSteps.AsReadOnly();

        /// <summary>
        /// 向此生成器应用生成步骤的上下文中添加一个组件。
        /// </summary>
        /// <param name="component">要添加到地图上下文的组件。</param>
        /// <param name="tag">要给组件的可选标签。默认为无标签。</param>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator AddComponent(object component, string? tag = null)
        {
            Context.Add(component, tag);
            return this;
        }

        /// <summary>
        /// 添加一个生成步骤。步骤将按照添加的顺序执行。
        /// </summary>
        /// <param name="step">要添加的生成步骤。</param>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator AddStep(GenerationStep step)
        {
            _generationSteps.Add(step);
            return this;
        }

        /// <summary>
        /// 添加给定的生成步骤。步骤将按照添加的顺序执行。
        /// </summary>
        /// <param name="steps">要添加的生成步骤。</param>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator AddSteps(params GenerationStep[] steps) => AddSteps((IEnumerable<GenerationStep>)steps);

        /// <summary>
        /// 添加给定的生成步骤。步骤将按照添加的顺序执行。
        /// </summary>
        /// <param name="steps">要添加的生成步骤。</param>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator AddSteps(IEnumerable<GenerationStep> steps)
        {
            _generationSteps.AddRange(steps);
            return this;
        }

        /// <summary>
        /// 清除上下文和生成步骤，将生成器重置回预先配置的状态。
        /// </summary>
        public void Clear()
        {
            _generationSteps.Clear();
            Context.Clear();
        }

        /// <summary>
        /// 按照添加的顺序应用已添加的生成步骤到<see cref="Context"/>以生成地图。如果你想要自动处理<see cref="RegenerateMapException"/>，请调用
        /// <see cref="ConfigAndGenerateSafe"/>。
        /// </summary>
        /// <remarks>
        /// 根据所使用的生成步骤，如果此函数检测到由于RNG（随机数生成器）导致生成的地图不满足不变量，它可能会抛出RegenerateMapException，
        /// 在这种情况下，将需要再次执行地图生成。请参阅<see cref="ConfigAndGenerateSafe"/>，了解一种确保以方便的方式实现这一点的方法。
        /// </remarks>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator Generate()
        {
            foreach (var step in _generationSteps)
                step.PerformStep(Context);

            return this;
        }

        /// <summary>
        /// 调用<paramref name="generatorConfig"/>函数以向生成器中添加组件/步骤，然后调用<see cref="Generate"/>。
        /// 如果抛出<see cref="RegenerateMapException"/>，则通过再次调用配置函数然后重新生成，直到达到指定的最大重试次数来重新生成地图。
        /// </summary>
        /// <remarks>
        /// 这是一个安全的包装器，用于处理可能使自身陷入无效状态并需要重新生成整个地图的生成过程。
        /// 如果生成步骤能产生这样的状态，那么文档中会有明确的标记。
        ///
        /// 确保不要在此函数内创建/使用具有静态种子的RNG（随机数生成器），因为它很容易创建一个无限循环（即反复重新生成相同的无效地图）。
        /// </remarks>
        /// <param name="generatorConfig">用于配置生成器的函数。</param>
        /// <param name="maxAttempts">在抛出MapGenerationFailed异常之前，尝试生成地图的最大次数。默认为无限次。</param>
        /// <returns>此生成器（用于链式调用）。</returns>
        public Generator ConfigAndGenerateSafe(Action<Generator> generatorConfig, int maxAttempts = -1)
        {
            int currentAttempts = 0;
            while (maxAttempts == -1 || currentAttempts < maxAttempts)
            {
                try
                {
                    Clear();
                    generatorConfig(this);
                    Generate();
                    break;
                }
                catch (RegenerateMapException)
                { }

                currentAttempts++;
            }

            if (currentAttempts == maxAttempts)
                throw new MapGenerationFailedException("Maximum retries for regenerating map exceeded.");

            return this;
        }

        /// <summary>
        /// 返回一个枚举器，当评估完成时，它会按顺序执行每个阶段（由生成步骤在其实现中定义）；每次MoveNext调用执行一个阶段。
        /// 通常，您会希望使用<see cref="Generate"/>代替。如果您想自动处理<see cref="RegenerateMapException"/>，
        /// 请根据适用情况调用<see cref="ConfigAndGetStageEnumeratorSafe"/>或<see cref="ConfigAndGenerateSafe"/>。
        /// </summary>
        /// <remarks>
        /// 对于传统情况，您将希望调用<see cref="Generate"/>函数，它简单地完成所有步骤。但是，如果您想直观地检查生成算法的每个阶段，
        /// 您可以调用此函数，然后每次想要完成一个阶段时，调用返回的枚举器的MoveNext函数。这对于演示目的和调试可能很有用。
        ///
        /// 请注意，在此迭代期间可能会引发<see cref="RegenerateMapException"/>，必须手动处理它。
        /// 请参阅<see cref="ConfigAndGetStageEnumeratorSafe"/>以了解自动处理此问题的方法。
        /// </remarks>
        /// <returns>
        /// 一个枚举器，每次调用其MoveNext函数时，都会完成一个生成步骤的阶段。
        /// </returns>
        public IEnumerator<object?> GetStageEnumerator()
        {
            foreach (var step in _generationSteps)
            {
                var stepEnumerator = step.GetStageEnumerator(Context);
                bool hasNext;
                do
                {
                    hasNext = stepEnumerator.MoveNext();
                    if (hasNext) // If we're past the end, we don't want to introduce an additional stopping point
                        yield return null;
                } while (hasNext);
            }
        }

        /// <summary>
        /// 调用<paramref name="generatorConfig"/>函数以向生成器添加组件/步骤，然后调用<see cref="GetStageEnumerator"/>并评估其枚举器，在每个步骤中返回。
        /// 如果抛出<see cref="RegenerateMapException"/>，则自动重新开始地图生成。通常，您会希望使用<see cref="ConfigAndGenerateSafe"/>代替。
        /// </summary>
        /// <remarks>
        /// 对于传统情况，您将希望调用<see cref="ConfigAndGenerateSafe"/>函数，该函数采用相同的参数并简单地完成所有步骤。
        /// 但是，如果您想直观地检查生成算法的每个阶段，可以调用此函数，然后每次想要完成一个阶段时，调用返回的枚举器的MoveNext函数。
        /// 这对于演示目的和调试可能很有用。
        /// </remarks>
        /// <param name="generatorConfig">用于配置生成器的函数。</param>
        /// <param name="maxAttempts">在抛出MapGenerationFailed异常之前，尝试地图生成的最大次数。默认为无限次。</param>
        /// <returns>
        /// 一个枚举器，每次调用其MoveNext函数时，都会完成一个生成步骤的阶段。
        /// </returns>
        public IEnumerator<object?> ConfigAndGetStageEnumeratorSafe(Action<Generator> generatorConfig,
                                                                    int maxAttempts = -1)
        {
            int currentAttempts = 0;
            while (maxAttempts == -1 || currentAttempts < maxAttempts)
            {
                // Break for step after reset if we're in the middle of an iteration
                if (Context.Count != 0 || _generationSteps.Count != 0)
                {
                    Clear();
                    generatorConfig(this);
                    yield return null;
                }
                else // Otherwise, just config (nothing to clear)
                    generatorConfig(this);

                // Enumerate, but catch exception and restart if we hit RegenerateMapException
                var stepEnumerator = GetStageEnumerator();
                bool hasNext;
                bool regenMap = false;
                do
                {
                    try
                    {
                        hasNext = stepEnumerator.MoveNext();
                    }
                    catch (RegenerateMapException)
                    {
                        regenMap = true;
                        break;
                    }

                    // If we're past the end, we don't want to introduce an additional stopping point
                    if (hasNext)
                        yield return null;

                } while (hasNext);

                // If we succeeded, just return out.  Otherwise, continue loop.  No pause is needed, as the pause occurs
                // after clear, or automatically at end of function..
                if (!regenMap)
                    break;

                currentAttempts++;
            }

            if (currentAttempts == maxAttempts)
                throw new MapGenerationFailedException("Maximum retries for regenerating map exceeded.");
        }
    }
}
