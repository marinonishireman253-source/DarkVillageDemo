# DarkVillageDemo / Ersarn 当前实现状态

文档状态：`当前生效`
建议用途：`接手开发前先读 / 作为当前实现真相入口 / Notion 首页正文来源`
不承担职责：`不替代玩法总纲，不替代 UI 专题规范，不替代逐条开发日志`

更新时间：`2026-04-15`

## 1. 当前一句话

`DarkVillageDemo / Ersarn` 已经从“单层原型”推进到“可切换两层的垂直切片骨架”。

当前主流程是：

- `Level 0 灰烬客厅`
- 通关后显示层结算
- 点击继续后切到 `Level 1 铜镜长廊`
- 第二层完成后回到 `Level 0` 重新开始

这意味着项目当前重点已经不是“把单个房间跑起来”，而是验证：

- 单层五段式模板是否能复用
- 楼层状态和切换是否稳定
- UI / 状态 / 存档 / prefab 化架构是否足够支撑后续扩层

## 2. 当前阶段判断

- 当前阶段：`M2 对话与剧情闭环 -> 向 M3 可扩展垂直切片过渡`
- 当前主目标：`把 Level 0 + Level 1 作为可复用楼层模板跑稳`
- 当前最重要的可玩验证：`灰烬客厅结算后进入铜镜长廊`

建议不要再把项目理解成“只剩灰烬客厅一层”。
现在更准确的说法是：

`灰烬客厅是第一层样板，铜镜长廊是第二层复用验证，项目正在验证扩层方法。`

## 3. 当前可玩内容

### Level 0：灰烬客厅

结构：

1. 落地房
2. 规则房
3. 压力房
4. 选择房
5. 终局房

当前已实现要点：

- 两盏烛台推进主流程
- 压力房预警、敌人启用、终局敌人差异配置
- 风险 / 保守选择
- 风险奖励掉落
- 全屏层结算面板
- 通过后切到下一层

### Level 1：铜镜长廊

结构仍然复用五段式，但内容换成镜廊主题：

1. 落地房独白与铜镜意象
2. 墙上刻痕规则提示
3. 双敌压力房
4. 铜镜选择房
5. 终局房 + 铜镜碎片奖励 + 专属结算

当前已实现要点：

- `MirrorCorridorRunController`
- 第二层对白资产
- 第二层房间内容、敌人、奖励、结算文案
- 第二层专属楼层切换状态

## 4. 当前实现架构

### 4.1 运行时总装配

入口：

- [GameBootstrap.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/GameBootstrap.cs)

职责：

- 创建 / 校验玩家
- 保证 UI、任务、背包、状态中枢存在
- 接入 `TowerInteriorSlice`
- 配置相机、环境与场景辅助系统

### 4.2 楼层与空间

入口：

- [TowerInteriorSlice.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/TowerInteriorSlice.cs)

当前状态：

- 按 `currentFloorIndex` 生成 `Ash Parlor` 或 `Mirror Corridor`
- 保留运行时代码生成房间骨架、相机区和灯光区
- 仍然是项目快速迭代的核心

### 4.3 楼层流程控制

关键文件：

- [FloorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/FloorRunController.cs)
- [AshParlorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/AshParlorRunController.cs)
- [MirrorCorridorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/MirrorCorridorRunController.cs)

当前状态：

- 已先做出第二层，再反抽薄基类
- 基类当前只统一交互接口、结算构建、选择弹窗配置和跨层继续入口
- 还不是“大而全”的完整阶段框架

### 4.4 状态与存档

关键文件：

- [GameStateHub.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/Core/GameStateHub.cs)
- [ChapterStateManager.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/ChapterStateManager.cs)
- [QuestTracker.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/QuestTracker.cs)
- [InventoryController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/InventoryController.cs)
- [SaveSystem.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/SaveSystem.cs)

当前状态：

- `GameStateHub` 已经是统一状态入口
- 已支持 `currentFloorIndex` 持久化
- 外部系统应优先依赖 `GameStateHub`，不要再直接跨系统读取状态

### 4.5 UI 架构

关键文件：

- [UiBootstrap.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/UI/Core/UiBootstrap.cs)
- [FloorSummaryPanel.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/UI/Overlay/FloorSummaryPanel.cs)
- [HudCanvasView.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/UI/HUD/HudCanvasView.cs)
- [InventoryCanvasView.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/UI/Inventory/InventoryCanvasView.cs)

当前状态：

