using System;
using System.Collections.Generic;
using System.Linq;
using GoRogue.Components;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 当参数配置错误时，由<see cref="GenerationStep.OnPerform(GenerationContext)" />中的生成步骤引发。
    /// </summary>
    [PublicAPI]
    public class InvalidConfigurationException : Exception
    {
        /// <summary>
        /// 创建一个带有自定义消息的配置异常。
        /// </summary>
        /// <param name="message">自定义的异常消息。</param>
        public InvalidConfigurationException(string message)
            : base(message)
        { }

        /// <summary>
        /// 创建一个带有自定义消息和内部异常的配置异常。
        /// </summary>
        /// <param name="message">自定义的异常消息。</param>
        /// <param name="innerException">内部异常。</param>
        public InvalidConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// 创建一个带有有用消息的配置异常。
        /// </summary>
        /// <param name="step">遇到配置错误参数的生成步骤。</param>
        /// <param name="parameterName">配置错误的参数名称。</param>
        /// <param name="message">解释参数值要求的消息。</param>
        public InvalidConfigurationException(GenerationStep step, string parameterName, string message)
            : base("Invalid configuration encountered for generation step parameter:\n" +
                   $"    Generation Step: ${step.GetType().Name} (name: {step.Name})\n" +
                   $"    Parameter Name : ${parameterName}\n" +
                   $"    Message        : ${message}")
        {
            ParameterName = parameterName;
            Step = step;
        }

        /// <summary>
        /// 创建一个空的配置异常。
        /// </summary>
        public InvalidConfigurationException()
        { }
        /// <summary>
        /// 配置错误的参数的名称。
        /// </summary>
        public string? ParameterName { get; }

        /// <summary>
        /// 包含配置错误参数的生成步骤。
        /// </summary>
        public GenerationStep? Step { get; }
    }

    /// <summary>
    /// 当调用<see cref="GenerationStep.PerformStep(GenerationContext)" />方法时，如果所需组件不存在，则由<see cref="GenerationStep" />引发。
    /// </summary>
    [PublicAPI]
    public class MissingContextComponentException : Exception
    {
        /// <summary>
        /// 未找到所需组件的标签，若不需要标签则为null。
        /// </summary>
        public readonly string? RequiredComponentTag;

        /// <summary>
        /// 未找到的所需组件的类型。
        /// </summary>
        public readonly Type? RequiredComponentType;

        /// <summary>
        /// 未能找到其所需组件的生成步骤。
        /// </summary>
        public readonly GenerationStep? Step;

        /// <summary>
        /// 创建一个带有有用错误消息的新异常。
        /// </summary>
        /// <param name="step">未能找到其所需组件的生成步骤。</param>
        /// <param name="requiredComponentType">未找到的所需组件的类型。</param>
        /// <param name="requiredComponentTag">未找到所需组件的标签，若不需要标签则为null。</param>
        public MissingContextComponentException(GenerationStep step, Type requiredComponentType,
                                                string? requiredComponentTag)
            : base("Generation step was performed on a context that did not have the required components:\n" +
                   $"    Generation Step   : {step.GetType().Name} (name: {step.Name})\n" +
                   $"    Required Component: {requiredComponentType.Name} " +
                   (requiredComponentTag != null ? $"(tag: {requiredComponentTag})" : "") + "\n")
        {
            Step = step;
            RequiredComponentType = requiredComponentType;
            RequiredComponentTag = requiredComponentTag;
        }

        /// <summary>
        /// 创建一个带有完全自定义消息的异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        public MissingContextComponentException(string message)
            : base(message)
        { }

        /// <summary>
        /// 创建一个带有完全自定义消息和内部异常的异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        /// <param name="innerException">内部异常。</param>
        public MissingContextComponentException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// 创建一个空的异常。
        /// </summary>
        public MissingContextComponentException()
        { }
    }

    /// <summary>
    /// 当地图生成步骤检测到无效状态（偶尔会发生）且应重新生成地图时引发。由<see cref="Generator.ConfigAndGenerateSafe"/>和
    /// <see cref="Generator.ConfigAndGetStageEnumeratorSafe"/>函数自动使用。
    /// </summary>
    [PublicAPI]
    public class RegenerateMapException : Exception
    {
        /// <summary>
        /// 创建没有消息的地图再生异常。
        /// </summary>
        public RegenerateMapException()
        { }

        /// <summary>
        /// 创建带有自定义消息的地图再生异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        public RegenerateMapException(string message)
            : base(message + "  This exception is expected for some generation steps; consider using ConfigAndGenerateSafe to automatically handle it.")
        { }

        /// <summary>
        /// 创建带有自定义消息和内部异常的地图再生异常。
        /// </summary>
        /// <param name="message">自定义消息。</param>
        /// <param name="innerException">内部异常。</param> 
        public RegenerateMapException(string message, Exception innerException)
            : base(message + "  This exception is expected for some generation steps; consider using ConfigAndGenerateSafe to automatically handle it.", innerException)
        { }
    }

    // TODO: 找出检查具有相同标签和类型的方法（针对某些生成步骤）？这会破坏ClosestMapAreaConnector
    /// <summary>
    /// 用于实现自定义地图生成步骤的基类。
    /// </summary>
    [PublicAPI]
    public abstract class GenerationStep
    {
        private readonly ComponentTypeTagPair[] _requiredComponents;

        /// <summary>
        /// 生成步骤的名称。
        /// </summary>
        public readonly string Name;

        // 此构造函数是必需的，以消除因其他两个构造函数都使用params而产生的构造函数调用不明确的问题
        /// <summary>
        /// 创建一个生成步骤，可选地带有自定义名称。
        /// </summary>
        /// <param name="name">正在创建的生成步骤的名称。默认为（运行时）类的名称。</param>
        protected GenerationStep(string? name = null)
            : this(name, Array.Empty<Type>())
        { }

        /// <summary>
        /// 创建一个生成步骤，该步骤需要在<see cref="GenerationContext" />上具有给定的组件才能运行。
        /// </summary>
        /// <param name="name">
        /// 正在创建的生成步骤的名称。默认为（运行时）类的名称。
        /// </param>
        /// <param name="requiredComponents">
        /// <see cref="OnPerform(GenerationContext)" />需要从上下文中获取的组件（和关联标签）。
        /// 指定为null的标签意味着不需要特定标签；只需要给定类型的组件。
        /// </param>
        protected GenerationStep(string? name = null, params Type[] requiredComponents)
            : this(name, requiredComponents.Select(type => new ComponentTypeTagPair(type, null)).ToArray())
        { }

        /// <summary>
        /// 创建一个生成步骤，该步骤需要<see cref="GenerationContext" />上的给定组件才能运行。
        /// </summary>
        /// <param name="requiredComponents">
        /// <see cref="OnPerform(GenerationContext)" />将从上下文中需要的组件，以及每个组件所需的标签。
        /// Null表示不需要特定标签。
        /// </param>
        /// <param name="name">正在创建的生成步骤的名称。默认为（运行时）类的名称。</param>
        protected GenerationStep(string? name = null, params ComponentTypeTagPair[] requiredComponents)
        {
            Name = name ?? GetType().Name;
            _requiredComponents = requiredComponents;
        }

        /// <summary>
        /// 当传递给<see cref="OnPerform(GenerationContext)" />时，<see cref="GenerationContext" />上必须存在且强制要求的组件。
        /// 每个组件可以有一个可选的必需标签。
        /// </summary>
        public IEnumerable<ComponentTypeTagPair> RequiredComponents => _requiredComponents;

        /// <summary>
        /// 在给定的地图上下文中执行生成步骤。如果缺少必需的组件，将抛出异常。
        /// 此函数不是虚拟的--要实现实际的生成逻辑，请实现<see cref="OnPerform(GenerationContext)" />。
        /// </summary>
        /// <param name="context">执行生成步骤的上下文。</param>
        public void PerformStep(GenerationContext context)
        {
            // Ensure required components exist
            CheckForRequiredComponents(context);

            // Evaluate entire enumerator to complete the entire step
            var enumerator = OnPerform(context);
            bool isNext;
            do
            {
                isNext = enumerator.MoveNext();
            } while (isNext);
        }

        /// <summary>
        /// 返回一个枚举器，当评估完成时，它将按顺序执行生成步骤的每个“阶段”。
        /// </summary>
        /// <param name="context">执行生成步骤的上下文。</param>
        /// <returns>一个枚举器，当评估时，它将按顺序执行步骤的每个阶段。</returns>
        public IEnumerator<object?> GetStageEnumerator(GenerationContext context)
        {
            // 检查必需的组件
            CheckForRequiredComponents(context);

            // 返回枚举器，当评估时，它将执行步骤的每个阶段。
            return OnPerform(context);
        }

        /// <summary>
        /// 实现以执行生成步骤的实际工作。使用“yield return null”来表示一个“阶段”的结束，
        /// 例如，在使用<see cref="Generator.GetStageEnumerator"/>时执行可以暂停的点。
        /// </summary>
        /// <param name="context">执行生成步骤的上下文。</param>
        protected abstract IEnumerator<object?> OnPerform(GenerationContext context);

        private void CheckForRequiredComponents(GenerationContext context)
        {
            foreach (var (componentType, tag) in _requiredComponents)
                if (!context.Contains(componentType, tag))
                    throw new MissingContextComponentException(this, componentType, tag);
        }
    }
}
