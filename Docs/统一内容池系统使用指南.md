# 统一内容池系统使用文档

## 1. 系统目标

统一内容池系统用于解决“从一组候选内容中，按事实、条件、权重、历史状态和全局 Modifier 抽出结果”的问题。

当前已接入的用途包括：

- 升级卡三选一
- 宝箱奖励三选一
- 商店商品刷新
- 掉落结果
- 波次刷怪候选
- 随机武器奖励

通用池只负责候选筛选、权重计算和抽取，不负责执行结果。购买、装备、应用升级、生成敌人、发放金币等行为仍由各业务系统自己处理。

核心代码位置：

- `Assets/Scripts/Content/Pools/ContentPoolSO.cs`
- `Assets/Scripts/Content/Pools/ContentPoolEntry.cs`
- `Assets/Scripts/Content/Runtime/ContentPoolRollService.cs`
- `Assets/Scripts/Content/Facts/ContentFactCollector.cs`
- `Assets/Scripts/Content/Runtime/ContentCondition.cs`
- `Assets/Scripts/Content/Runtime/ContentWeightRule.cs`
- `Assets/Scripts/Content/Runtime/ContentPoolModifierEffect.cs`
- `Assets/Scripts/Content/GameContentCatalogSO.cs`

核心资产位置：

- `Assets/ScriptableObjects/Content/Game Content Catalog.asset`
- `Assets/ScriptableObjects/Content/Pools/`
- `Assets/ScriptableObjects/Content/Facts/`

## 2. 基本概念

### 2.1 ContentPoolSO

`ContentPoolSO` 是一个内容池资产，表示某一类抽取用途。

关键字段：

- `Purpose`：池用途，例如 `UpgradeCard`、`Shop`、`Drop`、`WaveSpawn`。
- `DefaultRollCount`：默认抽取数量，例如升级卡和宝箱通常是 3，商店通常是 4。
- `AllowDuplicateResults`：同一次抽取中是否允许重复结果。
- `Entries`：候选条目列表。

一个池只做一类用途。不要把商店、宝箱、掉落混到同一个池里。

### 2.2 ContentPoolEntry

`ContentPoolEntry` 是池中的单个候选。

关键字段：

- `EntryId`：条目唯一 ID。建议稳定、可读，不要频繁改名。
- `Content`：候选内容资产，类型是 `UnityEngine.Object`。
- `BaseWeight`：基础权重。
- `Tags`：内容池标签，用于条件或 Modifier 匹配。
- `MaxRollCount`：本局最多被抽出多少次。
- `MaxPickCount`：本局最多被选择多少次。
- `MutuallyExclusiveEntryIds`：同一次抽取中互斥的其他条目 ID。
- `MinLevel` / `MaxLevel`：业务元数据，常用于武器等级范围。
- `QualityValue`：业务元数据，常用于品质或稀有度。
- `DomainFlags`：业务元数据，常用于枚举 bit mask，例如刷怪标签。
- `PriceMultiplier`：业务元数据，常用于商店价格倍率。
- `Conditions`：进入候选池前必须满足的条件。
- `WeightRules`：进入候选池后用于调整权重的规则。

`Content` 是通用引用，业务系统会在抽取时校验类型。例如：

- 升级卡池只接受 `UpgradeCardSO`。
- 宝箱池只接受 `AccessoryDataSO`。
- 商店池可接受 `WeaponDataSO` 和 `AccessoryDataSO`。
- 掉落池只接受 `CollectionSO`。
- 刷怪池接受 `EnemySO` 或 `WaveSpawnPackSO`。
- 武器奖励池只接受 `WeaponDataSO`。

### 2.3 FactDefinitionSO 与事实表

事实是抽取时的上下文，例如当前波次、幸运值、商店刷新次数、已拥有武器数量、已选升级卡标签数量。

事实资产位置：

`Assets/ScriptableObjects/Content/Facts/`

常用内建事实：

