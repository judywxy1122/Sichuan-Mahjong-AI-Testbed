# 四川麻将（SichuanMahjong）产品需求文档（PRD）

> 版本：v2.0（2026-07-13）。v2.0：补充 updated UI 版式目标与当前实现状态（avatar + 牌桌整体下移），并新增多人联机 / 真人+agent 智能体联机的未来产品方向。
> v1.1：依代码复核修正七对/龙七对的实际实现语义（§2.1、§6、附录 A）；明确文档层级。
> 代码基线：`SichuanMahjong/UnityVersion`（Unity 2022.3 表现层 + 纯 C# 规则引擎，Java 原版逐行移植）
> 文档层级：本 PRD 只描述特性层（what/why）；函数级接口契约（签名/参数/返回/错误/副作用）见 `SichuanMahjong_模块功能规格文档_v3.md`
> 定位：既是一款单机四川麻将（1 真人 + 3 概率 AI），更是 **AI mini-game Benchmark 的决策数据生产环境**——它已内置 LLM 代打（LlmPlayController）、合法动作校验（PlayerActionContext）与无头批量对局（ConsoleHarness）。v2 开始将 Unity 体验从调试可玩推进到更面向用户的牌桌视觉，并为未来多人/真人+agent 联机形态预留产品方向。

---

## 1. 产品概述

### 1.1 一句话描述
四人四川麻将（条/万/筒 108 张，无字牌），真人坐下家，其余三家由概率 AI（87MB 查表概率模型）驱动；真人座位可切换三种控制模式：**手动 / Auto Play（概率 AI 代打）/ LLM Play（大模型代打）**，三种模式共用同一规则引擎，仅决策来源不同。Unity 版已加入夜空海面背景、牌桌区域、玩家/AI avatar 与 reward 动画，目标是从"可玩 demo"逐步走向可展示的 mini-game。

### 1.2 架构分层（产品级承诺）
| 层 | 内容 | 关键约束 |
|---|---|---|
| Core 规则引擎 | 牌、对局状态机、胡/碰/杠判定、概率 AI、Auto/LLM 控制器 | **零 Unity 依赖**（asmdef `noEngineReferences:true`），可用 .NET 8 独立编译 |
| UnityApp 表现层 | 代码生成 uGUI、1920×960 逻辑坐标、键盘/鼠标交互、奖励动画、updated UI avatar 版式 | 只做渲染与输入，不含规则 |
| ConsoleHarness | 无头 .NET 控制台：`tests` / `simulate N` / `parity hands.txt` | 不参与 Unity 构建；已验证 300/300 与 Java 版对拍一致、100 局 0 异常 |

### 1.3 目标用户与使用场景
1. **玩家**：单机对局娱乐（点击出牌，H/C/P/K/S 快捷键胡/吃/碰/杠/过）。
2. **AI 研究 / Benchmark 数据工程师（核心角色）**：
   - 用 LLM Play 采集"决策点 → LLM 选择 + 理由 + 合法性"轨迹；
   - 用 ConsoleHarness 批量自对弈产生基线数据；
   - 用 Auto Play（概率 AI）作为稳定 baseline 对照。

---

## 2. 游戏规则规格（以代码实际行为为准）

### 2.1 已实现规则
- 牌组：条(B)/万(C)/筒(D) 各 1–9 × 4 = **108 张**，无风/箭/花牌。
- 发牌：每人 13 张，随机庄家多 1 张并先行；牌墙余 55 张。
- 胡牌型：**标准胡（4 面子 + 1 将）**、**七对**（暗手恰好 7 个不同对子值）；清七对可识别但不加番。⚠️ 代码中的"龙七对"分支以"已亮杠 ≥3"为门槛、与七对条件互斥，**实际不可达**；且暗手含一坎四张的七对型手牌（5 对+四张）**不被识别为胡牌**——移植保留的缺陷，详见模块文档 M2 与 §6。
- **缺一门硬约束**：手牌花色 ≤2 门才可胡（四川"缺一门"以校验形式存在，无玩家声明的定缺阶段）。
- 碰 / 明杠 / 加杠 / 暗杠，杠后补摸并继续出牌（无杠上花计分）。
- 终局：任一玩家胡牌即结束（**一胡即止**）/ 牌墙摸空流局 / 异常终止。
- 响应判定：出牌后按**座位顺序**轮询下家起三家，每家内部优先级 胡 > 碰/杠 > 过；**第一个有动作的座位截断后续座位**（注意：这不是全局"胡牌优先"，若近座可碰、远座可胡，碰会截胡——移植保留的规则简化，benchmark 语义需注意）。

