# AI Coding Test Data Generation - Plan 01

## 1. 目标与边界

本计划将 Unity 版四川麻将工程转化为一组可复现的 AI Coding benchmark 数据点。

每个数据点包含：

- 一个可由 Unity Hub 打开的 Unity 工程状态；
- 与该状态严格对应的任务说明（PRD）；
- 用于验证 AI 实现结果的验收标准；
- 描述基线、移除模块、文件清单和验证方式的 manifest。

当前只做 **one-hop** 任务：从完整、可运行的基线 `data_point_0000` 中移除一个独立模块 `M_x`，同时提供只要求重建该模块的 `PRD_x.md`，形成待修复输入 `data_point_x`。AI Coding 的第一次输出是 `data_point_x_gen`：它是 AI 在 `data_point_x` 上实现 `M'_x` 后的原始生成产物。愿景是该产物能恢复为可独立打开和运行的 Unity 工程，但这不是生成阶段的既定事实；是否能编译、进入 Play Mode 和复现目标行为，属于后续独立的数据标注/评测阶段。不在本轮构造多模块同时缺失、跨版本迁移或开放式产品设计任务。

本轮实际交付只到 Phase 1：生成并检查 `data/PRD_all.md` 和 `data/data_point_0000/`。在人工确认前，不生成任何 `data_point_0001+`。

## 2. 数据源、知识源与输出约定

### 2.1 数据源

- 源工程：`UnityVersion/`
- Unity 版本：以 `UnityVersion/ProjectSettings/ProjectVersion.txt` 为唯一准则（当前预期为 Unity `2022.3.62f3`）。
- 复制后工程：`data/data_point_0000/`

### 2.2 知识源

构造 PRD 时按以下优先级使用信息：

1. `annotation/SichuanMahjong_PRD_v2.md`：产品目标、规则、模式、非功能要求和风险；
2. `annotation/SichuanMahjong_模块功能规格文档_v3.md`：模块边界、接口、实现依赖与验收要点；
3. `UnityVersion/` 源码：文档与实际代码出现不一致时，以实际代码行为为准，并在 PRD 的“实现事实”中注明；
4. 无 `annotation/` 或其中没有可用文档时，仅根据源代码生成 PRD，并把“知识源缺失”记录在 manifest。

### 2.3 目录与命名

```text
data/
  PRD_all.md
  data_point_0000/
    Assets/
    Packages/
    ProjectSettings/
    Tools/
    README.md
    DATA_POINT_MANIFEST.md
    verify_baseline.sh
  PRD_1.md                 # Phase 2 起生成
  data_point_0001/         # Phase 2 起生成
    ...
    DATA_POINT_MANIFEST.md
  data_point_0001_gen/     # AI Coding 首次输出；不由数据构造步骤预先提供
    ...
    GENERATION_MANIFEST.md
    EVALUATION.md           # 后续数据标注产物；初始状态为 unlabeled
```

`data/` 保持在 `.gitignore` 中。它是本地生成的 benchmark 工作区，不应因运行日志、Unity 缓存或未来测试数据而推送到远端。

## 3. 统一数据点契约

每一个数据点必须满足以下约束。

### 3.1 运行状态约定

- `data_point_0000` 是唯一要求在数据构造完成时即可由 Unity Hub 独立打开并进入 Play Mode 的基线；
- `data_point_x` 是故意移除 `M_x` 的待开发输入。它**不要求**可由 Unity Hub 独立打开并运行，也可以因目标模块缺失而有编译错误、启动错误或缺失功能；
- `data_point_x_gen` 是 AI Coding 根据 `data_point_x + PRD_x` 首次生成的原始结果，即 `data_point_x + M'_x`。它可能仍有 C# 编译错误、Unity 启动错误或功能偏差；
- `data_point_x_gen` 只有经后续数据标注确认 Unity Hub 可独立打开、通过编译并进入 Play Mode 后，才被标记为“运行验证通过”；
- 无论输入或输出，项目都应携带 `Packages/`、`ProjectSettings/` 和所需资源，不得依赖原仓库目录；首次打开允许 Unity 在自身目录重新生成 `Library/`；
- 所有运行时资源均应位于相应数据点的 `Assets/StreamingAssets/` 中。

### 3.2 最小而完整的 Unity 工程复制规则

