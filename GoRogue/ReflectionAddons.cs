using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace GoRogue
{
    /// <summary>
    /// 包含一系列函数，这些函数补充了 C# 的反射功能，从而可以方便地实现诸如遍历给定类型可以转换成的所有类型等操作。
    /// </summary>
    [PublicAPI]
    public static class ReflectionAddons
    {
        /// <summary>
        /// 获取传入对象的实际运行时类型的完整继承/接口树。这将包括表示对象的实际运行时类型的类型，
        /// 表示该运行时类型的每个超类的类型，以及表示该运行时类型或其超类实现的每个接口的类型。
        /// </summary>
        /// <param name="instance">要返回其类型树的对象。</param>
        /// <returns>
        /// 传入对象的运行时类型的完整继承/接口树，包括运行时类型本身、该类型的所有超类，
        /// 以及表示运行时类型或其超类实现的每个接口的Type对象。
        /// </returns>
        public static IEnumerable<Type> GetRuntimeTypeTree(object instance) => GetTypeTree(instance.GetType());

        /// <summary>
        /// 获取类型 T 的完整继承/接口树。这将包括表示类型 T 的 Type，以及表示 T 的每个超类的 Type，
        /// 以及T或其超类实现的每个接口的 Type。
        /// </summary>
        /// <remarks>
        /// 这个函数的计算可能有些昂贵，所以如果你打算频繁使用它，建议缓存结果。
        /// </remarks>
        /// <typeparam name="T">要获取其继承/接口树的类型。</typeparam>
        /// <returns>
        /// T的完整接口/继承树，包括T、所有超类，以及T或其超类实现的所有接口。
        /// </returns>
        public static IEnumerable<Type> GetTypeTree<T>() => GetTypeTree(typeof(T));

        /// <summary>
        /// 获取指定类型的完整继承/接口树。这将包括<paramref name="type"/>本身，
        /// 以及表示<paramref name="type"/>所代表类型的每个超类的Type，和<paramref name="type"/>或其超类实现的每个接口的Type。
        /// </summary>
        /// <remarks>
        /// 这个函数的计算可能有些昂贵，所以如果你打算频繁使用它，建议缓存结果。
        /// </remarks>
        /// <returns>
        /// 由<paramref name="type"/>表示的类型的完整接口/继承，包括<paramref name="type"/>本身、
        /// 所有超类，以及T或其超类实现的所有接口。
        /// </returns>
        public static IEnumerable<Type> GetTypeTree(Type type)
        {
            var currentType = type;
            while (currentType != null)
            {
                yield return currentType;
                currentType = currentType.BaseType;
            }

            foreach (Type implementedInterface in type.GetInterfaces())
                yield return implementedInterface;
        }
    }
}
