# Roguelike 装备信息展示系统终版方案

## 1. 目标

本方案面向 roguelike / roguelite 游戏中的武器、饰品、Buff、升级奖励、临时强化与结算信息展示，目标是建立一套类似《土豆兄弟》的紧凑型信息系统。

核心要求：

- 信息紧凑，适合战斗间隙和选择界面快速扫读。
- 属性、特殊效果、标签、状态都支持小图标。
- 展示内容必须随真实运行时状态变化而更新。
- 武器、饰品、Buff、升级卡、后天强化带来的新能力，必须能直接出现在详情里。
- 不显示“来源追踪”，只显示“当前拥有的内容”。
- UI 只负责渲染，不直接理解玩法规则、不直接计算战斗数值。
- 不新增第二套运行时物品模型，避免 UI 显示和真实战斗状态分叉。

这套系统的本质不是“做一个更长的描述框”，而是建立一条从真实玩法状态到结构化展示数据的单向管线。

---

## 2. 核心边界

### 2.1 唯一事实源

展示系统不能替换、复制或并行维护战斗运行时状态。

项目已有的运行时模型应继续作为唯一事实源：

| 类型 | 事实源职责 | 展示系统使用方式 |
|---|---|---|
| `Weapon` | 单把武器的真实等级、运行时攻击属性、冷却、攻击方式 | 读取当前快照，生成武器详情 |
| `WeaponsHolder` | 当前装备武器列表、武器增删合并、武器槽位变化 | 读取已装备武器集合，生成构筑总览和武器列表 |
| `RuntimeAccessoryData` | 单个已装备饰品实例及其 `RuntimeId` | 读取饰品定义和实例标识，生成饰品详情 |
| `AccessoryManager` | 当前饰品列表、饰品装备/卸下 | 读取已装备饰品集合，生成饰品列表 |
| `BuffRuntimeHandle` | 单个 Buff 运行时实例、层数合并前状态、剩余时间 | 读取 Buff 运行时信息，生成 Buff 详情 |
| `FeatureHost` / `FeatureHostSourceHandle` | 已安装的运行时能力实例 | 在需要显示实际运行时效果时读取能力快照 |
| `FeatureEffectBase` | 可安装特殊能力、命中修正、展示标题/图标/说明 | 作为特殊效果展示数据来源 |

因此，本方案中不再设计 `RuntimeWeapon`、`RuntimeAccessory`、`RuntimeAugment` 这类替代模型。

### 2.2 展示层只生成快照

展示层可以生成 `ItemInfoViewData`，但它必须是只读展示快照。

它允许包含：

- 名称
- 图标
- 品质
- 等级
- 当前属性行
- 当前特殊效果行
- 当前标签
- 当前可操作项
- 当前选择预览差异

它不允许包含：

- 战斗结算逻辑
- Buff 生命周期控制
- 武器等级真实状态
- 饰品装备真实状态
- Feature 安装/卸载状态
- 与真实运行时并行的可变 Augment 列表

一句话：`ItemInfoViewData` 是“显示结果”，不是“玩法状态”。

---

## 3. 设计原则

### 3.1 紧凑优先

展示应以短行、图标、颜色、层级清晰为主，避免长篇段落。

推荐单行结构：

```text
[图标] 名称                    数值 / 状态
```

推荐详情结构：

```text
[图标] 心之钢 +1
近战 / 重型

[剑] 攻击力       28
[闪电] 攻击速度   80
[爆裂] 暴击率     5%

特殊效果
[心脏] 巨像回响
命中精英敌人时，永久获得 2 点最大生命。
```

### 3.2 结构化优先

不要把所有内容压成一段字符串。

属性、特殊效果、标签、状态、选择预览、操作项都应是独立结构，UI 根据结构渲染，而不是解析文本。

### 3.3 真实运行时优先

一个装备最终展示什么，不只来自静态配置，还来自当前运行时：