复制基线时保留：

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Tools/`
- `README.md`
- 任何 Unity 项目必需的根目录配置文件

不复制 Unity 可再生或机器相关目录：

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- IDE 缓存、构建产物、运行日志

复制后必须确认所有 `.meta` 文件与对应资源成对存在，特别是场景、StreamingAssets 图片、音效、概率表与头像素材。否则 Unity GUID 引用可能失效。

### 3.3 Manifest 与生成记录格式

每个 `DATA_POINT_MANIFEST.md` 至少包含：

```markdown
# Data Point Manifest

- Data point id: data_point_0000
- Parent: none / data_point_0000
- Unity version: 2022.3.62f3
- Source revision: <git commit SHA>
- Knowledge sources: <files actually used>
- Missing module: none / Mx
- Related PRD: ../PRD_all.md / ../PRD_x.md
- Copy policy: minimal Unity project, generated directories excluded
- Validation status: pending / passed / failed
- Validation record: <date, Unity version, operator, result>
```

`Source revision` 固定为生成时 `UnityVersion/` 所在 Git commit，确保任何数据点都可以追溯。

每个 AI 首次输出 `data_point_x_gen` 应额外保留 `GENERATION_MANIFEST.md`，至少包含：

```markdown
# Generation Manifest

- Input data point: ../data_point_000x
- Related PRD: ../PRD_x.md
- AI coding system/model: <name and version>
- Generation command or session reference: <reproducible reference>
- Generation timestamp: <ISO-8601 timestamp>
- Output scope: data_point_x + M'_x
- Evaluation status: unlabeled
```

后续标注在 `EVALUATION.md` 中记录，至少包括 Unity 版本、编译结果、Play Mode 结果、模块功能验收结果、错误摘要和最终标签（例如 `compile_failed`、`play_mode_failed`、`functional_failed`、`passed`）。生成记录与评测记录必须分离，避免把“AI 已生成”误解为“AI 已通过”。

### 3.4 生成与数据标注层次

每个 benchmark 任务分为三个阶段：

- **输入数据点检查**：确认 `data_point_x` 的缺失范围只对应 `M_x`，不存在答案泄漏；它不以可编译或可运行作为通过条件；
- **AI 首次生成**：AI Coding 产出 `data_point_x_gen`，记录模型、提示词/会话引用、时间和文件变化；此阶段只确认有产物，不做成功判定；
- **后续数据标注/评测**：对 `data_point_x_gen` 进行下面三层检查，并写入 `EVALUATION.md`。

1. **工程层**：Unity Hub 能打开，Console 无 C# 编译错误；
2. **启动层**：打开 `Main` 场景并进入 Play Mode，主界面显示；
3. **任务层**：在 `data_point_x_gen` 中，预设验收场景或脚本验证 `M'_x` 是否复现 `M_x` 的目标行为，且缺失模块之外的基线行为未被破坏。

## 4. 模块目录与 PRD_all 范围

`PRD_all.md` 以当前模块规格文档为基础，列出以下 9 个模块。M9 是 v3 新增 UI 排版规格，适合作为独立视觉/UI 重建任务，因此应纳入总 PRD。

| ID | 模块 | 基准范围 | one-hop 适合度 |
| --- | --- | --- | --- |
| M1 | 牌与编码 | Tile Domain & Codec | 高 |
| M2 | 胡牌与状态判定 | Hu / Pung / Kong Validator | 高，但规则验收要求严格 |
| M3 | 查表算法层 | Probability Table & Scoring | 中，需要保留概率表资源 |
| M4 | 概率 AI 玩家 | ProbabilityAI | 高 |
| M5 | 对局状态机 | Game Engine | 低，依赖面最大，留到后期 |
| M6 | 合法动作上下文 | PlayerActionContext | 高 |
| M7 | 代打控制器 | AutoPlayController / LlmPlayController | 中，建议拆成 M7a/M7b 子任务 |
| M8 | 无头模拟器 | Console Harness | 高 |
| M9 | Unity updated UI 排版 | 位置、头像、裁剪和绘制 | 高，视觉验收清晰 |

### 4.1 PRD_all 的固定章节

`data/PRD_all.md` 必须包含：

