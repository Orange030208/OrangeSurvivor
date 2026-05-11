# 命中事务流水线设计方案

本文档描述一套面向长期玩法扩展的命中与伤害结算目标架构。它不是当前实现说明，而是后续重构 `HitRequest -> HitResolver -> HealthComponent` 链路时的推荐目标。

目标是让武器、Buff、饰品、技能、敌人状态等系统可以稳定扩展命中规则，同时避免攻击方、受击方、Feature、生命组件之间互相直接改状态。

## 1. 设计目标

- 命中结算只有一个编排入口，避免武器、投射物、Buff 各自扣血。
- `Resolve` 只做纯计算，不扣血、不加 Buff、不播放表现。
- `Apply` 才真正修改生命、触发击退、发布结果事件和执行副作用。
- 攻击方和受击方都只提供规则与监听器，不直接互相操作。
- Feature 只负责在安装时注册规则，在卸载时移除规则。
- 规则按明确阶段执行，避免用模糊 priority 解决前后依赖。
- 每次命中可以输出 Trace，方便定位最终伤害来源。

## 2. 总体链路

```text
Feature / Buff / 装备 / 技能安装
-> 向实体的 CombatRuleRegistry 注册规则与监听器
-> 武器 / 投射物 / 近战命中盒创建 HitRequest
-> CombatService 收集攻击方、受击方、临时来源的规则快照
-> HitResolver 执行纯计算并生成 HitResult
-> HitApplier 将结果应用到目标 HealthComponent
-> HitAppliedListener 执行吸血、附加 Buff、反伤、连锁伤害、表现请求等副作用
```

核心约束：

- `CombatService` 是唯一命中编排者。
- `HitResolver` 不依赖 Unity 组件，不调用 `HealthComponent`。
- `HitApplier` 是唯一可以把 `HitResult` 落到目标身上的对象。
- `HealthComponent` 可以提供规则注册入口，但不能成为战斗系统总控。

## 3. 模块职责

### 3.1 `CombatService`

职责：

- 统一接收命中请求。
- 收集规则快照。
- 调用 `HitResolver.Resolve`。
- 调用 `HitApplier.Apply`。
- 作为所有命中、反伤、连锁伤害的统一入口。

不负责：

- 具体伤害公式。
- 具体规则实现。
- 具体表现播放。

### 3.2 `HitResolver`

职责：

- 执行固定命中生命周期。
- 在明确阶段执行规则。
- 计算防御、随机判定、最终伤害。
- 生成 `HitResult`。

不负责：

- 扣血。
- 击退实体。
- 加 Buff。
- 发布表现事件。

### 3.3 `HitApplier`

职责：

- 根据 `HitResult` 调用目标生命组件。
- 处理有效伤害、闪避、格挡、取消等应用结果。
- 构造 `HitAppliedContext`。
- 执行命中后监听器和命令队列。

不负责：

- 重新计算伤害。
- 重新读取攻击方或受击方属性。

### 3.4 `HealthComponent`

职责：

- 维护当前生命、最大生命、治疗、死亡。
- 应用已经结算好的伤害结果。
- 暴露规则注册门面，方便 Feature 使用。

不负责：

- 计算护甲、闪避、暴击、最终伤害。
- 决定攻击方能否吸血。
- 直接调用 Feature。
- 收集攻击方规则。

### 3.5 `CombatRuleRegistry`

职责：

- 按 sourceId 注册规则和监听器。
- 支持按 sourceId 移除。
- 构建只读快照。
- 缓存排序结果，避免命中热路径反复扫描。

### 3.6 Feature / Buff / 装备 / 技能

职责：

- 在安装时注册规则或监听器。
- 在卸载时移除对应 sourceId。
- 提供配置参数和来源信息。

不负责：

- 自己调用扣血。
- 自己执行命中结算。
- 在 `Resolve` 阶段做副作用。

## 4. 核心数据结构

### 4.1 规则作用范围

```csharp
public enum HitRuleScope
{
    Outgoing,
    Incoming,
    Both
}
```

语义：

- `Outgoing`：装在攻击方身上，只影响该实体造成的命中。
- `Incoming`：装在受击方身上，只影响该实体受到的命中。
- `Both`：同时适用于攻击方和受击方，慎用。

### 4.2 命中来源类型

当前项目已经有 `HitSourceKind`，目标设计应直接复用它，不额外引入新的 `HitTag` 概念。

```csharp
public enum HitSourceKind
{
    Weapon,
    Projectile,
    Feature,
    Buff,
    Explosion,
    Reflect,
    Direct
}
```

用途：

- 区分伤害来源。
- 支持“仅对投射物增伤”“免疫爆炸伤害”等规则。
- 通过 `HitSourceKind.Reflect` 标记反伤，避免反伤再次触发反伤。

如果未来确实需要多个并行标签，例如 `Fire | Projectile | AreaDamage`，可以在保留 `HitSourceKind` 的基础上新增 `HitTags`，但第一版不需要。

### 4.3 命中输入

