# Feature 系统配置说明

## 目标
这套系统用于让 `CharacterDataSO` 与 `AccessoryDataSO` 通过统一模型配置：
- 属性修饰
- 特殊能力
- 运行时挂载
- UI 展示

## 当前目录
- 定义层：`Assets/Scripts/Features/Definitions/`
- 运行时：`Assets/Scripts/Features/Runtime/`
- 最小编辑器扩展：`Assets/Scripts/ScriptableObjects/Editor/`

## 当前编辑器策略
当前只对 `specialFeatures` 做最小幅度扩展：
- 保留能力类型下拉菜单
- 尊重 Unity 原生的 `[HideInInspector]` 隐藏语义
- 不改造普通属性列表、初始武器列表、初始饰品列表的默认 Inspector 行为

## 如何给角色配置效果
打开 `CharacterDataSO`：

### 1. 角色额外属性
在 `extraProps` 列表中直接添加：
- `propType`
- `modifierType`
- `value`

### 2. 角色特殊能力
在 `specialFeatures` 列表中点击 `+` 下拉：
- 选择一个 `FeatureEffectBase` 子类
- 例如 `Low Health Explosion Feature Effect`
- 选择后会自动创建一个新 element
- 被 `[HideInInspector]` 标记的字段不会显示在面板中

### 3. 初始装备
- `initialWeapons`：添加初始武器和等级
- `initialAccessories`：添加初始饰品

## 如何给饰品配置效果
打开 `AccessoryDataSO`：

### 1. 饰品属性修饰
在 `propertyModifiers` 列表中直接添加

### 2. 饰品特殊能力
在 `specialFeatures` 列表中点击 `+` 下拉：
- 选择具体 feature 类型
- 配置参数

## 运行时模型
所有来源统一实现 `IFeatureSource`。
运行时由 `FeatureHost` 统一安装，`FeatureInstaller` 提供安装入口。

### 核心类型
- `FeatureContext`：运行时上下文
- `FeatureEffectBase`：所有运行时 feature 基类
- `PropertyModifierEffect`：属性修饰器运行时实现（不会出现在能力菜单）
- `LowHealthExplosionFeatureEffect`：事件驱动型 feature 示例

## 新增一个 feature 的步骤
1. 在 `Assets/Scripts/Features/Runtime/` 下新建类，继承 `FeatureEffectBase`
2. 实现：
   - `OnInstall(FeatureContext context)`
   - `OnUninstall(FeatureContext context)`
   - 如有需要可重写 `OnUpdate(FeatureContext context, float deltaTime)`
3. 如果依赖某个组件或事件，由 feature 自己在 `OnInstall` 中订阅，在 `OnUninstall` 中解绑
4. 如不希望它出现在能力菜单中，标记 `[HideInFeatureMenu]`
5. 如不希望某个字段出现在 Inspector 中，直接标记 Unity 原生的 `[HideInInspector]`
6. 编译后即可在 `CharacterDataSO` / `AccessoryDataSO` 的 `specialFeatures` 下拉中选到

## 当前推荐实践
- 能事件驱动就优先事件驱动
- 确实需要轮询时再使用 `OnUpdate`
- 不要让 `FeatureHost` 猜 feature 依赖什么事件
- feature 自己管理依赖和订阅
- 尽量使用 `FeatureContext` 提供的强类型字段，例如：
  - `context.PlayerHealth`
  - `context.PropertiesManager`
  - `context.WeaponsHolder`
  - `context.AccessoryManager`
  - `context.PlayerLevel`

## 当前已修复点
- `specialFeatures` 菜单只显示可直接创建的 feature 类型
- `PropertyModifierEffect` 不再出现在能力菜单里
- feature 面板会自动隐藏 `[HideInInspector]` 字段
- `CharacterDataSO` 不再在 `OnValidate` 中合并属性，避免点击添加时出现数值翻倍

## 测试建议
优先测试：
1. 角色额外属性点击添加后是否仍会翻倍
2. 角色特殊能力是否能正常从下拉菜单创建
3. `[HideInInspector]` 标记的字段是否不会显示在 feature 面板里
4. 饰品特殊能力是否能正常创建并生效
5. 初始武器 / 初始饰品是否正常添加
