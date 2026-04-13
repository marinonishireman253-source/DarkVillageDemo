# DarkVillage Notion 页面树

文档状态：`项目总览`
建议用途：`直接贴进 Notion 作为页面骨架或首页导航`
不承担职责：`不替代各专题长文档`

## 推荐首页标题

`DarkVillageDemo / Ersarn 项目知识库`

## 推荐首页简介

`DarkVillageDemo / Ersarn` 是一个基于 Unity 的 2.5D 黑暗叙事 RPG 原型项目，当前重点是把 `Main` 场景中的 `灰烬客厅` 做成可验证的垂直切片。项目核心不是传统刷怪 ARPG，而是围绕 `房间压迫、光暗控场、轻战斗破局、对话叙事推进` 形成单层闭环体验。

## 推荐页面树

### 1. 项目总览

- 项目一句话：Unity 2.5D 黑暗叙事 RPG
- 当前阶段：`M2 对话与剧情闭环`
- 当前主场景：`Assets/Scenes/Main.unity`
- 当前体验重点：`灰烬客厅（Ash Parlor）` 五房间切片
- 推荐来源文档：
  - `Docs/01_Project/DARKVILLAGE_NOTION_IMPORT_CN.md`

### 2. 当前状态

- 当前已实现：
  - 运行时场景装配
  - 横向移动、冲刺、交互、轻战斗
  - 对话节点、分支选项、剧情事件桥接
  - 任务追踪、背包、基础存档
  - 正式化中的 UGUI + TMP UI 架构
- 当前仍在推进：
  - 对话闭环场景内验证
  - 灰烬客厅演出与节奏强化
  - 光暗控场深度
  - 更完整的章节推进

### 3. 核心玩法

- 核心方向：
  - 单层闭环探索
  - 房间压迫感
  - 光暗局势控制
  - 轻战斗反打
  - 带后果的选择
- 推荐来源文档：
  - `Docs/02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`

### 4. Main 场景 / 灰烬客厅

- 当前结构：
  1. 落地房
  2. 规则房
  3. 压力房
  4. 选择房
  5. 终局房
- 当前主流程：
  1. 进入切片
  2. 触发对话与目标
  3. 点亮第一盏烛台
  4. 承受压力房推进
  5. 做出风险 / 保守选择
  6. 点亮第二盏烛台
  7. 解锁塔梯出口
- 推荐来源文档：
  - `Docs/02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`
  - `Assets/Scripts/AshParlorRunController.cs`

### 5. 场景与镜头规范

- 关键词：
  - cutaway
  - dollhouse
  - single-room framing
  - foreground masking
- 推荐来源文档：
  - `Docs/02_Gameplay/INTERIOR_CUTAWAY_SPEC.md`

### 6. 怪物方向

- 当前推荐：
  - 室内 stalker 型主怪
  - 借门框、遮挡、房间交界和镜头过渡制造压力
- 推荐来源文档：
  - `Docs/02_Gameplay/MONSTER_DESIGN_SPEC.md`

### 7. 系统架构

- 关键系统：
  - `GameBootstrap`：运行时总装配
  - `TowerInteriorSlice`：场景与房间结构
  - `PlayerMover`：移动与交互
  - `PlayerCombat`：轻战斗
  - `DialogueRunner`：对话流程
  - `QuestTracker`：当前目标
  - `InventoryController`：人物面板 / 背包
  - `UiBootstrap`：正式 UI 根装配

### 8. UI 架构

- 当前方向：
  - 从原型 `OnGUI` 迁移到 `UGUI + TextMeshPro`
  - 强调边缘承载、中心让景
- 推荐来源文档：
  - `Docs/03_UI/UI_ARCHITECTURE_SPEC.md`

### 9. 外部 UI 资产协作

- 用途：
  - 给 Gemini 等外部模型生成 UI 切图
- 推荐来源文档：
  - `Docs/03_UI/GEMINI_UI_ASSET_BRIEF.md`

### 10. 关键文件入口

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

## 推荐的文档使用规则

- 需要了解全局时，先看 `DARKVILLAGE_NOTION_IMPORT_CN.md`
- 需要判断玩法方向时，优先看 `CORE_GAMEPLAY_MASTER_SPEC_CN.md`
- 需要做场景和镜头时，看 `INTERIOR_CUTAWAY_SPEC.md`
- 需要做 UI 时，看 `UI_ARCHITECTURE_SPEC.md`
- 需要对外发 UI 资产需求时，再用 `GEMINI_UI_ASSET_BRIEF.md`

## 一句话交给后续 ChatGPT

> 请把这份页面树整理成 Notion 首页和子页面结构，保留“项目总览、当前状态、核心玩法、Main 场景流程、系统架构、UI 架构、关键文件入口”几个页面，并用简洁中文描述每页的用途。