```csharp
public readonly struct HitRequest
{
    private const float MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity Source { get; }
    public Entity Target { get; }
    public HitSpec Spec { get; }
    public Vector2 HitPoint { get; }
    /// <summary>
    /// 伤害源或攻击发起位置快照，不随 Source 后续移动而改变。
    /// </summary>
    public Vector2 SourcePosition { get; }
    public bool HasKnockbackDirection { get; }
    public Vector2 KnockbackDirection { get; }
    public HitSourceKind SourceKind { get; }
    public Weapon SourceWeapon { get; }

    public HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        HitSourceKind sourceKind,
        Vector2 sourcePosition,
        Weapon sourceWeapon = null)
        : this(
            source,
            target,
            spec,
            hitPoint,
            false,
            Vector2.zero,
            sourcePosition,
            sourceKind,
            sourceWeapon)
    {
    }

    public HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        Vector2 knockbackDirection,
        HitSourceKind sourceKind,
        Vector2 sourcePosition,
        Weapon sourceWeapon = null)
        : this(
            source,
            target,
            spec,
            hitPoint,
            true,
            knockbackDirection,
            sourcePosition,
            sourceKind,
            sourceWeapon)
    {
    }

    private HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        bool hasKnockbackDirection,
        Vector2 knockbackDirection,
        Vector2 sourcePosition,
        HitSourceKind sourceKind,
        Weapon sourceWeapon)
    {
        Source = source;
        Target = target;
        Spec = spec;
        HitPoint = hitPoint;
        SourcePosition = sourcePosition;
        HasKnockbackDirection = hasKnockbackDirection &&
            knockbackDirection.sqrMagnitude > MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE;
        KnockbackDirection = HasKnockbackDirection ? knockbackDirection.normalized : Vector2.zero;
        SourceKind = sourceKind;
        SourceWeapon = sourceWeapon;
    }
}
```

约束：

- `HitRequest` 是输入快照，不保存最终伤害、是否暴击、是否闪避。
- `SourcePosition` 是攻击发起位置快照，不随 `Source` 后续移动而改变。
- `KnockbackDirection` 在请求创建时归一化，避免结算阶段重复猜测方向。
- `SourceKind` 表达伤害来源类别，例如武器、投射物、Buff、反伤。
- `SourceWeapon` 用于武器标签、武器专属规则、统计与表现追踪。
- 反伤、连锁伤害必须创建新的 `HitRequest`，并通过 `CombatService` 再次进入流水线。

### 4.4 命中规格

```csharp
public readonly struct HitSpec
{
    public float BaseDamage { get; }
    public float CritChance { get; }
    public float CritMultiplier { get; }
    public float KnockbackStrength { get; }

    public HitSpec(float baseDamage, float critChance, float critMultiplier, float knockbackStrength)
    {
        BaseDamage = Mathf.Max(0f, baseDamage);
        CritChance = Mathf.Clamp01(critChance);
        CritMultiplier = Mathf.Max(1f, critMultiplier);
        KnockbackStrength = Mathf.Max(0f, knockbackStrength);
    }
}
```

### 4.5 计算上下文

```csharp
public sealed class HitContext
{
    public HitRequest Request { get; }
    public HitTrace Trace { get; }

    public float Damage { get; set; }
    public float CritChance { get; set; }
    public float CritMultiplier { get; set; }
    public float KnockbackStrength { get; set; }
    public float ArmorPenetrationPercent { get; set; }
    public float DodgeChance { get; set; }
    public float DamageReduction { get; set; }

    public bool IsCritical { get; set; }
    public bool IsDodged { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsCancelled { get; set; }

    public HitContext(HitRequest request, HitTrace trace)
    {
        Request = request;
        Trace = trace;
        Damage = request.Spec.BaseDamage;
        CritChance = request.Spec.CritChance;
        CritMultiplier = request.Spec.CritMultiplier;
        KnockbackStrength = request.Spec.KnockbackStrength;
    }
}
```

约束：

- `HitContext` 只在 `Resolve` 期间存在。
- 规则可以修改 `HitContext`，但不能直接改实体状态。

### 4.6 结算结果

```csharp
public readonly struct HitResult
{
    private const float MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity Source { get; }
    public Entity Target { get; }
    public float FinalDamage { get; }
    public float ActualDamage { get; }
    public float KnockbackStrength { get; }
    public Vector2 HitPoint { get; }
    /// <summary>
    /// 伤害源或攻击发起位置快照，不随 Source 后续移动而改变。
    /// </summary>
    public Vector2 SourcePosition { get; }
    public bool HasKnockbackDirection { get; }
    public Vector2 KnockbackDirection { get; }
    public bool IsCritical { get; }
    public bool IsDodged { get; }
    public bool IsBlocked { get; }
    public bool IsCancelled { get; }
    public HitSourceKind SourceKind { get; }
    public Weapon SourceWeapon { get; }

    public bool CanApplyDamage =>
        !IsCancelled && !IsDodged && !IsBlocked && FinalDamage > 0f && Target != null;

    public HitResult WithActualDamage(float actualDamage)
    {
        return new HitResult(
            Source,
            Target,
            FinalDamage,
            actualDamage,
            KnockbackStrength,
            HitPoint,
            HasKnockbackDirection,
            KnockbackDirection,
            IsCritical,
            IsDodged,
            IsBlocked,
            IsCancelled,
            SourceKind,
            SourcePosition,
            SourceWeapon);
    }

    public HitResult(
        Entity source,
        Entity target,
        float finalDamage,
        float actualDamage,
        float knockbackStrength,
        Vector2 hitPoint,
        bool hasKnockbackDirection,
        Vector2 knockbackDirection,
        bool isCritical,
        bool isDodged,
        bool isBlocked,
        bool isCancelled,
        HitSourceKind sourceKind,
        Vector2 sourcePosition,
        Weapon sourceWeapon = null)
    {
        Source = source;
        Target = target;
        FinalDamage = finalDamage;
        ActualDamage = actualDamage;
        KnockbackStrength = knockbackStrength;
        HitPoint = hitPoint;
        SourcePosition = sourcePosition;
        HasKnockbackDirection = hasKnockbackDirection &&
            knockbackDirection.sqrMagnitude > MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE;
        KnockbackDirection = HasKnockbackDirection ? knockbackDirection.normalized : Vector2.zero;
        IsCritical = isCritical;
        IsDodged = isDodged;
        IsBlocked = isBlocked;
        IsCancelled = isCancelled;
        SourceKind = sourceKind;
        SourceWeapon = sourceWeapon;
    }
}
```

