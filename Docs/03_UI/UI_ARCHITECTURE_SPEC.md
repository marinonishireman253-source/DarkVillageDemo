# DarkVillageDemo UI 整体架构规范

文档状态: `当前生效`
建议用途: `正式 UI 架构、迁移路线和视觉方向参考`
不承担职责: `不替代项目总览，也不是给外部生成模型的投喂稿`

版本: `v1`
适用项目: `DarkVillageDemo`
适用场景: `Main`
当前阶段: `正式运行时 UI 已落地，继续扩展与清理`

## 1. 文档目的

这份文档用于为当前项目建立统一的 UI 架构，解决以下问题:

- 当前 UI 已经能支撑完整垂直切片，但文档需要跟上实际架构
- 各 UI 模块已经有视觉共性，但还没有统一的层级、状态和数据通路
- 后续如果继续叠加章节、菜单、背包、地图、角色信息、设置页，会很容易失控
- 需要一套既符合项目气质，又适合 Unity 正式落地的 UI 骨架

这份规范的目标不是立刻把所有 UI 重做完，而是先确定:

- UI 视觉方向
- UI 分层和 Canvas 架构
- UI 状态切换规则
- UI 模块职责边界
- 从当前原型 UI 迁移到正式 UI 的顺序

## 2. 当前项目 UI 现状审计

以下内容是当前项目中已经验证的事实:

- UI 主要由运行时脚本自动创建，入口在 `Assets/Scripts/GameBootstrap.cs`
- 正式 UI 根装配入口为 `Assets/Scripts/UI/Core/UiBootstrap.cs`
- 当前主要 UI 组件包括:
  - `SimpleDialogueUI`
  - `InteractionPromptUI`
  - `QuestTrackerUI`
  - `HudCanvasView`
  - `DialogueChoiceUI`
  - `PortraitController`
  - `ChapterCompleteOverlay`
  - `FloorSummaryPanel`
  - `InventoryCanvasView`
  - `RuntimeDiagnostic`
- 主游戏 UI 已经主要使用 `UGUI + TextMeshPro + CanvasGroup`
- UI 当前已有统一的 `Canvas` 分层、`UiTheme` 和状态协调器
- UI 视觉默认值已开始抽到 `UiTokens`，原子组件入口统一收敛到 `UiComponentCatalog`
- 现有 UI 视觉上已经存在稳定共性:
  - 深色半透明背景
  - 暖金色强调色
  - 偏克制的文字信息密度
  - 对话框位于底部
  - 状态块主要分布在屏幕边缘，尽量不遮挡房间中心

以下内容是对现状的判断:

- 当前主 UI 架构已经不应再被描述为 `OnGUI 原型`
- `OnGUI` 仍可保留给诊断和临时调试用途
- 当前最宝贵的仍然是已经形成的视觉语气、统一主题和空间让位原则

## 3. UI 设计目标

这个项目的 UI 不应该做成高覆盖率、强占屏、强系统感的 RPG 面板，也不应该做成极简到信息不够的纯电影化 HUD。

最终 UI 应满足以下目标:

- 服务于 `室内 cutaway / dollhouse / single-room framing`
- 尽量把屏幕中央让给场景构图、角色、门框和怪物演出
- 保持低饱和、克制、阴郁、带一点旧宅感的视觉气质
- 在探索、对话、战斗之间切换时，UI 重心稳定，不让玩家迷失
- 所有信息都应有固定归属区，不临时乱飞
- 原型阶段和正式阶段应共用同一套布局思想

关键词:

- `边缘承载`
- `中心让景`
- `低噪声`
- `压抑但可读`
- `叙事优先`
- `局部金属暖光点缀`

## 4. 视觉方向

### 4.1 总体气质

推荐继续沿用当前方向，并正式命名为:

- `煤黑底`
- `旧铜金强调`
- `灰纸白文本`
- `轻雾感半透明层`

UI 看起来应该像“旧宅里的说明牌和记录纸片被整理成了现代界面”，而不是科幻 HUD，也不是传统奇幻 RPG 金边大框。

### 4.2 色彩令牌

推荐先固定以下基础色:

