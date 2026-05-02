# 游戏开发专项规则

## 玩法数值通用规范

### 适用范围

- 设计或修改角色属性、武器属性、升级卡、饰品、Buff、敌人属性、商店经济、经验收益、掉落收益等玩法数值时，必须先查看本节。
- 涉及 `PropType`、`PropModifierData`、`PropModifierType`、`PropertiesManager` 或相关 ScriptableObject 配置时，默认遵循本节单位与结算口径。
- 新增属性或改变属性语义前，应先补充本文件中的单位定义，再改代码和资产。

### 百分比点口径

- 所有可配置的百分比数值，统一使用“百分比点”写法：`1` 表示 `1%`，`10` 表示 `10%`，`50` 表示 `50%`。
- 不要在配置、构造函数或 ScriptableObject 资产中写 `0.01` 表示 `1%`，也不要写 `0.5` 表示 `50%`。
- 运行时消费比例时，由消费代码或属性系统负责换算为 ratio：`ratio = value / 100f`。
- `BaseMultiplier`、`BonusMultiplier`、`FinalMultiplier` 也使用百分比点：`value: 50` 表示乘以 `1.5`，`value: -10` 表示乘以 `0.9`。
- `Add` 是否是百分比点取决于属性本身语义。例如 `CriticalChance +1` 是 `+1%`，但 `MoveSpeed +1` 是移动速度加 `1` 点。

#### 示例

最终移动速度变为 `1.5` 倍，应配置为：

- `propType: MoveSpeed`
- `modifierType: FinalMultiplier`
- `value: 50`

移动速度增加 `50` 点，应配置为：

- `propType: MoveSpeed`
- `modifierType: Add`
- `value: 50`

暴击率增加 `6%`，应配置为：

- `propType: CriticalChance`
- `modifierType: Add`
- `value: 6`

暴击伤害增加 `18%`，应配置为：

- `propType: CriticalPercent`
- `modifierType: Add`
- `value: 18`

### 属性单位定义

| 属性 | 配置单位 | 说明 |
| --- | --- | --- |
| `Attack` | 点数 | `+1` 表示增加 1 点攻击力。乘区使用百分比点。 |
| `AttackSpeed` | 基础值为攻击频率；修饰值为百分比点 | 武器和敌人的基础 `AttackSpeed` 是每秒攻击次数。玩家、Buff、升级卡等修饰值写 `+10` 表示攻速 `+10%`。 |
| `CriticalChance` | 百分比点 | `1` 表示 `1%` 暴击率。 |
| `CriticalPercent` | 百分比点 | `180` 表示暴击伤害为 `180%`；作为修饰值时 `+18` 表示暴击伤害 `+18%`。 |
| `MoveSpeed` | 点数 | 玩家基础移速为 `100`；`+1` 表示增加 1 点移速。乘区使用百分比点。 |
| `MaxHealth` | 点数 | `+1` 表示增加 1 点最大生命。乘区使用百分比点。 |
| `DetectionRange` | 点数 | 检测距离点数；玩家武器索敌不使用它。 |
| `HealthRecoverySpeed` | 点数 | `+1` 表示每秒恢复 1 点生命。 |
| `Armor` | 点数 | 护甲减伤公式为 `Armor / (Armor + 100)`。 |
| `Luck` | 点数 | 掉落或经济概率相关的长期收益属性，`+1` 表示幸运增加 1 点。 |
| `Dodge` | 百分比点 | `1` 表示 `1%` 闪避率。 |
| `LifeSteal` | 百分比点 | `1` 表示 `1%` 生命偷取；当前规则是每打出 `1000` 最终伤害回复 `1` 点生命。 |
| `PickupRadius` | 点数 | 基础拾取半径为点数；乘区使用百分比点。 |
| `ProjectileCount` | 点数 | `+1` 表示增加 1 个投射物。 |
| `ProjectileSpeed` | 点数 | 基础弹速为点数；乘区使用百分比点。 |
| `AttackRange` | 点数 | 玩家武器实际使用的攻击距离属性；乘区使用百分比点。 |
| `ProjectilePierceCount` | 点数 | `+1` 表示增加 1 次穿透。 |
| `KnockbackStrength` | 点数 | 击退强度点数；默认 `10` 约等于击退 `1` 格/1 个世界单位，实际距离会受击退抗性、碰撞和最大速度限制影响；乘区使用百分比点。 |
| `KnockbackResistance` | 百分比点 | `1` 表示 `1%` 击退抗性。 |
| `ExperienceGain` | 百分比点 | `1` 表示 `+1%` 经验获取。 |
| `ShopPriceDiscount` | 百分比点 | `1` 表示 `1%` 商店折扣。 |
| `WaveGoldRewardBonus` | 点数 | `+1` 表示每波额外获得 1 金币。当前标准小额奖励为 `+5`。 |
| `DamageReduction` | 百分比点 | `1` 表示 `1%` 直接伤害减免；先算护甲，再算该减免。 |
| `HealingPower` | 百分比点 | `1` 表示 `+1%` 治疗效果；不影响生命偷取。 |
| `WeaponSlotCount` | 点数 | `+1` 表示增加 1 个武器槽，属于玩法级强属性。 |

