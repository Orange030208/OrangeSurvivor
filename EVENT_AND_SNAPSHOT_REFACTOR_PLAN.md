# 事件系统与 Snapshot 命名整理方案

本文档用于规划当前项目中全局事件系统与 `Snapshot` 命名的整理方向，先作为审核稿，不直接要求一次性重构全部代码。

## 1. 背景

当前项目已经形成了较完整的全局事件总线：

- `Assets/Scripts/Event/GameEventBus.cs`
- `Assets/Scripts/Event/Implementations/*.cs`

事件系统覆盖了主流程、波次、商店、升级、音频、敌人、实体死亡、UI 点击等多个模块。它带来的好处是模块之间耦合较低，表现层可以方便监听玩法事件；但问题也很明显：部分命令、查询和状态同步也被包装成事件，导致调用链不直观、调试成本上升。

同时，项目中大量类型和方法使用 `Snapshot` 命名，但语义并不完全一致。有些是真正的瞬时只读状态，有些更像 UI 展示模型、状态变更数据、刷新请求、配置投影或运行时结果。继续扩大这种命名会让后续维护者难以判断数据的生命周期、所有权和更新方式。

## 2. 整理目标

本次整理的目标不是移除事件系统，也不是机械删除所有 `Snapshot` 命名，而是建立明确边界：

- 事件系统只承担跨模块通知和少量用户意图转发。
- 命令型流程改为显式方法或接口调用。
- 查询型流程改为只读属性、查询方法或页面上下文注入。
- 高频战斗逻辑避免全局广播后由大量对象自行过滤。
- `Snapshot` 只用于真正表达某一时刻不可变状态副本的数据。
- UI 展示数据、选择项、运行时状态、刷新原因等使用更准确的命名。

## 3. 通信方式分层规则

| 场景 | 推荐方式 | 不推荐方式 | 示例 |
| --- | --- | --- | --- |
| 一对一命令 | 显式方法、接口 | `RequestedEvent` | 开始下一波、停止当前波、重置波次 |
| 状态查询 | 只读属性、`CreateXXXView`、`GetXXXState` | 请求事件再回推事件 | 获取波次运行状态、获取商店当前物品 |
| 跨模块通知 | 事件 | 直接持有所有监听者 | 波次开始、实体死亡、金币变化 |
| 用户意图 | UI 局部事件或少量全局意图事件 | UI 直接操作多个业务系统 | 开始游戏、继续商店、暂停 |
| 表现反馈 | 事件或组件局部事件 | 业务系统直接调用所有表现脚本 | 音效、飘字、受击闪烁 |
| 高频实体逻辑 | 直接调用、组件事件、精准派发 | 全局广播后所有实体过滤 | 伤害结算、吸血、Buff 触发 |

## 4. 事件系统整理范围

### 4.1 保留事件的场景

以下事件类型适合继续保留：

- `ChangedEvent`：状态已变化，例如 `CurrencyChangedEvent`、`GameStateChangedEvent`。
- `StartedEvent` / `CompletedEvent`：流程节点通知，例如 `WaveStartedEvent`、`WaveCompletedEvent`。
- `DiedEvent` / `DamagedEvent`：已发生的战斗结果通知，主要给 UI、音频、统计、表现层消费。
- UI 用户意图事件：例如 `MenuStartClickedEvent`、`ShopContinueClickedEvent`。这类事件可保留，但应由主流程编排者统一处理。
- 音频请求事件：例如 `AudioSfxPlayRequestedEvent`。音频是典型横切表现系统，可以继续通过事件解耦。

### 4.2 需要收敛的事件

以下事件更像命令或查询，应迁移为显式调用或显式查询：