说明：

- `FinalDamage` 是结算结果。
- `ActualDamage` 是实际扣除值，例如目标只剩 5 点血但最终伤害 100，实际伤害可记录为 5。
- `HitResult` 必须保留 `SourceKind`、`SourcePosition`、击退方向和 `SourceWeapon`，保证应用层、表现层和统计层不需要回读已经变化的攻击对象状态。

## 5. 规则接口

规则按阶段拆分，而不是所有效果共用一个 `ModifyHit`。

```csharp
public interface IHitRule
{
    int Order { get; }
    HitRuleScope Scope { get; }
    string DebugName { get; }
}

public interface IPreDefenseRule : IHitRule
{
    void Execute(HitContext context);
}

public interface IPreRollRule : IHitRule
{
    void Execute(HitContext context);
}

public interface IPostRollRule : IHitRule
{
    void Execute(HitContext context);
}

public interface IPreFinalDamageRule : IHitRule
{
    void Execute(HitContext context);
}
```

阶段语义：

| 阶段 | 适合规则 | 不适合规则 |
| --- | --- | --- |
| `PreDefense` | 护甲穿透、真实伤害标记、防御忽略 | 暴击后增伤、吸血 |
| `PreRoll` | 修改暴击率、闪避率、格挡率 | 强制扣血、加 Buff |
| `PostRoll` | 强制暴击、强制命中、取消闪避 | 播放特效、治疗 |
| `PreFinalDamage` | 增伤、易伤、最终减伤、击退修正 | 连锁伤害、掉落 |

## 6. 命中后监听器

命中后监听器只在 `Apply` 之后触发。

```csharp
public interface IHitAppliedListener
{
    int Order { get; }
    HitRuleScope Scope { get; }
    string DebugName { get; }
    void OnHitApplied(HitAppliedContext context);
}
```

适合放在监听器中的逻辑：

- 吸血。
- 命中后给目标加 Buff。
- 反伤。
- 连锁伤害。
- 命中后生成资源。
- 请求音效、特效、飘字。

不应该放在监听器中的逻辑：

- 修改本次最终伤害。
- 修改本次是否暴击。
- 修改本次是否闪避。

## 7. 命令队列

命中后监听器不应直接改世界，而是向命令队列追加命令。

```csharp
public sealed class CombatCommandBuffer
{
    private readonly List<ICombatCommand> commands = new();

    public void Heal(Entity target, float amount)
    {
        commands.Add(new HealCommand(target, amount));
    }

    public void ApplyBuff(Entity target, BuffDataSO buffData)
    {
        commands.Add(new ApplyBuffCommand(target, buffData));
    }

    public void DealDamage(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        Vector2 sourcePosition,
        HitSourceKind sourceKind,
        Vector2? knockbackDirection = null,
        Weapon sourceWeapon = null)
    {
        commands.Add(new DealDamageCommand(
            source,
            target,
            spec,
            hitPoint,
            sourcePosition,
            sourceKind,
            knockbackDirection,
            sourceWeapon));
    }

    public void Execute(CombatService combatService)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            commands[i].Execute(combatService);
        }

        commands.Clear();
    }
}
```

价值：

- 防止监听器在遍历过程中改注册表或重复触发。
- 让反伤、连锁伤害仍然经过 `CombatService`。
- 便于做调试、回放和事件记录。

## 8. 注册表与快照

### 8.1 来源数据

```csharp
public sealed class RuleSource
{
    public string SourceId { get; }
    public IReadOnlyList<IHitRule> Rules { get; }
    public IReadOnlyList<IHitAppliedListener> Listeners { get; }

    public RuleSource(
        string sourceId,
        IEnumerable<IHitRule> rules,
        IEnumerable<IHitAppliedListener> listeners)
    {
        SourceId = sourceId;
        Rules = rules != null ? new List<IHitRule>(rules) : Array.Empty<IHitRule>();
        Listeners = listeners != null ? new List<IHitAppliedListener>(listeners) : Array.Empty<IHitAppliedListener>();
    }
}
```

### 8.2 注册表

```csharp
public sealed class CombatRuleRegistry
{
    private readonly Dictionary<string, RuleSource> sources = new();
    private bool dirty = true;
    private HitRuleSnapshot cachedSnapshot = HitRuleSnapshot.Empty;

    public void AddSource(
        string sourceId,
        IEnumerable<IHitRule> rules,
        IEnumerable<IHitAppliedListener> listeners = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Combat rule sourceId cannot be empty.", nameof(sourceId));
        }

        sources[sourceId] = new RuleSource(sourceId, rules, listeners);
        dirty = true;
    }

    public void RemoveSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (sources.Remove(sourceId))
        {
            dirty = true;
        }
    }

    public HitRuleSnapshot CreateSnapshot()
    {
        if (!dirty)
        {
            return cachedSnapshot;
        }

        cachedSnapshot = HitRuleSnapshot.Build(sources.Values);
        dirty = false;
        return cachedSnapshot;
    }
}
```

### 8.3 快照