- `UI/Base/Charcoal = #0D1014`
- `UI/Base/Slate = #1E232A`
- `UI/Text/Primary = #EEE8DD`
- `UI/Text/Secondary = #BFC3C6`
- `UI/Accent/Brass = #C8A56E`
- `UI/Accent/DimBrass = #8C7350`
- `UI/Success/Moss = #516546`
- `UI/Warning/Amber = #C98B4A`
- `UI/Danger/Ember = #A94E3D`
- `UI/Shadow/Fog = rgba(0,0,0,0.35~0.55)`

说明:

- 平时以 `Charcoal + Slate + Primary Text` 为主
- 可交互、当前聚焦、说话者标签使用 `Brass`
- 成功或完成状态才使用 `Moss`
- 危险和受击只在短时提示中使用 `Ember`

### 4.3 字体策略

正式 UI 推荐统一使用 `TextMeshPro`，并以 serif 系字体为主，不再使用偏现代或科幻感强的无衬线体作为主界面正文。

推荐字体:

- 英文标题/角色名:
  - `EB Garamond`
  - `Libre Baskerville`
- 中文标题/正文:
  - `Source Han Serif / 思源宋体`
  - `方正清刻本悦宋`（若项目环境具备授权与安装）
- 英文正文回退:
  - `Baskerville`
  - `Times New Roman`
  - `Georgia`

字体原则:

- 角色名、章节名、对话正文统一保持旧印刷机与宗教文书感
- 正文允许更克制，但仍保持 serif 的历史厚重感
- 不在同一面板里混用过多字重或切回现代 sans-serif
- `TMP` 材质禁用 `Soft Shadow / Underlay`
- 仅保留轻微 `Face Dilate`，模拟墨水在旧纸或木板上的轻度晕染

#### Dialogue / Portrait 排版令牌

- `Dialogue Frame = 1600 x 420`
- 内边距: 上下 `60px`，左右 `100px`
- 说话人姓名: 左上对齐，`42pt`，英文全大写，`Character Spacing = +5`
- 正文: 姓名下方 `30px`，`36pt`，`Line Height ≈ 140%`，颜色使用 `WaxWhiteText` 的 `90%` 透明度
- `Portrait Frame = 360 x 360`
- 头像图像外留 `20px` 深石板灰内衬，再接黄铜边框，形成嵌入式相框层次

#### Interaction / Marker / Modal 排版令牌

- `Interaction Prompt = 780 x 180`
- 底部居中悬浮，不侵占屏幕中心与下中区域
- `Keycap Badge`: 深石板灰正方牌，细黄铜线边框，字体沿用 serif，按键字符使用 `WaxWhite`
- 提示文本紧邻按键牌右侧，主提示使用 `UiTheme.MossGreen`
- `Marker Chip = 220 x 80`
- 目标标记保持极简，仅保留暗色底板、轻微烟熏边缘与极小黄铜菱形 / 十字图标
- `Modal Frame = 1200 x 760`
- 模态中心区域保持大留白，边角比对话框更厚重，允许极轻的深色布纹 / 羊皮纸乘算纹理
- `Primary Button = 480 x 120`
- 主按钮使用灰黑漆木底与完整细黄铜边框，文字为 `WaxWhite`
- 次按钮不做明显底板，作为低存在感离开 / 放弃动作，仅保留微弱文字与 hover 余烬线
- Hover 不做整体发光，而是在按钮文本下方渐显 `UiTheme.EmberRed` 细线，Pressed 时底板加深

#### 9-Slice 资源落地规范

- UI 切图统一放入 `Assets/Resources/UI/Slices/`
- 运行时默认资源名:
  - `ModalFrame`
  - `DialogueFrame`
  - `PortraitFrame`
  - `ChoicePanel`
  - `CombatPanel`
  - `CombatEmberGlow`
  - `InteractionPrompt`
  - `KeycapBadge`
  - `MarkerChip`
  - `PrimaryButton`
- 以上资源会由 `UiTheme` 自动尝试加载，无需先手动挂到 scene
- `Assets/Editor/UiChromeSpriteImporter.cs` 会自动把该目录下 PNG / 贴图按 UI Sprite 导入:
  - `Texture Type = Sprite (2D and UI)`
  - `Sprite Mode = Single`
  - `Mesh Type = Full Rect`
  - `MipMap = Off`
  - `Compression = Uncompressed`
  - `Wrap Mode = Clamp`