| 当前事件 | 当前问题 | 建议替代 |
| --- | --- | --- |
| `StartFirstWaveRequestedEvent` | `GameManager` 明确控制 `WaveManager`，不需要广播 | `IWaveController.StartFirstWave()` |
| `StartNextWaveRequestedEvent` | 一对一命令被隐藏在事件中 | `IWaveController.StartNextWave()` |
| `StopCurrentWaveRequestedEvent` | 状态机退出时的明确命令 | `IWaveController.StopCurrentWave()` |
| `ResumeCurrentWaveRequestedEvent` | 明确命令 | `IWaveController.ResumeCurrentWave()` |
| `ResetWavesRequestedEvent` | 明确命令 | `IWaveController.ResetWaves()` |
| `RequestWaveHudSnapshotEvent` | 查询型事件，无返回值语义绕路 | `WaveManager.CreateHudView()` 或页面上下文传入 |
| `RequestWaveRuntimeSnapshotEvent` | 查询型事件，无返回值语义绕路 | `WaveManager.CurrentState` 或 `CreateRuntimeState()` |
| `DefeatAllEnemiesRequestedEvent` | 命令型事件，需确认是否只由波次和主流程触发 | `EnemyRegistry.DefeatAll()` 或 `IEnemyRegistry.DefeatAllTrackedEnemies()` |

### 4.3 事件命名规范

新增或保留事件时按语义命名：

- `XXXChangedEvent`：状态已经变化。
- `XXXStartedEvent`：流程已经开始。
- `XXXCompletedEvent`：流程已经完成。
- `XXXSelectedEvent`：用户或系统已经完成选择。
- `XXXClickedEvent`：UI 点击意图。
- `XXXRequestedEvent`：只允许用于跨模块意图，且调用方不应该知道具体执行者。

禁止把 `RequestedEvent` 作为默认函数调用方式。只要调用方明确知道接收者是谁，就优先使用显式依赖。

## 5. Snapshot 命名整理规则

### 5.1 什么可以叫 Snapshot

只有同时满足以下条件的数据才建议继续叫 `Snapshot`：

- 表示某一时刻状态的只读副本。
- 创建后不再被生产者修改。
- 消费者不拥有它，只消费它。
- 它不表达命令、刷新原因或 UI 操作意图。
- 它可以安全用于调试、展示、记录或比较。

符合条件的例子：

- `PoolSnapshot`：对象池某一刻的容量、激活数、空闲数等。
- `PlayerLevelSnapshot`：玩家等级、经验、升级点的瞬时状态。
- `StageCompleteResult`：结算页展示用的最终统计结果。当前 `StageCompleteSnapshot` 应改为该命名。
- `UIMotionTargetSnapshot`：UI 动画播放前捕获的初始 Transform / Graphic 状态。

### 5.2 不适合叫 Snapshot 的场景

以下数据不建议继续使用 `Snapshot` 命名：

- UI 列表项模型：应使用 `ViewData`、`ViewModel`、`ItemViewData`。
- 商店可交互状态：应使用 `ShopState`、`ShopViewData` 或 `ShopInventoryView`。
- 刷新原因：应使用 `ChangeReason`、`RefreshReason`，不要绑定在 `SnapshotReason` 上。
- 查询请求：应使用 `RequestXXX` 方法或显式接口，不要创建 `RequestXXXSnapshotEvent`。
- 运行时状态对象：如果生产者持续持有并更新，应命名为 `State`，不是 `Snapshot`。
- 配置投影：如果是从 ScriptableObject 转换给 UI 展示，应命名为 `OptionViewData` 或 `DisplayData`。

### 5.3 当前命名评估分类

