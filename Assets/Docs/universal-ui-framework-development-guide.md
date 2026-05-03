# 通用 UI 框架最终开发文档

## 1. 文档定位

本文档定义一套面向 Unity 2021+、C# 8.0+、UGUI、TextMeshPro、DOTween 的通用 UI 框架最终规格。它不是现有 UI 代码的总结，不继承已有的 Region、Contract、Facade 等零散抽象，也不以当前项目的局部实现作为设计边界。

框架目标是建立一套统一的 UI 运行时系统，让 Page、Modal、Popup、Tooltip、Panel、Widget、Toast 等 UI 单元都拥有一致的创建、挂载、输入、动画、异步、关闭、回收、诊断语义。

实现、迁移、评审、测试均以本文档为准。现有代码与本文档冲突时，新增框架按本文档实现；旧代码通过适配层过渡，不把旧抽象继续扩散到新框架。

## 2. 最终交付范围

框架最终必须完整支持以下能力：

- `ScreenSpaceOverlay`、`ScreenSpaceCamera`、`WorldSpace` 三种 Canvas 工作模式。
- 单 Root Canvas、多 UI Scope、多玩家或多相机 UI 场景。
- Page、Modal、Popup、Tooltip、Panel、Widget、Toast 的统一管理。
- 同步 API、异步 API、协程桥接 API。
- 异步打开、异步关闭、异步替换、异步重置、异步动画等待、操作取消。
- Page 栈、Modal 栈、Popup 集合、Panel 归属树、Tooltip 当前实例、Toast 队列。
- 全局层级、局部槽位、遮罩、输入阻塞、焦点栈、点击外部关闭、返回键处理。
- Popup 与 Tooltip 的屏幕点、RectTransform、Transform、世界坐标定位，以及边界夹取和目标跟随。
- DOTween 动画统一适配，标准动画阶段、冲突策略、等待策略、取消策略。
- Prefab Catalog、资源加载接口、池化、预热、回收清理、Handle 失效。
- 多语言、本地化字体、本地化图片、动效配置、无障碍选项、降低动效、字体缩放、手柄导航。
- UI 流程事务、路由守卫、条件打开、失败回滚。
- 运行时诊断面板、UI 操作日志、交互录制回放、自动截图校验入口。
- Editor 校验工具、Catalog 检查、Prefab 检查、Layer 检查、导航检查。
- EditMode 与 PlayMode 测试覆盖核心规则。

## 3. 技术边界

框架核心程序集只依赖：

- UnityEngine
- UnityEngine.UI
- TextMeshPro
- DOTween
- .NET `Task` / `CancellationToken`

框架核心不直接依赖：

- Addressables
- UniTask
- 业务 Manager
- 全局 EventBus
- 具体游戏数据类型

可选适配由独立程序集提供：

- `AXR.UIFoundation.AddressablesAdapter`
- `AXR.UIFoundation.UniTaskAdapter`
- `AXR.UIFoundation.LegacyAdapter`

适配程序集只能依赖核心程序集，核心程序集不得反向依赖适配程序集。

## 4. 设计原则

### 4.1 有效抽象标准

一个 UI 抽象只有满足以下全部条件，才允许进入框架层：

- 框架能创建它。
- 框架能关闭它。
- 框架能等待它的动画。
- 框架能取消它的操作。
- 框架能定位它。
- 框架能回收它。
- 框架能诊断它。
- 框架能知道它的 Owner。

否则它只是业务工具类，不属于框架核心能力。

### 4.2 禁止模式

以下做法禁止进入新框架：

- 业务脚本直接 `Instantiate` / `Destroy` 框架管理的 Popup、Modal、Page、Panel。
- Tooltip 使用 static Presenter 或全局查找兜底。
- 页面自己创建全屏透明 Image 作为 Popup 关闭遮罩。
- 页面自己管理 UI 层级 sorting order。
- 页面自己 Kill 框架动画。
- 页面自己回收框架对象。
- 页面自己维护关闭队列。
- 业务 payload 内传入 `GameObject`、Prefab、`UIManager`、Canvas Root。
- 框架底层 `FindFirstObjectByType` 查找业务服务。
- 用只封装事件、不接入生命周期的 Contract 冒充框架解耦。
- 用业务 Region 类代替框架 Panel/Slot 生命周期。

## 5. 核心术语

### 5.1 Scope

`UIScope` 是一套独立 UI 运行时上下文。每个 Scope 拥有自己的 Canvas、Layer、Surface 栈、焦点、Tooltip、Toast 队列和诊断信息。

使用场景：

- 主游戏 UI。
- 分屏玩家 UI。
- 独立相机 UI。
- World Space 交互 UI。
- 编辑器预览 UI。

### 5.2 Surface

`UISurface` 是框架管理的 UI 运行时单元。所有 Page、Modal、Popup、Tooltip、Panel、Widget、Toast 都是 Surface。

### 5.3 Owner

Owner 表示 UI 归属关系。Owner 关闭时，所有绑定为 `CloseWithOwner` 的子 Surface 必须自动关闭。

### 5.4 Slot

Slot 是 Page 或 Panel 内的命名挂载点。Panel 只能挂到 Slot 或 Layer，不允许业务任意指定 Transform。

### 5.5 Handle

Handle 是业务层持有 UI 实例的唯一凭证。业务层不得持有框架内部实例状态。

## 6. Surface 分类

```csharp
public enum UISurfaceKind
{
    Page,
    Modal,
    Popup,
    Tooltip,
    Panel,
    Widget,
    Toast
}
```

### 6.1 Page

Page 是主流程界面，进入 Page 栈。典型对象：主菜单、游戏 HUD、商店、结算、角色选择。

规则：

- Page 默认挂载到 `UILayerId.Page`。
- Page 默认进入 Page History。
- 同一 Scope 同一时间只有一个 Page 处于 `VisibleActive`。
- 被覆盖的 Page 处于 `VisibleInactive` 或 `Hidden`，由 `UIPageCoverPolicy` 决定。
- Page 只能通过 `IUIPageService` 或 `IUIService` 打开。

### 6.2 Modal

Modal 是阻塞式 UI，进入 Modal 栈。典型对象：确认框、设置框、奖励选择、失败提示。

规则：

- Modal 默认挂载到 `UILayerId.Modal`。
- Modal 默认创建遮罩。
- Modal 栈顶独占输入。
- Modal 关闭时可返回结果。
- Modal 的下层 Page 和 Popup 默认冻结输入。

### 6.3 Popup

Popup 是上下文浮层，进入 Popup 集合。典型对象：物品操作菜单、右键菜单、短详情面板。

规则：

- Popup 默认挂载到 `UILayerId.Popup`。
- Popup 必须有 Placement。
- Popup 默认绑定 Owner。
- Owner 关闭时 Popup 默认关闭。
- Popup 默认点击外部关闭。
- Popup 可选择抢焦点；操作菜单抢焦点，纯展示 Popup 不抢焦点。

### 6.4 Tooltip

Tooltip 是瞬时提示，Scope 内同一时间只允许一个 Active Tooltip。

规则：

