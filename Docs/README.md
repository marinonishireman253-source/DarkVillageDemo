# Docs 导航

这个目录现在按“用途”而不是“写作时间”整理，入口建议如下：

## 文档状态说明

- `当前生效`：继续开发时应优先遵循
- `项目总览`：用于交接、对外说明、AI 导入
- `外部简报`：给外部工具或生成模型使用，不是内部总规范

## 先读哪个

- 想快速了解整个项目：
  `01_Project/DARKVILLAGE_NOTION_IMPORT_CN.md`
- 想确认当前核心玩法总纲：
  `02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`
- 想继续做场景和镜头：
  `02_Gameplay/INTERIOR_CUTAWAY_SPEC.md`
- 想继续做怪物压迫感设计：
  `02_Gameplay/MONSTER_DESIGN_SPEC.md`
- 想继续做正式 UI：
  `03_UI/UI_ARCHITECTURE_SPEC.md`
- 想把 UI 需求丢给外部生成模型：
  `03_UI/GEMINI_UI_ASSET_BRIEF.md`

## 目录结构

### `01_Project/`

- 面向“项目总览 / 交接 / Notion 导入”的文档

### `02_Gameplay/`

- 面向玩法、关卡结构、镜头空间、怪物设计的长期规范

### `03_UI/`

- 面向 UI 架构和 UI 资产生成说明

## 当前建议

- `01_Project/` 适合给人或 AI 快速补上下文
- `02_Gameplay/` 适合作为设计和实现时的长期依据
- `03_UI/` 适合 UI 迭代、资源外包和视觉协作

## 各文档定位

- `01_Project/DARKVILLAGE_NOTION_IMPORT_CN.md`
  状态：`项目总览`
  用途：给新接手的人或 ChatGPT 快速补全上下文
- `01_Project/NOTION_DOCS_INDEX_CN.md`
  状态：`项目总览`
  用途：直接贴进 Notion 的精简页面树和摘要
- `02_Gameplay/CORE_GAMEPLAY_MASTER_SPEC_CN.md`
  状态：`当前生效`
  用途：核心玩法总纲，优先级最高
- `02_Gameplay/INTERIOR_CUTAWAY_SPEC.md`
  状态：`当前生效`
  用途：场景、镜头、房间结构专题规范
- `02_Gameplay/MONSTER_DESIGN_SPEC.md`
  状态：`当前生效`
  用途：怪物方向专题规范
- `03_UI/UI_ARCHITECTURE_SPEC.md`
  状态：`当前生效`
  用途：正式 UI 架构与迁移路线
- `03_UI/GEMINI_UI_ASSET_BRIEF.md`
  状态：`外部简报`
  用途：把 UI 资源需求发给外部生成模型

如果后面继续加文档，优先放进对应子目录，不要再直接堆到 `Docs/` 根目录。
