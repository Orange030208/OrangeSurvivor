# UI Runtime Motion V2 重构方案

## 目标

当前 UI Runtime Motion 的主要问题是使用链路过重、动作语义固定、Tween 生命周期只有单槽位、扩展新效果需要继承新组件。V2 的目标是把运行时动效改成“播放器 + 可配置片段 + 可组合轨道 + 播放通道”的结构，让普通 UI 和后续复杂卡牌表现都能复用同一套底座。

本次重构不改 `UIManager` 的页面栈和层级管理，不强制迁移现有 prefab。旧组件继续保留，V2 通过实现 `IUIRuntimeMotion` 与 `IUISequenceMotion` 接入现有页面编排。

## 设计边界

- 通用 Motion 层只负责“怎么动”：透明度、位置、缩放、旋转、颜色、填充、Sprite 序列、TMP 打字、事件回调、通道生命周期。
- 业务表现层负责“为什么动”：卡牌翻转、稀有度爆发、升级合成、奖励展示等应由专用 `CardEffectPlayer` 之类的上层组合 V2 Motion 完成。
- V2 使用 ScriptableObject 保存作者配置，不把运行时状态写入资产。
- V2 不依赖场景单例，也不主动查找全局系统。

## 核心模块

### UIMotionPlayer

挂在 UI 对象上的运行时播放器，职责：

- 持有一个 `UIMotionDefinition`。
- 缓存目标组件和命名目标。
- 按 clip id 播放、立即采样、停止通道、停止全部。
- 每个 channel 维护独立 tween，避免 hover、click、show 互相抢占。
- 实现 `IUIRuntimeMotion` 与 `IUISequenceMotion`，兼容现有 `UISequenceDirector`。

### UIMotionDefinition

ScriptableObject，一套可复用动效定义，例如 Button、Popup、Sidebar、Card。

包含多个 `UIMotionClipDefinition`。一个 Clip 表示一个动作，例如：

- `Show`
- `Hide`
- `Visible`
- `Hidden`
- `HoverIn`
- `HoverOut`
- `Press`
- `Release`
- `ClickPulse`
- `CardFlip`
- `RarityBurst`

### UIMotionClipDefinition

一个动作片段，包含：

- `ClipId`：字符串 id，避免 enum 修改成本。
- `Channel`：播放通道，例如 `Visibility`、`Interaction`、`Feedback`。
- `DurationScale`：片段级时间缩放。
- `PlayMode`：同一 clip 内轨道并行或串行。
- `ConflictPolicy`：播放时是否停止同 channel、停止所有 channel、或叠加播放。
- `Tracks`：多个 `UIMotionTrackDefinition`。

### UIMotionTrackDefinition

Track 是真正的扩展点。每个 Track 只控制一种表现能力：

- `UIAlphaMotionTrack`
- `UIMoveMotionTrack`
- `UIScaleMotionTrack`
- `UIRotateMotionTrack`
- `UIGraphicColorMotionTrack`
- `UIImageFillMotionTrack`
- `UISpriteSwapMotionTrack`
- `UISpriteSequenceMotionTrack`
- `UITMPTypewriterMotionTrack`
- `UICallbackMotionTrack`

Track 使用 `SerializeReference` 内嵌在 `UIMotionDefinition` 的 Clip 内，不需要每条轨道创建一个独立资产。后续复杂卡牌效果优先通过新增 Track 扩展，不修改 Player 主流程。

### UIMotionTargetRegistry

目标注册表，解决复杂 UI 不止一个 RectTransform 的问题。每个目标有稳定 key：

- `Self`
- `Root`
- `Front`
- `Back`
- `Frame`
- `Glow`
- `Title`
- `Description`

Track 通过 target key 找对象，避免在 Track 内自行 `GetComponent` 或硬编码层级路径。

### UIMotionTrigger

事件绑定组件，把 Unity UI 事件映射到 clip：

- Enable
- PointerEnter
- PointerExit
- PointerDown
- PointerUp
- PointerClick
- Select
- Deselect

普通按钮只需要 `UIMotionPlayer + UIMotionTrigger`。

## 播放通道规则

推荐内置通道：

- `Visibility`：Show、Hide、Visible、Hidden。
- `Interaction`：HoverIn、HoverOut、Press、Release。
- `Feedback`：ClickPulse、Emphasis、ErrorShake、RarityBurst。
- `Presentation`：卡牌翻转、词条展示、奖励展开等较长表现。

通道解决旧系统单 `currentTween` 的根问题。典型按钮播放规则：

```text
Visibility: Show / Hide
Interaction: HoverIn / HoverOut / Press / Release
Feedback: ClickPulse
```

点击时 `ClickPulse` 不应杀掉 `HoverIn`，关闭页面时 `Hide` 可以停止所有通道。

## Clip 与 Track 执行规则

- `Parallel`：所有 Track 同时开始，片段总时长为最长 Track。
- `Sequential`：Track 按顺序播放，适合翻牌中点换图、先移动后闪光。
- Track 自带 `StartDelay`、`Duration`、`Ease`、`TargetKey`。
- Track 返回 `Tween`，返回 null 代表立即完成。
- Player 负责统一收集、Kill、SetUpdate、错误上下文。

## 复杂卡牌效果接入方式

复杂卡牌不直接污染通用 Motion。推荐新增业务层：

```text
CardEffectPlayer
CardEffectDefinition
CardEffectContext
```

`CardEffectPlayer` 根据卡牌数据选择 Motion clip、目标颜色、Sprite、材质参数或事件回调。通用层提供可组合能力，上层决定业务语义。

示例：

```text
CardReveal
- Presentation channel
- Move Root from deck position
- Rotate Root / Front / Back
- Callback at middle: SwapFace
- MaterialFloat GlowIntensity
- TMPTypewriter Description
```

## 迁移策略

1. 新增 V2 核心代码，不删除旧 Runtime Motion。
2. `UIMotionPlayer` 实现旧接口，现有 `UISequenceDirector` 可直接引用它。
3. `UIMotionTrigger` 替代 `UIAutoMotionDriver` 的新 prefab 用法。
4. 旧 `UIRevealMotion`、`UIButtonMotion`、`UISidebarRevealMotion` 保留为 legacy。
5. 新 UI 或迁移过的 prefab 使用 V2；旧 prefab 可逐步替换。
6. 后续如需批量迁移，再写 Editor 工具把旧 preset 转为 V2 definition。

## 本次交付范围

本次先完成 V2 的可用闭环：

- `UIMotionPlayer`
- `UIMotionDefinition`
- `UIMotionClipDefinition`
- `UIMotionTargetRegistry`
- `UIMotionTrigger`
- 通道和 clip id 常量
- 基础 Track：Alpha、Move、Scale、Rotate、GraphicColor、ImageFill、SpriteSwap、Callback
- TMP 打字 Track
- Sprite 序列 Track
- Material Float / Material Color Track
- 与 `IUIRuntimeMotion` / `IUISequenceMotion` 兼容
- `UIMotionDefinition` Inspector：一键添加常用 Clip 和内嵌 Track
- `UIMotionPlayer` Inspector：运行时预览与立即采样

卡牌专用 `CardEffectPlayer`、批量 prefab 迁移工具不在本次第一轮强制完成，但 V2 结构必须能自然承载它们。
