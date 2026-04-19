# 波次系统重构计划

## 1. 背景

当前项目里的波次逻辑已经能够驱动基础流程，但系统职责过于集中，扩展空间非常有限。现状主要由以下几部分组成：

- `Assets/Scripts/Managers/WaveManager.cs`
  - 保存波次配置
  - 推进波次计时
  - 根据分段刷怪
  - 发布波次运行态事件
  - 直接计算刷怪位置并实例化敌人
- `Assets/Scripts/Managers/GameManager.cs`
  - 监听 `WaveCompletedEvent`
  - 依据 `hasMoreWaves` 与升级情况决定后续进入 `WaveTransition`、`Shop` 或 `StageComplete`
- `Assets/Scripts/Managers/WaveTransitionManager.cs`
  - 管理波间宝箱与升级选择流程
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
  - 只消费波次 HUD 快照与进度事件
- `Assets/Scripts/Managers/StageCompleteSummaryManager.cs`
  - 统计完成波次、存活时长、击杀数、金币等单局数据

当前系统的核心问题不是“少几个字段”，而是**波次配置、运行时状态、刷怪规则、位置策略、流程决策全部耦合在一起**。

---

## 2. 当前问题拆解

### 2.1 `WaveManager` 职责过重

当前 `WaveManager` 同时负责：

- 波次配置持有
- 计时推进
- 分段刷怪判定
- 计数器维护
- 刷怪位置计算
- 敌人实例化
- 波次运行态广播

这使得后续一旦增加：

- 精英波 / Boss 波
- 不同波次完成条件
- 事件波
- 多种刷怪位置策略
- 波次奖励配置
- 设计师资产化配置

就会不断往同一个类里打补丁。

### 2.2 数据模型表达力太弱

当前波次数据只有：

- `Wave.name`
- `Wave.segments`
- `WaveSegment.enemy`
- `WaveSegment.spawnFrequency`
- `WaveSegment.timeStartEnd`

问题：

- 所有波共用一个全局 `waveDuration`
- 无法表达不同波次时长
- 无法表达完成条件
- 无法表达奖励定义
- 无法表达特殊标签（普通 / 精英 / Boss / 事件）
- 无法表达多种触发方式（按时间 / 按击杀 / 按预算 / 按批次）

### 2.3 运行时状态不清晰

当前依赖：

- `currentWaveIndex`
- `timer`
- `isTimerOn`
- `counterList`

其中 `counterList` 只是依赖 segment 索引对齐的裸整数列表，可读性和可维护性都很差。后续如果 segment 需要记录：

- 已刷数量
- 上次刷怪时间
- 当前预算
- 是否激活过

这套结构就会立刻失控。

### 2.4 刷怪位置策略被写死

当前位置算法固定为：

- 围绕 `spawnAroundEntity`
- 半径随机 `6f ~ 10f`
- 最后强行 clamp 到 `-10 ~ 10`

这意味着：

- 不能适配不同地图尺寸
- 不能做边缘刷怪、定点刷怪、区域权重刷怪
- 位置策略与波次控制器耦合严重

### 2.5 `GameManager` 了解了过多波次内部事实

当前 `GameManager` 依赖：

- `WaveCompletedEvent`
- `WaveRuntimeChangedEvent.HasMoreWaves`
- `player.IsLevelUpInCurrentWave`

这能跑通主流程，但只能表达“波结束后去哪里”，不能表达“为什么结束”“是否特殊结束”“是否失败”“是否进入特殊波间流程”。

### 2.6 波间流程和波次系统边界不清晰

`WaveTransitionManager` 目前负责宝箱和升级流程，但本质上它应该属于“波间奖励流程”，而不应继续和波次运行时核心耦合。

---

## 3. 重构目标

### 3.1 第一目标：先止血，不炸现有主流程

第一阶段不追求一步到位重写，而是先把结构拉正，确保以下链路继续成立：

- `Menu -> CharacterSelection -> Game`
- `WaveCompleted -> WaveTransition / Shop / StageComplete`
- `Shop -> Game -> StartNextWaveRequestedEvent`
- `GameOver` / `Pause` / 结算统计保持正常

