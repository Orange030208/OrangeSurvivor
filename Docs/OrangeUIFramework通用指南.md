# OrangeUIFramework 通用框架指南

本文只描述 OrangeUIFramework 框架本身，不描述任何应用层功能、页面实例或资源清单。目标是让这份文档可以随框架迁移到其他 Unity 项目后继续作为维护手册和 AI 协作约束使用。

## 1. 框架定位

OrangeUIFramework 是一个面向 Unity UGUI 的运行时 UI 管理框架，用于统一托管以下 UI 类型：

- `Page`：主界面、HUD、菜单、结算页等主要界面。
- `Popup`：局部浮层、操作菜单、信息面板等轻量 UI。
- `Modal`：阻塞式弹窗，通常需要返回确认、取消或其他结果。
- `Tooltip`：瞬态提示，通常跟随鼠标、触摸点或锚点。
- `ViewPart`：页面内部子组件，不进入全局 UI 栈。

框架核心能力：

- 创建或复用 Root Canvas。
- 根据配置创建 UI 层级。
- 通过 Catalog 注册和实例化全局 View。
- 托管 View 打开、关闭、替换、重置、回收。
- 管理 PageStack、PopupStack、ModalStack 和当前 Tooltip。
- 管理输入焦点、射线阻挡、Modal 遮罩、Popup 外部点击关闭。
- 统一处理 Popup 和 Tooltip 的锚点定位、屏幕点定位、自动翻转和边缘裁剪。
- 支持 UniTask 异步流程。
- 支持 DOTween / UIMotion 进入退出动画。
- 支持对象池复用。
- 提供运行时诊断。

框架不负责：

- 具体业务规则。
- 自动生成应用层页面。
- 自动绑定复杂 Prefab 的所有 Inspector 引用。
- 为业务代码隐藏装配错误。
- 替代应用层状态机、数据层、存档、经济系统或输入逻辑。
- 在运行时隐式补齐业务依赖组件。

## 2. 依赖和适用范围

框架默认依赖：

- Unity UGUI。
- TextMeshPro，如果使用内置本地化文本组件。
- UniTask，用于异步打开、关闭、等待结果和取消。
- DOTween，如果使用 `UIMotionTransition`、`UIMotionPlayer` 或 Motion Track。

Canvas 支持：

- `ScreenSpaceOverlay`
- `ScreenSpaceCamera`

当前不支持：

- `WorldSpace` 作为 Root Canvas 模式。

推荐 Unity 版本：

- Unity 2022.3 LTS 或更高 LTS 版本。

## 3. 推荐目录结构

框架脚本推荐放置：

```text
Assets/Scripts/OrangeUIFramework/
  Core/
    Runtime/
    Data/
    Loading/
    Positioning/
  Motions/
    Runtime/
      UIMotion/
        V2/
          Tracks/
  Localization/
    Runtime/
    Data/
  Editor/
```

框架资产推荐按项目资源目录落地。当前 Survivors 项目使用：

```text
Assets/GameContent/UI/
  Data/
    OrangeUIFrameworkSettings.asset
    OrangeUIViewCatalog.asset
    OrangeCanvasProfile.asset
  Motion/
  Sprites/
  Fonts/
  Materials/
  Shaders/
```

UI Prefab 当前放置：

```text
Assets/GameContent/UI/Prefabs/
  Buttons/
  Container/
  Indicators/
  Item/
  Modals/
  Pages/
  Panel/
  Titles/
```

这些目录是当前工程约定，不是框架硬编码路径。框架运行时依赖 Inspector 中绑定的 `UIFrameworkSettings`、`ViewCatalog` 和 Prefab 引用；迁移或新增 UI 资源时，应优先保持在 `Assets/GameContent/UI/` 下按资源类型归档。

## 4. 命名空间和命名规则

框架命名空间：

```csharp
Orange.UIFramework
```

推荐类名：

- `UIManager`
- `IUIManager`
- `ViewBase`
- `PageBase`
- `PopupBase`
- `ModalBase<TResult>`
- `TooltipBase`
- `ViewPartBase`
- `ViewHandle`
- `OpenContext`
- `CloseReason`
- `ModalResult<TResult>`
- `UIFrameworkSettings`
- `CanvasProfile`
- `ViewCatalog`
- `ViewDefinition`
- `LayerDefinition`
- `FloatingViewPositioner`
- `UIMotionPlayer`
- `UIMotionTransition`

不推荐：

- 为每个项目重新命名一套 `AppUIManager`、`ProjectUIService`、`CustomPageBase`。
- 新增与 `UIManager` 并行的全局 UI 栈管理器。
- 把框架类名写成带项目名的变体。

## 5. 核心类型

### 5.1 UIManager

`UIManager` 是框架唯一运行时总入口，负责：

- 校验设置和 Catalog。
- 创建或复用 Root Canvas。
- 创建 LayerRoot。
- 创建 Modal 遮罩。
- 创建 Popup 外部点击拦截器。
- 加载 View Prefab。
- 创建 `ViewHandle`。
- 调用 View 生命周期。
- 管理打开状态、栈、对象池和诊断。

所有全局 UI 打开和关闭都应通过 `UIManager` 或 `IUIManager`。

### 5.2 IUIManager

`IUIManager` 是业务侧可依赖的最小接口。典型 API：

