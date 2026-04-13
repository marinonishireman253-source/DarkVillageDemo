# DarkVillageDemo / Ersarn 项目总览

文档状态：`项目总览`
建议用途：`交接 / 快速补上下文 / 给 ChatGPT 或 Notion 导入`
不承担职责：`不替代玩法总纲，不替代 UI 专题规范`

## 文档用途

这是一份为 `ChatGPT -> Notion` 导入准备的项目理解文档，目标是让外部协作者或后续 AI 在最短时间内理解这个项目目前的定位、结构、已实现系统与下一步方向。

适用对象：

- 新接手的开发者
- 需要帮忙整理知识库的 ChatGPT / AI 助手
- 需要快速了解项目现状的策划、设计或美术协作者

---

## 1. 项目一句话

`DarkVillageDemo`，代号 `Ersarn`，是一个基于 `Unity` 开发的 `2.5D 黑暗叙事 RPG` 原型项目。  
当前核心方向不是传统刷怪 ARPG，而是：

`单层闭环探索 + 房间压迫感 + 光暗控场 + 轻战斗反打 + 对话叙事推进`

玩家扮演 `伊尔萨恩`，在一座被记忆、灰烬与残响侵蚀的高塔中逐层上行。每一层应当是一个短时、完整、可验证的玩法单元。

---

## 2. 当前项目状态

### 当前阶段

- 当前里程碑：`M2 对话与剧情闭环`
- 当前目标：完成对话分支、立绘切换、剧情事件桥接、章节状态管理
- 当前可玩重点：`Main` 场景中的 `灰烬客厅（Ash Parlor）` 五房间体验切片

### 当前判断

这个项目已经不是“纯空白原型”，而是具备以下特征的可运行骨架：

- 有明确题材和美术气质
- 有可进入的主场景
- 有运行时自动生成的关卡骨架
- 有角色移动、攻击、交互和基础敌人
- 有正式化中的对话、任务、背包、UI 与存档系统

它现在更像一个 `正在从原型期进入可扩展垂直切片期` 的 Unity 项目。

---

## 3. 技术栈与工程信息

### 引擎与渲染

- 引擎版本：`Unity 6 / 6000.3.11f1`
- 渲染管线：`URP`
- UI 方向：`UGUI + TextMeshPro`

### 关键包

- `com.unity.render-pipelines.universal`
- `com.unity.inputsystem`
- `com.unity.ugui`
- `com.unity.timeline`
- `com.unity.ai.navigation`
- `com.unity.test-framework`

### 项目结构特点

- 当前只保留一个主场景：`Assets/Scenes/Main.unity`
- 多个核心系统通过 `GameBootstrap` 在运行时自动创建
- 室内 2.5D 切片空间由脚本动态搭建，而不是完全手工摆场景
- UI 也大量通过运行时脚本自动生成和接线

这意味着项目当前非常适合快速迭代，但也需要持续整理“运行时生成逻辑”和“正式场景资源化”的边界。

---

## 4. 项目定位与体验目标

### 核心体验

项目希望给玩家的体验不是“大地图刷图”，而是：

- 进入一个主题鲜明的楼层
- 很快理解这一层的主规则
- 在房间与镜头构图带来的压迫中行动
- 做出一次带代价的选择
- 通过本层挑战，获得叙事与系统反馈

### 关键词

- 黑暗叙事
- 高塔闯层
- 房间压迫
- 光暗局势控制
- 轻战斗
- 分支对话
- 单层闭环

### 当前最重要的设计结论

- 战斗不是主乐趣，而是“破局工具”
- 房间压力优先于单纯堆怪
- 光源应影响局势，而不是仅作为门锁开关
- 每层应有清晰的主目标和阶段反馈

---

## 5. 当前可玩内容概览

### 主场景

- 场景：`Main`
- 运行方式：进入场景后，由 `GameBootstrap` 自动补齐玩家、相机、UI、任务、对话、背包、环境骨架等系统

### 当前主要体验切片

当前可玩的内容可以理解为一个名为 `灰烬客厅（Ash Parlor）` 的五段式小关卡：

1. 落地房
2. 规则房
3. 压力房
4. 选择房
5. 终局房

这个结构已经和总设计稿中的“单层闭环模板”一致，说明项目正在把抽象设计落实为可玩的垂直切片。

### 当前玩法闭环

玩家在当前切片中的体验大致是：