- `Current Wave.asset`：当前波次，`Int`。
- `Luck.asset`：幸运值，`Float`。
- `Shop Refresh Count.asset`：商店刷新次数，`Int`。
- `Shop Reroll Count.asset`：商店重掷次数，`Int`。
- `Character.asset`：当前角色，`UnityObject`。
- `Owned Weapon Count.asset`：已拥有武器数量，`Int`。
- `Wave Id.asset`：当前波次 ID，`String`。
- `Wave Track Id.asset`：当前刷怪轨道 ID，`String`。
- `Wave Progress Percent.asset`：当前波次进度百分比，`Float`。
- `Upgrade Card Tag Pick Count *.asset`：指定升级卡标签已选择次数。
- `Owned Weapon Tag Count *.asset`：指定武器标签已拥有数量。

事实由 `ContentFactCollector` 从 `ContentFactSource` 中收集。调用 `ContentPoolRollService.Roll(pool, factSource, state)` 时，系统会自动收集池条件、权重规则和 Modifier 需要的事实。

## 3. 一次抽取如何执行

抽取流程固定如下：

1. 收集池条目、权重规则和 Modifier 需要的事实。
2. 根据 `ContentPoolEntry.Conditions` 过滤不可用条目。
3. 根据 `BaseWeight` 和 `WeightRules` 计算条目权重。
4. 执行已注册的 `IContentPoolModifier`，可修改候选权重或元数据。
5. 按最终权重随机抽取。
6. 根据 `AllowDuplicateResults`、互斥关系、运行时次数限制继续抽取后续结果。
7. 把结果记录到 `ContentPoolRuntimeState` 的 roll 统计。
8. 业务系统读取 `ContentRollResult` 并执行自己的逻辑。

结果对象 `ContentRollItem` 会包含：

- `Content`
- `Entry`
- `EntryId`
- `FinalWeight`
- `MinLevel`
- `MaxLevel`
- `QualityValue`
- `DomainFlags`
- `PriceMultiplier`

业务系统不要重新计算权重，只使用结果里的内容和元数据。

## 4. 池资产如何配置

### 4.1 创建或更新基础内容池

推荐使用编辑器菜单：

`Survivors/Content/Migrate Runtime Content Catalog`

这个工具会：

- 创建或更新内建事实资产。
- 创建或更新升级卡池、宝箱池、商店池、掉落池、武器奖励池。
- 校验刷怪池是否存在。
- 创建或更新 `Game Content Catalog.asset`。
- 在主场景中绑定 `GameContentBootstrap`。

注意：`Wave Spawn Pool.asset` 需要直接维护，因为波次敌人候选已经完全迁移到内容池，工具不会再从旧波次字段回填敌人候选。

### 4.2 手动创建 ContentPool

在 Project 面板中：

`Create/Survivors/Content Pool`

创建后配置：

1. 设置 `Purpose`。
2. 设置 `DefaultRollCount`。
3. 设置 `AllowDuplicateResults`。
4. 给 `Entries` 添加候选。
5. 每个 Entry 绑定 `Content` 和 `BaseWeight`。
6. 按需要配置条件、权重规则和业务元数据。

### 4.3 配置权重

`BaseWeight` 是相对权重，不是百分比。

例如 3 个候选权重为：

- A：10
- B：20
- C：70

如果没有条件、规则和 Modifier 影响，则概率约为：

- A：10%
- B：20%
- C：70%

如果候选被条件过滤，剩余候选会重新归一化。比如 C 被过滤后，A 和 B 的概率会变成约 33.3% 和 66.7%。

### 4.4 配置条件 Conditions

条件只决定“是否进入候选池”，不修改权重。

当前支持：

- `AlwaysContentCondition`：永远满足。
- `FactExistsContentCondition`：判断某个事实是否存在。
- `FactCompareContentCondition`：比较事实值。
- `CandidateTypeContentCondition`：判断候选资产类型名。
- `CandidateTagContentCondition`：判断候选是否带某个 `ContentTagSO`。
- `CandidateAssetContentCondition`：判断候选是否是指定资产。

常见例子：

- “第 3 波后才出现”：使用 `FactCompareContentCondition`，事实选 `Current Wave.asset`，操作符 `GreaterOrEqual`，值为 `3`。
- “只在某条刷怪轨道出现”：使用 `FactCompareContentCondition`，事实选 `Wave Track Id.asset`，操作符 `Equal`，值为轨道 ID。
- “某个角色专属奖励”：使用 `FactCompareContentCondition`，事实选 `Character.asset`，操作符 `Equal`，值为指定 `CharacterDataSO`。
- “某个候选不允许出现”：使用 `CandidateAssetContentCondition`，`required` 设为 `false`。