### 2.2 未实现（Roadmap 标记为 Coming）
- **血战到底**（胡后继续打）、**计分板/番型计分**（自摸、杠上花、根、清一色等番种均无）、换三张、玩家声明式定缺。
- 胜利奖励目前是娱乐性彩蛋（随机打开 TikTok 奖励视频 + 美元动画）。
- 多人联机 / 真人+agent 智能体联机尚未实现；当前所有真人/Auto/LLM 控制模式都运行在单机同一进程内。

### 2.3 三种控制模式（GAME_MODES）
| 模式 | 决策来源 | 触发方式 | 说明 |
|---|---|---|---|
| Player | 真人点击/快捷键 | 默认 | UI 只显示当前合法的动作按钮 |
| Auto Play | `ProbabilityAI`（本地查表） | 按钮切换，3 秒延迟执行 | 稳定基线，无需网络 |
| LLM Play | 大模型（HTTP chat completions） | 按钮切换，3 秒延迟，后台线程 | LLM 只能从引擎给出的合法动作集中选择；非法/超时/无 key 一律回退 Auto Play |

---

## 3. 功能需求

| 编号 | 功能 | 状态 | 优先级 |
|---|---|---|---|
| F1 | 完整单机对局（发牌/摸打/碰杠胡/流局） | 已有 | P0 |
| F2 | 三种控制模式互斥切换（Player/Auto/LLM） | 已有 | P0 |
| F3 | 合法动作按钮动态显示 + 快捷键 H/C/P/K/S | 已有 | P0 |
| F4 | LLM 决策日志 `llm_play_log.txt`（分节文本） | 已有 | P0 |
| F5 | ConsoleHarness：tests / simulate N / parity | 已有 | P0 |
| F6 | 胡牌牌型展示（WinningHandDialog）+ 奖励动画 | 已有 | P2 |
| F7 | **随机种子/牌墙注入接口**（可复现对局） | 缺失 | **P0（curation 前置）** |
| F8 | **JSONL 结构化决策日志**（含 game_id/turn_id/座位/胜负回填） | 缺失 | **P0（curation 前置）** |
| F9 | ConsoleHarness 支持指定任意 `IPlayController`（含 LLM）批量对局 | 缺失（改动极小） | P1 |
| F10 | 计分/番型模块（自摸/杠上花/根/清一色/七对番数） | 缺失 | P1（给 benchmark 提供密集 reward 信号） |
| F11 | 血战到底模式 | 缺失 | P2 |
| F12 | AI1/AI2/AI3 三个重复类合并为参数化单类 | 技术债 | P2 |
| F13 | updated UI 牌桌版式（牌区整体下移 + AI/player avatar） | 已有 | P2 |
| F14 | 多人联机对局（房间、座位、同步状态、断线重连） | 未来方向 | P1 |
| F15 | 真人 + agent 智能体联机（玩家可邀请/替换 AI agent 入座） | 未来方向 | P1 |

### 3.1 LLM Play 接口规格（现状，作为对外契约）

**环境变量**：`GPT_API_SG_KEY`（必需）、`MAHJONG_LLM_MODEL`（默认 `gpt-5.5-2026-04-24`）、`MAHJONG_LLM_ENDPOINT`、`MAHJONG_LLM_API_VERSION`（默认 `2025-01-01-preview`）、`MAHJONG_LLM_REASONING`（默认 medium）、`MAHJONG_LLM_TIMEOUT_SECONDS`（默认 18）。