- 正式 UI 主体已经是 `UGUI + TextMeshPro + runtime bootstrap`
- 不应再把项目描述成“主要依赖 OnGUI”
- `RuntimeDiagnostic` 一类调试用途仍可保留 `OnGUI`

### 4.6 Prefab 化与数据驱动

关键文件 / 资源：

- [CorePrefabCatalog.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/CorePrefabCatalog.cs)
- [CorePrefabCatalog.asset](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Resources/Prefabs/CorePrefabCatalog.asset)
- [Player.prefab](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Prefabs/Player.prefab)
- [StandardEnemy.prefab](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Prefabs/StandardEnemy.prefab)
- [InteractionPrompt.prefab](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Prefabs/InteractionPrompt.prefab)
- [Brazier.prefab](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Prefabs/Brazier.prefab)
- [WeaponData.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/WeaponData.cs)

当前状态：

- 玩家、标准敌人、交互提示、火盆已经 prefab 化
- 武器模板已改为 `ScriptableObject`
- 背包 / 装备 / UI 刷新走事件链，不应再回到硬编码引用

## 5. 当前最重要的设计结论

- `光源应影响局势，而不仅仅是门锁开关`
- `房间压迫优先于堆怪`
- `战斗是破局工具，不是主乐趣`
- `每层至少有一个能立刻感受到后果的选择`
- `层结算必须存在，且要承接到下一层`

## 6. 当前已知限制

- 第二层已能生成和接管流程，但“从 Level 0 正常打到结算再手动进入 Level 1”的完整实机验收仍值得继续确认
- `FloorRunController` 目前是薄基类，不是最终完整抽象；后续应继续从真实第三层需求里反抽
- `TowerInteriorSlice` 仍然偏大，后面如果继续扩层，建议逐步把楼层内容生成与通用结构分拆

## 7. 接手建议

继续开发时，建议顺序如下：

1. 先读这份文档
2. 再读 [PROJECT_PROGRESS.txt](/Users/kuzao/2.5d/DarkVillageDemo/PROJECT_PROGRESS.txt)
3. 然后按任务类型分流：

- 做玩法 / 扩层：看 `CORE_GAMEPLAY_MASTER_SPEC_CN.md`
- 做 UI：看 `UI_ARCHITECTURE_SPEC.md`
- 做具体实现：先看 `GameBootstrap`、`TowerInteriorSlice`、两个 `RunController`

## 8. 当前推荐入口文件

1. [GameBootstrap.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/GameBootstrap.cs)
2. [TowerInteriorSlice.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/TowerInteriorSlice.cs)
3. [FloorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/FloorRunController.cs)
4. [AshParlorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/AshParlorRunController.cs)
5. [MirrorCorridorRunController.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/MirrorCorridorRunController.cs)
6. [GameStateHub.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/Core/GameStateHub.cs)
7. [UiBootstrap.cs](/Users/kuzao/2.5d/DarkVillageDemo/Assets/Scripts/UI/Core/UiBootstrap.cs)
8. [PROJECT_PROGRESS.txt](/Users/kuzao/2.5d/DarkVillageDemo/PROJECT_PROGRESS.txt)

## 9. Notion 导入建议

如果要把项目知识库压到 Notion，建议直接以这份文档作为首页正文来源，不再单独维护第二份“导入稿”。

推荐首页标题：

`DarkVillageDemo / Ersarn 项目知识库`

推荐首页子页面结构：

1. `项目当前状态`
2. `核心玩法总纲`
3. `场景与镜头规范`
4. `怪物方向`
5. `UI 架构`
6. `外部 UI 资产协作`
7. `开发进度`

推荐各页面正文来源：

- `项目当前状态`：这份文档
- `核心玩法总纲`：`Docs/02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`
- `场景与镜头规范`：`Docs/02_Gameplay/INTERIOR_CUTAWAY_SPEC.md`
- `怪物方向`：`Docs/02_Gameplay/MONSTER_DESIGN_SPEC.md`
- `UI 架构`：`Docs/03_UI/UI_ARCHITECTURE_SPEC.md`
- `外部 UI 资产协作`：`Docs/03_UI/GEMINI_UI_ASSET_BRIEF.md`
- `开发进度`：`PROJECT_PROGRESS.txt`

推荐首页简介：

`DarkVillageDemo / Ersarn` 是一个基于 Unity 的 2.5D 黑暗叙事 RPG 原型项目，当前已经具备 `灰烬客厅 -> 铜镜长廊` 的两层切片切换骨架。项目核心不是传统刷怪 ARPG，而是围绕 `房间压迫、光暗控场、轻战斗破局、对话叙事推进` 形成单层闭环体验。
