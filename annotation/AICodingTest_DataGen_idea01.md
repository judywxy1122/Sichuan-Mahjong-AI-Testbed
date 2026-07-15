# AI Coding Test Data Generation

## Task Description
我刚创建了一个空的folder
/Users/bytedance/work/Sichuan-Mahjong-AI-Testbed/data

这个 data folder 在 .gitignore 里

我想在这个 data folder 里面放 测试 AI coding 能力的 "数据"

data folder 里面有多个subfolders

每一个 subfolder 对应一个 测试 AI coding 能力的 "数据点"。

为了能够构造出若干数据点，我们先明确我们用什么来构造，也就是数据源

### 数据源 和 知识源

数据源是
/Users/bytedance/work/Sichuan-Mahjong-AI-Testbed/UnityVersion
里面的 基于 UnityVersion 的代码

知识源是
/Users/bytedance/work/Sichuan-Mahjong-AI-Testbed/annotation
里面的 对 基于 UnityVersion 的代码 的解析 和 分析文档

需要注意的是，如果没有annotation这个folder，或者有annotation folder，但是folder为空的话，就是没有知识源，那就只能从代码里获得知识

### 数据点的构造

每一个 数据点 要么是初始代码库状态，要么是衍生代码库状态：
#### 初始代码库状态
初始代码库状态只有一个。 是数据点 data/data_point_0000，也就是 data folder 下面的一个叫 data_point_0000 的 subfolder
data_point_0000 里面装的是 /Users/bytedance/work/Sichuan-Mahjong-AI-Testbed/UnityVersion 里面的代码。

需要注意的是 可能需要做一些 整合的工作，来保证：
在unity hub 的 projects 里，用 /Users/bytedance/work/Sichuan-Mahjong-AI-Testbed/data/data_point_0000 来建一个新的projet的话，能启动起来相应的unity project

#### 衍生代码库状态
衍生代码库状态可以有很多。
一个衍生代码库状态 是 one of data/data_point_0001, data/data_point_0002, ...

目前focus on so-called one-hop 状态，也就是
   对于一个可以解析出来的重要模块，say M_x，详细写出 M_x 的 product development document (,i.e. PRD)
   需要注意的是：对于这款Sichuan Mahjong game，annotation 里面 已经有可以参考的知识文档：
   annotation/SichuanMahjong_PRD_v2.md 和 annotation/SichuanMahjong_模块功能规格文档_v3.md

   从这些知识文档里，你能看到都有哪些已经解析出来的模块。

   但是写出这个模块 M_x 的 PRD 的时候，一个关键的假定是：假设其他模块都ok，只有这个 M_x 是待开发。所以侧重点不是 data_point_0000 的相应模块的功能结构解析，而是 M_x 功能需求和开发计划。
   
   这个假定的愿景是：如果 data_point_0000 的代码质量不错，以及该模块的 PRD 写的好，
   那 AI coding (Claude or Codex) 能根据这个 PRD，开发出来一个 M_x 的 复现版 M‘_x，so that M‘_x 能在功能上能复现 M_x

  所以 一个衍生代码库状态 data_point_x 是 从 data_point_0000 里面拿掉 M_x，再加上 PRD_x。这个 PRD_x 跟 这个 M_x 严格对应。

#### PRD的设计
可以看到，构造数据点需要生成每个 解析出来的重要模块 的PRD，
我的一个想法是：
1）根据
annotation/SichuanMahjong_模块功能规格文档_v3.md
来 产生一个总的PRD，
该 PRD 里 列出 M1 到 M8 的各自的需求、功能、以及开发计划。当然，如果需要的话，可以参考 data_point_0000 里面的源代码

总的PRD 放到 data里，起名为 data/PRD_all.md 。

这里面你看看 界面排版规格（v3 新增）in SichuanMahjong_模块功能规格文档_v3.md 是不是一个可以当作是 M9 的 模块。如果是的话，你就给加到 PRD_all 里面。

2）for x = 1, 2, ...
   根据 PRD_all 和 M_x，写出 data/PRD_x.md，并且 生成出来 data_point_x，也就是 从 data_point_0000 里面拿掉 M_x

### Task 执行
采取一步一个脚印的方针，

每次只生成一个代码库状态。

第一次生成 data/PRD_all.md 和 data/data_point_0000，

我来看一下 PRD_all.md，

如果我觉得可以的话，

第二次生成 data/PRD_1.md 和 data/data_point_0001，

我来看一下 data/PRD_1.md，

如果我觉得可以的话，

...

以此类推。


### 补充 1
我想补充的一点是：
现在应该是只有 data/data_point_0000/` 可由 Unity Hub 独立打开并且运行。

data/data_point_0001/

data/data_point_0002/

...

都不还不能 由 Unity Hub 独立打开并且运行。因为 data_point_x 是 data_point_0000 拿掉 M_x 的状态

之后会做的事情是 用 AI coding 从 data_point_x 和 PRD_x 来生成 data_point_x_gen, 愿景是 data_point_x_gen 可由 Unity Hub 独立打开并且运行。


### 补充 2
还有个问题需要补充：
用 AI coding 从 data_point_x 和 PRD_x 来生成 data_point_x_gen, 
虽然 愿景是 data_point_x_gen 可由 Unity Hub 独立打开并且运行，
但实际上，很有可能发生的是 初次生成的 data_point_x_gen 放到 Unity Hub 独立打开时会报错。
也就是说比较实际的数据点验收就是 AI coding 先给 implement 出来 data_point_x + M'_x = data_point_x_gen

能否编译成功 并进入 Play Mode 是下一步 数据标注 的事情。
