# UGUI 组件参考与 JSON 映射草案

## 1. 目标

这份文档的目标不是单纯罗列 UGUI API，而是为后续的 `AI -> JSON -> UGUI` 流程提供一套稳定、可约束、可扩展的组件词表与结构规则。

- 项目基线：Unity `2022.3.62f3c1`
- UGUI 包版本：`com.unity.ugui 1.0.0`
- 清单来源：
  - 本地包源码：`Library/PackageCache/com.unity.ugui@1.0.0/Runtime`
  - Unity 官方文档：`com.unity.ugui` 手册页与 Unity 2022.3 文档
- 本文中的“所有组件”指：
  - 对 `JSON -> UGUI` 真正有生成价值的可挂载组件
  - 与这些组件强绑定的宿主组件
  - 必须理解但不建议直接让 AI 输出的共享基类

## 2. 推荐总原则

- 让 AI 优先输出“语义控件”，不要直接输出完整底层层级。
- 用你的转换器把语义控件展开成 UGUI 规范层级。
- 把 `RectTransform` 独立成通用块，不要混在普通组件字段里。
- 把 `Selectable`、`Graphic`、`LayoutGroup` 的共通字段抽成共享 schema。
- 对 `Dropdown`、`InputField`、`Slider`、`ScrollRect` 这类结构型控件，优先使用模板或预制体，不要要求 AI 每次手写内部节点。
- 事件不要直接生成复杂 `UnityEvent` 序列化数据，优先输出 `actionId` 或业务命令名，再由运行时绑定。

## 3. 建议的 JSON 分层

推荐把 JSON 分成三层：

1. `widgetType`
   - 面向 AI 的语义类型，例如 `Panel`、`Button`、`ScrollView`、`InputField`。
2. `rectTransform`
   - 统一描述锚点、尺寸、偏移、旋转、缩放。
3. `components`
   - 低层组件覆盖层，只写和默认模板不同的字段。

推荐的节点结构：

```json
{
  "id": "main_menu_root",
  "name": "MainMenuRoot",
  "widgetType": "Panel",
  "active": true,
  "rectTransform": {
    "anchorMin": { "x": 0.5, "y": 0.5 },
    "anchorMax": { "x": 0.5, "y": 0.5 },
    "pivot": { "x": 0.5, "y": 0.5 },
    "anchoredPosition": { "x": 0, "y": 0 },
    "sizeDelta": { "x": 900, "y": 540 }
  },
  "components": [
    {
      "type": "Image",
      "sprite": "ui/panel/panel_bg",
      "imageType": "Sliced",
      "color": "#FFFFFFFF"
    }
  ],
  "children": []
}
```

## 4. 必备宿主组件