1. 进入房间切片并开始横向探索
2. 与可交互对象对话或触发任务
3. 点亮第一盏烛台，打开新的推进路径
4. 进入压力房，处理敌人或压迫感事件
5. 在风险 / 保守之间做一次选择
6. 点亮第二盏烛台，解锁出口
7. 使用塔梯离开当前层段

---

## 6. 已实现系统盘点

下面是根据代码和现有文档整理出的“已落地系统”。

### 6.1 运行时总引导

入口：`Assets/Scripts/GameBootstrap.cs`

职责：

- 确保玩家存在
- 自动添加战斗与表现层组件
- 自动配置相机
- 自动创建任务、对话、背包、UI、音乐等系统
- 自动接入 `TowerInteriorSlice`
- 自动补环境效果，如地面湿痕、雨水禁用状态、积水区等

这是项目的总装配器，几乎是理解当前工程运行方式的第一入口。

### 6.2 场景与空间生成

核心文件：`Assets/Scripts/TowerInteriorSlice.cs`

职责：

- 构建当前主场景中的室内 cutaway / dollhouse 式 2.5D 空间
- 生成房间、前景遮挡、背墙、地板、灯光区、相机区
- 控制玩家出生点和房间镜头分区
- 将“落地房 / 规则房 / 压力房 / 选择房 / 终局房”组合成当前体验切片

这一系统体现了项目当前非常鲜明的特点：

- 玩家是横向移动的
- 空间是 3D 模块拼起来的
- 镜头接近正视，但带轻微俯角
- 画面重心是“单房间局部观察”，不是整栋建筑总览

### 6.3 玩家移动与交互

核心文件：`Assets/Scripts/PlayerMover.cs`

已实现能力：

- 横向移动
- 冲刺
- 交互目标检测
- 与可交互对象进行焦点切换
- 通过 Input System 或键盘回退输入工作

当前交互逻辑不是“点击物体”，而是典型的近距离聚焦交互：

- 玩家靠近目标
- 系统计算最优交互对象
- 显示交互提示
- 玩家按键触发

### 6.4 战斗系统

核心文件：

- `Assets/Scripts/PlayerCombat.cs`
- `Assets/Scripts/CombatantHealth.cs`
- `Assets/Scripts/SimpleEnemyController.cs`
- `Assets/Scripts/CombatVfxFactory.cs`

已实现能力：

- 玩家近战攻击
- 基础冷却与攻击判定
- 敌人受击与死亡
- 玩家生命值
- 死亡后重新加载当前体验
- 基础战斗 VFX

当前战斗是 `轻量级、节奏短、用于破局` 的设计，而不是复杂连招型系统。

### 6.5 交互系统

核心文件：

- `Assets/Scripts/IInteractable.cs`
- `Assets/Scripts/InteractableBase.cs`
- `Assets/Scripts/InteractionPromptUI.cs`
- `Assets/Scripts/DoorInteractable.cs`
- `Assets/Scripts/PickupInteractable.cs`
- `Assets/Scripts/InspectionInteractable.cs`
- `Assets/Scripts/DialogueInteractable.cs`

已实现能力：

- 统一交互接口
- 对象显示名与提示文本
- 门、拾取物、查看物、对话对象等交互类型
- 聚焦时高亮与提示文案切换

这说明项目已经从“临时硬编码交互”走向“统一交互协议”。

### 6.6 对话系统

核心文件：

- `Assets/Scripts/DialogueNode.cs`
- `Assets/Scripts/DialogueRunner.cs`
- `Assets/Scripts/DialogueChoiceUI.cs`
- `Assets/Scripts/DialogueUIBridge.cs`
- `Assets/Scripts/SimpleDialogueUI.cs`
- `Assets/Scripts/PortraitController.cs`
- `Assets/Scripts/DialogueEventSystem.cs`

已实现能力：

- `ScriptableObject` 数据驱动对话节点
- 多行文本
- 选项分支
- 节点跳转
- 进入节点时触发事件
- 立绘切换
- 对话与 UI 桥接

当前对话系统已经进入“正式化”阶段，不再只是测试字符串弹窗。

### 6.7 任务与章节状态

核心文件：

- `Assets/Scripts/QuestTracker.cs`
- `Assets/Scripts/QuestObjectiveTarget.cs`
- `Assets/Scripts/QuestTrackerUI.cs`
- `Assets/Scripts/ChapterStateManager.cs`
- `Assets/Scripts/TriggerZoneObjective.cs`