| 当前名称 | 当前判断 | 建议方向 |
| --- | --- | --- |
| `ActiveBuffSnapshot` | 包含 DisplayName、Icon、Describable 等展示字段，当前主要服务 UI 和 Inspector | 改为 `ActiveBuffViewData` |
| `InventoryUIItemSnapshot` | 明确是 UI 列表项数据 | 改为 `InventoryItemViewData` |
| `ShopSnapshot` | 包含商品列表、重随价格、是否可重随、刷新原因，更像商店视图状态 | 改为 `ShopViewState` 或 `ShopViewData` |
| `ShopSnapshotReason` | 表达刷新原因，不是快照 | 改为 `ShopRefreshReason` 或 `ShopChangeReason` |
| `PlayerLevelSnapshot` | 只读等级状态副本 | 可以保留 |
| `StageCompleteSnapshot` | 结算时生成的最终结果 | 改为 `StageCompleteResult` |
| `CharacterSelectionSnapshot` | 更像角色选择页面所需视图数据 | 改为 `CharacterSelectionViewData` |
| `UpgradeCardOptionSnapshot` | 从卡牌配置和运行态投影出的选择项展示数据 | 改为 `UpgradeCardOptionViewData` |
| `PoolSnapshot` | 对象池调试状态 | 保留 |
| `UIMotionTargetSnapshot` | 动画初始状态捕获 | 保留 |
| `RequestWaveHudSnapshotEvent` | 查询请求，不是快照数据 | 删除，改为显式读取或页面上下文 |
| `RequestWaveRuntimeSnapshotEvent` | 查询请求，不是快照数据 | 删除，改为显式读取或页面上下文 |

## 6. 完整完成要求

以下要求构成本次整理的完整完成标准。实现时可以按风险和依赖关系安排提交顺序，但最终交付必须覆盖本文档列出的全部要求。

### 6.1 逐系统评估结论

