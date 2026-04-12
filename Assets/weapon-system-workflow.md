# Weapon System Workflow

## 当前结论

- `Weapon` 负责通用索敌、攻击节奏、基础朝向与运行时属性。
- `MeleeWeapon` / `RangeWeapon` 负责各自攻击实现与序列事件消费。
- 武器朝向来源于持有者 `Entity`，不是 `PlayerController`。
- 固定动作帧不受 `Range` 影响；动态动作帧才结合 `Range` 与目标位置解算。

## 朝向与索敌

- 持有武器的对象必须是某个 `Entity`。
- `Entity` 当前最小契约：
  - `IsMoving`
  - `CurrentFacingDirection`
- 武器无目标时：
  - 若持有者正在移动，则朝 `CurrentFacingDirection`
  - 若持有者不在移动，则保持当前记录的瞄准方向
- 武器攻击中：
  - 保持当前攻击方向
  - 不因目标短暂丢失而立即大转向
  - 攻击结束后再平滑过渡到新方向
- 索敌统一通过 `EntityExtensions.FindClosestTargetInRange(...)`

## 动作序列

### Fixed

- `WeaponMotionPositionMode.Fixed`
- `localPosition` 直接表示武器本地空间中的写死轨迹
- 不乘 `RuntimeStats.Range`
- 适合完全手工指定的挥舞、后拉、枪械后坐等动作

### DynamicFromTarget

- `WeaponMotionPositionMode.DynamicFromTarget`
- 当前仅保留 `TowardTargetClampedRadius`
- 最终落点由：
  - 当前目标方向
  - `dynamicMinNormalizedReach`
  - `dynamicMaxNormalizedReach`
  - `RuntimeStats.Range`
 共同决定
- 适合长枪刺击、随目标远近变化的前送动作

## 使用建议

### 需要固定表现时

使用 `Fixed`：

- 动作完全按你写死的轨迹走
- 不希望因武器 Range 变化而变形
- 例如：大锤蓄力后拉、枪械后坐、定制横扫轨迹

### 需要随目标远近变化时

使用 `DynamicFromTarget`：

- 近目标打短一点
- 远目标打长一点
- 但始终被最小/最大 reach 区间约束

## 当前维护原则

- 不把输入控制层耦合到武器系统
- 不把近战/远程细节堆进 `Weapon`
- 不混用固定帧和动态帧语义
- 有新的高频约束时，先沉淀到项目规则文件