```csharp
UniTask<ViewHandle<TPage>> OpenPageAsync<TPage>(
    object payload = null,
    CancellationToken cancellationToken = default)
    where TPage : PageBase;

UniTask<ViewHandle<TPage>> ReplacePageAsync<TPage>(
    object payload = null,
    CancellationToken cancellationToken = default)
    where TPage : PageBase;

UniTask<ViewHandle<TPage>> ResetToPageAsync<TPage>(
    object payload = null,
    CancellationToken cancellationToken = default)
    where TPage : PageBase;

UniTask CloseTopPageAsync(CancellationToken cancellationToken = default);
UniTask CloseAllPagesAsync(CancellationToken cancellationToken = default);

UniTask<bool> ClosePageAsync<TPage>(
    CancellationToken cancellationToken = default)
    where TPage : PageBase;

UniTask<ViewHandle<TPopup>> ShowPopupAsync<TPopup>(
    object payload = null,
    PopupOptions options = default,
    CancellationToken cancellationToken = default)
    where TPopup : PopupBase;

UniTask<ModalResult<TResult>> ShowModalAsync<TModal, TResult>(
    object payload = null,
    CancellationToken cancellationToken = default)
    where TModal : ModalBase<TResult>;

UniTask<ViewHandle<TTooltip>> ShowTooltipAsync<TTooltip>(
    object payload,
    TooltipOptions options,
    CancellationToken cancellationToken = default)
    where TTooltip : TooltipBase;

void UpdateTooltipPosition(Vector2 screenPosition);
void HideTooltip();
bool IsOpen<TView>() where TView : ViewBase;
```

### 5.3 ViewBase

`ViewBase` 是所有全局 View 的抽象基类。它负责：

- 保存 `ViewHandle`。
- 保存当前生命周期状态。
- 管理根 `CanvasGroup`。
- 管理输入状态。
- 调用进入和退出动画。
- 暴露可重写生命周期钩子。

所有 `ViewBase` 派生类的 Prefab 根节点必须有 `CanvasGroup`。

### 5.4 PageBase

`PageBase` 用于主要界面。Page 会进入 PageStack，由 `UIManager` 统一打开、替换、重置和关闭。

适合：

- 主菜单。
- 设置页。
- HUD。
- 结算页。
- 列表页。
- 详情页。

### 5.5 PopupBase

`PopupBase` 用于轻量浮层。Popup 会进入 PopupStack，可锚定目标，可按组互斥，可点击外部关闭。

适合：

- 局部菜单。
- 物品或条目的操作面板。
- 局部说明浮层。
- 非阻塞选择面板。

### 5.6 ModalBase<TResult>

`ModalBase<TResult>` 用于阻塞式弹窗，并通过 `ModalResult<TResult>` 返回结果。

Modal 内部通过以下方法完成结果：

```csharp
SetResult(value);
Cancel(CloseReason.Cancel);
```

外部通过 `ShowModalAsync<TModal, TResult>()` 等待结果。

### 5.7 TooltipBase

`TooltipBase` 用于瞬态提示。当前模型通常是同一时间只保留一个 Tooltip，新 Tooltip 打开前会关闭旧 Tooltip。

适合：

- 鼠标悬停提示。
- 长按提示。
- 跟随指针的说明面板。

### 5.8 ViewPartBase

`ViewPartBase` 用于页面内部子组件。它不进入 Catalog，也不由 `UIManager` 全局打开。

适合：

- 页面内部列表。
- 页面内部面板。
- 可复用卡片。
- 可复用按钮组。
- 页面内部复杂表现控制器。

规则：

- `ViewPartBase` 由 Page、Popup 或 Modal 直接持有。
- 不注册到 `ViewCatalog`。
- 如需打开 Popup、Tooltip 或 Modal，应由拥有者显式传入 `UIManager` 或 `IUIManager`。

## 6. View 类型枚举

### 6.1 ViewKind

```csharp
public enum ViewKind
{
    Page,
    Popup,
    Modal,
    Tooltip,
    Part
}
```

说明：

- `Page`：全局主界面，进入 PageStack。
- `Popup`：全局浮层，进入 PopupStack。
- `Modal`：全局阻塞弹窗，进入 ModalStack。
- `Tooltip`：全局提示，通常唯一。
- `Part`：内部组件，不允许注册到 Catalog。

### 6.2 ViewLayer

```csharp
public enum ViewLayer
{
    Background,
    Hud,
    Page,
    Popup,
    ModalMask,
    Modal,
    Tooltip,
    System,
    Debug
}
```

推荐层级：

| Layer | 推荐用途 | 推荐 SortingOrder |
| --- | --- | ---: |
| `Background` | UI 背景层 | -300 |
| `Hud` | HUD 和常驻状态层 | -100 |
| `Page` | 常规页面 | 0 |
| `Popup` | 浮层和非阻塞面板 | 200 |
| `ModalMask` | Modal 遮罩 | 300 |
| `Modal` | Modal 内容 | 320 |
| `Tooltip` | Tooltip | 500 |
| `System` | 系统级 UI | 700 |
| `Debug` | 调试 UI | 900 |

实际层级由 `UIFrameworkSettings.Layers` 决定。

### 6.3 ViewRuntimePhase

```csharp
public enum ViewRuntimePhase
{
    None,
    Loading,
    Loaded,
    Opening,
    Opened,
    Closing,
    Closed,
    Recycled,
    Failed
}
```

典型生命周期：

```text
CreateRuntimeViewAsync
  -> Initialize
  -> Loaded
  -> OpenInternalAsync
  -> Opening
  -> OnOpeningAsync
  -> PlayEnterTransitionAsync
  -> Opened
  -> OnOpenedAsync
  -> CloseInternalAsync
  -> Closing
  -> OnClosingAsync
  -> PlayExitTransitionAsync
  -> OnClosed
  -> Closed
  -> Recycled 或 Destroy
```

### 6.4 CloseReason

```csharp
public enum CloseReason
{
    Normal,
    Replace,
    Reset,
    Back,
    OutsideClick,
    Cancel,
    Completed,
    Destroyed
}
```

建议语义：