| 系统 / 类型 | 当前做法 | 评估 | 最优解法 |
| --- | --- | --- | --- |
| `GameManager -> WaveManager` | 通过 `StartFirstWaveRequestedEvent`、`StartNextWaveRequestedEvent`、`StopCurrentWaveRequestedEvent` 等命令事件驱动 | 不合理。调用方和接收方明确，事件隐藏流程顺序 | 改为 `GameManager` 显式引用 `WaveManager` 或 `IWaveController` |
| 波次状态查询 | `RequestWaveHudSnapshotEvent` / `RequestWaveRuntimeSnapshotEvent` 请求后由 `WaveManager` 再发布状态事件 | 不合理。查询语义被事件化，且无返回值 | 改为只读属性、查询方法、页面上下文 |
| `WaveStartedEvent` / `WaveProgressEvent` / `WaveCompletedEvent` / `WaveRuntimeChangedEvent` | 波次状态变化后广播 | 合理。属于跨模块通知 | 保留，但数据命名可按 `Event` 语义保持 |
| `DefeatAllEnemiesRequestedEvent` | `GameManager` 请求 `WaveManager`，再转发 `DefeatAllTrackedEnemiesRequestedEvent` 给 `EnemyRegistry` | 不合理。命令链绕了一层 | `GameManager` 或 `WaveManager` 显式调用 `EnemyRegistry.DefeatAllTrackedEnemies()` |
| `EnemyRegisteredEvent` / `EnemyUnregisteredEvent` | `Enemy` 生命周期通知 `EnemyRegistry` | 可以保留。敌人和注册表解耦有价值 | 保留，或后续由 `EnemyFactory` 直接注册；不是当前必须解决项 |
| `GameStateChangedEvent` | 主状态变化广播给玩家、商店、统计、奖励等系统 | 合理。典型状态通知 | 保留，但避免它承载命令副作用 |
| UI 点击事件 | 页面发布 `MenuStartClickedEvent`、`ShopContinueClickedEvent` 等 | 基本合理。UI 表达用户意图 | 保留，统一由 `GameManager` 或对应流程编排者消费 |
| `PauseGameRequestedEvent` / 暂停菜单事件 | UI 表达暂停、继续、回菜单意图 | 合理 | 保留，命名已清楚 |
| `GameplaySimulationPauseRequestedEvent` / `ResumeRequestedEvent` | 奖励选择等系统请求暂停模拟 | 当前可运行，但 `RewardSelectionManager` 已持有 `GameManager`，继续用事件会隐藏模拟控制入口 | 改为 `GameManager.RequestSimulationPause(sourceId)` / `GameManager.ReleaseSimulationPause(sourceId)` 显式 API |
| `RewardSelectionCardSelectedEvent` | 弹窗卡片点击后全局发布，`RewardSelectionManager` 等待匹配 `RequestId` | 可用但不是最优。当前只有一个明确消费者 | 最优是将选择回调通过 `RewardSelectionPopupModel` 或打开 Popup 时的回调传入，移除全局事件 |
| `ChestCollectedEvent` | 宝箱收集后触发奖励选择 | 合理。掉落物不需要知道奖励系统 | 保留 |
| `UpgradeRewardAvailableEvent` | 玩家升级后通知奖励选择系统 | 合理。玩家等级系统不应直接打开 UI | 保留 |
| `ShopVideoAdRerollRequestedEvent` | 广告重随请求事件 | 当前代码只有 `ShopManager` 消费，且广告集成未完成 | 若广告系统未来独立，保留；若只有商店按钮触发，改为 `ShopManager.RequestVideoAdReroll()` |
| `ShopFreeRerollsGrantedEvent` | 升级卡给商店免费刷新次数 | 合理。升级效果不应直接依赖商店实现 | 保留，或抽 `IShopBonusReceiver` 但当前没必要 |
| `CurrencyChangedEvent` | 钱包变化广播 | 合理 | 保留 |
| `Audio*RequestedEvent` | 静态桥接向音频系统发请求 | 合理。音频是横切表现系统 | 保留，不作为过度事件化问题处理 |
| `EntityDamagedEvent` | 伤害应用后广播，表现层和部分逻辑监听 | 部分合理。表现监听合理；生命组件监听全局伤害做吸血不合理 | 保留给表现、统计；核心战斗副作用迁回结算链 |
| `EntityDiedEvent` | 死亡后广播 | 合理 | 保留 |
| `ApplyBuffRequestedEvent` / `RemoveBuffRequestedEvent` | 带 `owner.RuntimeId` 精准派发给目标 BuffController | 可以保留。不是无差别广播，适合跨来源给指定实体加 Buff | 保留；若调用方已持有目标 `BuffController`，应优先直接调用 |
| `BuffStackChangedEvent` / Removed / Expired | Buff 栈变化通知 | 合理 | 保留 |
| `PoolSnapshot` | 对象池运行时调试状态 | 合理 | 保留 |
| `UIMotionTargetSnapshot` | 动画播放前捕获 UI 初始状态 | 合理 | 保留 |
| `PlayerLevelSnapshot` | 等级状态只读副本 | 合理 | 保留，不列入本次必须解决问题 |
| `StageCompleteSnapshot` | 结算页最终统计数据 | 应表达最终结算结果，不是运行中快照 | 改为 `StageCompleteResult` |
| `InventoryUIItemSnapshot` | 背包 UI 条目展示数据 | 不适合叫 Snapshot | 改为 `InventoryItemViewData` |
| `ShopSnapshot` / `ShopSnapshotReason` | 商店页面状态 + 刷新原因 | 不适合叫 Snapshot | 改为 `ShopViewState` + `ShopRefreshReason` |
| `CharacterSelectionSnapshot` | 角色选择页面展示数据 | 不适合叫 Snapshot | 改为 `CharacterSelectionViewData` |
| `UpgradeCardOptionSnapshot` | 升级卡展示投影 | 不适合叫 Snapshot | 改为 `UpgradeCardOptionViewData` |
| `ActiveBuffSnapshot` | 当前 Buff 聚合展示数据，主要给 UI 和 Inspector | 不适合继续叫 Snapshot | 改为 `ActiveBuffViewData` |

### 6.2 最优总体解法

本项目不需要引入新的消息框架、服务定位器或大型响应式架构。最优解法是保留轻量 `GameEventBus`，同时把通信方式收敛为三条规则：

1. 主流程和 Manager 之间的一对一命令使用显式引用或薄接口。
2. 跨模块已发生事实继续使用事件通知。
3. 页面展示数据和运行时状态使用准确命名，不再统一套 `Snapshot`。