- 当前等级
- 当前属性计算结果
- 当前装备数量
- 当前 Buff 层数
- 当前 Buff 剩余时间
- 当前已安装 `FeatureEffectBase`
- 当前由升级卡、Buff、饰品、角色能力造成的附加能力

展示系统必须从真实运行时读取这些信息，而不是自己保存另一份。

### 3.4 无来源展示

不展示“这个效果来自某个系统”。

玩家只看到：

- 这个装备现在有什么
- 这个 Buff 当前是多少层
- 这个效果是否正在生效
- 这个选择会带来什么变化

来源标识可以作为内部查询键存在，但不应成为玩家界面内容。

---

## 4. 信息分层

系统按使用场景分为五层：

| 层级 | 场景 | 展示内容 |
|---|---|---|
| HUD 简略层 | 战斗中常驻 | 生命、经验、金币、波次、武器槽、Buff 图标 |
| 详情层 | 悬停、点击、打开弹窗 | 武器、饰品、Buff、升级卡、特殊能力的完整说明 |
| 选择层 | 奖励、商店、宝箱 | 选择后会获得什么、会变成什么 |
| 构筑总览层 | 暂停页、背包页 | 当前构筑概览、核心属性、核心特殊效果 |
| 复盘层 | 结算页 | 最终构筑摘要、关键成长、关键效果 |

每一层都应该复用同一套展示数据结构，只是渲染密度不同。

---

## 5. 展示数据模型

### 5.1 ItemInfoViewData

所有界面都从统一 ViewData 渲染，不直接读取玩法对象。

```csharp
public sealed class ItemInfoViewData
{
    public string Id;
    public string Name;
    public Sprite Icon;
    public ItemInfoKind Kind;
    public ItemRarityView Rarity;
    public int Level;

    public IReadOnlyList<InfoStatLine> StatLines;
    public IReadOnlyList<InfoEffectLine> EffectLines;
    public IReadOnlyList<InfoTagLine> TagLines;
    public IReadOnlyList<InfoStateLine> StateLines;
    public IReadOnlyList<InfoActionLine> ActionLines;
}
```

`Id` 只用于 UI 刷新、列表复用、悬停定位和测试断言，不用于承载玩法状态。

### 5.2 InfoStatLine

```csharp
public sealed class InfoStatLine
{
    public Sprite Icon;
    public string Name;
    public string Value;
    public string DeltaText;
    public InfoTone Tone;
}
```

用于展示属性和选择差异。

示例：

```text
[剑] 攻击力       28
[闪电] 攻击速度   80
[心] 最大生命     +12
[盾] 护甲         -2
```

### 5.3 InfoEffectLine

```csharp
public sealed class InfoEffectLine
{
    public Sprite Icon;
    public string Title;
    public string Description;
    public bool IsActive;
    public InfoTone Tone;
}
```

用于展示特殊效果、后天附加能力、Buff 能力、升级卡能力。

示例：

```text
[心脏] 巨像回响
命中精英敌人时，永久获得 2 点最大生命。
```

### 5.4 InfoTagLine

```csharp
public sealed class InfoTagLine
{
    public Sprite Icon;
    public string Text;
}
```

用于展示近战、远程、投射物、经济、生存、暴击、范围等标签。

### 5.5 InfoStateLine

```csharp
public sealed class InfoStateLine
{
    public Sprite Icon;
    public string Name;
    public string Value;
    public float NormalizedProgress;
}
```

用于展示运行时状态。

示例：

```text
[沙漏] 剩余时间   4.2s
[层数] 层数       3 / 5
[冷却] 冷却       0.6s
```

### 5.6 InfoActionLine

```csharp
public sealed class InfoActionLine
{
    public Sprite Icon;
    public string Text;
    public bool IsAvailable;
}
```

用于装备、购买、出售、合并、替换等操作提示。

---

## 6. 事实源适配器

### 6.1 为什么需要适配器