- Tooltip 默认挂载到 `UILayerId.Tooltip`。
- Tooltip 不进入历史栈。
- Tooltip 不抢焦点。
- Tooltip 不阻塞输入。
- 新 Tooltip 显示时替换旧 Tooltip。
- Pointer Exit、Owner Disable、Owner Close、Scope Destroy 时必须隐藏。

### 6.5 Panel

Panel 是 Page 或 Panel 内部具有独立生命周期的子界面。典型对象：商店左右侧栏、购物车栏、背包栏、设置页子面板。

规则：

- Panel 必须有 Owner。
- Panel 必须挂载到 Slot 或配置的 Layer。
- Panel 支持 Show/Hide 动画。
- Owner 关闭时 Panel 默认关闭。
- Panel 可管理自己的 Widget 和局部 Popup。
- Panel 不进入全局 Page History。

### 6.6 Widget

Widget 是由 Page、Panel、List 或 Grid 管理的小 UI 单元。典型对象：商品卡、背包格、Buff 图标、列表项。

规则：

- Widget 默认不进入全局 Surface 栈。
- Widget 由 `IUIWidgetFactory` 创建、绑定、回收。
- Widget 不允许直接打开 Page、Modal、Popup。
- Widget 将用户意图转为事件，由上层 Surface 决定业务响应。

### 6.7 Toast

Toast 是非阻塞通知。典型对象：获得金币、购买成功、保存成功、错误提示。

规则：

- Toast 默认挂载到 `UILayerId.Toast`。
- Toast 不抢焦点。
- Toast 不阻塞输入。
- Toast 进入 Toast 队列。
- Toast 支持合并策略，重复消息可计数合并。
- Toast 支持优先级，错误类 Toast 高于普通提示。

## 7. 命名与身份

```csharp
public readonly struct UISurfaceKey
{
    public string Value { get; }
}

public readonly struct UISurfaceId
{
    public string Value { get; }
}

public readonly struct UIScopeId
{
    public string Value { get; }
}
```

Key 命名规则：

```text
Page.Shop
Page.Gameplay
Modal.Confirm
Popup.Inventory.ItemActions
Tooltip.ItemDescription
Panel.Shop.LeftProperties
Panel.Shop.RightInventory
Widget.Inventory.ItemCell
Toast.CurrencyChanged
```

规则：

- `UISurfaceKey` 由配置资产声明，不能只依赖 C# 类型名。
- `UISurfaceId` 由运行时生成，格式为 `{ScopeId}:{SurfaceKey}:{Sequence}`。
- `UIScopeId` 由 Bootstrap 显式指定，主 Scope 固定为 `Main`。
- Key 重复时启动失败。

## 8. 目录与程序集

框架目录固定为：

```text
Assets/Scripts/Framework/UIFoundation/
├─ Runtime/
│  ├─ Core/
│  ├─ Scope/
│  ├─ Canvas/
│  ├─ Layers/
│  ├─ Surfaces/
│  ├─ Navigation/
│  ├─ Animation/
│  ├─ Input/
│  ├─ Loading/
│  ├─ Pooling/
│  ├─ Positioning/
│  ├─ Binding/
│  ├─ Localization/
│  ├─ Accessibility/
│  ├─ Transactions/
│  ├─ Recording/
│  └─ Diagnostics/
├─ Data/
├─ Editor/
└─ Adapters/
```

业务 UI 目录固定为：

```text
Assets/Scripts/UI/
├─ Pages/
├─ Modals/
├─ Popups/
├─ Tooltips/
├─ Panels/
├─ Widgets/
└─ Toasts/
```

资源目录固定为：

```text
Assets/Prefabs/UI/
├─ Pages/
├─ Modals/
├─ Popups/
├─ Tooltips/
├─ Panels/
├─ Widgets/
└─ Toasts/

Assets/ScriptableObjects/UIFoundation/
├─ Settings/
├─ Catalogs/
├─ Layers/
├─ Animation/
├─ Localization/
├─ Accessibility/
└─ PreloadPlans/
```

## 9. 全局设置资产

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Settings")]
public sealed class UIFoundationSettings : ScriptableObject
{
    [SerializeField] private UIScopeSettings mainScope;
    [SerializeField] private List<UIScopeSettings> additionalScopes;
    [SerializeField] private UISurfaceCatalog surfaceCatalog;
    [SerializeField] private UILayerSettings layerSettings;
    [SerializeField] private UIAnimationSettings animationSettings;
    [SerializeField] private UIInputSettings inputSettings;
    [SerializeField] private UIPoolingSettings poolingSettings;
    [SerializeField] private UILocalizationSettings localizationSettings;
    [SerializeField] private UIAccessibilitySettings accessibilitySettings;
    [SerializeField] private UIDiagnosticsSettings diagnosticsSettings;
}
```

启动时必须校验：

- `mainScope` 不为空。
- `surfaceCatalog` 不为空。
- `layerSettings` 不为空。
- 每个 Scope Id 唯一。
- 每个 Surface Key 唯一。
- 每个 Surface Prefab 存在。
- 每个 Layer Id 唯一。

校验失败时抛出 `UIFoundationConfigurationException`，并输出具体资产路径和字段名。

## 10. Scope 系统

```csharp
public interface IUIScope
{
    UIScopeId ScopeId { get; }
    IUICanvasHost CanvasHost { get; }
    IUILayerService LayerService { get; }
    IUISurfaceRegistry SurfaceRegistry { get; }
    IUIFocusService FocusService { get; }
}
```

Scope 创建规则：

- `UIFoundationBootstrap` 在场景启动时创建 Main Scope。
- 额外 Scope 由 `IUIScopeService.CreateScope` 创建。
- Scope 销毁时必须取消所有未完成 UI 操作。
- Scope 销毁时必须关闭所有 Surface。
- Scope 销毁时必须释放全部池化对象。

```csharp
public interface IUIScopeService
{
    IUIScope Main { get; }
    IUIScope Get(UIScopeId scopeId);
    IUIScope CreateScope(UIScopeSettings settings);
    void DestroyScope(UIScopeId scopeId);
}
```

## 11. Canvas 系统

```csharp
public enum UICanvasMode
{
    ScreenSpaceOverlay,
    ScreenSpaceCamera,
    WorldSpace
}
```

```csharp
[Serializable]
public sealed class UIScopeSettings
{
    public string scopeId = "Main";
    public UICanvasMode canvasMode = UICanvasMode.ScreenSpaceOverlay;
    public Camera explicitCamera;
    public float planeDistance = 100f;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public float matchWidthOrHeight = 0.5f;
    public bool dontDestroyOnLoad = true;
    public bool applySafeArea = true;
    public bool createEventSystemIfMissing = true;
}
```

`IUICanvasHost`：

```csharp
public interface IUICanvasHost
{
    UIScopeId ScopeId { get; }
    Canvas RootCanvas { get; }
    RectTransform RootRect { get; }
    RectTransform SafeAreaRect { get; }
    Camera UICamera { get; }
    UICanvasMode CanvasMode { get; }