推荐目标结构：

```text
GameManager
  -> IWaveController / WaveManager
  -> IEnemyRegistry / EnemyRegistry
  -> UIManager
  -> ShopManager / InventoryOperateManager

WaveManager
  -> 发布 WaveStarted / WaveProgress / WaveCompleted / WaveRuntimeChanged
  -> 暴露 CurrentState / CreateHudViewData()

UI Page
  -> 通过 PageContext 获取初始数据
  -> 通过局部事件或少量 UI 意图事件通知流程编排者

Gameplay Logic
  -> 核心结算直接调用或组件事件
  -> 表现、统计、音频监听事实事件
```

### 6.3 不需要解决或不应优先解决的内容

以下内容当前不应作为问题处理：

- 不需要删除 `GameEventBus`。项目已经围绕它形成大量低耦合通知，完全移除收益低、风险高。
- 不需要重写音频事件。音频请求属于横切表现系统，事件化是合理的。
- 不需要把所有 UI 点击事件改成直接引用 `GameManager`。UI 意图事件可以保留，关键是由唯一流程编排者消费。
- 不需要把 `EnemyRegisteredEvent` / `EnemyUnregisteredEvent` 立即改掉。它们是生命周期事实通知，不是命令。
- 不需要改 `PoolSnapshot` 和 `UIMotionTargetSnapshot`。它们是真正的快照。
- 不需要为了 `ApplyBuffRequestedEvent` 强行改成直接调用。它已经使用 key 精准派发，只有在调用方明确持有目标 `BuffController` 时才改直接调用。
- 不需要引入全局 service locator。当前问题来自隐藏依赖，服务定位器会放大同类问题。

### 6.4 规则与边界

必须完成：

- 本文档经审核确认后，作为后续事件和数据命名的项目约束。
- 新增事件前必须判断它属于命令、查询、通知、用户意图还是表现反馈。
- 新增数据类型前必须判断它属于 `State`、`ViewData`、`Result`、`Request`、`RefreshReason` 还是真正的 `Snapshot`。
- 禁止继续新增用事件包装的一对一命令。
- 禁止继续新增 `RequestXXXSnapshotEvent` 这类查询型事件。
- 禁止把 `Snapshot` 当作通用 DTO 后缀。

完成要求：

- 所有新增和重命名类型符合第 3 节和第 5 节的规则。
- 保留的例外必须有明确理由，例如动画系统需要捕获初始状态，或对象池需要调试快照。

### 6.5 `GameManager` 与 `WaveManager`

必须完成：

- 新增薄接口 `IWaveController`，或由 `GameManager` 显式引用 `WaveManager`。
- 将波次开始、停止、恢复、重置从事件改为显式调用。
- 移除 `GameManager` 对波次命令事件的发布。
- 移除 `WaveManager` 对波次命令事件的订阅。
- 保留 `WaveStartedEvent`、`WaveProgressEvent`、`WaveCompletedEvent`、`WaveRuntimeChangedEvent` 作为通知事件。

参考接口：

```csharp
public interface IWaveController
{
    WaveRuntimeState CurrentState { get; }
    void StartFirstWave();
    void StartNextWave();
    void StopCurrentWave();
    void ResumeCurrentWave();
    void ResetWaves();
}
```

约束：

- 如果 `WaveRuntimeState` 当前是内部运行结构，不适合直接暴露，可另建 `WaveRuntimeViewData`。
- 不要为了接口而引入复杂服务定位器；Inspector 显式引用即可。

完成要求：

- `GameManager` 中能直接看到波次控制调用链。
- 删除 `StartFirstWaveRequestedEvent` 等命令型事件后流程不回退。
- 波次控制方法具备必要的状态校验和可诊断日志，不依赖事件监听顺序保证正确性。

### 6.6 波次查询与展示数据

必须完成：