**发给 LLM 的 state JSON 字段**（部分可观测，符合真实麻将信息集）：
```json
{
  "player": "YOU", "last_action": "...", "status": "...",
  "last_played_tile": "C7",
  "legal_actions": ["hu", "pung", "skip"],
  "hand": ["B1","B2","C7","..."], "new_tile": "D5",
  "your_discards": ["..."],
  "melds": [{"type":"PUNG","tiles":["C3","C3","C3"]}]
}
```

**LLM 必须返回**：`{"action":"discard","tile":"C7","reason":"..."}`；action ∈ {hu, chow, pung, kong, skip, discard}；解析兼容中英同义词（win/胡/peng/碰/gang/杠/pass/过…）。

**安全网**：引擎用 `PlayerActionContext.IsLegal` 校验，非法即 FALLBACK 到 Auto Play——**LLM 永远无法执行非法动作**。

### 3.2 牌编码规格（三套并存，文档必须显式声明）
| 场景 | 编码 | 示例 |
|---|---|---|
| 领域对象 | `Tile{type∈{B,C,D}, number∈1..9}` | — |
| 传输/日志/LLM | `"<B|C|D><1-9>"`，B=条 C=万 D=筒 | `C7`=七万 |
| 算法/概率表 | 整数 1..27（万1-9，筒10-18，条19-27） | 7=七万 |

### 3.3 updated UI 版式（v2 已实现）

当前 Unity 版已完成第一步视觉改造：

- AI players 的牌面区域整体向下移动，为 AI avatar 留出上方空间。
- main player 的弃牌区与手牌区整体向下移动同样距离，保持桌面视觉重心一致。
- 每局启动时从 `StreamingAssets/avatars/ai_*.png` 随机选 3 张，按 AI1/AI2/AI3 显示在对应牌区上方。
- 每局启动时从 `StreamingAssets/avatars/player_*.png` 随机选 1 张，显示在 main player 牌面左侧。
- avatar 使用居中裁剪到固定显示框，避免不同素材比例导致布局漂移。

该改造仍属于单机 UI 表现层，不影响 Core 规则、Auto Play、LLM Play、ConsoleHarness 或 benchmark 数据语义。函数级规格见 `SichuanMahjong_模块功能规格文档_v3.md` §4。

### 3.4 未来联机方向（产品愿景）

多人联机与真人+agent 智能体联机是合理的下一阶段方向，原因是：

- 当前 Core 规则引擎已经相对独立，理论上可被服务端或房主进程复用。
- `PlayerActionContext` 已经把合法动作空间抽象出来，可作为网络消息校验与 agent 行为边界。
- Auto Play 与 LLM Play 已通过 `IPlayController` 抽象为可替换决策来源，未来可以把某个座位绑定到真人、概率 AI、LLM agent 或远程 agent。
- Playing log、LLM log 与未来 JSONL 结构化日志可以作为联机观战、复盘和 benchmark 数据的共同基础。

建议的未来形态：

1. **多人联机房间**：一个房间 4 个座位，支持真人入座、准备、开始、出牌/响应同步、断线重连。
2. **真人 + agent 房间**：空座可由概率 AI 或 LLM agent 托管；真人也可中途切换为 Auto/LLM 代打。
3. **观战与复盘**：基于事件日志回放整局，方便用户观看，也方便 AI benchmark 采样。
4. **服务端权威规则**：客户端只发送动作请求，服务端用 `PlayerActionContext` 与 Core 状态机校验并广播结果。

这部分暂不进入当前实现范围；进入开发前需要先定义网络协议、房间状态机、座位权限、动作超时、隐私信息边界（不能泄露对手暗牌和牌墙）。

---

## 4. 非功能需求

