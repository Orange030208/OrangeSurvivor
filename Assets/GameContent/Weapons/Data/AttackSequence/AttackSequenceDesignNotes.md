# 武器攻击序列设计说明

## 核心原则

武器攻击序列只负责表达武器自身在攻击过程中的运动，不应该用动画序列去补偿不属于动画层的玩法规则。

- 近战、冲刺、盾牌撞击等需要武器视觉上接近目标的攻击，可以根据目标距离做重定向。
- 枪械攻击通常应该使用固定动画。子弹飞行、命中检测、敌人距离由投射物或武器逻辑处理，不应该通过缩放武器动画来解决。

## 距离重定向

需要目标相关运动时，优先使用 `referenceTargetOffset: {x: 0, y: 1}` 作为动画制作参考。

- 突刺、盾牌撞击、冲刺攻击等只需要前向距离缩放的攻击，使用 `retargetScaleWeight: {x: 0, y: 1}`。
- 除非攻击本身就是横扫或侧向摆动，否则保持 `localPositionX` 稳定。
- 动画中最远的前向 `localPositionY` 不要超过 `referenceTargetOffset.y`。实践上建议最大值控制在 `0.95` 到 `0.98`，这样能接近目标但不越过参考距离。
- 长距离位移动画需要更多采样。被距离重定向放大后，20 帧以上的动作关键帧更容易保持连贯。

## 目标偏移策略

`targetOffsetMode` 决定攻击序列开始时，运行时传入的目标偏移来自真实敌人位置，还是沿锁定攻击方向打满武器 `Range`。

- `ActualTarget`：默认策略。序列按当前敌人的实际命中点做距离重定向，近处敌人会让动作缩短。适合横劈、环绕扫击、贴近目标的普通近战。
- `MaxRangeAlongAimDirection`：最大距离策略。序列先用真实敌人位置锁定攻击方向，再用 `origin + lockedDirection * Range` 作为重定向目标。适合突刺、冲撞、盾击等希望即使目标很近也打满路径的攻击。
- 不要用 `MaxRangeAlongAimDirection` 修复横劈命中问题。横劈近处打不到通常应调整命中盒、弧线路径、命中窗口或 `ActualTarget` 下的参考距离，而不是强制把动作拉到最远。
- 该策略只影响动作序列目标偏移和基于 `pendingTargetPosition` 的发射方向，不移动玩家或武器持有者本体。

推荐配置：

- 横劈类：`targetOffsetMode: ActualTarget`，`retargetScaleWeight: {x: 0, y: 1}`。
- 突刺、蓄力突刺、冲撞：`targetOffsetMode: MaxRangeAlongAimDirection`，`retargetScaleWeight: {x: 0, y: 1}`。
- 枪械固定后坐力：`targetOffsetMode: ActualTarget`，`retargetScaleWeight: {x: 0, y: 0}`。

## 反方向权重

`oppositeDirectionRetargetWeight` 用于限制“和参考目标方向相反”的关键帧被目标距离缩放的程度。实际有效权重为：

`effectiveWeight = retargetScaleWeight * oppositeDirectionRetargetWeight`

以常见的 `referenceTargetOffset: {x: 0, y: 1}` 为例，正向攻击使用 `retargetScaleWeight.y`，后向蓄力或后坐的负数 `localPositionY` 使用 `retargetScaleWeight.y * oppositeDirectionRetargetWeight.y`。

- 后向蓄力不希望随敌人距离变长时，使用 `oppositeDirectionRetargetWeight: {x: 1, y: 0}`。
- 如果希望后向蓄力轻微随距离增强，可以把 `oppositeDirectionRetargetWeight.y` 调到 `0.1` 到 `0.25`。
- 横劈通常仍保持 `retargetScaleWeight.x: 0`，因此 `oppositeDirectionRetargetWeight.x` 对默认横劈没有实际影响，保持 `1` 即可。
- 枪械固定后坐力通常使用 `retargetScaleWeight: {x: 0, y: 0}`，此时反方向权重不会产生影响。

## 枪械固定后坐力

枪械射击不需要根据敌人距离缩放动画。

- 使用 `retargetScaleWeight: {x: 0, y: 0}`。
- 使用 `targetOffsetMode: ActualTarget`。
- 除非武器设计上有明确蓄力，否则 `SpawnProjectile`、`PlaySfx`、`PlayVfx` 应放在 `normalizedTime: 0`，让开火立即发生。
- 后坐力沿本地 Y 轴向后移动，使用负数 `localPositionY`，然后回到 `0`。
- 直线后坐应保持 `localPositionX: 0`。横向抖动会让枪械手感不稳定，除非某个武器明确需要混乱感，否则不要加入。
- 固定后坐力不需要大量帧数。通常 5 到 8 个动作关键帧已经足够。

推荐后坐力范围：