- 删除 `RequestWaveHudSnapshotEvent`。
- 删除 `RequestWaveRuntimeSnapshotEvent`。
- UI 初始化所需数据通过页面上下文、只读属性或查询方法获得。
- 将波次 UI 展示数据命名为 `WaveHudViewData`、`WaveRuntimeViewData` 或等价明确名称。
- 如果保留运行时状态类型，应命名为 `WaveRuntimeState`，并避免对 UI 暴露可变内部结构。

命名选项：

- `WaveHudViewData`
- `WaveRuntimeViewData`
- `WaveRuntimeState`

改造方向：

- `GamingUIPage` 打开时从 `GamingPageContext` 获取初始波次展示数据。
- 后续变化继续监听 `WaveProgressEvent` 和 `WaveRuntimeChangedEvent`。

完成要求：

- UI 不再通过发布请求事件获取初始状态。
- 波次状态读取路径可从 `GameManager -> WaveManager -> PageContext` 直接追踪。
- 波次展示刷新不依赖事件请求和事件响应的隐式时序。

### 6.7 UI 展示数据命名

必须完成：

| 当前名称 | 必须调整为 | 要求 |
| --- | --- | --- |
| `InventoryUIItemSnapshot` | `InventoryItemViewData` | 表达背包 UI 条目展示数据 |
| `ShopSnapshot` | `ShopViewState` 或 `ShopViewData` | 表达商店页面当前可展示状态 |
| `ShopSnapshotReason` | `ShopRefreshReason` 或 `ShopChangeReason` | 表达刷新原因，不与快照绑定 |
| `CharacterSelectionSnapshot` | `CharacterSelectionViewData` | 表达角色选择页展示数据 |
| `UpgradeCardOptionSnapshot` | `UpgradeCardOptionViewData` | 表达升级卡选项展示数据 |

相关事件和方法必须同步改名：

- `SnapshotChanged` 根据语义改为 `ViewDataChanged`、`ItemsChanged`、`StateChanged` 或更具体名称。
- `CreateSnapshot()` 根据语义改为 `CreateViewData()`、`CreateState()`、`CreateResult()` 或更具体名称。
- `RequestSnapshot()` 根据语义改为 `RefreshView()`、`PublishCurrentState()`、`NotifyItemsChanged()` 或直接删除。
- `ApplySnapshot()` 根据语义改为 `ApplyViewData()`、`RenderState()`、`RenderResult()` 或更具体名称。

完成要求：

- UI 数据类型能从名称看出用途。
- `Snapshot` 不再被用作 UI DTO 的默认后缀。
- 重命名后没有遗留的旧事件名、旧方法名和旧文件名。
- 若类型可能被 Unity 序列化、反射或字符串引用，必须先确认影响并记录处理方式。

### 6.8 保留 Snapshot 的类型

必须确认并保留或调整以下类型：

| 当前名称 | 要求 |
| --- | --- |
| `PoolSnapshot` | 保留，属于对象池调试状态快照 |
| `PlayerLevelSnapshot` | 保留，属于玩家等级瞬时状态副本，不列入本次必须解决问题 |
| `StageCompleteSnapshot` | 改为 `StageCompleteResult`，表达结算完成后的最终统计结果 |
| `ActiveBuffSnapshot` | 改为 `ActiveBuffViewData`，表达 Buff 栏和 Inspector 使用的展示数据 |
| `UIMotionTargetSnapshot` | 保留，属于动画播放前的初始状态捕获 |

完成要求：

- 每个保留 `Snapshot` 的类型都有清晰语义，不再与 UI 展示模型混用。
- 每个改名类型完成文件名、类型名、方法名、事件名和调用点同步调整。

### 6.9 战斗高频事件

必须完成：

- 检查 `HealthComponent` 对全局 `EntityDamagedEvent` 的订阅。
- 将吸血等核心战斗逻辑从全局广播中过滤的模式中移出。
- 保留 `EntityDamagedEvent` 给飘字、音效、统计等表现或旁路系统。

改造方向：

