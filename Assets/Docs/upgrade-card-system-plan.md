# 升级卡牌系统规划

## 目标

升级卡牌系统用于替换波次过渡中的随机属性三选一，让玩家在每局中形成清晰构筑方向。第一阶段只覆盖波次间升级选择，优先复用现有 `PropModifierData`、`FeatureEffectBase`、`PropertiesManager`、`FeatureHost`、`PlayerLevel` 和 UI 事件流。

系统不负责经验、波次、商店、掉落和战斗结算本身，只负责：

- 生成本次可选升级卡牌。
- 展示卡牌名称、图标、描述、稀有度与标签。
- 在玩家选择后应用卡牌效果。
- 记录本局已选卡牌与标签，用于后续抽卡权重。

## 第一阶段范围

第一阶段实现最小可玩闭环：

- `UpgradeCardSO`：单张升级卡牌配置。
- `UpgradeCardPoolSO`：卡池与抽卡参数配置。
- `UpgradeRunState`：本局升级选择记录。
- `UpgradeCardRollService`：生成三选一卡牌。
- `UpgradeCardApplyService`：应用属性、Feature、Buff、武器与经济效果。
- 波次过渡接入：替换原 `WaveTransitionManager` 的随机属性升级。
- UI 接入：`UIUpgradeContainer` 改为显示卡牌快照。

当前仍暂不做：

- 局外永久成长。
- 卡牌图鉴。
- 卡牌合成和进化。
- 复杂羁绊系统。
- 专属卡池编辑器。

## 玩家流程

1. 波次结束。
2. 如果本波获得了升级点，进入升级选择阶段。
3. 系统生成 3 张升级卡牌。
4. 玩家选择 1 张。
5. 卡牌立即生效。
6. 消耗 1 个升级点。
7. 若仍有升级点，重新生成 3 张卡牌。
8. 升级点耗尽后，退出升级选择阶段。

## 卡牌数据

`UpgradeCardSO` 保存静态配置：

- `cardId`：稳定 ID，用于本局计数和后续存档扩展。
- `title`：显示名称。
- `icon`：显示图标。
- `rarity`：稀有度。
- `tags`：构筑标签。
- `maxPickCount`：本局最多选择次数。
- `baseWeight`：基础抽取权重。
- `description`：描述文本。
- `propertyModifiers`：属性修饰列表。
- `specialFeatures`：特殊能力列表。

第一阶段效果只包括属性修饰和 Feature 安装。后续需要武器升级、Buff、商店折扣时，再扩展卡牌效果层。

## 稀有度

第一阶段使用四档：

- `Common`：稳定属性提升。
- `Rare`：明显改变战斗倾向，或带轻微代价。
- `Epic`：构筑核心组件。
- `Legendary`：改变一局打法的强效果。

默认抽卡权重：

- 1-2 波：普通 80，稀有 20。
- 3-5 波：普通 60，稀有 30，史诗 10。
- 6 波以后：普通 45，稀有 35，史诗 17，传说 3。

## 标签

标签用于描述构筑方向，并参与权重：

- 攻击：`Attack`
- 防御：`Defense`
- 暴击：`Critical`
- 攻速：`AttackSpeed`
- 移动：`MoveSpeed`
- 拾取：`Pickup`
- 经济：`Economy`
- 武器：`Weapon`
- 近战：`Melee`
- 远程：`Ranged`
- 投射物：`Projectile`
- 回复：`Recovery`
- 低血：`LowHealth`
- 范围伤害：`AreaDamage`

玩家已选标签越多，同标签卡牌权重越高，但不会完全锁死卡池。

## 模块职责

### 数据层

- `UpgradeCardSO`：卡牌静态配置，不保存运行时状态。
- `UpgradeCardPoolSO`：卡池、选项数量、稀有度权重和标签加权参数。

### 逻辑层

- `UpgradeRunState`：记录本局已选卡牌次数、标签次数、上一轮出现卡牌。
- `UpgradeCardRollService`：根据卡池、波次和运行状态生成候选卡牌。
- `UpgradeCardApplyService`：把卡牌效果应用到玩家组件。

### 表现层

- `UpgradeCardOptionSnapshot`：UI 展示快照。
- `UIUpgradeContainer`：只展示快照并发布选择事件。
- `WaveTransitionUIPage`：监听卡牌选项变化，刷新三选一 UI。

### 编排层

- `WaveTransitionManager`：负责过渡阶段推进、消耗升级点、调用抽卡和应用服务。

## 通信规则

- 波次过渡内部使用直接引用和服务类，避免把抽卡逻辑散到 UI。
- UI 通过 `GameEventBus` 接收快照、发布点击索引。
- 卡牌应用通过现有组件接口完成：
  - 属性：`PropertiesManager.AddModifiers`
  - Feature：`FeatureHost.InstallFeature`

## 已实现效果类型

当前版本除了属性修饰和 Feature 安装，还包含以下卡牌效果：

- `GrantCurrencyUpgradeCardEffect`：立即获得金币。
- `ApplyBuffUpgradeCardEffect`：立即获得指定 Buff。
- `WaveStartBuffUpgradeCardEffect`：每波开始自动获得指定 Buff。
- `UpgradeRandomEquippedWeaponCardEffect`：随机提升一把已装备武器等级。
- `AddRandomWeaponUpgradeCardEffect`：获得指定或随机武器。
- `EconomyBonusUpgradeCardEffect`：商店折扣、免费刷新、波次金币奖励加成。

经济效果已接入 `ShopManager` 和 `WaveRewardManager`。商店显示价格、购买价格、刷新可用性都会读取运行时经济加成。

## 默认卡池

默认卡池必须由 `UpgradeCardSystemBuilder` 生成到 `Assets/Resources/Data/UpgradeCards/Default Upgrade Card Pool.asset`。运行时不会再生成代码兜底卡；如果资源缺失，系统会直接报错，方便尽早暴露资源配置问题。

正式默认卡池包含 20 张卡，覆盖属性卡、武器卡、Buff 卡、经济卡和少量传说 Feature 卡。当前资源生成菜单为 `Survivors/Upgrades/Rebuild Upgrade Card System`。

## 测试场景

专用测试场景为 `Assets/Scenes/Upgrade Card Test Scene.unity`。场景启动后会：

- 生成默认玩家。
- 给予测试金币。
- 给予多个升级点。
- 打开波次过渡升级页。
- 使用正式默认卡池进行三选一卡牌测试。

该场景用于快速验证抽卡、选择、连续升级点消耗、属性卡、Buff 卡、武器卡和经济卡的运行链路。

## 后续阶段

后续阶段：

- 按角色、武器、波次标签调整卡池。
- 增加互斥条件和前置条件。
- 增加稀有度 UI 视觉表现。
- 构筑统计和调试面板。
- 卡牌图鉴。
- 局外成长与解锁。