    Vector2 ScreenToLocal(Vector2 screenPosition);
    Vector2 WorldToLocal(Vector3 worldPosition);
    Vector2 ClampLocalToSafeArea(RectTransform target, Vector2 localPosition, Vector2 padding);
}
```

### 11.1 Overlay 模式

Overlay 模式强制规则：

- `Canvas.renderMode = RenderMode.ScreenSpaceOverlay`。
- `RootCanvas.worldCamera = null`。
- 所有 `RectTransformUtility` 屏幕坐标转换传入 camera 为 `null`。
- `GraphicRaycaster.eventCamera` 保持为空。

### 11.2 Camera 模式

Camera 模式强制规则：

- `Canvas.renderMode = RenderMode.ScreenSpaceCamera`。
- `explicitCamera` 必须存在。
- `RootCanvas.worldCamera = explicitCamera`。
- `RootCanvas.planeDistance = planeDistance`。
- `GraphicRaycaster.eventCamera = explicitCamera`。
- `ScreenToLocal` 必须使用 `explicitCamera`。

Camera 缺失时启动失败，不允许退回 Overlay。

### 11.3 WorldSpace 模式

WorldSpace 模式强制规则：

- `Canvas.renderMode = RenderMode.WorldSpace`。
- `explicitCamera` 必须存在。
- Scope Root 必须由调用方传入父节点或世界矩阵。
- Popup/Tooltip 的屏幕夹取使用相机投影到 Root Rect。
- WorldSpace Scope 不参与 Main Scope 的层级排序。

## 12. Layer 系统

Layer Id：

```csharp
public enum UILayerId
{
    Background,
    Page,
    Panel,
    ModalMask,
    Modal,
    PopupMask,
    Popup,
    Tooltip,
    Toast,
    System,
    Debug
}
```

固定默认排序：

```text
Background = 0
Page       = 100
Panel      = 200
ModalMask  = 300
Modal      = 310
PopupMask  = 400
Popup      = 410
Tooltip    = 500
Toast      = 550
System     = 600
Debug      = 900
```

配置：

```csharp
[Serializable]
public sealed class UILayerConfig
{
    public UILayerId layerId;
    public int sortingOrder;
    public bool blocksRaycasts;
    public bool createCanvas;
}
```

强制规则：

- 每个 Layer 都有一个 `RectTransform` Root。
- `createCanvas = true` 时 Layer Root 添加 `Canvas`，并设置 `overrideSorting = true`。
- `blocksRaycasts = true` 时添加 `GraphicRaycaster`。
- Surface 只能挂到配置中存在的 Layer。
- Layer Root 只由框架创建，不允许业务脚本创建同名 Root。

## 13. Surface Catalog

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Surface Catalog")]
public sealed class UISurfaceCatalog : ScriptableObject
{
    [SerializeField] private List<UISurfaceEntry> entries;
}
```

```csharp
[Serializable]
public sealed class UISurfaceEntry
{
    public string key;
    public UISurfaceKind kind;
    public GameObject prefab;
    public UILayerId layerId;
    public UISurfaceInstancePolicy instancePolicy;
    public UISurfaceHistoryPolicy historyPolicy;
    public UISurfaceOwnerPolicy ownerPolicy;
    public UISurfaceClosePolicy closePolicy;
    public UIMaskPolicy maskPolicy;
    public UIInputPolicy inputPolicy;
    public UIAnimationProfile animationProfile;
    public UIPoolingPolicy poolingPolicy;
    public UIPreloadPolicy preloadPolicy;
    public UISafeAreaPolicy safeAreaPolicy;
    public UILocalizationPolicy localizationPolicy;
}
```

枚举：

```csharp
public enum UISurfaceInstancePolicy
{
    SingletonPerScope,
    SingletonPerOwner,
    Multiple
}

public enum UISurfaceHistoryPolicy
{
    None,
    PageHistory,
    ModalHistory,
    LocalOwnerHistory
}

public enum UISurfaceOwnerPolicy
{
    NoOwnerRequired,
    OwnerRequired,
    CloseWithOwner,
    DetachWhenOwnerClosed
}

public enum UISurfaceClosePolicy
{
    Manual,
    CloseOnBack,
    CloseOnOutsideClick,
    CloseOnOutsideClickOrBack,
    CloseAfterDuration,
    CloseWhenOwnerClosed
}
```

Catalog 校验规则：

- `key` 不能为空。
- `prefab` 不能为空。
- `kind` 必须与 Prefab 上的 View 组件声明一致。
- `Panel` 必须 `OwnerRequired` 或 `CloseWithOwner`。
- `Tooltip` 必须 `SingletonPerScope`。
- `Toast` 必须 `Multiple` 或 `SingletonPerScope`，由合并策略决定。
- `Modal` 的 `maskPolicy` 不能是 `None`，除非配置显式标记为 `TransparentBlockingModal`。
- `Widget` 不允许配置 `PageHistory`。

## 14. Surface View 接口

最小接口：

```csharp
public interface IUISurfaceView
{
    RectTransform Root { get; }
    UISurfaceKind Kind { get; }
}
```

可选接口：

```csharp
public interface IUIBindable<in TPayload>
{
    void Bind(TPayload payload);
    void Unbind();
}

public interface IUIAsyncPrepare<in TPayload>
{
    Task PrepareAsync(TPayload payload, CancellationToken cancellationToken);
}

public interface IUICloseGuard
{
    Task<bool> CanCloseAsync(UICloseReason reason, CancellationToken cancellationToken);
}

public interface IUIResultProvider<TResult>
{
    event Action<TResult> ResultSubmitted;
    event Action Cancelled;
}

public interface IUIReuseHandler
{
    void OnBeforeRecycle();
    void OnAfterReuse();
}
```

规则：

- View 不负责实例化自己。
- View 不负责销毁自己。
- View 不直接操作 Layer。
- View 不直接操作全局 UI 栈。
- View 只处理显示、绑定、用户事件和必要的局部校验。

## 15. Handle 与实例状态

```csharp
public readonly struct UIHandle
{
    public UIScopeId ScopeId { get; }
    public UISurfaceId SurfaceId { get; }
    public UISurfaceKey Key { get; }
    public UISurfaceKind Kind { get; }
    public int Version { get; }
    public bool IsValid { get; }
}

public readonly struct UIHandle<TView>
    where TView : Component
{
    public UIHandle Raw { get; }
    public TView View { get; }
}
```

Handle 规则：

- 每次实例从池中复用时 Version 自增。
- 旧 Version 的 Handle 必须失效。
- 所有服务方法必须先校验 Handle。
- 无效 Handle 调用关闭返回 false 或抛出 `UIInvalidHandleException`，由 API 约定决定。
- 业务不得通过 View 引用绕开 Handle 生命周期。

## 16. 生命周期状态机

```csharp
public enum UISurfaceState
{
    None,
    Loading,
    Instantiated,
    Initialized,
    Preparing,
    Opening,
    Visible,
    Inactive,
    Closing,
    Closed,
    Pooled,
    Destroyed,
    Failed
}
```

状态转移固定为：

```text
None
 -> Loading
 -> Instantiated
 -> Initialized
 -> Preparing
 -> Opening
 -> Visible
 -> Inactive
 -> Closing
 -> Closed
 -> Pooled

Closed -> Destroyed
Pooled -> Initialized
任意结构性状态 -> Failed
```

生命周期顺序：