真实运行时对象适合战斗系统，但不一定适合 UI 直接渲染。

例如：

- `Weapon` 关注攻击、冷却、索敌、命中。
- `WeaponsHolder` 关注武器槽位、添加、移除、合并。
- `FeatureEffectBase` 关注能力安装、卸载、命中修正和描述。
- `BuffRuntimeHandle` 关注 Buff 生命周期和剩余时间。

UI 如果直接理解这些对象，会导致界面层变成半个玩法系统。正确做法是加一层只读适配器，把真实对象翻译成展示快照。

### 6.2 推荐适配器接口

```csharp
public interface IItemInfoBuilder
{
    ItemInfoViewData BuildWeapon(Weapon weapon);
    ItemInfoViewData BuildEquippedWeapon(EquippedWeaponInfo weaponInfo);
    ItemInfoViewData BuildAccessory(RuntimeAccessoryData accessoryData);
    ItemInfoViewData BuildBuff(BuffRuntimeHandle buffHandle);
    ItemInfoViewData BuildUpgradeCard(UpgradeCardSO card);
}
```

如果后续类型变多，可以拆成多个 Builder：

```text
WeaponInfoBuilder
AccessoryInfoBuilder
BuffInfoBuilder
UpgradeCardInfoBuilder
ChoicePreviewBuilder
BuildSummaryInfoBuilder
```

这里的 Builder 只读取，不写入。

### 6.3 能力展示适配器

`FeatureEffectBase` 已经拥有标题、图标、描述和额外描述信息的基础能力，应作为特殊效果展示的核心输入。

推荐规则：

```text
FeatureEffectBase.Title       -> InfoEffectLine.Title
FeatureEffectBase.Icon        -> InfoEffectLine.Icon
FeatureEffectBase.Description -> InfoEffectLine.Description
GetExtraInfos()               -> 附加 InfoStatLine 或 InfoEffectLine
```

当某个 `FeatureEffectBase` 的实际效果会随运行时变化时，应由该 Feature 自己通过展示接口暴露当前值，而不是让 UI 猜。

可选扩展接口：

```csharp
public interface IRuntimeInfoProvider
{
    void AppendRuntimeInfo(ItemInfoAppendContext context);
}
```

`FeatureEffectBase` 子类如果需要展示动态信息，可以实现该接口。

示例：

```text
[火焰] 灼烧
命中时附加灼烧，每秒造成 8 点伤害，持续 3 秒。
```

其中 `8` 和 `3` 如果来自运行时计算，就由 Feature 自己追加到展示上下文，而不是 UI 手写规则。

---

## 7. 后天能力的正确建模方式

### 7.1 不新增 RuntimeAugment

后天强化不能在展示系统里另起 `RuntimeAugment`。

如果一个强化会真正改变战斗能力，它必须进入现有战斗系统，例如：

- 安装为 `FeatureEffectBase`
- 添加到 `FeatureHost`
- 添加属性修饰到 `PropertiesManager`
- 调用现有武器升级、饰品装备、Buff 添加流程
- 或通过现有系统支持的扩展点改变真实战斗行为

展示系统只读取这些结果。

### 7.2 附加到装备的能力如何显示

如果某个升级让“心之钢”获得新功能，不能只在 UI 上追加一条文案。

正确链路应是：

1. 升级效果通过玩法系统找到目标武器或目标装备上下文。
2. 真实战斗系统安装对应能力，例如安装一个 `FeatureEffectBase` 或注册一段武器扩展能力。
3. 该能力拥有可展示的 `Title`、`Icon`、`Description`。
4. 详情面板打开或刷新时，从真实运行时读取该能力。
5. Builder 把能力转换成 `InfoEffectLine`。
6. UI 渲染新增特殊效果。

关键点：UI 显示的新功能必须和真实战斗新功能来自同一个对象或同一个能力定义。

### 7.3 装备定向能力的显示归属