这些组件决定 UGUI 能不能工作，或者决定整个 UI 树的渲染与输入行为。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `RectTransform` | 所有 UGUI 节点的布局基础 | `anchorMin` `anchorMax` `pivot` `anchoredPosition` `sizeDelta` `offsetMin` `offsetMax` `localScale` `localRotation` | 所有 UI 节点必带。建议独立为 `rectTransform` 段 | [RectTransform](https://docs.unity3d.com/2022.3/Documentation/Manual/class-RectTransform.html) |
| `Canvas` | UI 根画布，决定渲染模式 | `renderMode` `sortingOrder` `pixelPerfect` `overrideSorting` `planeDistance` | 通常只出现在根节点或子画布根节点 | [Canvas](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Canvas.html) |
| `CanvasScaler` | 控制整棵 Canvas 的缩放规则 | `uiScaleMode` `referenceResolution` `screenMatchMode` `matchWidthOrHeight` `scaleFactor` `referencePixelsPerUnit` | 一般只挂在 Canvas 根节点 | [CanvasScaler](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html) |
| `GraphicRaycaster` | 让 Canvas 内 Graphic 可参与点击命中 | `ignoreReversedGraphics` `blockingObjects` `blockingMask` | 屏幕 UI 基本都需要；通常和 Canvas 同级 | [GraphicRaycaster](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-GraphicRaycaster.html) |
| `CanvasGroup` | 控制一组 UI 的透明度与交互可达性 | `alpha` `interactable` `blocksRaycasts` `ignoreParentGroups` | 适合窗口显隐、整组禁用、过渡动画 | [CanvasGroup](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CanvasGroup.html) |
| `CanvasRenderer` | 真正把 Graphic 提交给 Canvas 渲染 | 不建议暴露 | 大多由 Unity 自动管理，不建议 AI 主动生成 | [CanvasRenderer](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CanvasRenderer.html) |

## 5. 视觉组件

这些组件负责把图像、文字或裁剪效果画出来。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `Image` | 显示 `Sprite` 的最常用 UI 图形组件 | `sprite` `color` `material` `raycastTarget` `maskable` `imageType` `preserveAspect` `fillMethod` `fillOrigin` `fillAmount` `fillClockwise` `pixelsPerUnitMultiplier` | `imageType` 直接决定是否可做九宫格、平铺、填充条 | [Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html) |
| `RawImage` | 显示任意 `Texture` | `texture` `color` `material` `raycastTarget` `maskable` `uvRect` | 常用于摄像机图、RenderTexture、视频贴图。每个 `RawImage` 通常会多一次 draw call | [RawImage](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RawImage.html) |
| `Text` | 旧版 UGUI 文本组件 | `text` `font` `fontSize` `fontStyle` `lineSpacing` `richText` `alignment` `horizontalOverflow` `verticalOverflow` `bestFit` `color` `raycastTarget` `maskable` | 这是 Legacy 组件。项目里如果文本最终要稳定落地，建议后续把 `TextMeshProUGUI` 也纳入 schema | [Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html) |
| `Mask` | 使用 stencil 对子物体做裁剪 | `showMaskGraphic` | 要求本节点同时有可写 stencil 的 `Graphic`，适合复杂非矩形遮罩 | [Mask](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Mask.html) |
| `RectMask2D` | 基于矩形区域裁剪子物体 | `padding` `softness` | 只适合共面 2D UI，但更轻量，滚动列表优先用它 | [RectMask2D](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RectMask2D.html) |

### 视觉组件共通字段

以下字段建议抽成 `Graphic` 共享 schema，不必在每个具体组件里重复描述：

- `color`
- `material`
- `raycastTarget`
- `raycastPadding`
- `maskable`

对应的共享基类：

- `Graphic`
- `MaskableGraphic`

参考：

- [Graphic API](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Graphic.html)
- [MaskableGraphic API](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.MaskableGraphic.html)

## 6. 交互组件

这些组件是你后续最值得纳入“语义控件模板”的部分。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `Button` | 点击触发事件 | `transition` `targetGraphic` `colors` `spriteState` `animationTriggers` `interactable` `navigation` `onClick` | 一般与 `Image` 搭配；`onClick` 建议只输出 `actionId` | [Button](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Button.html) |
| `Toggle` | 开关或单选项 | `isOn` `group` `toggleTransition` `graphic` `transition` `targetGraphic` `interactable` `navigation` `onValueChanged` | 典型结构是背景图 + 勾选图 + 文本。做单选时配 `ToggleGroup` | [Toggle](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Toggle.html) |
| `ToggleGroup` | 一组 `Toggle` 的排他规则 | `allowSwitchOff` | 本身不画东西，通常挂在父节点 | [ToggleGroup](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ToggleGroup.html) |
| `Slider` | 线性范围值输入 | `minValue` `maxValue` `value` `wholeNumbers` `direction` `fillRect` `handleRect` `transition` `targetGraphic` `interactable` `navigation` `onValueChanged` | 建议走模板展开，不建议让 AI 手写内部层级 | [Slider](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html) |
| `Scrollbar` | 滚动条 | `direction` `value` `size` `numberOfSteps` `handleRect` `transition` `targetGraphic` `interactable` `navigation` `onValueChanged` | 常作为 `ScrollRect` 的从属组件 | [Scrollbar](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Scrollbar.html) |
| `ScrollRect` | 让内容区域可滚动 | `content` `viewport` `horizontal` `vertical` `movementType` `elasticity` `inertia` `decelerationRate` `scrollSensitivity` `horizontalScrollbar` `verticalScrollbar` `horizontalScrollbarVisibility` `verticalScrollbarVisibility` `horizontalScrollbarSpacing` `verticalScrollbarSpacing` `onValueChanged` | 自己不裁剪，通常和 `RectMask2D` 一起用 | [ScrollRect](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ScrollRect.html) |
| `InputField` | 旧版文本输入框 | `textComponent` `placeholder` `text` `contentType` `inputType` `characterValidation` `characterLimit` `lineType` `keyboardType` `caretBlinkRate` `caretWidth` `caretColor` `selectionColor` `readOnly` `shouldActivateOnSelect` `onValueChanged` `onEndEdit` `onSubmit` | Legacy 组件。中文项目长期建议改走 `TMP_InputField` | [InputField](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-InputField.html) |
| `Dropdown` | 旧版下拉菜单 | `template` `captionText` `captionImage` `itemText` `itemImage` `value` `options` `alphaFadeSpeed` `transition` `targetGraphic` `interactable` `navigation` `onValueChanged` | 强烈建议模板化。内部层级复杂，不适合让 AI 直接拼 | [Dropdown](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Dropdown.html) |
| `EventTrigger` | 给一个节点挂多种 UI 事件回调 | `triggers` | 适合少量特例。大量使用会让 JSON 变复杂，优先用具体控件事件 | [EventTrigger](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-EventTrigger.html) |

### `Selectable` 共享字段

`Button`、`Toggle`、`Slider`、`Scrollbar`、`Dropdown`、`InputField` 都继承 `Selectable`。这些字段建议统一抽出来：

- `interactable`
- `navigation`
- `transition`
- `targetGraphic`
- `colors`
- `spriteState`
- `animationTriggers`

参考：

- [Selectable API](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Selectable.html)

## 7. 布局组件

这些组件决定 UI 是否能被 AI 稳定生出来。和手工写坐标相比，布局组更适合 JSON 驱动。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `HorizontalLayoutGroup` | 横向排列子节点 | `padding` `spacing` `childAlignment` `childControlWidth` `childControlHeight` `childForceExpandWidth` `childForceExpandHeight` `childScaleWidth` `childScaleHeight` `reverseArrangement` | 适合按钮行、横向工具栏 | [HorizontalLayoutGroup](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-HorizontalLayoutGroup.html) |
| `VerticalLayoutGroup` | 纵向排列子节点 | `padding` `spacing` `childAlignment` `childControlWidth` `childControlHeight` `childForceExpandWidth` `childForceExpandHeight` `childScaleWidth` `childScaleHeight` `reverseArrangement` | 适合菜单、列表、设置面板 | [VerticalLayoutGroup](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-VerticalLayoutGroup.html) |
| `GridLayoutGroup` | 网格排列子节点 | `padding` `spacing` `childAlignment` `startCorner` `startAxis` `cellSize` `constraint` `constraintCount` | 适合背包格子、道具面板、技能格 | [GridLayoutGroup](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-GridLayoutGroup.html) |
| `ContentSizeFitter` | 让自身尺寸跟随内容 | `horizontalFit` `verticalFit` | 常配文本或布局组使用。要注意与父级布局组的驱动冲突 | [ContentSizeFitter](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ContentSizeFitter.html) |
| `AspectRatioFitter` | 维持宽高比 | `aspectMode` `aspectRatio` | 适合头像、封面图、视频画面 | [AspectRatioFitter](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-AspectRatioFitter.html) |
| `LayoutElement` | 覆盖布局尺寸偏好 | `minWidth` `minHeight` `preferredWidth` `preferredHeight` `flexibleWidth` `flexibleHeight` `layoutPriority` `ignoreLayout` | 是 JSON 驱动布局时非常关键的补丁组件 | [LayoutElement](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html) |

### 布局建议

- 能用布局组，就少让 AI 写绝对坐标。
- `LayoutElement` 比硬写 `sizeDelta` 更适合复杂列表。
- `ContentSizeFitter` 和父级 `LayoutGroup` 同时驱动同一维度时，容易出现抖动或警告，需要规则约束。
- 滚动列表优先组合：`ScrollRect + RectMask2D + VerticalLayoutGroup/GridLayoutGroup + ContentSizeFitter/LayoutElement`。

## 8. 特效组件

这些组件不是结构核心，但很适合做 JSON 的风格层。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `Shadow` | 阴影效果 | `effectColor` `effectDistance` `useGraphicAlpha` | 常用于文本和按钮 | [Shadow](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Shadow.html) |
| `Outline` | 描边效果 | `effectColor` `effectDistance` `useGraphicAlpha` | 继承自 `Shadow`，适合高对比文本 | [Outline](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Outline.html) |
| `PositionAsUV1` | 把顶点位置写入 `uv1` | 一般不建议暴露 | 多用于特殊 Shader，通常由技术美术或特定渲染方案驱动 | [PositionAsUV1](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-PositionAsUV1.html) |

## 9. 事件系统组件

这些组件负责把输入送到 UGUI。

| 组件 | 作用 | 推荐暴露给 JSON 的字段 | 依赖与说明 | 官方文档 |
| --- | --- | --- | --- | --- |
| `EventSystem` | UI 事件调度中心 | `firstSelected` `sendNavigationEvents` `dragThreshold` | 场景里通常只保留一个 | [EventSystem](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-EventSystem.html) |
| `StandaloneInputModule` | 旧输入系统下的键鼠手柄输入模块 | `horizontalAxis` `verticalAxis` `submitButton` `cancelButton` `inputActionsPerSecond` `repeatDelay` `forceModuleActive` | 你当前项目没有 `Input System` 包时，它就是默认方案 | [StandaloneInputModule](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-StandaloneInputModule.html) |
| `TouchInputModule` | 触屏输入模块 | `forceModuleActive` | 多数项目不需要 AI 直接生成，通常由场景模板决定 | [TouchInputModule](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-TouchInputModule.html) |
| `PhysicsRaycaster` | 让 3D 物体接入 EventSystem 射线 | `eventMask` `maxRayIntersections` | 更偏向场景物体交互，不是普通屏幕 UI 核心组件 | [PhysicsRaycaster](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-PhysicsRaycaster.html) |
| `Physics2DRaycaster` | 让 2D 物体接入 EventSystem 射线 | 通常不需要额外字段 | 适合 2D 场景对象交互，不是普通屏幕 UI 核心组件 | [Physics2DRaycaster](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Physics2DRaycaster.html) |

### 明确排除

`com.unity.ugui` 里还包含下面两个 UI Toolkit 桥接组件，但它们不属于本次 UGUI JSON 词表：

- `PanelEventHandler`
- `PanelRaycaster`

## 10. 不建议让 AI 直接输出的类型

下面这些类型应该由你的转换器内部处理，或者只作为共享 schema 存在：

- `Graphic`
- `MaskableGraphic`
- `Selectable`
- `LayoutGroup`
- `BaseInputModule`
- `PointerInputModule`
- `BaseRaycaster`
- `BaseMeshEffect`

原因：

- 这些类型是抽象基类、共享基类或低层基础设施。
- AI 直接输出它们容易得到“不完整但看起来像对的” JSON。
- 真正稳定的做法是让 AI 输出具体控件或语义模板。

## 11. 建议的语义控件模板

这是后续最值得做的一层。建议你的 AI 先输出这些 `widgetType`，再由转换器展开成标准 UGUI 层级。

| `widgetType` | 推荐展开为 | 说明 |
| --- | --- | --- |
| `CanvasRoot` | `Canvas + CanvasScaler + GraphicRaycaster` | 用于根画布 |
| `Panel` | `Image` 或纯空节点 + 可选布局组件 | 最常用容器 |
| `Label` | `Text` 或未来的 `TextMeshProUGUI` | 文本展示 |
| `Button` | `Image + Button + child Label` | 典型按钮模板 |
| `Toggle` | `Toggle + Background + Checkmark + child Label` | 适合勾选框、单选项 |
| `Slider` | `Slider + Background + FillArea/Fill + HandleSlideArea/Handle` | 强结构型控件 |
| `ScrollView` | `ScrollRect + Viewport(RectMask2D) + Content + optional Scrollbar` | 列表、背包、日志面板 |
| `InputField` | `InputField + Text + Placeholder` | 文本录入 |
| `Dropdown` | `Dropdown + Caption + Arrow + Template + Item` | 最好模板化 |
| `Spacer` | 仅 `LayoutElement` | 适合布局留白 |

## 12. 复杂控件为什么必须模板化

`Slider`、`ScrollRect`、`Dropdown`、`InputField` 的问题不在于组件本身，而在于它们依赖固定的内部层级和引用关系。

例如：

- `Slider` 需要 `fillRect` 和 `handleRect` 指向正确子节点。
- `ScrollRect` 需要 `content`、`viewport`、可选 scrollbar 的双向引用。
- `InputField` 需要 `textComponent`、`placeholder`、光标与选区配置。
- `Dropdown` 需要 `template`、`captionText`、`itemText`、`itemImage` 等内部节点引用。

所以更稳的策略是：

1. AI 只输出语义控件和少量字段。
2. 转换器选择对应模板。
3. 转换器在模板上覆写差异字段。
4. 转换器自动回填内部引用。

## 13. 推荐的组件字段命名规范

为了让 AI 更稳定，建议 JSON 字段尽量贴近 Unity 原名，尤其是枚举值：

- 组件名直接用 Unity 类名，例如 `Image`、`Button`、`ScrollRect`
- 枚举值直接用 Unity 枚举名，例如 `Sliced`、`Filled`、`MatchWidthOrHeight`
- 颜色统一用 `#RRGGBBAA`
- 二维向量统一用 `{ "x": 0, "y": 0 }`
- 四边距统一用 `{ "left": 0, "right": 0, "top": 0, "bottom": 0 }`
- 引用统一用节点 `id`，不要直接让 AI 写运行时对象引用

## 14. 推荐的最小可用 schema

如果你想先做一个能跑的版本，可以先只支持下面这些：

- 宿主层：
  - `Canvas`
  - `CanvasScaler`
  - `GraphicRaycaster`
  - `CanvasGroup`
  - `RectTransform`
- 视觉层：
  - `Image`
  - `Text`
- 交互层：
  - `Button`
- 布局层：
  - `VerticalLayoutGroup`
  - `HorizontalLayoutGroup`
  - `GridLayoutGroup`
  - `LayoutElement`
  - `ContentSizeFitter`

这样已经足够覆盖：

- 主菜单
- 设置面板
- 章节选择
- HUD 面板
- 简单弹窗
- 背包/技能列表

## 15. 推荐的第二阶段 schema

等第一阶段稳定后，再加这些结构型控件：

- `Toggle`
- `ToggleGroup`
- `Slider`
- `ScrollRect`
- `Scrollbar`
- `InputField`
- `Dropdown`
- `Mask`
- `RectMask2D`
- `AspectRatioFitter`
- `Shadow`
- `Outline`

## 16. 一个更适合 AI 的 Button JSON 示例

推荐让 AI 输出这种“半语义、半组件”的结构：

```json
{
  "id": "btn_start",
  "name": "StartButton",
  "widgetType": "Button",
  "rectTransform": {
    "anchorMin": { "x": 0.5, "y": 0.5 },
    "anchorMax": { "x": 0.5, "y": 0.5 },
    "pivot": { "x": 0.5, "y": 0.5 },
    "anchoredPosition": { "x": 0, "y": 60 },
    "sizeDelta": { "x": 320, "y": 72 }
  },
  "style": {
    "backgroundSprite": "ui/button/primary",
    "backgroundType": "Sliced",
    "backgroundColor": "#FFFFFFFF",
    "textColor": "#1A1A1AFF",
    "fontSize": 28
  },
  "events": [
    {
      "event": "onClick",
      "actionId": "start_game"
    }
  ]
}
```

转换器再把它展开成：

- 根节点 `Image + Button`
- 子节点 `Label`
- 自动回填 `targetGraphic`
- 自动应用默认 `ColorBlock`

## 17. 对你这个需求最关键的结论

如果目标是“让 AI 更稳定地生成 Unity UGUI”，真正有效的不是让 AI 直接生成完整 UGUI 层级，而是：

- 先限制 AI 只能输出有限的 `widgetType`
- 再限制 AI 只能写有限字段
- 再由你的映射层把语义 JSON 展开成标准 UGUI 结构
- 再用模板把复杂控件的内部引用自动补齐

这样能显著减少以下问题：

- 锚点和尺寸乱写
- 控件内部子节点缺失
- `ScrollRect` 和 `Dropdown` 引用断裂
- `Selectable` 过多无效字段
- 事件绑定不可维护

## 18. 后续最建议补的内容

如果你下一步继续做，我最建议补这三样：

1. 一份机器可读的 `widgetType -> 默认组件树` 映射表
2. 一份 `component type -> 可反序列化字段白名单`
3. 一套 `JSON -> GameObject/Component` 的运行时构建器或编辑器导入器

## 19. 参考入口

- [UGUI 包手册首页](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/index.html)
- [UGUI API 首页](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/index.html)
- [Canvas Manual](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Canvas.html)
- [RectTransform Manual](https://docs.unity3d.com/2022.3/Documentation/Manual/class-RectTransform.html)
- [CanvasGroup API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CanvasGroup.html)