- 统一支持文件名后缀 `_bNN` 指定 border，例如:
  - `ModalFrame_b80.png`
  - `DialogueFrame_b64.png`
  - `CombatPanel_b56.png`
- 若不写 `_bNN`，导入器使用默认边框:
  - `ModalFrame = 80`
  - `DialogueFrame = 64`
  - `PortraitFrame = 48`
  - `ChoicePanel = 40`
  - `CombatPanel = 56`
  - `InteractionPrompt = 40`
  - `KeycapBadge = 18`
  - `MarkerChip = 20`
  - `PrimaryButton = 24`
- 复杂花纹必须只落在 border 区域，中心区只保留可拉伸的深色底板，不烘焙文字

#### Ember 呼吸灯实现约定

- 余烬呼吸脚本位于 `Assets/Scripts/UI/Effects/EmberBreathingEffect.cs`
- 战斗 HUD 会在 `CombatPanel` 内自动创建一层 `CombatEmberGlow`
- 若存在 `CombatEmberGlow` 资源，会优先使用该 Sprite；否则退回程序生成的红色雾层
- 常态呼吸建议 `speed ≈ 1.35`，`alpha ≈ 0.06 ~ 0.22`
- 紧张态通过 `SetPanicMode(true)` 提升节奏，不额外做廉价闪烁

### 4.4 运动语言

UI 动画不应跳脱，不应活泼弹出，应偏克制和“吸进雾里又浮出来”的感觉。

推荐动效:

- 面板出现: `120ms ~ 180ms` 的透明度渐入 + 6 到 12 像素位移
- 任务更新: 顶部轻推入，停留，再慢慢淡出
- 选项切换: 仅做弱发光和底色推进
- 受击反馈: 短促暗红 vignette，不做夸张屏幕闪白
- 章节完成: 全屏暗化后再抬出模态框

## 5. UI 技术选型结论

### 5.1 正式 UI 方案

推荐正式 UI 主体使用:

- `UGUI + TextMeshPro + CanvasGroup`

不推荐把主游戏 UI 主体继续建立在 `OnGUI` 上。

### 5.2 选择理由

选择 `UGUI` 的原因:

- 当前项目已经是典型的游戏内 HUD / 对话 / 提示 / 模态框结构
- 需要稳定的层级、锚点、分辨率适配和 prefab 化
- 需要和世界空间标记、角色立绘、黑幕过场、按钮交互顺畅协同
- Unity 内这类项目用 `UGUI` 的维护成本最低

不优先选择 `UI Toolkit` 作为主方案的原因:

- 当前需求更偏游戏 HUD 而不是工具型界面
- 世界空间提示、过场遮罩、逐层模态和运行时编排，用 `UGUI` 更直接
- 团队当前代码与思路已经更接近 `MonoBehaviour + runtime bootstrap`

保留 `OnGUI` 的唯一建议用途:

- `RuntimeDiagnostic`
- 临时调试面板

### 5.3 当前原子组件入口

当前推荐把可复用 UI 原子组件统一收敛到:

- `Assets/Scripts/UI/Core/UiTokens.cs`
- `Assets/Scripts/UI/Core/UiComponentCatalog.cs`

其中:

- `UiTokens` 负责颜色 / 间距 / 字号等默认令牌
- `UiComponentCatalog` 负责 `PrimaryButton / SecondaryButton / ModalPanel / DialogueFrame / 标题文本 / 正文文本` 的统一创建入口
- 新生成的 9-slice 资源继续从 `Assets/Resources/UI/Slices/` 走 `UiTheme` 解析，不在业务 View 里直连资源路径
- 开发期开关工具

## 6. 整体 UI 分层架构

正式 UI 建议建立一个持久化根节点:

- `UIRoot`

其下统一管理以下层级:

1. `Canvas_Backdrop`
   - 全屏暗化
   - vignette
   - 受击闪层
   - 过场淡入淡出
2. `Canvas_HUD`
   - 常驻探索 HUD
   - 任务面板
   - 玩家状态
   - 战斗状态
   - 交互提示
3. `Canvas_WorldMarkers`
   - 任务目标标记
   - 门口提示箭头
   - 关键交互物软定位