### 单位价值参考

- `MoveSpeed +1` 约等于玩家基础移速的 `1%` 移动能力。
- `AttackSpeed +1` 作为修饰值时约等于 `+1%` 攻速。
- `CriticalChance +1` 表示 `+1%` 暴击率。
- `CriticalPercent +1` 表示 `+1%` 暴击伤害。
- `KnockbackStrength +1` 约等于 `0.1` 格击退预算；`+10` 约等于 `1` 格。该属性主要调整命中手感，不建议让常规枪械轻易超过个位数。
- `Armor +1` 在低护甲时约等于接近 `1%` 的等效生命增益，后续边际减伤递减。
- `DamageReduction +1` 是护甲之后的 `1%` 直接减伤，价值高于普通护甲点数，应保守。
- `LifeSteal +1` 表示 `1%` 生命偷取，即每造成 `1000` 最终伤害回复 `1` 点生命。
- `ExperienceGain +1` 与 `ShopPriceDiscount +1` 都是长期雪球属性，设计时应比普通非战斗属性更谨慎。

### 配置与实现要求

- 百分比点属性作为直接属性加成时，优先使用 `PropModifierType.Add`。例如 `ExperienceGain +12%` 应写 `Add, value: 12`，不要写成乘区。
- 对已有点数属性做倍数变化时，使用 `BaseMultiplier`、`BonusMultiplier` 或 `FinalMultiplier`，并用百分比点写值。
- 不要为了表达 `+10%` 在资源中写 `0.1`。如果发现旧数据使用 ratio，应同步迁移到百分比点。
- 代码中消费概率、折扣、吸血、伤害减免等比例时，应明确调用 `PropValueUtility.PercentPointsToRatio` 或等价集中工具。
- 新增 UI 展示逻辑时，百分比点属性直接显示为 `%`；不要再乘以 `100`。
- 修改升级卡生成器时，必须同步修改已存在的升级卡资产，避免重建前后数值不一致。
- 修改武器暴击率、暴击伤害时，武器资产也使用百分比点：`criticalChance: 8` 表示 `8%`，`criticalPercent: 180` 表示 `180%`。
- 武器和敌人的基础 `AttackSpeed` 不按百分比点处理，它仍是攻击频率。只有通过 `PropModifierData` 施加的攻速修饰才按百分比点处理。

## 敌人设计

### 运行时属性修改

- 敌人运行时临时修改攻击范围、移动速度、攻击力、攻速等基础属性时，统一使用 `PropModifierData` 与 `PropertiesManager.AddModifiers/RemoveModifiers`。
- 不要在具体敌人行为脚本里直接改写基础属性、缓存属性结果，或额外传入临时倍率参数绕过属性系统。
- 可调的临时属性变化应放在敌人 ScriptableObject 中，例如 `List<PropModifierData>`，保持 Inspector 可配置、可调试、可复用。
- 临时属性修改必须使用稳定的 `sourceId`，状态进入时添加，状态退出时移除。
- `StopBrain`、`StartBrain`、`OnDisableComponent` 等中断入口必须清理可能残留的 Modifier。
- 同一状态可能重复进入或中断重入时，应避免重复 Add，并保证 Remove 幂等。

#### 示例

“攻击范围扩大 1.5 倍”应配置为：

- `propType: AttackRange`
- `modifierType: FinalMultiplier`
- `value: 50`

#### 例外

- 动画播放速度、状态计时、命中特效、Hit Shape 选择等不属于基础属性结算的数据，可以保留在行为状态或攻击定义中。
- 如果现有属性系统无法表达需求，应优先评估补充 `PropType`、`PropModifierType` 或属性结算扩展，而不是在具体玩法脚本中写死临时分支。