- `Normal`：普通关闭。
- `Replace`：被另一个 View 替换。
- `Reset`：全局重置或栈重置。
- `Back`：返回动作触发。
- `OutsideClick`：点击外部区域关闭。
- `Cancel`：取消或外部取消令牌触发。
- `Completed`：流程正常完成。
- `Destroyed`：对象或场景销毁。

## 7. 配置资产

### 7.1 UIFrameworkSettings

`UIFrameworkSettings` 是框架总配置，建议作为 ScriptableObject 资产保存。

关键字段：

- `instanceIdPrefix`：运行时实例 Id 前缀。
- `useUnscaledTime`：UI Tick 和动画是否使用非缩放时间。
- `rootName`：Root Canvas GameObject 名称。
- `canvasProfile`：Canvas 配置。
- `enablePooling`：是否开启对象池。
- `maxCachedInstancesPerView`：每种 View 默认最大缓存数量。
- `layers`：层级定义列表。
- `popupOutsideClickBlocker`：Popup 外部点击拦截器配置。

校验重点：

- `instanceIdPrefix` 不能为空。
- `canvasProfile` 必须存在。
- `layers` 必须存在且不能重复。
- Popup 外部点击拦截器 Prefab 如存在，必须可接收点击。

### 7.2 CanvasProfile

`CanvasProfile` 定义 Root Canvas 行为。

关键字段：

- `renderMode`
- `uiCamera`
- `planeDistance`
- `rootSortingOrder`
- `referenceResolution`
- `matchWidthOrHeight`

规则：

- `ScreenSpaceOverlay` 时 `worldCamera = null`。
- `ScreenSpaceCamera` 时必须绑定 `uiCamera`。
- `WorldSpace` 当前不支持。
- `referenceResolution` 必须大于 0。
- `planeDistance` 必须大于 0。

### 7.3 ViewCatalog

`ViewCatalog` 是全局 View 注册表。

每个可由 `UIManager` 打开的 `Page`、`Popup`、`Modal`、`Tooltip` 都必须注册。

不要注册：

- `ViewPartBase`。
- 普通 MonoBehaviour。
- 页面内部子节点。
- 不由 `UIManager` 打开的 Prefab。

### 7.4 ViewDefinition

`ViewDefinition` 表示一个全局 View 配置。

关键字段：

- `id`：View 唯一标识，建议 `page.xxx`、`popup.xxx`、`modal.xxx`、`tooltip.xxx`。
- `kind`：View 类型。
- `layer`：实例化到哪个层。
- `prefab`：View Prefab。
- `singleton`：同类型是否只保留一个打开实例。
- `cacheOnClose`：关闭后是否进入对象池。
- `trackInBackStack`：返回栈策略字段，使用前应确认当前框架是否已接入完整 Back 输入逻辑。
- `closeOnBackgroundClick`：通常用于 Modal 遮罩点击关闭。
- `warmupCount`：预热数量字段，使用前应确认当前 `UIManager` 是否已实现预热。
- `maxCachedInstancesOverride`：该 View 的缓存上限覆盖值。
- `allowDuplicateViewType`：是否允许同一 View 类型重复注册。

校验规则：

- `id` 不为空且不重复。
- `prefab` 不为空。
- `kind` 不能是 `Part`。
- Prefab 根节点必须有 `ViewBase`。
- `kind` 必须匹配根组件基类。
- 默认不允许同一个 View 类型注册多次。

### 7.5 LayerDefinition

`LayerDefinition` 定义一个 UI 层。

关键字段：

- `layer`
- `rootName`
- `sortingOrder`
- `blocksRaycasts`

`UIManager` 会为每个 LayerRoot 添加或复用：

- `RectTransform`
- `Canvas`
- `GraphicRaycaster`

并设置：

```text
Canvas.overrideSorting = true
Canvas.sortingOrder = LayerDefinition.SortingOrder
GraphicRaycaster.enabled = LayerDefinition.BlocksRaycasts
```

## 8. UIManager 初始化

`UIManager.Awake()` 通常执行：

1. 建立单例引用。
2. 调用 `Initialize()`。
3. 校验 `UIFrameworkSettings` 和 `ViewCatalog`。
4. 创建或复用 Root Canvas。
5. 应用 `CanvasProfile`。
6. 创建 `Layers` 根节点。
7. 创建所有 LayerRoot。
8. 创建 Modal 遮罩。
9. 创建 Popup 外部点击拦截器。
10. 创建默认 `IViewLoader`。
11. 创建默认 `IFloatingViewPositioner`。

Root Canvas 可由场景显式提供，也可由 `UIManager` 运行时创建。

如果提供场景 Canvas，框架仍会应用 `CanvasProfile` 并创建缺失的层级节点。

## 9. Page 管理

### 9.1 OpenPageAsync

```csharp
await uiManager.OpenPageAsync<MyPage>(payload, cancellationToken);
```

行为：

- 解析 Catalog 中的 `MyPage` 定义。
- 创建或复用实例。
- 调用 View 打开流程。
- 加入 PageStack。
- 刷新输入状态。

如果 `singleton = true` 且该页面已打开：

- 若正在关闭，等待关闭完成后继续。
- 否则移动到 PageStack 顶部并刷新输入。

### 9.2 ReplacePageAsync

```csharp
await uiManager.ReplacePageAsync<MyPage>(payload, cancellationToken);
```

行为：

- 关闭当前顶部 Page，原因 `CloseReason.Replace`。
- 打开新 Page。
- 使用 request version 避免旧流程覆盖新请求。

### 9.3 ResetToPageAsync

```csharp
await uiManager.ResetToPageAsync<MyPage>(payload, cancellationToken);
```

行为：

- 关闭所有 Page，原因 `CloseReason.Reset`。
- 打开指定 Page。
- 使用 request version 避免旧流程覆盖新请求。