- 轻型手枪或冲锋枪：峰值 `localPositionY` 约 `-0.12` 到 `-0.20`，建议在 `0.45` 到 `0.55` 前后回正。
- 重型手枪、霰弹枪或强力步枪：峰值 `localPositionY` 约 `-0.28` 到 `-0.36`，建议在 `0.55` 到 `0.65` 前后回正。

## 时间节奏

盾牌撞击或冲刺攻击：

- 从默认姿态开始。
- 使用 `targetOffsetMode: MaxRangeAlongAimDirection`，保证近处敌人触发时仍能沿锁定方向打满路径。
- 先保留一段可见的蓄力或预备动作。
- 后向蓄力使用负数 `localPositionY` 时，通常配合 `oppositeDirectionRetargetWeight.y: 0`，避免蓄力后撤距离被远处敌人放大。
- 蓄力后快速向前移动。
- 命中窗口应放在前冲过程中，而不是攻击刚开始时。
- 命中窗口应在前冲峰值之后不久关闭。

枪械后坐：

- `normalizedTime: 0` 立即开火。
- 后坐峰值应快速到达，通常在 `0.10` 到 `0.18`。
- 随后平滑恢复到默认位置。
- 结束时回到默认姿态，并保持 `restoreDefaultPoseOnComplete` 开启。

## 横劈轨迹

横劈不是简单的左右线性移动，也不应该做成从中心向两侧发散的扇形。更稳定的做法是让武器路径接近上半圆：左右端点较低，中心最高，形成明确的弧线扫击。

- 横劈可以使用 `retargetScaleWeight: {x: 0, y: 1}`，只让前向距离随目标缩放，横向跨度保持固定。
- 横劈应使用 `targetOffsetMode: ActualTarget`。近处目标需要缩短前向距离，否则扫击容易越过近处敌人。
- 如果横劈有明显后向蓄力，使用 `oppositeDirectionRetargetWeight: {x: 1, y: 0}`，让蓄力后撤不随敌人远近变化。
- 主扫击路径应优先接近半圆弧，而不是心形、扇形或中心收束形。
- 左右端点的 `localPositionY` 应明显低于中心峰值。例如端点在 `0.07` 到 `0.12`，中心峰值在 `0.95` 到 `0.98`。
- 起手蓄力应停在一侧端点附近，而不是从原点持续斜拉到端点。否则预览轨迹会出现明显扇形“骨架”。
- 横向位移是横劈的主要表现来源，允许 `localPositionX` 有较大跨度，但应保持连续单向扫过，避免来回抖动。
- 旋转方向需要和武器视觉朝向匹配。若剑身看起来像“内八”或向内夹，需要整体反转 Z 轴旋转符号。

普通横劈推荐范围：

- 横向跨度：`localPositionX` 约 `-0.9` 到 `0.9`。
- 中心峰值：`localPositionY` 约 `0.95` 到 `0.98`。
- 动作帧数：至少 30 帧，保证弧线预览和远距离重定向后仍然连贯。
- 主扫击窗口：可以覆盖约 `0.31` 到 `0.70`。

重型横劈推荐范围：

- 横向跨度：`localPositionX` 可扩大到约 `-1.15` 到 `1.18`。
- 起手蓄力：在左侧或右侧端点附近短暂停顿，强化蓄力感。
- 主扫击应更快，推荐集中在约 `0.30` 到 `0.58`，必要时可进一步压缩到 `0.30` 到 `0.52`。
- 旋转峰值可提高到约 `80` 度左右，但要避免剑身方向错误。

## 现有示例

- `Shield Bash Attack Sequence.asset`：带距离重定向的前冲盾击，26 个动作帧，最大前向 Y 约 `0.96`。
- `Gun Shot Attack Sequence.asset`：轻型固定后坐力，7 个动作帧，后坐峰值约 `-0.18`。
- `Gun Shot Heavy Recoil Attack Sequence.asset`：较强固定后坐力，7 个动作帧，后坐峰值约 `-0.32`。
- `Melee Horizontal Slash Attack Sequence.asset`：普通横劈，31 个动作帧，路径接近上半圆，横向跨度约 `-0.92` 到 `0.9`。
- `Melee Heavy Horizontal Slash Attack Sequence.asset`：重型横劈，31 个动作帧，横向跨度约 `-1.18` 到 `1.15`，主扫击更快。
- `Melee Quick Thrust Attack Sequence.asset`：无后撤蓄力的高速直刺，18 个动作帧，前刺峰值 Y 约 `0.98`，命中窗口前置到前冲初段。

这些示例的推荐策略：

- 盾击、突刺、蓄力突刺：`targetOffsetMode: MaxRangeAlongAimDirection`。
- 普通横劈、重型横劈：`targetOffsetMode: ActualTarget`。
- 枪械后坐力：`targetOffsetMode: ActualTarget`，`retargetScaleWeight: {x: 0, y: 0}`。