4. `Canvas_Dialogue`
   - 对话框
   - 角色名条
   - 立绘
   - 对话选项
5. `Canvas_Overlay`
   - 顶部通知
   - 自动保存提示
   - 获得物品提示
   - 章节标题
6. `Canvas_Modal`
   - 暂停菜单
   - 设置界面
   - 存档/读档
   - 章节完成弹窗
7. `Canvas_Debug`
   - 运行时诊断
   - 调试开关

关键原则:

- `Dialogue` 必须压过 `HUD`
- `Modal` 必须压过所有正常 UI
- `Debug` 可选择永远最上层，但仅开发环境开启
- `Backdrop` 不承载信息，只承载氛围和过渡

## 7. UI 状态模型

当前项目已经天然存在多种 UI 模式。正式架构建议显式定义状态，而不是让各面板各自猜当前是否该显示。

推荐状态:

- `Exploration`
- `InteractionFocus`
- `Dialogue`
- `Combat`
- `Reward`
- `ChapterComplete`
- `Loading`
- `Paused`

推荐规则:

- `Exploration`
  - 显示任务面板、玩家状态、弱提示
- `InteractionFocus`
  - 在 `Exploration` 基础上显示底部交互提示
- `Dialogue`
  - 隐藏交互提示
  - 压低战斗信息存在感
  - 显示对话框、立绘、选项
- `Combat`
  - 保留任务面板
  - 强化玩家和敌人状态显示
  - 保留必要交互反馈
- `ChapterComplete`
  - 全屏暗化
  - 锁定所有底层输入
  - 仅保留模态框交互
- `Paused`
  - 冻结世界输入
  - 保留暂停菜单及设置页

建议新增一个统一协调器:

- `UiStateCoordinator`

职责:

- 从 `DialogueRunner`、`CombatEncounterTrigger`、`QuestTracker`、`ChapterState`、`SaveSystem` 等系统读取状态
- 向各 UI presenter 广播当前 UI 模式
- 决定谁显示、谁让位、谁暂停动画

## 8. 布局架构

### 8.1 探索态布局

探索态建议保持“边缘承载、中央留空”。

布局建议:

- 左上:
  - 玩家生命
  - 当前武器或战斗键提示
- 右上:
  - 当前目标卡片
- 底部中央:
  - 交互提示
- 顶部中央:
  - 短时通知
- 世界空间:
  - 任务目标标记

原因:

- 屏幕中段通常是门框、角色和怪物可能出现的位置
- 右上目标卡不会压住玩家移动轴
- 底部中央交互提示与当前项目已形成的使用习惯一致

### 8.2 对话态布局

对话态是本项目最重要的 UI 场景，推荐作为正式视觉基准。

布局建议:

- 左下:
  - 角色立绘
- 底部:
  - 主对话框
  - 说话人姓名条
- 对话框上方偏右:
  - 选项列表
- 顶部中央:
  - 保留极轻量通知，不与对话抢主次

关键原则:

- 对话框不能太高，避免压住房间的中景结构
- 立绘不能占到中央，不然会破坏空间压迫感
- 选项尽量和对话框成一个区域，不要散到屏幕另一边

### 8.3 战斗态布局

战斗不应变成满屏 RPG 数值板，而应保持“探索中突发冲突”的感觉。

布局建议:

- 左上:
  - 玩家生命
  - 攻击冷却或操作提示
- 上方偏中:
  - 当前战斗标题
- 右上:
  - 敌人生命条
- 底部:
  - 只有在交互或战斗剧情需要时才出现额外信息

说明:

- 敌人状态不建议再放左上，避免和玩家信息抢区
- 如果敌人只有单体，优先做一条简短清晰的敌方状态条

### 8.4 模态和章节完成布局

章节完成、暂停、读档、设置等页面应统一进入 `Modal` 架构。

规则:

- 居中弹窗
- 背景全屏暗化
- 操作按钮结构固定
- 底部保留简短键位提示
- 同一风格适配暂停菜单、设置页、章节完成页

## 9. 模块职责拆分

推荐 UI 代码正式拆成以下层:

### 9.1 Bootstrap 层