```csharp
public sealed class HitRuleSnapshot
{
    public static readonly HitRuleSnapshot Empty = new(
        Array.Empty<IPreDefenseRule>(),
        Array.Empty<IPreRollRule>(),
        Array.Empty<IPostRollRule>(),
        Array.Empty<IPreFinalDamageRule>(),
        Array.Empty<IHitAppliedListener>());

    public IReadOnlyList<IPreDefenseRule> PreDefenseRules { get; }
    public IReadOnlyList<IPreRollRule> PreRollRules { get; }
    public IReadOnlyList<IPostRollRule> PostRollRules { get; }
    public IReadOnlyList<IPreFinalDamageRule> PreFinalDamageRules { get; }
    public IReadOnlyList<IHitAppliedListener> AppliedListeners { get; }

    private HitRuleSnapshot(
        IReadOnlyList<IPreDefenseRule> preDefenseRules,
        IReadOnlyList<IPreRollRule> preRollRules,
        IReadOnlyList<IPostRollRule> postRollRules,
        IReadOnlyList<IPreFinalDamageRule> preFinalDamageRules,
        IReadOnlyList<IHitAppliedListener> appliedListeners)
    {
        PreDefenseRules = preDefenseRules;
        PreRollRules = preRollRules;
        PostRollRules = postRollRules;
        PreFinalDamageRules = preFinalDamageRules;
        AppliedListeners = appliedListeners;
    }

    public static HitRuleSnapshot Build(IEnumerable<RuleSource> sources)
    {
        List<IPreDefenseRule> preDefenseRules = new();
        List<IPreRollRule> preRollRules = new();
        List<IPostRollRule> postRollRules = new();
        List<IPreFinalDamageRule> preFinalDamageRules = new();
        List<IHitAppliedListener> appliedListeners = new();

        foreach (RuleSource source in sources)
        {
            foreach (IHitRule rule in source.Rules)
            {
                if (rule is IPreDefenseRule preDefenseRule)
                {
                    preDefenseRules.Add(preDefenseRule);
                }

                if (rule is IPreRollRule preRollRule)
                {
                    preRollRules.Add(preRollRule);
                }

                if (rule is IPostRollRule postRollRule)
                {
                    postRollRules.Add(postRollRule);
                }

                if (rule is IPreFinalDamageRule preFinalDamageRule)
                {
                    preFinalDamageRules.Add(preFinalDamageRule);
                }
            }

            appliedListeners.AddRange(source.Listeners);
        }

        SortByOrder(preDefenseRules);
        SortByOrder(preRollRules);
        SortByOrder(postRollRules);
        SortByOrder(preFinalDamageRules);
        appliedListeners.Sort((left, right) => left.Order.CompareTo(right.Order));

        return new HitRuleSnapshot(
            preDefenseRules,
            preRollRules,
            postRollRules,
            preFinalDamageRules,
            appliedListeners);
    }

    private static void SortByOrder<T>(List<T> rules) where T : IHitRule
    {
        rules.Sort((left, right) => left.Order.CompareTo(right.Order));
    }
}
```

说明：

- 快照创建时可以使用 `is` 分类，因为它发生在注册表脏数据重建阶段，不发生在每次命中的内层循环。
- 命中热路径只遍历已经分好类并排序的数组或列表。

## 9. HealthComponent 作为注册门面

推荐让 `HealthComponent` 暴露简单入口，方便 Feature 使用。

```csharp
public partial class HealthComponent : EntityComponentBase
{
    private readonly CombatRuleRegistry combatRules = new();

    public void RegisterCombatRuleSource(
        string sourceId,
        IEnumerable<IHitRule> rules,
        IEnumerable<IHitAppliedListener> listeners = null)
    {
        combatRules.AddSource(sourceId, rules, listeners);
    }

    public void RemoveCombatRuleSource(string sourceId)
    {
        combatRules.RemoveSource(sourceId);
    }

    public HitRuleSnapshot CreateHitRuleSnapshot()
    {
        return combatRules.CreateSnapshot();
    }

    public AppliedDamage ApplyResolvedDamage(HitResult result)
    {
        if (!result.CanApplyDamage)
        {
            return AppliedDamage.None;
        }

        float damage = Mathf.Max(0f, result.FinalDamage);
        float actualDamage = Mathf.Min(damage, CurrentHealth);
        if (actualDamage <= 0f)
        {
            return AppliedDamage.None;
        }

        // 这里执行生命扣除、受伤事件和死亡检查。
        // 不重新计算暴击、闪避、减伤。
        return new AppliedDamage(actualDamage);
    }
}
```

约束：

- `HealthComponent` 可以组合 `CombatRuleRegistry`，但不要让它负责收集攻击方规则。
- `HealthComponent` 不应该调用 `HitResolver`。
- `HealthComponent` 不应该执行 `IHitAppliedListener`。

## 10. 规则集合

`HitRuleSet` 是一次命中事务使用的规则集合，由攻击方快照、受击方快照、临时规则合并而成。

```csharp
public sealed class HitRuleSet
{
    private readonly IReadOnlyList<IPreDefenseRule> preDefenseRules;
    private readonly IReadOnlyList<IPreRollRule> preRollRules;
    private readonly IReadOnlyList<IPostRollRule> postRollRules;
    private readonly IReadOnlyList<IPreFinalDamageRule> preFinalDamageRules;
    private readonly IReadOnlyList<IHitAppliedListener> appliedListeners;

    public void ExecutePreDefense(HitContext context)
    {
        Execute(preDefenseRules, context);
    }

    public void ExecutePreRoll(HitContext context)
    {
        Execute(preRollRules, context);
    }

    public void ExecutePostRoll(HitContext context)
    {
        Execute(postRollRules, context);
    }

    public void ExecutePreFinalDamage(HitContext context)
    {
        Execute(preFinalDamageRules, context);
    }

    public void ExecuteAppliedListeners(HitAppliedContext context)
    {
        for (int i = 0; i < appliedListeners.Count; i++)
        {
            appliedListeners[i].OnHitApplied(context);
        }
    }

    private static void Execute<T>(IReadOnlyList<T> rules, HitContext context) where T : IHitRule
    {
        for (int i = 0; i < rules.Count; i++)
        {
            if (context.IsCancelled)
            {
                return;
            }

            switch (rules[i])
            {
                case IPreDefenseRule preDefenseRule:
                    preDefenseRule.Execute(context);
                    break;
                case IPreRollRule preRollRule:
                    preRollRule.Execute(context);
                    break;
                case IPostRollRule postRollRule:
                    postRollRule.Execute(context);
                    break;
                case IPreFinalDamageRule preFinalDamageRule:
                    preFinalDamageRule.Execute(context);
                    break;
            }
        }
    }
}
```

