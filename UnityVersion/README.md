# Sichuan Mahjong — Unity Version

四川麻将的**纯 Unity / C# 重写版**。整个游戏(规则引擎、概率 AI、UI、Auto Play / LLM Play 模式)
都从 Java(`MacVersion/`)逐行移植为 C#,不再依赖 Java、JRE 或 Python。

- 目标 Unity 版本:**2022.3 LTS**(在 Unity 6 上也可打开,Hub 会提示切换版本)
- 平台:macOS / Windows / Linux(Editor 内直接 Play,或 Build 出独立应用)

## 在 Mac 上打开

1. 安装 [Unity Hub](https://unity.com/download),通过 Hub 安装 Unity **2022.3 LTS**(任意补丁号均可)。
2. Unity Hub → `Add` → 选择本目录(`UnityVersion/`)。
3. 打开工程后直接按 **Play**(任意场景都可以,`Assets/Scenes/Main.unity` 是缺省场景;
   入口代码通过 `RuntimeInitializeOnLoadMethod` 自举,不依赖场景里的任何对象)。
4. 首次 Play 会先显示 "Loading probability tables..."(解析 87MB 概率表,几秒钟),然后开局。

## 玩法(与 Java 版一致)

- 轮到你时点击手牌出牌;有动作时点 `Hu` / `Chow` / `Pung` / `Kong` / `Skip` 按钮,
  或用快捷键 `H` / `C` / `P` / `K` / `S`。
- `Auto Play`:概率 AI 代打(本地,无需网络)。
- `LLM Play`:LLM 代打,需要环境变量(与 Java 版相同):
  - `GPT_API_SG_KEY`(必需)
  - 可选:`MAHJONG_LLM_MODEL`、`MAHJONG_LLM_ENDPOINT`、`MAHJONG_LLM_API_VERSION`、
    `MAHJONG_LLM_REASONING`、`MAHJONG_LLM_TIMEOUT_SECONDS`
  - 注意:macOS 上 GUI 应用不继承 shell 环境变量;要在 Editor 里用 LLM Play,
    请从终端启动 Unity Hub/Unity(例如 `open -a "Unity Hub"` 前先 `export GPT_API_SG_KEY=...`),
    或使用 `launchctl setenv GPT_API_SG_KEY ...`。
  - 决策日志写在工程根目录 `llm_play_log.txt`。
- 你胡牌后可点 `Reward` 领奖励视频,`View winning hand` 查看胡牌牌型。

## 工程结构(模块化)

```
Assets/Scripts/
├── Core/                        # 纯 C# 规则引擎,零 UnityEngine 依赖(asmdef 强制)
│   ├── SichuanMahjong.Core.asmdef   #   noEngineReferences: true
│   ├── Model/                   # 牌、牌组、玩家、日志(model.*)
│   ├── Game/                    # Game / GameTurn(application.game)
│   ├── Validation/              # 胡/碰/杠判定(application.game.validation)
│   ├── Gameplay/                # 决策模型、Auto/LLM 控制器(application.gameplay)
│   ├── AiModel/                 # IAI / ProbabilityAI(aimodel)
│   ├── Algorithm/               # majiang_algorithm 库的 C# 移植(运行时子集)
│   └── Utils/
├── UnityApp/                    # 表现层 MonoBehaviour(替代 Swing 的 gameframe)
│   ├── Bootstrap.cs             # 入口:相机/Canvas/事件系统 + 异步加载概率表
│   ├── GamePanelBehaviour.cs    # GamePanel 移植:主循环、按键、按钮、3 秒定时器
│   ├── UiFactory.cs / TileView.cs / TileImageLoader.cs
│   ├── WinningHandDialog.cs / RewardCelebration.cs / BeepPlayer.cs
│   └── Config.cs                # 1920x960 逻辑坐标系里的布局常量
└── StreamingAssets/             # 牌面 PNG、背景、音效、概率表(87MB)
Tools/ConsoleHarness/            # 无 Unity 环境下验证 Core 的 .NET 控制台工具(不参与 Unity 构建)
```

## 与 Java 版的对应关系与差异

| Java | Unity 版 |
|---|---|
| Swing `GamePanel`/`Drawer`/`KeyHandler` | 代码生成的 uGUI(`GamePanelBehaviour` + `UiFactory`) |
| `com.github.esrrhs:majiang_algorithm` 依赖 | `Core/Algorithm/` 内的 C# 移植(仅运行时路径;离线建表工具 `gen()` 未移植) |
| `scripts/llm_play.py` Python 桥 | `LlmPlayController` 原生 HTTP(HttpClient),同一端点/协议/环境变量 |
| 动画 GIF 奖励图标 | 预抽帧 PNG 序列(`img/reward/dollar_frame_*.png`)循环播放 |
| `Toolkit.beep()` | 运行时合成的短促提示音 |
| `probability/` 目录(运行目录) | `Assets/StreamingAssets/probability/` |

规则引擎为**逐行移植**,并保留了原版的全部行为特性(包括 `Double.MIN_VALUE`、
`HashSet` 组合生成等边角语义)。验证方式见下节。

## 验证(已在无 Unity 的 Linux 上完成)

`Tools/ConsoleHarness` 用 .NET 8 直接编译 `Assets/Scripts/Core`:

```bash
cd Tools/ConsoleHarness
dotnet run -- ../../Assets/StreamingAssets/probability tests        # 三个 Java 测试的移植
dotnet run -- ../../Assets/StreamingAssets/probability simulate 100 # 100 局全 AI 对局
dotnet run -- ../../Assets/StreamingAssets/probability parity h.txt # 与 Java 版对拍
```

已验证:
- **300 组随机手牌对拍**:C# 与 Java 在出牌选择、碰/杠决策、玩家状态判定上 **300/300 完全一致**;
- **100 局全自动对局**:0 异常,胜/流局分布正常,四个座位均有胜场。