```text
LoadPrefab
InstantiateOrReuse
CreateHandle
InitializeContext
BindPayload
PrepareAsync
AttachToLayerOrSlot
SetHiddenImmediate
ApplyInputBlocked
PlayShowAnimation
RegisterVisible
RaiseOpened
```

关闭顺序：

```text
ValidateCloseGuard
MarkClosing
FreezeInput
CloseChildrenByOwnerPolicy
PlayHideAnimation
UnregisterVisible
UnbindPayload
ReleaseResultSubscriptions
RecycleOrDestroy
InvalidateHandle
RaiseClosed
```

事件完成语义：

- `OpenAsync` 在 Show 动画完成且状态进入 `Visible` 后完成。
- `CloseAsync` 在 Hide 动画完成且实例回收或销毁后完成。
- `ShowTooltip` 为同步替换操作，定位完成后返回。
- `Toast.Enqueue` 在进入队列后返回，不等待展示完成。

## 17. 异步操作与取消

所有结构性操作都由 `IUIOperationScheduler` 串行调度。

```csharp
public enum UIOperationKind
{
    Open,
    Close,
    Replace,
    Reset,
    Show,
    Hide,
    Reposition,
    Transaction
}
```

```csharp
public sealed class UIOperation
{
    public UIOperationKind Kind { get; }
    public UIScopeId ScopeId { get; }
    public UISurfaceId SurfaceId { get; }
    public CancellationTokenSource LifetimeCancellation { get; }
}
```

强制取消规则：

| 当前状态 | 收到取消 | 结果 |
|---|---|---|
| Loading | 停止加载，释放已加载资源，Handle 不创建 |
| Instantiated | 销毁或入池，Handle 失效 |
| Preparing | 调用 Unbind，销毁或入池 |
| Opening | Kill Show 动画，播放 Hide 或立即隐藏，随后回收 |
| Visible | 不受打开 token 影响 |
| Closing | Kill Hide 动画后立即完成关闭 |

并发规则：

- 同一 Surface 不能同时执行两个 Open。
- 同一 Surface 不能同时执行 Open 和 Close；Close 优先。
- 同一 Scope 的 Page Replace/Reset 互斥。
- Modal 操作按栈顶顺序串行。
- Popup 操作按 Key 和 Owner 串行。
- Tooltip Show 替换旧 Tooltip，不排队。
- Toast Enqueue 不阻塞其他 UI 操作。

## 18. 公共服务 API

### 18.1 IUIService

```csharp
public interface IUIService
{
    IUIPageService Pages { get; }
    IUIModalService Modals { get; }
    IUIPopupService Popups { get; }
    IUITooltipService Tooltips { get; }
    IUIPanelService Panels { get; }
    IUIToastService Toasts { get; }
    IUITransactionService Transactions { get; }
}
```

### 18.2 Page

```csharp
public interface IUIPageService
{
    Task<UIHandle<TView>> OpenAsync<TView, TPayload>(
        TPayload payload,
        UIOpenOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView;

    Task<UIHandle<TView>> ReplaceAsync<TView, TPayload>(
        TPayload payload,
        UIReplaceOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView;

    Task<UIHandle<TView>> ResetAsync<TView, TPayload>(
        TPayload payload,
        UIResetOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView;

    Task<bool> CloseTopAsync(CancellationToken cancellationToken);
}
```

### 18.3 Modal

```csharp
public interface IUIModalService
{
    Task<UIHandle<TView>> OpenAsync<TView, TPayload>(
        TPayload payload,
        UIModalOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView;

    Task<TResult> ShowAsync<TView, TPayload, TResult>(
        TPayload payload,
        UIModalOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView, IUIResultProvider<TResult>;

    Task<bool> CloseTopAsync(UICloseReason reason, CancellationToken cancellationToken);
}
```

### 18.4 Popup

```csharp
public interface IUIPopupService
{
    Task<UIHandle<TView>> ShowAsync<TView, TPayload>(
        TPayload payload,
        UIPopupPlacement placement,
        UIPopupOptions options,
        CancellationToken cancellationToken)
        where TView : Component, IUISurfaceView;

    Task<bool> CloseAsync(UIHandle handle, CancellationToken cancellationToken);
    Task<int> CloseByOwnerAsync(UIHandle owner, CancellationToken cancellationToken);
}
```

### 18.5 Tooltip

```csharp
public interface IUITooltipService
{
    void Show<TPayload>(TPayload payload, UITooltipPlacement placement);
    void Move(UITooltipPlacement placement);
    void Hide();
}
```

### 18.6 Panel

```csharp
public interface IUIPanelService
{
    Task<UIHandle<TPanel>> OpenAsync<TPanel, TPayload>(
        UIHandle owner,
        string slotId,
        TPayload payload,
        UIPanelOptions options,
        CancellationToken cancellationToken)
        where TPanel : Component, IUISurfaceView;

    Task<bool> CloseSlotAsync(
        UIHandle owner,
        string slotId,
        CancellationToken cancellationToken);
}
```

### 18.7 Toast

```csharp
public interface IUIToastService
{
    void Enqueue<TPayload>(TPayload payload, UIToastOptions options);
    void Clear(UIToastClearMode clearMode);
}
```

## 19. Options 明确定义

```csharp
public sealed class UIOpenOptions
{
    public UIScopeId ScopeId = new UIScopeId("Main");
    public bool Activate = true;
    public bool BringToFront = true;
    public UIAnimationPolicy AnimationPolicy = UIAnimationPolicy.Play;
}

public sealed class UIModalOptions
{
    public UIScopeId ScopeId = new UIScopeId("Main");
    public bool CloseOnMaskClick = true;
    public bool CloseOnBack = true;
    public bool BlockLowerInput = true;
    public bool PauseGameTime = false;
    public Color MaskColor = new Color(0f, 0f, 0f, 0.55f);
    public UIAnimationPolicy AnimationPolicy = UIAnimationPolicy.Play;
}

public sealed class UIPopupOptions
{
    public UIHandle Owner;
    public bool CloseWithOwner = true;
    public bool CloseOnOutsideClick = true;
    public bool CloseOnBack = true;
    public bool CaptureFocus = true;
    public UIAnimationPolicy AnimationPolicy = UIAnimationPolicy.Play;
}

public sealed class UIPanelOptions
{
    public UIPanelSlotPolicy SlotPolicy = UIPanelSlotPolicy.ReplaceExisting;
    public bool CloseWithOwner = true;
    public bool Activate = true;
    public UIAnimationPolicy AnimationPolicy = UIAnimationPolicy.Play;
}
```

```csharp
public enum UIAnimationPolicy
{
    Play,
    SkipToEnd,
    ImmediateHidden,
    ImmediateVisible
}

public enum UIPanelSlotPolicy
{
    ReplaceExisting,
    StackInSlot,
    AllowMultiple
}
```

## 20. Page 管理

### 20.1 Page 栈规则

Page 栈记录所有 `trackInHistory` 的 Page。

`OpenAsync`：

- Singleton Page 已存在：复用实例、更新 payload、置顶、播放 Focus 动画。
- Multiple Page：创建新实例、压栈、激活。
- 当前 Page 根据 `UIPageCoverPolicy` 转为 Inactive 或 Hidden。