- 伤害结算继续由 `HitService` / `HitResolver` / `HealthComponent.ApplyHitResult()` 收口。
- 吸血放入命中结算链的 modifier、攻击者组件或命中应用后的直接回调。
- 表现层继续消费 `EntityDamagedEvent`。

完成要求：

- 大量实体不再都监听全局伤害事件只为判断是否与自己有关。
- 伤害核心规则可通过结算链追踪，而不是依赖事件副作用。
- `EntityDamagedEvent` 的消费者职责明确，不能再承载核心结算副作用。

### 6.10 商店、奖励、敌人注册与剩余 Manager

必须完成：

- 检查 `ShopManager`、`RewardSelectionManager`、`EnemyRegistry` 中的事件是否存在命令、查询、通知混用。
- 对明确一对一控制改成显式调用。
- 对纯通知保留事件。
- 对 UI 选择结果和用户意图保留必要事件，但必须由明确的流程编排者消费。
- 消除 Manager 之间通过请求事件互相调用的链路。

完成要求：

- 每个 Manager 的输入来源清晰：显式命令、UI 意图、事件通知三者不混用。
- 查询当前状态时不再发布事件，而是读取只读状态或调用查询方法。
- 商店、奖励、敌人注册相关数据命名符合第 5 节规则。

### 6.11 事件调试能力

必须完成：

- 给 `GameEventBus` 增加开发期调试辅助。
- 能看到事件类型、监听数量、可选发布来源。
- 能在开发环境定位当前事件是否没有监听者或监听者过多。

实现要求：

- 使用条件编译或开关控制，避免正式包噪音。
- 先只做日志和监听数量统计，不引入复杂事件框架。
- 可考虑对监听者异常做隔离，避免一个监听者异常中断后续监听。

完成要求：

- 调试事件链路时不再完全依赖全局搜索。
- 事件日志不影响正式运行性能和发布包输出。

## 7. 总体验收标准

完成整理后必须满足：

- 主流程命令调用链可以直接从 `GameManager` 追到目标 Manager。
- 查询状态不再通过事件绕路。
- 所有命令型 `RequestedEvent` 已移除或有明确跨模块意图理由。
- `Snapshot` 命名只用于真正的瞬时只读状态副本。
- UI 展示模型使用 `ViewData` / `ViewState` 等更明确命名。
- 高频战斗逻辑不依赖全局广播给所有实体过滤。
- 表现层、音频、统计仍可通过事件低耦合监听。
- 原有菜单、角色选择、战斗、波次、商店、结算流程行为不回退。
- 旧命名文件、类型、事件、方法没有残留误导性引用。
- 新增或保留的例外都能从语义上解释清楚。

## 8. 验证要求

完成代码改造后必须执行或记录以下验证：

- Unity 编译无错误。
- 主菜单进入角色选择正常。
- 角色确认进入战斗正常。
- 第一波开始、波次进度显示、波次完成正常。
- 波次结束进入商店或结算的流程正常。
- 商店刷新、购买、继续下一波正常。
- 玩家死亡进入 GameOver 正常。
- 暂停、继续、返回菜单正常。
- 伤害飘字、受击音效、实体死亡统计等依赖事件的表现不回退。
- 背包、商店、Buff、升级卡 UI 能正确渲染改名后的展示数据。
- 对象池、UI Motion 等保留 `Snapshot` 命名的系统行为不回退。

若无法执行完整 Unity 验证，必须在交付说明中明确未验证项和原因。

## 9. 风险与注意事项

- 重命名类型会影响引用范围，应按模块逐步修改并及时编译。
- 如果某些类型被 Unity 序列化、反射或字符串引用，需要先确认引用方式。
- 事件改显式调用时要防止初始化顺序问题，尤其是场景对象引用和运行时生成玩家。
- 不建议一次性引入服务定位器或大型消息框架，否则会把当前问题换成新的隐藏依赖。
- 若某个事件已经被多个不相关系统消费，应先确认它是“通知”还是“命令”。通知保留，命令拆分。