有些 `FeatureEffectBase` 是全局安装在角色身上的，但玩家理解上希望显示在某把武器详情里。

这类能力需要一个“展示归属”概念，但它仍然不是第二套运行时状态。

推荐新增轻量接口：

```csharp
public interface IItemInfoAttachment
{
    string DisplayTargetRuntimeId { get; }
    ItemInfoAttachmentKind AttachmentKind { get; }
}
```

用途：

- 标记这个能力应该显示在哪个武器、饰品或 Buff 下。
- 只影响展示归类，不决定战斗是否生效。
- 实际战斗仍由 `FeatureEffectBase`、`FeatureHost`、`Weapon` 等现有系统负责。

如果暂时没有稳定的目标 RuntimeId，也可以先显示在“全局特殊效果”区域，而不是强行塞进某个装备详情。

---

## 8. 构建管线

推荐管线：

```text
真实运行时对象
    -> 只读适配器
    -> ItemInfoViewData
    -> UI Renderer
```

展开后：

```text
Weapon / EquippedWeaponInfo
RuntimeAccessoryData
BuffRuntimeHandle
UpgradeCardSO
FeatureEffectBase
PropertiesManager 当前值
    -> InfoBuilder
    -> ItemInfoViewData
    -> 详情面板 / 选择卡 / 商店 / 构筑页 / 结算页
```

管线约束：

- Builder 不缓存长期状态。
- Builder 不安装 Feature。
- Builder 不修改武器等级。
- Builder 不修改饰品列表。
- Builder 不修改 Buff 时间。
- Builder 可以做短生命周期的预览模拟，但不能写回真实运行时。

---

## 9. 选择预览规则

选择卡不只展示“这是什么”，还要展示“选了会变成什么”。

### 9.1 应显示内容

- 新增属性
- 新增特殊效果
- 目标装备
- 可否装备
- 选择后差异
- 购买、替换、合并等操作结果

### 9.2 应避免内容

- 冗长解释
- 重复描述
- 来源追踪
- 业务内部名词
- 只存在于 UI 的虚假强化状态

### 9.3 预览模拟边界

选择预览可以做“只读模拟”，但必须和真实应用逻辑共享规则。

推荐做法：

```text
候选项
当前真实状态快照
应用规则的只读模拟器
    -> 预览 ViewData
```

禁止做法：

```text
候选项
UI 自己拼文案
UI 自己猜属性变化
UI 自己维护一份强化列表
```

### 9.4 推荐展示格式

原始武器：

```text
心之钢 +1
[剑] 攻击力       28
[闪电] 攻击速度   80
[爆裂] 暴击率     5%

特殊效果
无
```

选择强化后预览：

```text
心之钢 +1
[剑] 攻击力       28
[闪电] 攻击速度   80
[爆裂] 暴击率     5%

新增特殊效果
[心脏] 巨像回响
命中精英敌人时，永久获得 2 点最大生命。
```

---

## 10. 装备详情展示规则

详情面板按固定顺序渲染：

1. 名称与图标
2. 品质与等级
3. 标签
4. 核心属性
5. 运行时状态
6. 特殊效果
7. 可用操作

### 10.1 武器

武器详情重点展示：

- 当前等级
- 当前攻击力
- 当前攻击速度
- 当前暴击率
- 当前暴击倍率
- 当前攻击范围
- 当前击退
- 攻击标签
- 武器本体效果
- 真实运行时已附加到该武器的能力

数据来源优先级：

```text
Weapon 当前运行时属性
Weapon.WeaponData 静态标签和基础展示
WeaponsHolder 当前装备关系
FeatureEffectBase / IRuntimeInfoProvider 当前特殊效果
```

### 10.2 饰品

饰品详情重点展示：

- 品质
- 属性修正
- 常驻特殊效果
- 当前装备数量
- 真实运行时已安装的特殊能力

数据来源优先级：