### 3.2 第二目标：让波次配置具备“内容生产能力”

后续波次需要支持：

- 每波独立时长
- 多种刷怪计划
- 波次标签
- 波次奖励
- 多种完成条件
- ScriptableObject 资产化配置

### 3.3 第三目标：从波次系统升级到关卡流程系统

最终系统需要区分：

- 波次运行时
- 刷怪执行
- 波间奖励
- 流程编排
- 统计结算

并让它们通过事件协作，而不是继续堆在一个 MonoBehaviour 里。

---

## 4. 目标架构

### 4.1 配置层

#### `StageDefinitionSO`
表示整局关卡配置。

建议字段：

- `stageId`
- `displayName`
- `WaveDefinitionSO[] waves`
- 可选难度参数
- 可选全局奖励参数

#### `WaveDefinitionSO`
表示单波配置。

建议字段：

- `waveId`
- `displayName`
- `duration`
- `completionType`
- `WaveSpawnPlan[] spawnPlans`
- `WaveTag[] tags`
- `WaveRewardDefinition reward`

#### `WaveSpawnPlan`
表示一条刷怪计划。

建议字段：

- `enemy` 或 `EnemyDefinitionSO`
- `triggerType`
- `startTime`
- `endTime`
- `spawnInterval`
- `spawnCountPerBatch`
- `spawnBudget`
- `spawnLocationPolicy`

#### `SpawnLocationPolicySO`
表示刷怪位置策略。

至少预留这些类型：

- AroundPlayerRing
- MapEdgeRandom
- FixedPointsWeighted
- OutsideCameraBounds

---

### 4.2 运行时层

#### `StageRuntimeState`
保存整局运行态：

- 当前波索引
- 已完成波次数
- 是否还有下一波
- 是否正在运行
- 单局累计信息（必要时）

#### `WaveRuntimeState`
保存当前波运行态：

- 当前波配置引用
- 已经过时间
- 是否正在运行
- 是否完成
- 完成原因
- 当前 segment / plan 的运行态列表

#### `WaveSegmentRuntimeState` 或 `WaveSpawnPlanRuntimeState`
替代当前 `counterList`。

建议字段：

- `spawnedCount`
- `lastSpawnTime`
- `elapsedBatchCount`
- `remainingBudget`
- `isActivated`

---

### 4.3 服务层

#### `WaveProgressService`
负责：

- 开始波次
- 推进时间
- 判断完成
- 产出本帧结果

#### `WaveSpawnScheduleService`
负责：

- 根据波配置与运行态
- 计算当前帧应执行哪些刷怪请求

#### `WaveCompletionService`
负责：

- 判断波次何时完成
- 支持不同完成条件

#### `SpawnPositionResolver`
负责：

- 根据策略和上下文解析具体刷怪位置

---

### 4.4 Unity 适配层

#### `StageRuntimeController`
作为未来波次运行总入口，职责：

- 持有 stage/wave 配置
- 在 `Update` 中推进运行态
- 调服务层计算结果
- 通过事件发布运行结果
- 调用 `EnemySpawnService` 执行实际刷怪

#### `EnemySpawnService`
负责：

- 真正实例化或从对象池获取敌人
- 为后续对象池化留下扩展位

---

### 4.5 流程层

#### `WaveFlowCoordinator`
负责：

- 监听波次完成事件
- 判断进入 `WaveTransition` / `Shop` / `StageComplete` / 特殊事件页
- 将波次完成原因翻译给 `GameManager`

注意：

- `GameManager` 继续负责全局 `GameState` 切换
- `WaveFlowCoordinator` 只负责“流程解释”和“下一步建议”

---

## 5. 目录落点建议

### 第一阶段新增目录

- `Assets/Scripts/Waves/Core/`
- `Assets/Scripts/Waves/Services/`
- `Assets/Scripts/Waves/Runtime/`

### 第二阶段新增目录

- `Assets/Scripts/ScriptableObjects/Waves/`

### 拟新增文件（分期）