### 9.4 ClosePageAsync

```csharp
bool closed = await uiManager.ClosePageAsync<MyPage>(cancellationToken);
```

行为：

- 从 PageStack 顶部向下查找指定类型。
- 找到后关闭，返回 `true`。
- 未找到返回 `false`。

### 9.5 CloseTopPageAsync

```csharp
await uiManager.CloseTopPageAsync(cancellationToken);
```

行为：

- 关闭 PageStack 顶部页面。
- 关闭原因 `CloseReason.Back`。

## 10. Popup 管理

### 10.1 ShowPopupAsync

```csharp
PopupOptions options = new PopupOptions(
    anchor: anchorRect,
    closeOnOutsideClick: true,
    groupId: "item.actions",
    replaceSameGroup: true,
    preferredAnchor: FloatingViewAnchor.BottomRight);

ViewHandle<MyPopup> handle = await uiManager.ShowPopupAsync<MyPopup>(
    payload,
    options,
    cancellationToken);
```

行为：

1. 解析 Catalog 中的 Popup 定义。
2. 如配置了同组替换，先关闭同组 Popup。
3. 创建或复用 Popup。
4. 调用打开流程。
5. 保存 `PopupOptions`。
6. 加入 PopupStack。
7. 移到层级顶部。
8. 调用定位器定位。
9. 刷新输入和外部点击拦截器。

### 10.2 PopupOptions

```csharp
public readonly struct PopupOptions
{
    public RectTransform Anchor { get; }
    public Vector2 ScreenPosition { get; }
    public Vector2 Offset { get; }
    public bool CloseOnOutsideClick { get; }
    public string GroupId { get; }
    public bool ReplaceSameGroup { get; }
    public bool TrackInStack { get; }
    public float Margin { get; }
    public FloatingViewAnchor PreferredAnchor { get; }
}
```

字段说明：

- `Anchor`：锚定某个 `RectTransform` 中心点。
- `ScreenPosition`：使用屏幕坐标作为定位原点。
- `Offset`：定位偏移。
- `CloseOnOutsideClick`：点击外部是否关闭。
- `GroupId`：Popup 分组。
- `ReplaceSameGroup`：打开前关闭同组 Popup。
- `TrackInStack`：是否参与 Popup 栈语义，具体 Back 行为取决于框架实现。
- `Margin`：边缘裁剪安全距离。
- `PreferredAnchor`：优先展开方向。

如果需要把 `(0,0)` 作为有效屏幕坐标，构造时应设置 `useScreenPosition: true`。

### 10.3 Popup 使用建议

- 同类互斥浮层设置相同 `GroupId`。
- 需要外部点击关闭时使用 `CloseOnOutsideClick = true`。
- 需要手动关闭时保存 `ViewHandle`。
- 不要在 Popup 内自己创建全屏透明遮罩，优先使用框架外部点击拦截器。
- 不要在业务代码中重复实现屏幕裁剪、翻转和坐标换算。

## 11. Modal 管理

### 11.1 ShowModalAsync

```csharp
ModalResult<bool> result = await uiManager.ShowModalAsync<MyConfirmModal, bool>(
    payload,
    cancellationToken);

if (result.Confirmed && result.Value)
{
    // confirmed
}
```

行为：

1. 解析 Catalog 中的 Modal 定义。
2. 创建或复用 Modal。
3. 调用打开流程。
4. 加入 ModalStack。
5. 激活 ModalMask。
6. 禁用下层 Page 和 Popup 输入。
7. 等待 Modal 内部完成结果。
8. 根据结果关闭 Modal。
9. 返回 `ModalResult<TResult>`。

### 11.2 ModalResult

```csharp
public readonly struct ModalResult<TResult>
{
    public bool Confirmed { get; }
    public TResult Value { get; }
    public CloseReason CloseReason { get; }
    public bool Cancelled => !Confirmed;
}
```

规则：

- `Confirmed = true` 时才读取 `Value`。
- 取消时 `Value` 是默认值。
- 外部取消令牌触发时，框架应补全取消结果并关闭 Modal。

### 11.3 Modal 内部写法

```csharp
public sealed class MyConfirmModal : ModalBase<bool>
{
    private void OnConfirmClicked()
    {
        SetResult(true);
    }

    private void OnCancelClicked()
    {
        Cancel(CloseReason.Cancel);
    }
}
```

不要让外部直接订阅 Modal 按钮事件来判断结果。统一使用 `ModalResult<TResult>`。

## 12. Tooltip 管理

### 12.1 ShowTooltipAsync

```csharp
TooltipOptions options = new TooltipOptions(
    screenPosition: pointerPosition,
    offset: new Vector2(18f, -18f),
    followPointer: true,
    margin: 12f,
    preferredAnchor: FloatingViewAnchor.BottomRight,
    useScreenPosition: true);

ViewHandle<MyTooltip> handle = await uiManager.ShowTooltipAsync<MyTooltip>(
    payload,
    options,
    cancellationToken);
```

行为：

- 打开新 Tooltip 前关闭旧 Tooltip。
- 打开并定位 Tooltip。
- Tooltip 默认不接收输入、不阻挡射线。

### 12.2 更新和隐藏

```csharp
uiManager.UpdateTooltipPosition(pointerPosition);
uiManager.HideTooltip();
```

`UpdateTooltipPosition` 只有当前 Tooltip 配置 `FollowPointer = true` 时才会生效。

### 12.3 TooltipOptions

```csharp
public readonly struct TooltipOptions
{
    public RectTransform Anchor { get; }
    public Vector2 ScreenPosition { get; }
    public Vector2 Offset { get; }
    public bool FollowPointer { get; }
    public float Margin { get; }
    public FloatingViewAnchor PreferredAnchor { get; }
}
```

建议：