### 4.5 配置权重规则 WeightRules

权重规则只修改“已通过条件的候选”的权重，不负责发奖或执行逻辑。

当前支持：

- `AddWeightContentRule`：固定增减权重。
- `MultiplyWeightContentRule`：倍率调整。
- `PreviousRollWeightContentRule`：如果本局之前抽到过，按倍率调整。
- `FactScaleWeightContentRule`：根据数值事实按比例缩放。
- `TagWeightContentRule`：如果 Entry 有指定标签，按倍率和加权调整。
- `FactDrivenCandidateTagWeightContentRule`：数值事实越高，指定标签候选权重越高。

常见例子：

- “幸运越高，宝箱掉落权重越高”：掉落池的宝箱 Entry 使用 `FactScaleWeightContentRule`，事实选 `Luck.asset`。
- “已选火焰流派卡越多，火焰卡权重越高”：火焰卡 Entry 加 `FactDrivenCandidateTagWeightContentRule`，事实选对应 `Upgrade Card Tag Pick Count`，目标标签选火焰内容标签。
- “本局已经出现过的商品降低权重”：使用 `PreviousRollWeightContentRule`，倍率设为小于 1。

## 5. 运行时状态 ContentPoolRuntimeState

`ContentPoolRuntimeState` 记录本局抽取历史：

- 某个 Entry 被抽出过几次。
- 某个 Entry 被选择过几次。
- 某个 Entry 是否之前被抽到过。

池系统会在每次 Roll 后自动记录 “roll” 次数。

“pick” 次数需要业务系统在玩家真正选择、购买或领取后记录。

例如升级卡系统保留 `UpgradeRunState.RecordPick`；如果后续新业务也需要 `MaxPickCount` 生效，需要在业务确认选择时调用对应运行状态的 `RecordPick(entryId)`。

使用建议：

- 每个业务用途维护自己的 RuntimeState。
- 不要让商店和升级卡共用同一个 RuntimeState。
- 本局结束时丢弃 RuntimeState。
- 当前第一版不做存档序列化。

## 6. Modifier 如何使用

### 6.1 Modifier 的作用

`IContentPoolModifier` 可以在抽取前修改候选：

- 增加或降低候选权重。
- 移除候选。
- 修改等级范围。
- 修改品质值。
- 修改价格倍率。
- 修改业务标记。

Modifier 通过 `ContentPoolModifierRegistry` 安装和卸载。

已有可配置 Effect：

- `TagContentPoolWeightModifierEffect`
- `AssetContentPoolWeightModifierEffect`
- `TagContentPoolMetadataModifierEffect`
- `AssetContentPoolMetadataModifierEffect`
- `WaveEnemyWeightModifier`

这些 Effect 继承自 `FeatureEffectBase`，可由升级卡、Buff、饰品、角色能力等系统安装。

### 6.2 Modifier 作用范围

通用 Modifier 有两个范围字段：

- `targetPurpose`
- `affectAllPurposes`

如果只想影响商店，`targetPurpose` 设为 `Shop`。

如果想让某个标签在所有池都提高权重，例如“火焰流派内容出现率提升”，可开启 `affectAllPurposes`，并使用标签匹配。

### 6.3 Modifier 优先级

Modifier 有 `priority`。注册表会按优先级排序执行。

建议：

- 常规权重调整使用默认优先级。
- 强制移除类 Modifier 放在较早优先级。
- 最终价格、品质覆盖类 Modifier 放在较后优先级。

不要依赖同优先级 Modifier 的执行顺序表达关键业务逻辑。

## 7. GameContentCatalog 和 Bootstrap

`GameContentCatalogSO` 是运行时内容总目录，替代旧的固定 `Resources` 路径。

主场景应挂载一个 `GameContentBootstrap`，并绑定：

`Assets/ScriptableObjects/Content/Game Content Catalog.asset`

运行时代码通过：

```csharp
ContentPoolSO pool = GameContentRuntime.Provider.ShopPool;
```

