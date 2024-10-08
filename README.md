# GoRogueCN

这是 GoRogue 库的中文分支，现阶段代码上不做添加，并将与原库主分支保持一致。以下为 GoRogue 的描述。

## GoRogue
[![Chat on discord](https://img.shields.io/discord/660952837572001804.svg)](https://discord.gg/fxj5kPq)
[![Join us on Reddit](https://img.shields.io/badge/reddit-GoRogueLib-red.svg)](http://reddit.com/r/goroguelib)
[![NuGet](https://img.shields.io/nuget/vpre/GoRogue.svg)](http://www.nuget.org/packages/GoRogue/)

欢迎来到 GoRogue 的首页，这是一个现代的 .NET Standard roguelike/2D 游戏实用程序库！本库提供了多种可能有助于 2D 网格和 roguelike 游戏开发的功能，包括用于计算视野(FOV)、寻路、生成地图、绘制线条、生成随机数、创建消息系统架构等的算法，以及更多功能！有关详细信息，请参阅下面的功能列表。

## 文档
您可以在[文档网站](http://www.roguelib.com)上找到入门指南、教程文章和API文档。此外，API文档也会如您所期望的那样显示在您的集成开发环境(IDE)中。 GoRogue 还有一个 Subreddit 社区，位于 [r/GoRogueLib](https://www.reddit.com/r/GoRogueLib/) ，以及一个 [Discord服务器](https://discord.gg/fxj5kPq) 。

## 功能列表
**支持可空引用类型:** GoRogue 完全支持 C# 8 中引入的[可空引用类型](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references)特性的代码注释。它不会影响或破坏未启用该特性的代码，但对于使用该特性的项目，它提供了更多的编译时安全性，使您能够更快地构建有效且可靠的代码。

**便捷的原始类型:** GoRogue 基于 `SadRogue.Primitives` 库，该库为坐标、矩形、网格等提供了全面、易用且灵活的原始类型。此外， `SadRogue.Primitives` 还为定义了这些类型的其他常用库(如 MonoGame、SFML 等)提供了集成包，便于与这些库的等效类型进行集成。它还提供了易于操作和处理网格的功能，包括确定半径内的位置、在网格上移动位置以及计算距离等操作。

**无侵入的算法:** GoRogue 算法基于一个简单的输入/输出数据抽象，因此 GoRogue 可以轻松地集成到许多不同的现有系统/库中，而无需复制数据或合并数据结构。

**灵活的组件系统:** GoRogue 实现了一个高性能、类型安全的组件“容器”结构，允许您轻松地将任意类的实例作为组件附加到对象上，并通过类型或标签检索它们。

**骰子表示法解析器:** GoRogue 实现了一个骰子表示法解析器，它可以处理复杂的骰子表达式，使用随机数生成器(RNGs)进行模拟，并返回结果。

**工厂框架:** GoRogue 通过定义“蓝图”并调用它们来按名称实例化对象，从而为实现[工厂方法模式](https://en.wikipedia.org/wiki/Factory_method_pattern)提供了一个面向对象的框架。

**具体的地图/对象表示系统:** 除了提供通用的核心算法外，GoRogue 还提供了一个具体的 "GameFramework" 系统，该系统提供了一种现成的方式来表示地图及其上的对象，并集成了许多 GoRogue 的核心功能，如视野(FOV)、寻路以及组件系统，使它们能够即插即用。

**灵活的地图生成框架:** GoRogue 提供了快速启动地图生成的方法，允许您以常见的方式生成地图。它包括每个步骤作为一个独立的“步骤”，这些步骤也可用于自定义地图生成。此外，它还提供了一个地图生成的类框架，使得设计可调试的、独特的自定义地图生成步骤并按顺序应用它们以生成最终地图变得容易。

**消息总线系统:** GoRogue 提供了一个简单且类型安全的系统，用于创建"消息总线"架构，在该架构中，消息可以通过消息总线发送，并且各种系统可以订阅并响应相应类型的消息。

**寻路:** GoRogue 提供了多种寻路算法。这些算法包括性能极高的 A* 寻路算法和"目标地图"(也称为[Dijkstra 地图](http://www.roguebasin.com/index.php?title=The_Incredible_Power_of_Dijkstra_Maps))的实现。 GoRogue 还提供了"逃离地图"的实现。

**随机数生成:** GoRogue 基于[Troschuetz.Random](https://gitlab.com/pomma89/troschuetz-random)库构建，该库提供了使用各种方法方便地生成随机数和序列的生成器，以及序列化这些生成器的功能。GoRogue 增加了一些设施，用于轻松地为需要生成器的GoRogue函数提供自定义生成器，以及一些可能在调试/单元测试中有用的自定义数字生成器。

**视野(Field-of-View):** GoRogue为计算视野提供了[递归阴影投射](http://www.roguebasin.com/index.php?title=FOV_using_recursive_shadowcasting)的灵活实现，该实现支持距离限制和角度限制的锥形视野。

**感知地图(Sense Maps):** 除了视野算法外，GoRogue还提供了一个框架，用于使用各种算法原始地模拟声音、光线、热量等的传播。

**有用的数据结构:** GoRogue提供了许多在类Roguelike游戏开发中常用的数据结构。这些包括“空间地图”，这是一种高效存储和分层网格上定位对象的方法，以及实现路径压缩的[不相交集](https://en.wikipedia.org/wiki/Disjoint-set_data_structure)结构。

**效果系统:** GoRogue提供了一个强大且类型安全的效果系统，该系统可用于在回合制游戏系统中模拟具有或不具有持续时间的“效果”。它提供了处理具有持续时间（以任意单位表示）、即时效果和无限效果的方法。此外，它还提供了一个结构来分组和自动触发效果，并支持取消效果。

**线条算法:** GoRogue当前提供了[Bresenham's](https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm)线条算法的实现，以及一些用于确定网格上线条的其他算法。在 GoRogue 3.0 正式版发布之前，这些算法将被移至 `TheSadRogue.Primitives` 中。

**数学工具:** GoRogue 在 `TheSadRogue.Primitives` 提供的数学工具函数的基础上，增加了一些有用的方法，用于处理数组索引的环绕数字、四舍五入到最接近的数字倍数，以及近似计算某些三角函数。在 GoRogue 3.0 正式版发布之前，这些方法可能会被移至 `TheSadRogue.Primitives` 中。

**实用函数:** GoRogue为各种类添加了许多杂项实用函数作为扩展方法。这些函数包括使用GoRogue的随机数生成框架从列表中随机选择项目/索引的方法、合理创建表示列表和其他可枚举对象元素的字符串的方法、对列表中的项目进行洗牌的方法等等！

**序列化(进行中):** GoRogue 3中的序列化支持仍在开发中。目前， GoRogue 计划通过 C# 内置的[数据契约序列化](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.datacontractserializer?view=net-5.0)功能来提供序列化。此外，还计划推出一个 `GoRogue.JSON` 包，该包将使序列化功能与[Newtonsoft.JSON](https://www.newtonsoft.com/json)无缝协作。

## 许可协议
### GoRogue
本项目采用MIT许可协议授权 - 详情请参阅[LICENSE.md](LICENSE.md)文件。

### 其他许可协议
GoRogue所依赖或从中获得灵感的其他项目的许可协议，在致谢部分列出。

## 致谢
GoRogue的部分功能依赖于一些其他的.NET Standard库，并且从其他优秀的类Roguelike/2D游戏相关项目中获得了一些灵感。以下列出了这些项目及其许可协议。

### TheSadRogue.Primitives
这个库为 GoRogue 在其核心算法中使用的 2D 网格操作提供了基础类和算法。它的许多功能最初是 GoRogue v2 的一部分。该项目也遵循 MIT 许可协议，并由我和 Thraka (SadConsole 的创建者)共同维护：
- [TheSadRogue.Primitives](https://github.com/thesadrogue/TheSadRogue.Primitives)
- [TheSadRogue.Primitives License](https://github.com/thesadrogue/TheSadRogue.Primitives/blob/master/LICENSE)

### Troschuetz.Random
GoRogue 依赖于这个库来实现其随机数生成器 (RNG) 功能的基础。该项目也遵循 MIT 许可协议：
- [Troschuetz.Random](https://gitlab.com/pomma89/troschuetz-random/-/tree/master)
- [Troschuetz.Random License](https://gitlab.com/pomma89/troschuetz-random/-/blob/master/LICENSE)

### 优化后的优先级队列
GoRogue 依赖于这个库来提供其路径查找算法中使用的队列。该项目也遵循 MIT 许可协议：
- [OptimizedPriorityQueue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)
- [OptimizedPriorityQueue License](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/blob/master/LICENSE.txt)

### SquidLib
这个 Java 类 Roguelike 库是 GoRogue 许多功能的另一个重要灵感来源。 `SenseMap` 中使用了类似的 RIPPLE 算法，而"空间地图"的概念最初是从 SquidLib 的实现中借鉴的。 GoRogue 中没有直接使用 SquidLib 的源代码， GoRogue 中的任何项目都不依赖于 SquidLib 或使用 SquidLib 的二进制文件。
- [SquidLib](https://github.com/SquidPony/SquidLib)
- [SquidLib License](https://github.com/SquidPony/SquidLib/blob/master/LICENSE.txt)

### Dice Notation .NET
`GoRogue.DiceNotation` 命名空间的功能设计灵感主要来源于 Dice Notation .NET 库。该项目也遵循 MIT 许可协议：
- [Dice Notation .NET](https://github.com/eropple/DiceNotation)
- [Dice Notation .NET License](https://github.com/eropple/DiceNotation/blob/develop/LICENSE.txt)

### RogueSharp
`GoRogue.MapGeneration` 命名空间中某些算法的设计灵感主要来源于 C# 库 RogueSharp 。该项目也遵循 MIT 许可协议：
- [RogueSharp](https://bitbucket.org/FaronBracy/roguesharp)
- [RogueSharp License](https://bitbucket.org/FaronBracy/roguesharp/src/master/LICENSE.txt?at=master)

### Doryen Library (libtcod)
这个经典的类 Roguelike 工具包库为 GoRogue 的许多功能提供了灵感。
- [Libtcod](https://github.com/libtcod/libtcod)
- [Libtcod License](https://github.com/libtcod/libtcod/blob/develop/LICENSE.txt)