- `UiBootstrap`
  - 创建 `UIRoot`
  - 创建 `EventSystem`
  - 挂载主 Canvas
  - 初始化主题、输入路由和状态协调器

### 9.2 State 层

- `UiStateCoordinator`
  - 统一维护当前 UI 模式
- `UiInputRouter`
  - 区分探索输入、对话输入、菜单输入

### 9.3 Theme 层

- `UiTheme`
  - 使用 `ScriptableObject`
  - 管理颜色、字体、间距、圆角、动画时长、阴影参数
- `UiAudioTheme`
  - 可选
  - 管理确认、切换、受击、完成等 UI 音效

### 9.4 Presenter 层

- `HudPresenter`
  - 驱动探索 HUD
- `DialoguePresenter`
  - 驱动对话框、名字、翻页、历史滚动
- `DialogueChoicePresenter`
  - 驱动选项列表
- `PortraitPresenter`
  - 驱动角色立绘显示
- `QuestPresenter`
  - 驱动目标面板和完成提示
- `CombatPresenter`
  - 驱动玩家与敌人状态
- `OverlayPresenter`
  - 驱动通知、自动保存、获得物品、章节标题
- `ModalPresenter`
  - 驱动暂停页、章节完成页、确认框
- `WorldMarkerPresenter`
  - 驱动任务目标和关键对象标记

### 9.5 View 层

每个 View 只负责:

- 挂引用
- 播放本地动画
- 刷新图像和文本
- 不直接读取游戏系统状态

View 不直接访问:

- `QuestTracker.Instance`
- `DialogueRunner.Instance`
- `CombatEncounterTrigger.ActiveEnemy`

这些依赖应由 presenter 统一读取后再下发。

## 10. 数据流建议

推荐 UI 数据流统一为:

`Gameplay System -> Presenter/Coordinator -> View`

不推荐:

`每个 View 自己去 FindObject / 自己查全局状态`

当前代码中已经存在较多 `FindFirstObjectByType` 和静态单例直连，这在原型阶段没问题，但正式 UI 应减少。

正式数据源建议如下:

- `QuestTracker` -> `QuestPresenter`
- `DialogueRunner` -> `DialoguePresenter`
- `CombatEncounterTrigger` + `PlayerCombat` -> `CombatPresenter`
- `SaveSystem` -> `OverlayPresenter`
- `ChapterState` -> `ModalPresenter`

世界空间标记建议单独由 `WorldMarkerPresenter` 统一处理，以避免每个系统各自做 `WorldToScreenPoint`。

## 11. 推荐的 UI 资源组织

建议新增以下目录结构:

- `Assets/Scripts/UI/Core`
  - `UIRoot.prefab`
  - `UiTheme.asset`
  - `UiBootstrap.cs`
  - `UiStateCoordinator.cs`
  - `UiTokens.cs`
  - `UiComponentCatalog.cs`
- `Assets/Scripts/UI/HUD`
  - `HudCanvas.prefab`
  - `QuestPanel.prefab`
  - `InteractionPrompt.prefab`
  - `CombatPanel.prefab`
- `Assets/Scripts/UI/Dialogue`
  - `DialogueCanvas.prefab`
  - `DialoguePanel.prefab`
  - `PortraitPanel.prefab`
  - `ChoiceList.prefab`
- `Assets/Scripts/UI/Overlay`
  - `ToastNotification.prefab`
  - `FloorSummaryPanel.cs`
  - `AshParlorChoiceOverlay.cs`
- `Assets/Scripts/UI/Inventory`
  - `InventoryCanvasView.cs`
  - `AutosaveToast.prefab`
  - `ChapterTitle.prefab`
- `Assets/UI/Modal`
  - `ModalCanvas.prefab`
  - `PauseMenu.prefab`
  - `ChapterCompleteModal.prefab`
- `Assets/UI/Debug`
  - `DebugCanvas.prefab`

脚本建议对应放在:

- `Assets/Scripts/UI/Core`
- `Assets/Scripts/UI/HUD`
- `Assets/Scripts/UI/Dialogue`
- `Assets/Scripts/UI/Overlay`
- `Assets/Scripts/UI/Modal`

## 12. 关键组件设计建议

### 12.1 对话框

应作为项目主视觉组件重点打磨。