读取池或其他内容。

如果场景没有 Bootstrap，`GameContentRuntime.Provider` 会抛出错误。只有可降级的表现逻辑才应该使用 `TryGetProvider`。

## 8. 各业务系统怎么用

### 8.1 升级卡池

资产：

`Assets/ScriptableObjects/Content/Pools/Upgrade Card Pool.asset`

用途：

`ContentPoolPurpose.UpgradeCard`

内容类型：

`UpgradeCardSO`

默认抽取：

三选一，`DefaultRollCount = 3`。

运行时入口：

- `RewardSelectionManager`
- `UpgradeCardRollService`

配置建议：

- 卡牌资产只保留展示和效果数据。
- 抽取权重、出现条件、最大选择次数、互斥关系都配置到 `ContentPoolEntry`。
- 新增卡牌后运行 `Survivors/Content/Migrate Runtime Content Catalog` 可自动生成/更新卡池条目。
- 如果卡牌有特殊权重，需要在 `UpgradeCardContentPoolTuningUtility` 中维护生成规则，或直接在池资产中编辑。

常见需求：

- “某张卡只能选一次”：Entry 的 `MaxPickCount = 1`。
- “两张卡不能同一轮出现”：互相填入 `MutuallyExclusiveEntryIds`。
- “第 5 波后出现”：加 `FactCompareContentCondition(Current Wave >= 5)`。
- “拥有某个武器后出现”：加对应 `Owned Weapon` 或 `Owned Weapon Tag Count` 事实条件。

### 8.2 宝箱奖励池

资产：

`Assets/ScriptableObjects/Content/Pools/Chest Reward Pool.asset`

用途：

`ContentPoolPurpose.ChestReward`

内容类型：

`AccessoryDataSO`

默认抽取：

三选一，`DefaultRollCount = 3`。

运行时入口：

`RewardSelectionManager`

配置建议：

- 饰品品质写入 `QualityValue`。
- 稀有饰品提高权重或降低权重都在 Entry 上配置。
- 不要在宝箱业务代码里写死全局饰品随机。

### 8.3 商店池

资产：

`Assets/ScriptableObjects/Content/Pools/Shop Pool.asset`

用途：

`ContentPoolPurpose.Shop`

内容类型：

`WeaponDataSO` 和 `AccessoryDataSO`

默认抽取：

当前商店刷新 4 个商品，`DefaultRollCount = 4`。

运行时入口：

`ShopManager`

业务元数据：

- 武器可使用 `MinLevel` / `MaxLevel` 表示生成等级范围。
- 商品折扣或涨价可通过 `PriceMultiplier` 表达。
- 品质可通过 `QualityValue` 表达。

配置建议：

- 不再使用 `ACCESSORY_WEIGHT`、`WEAPON_WEIGHT` 这类硬编码常量。
- 想让某类商品更常出现，用 `BaseWeight`、标签权重规则或 Modifier。
- 想让某个 Buff 临时打折，用 `TagContentPoolMetadataModifierEffect` 或 `AssetContentPoolMetadataModifierEffect` 修改 `PriceMultiplier`。
- 锁定商品保留属于商店业务逻辑，不放进内容池。

### 8.4 掉落池

资产：

`Assets/ScriptableObjects/Content/Pools/Drop Pool.asset`

用途：

`ContentPoolPurpose.Drop`

内容类型：

`CollectionSO`

运行时入口：

`DropManager`

配置建议：

- 金币、宝箱、未来特殊掉落都作为 Entry。
- 幸运值不要写成掉落代码里的固定公式，应通过 `Luck.asset` 事实和权重规则影响宝箱或特殊掉落权重。
- 如果某个掉落要按波次提高权重，加 `Current Wave` 条件或权重规则。

### 8.5 刷怪池

资产：

`Assets/ScriptableObjects/Content/Pools/Wave Spawn Pool.asset`

用途：

`ContentPoolPurpose.WaveSpawn`

内容类型：

`EnemySO` 或 `WaveSpawnPackSO`

运行时入口：

`WaveSpawnExecutionService`

配置方式：

