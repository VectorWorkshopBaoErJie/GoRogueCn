using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration.ContextComponents
{
    /// <summary>
    /// 一个由地图生成步骤添加的项目的通用列表，用于跟踪哪个生成步骤添加了哪个项目。
    /// </summary>
    /// <typeparam name="TItem">正在存储的项目的类型。</typeparam>
    [PublicAPI]
    [DataContract]
    public class ItemList<TItem> : IEnumerable<ItemStepPair<TItem>>
        where TItem : notnull
    {
        private readonly List<TItem> _items;

        private readonly Dictionary<TItem, string> _itemToStepMapping;

        /// <summary>
        /// 创建一个空的项列表。
        /// </summary>
        public ItemList()
        {
            _items = new List<TItem>();
            _itemToStepMapping = new Dictionary<TItem, string>();
        }

        /// <summary>
        /// 创建一个新的项列表，并向其中添加给定的项。
        /// </summary>
        /// <param name="initialItems">要添加到列表中的初始项/步骤对。</param>
        public ItemList(IEnumerable<ItemStepPair<TItem>> initialItems)
            : this()
        {
            foreach (var (item, step) in initialItems)
                Add(item, step);
        }

        /// <summary>
        /// 使用指定的初始项容量创建一个空的项列表。
        /// </summary>
        /// <param name="initialItemCapacity">项的初始容量。</param>
        public ItemList(int initialItemCapacity)
        {
            _items = new List<TItem>(initialItemCapacity);
            _itemToStepMapping = new Dictionary<TItem, string>(initialItemCapacity);
        }

        /// <summary>
        /// 已添加的项列表。
        /// </summary>
        public IReadOnlyList<TItem> Items => _items;

        /// <summary>
        /// 每个项到创建该项的生成步骤的 <see cref="GenerationStep.Name" /> 的映射。
        /// </summary>
        public IReadOnlyDictionary<TItem, string> ItemToStepMapping => _itemToStepMapping.AsReadOnly();

        /// <summary>
        /// 向列表中添加一个项。
        /// </summary>
        /// <param name="item">要添加的项。</param>
        /// <param name="generationStepName">创建该项的生成步骤的 <see cref="GenerationStep.Name" />。</param>
        public void Add(TItem item, string generationStepName)
        {
            _items.Add(item);
            _itemToStepMapping.Add(item, generationStepName);
        }

        /// <summary>
        /// 将给定的项添加到列表中。
        /// </summary>
        /// <param name="items">要添加的项。</param>
        /// <param name="generationStepName">创建这些项的生成步骤的 <see cref="GenerationStep.Name" />。</param>
        public void AddRange(IEnumerable<TItem> items, string generationStepName)
        {
            foreach (var item in items)
            {
                _items.Add(item);
                _itemToStepMapping.Add(item, generationStepName);
            }
        }

        /// <summary>
        /// 从列表中移除给定的项。
        /// </summary>
        /// <param name="item">要移除的项。</param>
        public void Remove(TItem item) => Remove(item.Yield());

        /// <summary>
        /// 从列表中移除给定的多个项。
        /// </summary>
        /// <param name="items">要移除的项集合。</param>
        public void Remove(IEnumerable<TItem> items)
        {
            foreach (var item in items)
            {
                if (!_itemToStepMapping.ContainsKey(item))
                    throw new ArgumentException(
                        $"Tried to remove a value from an {nameof(ItemList<TItem>)} that was not present.");

                _items.Remove(item);
                _itemToStepMapping.Remove(item);
            }
        }

        /// <summary>
        /// 移除列表中所有使给定函数返回 true 的项。
        /// </summary>
        /// <param name="predicate">用于确定要移除哪些元素的谓词。</param>
        public void Remove(Func<TItem, bool> predicate)
        {
            var toRemove = _items.Where(predicate).ToList();

            _items.RemoveAll(i => predicate(i));
            foreach (var item in toRemove)
                _itemToStepMapping.Remove(item);
        }

        /// <summary>
        /// 获取所有项及其添加步骤的枚举器。
        /// </summary>
        /// <returns/>
        public IEnumerator<ItemStepPair<TItem>> GetEnumerator()
        {
            foreach (var obj in _items)
                yield return new ItemStepPair<TItem>(obj, _itemToStepMapping[obj]);
        }

        /// <summary>
        /// 获取所有项及其添加步骤的通用枚举器。
        /// </summary>
        /// <returns/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