`ReplaceAsync`：

- 关闭当前栈顶 Page。
- 等待 Hide 完成。
- 打开目标 Page。
- Replace 失败时不恢复旧 Page；失败结果进入 Diagnostics，调用方得到异常。

`ResetAsync`：

- 从栈顶到栈底关闭所有 Page。
- 等待全部 Hide 完成。
- 打开目标 Page。
- 任意关闭失败时终止 Reset，并抛出 `UITransitionException`。

### 20.2 Cover Policy

```csharp
public enum UIPageCoverPolicy
{
    KeepVisibleDisableInput,
    HideVisualDisableInput,
    ClosePrevious
}
```

默认策略固定为 `KeepVisibleDisableInput`。

## 21. Modal 管理

Modal 栈规则：

- 新 Modal 永远压到 Modal 栈顶。
- 栈顶 Modal 拥有焦点。
- 非栈顶 Modal 保持可见但不可交互。
- Modal 关闭后，前一个 Modal 恢复焦点。
- 最后一个 Modal 关闭后，焦点返回当前 Page 或 Popup。

Modal 结果规则：

- `ResultSubmitted` 触发后，框架记录结果并关闭 Modal。
- `Cancelled` 触发后，框架按 `UIModalCancelResultPolicy` 返回结果或抛取消异常。

```csharp
public enum UIModalCancelResultPolicy
{
    ReturnDefault,
    ThrowOperationCanceled
}
```

## 22. Popup 管理

Popup 集合按 `(ScopeId, Key, Owner)` 索引。

打开规则：

- `SingletonPerScope`：关闭旧实例后打开新实例。
- `SingletonPerOwner`：同 Owner 下复用或替换。
- `Multiple`：创建新实例。

点击外部关闭规则：

- 框架注册 Popup 内容 Rect。
- 指针按下时检测是否落在任何更高层 Surface 内。
- 若不在当前 Popup 内，且当前 Popup 配置为外部点击关闭，则关闭。
- 外部点击不会穿透触发下层业务按钮，除非 Popup 配置 `AllowClickThrough`。

## 23. Tooltip 管理

Tooltip 固定为 `SingletonPerScope`。

显示规则：

- `Show` 替换当前 Tooltip payload。
- 当前 Tooltip 未实例化时从池或 Prefab 创建。
- Tooltip 位置立即计算。
- Tooltip 跟随由 `UIPositioningService` 管理。

隐藏规则：

- Pointer Exit 隐藏。
- Owner 关闭隐藏。
- Scope 销毁隐藏。
- 新 Page Reset 隐藏。
- Modal 打开时隐藏，除非 Tooltip Owner 属于该 Modal。

## 24. Panel 与 Slot 管理

Page 或 Panel 声明 Slot：

```csharp
public interface IUISlotHost
{
    bool TryGetSlot(string slotId, out RectTransform slotRoot);
}
```

Slot 规则：

- Slot Id 在同一个 Owner 内必须唯一。
- `ReplaceExisting`：打开新 Panel 前关闭旧 Panel。
- `StackInSlot`：同 Slot 内维护局部栈。
- `AllowMultiple`：同 Slot 内允许多个 Panel，调用方必须用 Handle 关闭。
- Owner 关闭时，所有 `CloseWithOwner` Panel 从内到外关闭。

Panel 不是普通子控件。只有满足以下任意条件才定义为 Panel：

- 需要独立打开关闭。
- 需要独立动画。
- 需要独立异步加载或准备数据。
- 需要独立输入冻结。
- 需要独立回收。
- 需要被诊断系统看见。

## 25. Widget 与高性能列表

Widget 工厂：

```csharp
public interface IUIWidgetFactory
{
    TWidget Spawn<TWidget, TPayload>(Transform parent, TPayload payload)
        where TWidget : Component, IUIWidget<TPayload>;

    void Despawn<TWidget>(TWidget widget)
        where TWidget : Component;
}
```

列表能力：

- 支持固定高度虚拟列表。
- 支持动态高度缓存列表。
- 支持 Grid 虚拟化。
- 支持数据 diff 刷新。
- 支持局部刷新、插入、删除、移动。
- 支持选中态、焦点态、Hover 态复用。
- 支持低 GC 刷新，不在每次刷新时分配闭包和临时 List。

Widget 规则：

- Widget `Bind` 前必须先 `Unbind` 上一次 payload。
- Widget 回收前必须解绑事件。
- Widget 不能持有业务服务的生命周期所有权。

## 26. Toast 通知中心

Toast 是框架内置好用功能之一，用来替代业务到处写临时提示文本。

```csharp
public enum UIToastPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum UIToastMergePolicy
{
    None,
    MergeSameKey,
    ReplaceSameKey,
    CountSameKey
}
```

规则：

- Toast 按 Priority 排队。
- Critical Toast 立即插队。
- 同 Key Toast 按 MergePolicy 合并。
- Toast 同时显示数量由设置控制，默认 3。
- Toast 超时自动关闭。
- Toast 不阻塞输入。

## 27. 定位系统

```csharp
public enum UIAnchorKind
{
    ScreenPoint,
    RectTransform,
    Transform,
    WorldPosition
}

public enum UIPlacementFallback
{
    None,
    FlipHorizontal,
    FlipVertical,
    FlipBoth,
    BestFit
}
```

```csharp
public sealed class UIPlacement
{
    public UIAnchorKind AnchorKind;
    public Vector2 ScreenPoint;
    public RectTransform RectAnchor;
    public Transform TransformAnchor;
    public Vector3 WorldPosition;
    public Vector2 Offset;
    public Vector2 Pivot;
    public Vector2 Padding;
    public bool ClampToSafeArea;
    public bool FollowAnchor;
    public UIPlacementFallback Fallback;
}
```

定位算法固定顺序：

1. 将 Anchor 转为屏幕点。
2. 将屏幕点转为 Scope Root 本地坐标。
3. 应用 Offset。
4. 应用 Pivot。
5. 根据 SafeArea 计算边界。
6. 若超出边界，按 Fallback 选择翻转或最佳位置。
7. 写入 `RectTransform.anchoredPosition`。

跟随规则：

- `FollowAnchor = true` 时注册到 `UIFollowPlacementScheduler`。
- Scheduler 在 LateUpdate 统一刷新。
- Anchor Transform 失效时关闭 Surface。
- 刷新使用脏标记，Transform 未变化时不重新布局。

## 28. 动画系统

标准动画阶段：

```csharp
public enum UIAnimationPhase
{
    Hidden,
    Visible,
    Show,
    Hide,
    Focus,
    Blur,
    HoverIn,
    HoverOut,
    Press,
    Release,
    Disabled,
    Enabled,
    Attention
}
```

动画服务：

```csharp
public interface IUIAnimationService
{
    Task PlayAsync(
        UISurfaceInstance instance,
        UIAnimationPhase phase,
        UIAnimationPolicy policy,
        CancellationToken cancellationToken);

    void SetImmediate(UISurfaceInstance instance, UIAnimationPhase phase);
    void Kill(UISurfaceInstance instance, UIAnimationKillMode killMode);
}
```

适配优先级固定为：