已实现能力：

- 当前目标记录
- 当前目标文本显示
- 目标完成状态
- 目标标记跟踪
- 章节标记与已收集物管理

当前任务系统属于 `最小但完整` 的状态，已经可以支撑“目标推进 -> 完成反馈 -> 指向下一个目标”。

### 6.8 背包与收集

核心文件：

- `Assets/Scripts/InventoryController.cs`
- `Assets/Scripts/InventoryItemCatalog.cs`
- `Assets/Scripts/UI/InventoryCanvasView.cs`
- `Assets/Scripts/PickupInteractable.cs`

已实现能力：

- 打开 / 关闭人物面板
- 人物页与背包页双页签
- 收集物进入背包
- 显示名称、分类和说明
- 面板打开时暂停世界时间

当前背包不属于复杂数值系统，更偏向：

- 叙事线索容器
- 收集记录页
- 人物与当前目标信息面板

### 6.9 UI 架构

核心文件：

- `Assets/Scripts/UI/Core/UiBootstrap.cs`
- `Assets/Scripts/UI/Core/UiFactory.cs`
- `Assets/Scripts/UI/Core/UiTheme.cs`
- `Assets/Scripts/UI/Core/UiStateCoordinator.cs`
- `Assets/Scripts/UI/HUD/HudCanvasView.cs`
- `Assets/Scripts/UI/Dialogue/DialogueCanvasView.cs`
- `Assets/Scripts/UI/Modal/ModalCanvasView.cs`
- `Assets/Scripts/UI/InventoryCanvasView.cs`

当前方向：

- 从早期 `OnGUI` 原型，逐步过渡到正式 `Canvas + TMP` 架构
- UI 仍保留运行时创建特征，但已经开始明确分层

当前 UI 分层主要包括：

- Backdrop
- HUD
- World Marker
- Dialogue
- Inventory
- Modal
- Debug

UI 视觉气质明确偏向：

- 深色底
- 黄铜暖金强调
- 旧纸 / 旧宅气质
- 让出屏幕中央给场景构图

### 6.10 存档与场景加载

核心文件：

- `Assets/Scripts/SaveSystem.cs`
- `Assets/Scripts/Core/SceneLoader.cs`

已实现能力：

- 自动保存
- 读取最新存档
- 保存玩家位置、朝向、生命、目标文本、标记和已收集物
- 死亡或重新进入时恢复运行时状态

当前项目是 `单主场景` 结构，因此加载逻辑本质上会回到 `Main` 场景。

### 6.11 当前主流程控制

核心文件：

- `Assets/Scripts/AshParlorRunController.cs`
- `Assets/Scripts/AshParlorBrazierInteractable.cs`
- `Assets/Scripts/AshParlorChoiceInteractable.cs`
- `Assets/Scripts/AshParlorExitInteractable.cs`
- `Assets/Scripts/AshParlorSealBarrier.cs`

职责：

- 控制当前五房间切片的推进顺序
- 管理两盏烛台的点亮流程
- 管理风险 / 保守二选一分支
- 管理封印门、敌人状态与出口解锁
- 驱动任务文本刷新

可以把它理解为“当前章节/当前关卡切片的导演脚本”。

---

## 7. 当前玩家操作

根据代码，当前常用操作包括：

- `WASD / 方向键`：移动
- `Shift`：冲刺
- `E`：交互
- `Space / J / 鼠标左键`：攻击
- `Tab / I`：打开人物面板 / 背包
- `Esc`：关闭面板
- `A / D`、左右方向键、`1 / 2`：切换人物页 / 背包页
- `W / S`、上下方向键：在背包页切换条目

---

## 8. 当前目录结构说明

### 顶层目录

- `Assets/`：核心资源、脚本、场景、UI、音频、美术资源
- `Docs/`：项目总览、玩法规范、UI 规范与 AI 资产说明
- `Packages/`：Unity 包依赖
- `ProjectSettings/`：Unity 项目配置
- `PROJECT_PROGRESS.txt`：接力型进度日志，记录最近改动与下一步

### Assets 目录要点

