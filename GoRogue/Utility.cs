using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;

namespace GoRogue
{
    /// <summary>
    /// 包含各种内置 C# 类的扩展帮助方法的静态类，以及一个用于"交换"引用的静态帮助方法。
    /// </summary>
    [PublicAPI]
    public static class Utility
    {
        /// <summary>
        /// 向<see cref="IDictionary{K, V}" />添加一个 AsReadOnly 方法，类似于<see cref="IList{T}" />的 AsReadOnly 方法，
        /// 该方法返回一个对字典的只读引用。
        /// </summary>
        /// <typeparam name="TKey">字典键的类型。</typeparam>
        /// <typeparam name="TValue">字典值的类型。</typeparam>
        /// <param name="dictionary">要操作的字典。</param>
        /// <returns>为指定字典返回的ReadOnlyDictionary实例。</returns>
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary)
            where TKey : notnull
            => new ReadOnlyDictionary<TKey, TValue>(dictionary);

        /// <summary>
        /// 相乘是指字符串按照指定的次数重复。
        /// </summary>
        /// <param name="str">要重复的字符串。</param>
        /// <param name="numTimes">重复字符串的次数。</param>
        /// <returns>当前字符串重复<paramref name="numTimes" />次的结果。</returns>
        public static string Multiply(this string str, int numTimes) => string.Concat(Enumerable.Repeat(str, numTimes));

        /// <summary>
        /// 交换 <paramref name="lhs" /> 和 <paramref name="rhs" />.
        /// </summary>
        /// <typeparam name="T" />
        /// <param name="lhs" />
        /// <param name="rhs" />
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            (lhs, rhs) = (rhs, lhs);
        }

        /// <summary>
        /// 这是一个快捷函数，它将给定的项作为单个的项目 IEnumerable 生成。
        /// </summary>
        /// <typeparam name="T" />
        /// <param name="item" />
        /// <returns>一个仅包含该函数调用条目的 IEnumerable 。</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// 接收多个参数并将它们转换为 IEnumerable。
        /// </summary>
        /// <typeparam name="T">类型参数。</typeparam>
        /// <param name="values">参数（作为函数的多个参数指定）。</param>
        /// <returns>
        /// 一个包含所有给定项的 IEnumerable，按它们传递给函数的顺序排列。
        /// </returns>
        public static IEnumerable<T> Yield<T>(params T[] values)
        {
            foreach (var value in values)
                yield return value;
        }

        /// <summary>
        /// 接收多个可枚举的项目集合，并将它们扁平化为一个单一的 IEnumerable 。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="lists">要“扁平化”的列表。</param>
        /// <returns>一个包含所有传入的可枚举集合中的项目的 IEnumerable 。</returns>
        public static IEnumerable<T> Flatten<T>(params IEnumerable<T>[] lists)
        {
            foreach (var list in lists)
                foreach (var i in list)
                    yield return i;
        }
    }
}