1. `IUIAnimationAdapter`
2. `UIMotionPlayer`
3. `UISequenceDirector`
4. `CanvasGroupAnimationFallback`

冲突策略：

```csharp
public enum UIAnimationConflictPolicy
{
    StopSameChannel,
    StopAllOnSurface,
    AllowParallel,
    RejectNew
}
```

规则：

- Show 完成后 OpenAsync 才完成。
- Hide 完成后 CloseAsync 才完成。
- 关闭中的 Surface 不接受 Hover/Press 动画。
- 回收前必须 Kill 所有动画。
- Reduced Motion 开启时，Show/Hide 压缩到 0.05 秒，Attention 动画禁用。

## 29. 输入、焦点与返回键

焦点优先级：

```text
Debug > System > Modal > Popup > Page > Panel > Widget
```

返回键处理顺序：

1. Debug Overlay
2. System Surface
3. Modal 栈顶
4. Popup 栈顶
5. 当前 Page
6. Toast 不处理返回键
7. Tooltip 直接隐藏

输入策略：

```csharp
public enum UIInputBlockPolicy
{
    None,
    BlockLowerLayers,
    BlockLowerSurfaces,
    Exclusive
}
```

规则：

- Modal 默认 `Exclusive`。
- Popup 默认 `BlockLowerSurfaces`。
- Tooltip 默认 `None`。
- Toast 默认 `None`。
- Page 默认 `BlockLowerSurfaces`。

手柄导航：

- 每个 Modal 必须声明默认 Selectable。
- 每个 Page 可声明默认 Selectable。
- Surface 激活时焦点进入默认 Selectable。
- Surface 关闭时焦点还原到前一个 Surface。

## 30. UI 流程事务

UI 流程事务是框架内置高级功能，用于解决复杂 UI 切换半成功的问题。

```csharp
public interface IUITransactionService
{
    IUITransaction Begin(string name);
}

public interface IUITransaction
{
    IUITransaction Close(UIHandle handle);
    IUITransaction OpenPage<TView, TPayload>(TPayload payload);
    IUITransaction OpenModal<TView, TPayload>(TPayload payload);
    IUITransaction Callback(Func<CancellationToken, Task> callback);
    Task CommitAsync(CancellationToken cancellationToken);
}
```

事务规则：

- 事务内步骤串行执行。
- 任一步骤失败时，执行已登记的补偿动作。
- 关闭旧 Page 后打开新 Page 失败时，事务按配置恢复旧 Page 或进入 Fallback Page。
- 事务记录进入 Diagnostics。
- 同一 Scope 同一时间只允许一个 Page 事务。

## 31. 路由与守卫

路由表：

```csharp
public sealed class UIRoute
{
    public string RouteId;
    public UISurfaceKey TargetKey;
    public UISurfaceKind Kind;
    public List<UIRouteGuard> Guards;
}
```

守卫：

```csharp
public abstract class UIRouteGuard : ScriptableObject
{
    public abstract Task<UIRouteGuardResult> EvaluateAsync(
        UIRouteContext context,
        CancellationToken cancellationToken);
}
```

用途：

- 背包未解锁时禁止打开背包 Panel。
- 商店货币数据未准备好时延迟打开。
- 战斗中禁止打开某些 Modal。
- 新手教程强制跳转指定 Page。

守卫失败时返回明确结果：

```csharp
public enum UIRouteGuardResultKind
{
    Allow,
    Deny,
    Redirect,
    Delay
}
```

## 32. 多语言与无障碍

多语言系统是框架核心能力。所有可见文本、可本地化图片、字体回退、数字格式、时间格式、复数规则、RTL 布局都由 `IUILocalizationService` 统一管理。

### 32.1 语言标识

```csharp
public readonly struct UILocaleId
{
    public string Value { get; }
}
```

Locale 命名使用 BCP 47 风格字符串：

```text
zh-CN
zh-TW
en-US
ja-JP
ko-KR
fr-FR
de-DE
es-ES
ru-RU
ar-SA
```

规则：

- 默认语言由 `UILocalizationSettings.defaultLocale` 指定。
- 当前语言持久化到玩家本地设置。
- 缺失语言回退顺序固定为：当前区域语言 -> 当前主语言 -> 默认语言 -> Key 原文。
- 运行时切换语言必须刷新所有已注册本地化目标。

### 32.2 设置资产

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Localization Settings")]
public sealed class UILocalizationSettings : ScriptableObject
{
    public UILocaleId defaultLocale;
    public List<UILocaleDefinition> supportedLocales;
    public List<UILocalizationTable> stringTables;
    public List<UILocalizedSpriteTable> spriteTables;
    public List<UILocalizedFontTable> fontTables;
    public bool persistSelectedLocale = true;
    public bool logMissingKeys = true;
    public bool useKeyAsFallbackText = true;
}
```

```csharp
[Serializable]
public sealed class UILocaleDefinition
{
    public string localeId;
    public string displayNameKey;
    public bool rightToLeft;
    public TMP_FontAsset defaultFont;
    public TMP_FontAsset fallbackFont;
}
```

### 32.3 文本表

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Localization/String Table")]
public sealed class UILocalizationTable : ScriptableObject
{
    public UILocaleId localeId;
    public List<UILocalizedStringEntry> entries;
}

[Serializable]
public sealed class UILocalizedStringEntry
{
    public string key;
    [TextArea] public string value;
}
```

Key 命名规则：

```text
ui.shop.title
ui.shop.buy
ui.shop.reroll.cost
ui.inventory.empty
ui.modal.confirm.sell_weapon
ui.toast.purchase_success
```

规则：

- UI 文本不允许直接硬编码显示文案。
- Key 必须小写，使用点号分组。
- 同一个 Key 在同一语言表内不能重复。
- 参数使用 `{name}` 命名占位，不使用 `{0}` 位置占位。
- 缺失 Key 必须进入 Diagnostics。

### 32.4 格式化

```csharp
public interface IUILocalizationService
{
    UILocaleId CurrentLocale { get; }
    event Action<UILocaleId> LocaleChanged;

    void SetLocale(UILocaleId localeId);
    string GetText(string key);
    string Format(string key, IReadOnlyDictionary<string, object> arguments);
    Sprite GetSprite(string key);
    TMP_FontAsset GetFont(string fontKey = null);
    bool IsRightToLeft(UILocaleId localeId);
}
```

格式化规则：

- 数字、金币、百分比、时间、日期由 `IUILocalizedFormatter` 统一处理。
- `Format` 必须支持命名参数。
- 参数缺失时保留占位符，并记录 Diagnostics。
- 复数规则由 `IUIPluralRuleProvider` 处理。

```csharp
public interface IUILocalizedFormatter
{
    string Number(double value);
    string Integer(int value);
    string Percent(float value);
    string Currency(int value, string currencyKey);
    string Duration(TimeSpan duration);
    string Date(DateTime dateTime);
}
```

### 32.5 本地化组件

```csharp
public sealed class UILocalizedText : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string key;
    [SerializeField] private List<UILocalizedArgumentBinding> arguments;
}
```

```csharp
public sealed class UILocalizedImage : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private string spriteKey;
}
```