| 项 | 要求 |
|---|---|
| 规则引擎可信度 | 任何规则改动必须过 ConsoleHarness `tests` 与 `parity`（与 Java 参考 300/300 一致为基线） |
| 启动性能 | 概率表 87.4MB / 81 万行，异步后台加载（Unity 数秒）；无头工具同样按 jian→feng→normal 顺序加载 |
| LLM 可靠性 | 超时/异常/非法输出必须回退，不得阻塞对局主循环（LLM 调用在后台线程，经主线程队列回投，用 actionVersion+手牌哈希防过期决策） |
| 跨平台 | Core 与 ConsoleHarness 可在无 Unity 的 Linux 运行（已验证） |

---

## 5. AI Benchmark 数据 Curation 需求（本 PRD 重点）

### 5.1 现状：已具备的数据能力

| 能力 | 位置 | 产出 |
|---|---|---|
| 决策点五元组 | `LlmPlayController` → `llm_play_log.txt` | (合法动作集, LLM 选择, reason, 合法/回退, 延迟 ms) —— reason 可直接作 CoT 标签；FALLBACK 率 = 模型非法输出率 |
| 合法动作真值源 | `PlayerActionContext.LegalActionLabels()/IsLegal()` | 每个决策点的动作空间标注，**必须复用而非重新实现** |
| 批量自对弈 | `ConsoleHarness simulate N` | N 局胜/流局/异常统计 + 各座胜场 |
| 引擎一致性验证 | `ConsoleHarness parity` | 与参考实现逐行 diff（弃牌/碰杠票/状态位） |
| 状态快照 | `Game.GetGameState()` + `BuildRequestJson` | LLM 视角 observation（部分可观测） |

**现有日志格式**（`llm_play_log.txt`，追加式分节文本）：每段 `=== <ISO时间> <SECTION> ===`，SECTION ∈ {NEW GAME, REQUEST, RESPONSE, ACCEPTED, FALLBACK}；REQUEST 含 legal_actions + 完整 state JSON，RESPONSE 含 elapsed_ms + 原始输出。

### 5.2 缺口与新增需求

1. **F7 可复现性**：`Game` 使用无种子 `System.Random`，无法复现对局。要求：构造函数支持注入 `seed` 或完整牌墙序列；日志记录 seed。这是 curation 的硬前置。
2. **F8 结构化日志**：现文本日志无 game_id / turn_id / 座位 / 最终胜负回填，难以按局聚合。要求新增 JSONL：

```json
{"t":"decision","game_id":"g-0001","turn":42,"seat":0,"controller":"llm",
 "state":{...BuildRequestJson 原样...},"legal_actions":["hu","skip"],
 "chosen":{"action":"hu","tile":null,"reason":"..."},"legal":true,
 "fallback":null,"elapsed_ms":812}
{"t":"result","game_id":"g-0001","winner_seat":0,"winning_tile":"D5",
 "end":"win|draw|error","rounds":63,"seed":12345}
```

3. **F9 无头 LLM 批量采集**：`simulate` 当前真人座固定 `AutoPlayController`；`IPlayController` 抽象已就绪，加参数 `--controller llm` 即可无头批量采集 LLM 轨迹（无需开 Unity）。
4. **F10 计分信号**：当前只有胜/负/流局的稀疏结果；补番型计分后，benchmark 可获得细粒度 reward（番数）用于策略质量评估。

### 5.3 可派生的 Benchmark 任务集

| 任务 | 输入 | 输出 | 评测指标 |
|---|---|---|---|
| T1 合法动作选择 | state JSON + legal_actions | 单个动作 | 合法率、与 ProbabilityAI/搜索基线一致率、最终胜率 |
| T2 弃牌决策 | 手牌 + 场面 | discard:X | 与概率 AI 最优弃牌一致率（parity 模式现成）、听牌进度提升 |
| T3 胡牌/听牌判定 | 14 张手牌（文本码） | 能否胡 / 听哪些张 | 精确匹配（HuFitter/HuUtil 可无限合成标签） |
| T4 全局对弈 | 完整对局 | 胜率/流局率 | simulate 批量统计，vs 三个概率 AI |
| T5 决策解释 | 决策点 | reason 文本 | 人评/LLM 评（已有 reason 字段做参照） |

