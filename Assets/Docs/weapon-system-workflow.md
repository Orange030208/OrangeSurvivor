# Weapon System Workflow

## 1. 目标与整体思路

当前武器系统的核心目标是：

- 让 `Weapon` 只负责通用攻击节奏、索敌、朝向与运行时属性
- 让 `MeleeWeapon` / `RangeWeapon` 只负责各自攻击实现
- 让动作表现通过 `AttackSequenceDefinitionSO` 数据驱动
- 让命中窗口、发射、SFX、VFX 都挂在同一套序列事件上
- 让动态距离只在明确需要的轴上生效，不污染固定动画轨迹

一句话概括：

`**Weapon` 决定“何时打”和“朝哪打”，子类决定“怎么打”，序列资源决定“看起来怎么打”。**

---

## 2. 核心组成

### `Weapon`

负责：

- 索敌
- 攻击冷却推进
- 攻击前朝向对齐
- ac
- 统一攻击起手入口

不负责：

- 近战命中窗口细节
- 远程发射细节
- 输入读取
- 玩家专属控制

### `MeleeWeapon`

负责：

- 播放近战序列
- 基于目标预解算动态关键帧
- 开关命中窗口
- 在命中窗口内持续做碰撞盒检测

### `RangeWeapon`

负责：

- 播放远程序列
- 消费 `SpawnProjectile` 事件
- 构造投射物攻击上下文
- 单发 / Burst / Spread / Nova 发射

### `WeaponSequenceBridge`

负责：

- 把场景里的 `Transform` 和纯代码序列播放器连起来
- 转发序列事件
- 转发序列完成事件

### `WeaponMotionSequencePlayer`

负责：

- 根据 `motionKeyframes` 插值位移与旋转
- 根据 `eventKeyframes` 推进逻辑事件
- 按实际播放时长把归一化时间映射到运行时

### `AttackSequenceDefinitionSO`

负责：

- 保存动作关键帧数据
- 保存事件关键帧数据
- 作为近战 / 远程共用的攻击序列资源

### `WeaponDataSO`

负责：

- 提供武器 prefab
- 提供武器基础属性
- 可选提供默认攻击序列
- 提供弹射物定义列表
- 提供攻击表现相关基础配置

### `Entity`

负责：

- 作为武器持有者语义主体
- 提供最小朝向契约：`IsMoving`、`CurrentFacingDirection`

---

## 3. 运行时总流程

### 通用攻击流程

1. `Weapon.Update()`
2. `TickTargeting()` 刷新当前目标与当前期望朝向
3. `TickWeapon()` 推进攻击冷却
4. 满足以下条件后允许起手：
  - 有目标
  - 冷却完成
  - `CanStartAttack()` 为真
  - 当前武器朝向已经足够接近目标方向
5. 调用子类 `BeginAttack(target)`
6. 子类播放攻击序列
7. 序列事件驱动近战命中或远程发射
8. 序列完成后调用 `CompleteAttackCycle()` 结束本轮攻击

---

## 4. 朝向与索敌规则

### 持有者约束

- 武器通过 `GetComponentInParent<Entity>()` 找到持有者
- 武器索敌主体是持有者 `Entity`
- 不把 `Weapon` 自己当作实体语义主体

### 索敌规则

- 统一通过 `ownerEntity.FindClosestTargetInRange(RuntimeStats.Range, targetLayerMask)`
- `Range` 同时决定：
  - 索敌半径
  - 动态关键帧距离解算上限
  - 调试 gizmo 显示半径

### 平时朝向

有目标时：

- 朝当前目标方向插值转动

无目标时：

- 若持有者正在移动，朝 `CurrentFacingDirection`
- 若持有者静止，保持上一次有效瞄准方向

### 攻击前对准判定

冷却完成后不会立刻攻击，还会检查：

- 当前 `transform.up`
- 与期望攻击方向之间的夹角
- 是否小于 `attackStartAimToleranceDegrees`

这样做的目的：

- 避免远程子弹发射时视觉还没转正
- 避免近战动作方向和目标方向不一致

### 攻击中锁定朝向

- 进入 `IsAttacking` 后保持当前攻击方向
- 攻击过程中目标即使离开范围，也不立刻大角度跳转
- 攻击结束后再重新追新目标或持有者朝向