- `Assets/Scenes`：主场景 `Main`
- `Assets/Scripts`：主要游戏逻辑
- `Assets/Scripts/UI`：正式 UI 架构代码
- `Assets/Data/Dialogue`：对话数据资产目录
- `Assets/Resources`：运行时加载资源，如字体、UI、角色、环境素材等
- `Assets/Art`：角色、环境、怪物、道具等美术资源
- `Assets/Audio`：音频与语音相关资源
- `Assets/Prefabs`：当前 prefab 资源较少，项目仍以运行时生成和程序化装配为主

---

## 9. 关键文件入口

如果要快速读懂项目，推荐阅读顺序如下：

1. `Assets/Scripts/GameBootstrap.cs`
2. `Assets/Scripts/TowerInteriorSlice.cs`
3. `Assets/Scripts/AshParlorRunController.cs`
4. `Assets/Scripts/PlayerMover.cs`
5. `Assets/Scripts/PlayerCombat.cs`
6. `Assets/Scripts/DialogueRunner.cs`
7. `Assets/Scripts/QuestTracker.cs`
8. `Assets/Scripts/InventoryController.cs`
9. `Assets/Scripts/UI/Core/UiBootstrap.cs`
10. `PROJECT_PROGRESS.txt`

如果要先看设计目标，再看实现，建议先读：

- `Docs/02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`
- `Docs/02_Gameplay/INTERIOR_CUTAWAY_SPEC.md`
- `Docs/03_UI/UI_ARCHITECTURE_SPEC.md`

---

## 10. 设计文档与代码落地的对应关系

### 已经比较一致的部分

- 单层闭环结构已经有原型落地
- 室内 cutaway 视角已有明确实现
- UI 视觉方向和设计文档一致
- 对话、任务、交互、角色面板已经从“测试 UI”向正式结构收口

### 仍在推进中的部分

- 完整章节演出仍在继续补
- 光暗控场的系统深度还可以继续加强
- 战斗仍偏最小实现
- 对话事件系统仍有一部分能力留在 TODO
- 当前仍以单场景切片为主，尚未进入真正多层循环

---

## 11. 当前项目的优势

- 题材、氛围和镜头语言已经比较统一
- 核心玩法方向足够清晰，没有明显跑偏成普通横版或普通刷怪游戏
- 运行时脚本化程度高，原型迭代效率快
- 设计文档相对完整，且与当前实现已有对齐趋势
- 叙事、空间、UI 这三条线已经开始互相支撑

---

## 12. 当前项目的风险与注意点

### 架构层面

- 运行时自动生成内容较多，后期需要明确哪些转 prefab，哪些继续保留代码生成
- 单场景结构现在很省事，但未来扩章节时需要谨慎处理切层、存档与初始化逻辑

### 系统层面

- 战斗目前够用，但还不算深
- 对话事件系统存在继续扩展空间
- 标记、任务、章节状态相关逻辑后续最好进一步统一，避免多套状态来源并行变复杂

### 生产层面

- 现有 `PROJECT_PROGRESS.txt` 很重要，已经承担了“接力日志”的作用
- 任何继续开发的人，最好沿用这套记录方式，否则上下文很容易断

---

## 13. 推荐的 Notion 页面拆分方式

如果要把这份文档导入 Notion，推荐拆成以下页面：

- `项目总览`
- `核心玩法`
- `当前里程碑与进度`
- `系统架构`
- `Main 场景 / 灰烬客厅流程`
- `UI 架构`
- `对话与任务系统`
- `目录结构与关键文件`
- `接手须知`

---

## 14. 给后续 ChatGPT 的一句话说明

如果把这份文档继续交给另一个 ChatGPT，可以附上这段说明：

> 这是一个 Unity 2.5D 黑暗叙事 RPG 原型项目的总览文档。请基于这份文档，把内容整理成适合 Notion 的项目知识库结构，保留“项目定位、当前状态、系统拆分、关键文件、后续推进建议”几个主页面。

---

## 15. 总结

`DarkVillageDemo / Ersarn` 当前已经具备清晰的世界观气质、镜头语言、空间结构和系统骨架。  
它最值得保留的不是某一个临时功能，而是下面这条已经逐渐成型的主线：

`高压房间探索 -> 轻战斗破局 -> 光暗与选择塑造节奏 -> 对话与残响推进叙事`

从工程状态上看，它目前最适合的工作方式是：

- 继续围绕 `Main` 场景做强一条完整垂直切片
- 逐步把高频系统从原型式脚本接线收口为更稳定的正式结构
- 保持设计文档、进度日志和实际代码三者同步

这会比“同时摊大功能面”更适合这个项目当前阶段。