### 5.4 Curation Pipeline（目标形态）

```
seed 池 ──▶ ConsoleHarness simulate --controller {auto|llm} --seed s --jsonl out/
                    │
                    ├─▶ decision 事件流（每个决策点：state + 动作空间 + 选择 + 理由 + 合法性）
                    ├─▶ result 事件（胜负回填 → 按 game_id join 回每个 decision）
                    └─▶ 统计报表（合法率 / 胜率 / 平均局长 / fallback 率）
```

数据组织建议：`data/curation/mahjong/{run_id}/games.jsonl + decisions.jsonl + meta.json`（meta 记 controller、model、seed 范围、引擎版本、概率表 hash）。

---

## 6. 已知问题与风险

1. **无随机种子注入**（F7）——curation 硬伤，优先修。
2. 响应优先级为"座位就近优先"而非"全局胡牌优先"（§2.1）；作为 benchmark 环境规则须在数据卡中显式声明，或增开关改为全局优先。
2a. **七对含暗四张不可胡 / 龙七对分支不可达**（§2.1）：如按真实四川麻将规则修复（四张计两对），属规则变更，必须过 `tests`+`parity` 并在数据卡声明版本；如保持现状，须在 benchmark 任务 T3 的标签生成中显式排除该类手牌歧义。
3. `ProbabilityAI.ShouldChow` 恒 true、CHOW 状态几乎不可达（四川麻将本无吃）；`chow` 仍出现在动作枚举中，建议从 legal_actions 生成中移除以免干扰模型。
4. LLM endpoint 默认值按 OS 硬编码内网域名；需允许完全由环境变量覆盖（已支持）并在文档声明。
5. `AI1/AI2/AI3` 三类代码重复；`Game.cs` 586 行混合状态机/文本生成/快照三职（拆分建议见模块文档）。
6. 胜利彩蛋打开外部 TikTok 链接，发布/评测环境建议可配置关闭。

---

## 7. 里程碑建议

| 里程碑 | 内容 |
|---|---|
| M1 | F7 种子注入 + F8 JSONL 决策日志 + F9 无头 LLM simulate（curation 最小可用） |
| M2 | F10 番型计分模块；产出首批 decisions/games 数据集与统计报表 |
| M3 | 血战到底/换三张/定缺声明等规则扩展（提升与真实四川麻将的一致性） |
| M4 | 多人联机 / 真人+agent 智能体联机原型：房间、座位、服务端权威状态机、事件回放 |

---

## 附录 A：关键事实速查
- 牌墙 108 张；发牌 4×13+庄家 1 张；余 55 张可摸
- 胡型：标准 4+1 / 七对（7 个不同对子值；含暗四张不识别，龙七对分支不可达，见 §2.1）；缺一门（suits≤2）硬校验（`PlayerStatusChecker.cs:115-131`）
- 一胡即止（`Game.cs:245`）；无计分/番型/血战（LinuxVersion README 标 Coming）
- 概率表：`majiang_ai_normal.txt` 87.4MB/810,700 行 + feng/jian 两张小表；行格式 `key jiang p <注释>`
- LLM 决策日志：`llm_play_log.txt`，分节 NEW GAME/REQUEST/RESPONSE/ACCEPTED/FALLBACK（`LlmPlayController.cs:465-480`）
- 无头验证：`dotnet run -- <表目录> tests|simulate N|parity hands.txt`（已验证 300/300 对拍、100 局 0 异常）
- 真人座 3 秒决策延迟（`GamePanelBehaviour` PlayDelaySeconds=3f）；LLM 跑后台线程防阻塞