- Tooltip 数据源使用接口或轻量 payload。
- 动态内容应在打开时刷新布局。
- Tooltip 不应承担交互输入。

## 13. View 生命周期

### 13.1 生命周期钩子

```csharp
protected override void Awake()
{
    base.Awake();
    ResolveReferences();
    ValidateConfiguration();
}

protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
{
    return UniTask.CompletedTask;
}

protected override UniTask OnOpenedAsync(CancellationToken cancellationToken)
{
    return UniTask.CompletedTask;
}

protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
{
    return UniTask.CompletedTask;
}

protected override void OnClosed(CloseReason reason)
{
}

protected override void OnInputChanged(bool interactable, bool blocksRaycasts)
{
}

public override bool RequiresTick => true;

protected override void OnTick(float deltaTime)
{
}
```

### 13.2 生命周期规则

- `Awake` 中必须调用 `base.Awake()`。
- `Awake` 适合解析和校验 Inspector 引用。
- `OnOpeningAsync` 适合读取 payload、绑定事件、刷新显示。
- `OnOpenedAsync` 适合设置默认焦点或播放打开完成后的逻辑。
- `OnClosingAsync` 适合等待内部关闭动画或保存状态。
- `OnClosed` 必须解绑事件、清空外部引用、重置会话状态。
- `OnTick` 只有 `RequiresTick = true` 时才会被 `UIManager.Update` 调用。
- 不要直接调用 `OpenInternalAsync` 或 `CloseInternalAsync`。

### 13.3 OpenContext

`OpenContext` 包含：

- `ViewType`
- `ViewId`
- `InstanceId`
- `Kind`
- `Payload`
- `RequestVersion`

读取 payload：

```csharp
MyPageContext pageContext = context.GetPayload<MyPageContext>()
    ?? throw new InvalidOperationException($"{nameof(MyPage)} requires {nameof(MyPageContext)} payload.");
```

如果 payload 可选，应显式处理 null。

### 13.4 ViewHandle

`ViewHandle<TView>` 包含：

- `InstanceId`
- `ViewId`
- `Kind`
- `View`
- `Owner`
- `ClosedTask`
- `IsValid`
- `CloseAsync(...)`

使用建议：

- 外部需要主动关闭 Popup 或 Tooltip 时保存 Handle。
- 使用 `handle.CloseAsync(reason)` 关闭。
- 不要直接销毁 `handle.View.gameObject`。
- View 关闭后不要长期保存旧 View 引用，对象池可能复用该实例。

## 14. 输入和射线规则

`UIManager` 刷新输入状态时通常遵循：

- 没有 Modal 时，PageStack 顶部 Page 可输入。
- 没有 Modal 时，PopupStack 顶部 Popup 可输入。
- 有 Modal 时，只有 ModalStack 顶部 Modal 可输入。
- Tooltip 不可输入、不阻挡射线。
- Modal 打开后，下层 Page 和 Popup 输入关闭。

注意：

- Page 和 Popup 可以同时处于输入可用状态，具体取决于是否存在 Modal。
- Popup 外部点击拦截器激活时位于 Popup 所在层，并保持在顶层 Popup 下方。
- Tooltip 层通常不需要 GraphicRaycaster。

## 15. ModalMask

`UIManager` 会在 `ModalMask` 层创建遮罩。

典型结构：

```text
ModalMaskLayer
  ModalMask
```

ModalMask 通常包含：

- 全屏 `RectTransform`。
- 半透明 `Image`。
- `Button` 或等效点击组件。

行为：

- ModalStack 非空时显示。
- ModalStack 为空时隐藏。
- 点击遮罩时，如果顶部 Modal 的 `CloseOnBackgroundClick = true`，关闭顶部 Modal。

## 16. Popup 外部点击拦截器

Popup 外部点击拦截器用于实现“点击 Popup 外部关闭”。

默认行为：

- 在 Popup 层创建透明全屏 `Image + Button`。
- 当顶部 Popup 允许外部点击关闭且没有 Modal 时激活。
- 点击后关闭顶部 Popup，关闭原因 `CloseReason.OutsideClick`。

如果使用自定义 Prefab，Prefab 必须满足：

- 根或子层级中存在启用的 `Graphic`。
- 该 `Graphic.raycastTarget = true`。
- 同对象或父对象存在启用的 `Button`。

否则配置校验应报错。

## 17. Popup 和 Tooltip 定位

定位器：

```csharp
FloatingViewPositioner
```

接口：

```csharp
IFloatingViewPositioner
```

定位支持：

- `RectTransform` 锚点。
- 屏幕坐标。
- 偏移。
- 边距。
- 首选方向。
- 自动翻转。
- 边缘裁剪。
- Overlay / Camera Canvas 相机选择。

锚点枚举：

```csharp
public enum FloatingViewAnchor
{
    BottomRight,
    TopRight,
    BottomLeft,
    TopLeft,
    Center
}
```

定位流程：

1. 解析定位原点。
2. 计算 LayerRoot 本地坐标。
3. 根据首选锚点生成候选方向。
4. 尝试找到完全位于边界内的方向。
5. 如果没有完全合适的方向，使用首选方向并 Clamp。
6. 写入 `RectTransform.anchoredPosition`。
7. 返回 `FloatingViewPlacement` 供诊断使用。

规则：

- Popup 和 Tooltip 不应各自实现坐标换算。
- 不要在打开后立即手写覆盖位置，除非明确绕开框架定位。
- 动态尺寸 UI 应在定位前完成布局刷新。
- Camera Canvas 下必须使用 Root Canvas 的 `worldCamera` 做换算。

## 18. 对象池

对象池按 View 类型缓存：

```text
Dictionary<Type, Queue<ViewBase>>
```

关闭时，如果满足以下条件则进入池：

