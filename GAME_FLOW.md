# 游戏流程说明

## 目标流程

当前游戏主流程按下面这条链路设计：

1. 进入游戏后先到 `Menu`
2. 点击开始后进入 `CharacterSelection`
3. 角色确认后进入 `Game`
4. 每一波结束后：
   - 如果还有下一波：
     - 本波升过级：先进入 `WaveTransition` 选择升级
     - 升级完成后进入 `Shop`
     - 如果本波没升级：直接进入 `Shop`
     - 商店点击继续后回到 `Game` 开下一波
   - 如果已经没有下一波：进入 `StageComplete`
5. 玩家战斗中死亡时进入 `GameOver`
6. 游戏中可随时暂停，打开 `GamePauseMenu`

---

## 当前代码中的状态定义

`GameState` 当前包含这些状态：

- `Menu`
- `CharacterSelection`
- `Game`
- `GameOver`
- `StageComplete`
- `WaveTransition`
- `Shop`

---

## 当前已确认正常的流程

### 1. 主菜单 -> 角色选择
- `MenuUIPage` 点击开始时发送 `MenuStartClickedEvent`
- `GameManager` 收到后把状态切到 `CharacterSelection`

### 2. 角色选择 -> 正式战斗
- `CharacterSelectUIPage` 点击确认时发送 `CharacterSelectionCompletedEvent`
- `GameManager` 在 `CharacterSelection` 状态下收到后切到 `Game`
- 进入 `Game` 时会确保运行时实例化玩家，并发送 `PlayerSpawnedEvent`
- 首次进入 `Game` 会发送 `StartFirstWaveRequestedEvent`

### 3. 波次结束后的分流
- `WaveManager` 在时间结束后发送 `WaveCompletedEvent`
- `GameManager` 根据当前波次和升级情况决定后续状态：
  - 还有下一波且本波升过级 -> `WaveTransition`
  - 还有下一波且本波没升级 -> `Shop`
  - 没有下一波 -> `StageComplete`

### 4. 升级页 -> 商店
- `WaveTransitionManager` 负责过渡期流程
- 当前逻辑支持：
  - 如果有宝箱，先走宝箱选择
  - 然后进入升级选择
  - 升级点用完后发送 `UpgradeSelectionCompletedEvent`
- `GameManager` 收到后切到 `Shop`

### 5. 商店 -> 下一波战斗
- `ShopUIPage` 点击继续时发送 `ShopContinueClickedEvent`
- `GameManager` 收到后切回 `Game`
- 从 `Shop` 或 `WaveTransition` 回到 `Game` 时会发送 `StartNextWaveRequestedEvent`

### 6. 战斗中死亡 -> GameOver
- `HealthComponent` 在实体死亡时发送 `EntityDiedEvent`
- `GameManager` 现在会监听玩家自己的死亡事件
- 如果死亡实体就是当前玩家，则切到 `GameOver`

### 7. 暂停
- `GamingUIPage` 点击暂停按钮时发送 `PauseGameRequestedEvent`
- `GameManager` 负责真正暂停，并广播 `PauseStateChangedEvent`
- `GameStateUIController` 根据暂停事件打开/关闭 `GamePauseMenu`
- `PauseMenuFlowController` 负责“先关菜单，再继续 / 回主菜单”

---

## 本次检查后补上的内容

### 1. 修正了通关后的状态走向
之前波次结束时，如果已经没有下一波，代码仍然会继续落到商店链路。

现在已经改成：
- 没有下一波时直接进入 `StageComplete`

### 2. 补上了玩家死亡进入 `GameOver`
之前代码里有 `GameOver` 页面和状态，但主流程里没有把玩家死亡真正接到 `GameOver`。

现在已经补上：
- `GameManager` 监听 `EntityDiedEvent`
- 当前玩家死亡时进入 `GameOver`

### 3. 新增了结算页占位脚本
已新增：
- `StageCompleteUIPage`

并新增事件：
- `StageCompleteRestartClickedEvent`
- `StageCompleteReturnToMenuClickedEvent`

### 4. 补上了结算页按钮逻辑
`GameManager` 现在已经支持：
- 结算页点击“再来一局” -> 重开场景
- 结算页点击“回到主菜单” -> 回菜单并重载场景

### 5. 修正了战斗 UI 对运行时生成玩家的绑定
`GamingUIPage` 现在会：
- 打开时尝试绑定当前玩家血量
- 如果玩家是运行时生成的，也会在收到 `PlayerSpawnedEvent` 后重新绑定

---

## 目前和你描述相比的结论

你的描述整体是正确的，建议将正式流程定义为：

```text
Menu
-> CharacterSelection
-> Game
-> WaveCompleted
   -> if hasMoreWaves == false: StageComplete
   -> else if 本波升级过: WaveTransition
        -> UpgradeSelectionCompleted -> Shop
   -> else: Shop
-> ShopContinue -> Game
```

并且还要并行成立这两条支线：

```text
Game 中死亡 -> GameOver
Game 中暂停 -> GamePauseMenu -> 继续 / 回主菜单
```

---

## 当前仍需你在 Unity 里补的配置

本次代码层已经把 `StageComplete` 流程和占位脚本补上了，但如果想让它真正显示独立页面，还需要在 Unity 资源里补一条 UI 配置：

1. 创建一个 `UI Stage Complete.prefab`
2. 绑定 `StageCompleteUIPage` 脚本
3. 在 `UIPrefabCatalog.asset` 里注册这个 prefab

### 当前现状
为了保证流程先跑通，`GameStateUIController` 里的 `StageComplete` 状态目前先复用了 `GameOverUIPage` 作为临时显示页。

也就是说：
- **状态已经是 `StageComplete`**
- **事件已经分离成结算页专用事件**
- **但视觉上暂时还是复用现有 GameOver 页面，直到独立 prefab 配好**

---

## 推荐的后续收尾项

### 高优先级
1. 给 `StageCompleteUIPage` 配一个独立 prefab
2. 在 `UIPrefabCatalog` 中注册它
3. 把 `GameStateUIController` 的 `StageComplete` 显示切回独立结算页

### 中优先级
1. 给结算页增加本局统计占位字段
   - 存活波次
   - 击杀数
   - 获得金币
   - 角色名
2. 明确“宝箱选择是否只在升级页内出现”这条产品规则
3. 确认 `MenuUIPage` 上未使用的 `characterSelectButton` 是否需要移除

---

## 流程总结

当前主流程可以概括为：

```text
主菜单
-> 角色选择
-> 战斗
-> 波次结算（升级 / 商店）
-> 下一波战斗
-> 全部波次完成后进入结算占位
```

额外分支：

```text
战斗中死亡 -> GameOver
战斗中暂停 -> PauseMenu
```
