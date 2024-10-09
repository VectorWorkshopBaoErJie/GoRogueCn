using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue
{

    /// <summary>
    /// 提供各种有用的辅助函数的实现，这些函数用于从行中收集信息。
    /// </summary>
    [PublicAPI]
    public static class LineHelpers
    {
        #region IEnumerable<Point> Extensions

        /// <summary>
        /// 获取给定y值上最左侧的点。
        /// </summary>
        /// <param name="self"/>
        /// <param name="y">要在其上找到最左侧点的y值。</param>
        /// <returns/>
        public static int LeftAt(this IEnumerable<Point> self, int y) => self.Where(c => c.Y == y).OrderBy(c => c.X).First().X;

        /// <summary>
        /// 获取给定 y 值上最右侧的点。
        /// </summary>
        /// <param name="self"/>
        /// <param name="y">要在其上找到最右侧点的 y 值。</param>
        /// <returns/>
        public static int RightAt(this IEnumerable<Point> self, int y) => self.Where(c => c.Y == y).OrderBy(c => -c.X).First().X;

        /// <summary>
        /// 获取给定x值上最顶部的点。
        /// </summary>
        /// <param name="self"/>
        /// <param name="x">要在其上找到最顶部点的x值。</param>
        /// <returns/>
        public static int TopAt(this IEnumerable<Point> self, int x) => Direction.YIncreasesUpward
            ? self.Where(c => c.X == x).OrderBy(c => -c.Y).First().Y
            : self.Where(c => c.X == x).OrderBy(c => c.Y).First().Y;

        /// <summary>
        /// 在给定的x值上，获取列表中最顶部的点。
        /// </summary>
        /// <param name="self">当前对象实例。</param>
        /// <param name="x">要在其上查找最顶部点的x值。</param>
        /// <returns>返回在给定的x值上找到的最顶部的点。</returns>
        public static int BottomAt(this IEnumerable<Point> self, int x) => Direction.YIncreasesUpward
            ? self.Where(c => c.X == x).OrderBy(c => c.Y).First().Y
            : self.Where(c => c.X == x).OrderBy(c => -c.Y).First().Y;

        #endregion
    }
}
