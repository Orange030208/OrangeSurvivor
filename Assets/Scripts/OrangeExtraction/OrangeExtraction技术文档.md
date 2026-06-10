# Orange.Extraction 技术文档

## 1. 模块定位

`Orange.Extraction` 是一个纯 C# 加权抽取框架，用于承载武器、奖励、商店候选、掉落候选等“从一组业务数据中按权重选出结果”的通用流程。

当前版本只负责抽取算法与扩展点，不绑定 Unity `ScriptableObject`、Prefab、场景对象或具体玩法规则。业务系统应自行把 SO、表格、运行时数据转换成抽取条目，再调用抽取池。

第一版已经可以覆盖以下需求：

- 业务类继承 `WeightedExtractionPool<TItem, TContext>`。
- 使用一组业务数据初始化统一基础权重条目。
- 每个条目拥有 `EntryId`、`Item`、`BaseWeight`、可选可抽条件、可选权重修改器。
- 支持 `Evaluate` 权重预览、`TryDrawOne` 单抽、`DrawManyUnique` 不放回多抽。
- 支持注入 `IExtractionRandom`，用于固定 seed、测试复现和抽取问题排查。

## 2. 核心类型

### WeightedExtractionPool<TItem, TContext>

推荐业务继承入口。

`TItem` 是被抽取的数据类型，例如 `WeaponDataSO`、`RewardCardSO`、敌人包、掉落配置等。

`TContext` 是业务上下文，例如玩家等级、已有武器、当前波次、幸运值、商店状态等。所有动态权重逻辑都应优先从上下文读取信息，而不是让框架层依赖单例、场景对象或具体业务系统。

### WeightedExtractionPool<TItem>

无上下文便捷版本，内部使用 `EmptyExtractionContext`。适合简单抽取，例如只按固定权重从列表里选一个元素。

### WeightedExtractionEntry<TItem, TContext>

表示一个候选条目：

- `EntryId`：条目唯一语义标识，建议使用业务稳定 id。
- `Item`：实际被抽取的数据。
- `BaseWeight`：基础权重，必须是有限且大于等于 0 的数值。
- `Eligibility`：可选可抽条件；返回 `false` 时该条目不会参与本次抽取。
- `WeightModifier`：可选权重修改器；用于根据上下文计算最终有效权重。

### IExtractionWeightModifier<TItem, TContext>

用于定义业务权重变化规则。修改器接收条目、基础权重和上下文，返回最终权重。

注意：

- 返回负数会被框架钳制为 0。
- 返回 `NaN` 或无穷大会抛出异常。
- 当前每个条目只挂一个修改器；多个规则需要业务侧自行封装为组合修改器。

### IExtractionRandom

提供 `[0, 1)` 区间内的随机值。框架默认使用 `SystemExtractionRandom`，业务也可以传入固定序列或固定 seed 随机源，便于测试和复现。

## 3. 抽取流程

一次抽取的核心流程如下：

1. 遍历所有候选条目。
2. 执行 `Eligibility`，不可抽条目标记为 `Ineligible`。
3. 对可抽条目执行 `WeightModifier`，得到最终权重。
4. 最终权重小于等于 0 的条目标记为 `ZeroWeight`。
5. 最终权重大于 0 的条目标记为 `Drawable`，加入总权重。
6. 根据随机值和总权重选中一个 `Drawable` 条目。

`Evaluate(context)` 只执行第 1 到第 5 步，不消耗随机数，适合 UI 预览、调试输出和测试断言。

`TryDrawOne(context, out result)` 执行完整单抽。没有可抽条目或总权重为 0 时返回 `false`。

`DrawManyUnique(context, count)` 执行不放回多抽。每抽中一个条目后，会从后续抽取中排除该条目，并重新计算剩余候选权重。

## 4. 推荐业务接入方式

下面示例展示一个武器抽取池的典型写法。示例代码用于说明结构，具体字段应以业务项目中的真实类型为准。