#### Phase 1
- `Assets/Scripts/Waves/Core/WaveRuntimeState.cs`
- `Assets/Scripts/Waves/Core/WaveSegmentRuntimeState.cs`
- `Assets/Scripts/Waves/Services/SpawnPositionResolver.cs`
- `Assets/Scripts/Waves/Runtime/SpawnContext.cs`

#### Phase 2
- `Assets/Scripts/ScriptableObjects/Waves/StageDefinitionSO.cs`
- `Assets/Scripts/ScriptableObjects/Waves/WaveDefinitionSO.cs`
- `Assets/Scripts/ScriptableObjects/Waves/SpawnLocationPolicySO.cs`

#### Phase 3
- `Assets/Scripts/Managers/WaveFlowCoordinator.cs`
- `Assets/Scripts/Waves/Runtime/StageRuntimeController.cs`
- `Assets/Scripts/Waves/Runtime/EnemySpawnService.cs`

---

## 6. 事件演进策略

## 核心原则

- 第一阶段尽量兼容现有事件名
- 优先扩展载荷，不立即删除旧事件
- UI 与统计继续通过快照事件和领域事件消费状态

### 6.1 保留的现有事件

- `WaveStartedEvent`
- `WaveCompletedEvent`
- `WaveProgressEvent`
- `WaveRuntimeChangedEvent`
- `StartFirstWaveRequestedEvent`
- `StartNextWaveRequestedEvent`
- `StopCurrentWaveRequestedEvent`
- `ResetWavesRequestedEvent`

### 6.2 第一阶段建议增强的事件载荷

#### `WaveCompletedEvent`
当前只有：

- `WaveNumber`

建议增强为：

- `WaveNumber`
- `TotalWaves`
- `CompletionReason`
- `ElapsedTime`
- `HasNextWave`

第一阶段如果为了降低改动面，也可以先新增字段并给默认值。

#### `WaveRuntimeChangedEvent`
当前已有：

- `CurrentWave`
- `TotalWaves`
- `HasStarted`
- `HasMoreWaves`
- `IsRunning`

建议后续逐步增强：

- `ElapsedTime`
- `CurrentWaveDuration`
- `WaveState`

### 6.3 后续可新增的事件

- `WaveFailedEvent`
- `WaveRewardPhaseStartedEvent`
- `StageStartedEvent`
- `StageCompletedEvent`
- `WaveSpawnRequestedEvent`（如果后续希望进一步解耦执行层）

---

## 7. 分期实施方案

## Phase 1：兼容式止血重构

### 目标

- 不推翻现有流程
- 不改 UI 事件消费方式
- 优先把内部结构理顺

### 具体工作

1. 把 `Wave` / `WaveSegment` 从 `WaveManager.cs` 中拆出为独立文件
2. 给每个 `Wave` 增加独立 `duration`
3. 新建 `WaveRuntimeState`
4. 新建 `WaveSegmentRuntimeState`，替换 `counterList`
5. 抽离刷怪位置计算到 `SpawnPositionResolver`
6. 给 `WaveCompletedEvent` 增加更多上下文
7. 让 `WaveManager` 退化为薄控制器，而不是“大而全的实现类”

### Phase 1 不做的事

- 不把波次直接改成 ScriptableObject
- 不引入复杂完成条件
- 不改 Shop / UI / WaveTransition 主流程
- 不直接替换 `GameManager`

### Phase 1 验收标准

- 主流程和现在一致
- 每波支持独立时长
- segment 运行态不再依赖裸 `List<int>`
- 刷怪位置逻辑不再硬编码在 `WaveManager`
- 现有 HUD 正常显示波次和倒计时
- 结算统计不受破坏

---

## Phase 2：配置资产化

### 目标

- 让波次配置从 MonoBehaviour 内联数组转为 SO 资产
- 让设计侧可以独立编辑 stage/wave 数据

### 具体工作

1. 新建 `StageDefinitionSO`
2. 新建 `WaveDefinitionSO`
3. 新建 `SpawnLocationPolicySO`
4. 将当前场景内波次数组逐步迁移为配置资产
5. 为波次完成条件预留扩展点
6. 将刷怪策略改成数据驱动选择

### Phase 2 验收标准

