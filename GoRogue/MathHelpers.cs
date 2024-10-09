using System;
using JetBrains.Annotations;

namespace GoRogue
{
    /// <summary>
    /// 由数学“辅助”函数和常量组成的静态类——比如角度单位转换和其他有用的函数。
    /// </summary>
    [PublicAPI]
    public static class MathHelpers
    {
        /// <summary>
        /// 将给定数字向上取整（朝向最高数字）到最接近指定值的倍数。
        /// </summary>
        /// <param name="number">要取整的数字。</param>
        /// <param name="toMultipleOf">给定数字将向上取整到此数字的最接近倍数。</param>
        /// <returns>数字参数，向上取整到最接近的<paramref name="toMultipleOf"/>的倍数。</returns>
        public static int RoundToMultiple(int number, int toMultipleOf)
        {
            var isPositive = number >= 0 ? 1 : 0;
            return (number + isPositive * (toMultipleOf - 1)) / toMultipleOf * toMultipleOf;
        }

        // 基本上是对数组索引取模，解决了负数问题。例如，(-1, 3) 的结果是 2。
        /// <summary>
        /// 一个修改过的取模运算符，与 <paramref name="num" /> % <paramref name="wrapTo" /> 的实际区别在于，
        /// 它可以从 0 包裹到 <paramref name="wrapTo" /> - 1，也可以从 <paramref name="wrapTo" /> - 1 包裹到 0。
        /// </summary>
        /// <remarks>
        /// 一个修改过的取模运算符。返回公式 (<paramref name="num" /> % <paramref name="wrapTo" /> + <paramref name="wrapTo" />) % <paramref name="wrapTo" /> 的结果。
        /// 它与常规取模的实际区别在于，当 <paramref name="num" /> 为负数时，它返回的值会像你希望的数组索引那样进行包裹
        /// （如果 wrapTo 是 list.length，那么 -1 会包裹到 list.length - 1）。例如，
        /// 0 % 3 = 0, -1 % 3 = -1, -2 % 3 = -2, -3 % 3 = 0，以此类推，但是 WrapTo(0, 3) = 0，
        /// WrapTo(-1, 3) = 2, WrapTo(-2, 3) = 1, WrapTo(-3, 3) = 0，以此类推。如果你试图在两端都“包裹”一个数字，
        /// 例如包裹到 3，使得 3 包裹到 0，-1 包裹到 2，这会很有用。如果你正在将数组索引包裹到数组长度，
        /// 并且需要确保大于或等于数组长度的正数包裹到数组的开头（索引 0），并且负数（小于 0）包裹到数组的末尾（Length - 1），
        /// 那么这是很常见的。
        /// </remarks>
        /// <param name="num">要包裹的数字。</param>
        /// <param name="wrapTo">
        /// 要包裹到的数字——函数的结果如函数描述中所述，并保证在 [0, wrapTo - 1] 范围内（包含）。
        /// </param>
        /// <returns>
        /// 如函数描述中所述的包裹结果。保证在范围 [0, wrapTo - 1] 内（包含）。
        /// </returns>
        public static int WrapAround(int num, int wrapTo) => (num % wrapTo + wrapTo) % wrapTo;

        /// <summary>
        /// 与 <see cref="WrapAround(int,int)"/> 的效果相同，但针对的是双精度浮点数。
        /// </summary>
        /// <param name="num">要包裹的数字。</param>
        /// <param name="wrapTo">要包裹到的数字。</param>
        /// <returns>包裹后的结果。保证在范围 [0, wrapTo) 内。</returns>
        public static double WrapAround(double num, double wrapTo)
        {
            // 同样的取模运算也有效，但更不容易产生舍入误差
            while (num < 0)
                num += wrapTo;
            while (num >= wrapTo)
                num -= wrapTo;

            return num;
        }

        /// <summary>
        /// Atan2函数的近似值，该函数将返回的值缩放到[0.0, 1.0]范围内，以保持对单位（半径与度数）的不可知性。它永远不会返回负数，
        /// 因此也有助于避免浮点取模运算。感谢SquidLib java RL库以及用户njuffa在
        /// <a href="https://math.stackexchange.com/a/1105038">此处</a>的建议。
        /// </summary>
        /// <param name="y">要查找其朝向角度的点的Y分量。</param>
        /// <param name="x">要查找其朝向角度的点的X分量。</param>
        /// <returns>表示到给定点的角度的值，已缩放到[0.0, 1.0]范围。</returns>
        public static double ScaledAtan2Approx(double y, double x)
        {
            if (Math.Abs(y) < 0.0000000001 && x >= 0.0)
                return 0.0;

            var ax = Math.Abs(x);
            var ay = Math.Abs(y);

            if (ax < ay)
            {
                var a = ax / ay;
                var s = a * a;
                var r = 0.25 - (((-0.0464964749 * s + 0.15931422) * s - 0.327622764) * s * a + a) * 0.15915494309189535;
                return x < 0.0 ? y < 0.0 ? 0.5 + r : 0.5 - r : y < 0.0 ? 1.0 - r : r;
            }
            else
            {
                var a = ay / ax;
                var s = a * a;
                var r = (((-0.0464964749 * s + 0.15931422) * s - 0.327622764) * s * a + a) * 0.15915494309189535;
                return x < 0.0 ? y < 0.0 ? 0.5 + r : 0.5 - r : y < 0.0 ? 1.0 - r : r;
            }
        }
    }
}