```csharp
using System.Collections.Generic;
using Orange.Extraction;

public sealed class WeaponDrawContext
{
    public int PlayerLevel { get; }
    public float Luck { get; }
    public IReadOnlyCollection<string> OwnedWeaponIds { get; }

    public WeaponDrawContext(int playerLevel, float luck, IReadOnlyCollection<string> ownedWeaponIds)
    {
        PlayerLevel = playerLevel;
        Luck = luck;
        OwnedWeaponIds = ownedWeaponIds;
    }
}

public sealed class WeaponExtractionPool : WeightedExtractionPool<WeaponDataSO, WeaponDrawContext>
{
    private const float DEFAULT_WEAPON_WEIGHT = 1f;

    public WeaponExtractionPool(IEnumerable<WeaponDataSO> weapons, IExtractionRandom random = null)
        : base(random)
    {
        AddUniformEntries(weapons, weapon => weapon.WeaponId, DEFAULT_WEAPON_WEIGHT);
    }
}
```

如果需要按业务动态调整权重，可以为条目提供修改器：

```csharp
using Orange.Extraction;

public sealed class LuckWeaponWeightModifier : IExtractionWeightModifier<WeaponDataSO, WeaponDrawContext>
{
    public float ModifyWeight(
        WeightedExtractionEntry<WeaponDataSO, WeaponDrawContext> entry,
        float baseWeight,
        WeaponDrawContext context)
    {
        return baseWeight * (1f + context.Luck * 0.01f);
    }
}
```

如果需要过滤已有武器，可以使用 `Eligibility`：

```csharp
pool.AddEntry(
    weapon.WeaponId,
    weapon,
    baseWeight: 1f,
    eligibility: (entry, context) => !context.OwnedWeaponIds.Contains(entry.EntryId),
    weightModifier: new LuckWeaponWeightModifier());
```

## 5. 调试与结果解释

`ExtractionResult<TItem>` 会保留本次选中结果与权重快照：

- `EntryId`：选中条目 id。
- `Item`：选中数据。
- `BaseWeight`：基础权重。
- `FinalWeight`：本次最终权重。
- `TotalWeight`：本次参与抽取的总权重。
- `RollValue`：映射到总权重区间后的随机命中值。
- `Evaluation`：本次候选快照。

`ExtractionEvaluation<TItem>` 中的每个 `ExtractionCandidate<TItem>` 会记录：

- `Ineligible`：业务条件判定为不可抽。
- `ZeroWeight`：可抽但最终权重为 0。
- `Drawable`：参与本次抽取。

建议在接入复杂业务规则时，先调用 `Evaluate(context)` 打印或断言候选状态，再执行实际抽取。

## 6. 当前边界

当前版本刻意不包含以下能力：

- 不提供 ScriptableObject 配置资产。
- 不内置保底、历史记录、互斥组、黑白名单或稀有度衰减。
- 不内置放回多抽；当前 `DrawManyUnique` 是不放回多抽。
- 不自动检查重复 `EntryId`。
- 不记录过滤原因文本或权重修改原因文本。
- 不提供抽取历史持久化。
- 不提供可视化调试窗口。

这些能力可以在业务层通过 `TContext`、`Eligibility`、`IExtractionWeightModifier` 先行组合实现。只有当多个业务系统反复需要同一种能力时，再考虑下沉到框架层。

## 7. 后续可选增强

低成本增强：

- 增加重复 `EntryId` 检查。
- 在候选快照中增加概率百分比字段。
- 支持按 `EntryId` 临时排除候选。
- 文档化或内置 `CompositeWeightModifier`。

中等增强：

- 增加 `DrawManyWithReplacement` 放回多抽。
- 增加 `TryGetEntry`、`RemoveEntry`、`ContainsEntryId` 等管理 API。
- 为候选快照增加过滤原因和权重修改原因。

较大增强：

- 增加 ScriptableObject 适配层。
- 增加抽取历史、保底或互斥规则系统。
- 增加 Editor 调试窗口，用于查看候选权重、最终概率和命中结果。

## 8. 使用建议

- 框架层只放通用抽取机制，不写死武器、稀有度、商店、波次等业务概念。
- 业务变化优先放进 `TContext`、`Eligibility` 和 `IExtractionWeightModifier`。
- 需要复现问题时，优先注入固定 seed 或固定序列的 `IExtractionRandom`。
- 多个权重规则叠加时，先在业务侧封装组合修改器，等规则稳定后再考虑沉淀通用实现。
- 接入 UI 概率预览时优先使用 `Evaluate(context)`，不要为了预览调用真实抽取。