1. benchmark 目标与使用方法；
2. Unity 工程运行前提与目录结构；
3. 游戏规则和三种运行模式（玩家、Auto Play、LLM Play）；
4. M1-M9 的模块卡片：职责、输入/输出、依赖、关键文件、运行时资源、非目标；
5. 各模块的验收标准；
6. 明确的实现事实与限制，例如 ProbabilityAI 的弃牌评分只观察手牌，当前不利用桌面可见牌或剩余张数；
7. 数据点构造规范与后续 `PRD_x` 模板。

它不是源代码逐行注释，也不要求 AI 复写整个项目；它是后续单模块重建任务的索引和事实基线。

## 5. 衍生数据点的构造规则

### 5.1 PRD_x 的职责

`PRD_x.md` 只描述目标模块 `M_x` 的产品/功能要求和恢复计划，默认其余模块正确可用。它必须避免把答案直接粘贴进文档。

每份 `PRD_x` 使用固定模板：

1. 任务背景与唯一目标；
2. 不可修改的外部契约；
3. 功能需求与边界情况；
4. 依赖模块可提供的能力；
5. 需要创建/补全的文件范围；
6. 运行时资源要求；
7. 验收用例（正常、边界、失败路径）；
8. 非目标与禁止事项；
9. 完成定义（Definition of Done）。

### 5.2 “移除模块”的实现原则

构造 `data_point_x` 时，可以直接移除实现，也可以为了把任务聚焦在目标模块而采用最小兼容措施。它不需要可独立运行；关键是缺失范围清楚，且 AI 无法从数据点中直接复制答案。可选择以下策略：

- **直接缺失方式**：删除目标实现或核心文件；允许 `data_point_x` 出现与 `M_x` 直接相关的编译/运行错误；
- **接口桩方式（可选）**：保留类名、命名空间、公共方法签名与必要类型，但把实现替换为显式 `TODO`/`NotImplementedException` 或确定的占位行为；
- **编译隔离方式（可选）**：当模块位于可独立的 assembly definition 中，可隔离该程序集以降低无关错误；
- **场景替换方式（可选）**：UI/素材类模块可保留场景入口，但移除对应渲染/布局逻辑，使目标功能明显缺失。

具体采用方式必须写在该数据点的 manifest，且 PRD 必须只要求恢复被移除的功能。

### 5.3 防止答案泄漏

衍生点不得保留可直接恢复 `M_x` 的副本，包括：

- 原模块同名备份文件；
- 可被直接复制的旧实现；
- 包含完整实现的测试脚本、日志或注释；
- 从 `Library/`、IDE 历史、构建产物间接带入的程序集。

PRD 可以写清接口和行为，不写具体算法代码、完整条件分支或隐藏答案。对 M3 等表驱动模块，可保留必要数据表资源，但不得保留算法实现副本。

### 5.4 推荐构造顺序

为减少依赖风险，建议按以下顺序逐个生成、审核与验证：

1. M9：Unity updated UI 排版；
2. M8：Console Harness；
3. M6：PlayerActionContext；
4. M4：ProbabilityAI；
5. M1：Tile Domain & Codec；
6. M2：Validator；
7. M7a：AutoPlayController；
8. M7b：LlmPlayController；
9. M3：查表算法层；
10. M5：Game Engine。

这只是建议顺序，不代表模块编号。每次只推进一个数据点，审核通过后再处理下一个。

## 6. Phase 1：生成基线 data_point_0000 和 PRD_all

### 6.1 前置检查

1. 确认工作树状态，记录生成时 Git commit；
2. 读取 `ProjectVersion.txt`，记录精确 Unity 版本；
3. 确认 `data/` 在 `.gitignore` 中；
4. 检查 `UnityVersion/` 是否已有未提交的必需改动。若有，manifest 必须写明基线取自工作树而非纯 commit；
5. 枚举 Assets、StreamingAssets、Packages、ProjectSettings 和 Tools，确定复制清单。

### 6.2 创建 data_point_0000

1. 创建 `data/data_point_0000/`；
2. 按 3.2 的最小完整工程规则复制文件；
3. 写入 `DATA_POINT_MANIFEST.md`；
4. 写入 `verify_baseline.sh`，至少检查关键目录、`Main.unity`、`ProjectVersion.txt`、概率表和背景/牌面资源是否存在；
5. 在 Unity Hub 添加 `data/data_point_0000`，使用 manifest 中的 Unity 版本打开；
6. 打开 `Main` 场景，进入 Play Mode；
7. 检查基础玩家模式能发牌、打牌、AI 回合推进，且无脚本编译错误；
8. 将验证结果、Unity Console 结果和人工观察记入 manifest。