- 全局 `EnablePooling = true`。
- `ViewDefinition.CacheOnClose = true`。
- 当前类型池数量未达到上限。

缓存上限：

- 优先使用 `ViewDefinition.MaxCachedInstancesOverride`。
- 否则使用 `UIFrameworkSettings.MaxCachedInstancesPerView`。

进入池时：

- 标记为 `Recycled`。
- 关闭 GameObject。
- 从运行时追踪字典移除。

复用时：

- 从池取出实例。
- 重新设置父节点。
- 重新生成 `ViewHandle`。
- 再次调用 `Initialize`。
- 再次执行打开流程。

池化约束：

- `Awake` 不会在复用时再次执行。
- 每次打开绑定的事件必须在关闭时解绑。
- 每次打开应重新应用 payload 和显示状态。
- 关闭时应清空外部引用。
- 动画默认快照应在打开前后按需要刷新。

## 19. 加载器

框架通过 `IViewLoader` 加载 View。

默认实现通常是同步 Prefab 实例化：

```csharp
public interface IViewLoader
{
    UniTask<ViewBase> LoadAsync(
        ViewDefinition definition,
        Transform parent,
        CancellationToken cancellationToken);

    void Release(ViewBase view, ViewDefinition definition);
}
```

如果项目需要 Addressables、AssetBundle 或自定义资源系统，应新增 `IViewLoader` 实现，而不是修改 `ViewBase` 或业务 View 生命周期。

## 20. 动画系统

### 20.1 入口类型

常用类型：

- `IViewTransition`
- `UIMotionTransition`
- `IUIRuntimeMotion`
- `IUISequenceMotion`
- `UIMotionPlayer`
- `UIMotionDefinition`
- `UIMotionClipDefinition`
- `UIMotionTrackDefinition`
- `UISequenceDirector`

`ViewBase` 通过 `IViewTransition` 播放进入和退出动画，不直接依赖 DOTween。

### 20.2 UIMotionTransition

`UIMotionTransition` 是 `ViewBase` 与 Motion 系统之间的适配器。

进入时通常执行：

1. `RefreshDefaults()`
2. `SetHiddenImmediate()`
3. `PlayEnter()`
4. 等待 Tween 完成

退出时通常执行：

1. `PlayExit()`
2. 等待 Tween 完成

如果没有 Tween，可按配置立即显示或跳过。

### 20.3 UIMotionPlayer

`UIMotionPlayer` 负责：

- 读取 `UIMotionDefinition`。
- 根据 ClipId 查找 Clip。
- 根据 TargetKey 查找 Transform。
- 捕获 Initial 快照。
- 创建 DOTween Sequence。
- 按 Channel 管理冲突。

常用 ClipId：

```text
Show
Hide
Visible
Hidden
HoverIn
HoverOut
Press
Release
ClickPulse
Emphasis
```

常用 Channel：

```text
Visibility
Interaction
Feedback
Presentation
```

常用 TargetKey：

```text
Self
Root
Front
Back
Frame
Glow
Title
Description
```

冲突策略：

- `StopSameChannel`：停止同 Channel 旧动画。
- `StopAllChannels`：停止当前 Player 所有动画。
- `AllowParallel`：允许并行，只有确认不会写同一属性时使用。

### 20.4 LayoutGroup 协作规则

如果 UI 元素受 `LayoutGroup`、`ContentSizeFitter` 或动态布局控制，动画不应直接修改布局根节点的位置或尺寸。

推荐结构：

```text
LayoutGroup
  ItemRoot        <- 布局占位
    VisualRoot    <- 动画目标
      Background
      Icon
      Text
```

推荐绑定：

```text
Key: Self
Target: VisualRoot
```

这样 Motion 资产可以继续使用 `targetKey = Self`，实际运动作用到视觉节点。

## 21. 本地化

框架提供轻量本地化模块：

- `LocalizationService`
- `ILocalizationService`
- `LocalizationTable`
- `LocalizationEntry`
- `LocalizedText`

`LocalizationService`：

- 维护当前语言。
- 按语言查找 `LocalizationTable`。
- 找不到 key 时返回 key。
- 支持简单 `{arg}` 占位替换。
- 语言变化时触发 `LanguageChanged`。

`LocalizedText`：

- 要求同对象有 `TMP_Text`。
- 启用时绑定服务。
- 语言变化时自动刷新。
- 支持运行时修改 key 和参数。

本地化模块是可选能力。项目也可以接入 Unity Localization 或其他本地化系统，但应通过接口或适配层隔离。

## 22. 新增 Page 流程

1. 新增脚本继承 `PageBase`。
2. 创建 Page Prefab。
3. Prefab 根节点添加：
   - `RectTransform`
   - `CanvasGroup`
   - Page 脚本
4. 如需动画，配置 `UIMotionPlayer` / `UISequenceDirector` 和 `UIMotionTransition`。
5. 在 `Awake` 中调用 `base.Awake()`、解析引用、校验引用。
6. 在 `OnOpeningAsync` 中读取 payload、绑定事件、刷新显示。
7. 在 `OnClosed` 中解绑事件、清空状态。
8. 在 `ViewCatalog` 注册：
   - `id = page.xxx`
   - `kind = Page`
   - `layer = Page` 或其他明确层
   - `prefab = Page Prefab`
9. 通过 `OpenPageAsync<TPage>` 打开。