要求:

- 底部横向长条
- 上层为名字条，下层为正文
- 支持多行、逐页、继续提示
- 支持显示立绘但不强制
- 支持有选项和无选项两种模式

### 12.2 任务面板

要求:

- 固定右上
- 默认只显示当前目标，不显示长列表
- 更新时支持短暂强化动画
- 完成时顶部出现横幅

### 12.3 交互提示

要求:

- 固定底部中央
- 小尺寸
- 始终只显示一个主要交互
- 与对话框位置兼容，不抢占同一区域

### 12.4 战斗信息

要求:

- 玩家状态与敌人状态分区
- 单场景轻战斗为主，不做复杂技能栏
- 键位提示只在必要时显示，不常驻堆满

### 12.5 世界空间标记

要求:

- 屏幕内跟随目标
- 超出屏幕边界时做软夹取，不突然消失
- 与场景风格统一，不做强荧光箭头

## 13. 与当前项目系统的对接方案

以下是基于当前代码的推荐映射:

- `SimpleDialogueUI` -> 未来拆为
  - `DialoguePresenter`
  - `DialoguePanelView`
- `DialogueChoiceUI` -> 未来替换为
  - `DialogueChoicePresenter`
  - `ChoiceListView`
- `PortraitController` -> 未来替换为
  - `PortraitPresenter`
  - `PortraitView`
- `QuestTrackerUI` -> 未来替换为
  - `QuestPresenter`
  - `QuestPanelView`
- `CombatHud` -> 未来替换为
  - `CombatPresenter`
  - `CombatHudView`
- `InteractionPromptUI` -> 未来替换为
  - `InteractionPromptPresenter`
  - `InteractionPromptView`
- `ChapterCompleteOverlay` -> 未来替换为
  - `ModalPresenter`
  - `ChapterCompleteModalView`

以下组件建议暂时保留:

- `DialogueRunner`
- `QuestTracker`
- `CombatEncounterTrigger`
- `SaveSystem`
- `ChapterState`

也就是说:

- 先替换 UI 表现层
- 不先重写玩法系统

## 14. 从当前原型迁移到正式 UI 的路线

推荐分四步进行。

### 第一步: 建立正式 UI 根节点

目标:

- 引入 `UIRoot`
- 引入多 Canvas 结构
- 引入 `UiTheme`
- 保留旧 `OnGUI` UI 不动

结果:

- 新旧 UI 可以并行
- 不打断当前玩法验证

### 第二步: 先替换对话链路

优先级最高替换:

- `SimpleDialogueUI`
- `DialogueChoiceUI`
- `PortraitController`

原因:

- 对话是当前项目最核心的 UI 气质来源
- 这部分最能建立正式视觉基准

### 第三步: 替换 HUD 与提示

替换:

- `QuestTrackerUI`
- `InteractionPromptUI`
- `CombatHud`
- 世界空间目标标记

结果:

- 核心常驻 HUD 完整进入正式架构

### 第四步: 替换模态和开发外壳

替换:

- `ChapterCompleteOverlay`
- 暂停菜单
- 设置页
- 存档/读档页面

保留:

- `RuntimeDiagnostic` 可以继续使用 `OnGUI`

## 15. 最终推荐结论

对于 `DarkVillageDemo`，最合适的整体 UI 架构不是“大而全的 RPG 菜单系统”，而是:

- 以 `Dialogue + Exploration HUD + Modal` 三大主轴构成
- 以 `UGUI + TextMeshPro + Presenter/View` 为正式实现方式
- 以 `UiStateCoordinator` 管理显示状态
- 以 `UiTheme` 统一风格令牌
- 以“边缘承载、中央让景”作为所有布局决策的第一原则

一句话总结:

这个项目的 UI 应该像一层温和、压抑、很懂分寸的空气，贴着画面边缘陪伴玩家，而不是一堵盖住场景的系统墙。

## 16. 当前实现建议

如果按推荐顺序继续做，下一步最值得开始实现的是:

1. `UIRoot + Canvas 分层`
2. `UiTheme`
3. `正式版 Dialogue UI`

原因:

- 这是最能快速建立项目整体质感的一组基础设施
- 也是最容易从当前原型里平滑迁移的一组模块
