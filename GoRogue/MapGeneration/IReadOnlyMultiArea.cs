using System.Collections.Generic;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// 一个扩展自IReadOnlyArea的接口，旨在表示由多个其他区域组成的区域。
    /// 公开一个<see cref="SubAreas"/>字段，该字段列出了组成区域。
    /// </summary>
    public interface IReadOnlyMultiArea : IReadOnlyArea
    {
        /// <summary>
        /// MultiArea中的所有子区域的列表。
        /// </summary>
        IReadOnlyList<IReadOnlyArea> SubAreas { get; }
    }
}