不复制 `Library/` 也不要求它在生成时存在；由 Unity 在数据点目录中独立生成。

### 6.3 编写 PRD_all

1. 以 v3 模块规格的 M1-M8 为主结构；
2. 把 v3 的 UI 排版规格整理为 M9；
3. 用 PRD_v2 补齐产品模式、规则和 LLM 环境约束；
4. 对每个模块标注真实代码位置和资源依赖；
5. 明确“规格”与“当前实现限制”的区别；
6. 加入 5.1 的单模块 PRD 模板，方便后续直接派生 `PRD_x`；
7. 最后由人工审阅 `PRD_all.md`，确认模块边界是否符合 benchmark 想测的 AI Coding 能力。

### 6.4 Phase 1 完成定义

Phase 1 只有同时满足以下条件才算完成：

- `data/PRD_all.md` 已生成，包含 M1-M9；
- `data/data_point_0000/` 可由 Unity Hub 独立打开；
- manifest 记录了来源 revision、Unity 版本和验证结果；
- 基线在 Play Mode 下可进入基本对局；
- 人工已审阅并批准 `PRD_all.md`。

## 7. Phase 2+：每个 one-hop 数据点的标准流程

在用户批准 Phase 1 后，对每个模块重复以下流程：

1. 选定一个模块 `M_x` 与其边界；
2. 从 `data_point_0000` 复制出 `data_point_x`；
3. 选择并实施最小移除策略；
4. 编写 `data/PRD_x.md`；
5. 搜索残留答案并确认没有可复制的实现；
6. 检查 `data_point_x` 中的缺失范围只对应目标模块，且不存在可复制的答案；不要求该输入工程可运行；
7. 由指定 AI Coding 系统根据 `data_point_x + PRD_x` 生成 `data_point_x_gen`，保留原始输出和 `GENERATION_MANIFEST.md`；此时不以编译或运行成功作为生成完成条件；
8. 在独立的数据标注/评测步骤中，对 `data_point_x_gen` 执行工程层、启动层和任务层检查，并写入 `EVALUATION.md`；
9. 由人工审阅 PRD_x、输入边界和标注结果；
10. 审批后才开始下一个 `x`。

## 8. 风险与处理原则

| 风险 | 处理原则 |
| --- | --- |
| Unity 复制后丢失资源引用 | 保留 `.meta`，不复制 Library，首次打开后检查 Console 和场景 |
| 模块删除导致输入工程不编译 | 这是允许的输入状态；记录错误范围。AI 首次输出不要求立即恢复成功，后续标注再记录实际编译/运行结果 |
| AI 首次输出仍无法运行 | 这是有效 benchmark 结果，不覆盖或丢弃原始 `data_point_x_gen`；在 `EVALUATION.md` 标记失败层级和错误摘要 |
| PRD 写得过于像答案 | 只给需求、契约和验收，不提供完整算法/实现代码 |
| 文档与代码不一致 | 以代码为准，在 PRD 中记录差异 |
| LLM Play 依赖密钥和 macOS GUI 环境 | 不把密钥写入数据点；将其标为可选集成验收，基线玩家/Auto Play 必须可离线验证 |
| data 被 Git 忽略后难以共享 | 把生成工具、规则和文档保留在仓库；数据点通过受控制品存储或压缩包另行分发 |

## 9. 本轮评审清单

在执行任何数据复制前，请确认以下设计选择：

- `data_point_0000` 采用“最小完整 Unity 工程”而非整个工作目录镜像；
- M9（updated UI 排版）作为独立 benchmark 模块纳入 M1-M9；
- `data_point_0000` 必须可独立打开运行；`data_point_x` 可以不运行；`data_point_x_gen` 是未判定的首次 AI 产物，只有后续数据标注通过后才可被认定为可独立打开运行；
- 根据模块特性选择直接缺失、接口桩、编译隔离或场景替换，不以输入工程是否编译作为构造成功标准；
- 每次只生成一个数据点，并在人审 PRD 后才推进下一点；
- `data/` 继续保持不进入 Git，代码仓库只保存生成规则、文档与工具。

批准后，下一步是只执行 Phase 1：创建 `data/PRD_all.md` 和 `data/data_point_0000/`，然后停下来等待审阅。