- 波次和轨道节奏仍由 `WaveSpawnPlan` / `WaveSegment` 控制。
- 单一敌人候选使用 `EnemySO`，抽中后按轨道的 `SpawnCountPerBatch` 生成一批同类敌人。
- 组合敌人候选使用 `WaveSpawnPackSO`，抽中后按包内条目展开为多条刷怪请求。
- 敌人候选、刷怪包权重、候选标签必须在 `Wave Spawn Pool.asset` 中配置。
- `WaveSpawnStructureModifier` 只调整轨道节奏，不再筛选敌人标签。
- `WaveEnemyWeightModifier` 作为 `IContentPoolModifier` 修改 `WaveSpawn` 池候选权重。

业务元数据：

- `DomainFlags` 存 `WaveEnemyTag` 的 int 值。
- 未配置 `DomainFlags` 时，运行时按 `WaveEnemyTag.Normal` 处理。
- `WaveSpawnPackSO` 的包条目可以单独覆盖标签；不覆盖时继承池 Entry 的 `DomainFlags`。

常见条件：

- “第 10 波出现”：`Current Wave == 10`。
- “只在 Main Swarm 轨道出现”：`Wave Track Id == Main Swarm`。
- “波次 50% 后出现”：`Wave Progress Percent >= 50`。

注意：

不要再往波次定义里加敌人候选字段；所有单体敌人候选和刷怪包候选都进 `Wave Spawn Pool.asset`。

### 8.6 随机武器奖励池

资产：

`Assets/ScriptableObjects/Content/Pools/Weapon Reward Pool.asset`

用途：

`ContentPoolPurpose.WeaponReward`

内容类型：

`WeaponDataSO`

运行时入口：

`AddRandomWeaponCard`

配置建议：

- 新武器进入 `Assets/ScriptableObjects/Content/Weapons` 后，可通过迁移工具更新池。
- 如果想让某类武器在特定角色或流派下更常出现，用条件、标签和 Modifier。

## 9. 程序接入示例

### 9.1 从已有池抽取

```csharp
ContentPoolSO pool = GameContentRuntime.Provider.ShopPool;
ContentFactSource factSource = ContentFactSource.ForPlayer(player, waveNumber);
factSource.ShopRefreshCount = refreshCount;

ContentRollResult result = contentPoolRollService.Roll(
    pool,
    factSource,
    shopRuntimeState);

for (int i = 0; i < result.Items.Count; i++)
{
    ContentRollItem item = result.Items[i];
    if (item.Content is WeaponDataSO weapon)
    {
        CreateWeaponShopItem(weapon, item.MinLevel, item.MaxLevel, item.PriceMultiplier);
    }
    else if (item.Content is AccessoryDataSO accessory)
    {
        CreateAccessoryShopItem(accessory, item.PriceMultiplier);
    }
}
```

### 9.2 限制候选类型

```csharp
ContentRollResult result = contentPoolRollService.Roll(
    pool,
    factSource,
    runtimeState,
    1,
    entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);
```

业务适配层应该使用 `entryFilter` 做类型校验。不要让通用池服务写死业务类型。

### 9.3 玩家确认选择后记录 Pick

```csharp
ContentRollItem selected = result.Items[index];
runtimeState.RecordPick(selected.EntryId);
```

只有在玩家购买、领取或确认选择后才记录 Pick。仅刷新或展示不应记录 Pick。

### 9.4 新增自定义条件

新增类继承 `ContentCondition`：

```csharp
[System.Serializable]
public sealed class MyContentCondition : ContentCondition
{
    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        return true;
    }
}
```

如果条件依赖事实，覆写 `CollectFactDefinitions`：

```csharp
public override void CollectFactDefinitions(List<FactDefinitionSO> results)
{
    if (factDefinition != null && !results.Contains(factDefinition))
    {
        results.Add(factDefinition);
    }
}
```

新增条件后可通过 `[SerializeReference]` 出现在 Entry 的条件列表中。若 Inspector 不方便选择，需要补一个 SerializeReference 类型选择器或用编辑器工具生成。

### 9.5 新增自定义权重规则

新增类继承 `ContentWeightRule`：