- 场景只引用 `StageDefinitionSO`
- 大部分波次内容不再手写在 `WaveManager` Inspector 里
- 不同波可以配置不同刷怪策略
- 后续添加新波次不需要改代码结构

---

## Phase 3：流程协调拆分

### 目标

- 把“波次运行”和“波间流程决策”彻底拆开
- 为 Boss 波、事件波、特殊奖励波预留空间

### 具体工作

1. 引入 `WaveFlowCoordinator`
2. 让 `GameManager` 只保留状态切换职责
3. 将“波结束后去哪”从 `GameManager` 中逐步迁出
4. 让 `WaveTransitionManager` 收敛为纯波间奖励控制器
5. 支持不同波次完成原因与后续去向映射

### Phase 3 验收标准

- `GameManager` 不再直接解析波次内部完成逻辑
- 波间奖励、商店、结算、特殊事件入口更加清晰
- 可以自然扩展精英波 / Boss 波 / 事件波

---

## 8. 第一阶段详细落地顺序

### Step 1
新增运行时状态类：

- `WaveRuntimeState`
- `WaveSegmentRuntimeState`

先让 `WaveManager` 内部改用这些类型，但对外事件尽量不变。

### Step 2
把 `Wave` / `WaveSegment` 移出 `WaveManager.cs`，减少单文件耦合。

### Step 3
给 `Wave` 加入独立 `duration` 字段，并让 `WaveManager` 改为读取当前波自己的时长。

### Step 4
新增 `SpawnPositionResolver`，把现有刷怪位置逻辑搬出去。

### Step 5
增强 `WaveCompletedEvent` 与 `WaveRuntimeChangedEvent` 的上下文信息。

### Step 6
回归验证：

- 首波开始
- 下一波开始
- 波次结束跳转
- 商店返回战斗
- 通关结算
- 战斗中死亡
- 暂停恢复

---

## 9. 对现有类的处理建议

### `WaveManager`

第一阶段：
- 保留类名，降低影响范围
- 内部重构为薄控制器

第二阶段后：
- 可考虑逐步过渡为 `StageRuntimeController`
- 旧类保留一段时间作为兼容入口，确认稳定后再替换

### `GameManager`

第一阶段：
- 尽量少动
- 只适配必要的新事件字段

第三阶段：
- 从“直接决定波结束后去哪”过渡为“消费流程协调器给出的结果”

### `WaveTransitionManager`

第一阶段：
- 不改职责边界
- 继续作为波间奖励控制器使用

第三阶段：
- 明确其不是波次运行核心，而是 reward/transition controller

---

## 10. 风险点与规避方式

### 风险 1：一次性重写导致流程断裂

规避：
- 第一阶段只做兼容式重构
- 先保留旧事件名与主流程

### 风险 2：UI / 统计 / 关卡流程被连锁影响

规避：
- 保留快照事件
- 增量扩展事件载荷
- 每一步都做回归验证

### 风险 3：Unity 场景引用大面积失效

规避：
- 第一阶段尽量不改已有场景组件名与序列化字段名
- 大型迁移放到第二阶段进行

### 风险 4：设计资产过早引入，导致第一阶段复杂度暴涨

规避：
- 第一阶段不做 SO 资产化
- 先整理运行时和职责边界

---

## 11. 本次实施约束

本轮开始执行时遵守以下约束：

- 不修改计划文件本身
- 先产出计划文档，再按文档推进
- 第一阶段优先，避免超范围重构
- 不为未来假想需求过度抽象
- 以兼容现有事件链和主流程为最高优先级

---

## 12. 下一步执行方式

按照本计划，后续实际编码推进顺序为：

1. 先完成 Phase 1 Step 1：运行时状态类落地
2. 再完成 Phase 1 Step 2：拆分 `Wave` / `WaveSegment`
3. 再完成 Phase 1 Step 3：每波独立时长
4. 再完成 Phase 1 Step 4：抽离刷怪位置解析
5. 再完成 Phase 1 Step 5：增强波次事件上下文
6. 每完成一个小步都进行一次针对性验证，再继续下一步

这份文档作为后续波次系统改造的唯一执行蓝图，后续实现必须围绕本文分阶段推进，不再临时散改。