```text
RuntimeAccessoryData.RuntimeId
RuntimeAccessoryData.AccessoryData
AccessoryDataSO.PropertyModifiers
AccessoryDataSO.SpecialFeatures
FeatureHost 中同 RuntimeId 对应的运行时 Feature
```

### 10.3 Buff

Buff 详情重点展示：

- 正面、负面或中性
- 当前层数
- 最大层数
- 剩余时间
- 当前实际效果
- 当前特殊能力

数据来源优先级：

```text
BuffRuntimeHandle
BuffDataSO
BuffRuntimeHandle.CreateMergedViewData 产出的合并视图
BuffDataSO.SpecialFeatures
FeatureHost 中 Buff RuntimeSourceId 对应的运行时 Feature
```

### 10.4 升级卡

升级卡详情重点展示：

- 品质
- 标签
- 属性修正
- 特殊能力
- 选择次数限制
- 选择后会作用到哪里

数据来源优先级：

```text
UpgradeCardSO.SpecialFeatures
PropertyModifierFeature
UpgradeCardSO.GetExtraInfos()
选择预览服务产出的差异
```

---

## 11. 图标系统

图标是这套方案的关键，不是装饰。

### 11.1 属性图标

每个属性名都映射到一个小图标，例如：

| 属性 | 图标语义 |
|---|---|
| 攻击力 | 剑 |
| 攻击速度 | 闪电 |
| 暴击率 | 爆裂 |
| 暴击伤害 | 爆裂强化 |
| 最大生命 | 心 |
| 护甲 | 盾 |
| 移速 | 箭头 |
| 拾取范围 | 磁铁 |
| 金币收益 | 金币 |

建议新增 `StatIconCatalog`：

```csharp
public sealed class StatIconCatalog : ScriptableObject
{
    public Sprite GetIcon(PropType propType);
    public Sprite DefaultStatIcon;
}
```

### 11.2 效果图标

每个特殊效果尽量自带图标。

优先级：

```text
FeatureEffectBase.Icon
BuffDataSO.Icon
AccessoryDataSO.Icon
WeaponDataSO.Icon
默认特殊效果图标
```

升级卡当前不提供独立图标，展示层应直接容忍空值。

如果没有图标，使用默认特殊效果图标，不能让 UI 因缺图标而错位。

### 11.3 标签图标

标签可做成小 Chip 或轻量图标块，例如：

- 近战
- 远程
- 投射物
- 范围
- 经济
- 生存
- 暴击
- 重型
- 快速

标签适合放在名称下方，不应挤占属性区。

---

## 12. 典型展示示例

### 12.1 心之钢原始状态

```text
[武器图标] 心之钢 +1
近战 / 重型

[剑] 攻击力       28
[闪电] 攻击速度   80
[爆裂] 暴击率     5%

特殊效果
无
```

### 12.2 获得真实强化后

假设玩家获得“巨像回响”，该强化真实安装为某个 `FeatureEffectBase`，并且展示归属指向心之钢。

详情刷新后显示：

```text
[武器图标] 心之钢 +1
近战 / 重型

[剑] 攻击力       28
[闪电] 攻击速度   80
[爆裂] 暴击率     5%

特殊效果
[心脏] 巨像回响
命中精英敌人时，永久获得 2 点最大生命。
```

### 12.3 连续强化后的状态

```text
[武器图标] 心之钢 +1
近战 / 重型

[剑] 攻击力       32
[闪电] 攻击速度   85
[爆裂] 暴击率     8%

特殊效果
[心脏] 巨像回响
命中精英敌人时，永久获得 2 点最大生命。

[护盾] 坚韧打击
最大生命越高，造成伤害越高。
```

这里的攻击力、攻速、暴击率必须来自 `Weapon` 或统一属性计算结果；两个特殊效果必须来自真实安装的能力，而不是 UI 自己保存的强化记录。

---

## 13. 各界面职责