```csharp
[System.Serializable]
public sealed class MyWeightRule : ContentWeightRule
{
    public override float ModifyWeight(
        float currentWeight,
        ContentPoolEvaluationContext context,
        ContentPoolEntry entry)
    {
        return currentWeight;
    }
}
```

规则只返回新权重，不应执行结果逻辑。

### 9.6 新增自定义 Modifier

```csharp
[System.Serializable]
public sealed class MyPoolModifierEffect : ContentPoolModifierEffect
{
    public override string Description => "调整指定内容池候选。";

    public override void ModifyCandidates(
        ContentPoolEvaluationContext context,
        List<ContentPoolCandidate> candidates)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate.Entry != null && candidate.Entry.HasTag(targetTag))
            {
                candidate.Weight *= 2f;
            }
        }
    }
}
```

Modifier 需要通过 Feature/Buff/升级卡等系统安装，安装时会注册，卸载时会反注册。

## 10. 内容制作流程

### 10.1 新增升级卡

1. 创建 `UpgradeCardSO`。
2. 配置展示、标签和效果。
3. 运行 `Survivors/Content/Migrate Runtime Content Catalog`。
4. 打开 `Upgrade Card Pool.asset`。
5. 检查 Entry 的权重、条件、互斥和次数限制。
6. 进入游戏验证三选一是否正常出现。

### 10.2 新增饰品

1. 创建 `AccessoryDataSO`。
2. 放入 `Assets/ScriptableObjects/Content/Accessories`。
3. 运行迁移工具。
4. 检查 `Chest Reward Pool.asset` 和 `Shop Pool.asset`。
5. 按需要调整宝箱和商店权重。

### 10.3 新增武器

1. 创建 `WeaponDataSO`。
2. 放入 `Assets/ScriptableObjects/Content/Weapons`。
3. 运行迁移工具。
4. 检查 `Shop Pool.asset` 和 `Weapon Reward Pool.asset`。
5. 配置等级范围、权重、价格倍率。

### 10.4 新增敌人到刷怪池

1. 创建或确认 `EnemySO`。
2. 打开 `Wave Spawn Pool.asset`。
3. 手动新增 Entry。
4. `Content` 指向敌人资产。
5. 设置 `BaseWeight`。
6. `DomainFlags` 填入对应 `WaveEnemyTag` bit mask 数值。
7. 添加 `Current Wave`、`Wave Track Id`、`Wave Progress Percent` 条件。
8. 运行波次验证。

刷怪池不要依赖迁移工具自动生成候选。

### 10.5 新增刷怪包到刷怪池

1. 在 Project 面板创建 `Survivors/Gameplay/Waves/Wave Spawn Pack`。
2. 配置 `PackId`。
3. 在 `Entries` 中添加多个敌人条目。
4. 每个条目配置 `EnemyDefinition`、`SpawnCount`。
5. 如果条目需要自己的标签，开启 `OverrideTags` 并设置 `EnemyTags`。
6. 打开 `Wave Spawn Pool.asset`，新增 Entry。
7. `Content` 指向该 `WaveSpawnPackSO`。
8. 设置 `BaseWeight`、`DomainFlags` 和条件。

示例：

- `突袭小队` 包：3 个近战普通怪 + 1 个远程怪。
- `精英护卫` 包：1 个精英怪 + 4 个普通怪。
- `后半波压制` 包：条件配置 `Wave Progress Percent >= 50`。

## 11. 常见错误和排查

### 11.1 抽不到任何结果

检查：

- Pool 是否为空。
- Entry 的 `Content` 是否为空。
- `BaseWeight` 是否为 0。
- 所有候选是否都被 Conditions 过滤。
- 所有候选是否都被 Modifier 移除或降到 0 权重。
- `MaxRollCount` 或 `MaxPickCount` 是否已经达到上限。
- 业务 `entryFilter` 是否过滤掉了所有候选。

### 11.2 结果类型不对

检查：

- Pool 的 `Purpose` 是否正确。
- 业务系统传入的 Pool 是否来自正确 Catalog 字段。
- Entry 的 `Content` 是否绑定错类型。
- 业务入口是否使用了 `entryFilter` 限制类型。

### 11.3 权重看起来不生效

检查：

