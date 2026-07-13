# SichuanMahjong PRD v1 → v2 差异说明

> 日期：2026-07-13
> 范围：记录 `SichuanMahjong_PRD_v1.md` 到 `SichuanMahjong_PRD_v2.md` 的产品层变化。

## 1. 变更结论

PRD v2 在 v1 的基础上补充两类内容：

- 当前已实现的 updated UI 版式：牌桌整体下移 + AI/player avatar。
- 未来产品方向：多人联机，以及真人 + agent 智能体联机。

其中 updated UI 已经进入当前 UnityVersion；多人联机与真人+agent 联机只是产品愿景和后续路线，不属于当前实现范围。

## 2. 文档层级变化

v1 指向：

- `SichuanMahjong_模块功能规格文档_v2.md`

v2 改为指向：

- `SichuanMahjong_模块功能规格文档_v3.md`

原因：v3 模块规格文档补充了 updated UI 的具体布局、avatar 资源、裁剪、绘制顺序与验收标准。

## 3. updated UI 已实现项

PRD v2 在功能需求中新增：

| 编号 | 功能 | 状态 |
|---|---|---|
| F13 | updated UI 牌桌版式（牌区整体下移 + AI/player avatar） | 已有 |

新增的产品描述包括：

- Unity 版从"可玩 demo"向"可展示 mini-game"演进。
- 夜空海面背景、牌桌区域、玩家/AI avatar、reward 动画共同构成当前视觉方向。
- updated UI 是表现层变化，不改变 Core 规则、Auto Play、LLM Play 或 benchmark 数据语义。

## 4. 未来联机方向

PRD v2 新增两个未来功能项：

| 编号 | 功能 | 状态 |
|---|---|---|
| F14 | 多人联机对局（房间、座位、同步状态、断线重连） | 未来方向 |
| F15 | 真人 + agent 智能体联机（玩家可邀请/替换 AI agent 入座） | 未来方向 |

这个方向是合理的，原因是：

- Core 规则引擎已经和 Unity 表现层分离，可作为服务端权威状态机的基础。
- `PlayerActionContext` 已经提供合法动作空间和动作校验，可作为网络动作请求的安全边界。
- Auto Play / LLM Play 已通过 `IPlayController` 抽象出不同决策来源，未来可把一个座位绑定到真人、概率 AI、LLM agent 或远程 agent。
- 当前 log/LLM log/未来 JSONL 日志可以复用为观战、复盘和 benchmark 数据基础。

PRD v2 建议的未来形态：

- 多人联机房间。
- 真人 + agent 房间。
- 观战与复盘。
- 服务端权威规则校验。

## 5. 里程碑变化

PRD v2 在原有 M1-M3 之后新增：

| 里程碑 | 内容 |
|---|---|
| M4 | 多人联机 / 真人+agent 智能体联机原型：房间、座位、服务端权威状态机、事件回放 |

## 6. 非变更项

PRD v2 没有改变：

- 已实现游戏规则。
- 当前一胡即止、无计分、无血战到底等现状。
- 三种控制模式：Player / Auto Play / LLM Play。
- LLM Play 的合法动作校验与 fallback 机制。
- curation 相关 F7/F8/F9/F10 的优先级判断。

