# SichuanMahjong 模块功能规格文档 v2 → v3 差异说明

> 日期：2026-07-13
> 范围：仅记录 `SichuanMahjong_模块功能规格文档_v2.md` 到 `SichuanMahjong_模块功能规格文档_v3.md` 的规格变化。

## 1. 变更结论

v3 是一次 **Unity 表现层 UI 版式规格补充**。它不改变 Core 规则引擎、概率 AI、Auto Play、LLM Play、ConsoleHarness、胡牌判定、概率表算法或日志语义。

核心变化：

- 新增 updated UI 牌桌排版规格。
- 明确 AI/player 牌面整体下移的坐标规则。
- 明确 AI/player avatar 的资源目录、随机选择、居中裁剪与绘制顺序。
- 明确这次 UI 改动的验收标准。
- 将配套 PRD 指向 `SichuanMahjong_PRD_v2.md`。

## 2. v2 原状态

v2 的主要增量是 M3 概率表算法规格，重点说明：

- `out_ai(cards)` 的弃牌依据。
- `calc(cards)` 的概率表评分逻辑。
- `majiang_ai_normal.txt` 的 key / jiang / p 行语义。
- Probability AI 只看暗手 + newTile，不看桌面公开牌的局限性。

v2 对 Unity 表现层只做模块清单级描述：

- UnityApp 是表现层。
- `Config.cs` 管理 1920×960 逻辑坐标。
- `GamePanelBehaviour` 负责各类 Draw*。
- 但没有对 updated UI 排版、avatar、资源目录和裁剪规则做函数级规格。

## 3. v3 新增 UI 规格

v3 新增 `## 4. Unity updated UI 界面排版规格（v3 新增）`，覆盖以下内容。

### 3.1 布局目标

updated UI 的目标是把原本偏调试面板的界面，调整成更像娱乐场景的牌桌：

- 顶部状态栏、右侧 playing log、按钮区保留原功能。
- AI players 的牌面显示整体向下移动，为头像留空间。
- main player 的弃牌区和手牌区整体向下移动同样距离。
- AI avatar 放在对应 AI 牌面上方。
- player avatar 放在 main player 牌面左侧。

### 3.2 坐标规格

v3 明确了新增布局常量：

| 参数 | 当前值 | 语义 |
|---|---:|---|
| `BOARD_VERTICAL_SHIFT` | 145 | 牌桌整体下移距离 |
| `AI_AVATAR_WIDTH` / `AI_AVATAR_HEIGHT` | 110 / 140 | AI avatar 显示框 |
| `AI_AVATAR_GAP_BELOW` | 20 | AI avatar 与 AI 牌区之间的间距 |
| `PLAYER_AVATAR_X` / `PLAYER_AVATAR_Y` | 30 / `PLAYER_HAND_Y + 20` | player avatar 左上角 |
| `PLAYER_AVATAR_WIDTH` / `PLAYER_AVATAR_HEIGHT` | 145 / 145 | player avatar 显示框 |

受统一下移影响的区域：

- `AI_TABLE_Y`
- `PLAYER_TABLE_Y`
- `PLAYER_HAND_Y`
- `PLAYER_HAND_TOP_INDENT`

### 3.3 Avatar 资源规格

v3 新增 avatar 资源规范：

```
UnityVersion/Assets/StreamingAssets/avatars/
```

命名约定：

- `ai_*.png`
- `player_*.png`

加载逻辑：

1. 优先读 `Application.streamingAssetsPath/avatars`。
2. Editor 开发阶段 fallback 到 repo 根目录 `avatars/`。
3. AI avatar 每次启动随机选最多 3 张。
4. player avatar 每次启动随机选 1 张。
5. 随机选择发生在 `TileImageLoader` 初始化阶段，避免 UI redraw 时头像闪烁。

### 3.4 Avatar 裁剪与绘制

v3 明确 avatar 采用居中裁剪：

- 源图过宽：裁掉左右边缘。
- 源图过高：裁掉上下边缘。
- 裁剪后固定尺寸绘制，不做留白。

绘制顺序也被规格化：

1. 区域高亮与边框。
2. avatar。
3. playing log。
4. 顶部状态栏与按钮。
5. 牌面。
6. reward 动画。

## 4. 验收标准变化

v3 新增 UI 验收点：

- 三个 AI 头像显示在对应 AI 牌区上方。
- player 头像显示在主玩家牌区左侧。
- 头像不遮挡状态栏、日志窗口、手牌、弃牌区或按钮。
- 新局、窗口缩放、手动/Auto/LLM 模式切换时头像不闪烁、不重新随机、不影响点击。
- 缺失头像资源时只 warning，不影响游戏可玩性。
- Core/Tools 对拍结果不应因 UI 变化改变。

## 5. 非变更项

以下内容在 v3 中没有改变：

- 四川麻将规则。
- 缺一门胡牌校验。
- 碰/杠/胡/过状态机。
- Probability AI 的 `calc` / `out_ai` 算法。
- Auto Play 和 LLM Play 决策链。
- LLM API key、endpoint、fallback 机制。
- ConsoleHarness 的 tests / simulate / parity。
- 胜利 reward 的 TikTok URL 与美元动画语义。