实际落地时可以避免 `switch`，为每个阶段写独立循环：

```csharp
for (int i = 0; i < preDefenseRules.Count; i++)
{
    preDefenseRules[i].Execute(context);
}
```

这样热路径更直接。

## 11. 规则收集

```csharp
public static class HitRuleCollector
{
    public static HitRuleSet Collect(HitRequest request)
    {
        HitRuleSnapshot sourceSnapshot = CreateSnapshot(request.Source);
        HitRuleSnapshot targetSnapshot = CreateSnapshot(request.Target);

        return HitRuleSetBuilder.Create()
            .AddOutgoing(sourceSnapshot)
            .AddIncoming(targetSnapshot)
            .Build();
    }

    private static HitRuleSnapshot CreateSnapshot(Entity entity)
    {
        if (entity == null || !entity.TryGetComponent(out HealthComponent health))
        {
            return HitRuleSnapshot.Empty;
        }

        return health.CreateHitRuleSnapshot();
    }
}
```

合并规则时必须遵守：

- 攻击方只取 `Outgoing` 和 `Both`。
- 受击方只取 `Incoming` 和 `Both`。
- 合并后按 `Order` 排序。
- 同一 sourceId 的规则在同一实体上可被替换或移除。

## 12. 结算流程

```csharp
public sealed class HitResolver
{
    private readonly IRandomSource random;

    public HitResolver(IRandomSource random)
    {
        this.random = random;
    }

    public HitResult Resolve(HitRequest request, HitRuleSet rules)
    {
        HitTrace trace = HitTrace.Create();
        HitContext context = new(request, trace);

        if (!Validate(context))
        {
            return BuildResult(context);
        }

        rules.ExecutePreDefense(context);
        ResolveDefense(context);

        rules.ExecutePreRoll(context);
        Roll(context);

        rules.ExecutePostRoll(context);
        rules.ExecutePreFinalDamage(context);

        FinalizeDamage(context);
        return BuildResult(context);
    }

    private bool Validate(HitContext context)
    {
        if (context.Request.Source == null || context.Request.Target == null)
        {
            context.IsCancelled = true;
            context.Trace.Add("Validate", "source or target is null");
            return false;
        }

        if (!context.Request.Target.TryGetComponent(out HealthComponent targetHealth) ||
            targetHealth.CurrentHealth <= 0f)
        {
            context.IsCancelled = true;
            context.Trace.Add("Validate", "target has no valid health");
            return false;
        }

        return true;
    }

    private void ResolveDefense(HitContext context)
    {
        if (context.Request.SourceKind == HitSourceKind.Direct)
        {
            // Direct 是否无视防御应由具体规则决定。
            // 不建议把 SourceKind.Direct 永久绑定为真实伤害，避免语义膨胀。
        }

        // 从目标属性快照或属性读取接口取护甲、减伤、闪避。
        // 这里可以依赖只读属性查询接口，而不是让规则直接拿组件。
    }

    private void Roll(HitContext context)
    {
        context.IsCritical = random.Value <= context.CritChance;
        context.IsDodged = random.Value <= context.DodgeChance;
    }

    private void FinalizeDamage(HitContext context)
    {
        if (context.IsCancelled || context.IsDodged || context.IsBlocked)
        {
            context.Damage = 0f;
            return;
        }

        float damage = Mathf.Max(0f, context.Damage);
        if (context.IsCritical)
        {
            damage *= context.CritMultiplier;
        }

        context.Damage = damage * Mathf.Max(0f, 1f - context.DamageReduction);
    }

    private static HitResult BuildResult(HitContext context)
    {
        return new HitResult(
            context.Request.Source,
            context.Request.Target,
            Mathf.Max(0f, context.Damage),
            0f,
            context.KnockbackStrength,
            context.Request.HitPoint,
            context.Request.HasKnockbackDirection,
            context.Request.KnockbackDirection,
            context.IsCritical,
            context.IsDodged,
            context.IsBlocked,
            context.IsCancelled,
            context.Request.SourceKind,
            context.Request.SourcePosition,
            context.Request.SourceWeapon);
    }
}
```

## 13. 应用流程

```csharp
public sealed class HitApplier
{
    public HitResult Apply(HitRequest request, HitResult result, HitRuleSet rules, CombatService combatService)
    {
        if (result.Target == null || !result.Target.TryGetComponent(out HealthComponent targetHealth))
        {
            return result;
        }

        if (result.IsCancelled)
        {
            return result;
        }

        if (result.IsDodged)
        {
            // 可以通知闪避表现，但不扣血。
            return result;
        }

        AppliedDamage appliedDamage = targetHealth.ApplyResolvedDamage(result);
        HitResult appliedResult = result.WithActualDamage(appliedDamage.Amount);

        HitAppliedContext appliedContext = new(
            request,
            appliedResult,
            new CombatCommandBuffer());

        rules.ExecuteAppliedListeners(appliedContext);
        appliedContext.Commands.Execute(combatService);

        return appliedResult;
    }
}
```

## 14. CombatService