---

## 5. 运行时属性与 Range 的计算

### 武器运行时最终使用的是 `WeaponRuntimeStats`

字段：

- `Damage`
- `AttackInterval`
- `Range`
- `CriticalChance`
- `CriticalMultiplier`

### 基础计算来源

`Weapon.BuildRuntimeStats()` 会把两部分相加/合并：

1. 武器数据本身的属性
  - 来自 `WeaponDataSO`
  - 先经过 `WeaponPropsCalculator.GetProps(WeaponData, Level)` 等级换算
2. 持有者当前属性
  - 来自 `PropertiesManager`

### 当前等级公式

`WeaponPropsCalculator` 当前是简单线性倍率：

- `multiplier = 1 + level / MaxLevel`
- `MaxLevel = 6`

也就是武器基础属性会按等级整体放大。

### 最终公式

#### Damage

- `damage = weaponAttack + playerAttack`

#### AttackSpeed / AttackInterval

- `finalAttackSpeed = weaponAttackSpeed * playerAttackSpeedMultiplier`
- `attackInterval = 1 / finalAttackSpeed`

所以：

- 攻速越高，攻击间隔越短
- 运行时真正用于冷却判断的是 `AttackInterval`

#### Critical

- `criticalChance = weaponCriticalChance + playerCriticalChance`
- 最后会 `Clamp01`
- `criticalMultiplier = weaponCriticalMultiplier + playerCriticalBonus`
- 最低不会小于 1

#### Range

- `range = weaponRange + playerRange`
- 最后至少为 `0.1`

### Range 在系统中的实际作用

`RuntimeStats.Range` 不是只给索敌看的，它同时影响：

1. **索敌半径**
2. **近战动态关键帧的距离解算**
3. **调试可视化半径**
4. **部分表现层对攻击可达范围的理解**

### 一个关键约定

- **固定轴不吃 Range**
- **只有设成 `DynamicFromTarget` 的轴才会按 Range 解算**

这点非常重要：

- 想写死动作轨迹，就保持 `Fixed`
- 想让某个轴随攻击距离变化，才切到 `DynamicFromTarget`

---

## 6. 攻击序列的时间模型

### 序列时长

`AttackSequenceDefinitionSO.Duration` 表示这套动作原始总时长。

所有关键帧时间都使用：

- `normalizedTime`：0 到 1

实际运行时：

- `实际秒数 = normalizedTime * 实际播放时长`

### 实际播放时长并不一定等于资源原始 Duration

`Weapon.ResolveAttackSequenceDuration()` 会根据攻速和占用率压缩：

- `attackInterval = RuntimeStats.AttackInterval`
- `reservedWindow = attackInterval * WeaponData.AttackSequenceOccupancy`
- `effectiveDuration = Min(sequence.Duration, reservedWindow)`

### 这意味着什么

如果：

- 原始动画 1.2 秒
- 当前攻速对应攻击间隔只有 0.6 秒
- `AttackSequenceOccupancy = 0.85`

那动作最多只允许占：

- `0.6 * 0.85 = 0.51 秒`

最终会压缩到 `0.51 秒` 播放。

### 设计目的

- 不让慢动画拖垮高攻速武器节奏
- 短动作保持原样，不会无脑拉长
- 给每次攻击留出一点动作后的呼吸空间

---

## 7. 序列播放器如何工作

### `WeaponMotionSequencePlayer`

播放时会：

1. 缓存默认待机姿态
2. 记录当前序列和覆盖位移数据
3. 每帧推进 `elapsed`
4. 计算 `normalizedTime = elapsed / playbackDuration`
5. 在 `motionKeyframes` 中找到当前所处区间
6. 对位移和旋转做插值
7. 推进 `eventKeyframes`
8. 播放结束时按配置恢复默认姿态

### 动作关键帧插值

位移：

- 在相邻两帧之间 `LerpUnclamped`

旋转：

- 在相邻两帧之间 `SlerpUnclamped`

### Ease 的使用方式

每个关键帧都有 `ease`

当前实现中：

