using System;
using GoRogue.Components;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 用于地图生成的上下文对象。地图生成步骤将需要并检索已添加到此上下文中的组件，
    /// 当它们需要检索由先前步骤生成的地图的数据时。
    /// </summary>
    [PublicAPI]
    public class GenerationContext : ComponentCollection
    {
        /// <summary>
        /// 此上下文表示的地图的高度。
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// 此上下文表示的地图的宽度。
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// 使用给定的宽度/高度值创建一个没有组件的地图上下文。
        /// </summary>
        /// <param name="width">此上下文表示的地图的宽度。</param>
        /// <param name="height">此上下文表示的地图的高度。</param>
        public GenerationContext(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentException("Width for generation context must be greater than 0.", nameof(width));

            if (height <= 0)
                throw new ArgumentException("Height for generation context must be greater than 0.", nameof(height));

            Width = width;
            Height = height;
        }

        /// <summary>
        /// 检索上下文组件（可选地带有给定标签），或者如果没有现有组件，则使用指定的函数创建一个新组件并将其添加。
        /// </summary>
        /// <typeparam name="TComponent">要检索的组件的类型。</typeparam>
        /// <param name="newFunc">如果没有现有组件，则用于创建新组件的函数。</param>
        /// <param name="tag">
        /// 一个可选标签，必须与检索或创建的组件相关联。如果指定为null，则新对象不会关联任何标签，
        /// 并且任何满足类型要求的对象都将被允许作为返回值。
        /// </param>
        /// <returns>
        /// 如果存在，则返回适当类型的现有组件；如果不存在，则返回新创建/添加的组件。
        /// </returns>
        public TComponent GetFirstOrNew<TComponent>([InstantHandle] Func<TComponent> newFunc, string? tag = null)
            where TComponent : class
        {
            var contextComponent = GetFirstOrDefault<TComponent>(tag);
            if (contextComponent != null)
                return contextComponent;

            contextComponent = newFunc();
            Add(contextComponent, tag);

            return contextComponent;
        }
    }
}