```csharp
public sealed class CombatService
{
    private readonly HitResolver resolver;
    private readonly HitApplier applier;

    public CombatService(IRandomSource random)
    {
        resolver = new HitResolver(random);
        applier = new HitApplier();
    }

    public HitResult Resolve(HitRequest request)
    {
        HitRuleSet rules = HitRuleCollector.Collect(request);
        return resolver.Resolve(request, rules);
    }

    public HitResult ResolveAndApply(HitRequest request)
    {
        HitRuleSet rules = HitRuleCollector.Collect(request);
        HitResult result = resolver.Resolve(request, rules);
        return applier.Apply(request, result, rules, this);
    }
}
```

要求：

- 所有伤害、反伤、连锁伤害都调用 `CombatService.ResolveAndApply`。
- 不允许绕过 `CombatService` 直接调用 `HealthComponent.ApplyResolvedDamage`。

## 15. Feature 注册模式

Feature 的使用体验应保持简单。

```csharp
public abstract class FeatureEffectBase
{
    public FeatureRuntimeContext Context { get; private set; }
    public string SourceId { get; private set; }

    public void Install(FeatureRuntimeContext context, string sourceId)
    {
        Context = context;
        SourceId = sourceId;
        OnInstall();
    }

    public void Uninstall()
    {
        OnUninstall();
        Context = null;
        SourceId = null;
    }

    protected virtual void OnInstall() {}
    protected virtual void OnUninstall() {}
}
```

```csharp
public sealed class FeatureRuntimeContext
{
    public Entity Owner { get; }
    public HealthComponent Health { get; }

    public FeatureRuntimeContext(Entity owner)
    {
        Owner = owner;
        Health = owner != null ? owner.GetComponent<HealthComponent>() : null;
    }
}
```

Feature 注册规则：

```csharp
public sealed class ExecutionFeature : FeatureEffectBase
{
    protected override void OnInstall()
    {
        Context.Health.RegisterCombatRuleSource(
            SourceId,
            new IHitRule[]
            {
                new ArmorPenetrationRule(30f),
                new ForceCriticalRule()
            });
    }

    protected override void OnUninstall()
    {
        Context.Health.RemoveCombatRuleSource(SourceId);
    }
}
```

注意：

- `SourceId` 必须稳定。
- 同一个 Feature 重复安装前应先移除旧 source。
- Buff 每层如果要独立计时，可以为每层生成独立 sourceId。
- 永久装备可以使用装备实例 runtimeId。

## 16. 示例一：破甲处刑

需求：

- 玩家装备一个饰品。
- 玩家造成的命中获得 30% 护甲穿透。
- 玩家造成的命中强制暴击。

### 16.1 规则实现

```csharp
public sealed class ArmorPenetrationRule : IPreDefenseRule
{
    private readonly float percent;

    public int Order => 0;
    public HitRuleScope Scope => HitRuleScope.Outgoing;
    public string DebugName => "Armor Penetration";

    public ArmorPenetrationRule(float percent)
    {
        this.percent = Mathf.Max(0f, percent);
    }

    public void Execute(HitContext context)
    {
        context.ArmorPenetrationPercent += percent;
        context.Trace.Add(DebugName, $"+{percent:0.#}% armor penetration");
    }
}
```

```csharp
public sealed class ForceCriticalRule : IPostRollRule
{
    public int Order => 0;
    public HitRuleScope Scope => HitRuleScope.Outgoing;
    public string DebugName => "Force Critical";

    public void Execute(HitContext context)
    {
        context.IsCritical = true;
        context.Trace.Add(DebugName, "critical forced");
    }
}
```

### 16.2 Feature 实现

```csharp
[Serializable]
public sealed class ExecutionFeature : FeatureEffectBase
{
    [SerializeField] private float armorPenetrationPercent = 30f;

    protected override void OnInstall()
    {
        if (Context.Health == null)
        {
            return;
        }

        Context.Health.RegisterCombatRuleSource(
            SourceId,
            new IHitRule[]
            {
                new ArmorPenetrationRule(armorPenetrationPercent),
                new ForceCriticalRule()
            });
    }

    protected override void OnUninstall()
    {
        Context.Health?.RemoveCombatRuleSource(SourceId);
    }
}
```

### 16.3 运行链路

```text
1. 玩家装备饰品。
2. 饰品 Feature 安装。
3. Feature 向玩家 HealthComponent 注册破甲规则和强制暴击规则。
4. 武器命中敌人，创建 HitRequest。
5. CombatService 收集玩家 Outgoing 规则和敌人 Incoming 规则。
6. HitResolver 执行：
   - Validate：确认双方和目标生命有效。
   - PreDefense：破甲 +30%。
   - ResolveDefense：按穿透后的护甲计算减伤。
   - PreRoll：如果有暴击率规则，在这里修改。
   - Roll：正常做暴击和闪避随机。
   - PostRoll：强制暴击。
   - PreFinalDamage：如果有最终增伤，在这里执行。
   - FinalizeDamage：计算最终伤害。
7. HitApplier 调用敌人 HealthComponent.ApplyResolvedDamage。
8. 敌人扣血并触发受击、死亡等结果事件。
```

### 16.4 Trace 示例

```text
主命中 Player -> Enemy
- Input: BaseDamage=100, CritChance=8%, CritMultiplier=180%
- Armor Penetration: +30% armor penetration
- Defense: armor 50 -> 35 after penetration, reduction 25.9%
- Roll: critical=false, dodged=false
- Force Critical: critical forced
- Finalize: 100 * 1.8 * (1 - 0.259) = 133.38
```

## 17. 示例二：吸血、燃烧与反伤

需求：