- 一个段落的插值曲线由“目标帧”的 `ease` 决定
- `CustomCurve` 时使用该帧自己的 `AnimationCurve`

也就是说在作者视角里：

- **这一段怎么进入下一帧，看下一帧的 ease**

---

## 8. 动作关键帧怎么写

### 当前坐标约定

项目当前约定：

- 武器模型默认“竖着放”
- 待机姿态下沿 `local +Y / transform.up` 指向前方

因此：

- `localPositionX` = 横向摆动 / 横拉 / 横扫
- `localPositionY` = 沿武器前方的前送 / 后拉
- `localEulerAngles.z` = 2D 平面里的可视旋转主轴

### 写帧时最重要的原则

#### 固定轨迹用 Fixed

适合：

- 后摇
- 枪械后坐
- 夸张横甩
- 完全作者手工轨迹

#### 距离自适应用 DynamicFromTarget

适合：

- 刺击前送
- 挥砍中沿目标距离变化的前伸
- 只想让某一个轴跟目标远近变化

### 逐轴模式

每一帧都有：

- `xPositionMode`
- `yPositionMode`

所以你可以写出：

#### 常见近战写法

- `x = Fixed`
- `y = DynamicFromTarget`

效果：

- 横向蓄力和横扫保留作者风格
- 前送距离跟目标远近变化

#### 完全固定写法

- `x = Fixed`
- `y = Fixed`

效果：

- 完全不依赖目标距离
- 每次都播同一条轨迹

### DynamicFromTarget 的当前策略

当前只保留：

- `TowardTargetClampedRadius`

它会做的事：

1. 计算目标在武器本地空间中的距离
2. 用 `targetDistance / attackRange` 得到归一化目标距离
3. 再把它夹到该轴自己的 `Min/Max Normalized Reach` 区间里
4. 再乘回 `attackRange`
5. 再保留该轴原本的正负号方向

### 关键结论

- `Min/Max Normalized Reach` 决定这个动态轴允许落在攻击半径的哪个区间
- 不是所有动态帧都一定打到最大范围
- 过渡帧可以只落在 0.2~0.4
- 命中帧可以落在 0.8~1.0

这正是“蓄力阶段短、爆发阶段长”的关键。

---

## 9. 怎么作者化一套近战序列

推荐思路：

1. `0.00` 起始待机
2. 前几帧做左拉 / 后拉 / 抬手
3. 在蓄力顶点停一小段
4. 劈砍段加 3~5 帧，让动作平滑
5. 命中后短暂停顿
6. 再回收

### 近战常见分工

- `X`：蓄力、横甩、横扫风格
- `Y`：前送距离与打击可达感
- `Z 旋转`：视觉切割感、鞭甩感、重砸感

### 经验建议

#### 如果动作太“假”

优先检查：

- 劈砍段是不是帧太少
- 命中后是不是没有停顿
- 回收是不是太快

#### 如果动作太“飘”

优先检查：

- 命中帧的 reach 是否过高
- 横向摆动是否过大
- ease 是否过于弹性

#### 如果近身目标不好打

优先检查：

- 命中关键帧是否设成动态轴
- `MinNormalizedReach` 是否太大

---

## 10. 事件关键帧怎么写

事件帧用于定义逻辑时机，而不是轨迹。

当前支持：

- `OpenHitWindow`
- `CloseHitWindow`
- `SpawnProjectile`
- `PlaySfx`
- `PlayVfx`

### 近战

常见写法：

- 在真正进入挥砍主段前后开窗
- 在命中表现结束附近关窗

### 远程

常见写法：

- `SpawnProjectile` 放在枪口前冲或后坐前一瞬
- `PlaySfx` 与发射同点或提前一点
- `PlayVfx` 与枪口火光同点或略后一点

### 重要原则

- 事件时机服务于逻辑判定
- Motion 帧服务于视觉轨迹
- 二者要互相贴合，但职责不同

---

## 11. 近战运行机制

### 起手

`MeleeWeapon.BeginAttack(target)` 会：

- 标记 `IsAttacking = true`
- 清空上一轮命中窗口状态
- 根据目标预计算动态关键帧覆盖表
- 计算有效播放时长
- 播放序列

### 动态关键帧不是实时追目标

注意：