### 13.1 HUD

只保留最必要信息：

- 生命
- 经验
- 金币
- 波次
- 武器槽
- Buff 图标

HUD 只显示摘要。详细说明通过悬停或点击进入详情层。

### 13.2 详情页

展示完整装备信息，不做过度排版。

职责：

- 渲染 `ItemInfoViewData`
- 根据 ViewData 自动生成属性区、效果区、标签区、状态区
- 不直接读 `PropertiesManager`
- 不直接读 `FeatureHost`
- 不直接拼接 Feature 文案

### 13.3 商店页

展示价格、是否可买、购买后会得到什么。

推荐包含：

- 当前商品信息
- 购买后属性或效果变化
- 是否会合并
- 是否会替换
- 是否因金币不足不可购买

### 13.4 奖励页

展示选择后差异，是玩家决策的核心界面。

推荐包含：

- 获得内容
- 新增属性
- 新增特殊效果
- 目标装备或全局效果
- 与当前构筑的关键关联

### 13.5 暂停构筑页

展示当前完整构筑，方便玩家理解当前流派。

推荐分区：

- 当前核心属性
- 已装备武器
- 已装备饰品
- 当前 Buff
- 全局特殊效果
- 关键构筑标签

### 13.6 结算页

展示最终构筑摘要，帮助玩家理解本局成长路径。

推荐包含：

- 最终武器列表
- 最终饰品列表
- 最终核心属性
- 最终关键特殊效果
- 主要伤害来源
- 主要生存来源

---

## 14. 动态刷新策略

### 14.1 默认策略

推荐默认策略：

- UI 打开时重新构建。
- 武器、饰品、Buff 列表变化时重新构建相关区域。
- Buff 剩余时间这类高频状态可以定时轻量刷新。
- 选择预览使用只读模拟，不写回真实状态。
- 不要求所有面板每帧全量重排。

这样能兼顾：

- 展示正确性
- 实现稳定性
- 维护成本
- 性能可控

### 14.2 事件触发点

可接入的刷新触发点：

| 事件 | 刷新范围 |
|---|---|
| `WeaponsHolder.OnWeaponsChanged` | 武器列表、构筑页、武器详情 |
| `WeaponsHolder.OnWeaponBenefitBonusChanged` | 武器属性详情、构筑属性摘要 |
| `AccessoryManager.OnAccessoryEquipped` | 饰品列表、构筑页、属性摘要 |
| `AccessoryManager.OnAccessoryUnequipped` | 饰品列表、构筑页、属性摘要 |
| Buff 添加、移除、层数变化 | Buff 列表、Buff 详情 |
| `PropertiesManager.OnPropertyChanged` | 属性摘要、受影响装备详情 |
| `FeatureHost.InstallFeature` / `RemoveFeature` | 特殊效果区、构筑页 |

如果某些事件当前没有公开，不建议 UI 直接侵入内部字段。优先补充窄事件或只读查询接口。

### 14.3 实时刷新边界

需要实时刷新的内容：

- Buff 剩余时间
- 冷却进度
- 临时状态是否激活

不需要实时刷新的内容：

- 静态描述
- 标签
- 品质
- 长期属性
- 已装备列表，除非发生变化

---

## 15. 推荐模块划分

```text
InfoViewData
InfoBuilders
InfoSourceAdapters
InfoPreview
IconCatalog
InfoUIViews
```

### 15.1 InfoViewData

定义所有展示数据结构。

只包含 UI 渲染需要的只读信息，不包含玩法状态。

### 15.2 InfoBuilders

从真实运行时对象生成展示数据。

建议包含：

- `WeaponInfoBuilder`
- `AccessoryInfoBuilder`
- `BuffInfoBuilder`
- `UpgradeCardInfoBuilder`
- `BuildSummaryInfoBuilder`

### 15.3 InfoSourceAdapters

负责把现有系统对象适配成 Builder 容易消费的只读输入。