- 玩家有饰品：命中后按实际伤害 5% 吸血。
- 玩家武器有词条：命中后给目标施加燃烧 Buff。
- 敌人有状态：受到伤害后反弹实际伤害 15%。
- 反伤不能再次触发反伤。

### 17.1 吸血监听器

```csharp
public sealed class LifeStealOnHitListener : IHitAppliedListener
{
    private readonly float ratio;

    public int Order => 0;
    public HitRuleScope Scope => HitRuleScope.Outgoing;
    public string DebugName => "Life Steal";

    public LifeStealOnHitListener(float percent)
    {
        ratio = Mathf.Max(0f, percent) * 0.01f;
    }

    public void OnHitApplied(HitAppliedContext context)
    {
        if (context.Result.ActualDamage <= 0f)
        {
            return;
        }

        float healAmount = context.Result.ActualDamage * ratio;
        context.Commands.Heal(context.Request.Source, healAmount);
        context.Trace.Add(DebugName, $"heal source {healAmount:0.#}");
    }
}
```

### 17.2 燃烧监听器

```csharp
public sealed class ApplyBurnOnHitListener : IHitAppliedListener
{
    private readonly BuffDataSO burnBuff;

    public int Order => 10;
    public HitRuleScope Scope => HitRuleScope.Outgoing;
    public string DebugName => "Apply Burn";

    public ApplyBurnOnHitListener(BuffDataSO burnBuff)
    {
        this.burnBuff = burnBuff;
    }

    public void OnHitApplied(HitAppliedContext context)
    {
        if (burnBuff == null || context.Result.ActualDamage <= 0f)
        {
            return;
        }

        context.Commands.ApplyBuff(context.Request.Target, burnBuff);
        context.Trace.Add(DebugName, $"apply burn to {context.Request.Target.name}");
    }
}
```

### 17.3 反伤监听器

```csharp
public sealed class ThornsOnHitListener : IHitAppliedListener
{
    private readonly float ratio;

    public int Order => 20;
    public HitRuleScope Scope => HitRuleScope.Incoming;
    public string DebugName => "Thorns";

    public ThornsOnHitListener(float percent)
    {
        ratio = Mathf.Max(0f, percent) * 0.01f;
    }

    public void OnHitApplied(HitAppliedContext context)
    {
        if (context.Result.ActualDamage <= 0f)
        {
            return;
        }

        if (context.Request.SourceKind == HitSourceKind.Reflect)
        {
            context.Trace.Add(DebugName, "skip reflected hit");
            return;
        }

        float reflectedDamage = context.Result.ActualDamage * ratio;
        HitSpec spec = new HitSpec(reflectedDamage, 0f, 1f, 0f);
        context.Commands.DealDamage(
            context.Request.Target,
            context.Request.Source,
            spec,
            context.Request.Target.transform.position,
            context.Request.Target.Center,
            HitSourceKind.Reflect);

        context.Trace.Add(DebugName, $"reflect {reflectedDamage:0.#} damage");
    }
}
```

### 17.4 Feature 注册

玩家吸血 Feature：

```csharp
[Serializable]
public sealed class LifeStealFeature : FeatureEffectBase
{
    [SerializeField] private float lifeStealPercent = 5f;

    protected override void OnInstall()
    {
        Context.Health.RegisterCombatRuleSource(
            SourceId,
            null,
            new IHitAppliedListener[]
            {
                new LifeStealOnHitListener(lifeStealPercent)
            });
    }

    protected override void OnUninstall()
    {
        Context.Health.RemoveCombatRuleSource(SourceId);
    }
}
```

武器燃烧 Feature：

```csharp
[Serializable]
public sealed class BurnOnHitFeature : FeatureEffectBase
{
    [SerializeField] private BuffDataSO burnBuff;

    protected override void OnInstall()
    {
        Context.Health.RegisterCombatRuleSource(
            SourceId,
            null,
            new IHitAppliedListener[]
            {
                new ApplyBurnOnHitListener(burnBuff)
            });
    }

    protected override void OnUninstall()
    {
        Context.Health.RemoveCombatRuleSource(SourceId);
    }
}
```

敌人反伤 Feature：

```csharp
[Serializable]
public sealed class ThornsFeature : FeatureEffectBase
{
    [SerializeField] private float reflectPercent = 15f;

    protected override void OnInstall()
    {
        Context.Health.RegisterCombatRuleSource(
            SourceId,
            null,
            new IHitAppliedListener[]
            {
                new ThornsOnHitListener(reflectPercent)
            });
    }

    protected override void OnUninstall()
    {
        Context.Health.RemoveCombatRuleSource(SourceId);
    }
}
```

### 17.5 完整运行链路

```text
1. 玩家安装 LifeStealFeature。
   - 玩家 HealthComponent 注册 Outgoing 监听器 LifeStealOnHitListener。

2. 玩家武器安装 BurnOnHitFeature。
   - 玩家 HealthComponent 注册 Outgoing 监听器 ApplyBurnOnHitListener。

3. 敌人安装 ThornsFeature。
   - 敌人 HealthComponent 注册 Incoming 监听器 ThornsOnHitListener。

4. 玩家投射物命中敌人。
   - Projectile 创建 HitRequest：
     Source=Player
     Target=Enemy
     SourceKind=Projectile
     SourceWeapon=当前武器
     SourcePosition=投射物生成位置或发射点位置快照

5. CombatService.ResolveAndApply(request)。

6. HitRuleCollector 收集规则：
   - 玩家 Outgoing：吸血、燃烧。
   - 敌人 Incoming：反伤。

7. HitResolver 计算最终伤害，例如 FinalDamage=120。

8. HitApplier 调用敌人 HealthComponent.ApplyResolvedDamage。
   - 敌人实际扣血 120。
   - AppliedResult.ActualDamage=120。

9. HitApplier 构造 HitAppliedContext 并执行监听器：
   - LifeStealOnHitListener 添加 Heal(Player, 6)。
   - ApplyBurnOnHitListener 添加 ApplyBuff(Enemy, BurnBuff)。
   - ThornsOnHitListener 添加 DealDamage(Enemy, Player, 18, HitSourceKind.Reflect)。

10. CombatCommandBuffer.Execute。
   - 玩家回血 6。
   - 敌人获得燃烧 Buff。
   - 通过 CombatService 发起一条新的反伤 HitRequest。

11. 反伤 HitRequest 使用 HitSourceKind.Reflect。
   - 如果玩家或敌人也有反伤监听器，会因为 Reflect 来源类型跳过再次反伤。
```

