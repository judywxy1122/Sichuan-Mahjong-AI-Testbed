# SichuanMahjong 模块功能规格文档 v1 → v2 差异核对

## 核对范围

本文件只核对一个问题：`annotation/SichuanMahjong_模块功能规格文档_v2.md` 是否把 **Auto Play 模式下 player 出牌依据** 讲清楚，并且是否与 UnityVersion 当前 C# 代码一致。

对照文件：

- v1：`annotation/SichuanMahjong_模块功能规格文档_v1.md`
- v2：`annotation/SichuanMahjong_模块功能规格文档_v2.md`
- 代码：`UnityVersion/Assets/Scripts/Core/Gameplay/AutoPlayController.cs`
- 代码：`UnityVersion/Assets/Scripts/Core/AiModel/ProbabilityAI.cs`
- 代码：`UnityVersion/Assets/Scripts/Core/Algorithm/AIUtil.cs`

## 结论

v2 对 Auto Play 出牌依据的说明 **与当前代码一致**，并且比 v1 清楚很多。

核心结论是：

> Auto Play 在需要出牌时，不是“按每张牌的弃牌概率抽样”，也不是让 LLM 判断；它是枚举当前可弃的每一种牌，假设弃掉该牌后，对剩余手牌调用 `calc` 打分，然后选择使剩余手牌评分最高的那张牌。

也就是说，v2 的这句话是准确的：

> 表里不存在“某张牌该被弃掉的概率”这类条目。弃牌是“逐一试删后给剩余手牌评分取 argmax”，概率表只是评分的查询底座。

## v1 的不足

v1 已经写到了：

- `get_tile_to_play() = from_card(out_ai(cards))`
- `out_ai(cards)` 会“枚举弃掉每种牌后 `calc` 其余牌，取分最高者”
- `calc` 会对每门查概率表，DFS 组合并取最大值

但 v1 讲得比较压缩，容易让读者误解为：

- 概率表里直接存了“每张牌的弃牌概率”
- Auto Play 是概率采样
- `calc` 的分数就是一个简单的“距离胡牌还有几步”

这些理解都不准确。

## v2 新增内容

v2 主要在 M3 补齐了三类信息。

### 1. 概率表行语义

v2 说明了 `majiang_ai_normal.txt` 的行格式：

```text
<key> <jiang:0|1> <p:double> <人类可读注释...>
```

并解释了：

- `key` 是某一门 1..9 点各自张数拼成的十进制数。
- `jiang=1` 表示该门承担“将”的角色。
- `p` 是该门型在该角色下的概率权重。
- 万、筒、条只要点数分布一样，就查同一张 normal 表。

我核对了真实表文件，v2 给出的样例行确实存在于：

`UnityVersion/Assets/StreamingAssets/probability/majiang_ai_normal.txt`

例如：

```text
0 0 1.0
0 1 0.05480530240265118
12 0 0.0016140602582496414
12 1 0.013707897514633905
```

### 2. `calc` 评分函数

v2 对 `calc(cards)` 的描述与 `AIUtil.Calc` 一致：

1. 先把牌转换成 42 槽计数向量。
2. 调 `HuUtil.IsTingCard` 检查听牌。
3. 但当前游戏没有加载 `HuTable*`，所以听牌分支运行时不可达。
4. 构造万、筒、条、风、箭五类 key。
5. 各类 key 查表。
6. DFS 枚举五类表行组合。
7. 全局必须恰好一个 `jiang=true`。
8. 各门 `p` 是相加，不是相乘。
9. 返回合法组合中的最大分。

这与代码一致：

```csharp
CalcAITableInfo(ret, tmp, 0, false, 0.0);
return ret.Max();
```

以及 DFS 中：

```csharp
cur + aiTableInfo.p
```

所以 v2 说“评分是手牌整体成胡潜力，不是某张牌自己的概率”，这是准确的。

### 3. `out_ai` 弃牌选择

v2 对 `out_ai(cards)` 的描述与 `AIUtil.OutAI` 一致：