- 不是播放到某一帧时再现算目标位置
- 而是在**攻击开始瞬间**先算出这次攻击专属的动态轨迹

这样做的好处：

- 动作更稳定
- 攻击中不会因为目标稍微挪动导致武器轨迹抖动
- 保留“攻击方向锁定”的一致性

### 命中窗口

- `OpenHitWindow(windowId)` 打开窗口
- `CloseHitWindow(windowId)` 关闭窗口
- 窗口开启期间，`TickWeapon()` 中持续做碰撞检测

### 防止一窗多次命中同一目标

每个窗口维护：

- `HashSet<HealthComponent>`

这样同一目标在同一个窗口中只会被结算一次。

---

## 12. 远程运行机制

### 起手

`RangeWeapon.BeginAttack(target)` 会：

- 标记 `IsAttacking = true`
- 缓存 `pendingTarget`
- 播放远程序列

### 发射由事件驱动

不是起手立刻发射，而是等收到：

- `SpawnProjectile`

然后根据 payload 决定具体发射模式。

### 支持的模式

#### Default

- 发一发

#### Burst

- 启动协程连发
- `BurstId` 用于避免同一 burst 重复启动

#### Spread

- 围绕当前瞄准方向左右散开

#### Nova

- 按整圆均匀发射

### 发射点选择

优先级：

1. `additionalShootingPoints[spawnPointIndex]`
2. `shootingPoint`
3. `transform`

### 投射物定义选择

优先级：

1. 事件 payload 直接引用的 `ProjectileDefinitionSO`
2. `WeaponDataSO.ProjectileDefinitions` 中第一个可用定义
3. 子节点里能找到的 `Bullet`

---

## 13. 调试与可视化

### Weapon Gizmo

`Weapon.DrawSharedWeaponDebugGizmos()` 会画：

- 当前攻击范围圆
- 序列事件点分布

颜色约定：

- 绿色：开命中窗
- 橙色：关命中窗
- 青色：发射事件
- 黄色：SFX
- 紫色：VFX

### Debug 面板里的几个关键值

- `AttackInterval`
- `SequenceWindowDuration`
- `OriginalSequenceDuration`
- `EffectiveSequenceDuration`
- `CompressionRatio`

调手感时优先看：

- 攻速提高后序列是否被压缩过度
- 动画是否总是比逻辑节奏长

---

## 14. 资源与 prefab 的配置建议

### 近战武器至少要有

- `WeaponSequenceBridge`
- `hitDetectionTransform`
- `hitCollider`
- 可选 `AttackSequenceDefinitionSO`

### 远程武器至少要有

- `WeaponSequenceBridge`
- `shootingPoint`
- 可选 `additionalShootingPoints`
- 可选 `AttackSequenceDefinitionSO`
- 可用的 `ProjectileDefinitionSO`

### `WeaponSequenceBridge.animatedTransform`

推荐指向：

- 真正的可视武器节点

不要轻易指向：

- 带逻辑职责的根节点

这样可以避免：

- 逻辑 root 跟着动作位移乱跑
- 命中盒 / 发射点和视觉层级纠缠

---

## 15. 当前高频约束总结

- `Weapon` 只做通用攻击与瞄准
- 近战细节留在 `MeleeWeapon`
- 远程细节留在 `RangeWeapon`
- 动态距离只作用于明确配置为 Dynamic 的轴
- 固定轴不吃 `Range`
- 攻击必须先转到位再起手
- 攻击中保持当前攻击方向
- 动态关键帧在攻击开始时一次性预解算，不在播放过程中实时追目标
- 序列事件表达逻辑时机，动作帧表达视觉轨迹

---

## 16. 给调动作的人一句话建议

如果你在调一把武器，推荐按这个顺序：

1. 先确认 `Range` 是否合理
2. 再确认攻速下 `EffectiveSequenceDuration` 是否合适
3. 再写 motion 帧的大节奏：蓄力 → 爆发 → 停顿 → 回收
4. 再补事件帧：开窗 / 关窗 / 发射 / SFX / VFX
5. 最后才调 ease、停顿长度、动态 reach 区间

先把**逻辑时序**调对，再把**视觉轨迹**调顺，效率最高。