### 17.6 Trace 示例

```text
主命中 Player -> Enemy
- Input: BaseDamage=120
- Defense: reduction 0%
- Roll: critical=false, dodged=false
- Finalize: 120
- Apply: Enemy actual damage 120
- Life Steal: heal Player 6
- Apply Burn: apply burn to Enemy
- Thorns: reflect 18 damage

反伤命中 Enemy -> Player
- SourceKind: Reflect
- Input: BaseDamage=18
- Defense: player reduction 10%
- Finalize: 16.2
- Apply: Player actual damage 16.2
- Thorns: skip reflected hit
```

## 18. 攻击方与受击方协调机制

攻击方和受击方通过以下机制保持协调：

### 18.1 统一规则快照

`CombatService` 在结算开始时创建规则快照。

要求：

- 本次命中只使用开始时快照。
- 结算中途 Buff 变化不影响当前命中。
- 监听器追加的反伤或连锁伤害进入新的命中事务。

### 18.2 单一应用点

只有 `HitApplier` 能把结果应用到目标 `HealthComponent`。

禁止：

- 武器直接调用 `HealthComponent.Heal` 以外的扣血接口。
- Feature 直接扣目标血。
- 监听器直接调用目标扣血。

### 18.3 后置副作用

所有副作用都在结果应用后执行。

好处：

- 吸血基于实际伤害。
- 燃烧只在命中有效时附加。
- 反伤知道本次实际扣了多少血。
- 表现层消费的是最终事实。

## 19. 调试与 Trace

推荐实现：

```csharp
public sealed class HitTrace
{
    private readonly List<string> lines = new();

    private HitTrace()
    {
    }

    public static HitTrace Create()
    {
        return new HitTrace();
    }

    public void Add(string source, string message)
    {
        lines.Add($"[{source}] {message}");
    }

    public IReadOnlyList<string> Lines => lines;
}
```

Trace 输出建议只在 Editor 或 Development Build 开启。

必须记录：

- 输入基础伤害。
- 规则执行顺序。
- 护甲、减伤、暴击、闪避关键值。
- 最终伤害。
- 实际伤害。
- 命中后命令。

## 20. 性能与生命周期

### 20.1 性能

要求：

- 注册表脏时才重建快照。
- 命中时不做全局 `Find`。
- 命中时不扫描全部 Feature。
- 阶段列表提前分类并排序。
- 高频命中场景可复用临时列表或对象池。

### 20.2 生命周期

要求：

- Feature 安装时注册 sourceId。
- Feature 卸载时移除 sourceId。
- Buff 每层如果独立存在，使用独立 sourceId。
- 实体 Disable 或死亡时清理注册表。
- 规则对象不要持有已经销毁的场景对象引用，必要时通过 `Entity` 弱校验。

## 21. 命名规范

推荐命名：

- `CombatService`：统一战斗结算入口。
- `HitResolver`：纯计算。
- `HitApplier`：结果应用。
- `HitRule`：阶段规则。
- `HitAppliedListener`：应用后监听器。
- `CombatRuleRegistry`：实体规则注册表。
- `HitRuleSnapshot`：注册表只读快照。
- `HitRuleSet`：一次命中的合并规则集合。
- `HitTrace`：调试轨迹。

避免命名：

- `HitModifier`：语义过泛，容易把所有东西都塞进去。
- `DamageManager`：职责不清。
- `HealthSystem`：如果只在组件中使用，会和 `HealthComponent` 混淆。

## 22. 落地顺序建议

推荐分阶段推进：

1. 新增 `CombatService`、`HitResolver`、`HitApplier` 的目标结构，但保持旧入口适配。
2. 新增 `CombatRuleRegistry` 并挂到 `HealthComponent`。
3. 将现有 `IHitModifier` 迁移为阶段化规则。
4. 将吸血、命中后 Buff、反伤等副作用迁移到 `IHitAppliedListener`。
5. 给关键规则补充 EditMode 测试。
6. 移除旧的 priority modifier 链和旧文档中的过时说明。

不建议：

- 一次性重写全部战斗系统。
- 引入大型 Ability Framework。
- 让 `HealthComponent` 变成战斗总控。
- 让 Feature 直接调用 `HitResolver` 或 `HitApplier`。

## 23. 验证要求

实现后必须验证：

- 普通攻击能正确扣血。
- 暴击、闪避、护甲、减伤顺序正确。
- 破甲发生在防御结算前。
- 强制暴击发生在随机判定后。
- 吸血基于实际伤害。
- 燃烧只在有效命中后附加。
- 反伤通过新命中事务执行，且不会无限反弹。
- Feature 卸载后规则不再生效。
- Buff 到期后 sourceId 被正确移除。
- 敌人死亡后不再接受新的有效伤害。
- Trace 能完整解释一次复杂命中。