```text
for c in input:
  同值只试一次
  tmp = input 移除一张 c
  score = calc(tmp)
  if score > max:
      max = score
      ret = c
return ret
```

代码里对应逻辑是：

```csharp
foreach (int c in input)
{
    if (cache[c] == 0)
    {
        List<int> tmp = new List<int>(input);
        tmp.Remove(c);
        double score = Calc(tmp, guiCard);
        if (score > max)
        {
            max = score;
            ret = c;
        }
    }
    cache[c] = 1;
}
```

关键细节也一致：

- 候选牌按列表顺序遍历。
- 同值牌只试一次。
- 比较是严格 `>`，所以平分不换，先出现的候选获胜。
- 初始 `max` 是 `double.Epsilon`，对应 Java `Double.MIN_VALUE` 的“最小正 double”语义。
- 如果没有任何候选分数超过初值，返回 `0`。

## 与“人的直觉理解”的关系

用户原始理解是：

1. player 应该打牌时，通常已经摸到一张牌，且系统没有判定可胡。
2. 对每张候选弃牌，计算打出后剩余牌面离胡牌的距离；距离越小，这张牌越应该打出。

代码层面的精确说法应该是：

- 第 1 点大体成立。Auto Play 会先检查 `player.ContainsHu()`，可胡则直接胡，不进入弃牌分支。
- 第 2 点方向成立，但代码不是“距离越小、概率越大、再抽样”，而是“剩余牌面评分越高，直接选择该牌”。
- `calc` 不是显式 shanten 数，也不是真正逐张剩余牌可见概率；它是基于离线概率表的手牌结构评分。

因此更准确的描述是：

> Auto Play 选择弃牌时，会问：“如果我把这张牌打掉，剩下这手牌在概率表评分里是否更有成胡潜力？”它对所有候选牌都问一遍，然后选评分最高的弃牌。

## 小的边界说明

v2 里把 `out_ai(cards)` 写成“14 张手牌的建议弃牌”。这对普通摸牌后出牌是成立的。

但从代码看，`ProbabilityAI.SetHand()` 实际传入的是：

```csharp
暗手牌 + newTile
```

如果玩家已经有碰/杠面子，隐藏手牌数量可能少于 14。因此严格说法可以是：

> `out_ai` 输入的是当前可弃的隐藏手牌集合，通常是 13 张暗手牌 + 1 张 newTile；若已有碰/杠面子，则数量会相应减少。

这不是 v2 的实质错误，只是可以进一步精确。

## 重要局限：只看手牌，不看桌面公开信息

Probability AI 的出牌评分只使用 `ProbabilityAI.SetHand()` 生成的 `cards`：

```csharp
暗手牌 + newTile
```

它不会把以下信息纳入 `calc` / `out_ai`：

- 四家已经打到桌面的弃牌；
- 已亮出的碰、杠面子；
- 某张牌已经公开出现了几张；
- 牌墙剩余信息；
- 其他玩家的潜在听牌、危险牌、放炮风险。

因此它不是“完整牌局信息型 AI”。它更像一个 baseline：只根据自己当前隐藏手牌的结构，问“打掉哪张以后，剩余手牌在概率表中的结构评分最高”。

这个局限对用户体验和 AI 强度都有影响。例如，如果某张牌理论上能改善手牌结构，但桌面上已经出现了三张，真实玩家会降低它的价值；当前 Probability AI 不会做这种修正。

我已把这个限制同步补进 v2 文档的 `out_ai` 与 `set_hand` 说明中。

## 最终判断

v2 对 Auto Play 模式下 player 出牌依据的补充是可信的，和代码实现一致。它正确澄清了三件事：

1. 概率表不是弃牌概率表。
2. 弃牌选择是枚举候选弃牌后的评分 argmax。
3. 评分函数 `calc` 是基于概率表行组合的手牌整体评分。

建议后续以 v2 的 M3 为准，替代 v1 中较简略的 M3 描述。