模板：

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MyPage : PageBase
{
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        MyPageContext pageContext = context.GetPayload<MyPageContext>()
            ?? throw new InvalidOperationException($"{nameof(MyPage)} requires {nameof(MyPageContext)} payload.");

        closeButton.onClick.AddListener(OnCloseClicked);
        Apply(pageContext);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void OnCloseClicked()
    {
        Handle.CloseAsync(CloseReason.Normal).Forget();
    }

    private void Apply(MyPageContext context)
    {
    }

    private void ValidateConfiguration()
    {
        if (closeButton == null)
        {
            throw new MissingReferenceException($"{nameof(MyPage)} '{name}' is missing close button.");
        }
    }
}
```

## 23. 新增 Popup 流程

1. 新增脚本继承 `PopupBase`。
2. 创建 Popup Prefab。
3. Prefab 根节点添加 `CanvasGroup` 和 Popup 脚本。
4. 在 `OnOpeningAsync` 中读取 payload 并刷新显示。
5. 在 `OnClosed` 中解绑事件和清空状态。
6. 在 `ViewCatalog` 注册：
   - `id = popup.xxx`
   - `kind = Popup`
   - `layer = Popup`
7. 通过 `ShowPopupAsync<TPopup>` 打开。

模板：

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;

public sealed class MyPopup : PopupBase
{
    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        MyPopupPayload payload = context.GetPayload<MyPopupPayload>()
            ?? throw new InvalidOperationException($"{nameof(MyPopup)} requires {nameof(MyPopupPayload)} payload.");

        Apply(payload);
        return UniTask.CompletedTask;
    }

    private void Apply(MyPopupPayload payload)
    {
    }
}
```

打开：

```csharp
PopupOptions options = new PopupOptions(
    anchor: anchorRect,
    closeOnOutsideClick: true,
    groupId: "my.popup",
    replaceSameGroup: true);

ViewHandle<MyPopup> handle = await uiManager.ShowPopupAsync<MyPopup>(
    payload,
    options,
    cancellationToken);
```

## 24. 新增 Modal 流程

1. 新增脚本继承 `ModalBase<TResult>`。
2. 创建 Modal Prefab。
3. Prefab 根节点添加 `CanvasGroup` 和 Modal 脚本。
4. Confirm 路径调用 `SetResult(value)`。
5. Cancel 路径调用 `Cancel(CloseReason.Cancel)`。
6. 在 `ViewCatalog` 注册：
   - `id = modal.xxx`
   - `kind = Modal`
   - `layer = Modal`
   - 按需求设置 `closeOnBackgroundClick`
7. 通过 `ShowModalAsync<TModal, TResult>` 打开并等待结果。

模板：

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MyConfirmModal : ModalBase<bool>
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        MyConfirmPayload payload = context.GetPayload<MyConfirmPayload>()
            ?? throw new InvalidOperationException($"{nameof(MyConfirmModal)} requires {nameof(MyConfirmPayload)} payload.");

        Apply(payload);
        return UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancel);
        }
    }

    private void OnConfirm()
    {
        SetResult(true);
    }

    private void OnCancel()
    {
        Cancel(CloseReason.Cancel);
    }

    private void Apply(MyConfirmPayload payload)
    {
    }

    private void ValidateConfiguration()
    {
        if (confirmButton == null)
        {
            throw new MissingReferenceException($"{nameof(MyConfirmModal)} '{name}' is missing confirm button.");
        }

        if (cancelButton == null)
        {
            throw new MissingReferenceException($"{nameof(MyConfirmModal)} '{name}' is missing cancel button.");
        }
    }
}
```

## 25. 新增 Tooltip 流程

1. 新增脚本继承 `TooltipBase`。
2. 创建 Tooltip Prefab。
3. Prefab 根节点添加 `CanvasGroup` 和 Tooltip 脚本。
4. 在 `OnOpeningAsync` 中读取 payload、刷新文本和布局。
5. 在 `ViewCatalog` 注册：
   - `id = tooltip.xxx`
   - `kind = Tooltip`
   - `layer = Tooltip`
6. 通过 `ShowTooltipAsync<TTooltip>` 打开。
7. 如需跟随指针，移动时调用 `UpdateTooltipPosition`。
8. 结束时调用 `HideTooltip` 或关闭 Handle。

打开：

```csharp
TooltipOptions options = new TooltipOptions(
    screenPosition: pointerPosition,
    offset: new Vector2(18f, -18f),
    followPointer: true,
    useScreenPosition: true);

await uiManager.ShowTooltipAsync<MyTooltip>(
    payload,
    options,
    cancellationToken);