```csharp
public interface IUILocalizationTarget
{
    void RefreshLocalization(IUILocalizationService localization);
}
```

规则：

- Surface 打开时，框架自动刷新其所有 `IUILocalizationTarget`。
- 语言切换时，已打开 Surface 必须立即刷新。
- 本地化文本刷新后必须触发布局重建。
- RTL 语言下，标记为 `MirrorForRTL` 的布局和图标必须镜像。
- 字体由 Locale 定义统一应用，单个文本可显式覆盖。

### 32.6 本地化图片与图标

本地化图片用于含文字的图片、语言相关图标、地区相关标识。

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Localization/Sprite Table")]
public sealed class UILocalizedSpriteTable : ScriptableObject
{
    public UILocaleId localeId;
    public List<UILocalizedSpriteEntry> entries;
}
```

规则：

- 普通无文字图标不进入本地化表。
- 含文字图片必须进入本地化表。
- 缺失图片按 Locale 回退链查找。
- 最终缺失时显示配置的 Missing Sprite，并记录 Diagnostics。

### 32.7 无障碍设置

```csharp
public sealed class UIAccessibilitySettings : ScriptableObject
{
    public float fontScale = 1f;
    public bool reduceMotion;
    public bool showControllerHints;
    public bool enableScreenReaderLabels;
}
```

规则：

- 所有文本组件通过 `IUIFontScaleTarget` 接收字体缩放。
- Reduced Motion 由 Animation Service 统一执行。
- 控制器提示由当前输入设备类型切换。
- Screen Reader Label 使用本地化 Key，不直接写死文本。

## 33. 数据绑定与 Controller

框架提供组合规则，不强制业务使用特定 MVVM 框架。

推荐结构固定为：

```text
View：UGUI 引用、渲染、用户事件
Controller：订阅 View 事件、调用业务服务、驱动 View
Payload：打开 UI 时传入的依赖和初始数据
Model Snapshot：用于渲染的不可变快照
```

Controller 生命周期：

```csharp
public interface IUIController : IDisposable
{
    void Enter();
    void Exit();
}
```

规则：

- Controller 由 Surface 打开时创建。
- Surface 关闭时 Dispose。
- Controller 订阅的所有事件必须在 Dispose 解除。
- View 不直接访问业务 Manager。
- 业务服务通过 Payload 或 Context Resolver 显式传入。

## 34. Context Resolver

```csharp
public interface IUIContextResolver
{
    bool TryResolve<TService>(out TService service);
}
```

规则：

- Resolver 只在组合根、Controller 工厂、迁移适配层中使用。
- View 不直接调用 Resolver。
- 框架核心不注册业务服务。
- Resolver 解析失败必须返回 false，不允许静默创建业务对象。

## 35. 资源加载、预热与池化

加载接口：

```csharp
public interface IUISurfaceLoader
{
    Task<GameObject> LoadAsync(UISurfaceEntry entry, CancellationToken cancellationToken);
    void Release(UISurfaceEntry entry);
}
```

核心内置加载器：

- `DirectReferenceSurfaceLoader`：使用 Catalog Prefab 引用。
- `ResourcesSurfaceLoader`：使用 Resources 路径。

Addressables 由适配程序集提供，不进入核心。

预热计划：

```csharp
[CreateAssetMenu(menuName = "AXR/UI Foundation/Preload Plan")]
public sealed class UIPreloadPlan : ScriptableObject
{
    public List<UISurfaceKey> preloadOnBoot;
    public List<UISurfaceKey> preloadOnSceneLoaded;
    public List<UISurfaceKey> preloadBeforeCombat;
}
```

池化规则：

- Page、Modal、Popup、Panel、Widget、Toast 支持池化。
- Tooltip 固定复用一个实例。
- 池满时销毁最旧的可销毁实例。
- 回收入池前调用 `Unbind`、`OnBeforeRecycle`、Kill 动画、解绑输入、取消跟随。
- 出池后调用 `OnAfterReuse`。

## 36. 诊断面板与操作记录

诊断服务：

```csharp
public interface IUIDiagnosticsService
{
    UISnapshot Capture();
    void Log();
    void ShowOverlay();
    void HideOverlay();
}
```

Snapshot 必须包含：

- Scope 列表。
- Canvas 模式。
- Layer Root 列表。
- Page 栈。
- Modal 栈。
- Popup 集合。
- 当前 Tooltip。
- Toast 队列。
- Panel Owner 树。
- Widget 池状态。
- 当前焦点。
- 正在执行的 Operation。
- 动画播放状态。
- 最近 100 条 UI 操作日志。

操作记录器：

```csharp
public interface IUIInteractionRecorder
{
    void StartRecording(string name);
    UIInteractionRecord StopRecording();
    Task ReplayAsync(UIInteractionRecord record, CancellationToken cancellationToken);
}
```

用途：

- 复现 UI Bug。
- 自动化 QA。
- 验证复杂 UI 流程。
- 录制教程步骤。

## 37. Editor 工具

必须提供以下 Editor 工具：

- `UIFoundationSettingsValidator`
- `UISurfaceCatalogValidator`
- `UIPrefabSurfaceValidator`
- `UILayerPreviewWindow`
- `UISurfaceRuntimeDebugger`
- `UIRouteGraphWindow`
- `UILocalizationPreviewWindow`
- `UIAnimationProfilePreviewWindow`

校验项：

- Catalog Key 重复。
- Prefab 缺少 `IUISurfaceView`。
- Prefab Root 不是 `RectTransform`。
- Modal 缺少默认 Selectable。
- Popup 缺少 Placement 支持。
- Tooltip 不在 Tooltip Layer。
- Panel 配置缺少 OwnerPolicy。
- Animation Profile 缺少 Show/Hide。
- 本地化文本 Key 缺失。
- 本地化图片 Key 缺失。
- 语言表 Key 重复。
- 支持语言缺少字体配置。

## 38. 测试规格

### 38.1 EditMode

必须覆盖：

- Key 唯一性。
- Layer 排序。
- Handle Version 失效。
- Page 栈规则。
- Modal 栈规则。
- Popup Owner 索引。
- Panel Slot Policy。
- Toast Merge Policy。
- Route Guard。
- Transaction 回滚。
- Operation 取消。
- Pool 上限。
- Placement 边界计算。
- Localization Key 回退。
- 命名参数格式化。
- Locale 持久化。

### 38.2 PlayMode

必须覆盖：

- Overlay Canvas 下 Page/Modal/Popup/Tooltip/Panel/Toast。
- Camera Canvas 下 Page/Modal/Popup/Tooltip/Panel/Toast。
- WorldSpace Scope 下 Tooltip 和 Popup。
- Modal 输入阻塞。
- Popup 外部点击关闭。
- Owner 关闭自动关闭子 Surface。
- Hide 动画完成后回收。
- Open 取消不留下半激活对象。
- Locale 切换更新已打开 UI。
- 缺失本地化 Key 进入 Diagnostics。
- RTL 语言下标记布局正确镜像。
- Reduced Motion 生效。
- 诊断 Overlay 能显示当前栈。

## 39. 最终验收标准

框架完成时必须满足：

- 所有 Surface 类型都由框架创建、关闭、定位、动画、回收、诊断。
- 业务代码不直接 Instantiate/Destroy 框架 Surface。
- Overlay、Camera、WorldSpace 三种 Canvas 模式可通过同一套 API 使用。
- Page、Modal、Popup、Tooltip、Panel、Widget、Toast 行为边界清晰。
- OpenAsync 与 CloseAsync 的完成时机与动画完成一致。
- 所有异步操作可取消，取消后状态确定。
- Popup 不需要业务手写遮罩。
- Tooltip 不使用 static Presenter。
- Panel 是框架管理的 Owner/Slot 子 Surface，不是无生命周期 Region 类。
- Modal 支持结果返回。
- Toast 支持队列、合并、优先级。
- Localization、Accessibility、Reduced Motion 对已打开 UI 生效。
- Diagnostics 能看到所有打开中和操作中的 UI。
- Interaction Recorder 能录制和回放基础 UI 操作。
- 核心框架不依赖具体业务类型。

## 40. 实现顺序

构建顺序固定为：

1. Core 类型：`UIScopeId`、`UISurfaceKey`、`UISurfaceId`、`UIHandle`、异常类型。
2. Settings：`UIFoundationSettings`、Scope、Layer、Catalog、Animation、Input、Localization。
3. Bootstrap：创建 Main Scope、Canvas、Layer、EventSystem。
4. Catalog 校验与 Loader。
5. Surface Factory 与 Pool。
6. Animation Service。
7. Operation Scheduler。
8. Page Service。
9. Modal Service 与 Mask Service。
10. Positioning Service。
11. Popup Service。
12. Tooltip Service。
13. Panel Service 与 Slot Host。
14. Widget Factory 与虚拟列表。
15. Toast Service。
16. Focus、OutsideClick、Back Input。
17. Transaction Service。
18. Route Guard。
19. Localization 与 Accessibility。
20. Diagnostics Overlay。
21. Interaction Recorder。
22. Editor Validators。
23. 旧 UI 适配层。

## 41. 示例

### 41.1 打开 Page

```csharp
UIHandle<ShopPageView> shop = await ui.Pages.OpenAsync<ShopPageView, ShopPagePayload>(
    new ShopPagePayload(shopService, inventoryService),
    new UIOpenOptions(),
    cancellationToken);