- 候选是否先被条件过滤。
- 权重规则依赖的 FactDefinitionSO 是否配置正确。
- 事实类型是否匹配。
- Modifier 是否安装。
- Modifier 的 `targetPurpose` 是否匹配当前池。
- 多个 Modifier 是否互相抵消。

### 11.4 MaxPickCount 不生效

检查：

- 玩家确认选择后，业务系统是否调用了 `RecordPick(entryId)`。
- 是否使用了同一个 `ContentPoolRuntimeState`。
- EntryId 是否稳定，没有在运行中变化。

### 11.5 刷怪池候选不出现

检查：

- `Wave Spawn Pool.asset` 的 Entry 是否绑定 `EnemySO` 或 `WaveSpawnPackSO`。
- `WaveSpawnPackSO` 是否至少包含一个有效条目。
- `Current Wave` 条件是否匹配当前波次。
- `Wave Track Id` 字符串是否与轨道 ID 完全一致。
- `Wave Progress Percent` 条件是否过窄。
- `DomainFlags` 是否误填为不符合 Modifier 目标标签的值。
- `WaveManager` 是否绑定或能从 Catalog 读取 `WaveSpawnPool`。

### 11.6 GameContentRuntime 未初始化

检查：

- 当前场景是否挂了 `GameContentBootstrap`。
- Bootstrap 是否绑定了 `Game Content Catalog.asset`。
- Catalog 是否通过 `ValidateCatalog`。
- 是否在 Awake 顺序极早的对象中访问 Provider。必要时调整执行顺序或显式保证 Bootstrap 存在。

## 12. 验证建议

### 12.1 代码编译

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

当前项目可能存在 Unity 引用版本冲突 warning，只要没有 error 即可继续。

### 12.2 EditMode 测试

关闭已打开的 Unity 项目实例后执行：

```powershell
& 'E:\UnityEditor\2022.3.62f3c1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'E:\AXR_Projects\unity\Survivors' `
  -runTests `
  -testPlatform EditMode `
  -testFilter ContentPoolTests `
  -testResults 'E:\AXR_Projects\unity\Survivors\Temp\ContentPoolTests.xml' `
  -quit `
  -nographics `
  -logFile 'E:\AXR_Projects\unity\Survivors\Temp\ContentPoolTests.log'
```

如果 Unity 报 `Multiple Unity instances cannot open the same project`，说明已有 Unity 实例打开了同一项目，需要先关闭。

### 12.3 残留旧系统扫描

```powershell
rg -n "UpgradeCardPoolSO|UpgradeCardOfferContext|UpgradeCardOfferConditions|WaveEnemySpawnCandidate|WaveEnemySpawnOption|ACCESSORY_WEIGHT|WEAPON_WEIGHT|Default Upgrade Card Pool|Assets/Resources/Data/(UpgradeCards|Weapons|Accessories|Characters|Collections|Waves)" Assets/Scripts Assets/Tests Assets/ScriptableObjects
```

正常情况下不应命中玩法池旧系统。

## 13. 设计约束

- 不要在通用内容池里写购买、装备、应用升级、生成敌人、掉落发放等业务逻辑。
- 不要在业务代码中重新实现随机权重。
- 不要继续扩展旧的专用池类型。
- 不要把 Fact 键写成散落字符串。配置层使用 `FactDefinitionSO`，运行时内建通道使用 `ContentFactIds`。
- 不要依赖 `Resources.Load` 读取玩法内容；运行时统一走 `GameContentRuntime.Provider` 和 `GameContentCatalogSO`。
- 不要把所有系统塞进一个统一管理器；各业务系统只负责消费抽取结果。

## 14. 推荐维护方式

- 策划主要维护 `Assets/ScriptableObjects/Content/Pools/` 和 `Assets/ScriptableObjects/Content/Facts/`。
- 程序新增规则时优先新增 `ContentCondition`、`ContentWeightRule` 或 `IContentPoolModifier` 实现，不改 `ContentPoolRollService` 主流程。
- 新增业务池时先扩展 `ContentPoolPurpose`，再在 Catalog 中加显式字段和校验。
- 每次内容迁移后运行编译、内容池测试和旧系统残留扫描。
- 复杂规则优先写 EditMode 测试，避免靠手动刷新商店或跑波次验证概率。