```

## 26. 新增 ViewPart 流程

1. 新增脚本继承 `ViewPartBase`。
2. 放在 Page、Popup 或 Modal 的子层级，或作为可复用子 Prefab。
3. 由拥有者通过 SerializeField 显式引用。
4. 如有会话数据，提供 `BeginSession(...)` / `EndSession()` 或 `Bind(...)` / `Unbind()`。
5. 如需打开全局 UI，由拥有者传入 `UIManager` 或 `IUIManager`。
6. 不注册到 `ViewCatalog`。

建议：

- 简单子节点不必拆成 ViewPart。
- 有独立生命周期、复杂绑定、复用价值或独立表现逻辑时再拆。
- ViewPart 不应主动扫描场景寻找 UIManager。

## 27. Button 点击绑定

UI 点击目标统一使用 Unity `Button`。

规则：

- 打开时通过 `button.onClick.AddListener(...)` 订阅，关闭时通过 `RemoveListener(...)` 解绑。
- 不要多次打开后重复订阅。
- 可交互状态使用 `button.interactable`。
- 需要默认焦点时通过 `EventSystem.current?.SetSelectedGameObject(button.gameObject)` 设置。

## 28. 诊断

`UIManager` 应提供运行时诊断入口，通常在 Inspector 中显示：

```text
Log Runtime Diagnostics
```

诊断建议包含：

- 是否初始化。
- Root Canvas 名称和模式。
- 当前 Camera。
- 当前 RequestVersion。
- 当前 Tooltip。
- 所有 Layer 状态。
- PageStack。
- PopupStack。
- ModalStack。
- 当前打开 View。
- 池状态。
- 操作是否忙。
- 输入焦点状态。
- ModalMask 状态。
- PopupOutsideClickBlocker 状态。
- Popup / Tooltip 最近定位结果。

排错顺序：

1. 确认场景中只有一个有效 `UIManager`。
2. 确认 `UIFrameworkSettings` 和 `ViewCatalog` 已绑定。
3. 执行 Settings 和 Catalog 校验。
4. Play Mode 下输出 Runtime Diagnostics。
5. 检查目标 View 是否注册到 Catalog。
6. 检查 Prefab 根节点是否有正确 View 脚本和 `CanvasGroup`。
7. 检查 payload 类型是否正确。
8. 检查事件是否重复订阅或未解绑。
9. 检查 Popup / Tooltip Options。
10. 检查动画 Clip、TargetKey、CanvasGroup 和 Binding。

## 29. 常见错误

`UIManager is missing UIFrameworkSettings`

- UIManager 没有绑定设置资产。

`UIManager is missing ViewCatalog`

- UIManager 没有绑定 Catalog。

`ScreenSpaceCamera but uiCamera is not assigned`

- CanvasProfile 使用 Camera 模式但未绑定 UI Camera。

`view type is not registered in ViewCatalog`

- 代码打开了未注册的 View 类型。

`kind does not match prefab component type`

- ViewDefinition 的 Kind 和 Prefab 根脚本基类不匹配。

`prefab does not contain ViewBase on the root`

- Prefab 根节点没有 `PageBase`、`PopupBase`、`ModalBase<>` 或 `TooltipBase` 子类。

`View requires a CanvasGroup`

- View 根节点缺少 `CanvasGroup`。

`requires an explicit UIManager`

- 内部组件需要打开全局 UI，但没有由拥有者注入 UIManager。

## 30. 测试建议

建议至少覆盖：

- `UIFrameworkSettings.Validate()`。
- `CanvasProfile.Validate()`。
- `ViewCatalog.Validate()`。
- Page 打开、关闭、替换、重置。
- Popup 打开、同组替换、外部点击关闭。
- Modal Confirm、Cancel、外部取消。
- Tooltip 替换、跟随指针、隐藏。
- 对象池复用。
- 事件订阅不会因池化重复。
- 动画缺失 Clip 或目标时的降级和日志。
- ScreenSpaceOverlay 和 ScreenSpaceCamera 下的定位。

## 31. AI 协作规则

AI 或自动化脚本修改 UI 时，必须先判断：

1. 这个 UI 是否需要由 `UIManager` 全局打开？
   - 是：继承 `PageBase`、`PopupBase`、`ModalBase<>` 或 `TooltipBase`，并注册 Catalog。
   - 否：使用 `ViewPartBase` 或普通 MonoBehaviour，不注册 Catalog。
2. 是否已有同类 View？
   - 优先复用既有模式。
3. 是否需要外部数据？
   - 使用 payload 或上下文对象。
4. 是否需要打开另一个全局 UI？
   - 通过 `UIManager` 或 `IUIManager`。
5. 是否需要动画？
   - 使用 `IViewTransition`、`UIMotionTransition`、`UIMotionPlayer`。
6. 是否受 LayoutGroup 控制？
   - 动画作用于视觉子节点，不抢布局根节点。
7. 是否订阅事件？
   - 打开时订阅，关闭时解绑。
8. 是否会被池化复用？
   - 关闭时清状态，打开时重新应用 payload。
9. 是否需要 Inspector 引用？
   - 显式 SerializeField，缺失时抛清晰异常。
10. 是否涉及复杂 Prefab 绑定？
    - 优先在 Unity Editor 中配置，不手写 YAML。

## 32. AI 禁止事项

不要做这些事：

- 不要新增第二套 UIManager 或 UIService。
- 不要把 `ViewPartBase` 注册到 `ViewCatalog`。
- 不要让 Popup 或 Tooltip 自己实现屏幕裁剪和翻转定位。
- 不要直接销毁由 UIManager 打开的 View。
- 不要直接调用 `ViewBase.OpenInternalAsync` 或 `ViewBase.CloseInternalAsync`。
- 不要靠全局扫描寻找 UI 依赖。
- 不要在运行时隐式补加业务组件。
- 不要只订阅事件不解绑。
- 不要假设池化 View 的 `Awake` 会再次执行。
- 不要假设 `trackInBackStack` 或 `warmupCount` 已经具备完整行为，必须以当前实现为准。
- 不要把设计文档中未实现的 API 当成可用 API。

## 33. 迁移到新项目的最小清单

迁移框架到新项目时，至少需要：

1. 拷贝 `OrangeUIFramework` 运行时代码。
2. 安装 UniTask。
3. 如使用动画，安装 DOTween。
4. 如使用本地化文本，安装 TextMeshPro。
5. 创建 `CanvasProfile`。
6. 创建 `UIFrameworkSettings`。
7. 创建 `ViewCatalog`。
8. 在启动场景放置 `UIManager`。
9. 绑定 Settings 和 Catalog。
10. 准备至少一个 Page Prefab。
11. 在 Catalog 中注册该 Page。
12. 从启动逻辑调用 `OpenPageAsync<FirstPage>()`。

## 34. 总结

OrangeUIFramework 的核心使用原则是：全局 UI 必须注册到 `ViewCatalog` 并通过 `UIManager` 打开；页面内部组件使用 `ViewPartBase` 直接组合；生命周期中打开绑定、关闭解绑；Popup 和 Tooltip 定位交给框架；Modal 结果通过 `ModalResult<TResult>` 返回；动画通过 `IViewTransition` 接入；对象池复用要求 View 每次打开都能重新应用当前上下文。