```

### 41.2 显示 Modal 并等待结果

```csharp
bool confirmed = await ui.Modals.ShowAsync<ConfirmModalView, ConfirmPayload, bool>(
    new ConfirmPayload("出售该武器？"),
    new UIModalOptions { CloseOnMaskClick = true },
    cancellationToken);
```

### 41.3 显示 Popup

```csharp
UIPopupPlacement placement = UIPopupPlacement.FromRectTransform(itemCell.Root)
    .WithOffset(new Vector2(12f, 0f))
    .WithFallback(UIPlacementFallback.BestFit)
    .WithClampToSafeArea(true);

await ui.Popups.ShowAsync<ItemActionPopupView, ItemActionPayload>(
    new ItemActionPayload(itemId),
    placement,
    new UIPopupOptions { Owner = inventoryPanel.Raw },
    cancellationToken);
```

### 41.4 显示 Tooltip

```csharp
ui.Tooltips.Show(
    itemDescription,
    UITooltipPlacement.FromScreenPoint(pointerPosition));
```

### 41.5 打开 Panel

```csharp
UIHandle<InventoryPanelView> inventory = await ui.Panels.OpenAsync<InventoryPanelView, InventoryPayload>(
    shop.Raw,
    "Shop.RightInventory",
    new InventoryPayload(inventoryService),
    new UIPanelOptions { SlotPolicy = UIPanelSlotPolicy.ReplaceExisting },
    cancellationToken);
```

### 41.6 Toast

```csharp
ui.Toasts.Enqueue(
    new ToastPayload("购买成功"),
    new UIToastOptions
    {
        Priority = UIToastPriority.Normal,
        MergePolicy = UIToastMergePolicy.ReplaceSameKey,
        Duration = 1.5f
    });
```

### 41.7 UI 事务

```csharp
await ui.Transactions.Begin("EnterShop")
    .Close(currentGameplay.Raw)
    .OpenPage<ShopPageView, ShopPagePayload>(new ShopPagePayload(shopService, inventoryService))
    .CommitAsync(cancellationToken);
```

### 41.8 切换语言

```csharp
localization.SetLocale(new UILocaleId("en-US"));
```

### 41.9 格式化本地化文本

```csharp
string text = localization.Format(
    "ui.shop.reroll.cost",
    new Dictionary<string, object>
    {
        { "cost", 25 },
        { "currency", "Gold" }
    });
```

## 42. 代码评审清单

新增或迁移 UI 时必须检查：

- Surface 分类正确。
- Catalog Key 正确。
- OwnerPolicy 正确。
- Layer 正确。
- ClosePolicy 正确。
- 输入策略正确。
- 动画 Profile 正确。
- Overlay、Camera、WorldSpace 坐标转换正确。
- 异步操作支持取消。
- 关闭时等待 Hide 动画。
- 回收前解绑事件。
- Handle 失效正确。
- Diagnostics 能看到该 Surface。
- Localization 与 Reduced Motion 生效。
- 可见文本使用本地化 Key。
- 本地化参数使用命名占位。
- 含文字图片进入本地化图片表。
- RTL 需要镜像的布局已标记。
- 没有 static Presenter。
- 没有业务手写遮罩。
- 没有直接 Instantiate/Destroy 框架 Surface。
- 没有框架层依赖业务类型。

## 43. 我认为必须加入的优秀功能

以下能力不是装饰，而是能显著降低长期 UI 维护成本：

- UI 流程事务：复杂切换失败时自动回滚，避免半开半关。
- UI Scope：支持多 Canvas、多相机、多玩家、World Space，不把 UIManager 写成单场景单例。
- 智能定位与 BestFit：Popup/Tooltip 自动翻转、夹取、跟随，解决边缘遮挡。
- Toast 通知中心：统一短提示、合并重复消息、支持优先级。
- 多语言与 Accessibility：文本、参数、字体回退、本地化图片、RTL、字体缩放、降低动效统一管理。
- Runtime Diagnostics Overlay：运行时直接看 Page 栈、Modal 栈、Popup、Panel Owner 树、动画和池状态。
- Interaction Recorder：录制并回放 UI 操作，用于复现 Bug、教程和自动化测试。
- Route Guard：UI 打开前统一做条件判断、重定向、延迟，避免业务到处写 if。
- 低 GC 虚拟列表：背包、商店、任务列表、图鉴等高频 UI 都能复用。
- Preload Plan：按场景和玩法阶段预热关键 UI，减少首次打开卡顿。

## 44. 最终原则

通用 UI 框架必须统一回答以下问题：

- 谁创建。
- 挂到哪。
- 谁拥有。
- 谁能关闭。
- 关闭是否等待动画。
- 输入被谁阻塞。
- 资源由谁释放。
- 异步由谁取消。
- 出错时如何定位。
- UI 状态如何复现。

只有这些问题全部由框架回答，Page、Modal、Popup、Tooltip、Panel、Widget、Toast 才真正属于同一套 UI 框架。