例如：

- 从 `Weapon` 读取当前攻击属性。
- 从 `RuntimeAccessoryData` 读取饰品配置和 RuntimeId。
- 从 `FeatureHostSourceHandle` 读取运行时 Feature。
- 从 `BuffRuntimeHandle` 读取剩余时间。

### 15.4 InfoPreview

生成奖励、商店、合成的结果预览。

它可以做只读模拟，但不写回真实运行时。

### 15.5 IconCatalog

负责统一属性、效果、标签图标。

### 15.6 InfoUIViews

只负责渲染，不参与玩法计算。

---

## 16. 设计边界

不要做的东西：

- 新增第二套 `RuntimeWeapon`
- 新增第二套 `RuntimeAccessory`
- 新增第二套 `RuntimeAugment`
- UI 自己维护强化列表
- UI 自己计算战斗结果
- UI 自己安装或卸载 Feature
- 来源追踪展示
- 复杂全局信息总线
- 每个按钮独立一套专用逻辑
- 纯文本替代结构化数据

要坚持的东西：

- 现有运行时模型是唯一事实源。
- 展示数据是只读快照。
- 属性和特殊效果都支持图标。
- 详情必须能体现后天获得的真实能力。
- Feature 的战斗效果和展示说明应来自同一份能力定义。
- UI 渲染和玩法逻辑严格分离。

---

## 17. 实施顺序

建议按下面顺序落地：

1. 定义展示数据结构 `ItemInfoViewData`、`InfoStatLine`、`InfoEffectLine`、`InfoTagLine`、`InfoStateLine`。
2. 定义 `StatIconCatalog`、`EffectIconCatalog`、`TagIconCatalog`。
3. 实现 `FeatureEffectBase` 到 `InfoEffectLine` 的转换。
4. 实现 `WeaponInfoBuilder`，从 `Weapon` / `EquippedWeaponInfo` 读取真实武器状态。
5. 实现 `AccessoryInfoBuilder`，从 `RuntimeAccessoryData` 和 `FeatureHost` 读取饰品状态。
6. 实现 `BuffInfoBuilder`，从 `BuffRuntimeHandle` 或合并 Buff 视图读取状态。
7. 实现 `UpgradeCardInfoBuilder`，从 `UpgradeCardSO` 读取奖励展示。
8. 实现 `ChoicePreviewBuilder`，只读模拟选择后差异。
9. 接入装备详情面板。
10. 接入奖励选择界面。
11. 接入商店界面。
12. 接入暂停构筑总览。
13. 接入结算页。

每一步都可以独立验证，不需要一次性替换所有 UI。

---

## 18. 验收标准

当系统完成后，应满足以下条件：

- 任意武器都能显示图标化属性和特殊效果。
- 任意饰品都能显示图标化属性和特殊效果。
- 任意 Buff 都能显示层数、剩余时间和实际效果。
- 武器获得后天真实能力后，详情中能立即显示新增功能。
- 饰品或 Buff 安装的 `FeatureEffectBase` 能出现在对应详情或全局特殊效果区。
- 奖励选择能显示选择后变化。
- 商店能显示购买后结果。
- 暂停页能显示当前构筑概览。
- 结算页能显示最终构筑摘要。
- UI 不维护第二份运行时状态。
- UI 展示数值与真实战斗数值不会因为双状态而分叉。
- 整个系统不依赖来源展示，也不依赖长描述堆叠。

---

## 19. 最关键的落地结论

这套方案最终应落成三句话：

```text
真实状态只在现有运行时系统里。
展示系统只做只读适配和结构化快照。
特殊能力既驱动战斗，也提供自己的展示信息。
```

只要坚持这三点，就能支持“心之钢获得新功能后详情自动显示”，同时避免新增 `RuntimeWeapon` / `RuntimeAccessory` / `RuntimeAugment` 造成双运行时状态。
