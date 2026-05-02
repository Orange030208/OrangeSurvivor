# AXR UI Shell 通用 UI 框架设计文档

## 文档定位

本文档是后续重构 `AXR.Framework.UI` 的执行规格书。后续实现、迁移、测试和代码评审都应以本文档为准，除非新的需求明确修改本文档。

目标不是重新发明一套庞大的 UI 框架，而是在当前项目已有 `UIManager`、`UIPageBase`、`UIPrefabCatalog`、`UILayerDefinition`、`UIMotionPlayer`、`UISequenceDirector`、`UIScrollListBase` 的基础上，升级出一套支持 Page、Modal、Popup、Tooltip、Widget/Region 的通用 UI Shell。

本文档默认适用于 Unity 2022.3、UGUI、TextMeshPro、DOTween、NaughtyAttributes 以及当前项目的 `Assets/Scripts/Framework/UI` 目录结构。当前项目没有 UniTask 依赖，因此第一版异步方案不引入新包。

## 当前基线

### 已有能力

当前 UI 框架已经具备以下能力，后续重构必须尽量复用：

- `UIManager`：页面实例化、打开关闭、层级挂载、焦点切换、对象池回收。
- `IUIManager`：Page 级 API，包括 `OpenPage<TPage>`、`ClosePage<TPage>`、`ReplaceTopPage<TPage>`、`ResetToPage<TPage>`。
- `UIPageBase`：页面公共生命周期、`CanvasGroup` 状态、进入/退出动画接入。
- `UIPrefabCatalog` / `UIPrefabEntry`：Prefab 注册、是否单例、是否缓存、是否加入回退栈、预热数量。
- `UIFrameworkSettings`：Root Canvas 配置、层级配置、池化配置。
- `UILayerDefinition` / `UILayerType`：基础分层与 sorting order。
- `UITransitionSequence` / `UITransitionRunner`：关闭、打开、回调的串行过渡。
- `UIMotionPlayer` / `UISequenceDirector`：Motion V2 动画编排。
- `UIScrollListBase` / `UIScrollListItemBase`：手动布局滚动列表与列表项 reveal 动画。
- `UIClickTarget`：轻量点击事件组件。

### 当前痛点

当前框架的主要抽象单位是 Page，但实际项目里已经存在更多 UI 类型：

- Page：`GamingUIPage`、`ShopUIPage`、`MenuUIPage`、`GameOverUIPage`。
- Popup：`WeaponOperatePopup`、`AccessoryInfoPopup`。
- Tooltip：`UITooltipPresenter`、`TooltipHoverTarget`。
- Widget/Region：`InventoryUI`、`BuffBarUI`、`ShopListRegionView`、`GamingHudRegionHost`。
- Modal：暂停菜单、确认面板、系统提示面板等后续会自然出现的阻塞式 UI。

因此出现了这些问题：

- Popup 由业务脚本手动 `Instantiate` / `Destroy`，重复处理挂层、关闭遮罩、点击外部关闭、事件清理。
- Tooltip 自己处理 Canvas camera、坐标转换、屏幕夹取，后续 Overlay/Camera 模式并存时容易重复和出错。
- Modal 的遮罩、输入独占、下层输入冻结没有统一语义。
- Page 打开事件目前表示“已触发打开流程”，不等入场动画完成，异步语义不够清晰。
- 业务代码容易直接依赖 `UIManager.Instance`、具体 prefab 或具体页面类型，不利于测试和替换。
- `UIPrefabEntry` 只能表达 Page 风格配置，无法准确表达 Popup、Tooltip、Widget 的差异行为。

## 总体目标

AXR UI Shell 第一版必须实现以下目标：

- 同时支持 `ScreenSpaceOverlay` 和 `ScreenSpaceCamera` Canvas。
- 统一管理 Page、Modal、Popup、Tooltip、Widget/Region。
- 保留当前项目 UI 框架已有功能，不破坏现有页面和 Motion V2。
- 支持异步打开、异步关闭、等待动画完成、取消未完成操作。
- 通过接口解耦业务层与 UI 框架层。
- Popup 和 Tooltip 统一使用定位服务，支持屏幕点、RectTransform、世界坐标、目标跟随、边界夹取。
- Modal 统一管理遮罩、输入独占、点击遮罩关闭。
- 支持池化、预热、单例、多实例、回退栈。
- 保持 Inspector 友好、序列化稳定、Prefab 可检查。
- 以渐进迁移为原则，不一次性大规模重写所有 UI。

## 非目标

第一版不做以下事情：

- 不引入 UniTask、Addressables 或新的第三方 UI 框架。
- 不把所有业务事件迁移到全局 EventBus。
- 不强制把所有现有 Page 改名或重写。
- 不重写 Motion V2。
- 不重写 ScrollList。
- 不在框架底层写死商店、背包、武器、饰品、升级卡等具体业务规则。
- 不通过直接编辑 Prefab YAML 批量改序列化结构。

## 架构分层

### 项目层级

推荐后续目录结构：

```text
Assets/Scripts/Framework/UI/
  UIFrameworkConstants.cs
  Core/
    Data/
    Runtime/
      Async/
      Canvas/
      Layers/
      Placement/
      Services/
      Views/
      Navigation/
      UIMotion/
      ScrollList/
    Editor/

Assets/Scripts/UI/
  Contracts/
  Instances/
  Pages/
  Regions/
  Presentation/
```

`Framework/UI` 只放通用框架能力。  
`Scripts/UI` 放本项目具体业务 UI、页面控制器、Facade、Region、Presenter。

### 推荐模块

第一版控制在 6 个核心模块内：

1. `UIRootHost`
   - 负责 Root Canvas、CanvasScaler、GraphicRaycaster、EventSystem 检查、Overlay/Camera 模式。

2. `UILayerService`
   - 负责 Layer root 创建、排序、查找、输入层级策略。

3. `UIViewRegistry`
   - 负责从配置中根据类型解析 View 描述信息。

4. `UISurfaceService`
   - 负责统一打开、关闭、池化、生命周期、回退栈、异步操作。

5. `UIPlacementService`
   - 负责 Popup/Tooltip 坐标转换、边界夹取、跟随目标。

6. `UIInputBlockerService`
   - 负责 Modal/Popup 遮罩、点击外部关闭、输入独占。

`UIManager` 可以作为过渡期门面保留，并逐步将内部职责委托给这些服务。

## 核心概念

### UIViewKind

新增 View 类型枚举：

```csharp
namespace AXR.Framework.UI
{
    public enum UIViewKind
    {
        Page = 0,
        Modal = 1,
        Popup = 2,
        Tooltip = 3,
        Widget = 4
    }
}
```

语义：

- `Page`：占据主要界面区域，通常加入回退栈。
- `Modal`：阻塞式界面，默认带遮罩，默认冻结下层输入。
- `Popup`：临时浮层，可依附目标，通常可点击外部关闭。
- `Tooltip`：轻量提示层，通常单例复用，不阻塞输入。
- `Widget`：页面内部或全局常驻子 UI，默认不进入回退栈。

### UILayerType

推荐将层级扩展为：

```csharp
public enum UILayerType
{
    Background = 0,
    Hud = 1,
    Page = 2,
    Popup = 3,
    Modal = 4,
    Tooltip = 5,
    System = 6,
    Debug = 7
}
```

迁移期可保留旧值 `SceneOverlay`、`Default`。若为了兼容序列化不宜直接删改 enum，可先追加新值，并在 `UILayerDefinition` 中显式配置排序。

推荐 sorting order：

```text
Background: -300
Hud: -100
Page: 0
Popup: 200
Modal: 400
Tooltip: 600
System: 800
Debug: 1000
```

### UIViewDescriptor

替代或扩展 `UIPrefabEntry`：

```csharp
[Serializable]
public sealed class UIViewDescriptor
{
    public UIViewKind kind = UIViewKind.Page;
    public UILayerType layerType = UILayerType.Page;
    public GameObject prefab;
    public bool singleton = true;
    public bool cacheOnClose = true;
    public bool trackInBackStack = true;
    public int warmupCount;
    public int maxCachedInstancesOverride = -1;
    public UIInputPolicy inputPolicy = UIInputPolicy.Default;
    public UIClosePolicy closePolicy = UIClosePolicy.Default;
}
```

第一阶段可以让 `UIPrefabEntry` 增加这些字段，而不是立刻重命名所有资产。等迁移完成后再考虑改名。

### UIInputPolicy

```csharp
[Serializable]
public sealed class UIInputPolicy
{
    public bool blocksLowerLayers;
    public bool receivesInput = true;
    public bool createsBlocker;
    public bool blockerConsumesClick = true;
}
```

默认建议：

```text
Page: receivesInput=true, blocksLowerLayers=true, createsBlocker=false
Modal: receivesInput=true, blocksLowerLayers=true, createsBlocker=true
Popup: receivesInput=true, blocksLowerLayers=false, createsBlocker=按配置
Tooltip: receivesInput=false, blocksLowerLayers=false, createsBlocker=false
Widget: receivesInput=true, blocksLowerLayers=false, createsBlocker=false
```

### UIClosePolicy

```csharp
[Serializable]
public sealed class UIClosePolicy
{
    public bool closeOnOutsideClick;
    public bool closeOnEscape;
    public bool closeWhenOwnerClosed = true;
    public bool closeWhenSceneUnloaded = true;
}
```

默认建议：

```text
Page: closeOnEscape=false
Modal: closeOnEscape=true, closeOnOutsideClick=按配置
Popup: closeOnOutsideClick=true, closeOnEscape=true
Tooltip: closeOnOutsideClick=false, closeOnEscape=false
Widget: closeOnEscape=false
```

### UIOpenOptions

运行时打开参数，不写入资产：

```csharp
public sealed class UIOpenOptions
{
    public object Payload { get; private set; }
    public UIPlacement Placement { get; private set; }
    public Transform Owner { get; private set; }
    public bool? OverrideSingleton { get; private set; }
    public bool BringToFront { get; private set; } = true;
    public bool WaitForEnter { get; private set; } = true;

    public static UIOpenOptions WithPayload(object payload);
    public UIOpenOptions WithPlacement(UIPlacement placement);
    public UIOpenOptions WithOwner(Transform owner);
}
```

为兼容现有调用，`OpenPage<TPage>(object payload = null)` 内部可转换为：

```csharp
UIOpenOptions.WithPayload(payload)
```

### UIHandle

每个打开的 UI 返回一个句柄：

```csharp
public interface IUIHandle
{
    string InstanceId { get; }
    Type ViewType { get; }
    UIViewKind Kind { get; }
    bool IsAlive { get; }
    bool IsClosing { get; }

    void BringToFront();
    bool Close();
    IUIOperation CloseAsync();
}
```

句柄规则：

- 句柄不暴露具体对象池实现。
- 句柄关闭后 `IsAlive=false`。
- 重复关闭同一个句柄必须安全返回，不抛无意义异常。
- 业务层保存句柄时必须在自身 `OnDisable` / `OnDestroy` 清理。

## 异步模型

### 技术选择

第一版使用 Coroutine 驱动异步流程，不引入 UniTask。

原因：

- 当前项目没有 UniTask 依赖。
- 打开/关闭 UI 的异步主要是等待 DOTween、等待一两个帧、等待关闭动画完成，属于 Unity-bound 短流程。
- Coroutine 足够表达生命周期和取消，且与 `MonoBehaviour` 宿主天然兼容。

后续如项目正式引入 UniTask，可在不改变核心生命周期的前提下增加适配层：

```csharp
UniTask<IUIHandle> OpenTaskAsync<TView>(UIOpenOptions options, CancellationToken token)
```

但第一版不要把 UniTask 写进框架核心。

### IUIOperation

```csharp
public interface IUIOperation
{
    bool IsDone { get; }
    bool IsCancelled { get; }
    Exception Error { get; }
    event Action<IUIOperation> Completed;

    void Cancel();
    IEnumerator Wait();
}

public interface IUIOpenOperation<out TView> : IUIOperation
    where TView : Component, IUIView
{
    TView View { get; }
    IUIHandle Handle { get; }
}
```

操作规则：

- `Completed` 必须在成功、失败、取消时都触发一次。
- `Cancel()` 可重复调用。
- `Wait()` 在操作完成前持续 yield，完成后立即结束。
- 如果对象已销毁或 Root 正在销毁，操作应取消并给出可诊断日志。

### 生命周期接口

```csharp
public interface IUIView
{
    string InstanceId { get; }
    UIViewKind Kind { get; }
    bool IsOpen { get; }

    void Setup(UIViewSetupContext context);
    void OnOpen(UIOpenContext context);
    IEnumerator PlayEnter(UITransitionContext context);
    IEnumerator PlayExit(UITransitionContext context);
    void OnClose(UICloseContext context);
    void OnActivationChanged(UIActivationContext context);
    void OnTick(float deltaTime);
}
```

过渡期 `UIPageBase` 可以实现 `IUIView`，并保留现有 `IUIPage` 行为。

推荐基类：

```text
UIViewBase
  UIPageBase
  UIModalBase
  UIPopupBase
  UITooltipBase
  UIWidgetBase
```

第一阶段可以先只新增 `UIViewBase` 和 `UIPopupBase` / `UITooltipBase`，不要一次改完所有 Page。

### 打开流程

标准打开流程：

```text
1. 校验 TView 类型
2. 从 UIViewRegistry 解析 descriptor
3. 若 singleton 且已打开，返回已有 handle，并按 BringToFront 策略置顶
4. 从对象池取实例，或 Instantiate prefab
5. 挂到对应 layer root
6. 分配 instanceId
7. 创建 UIHandle
8. 注册 RuntimeState
9. 创建必要 blocker
10. 调用 view.Setup
11. 调用 view.OnOpen
12. 应用 Placement
13. 应用 Activation/Input 状态
14. 播放 Enter 动画
15. Enter 完成后标记 operation 完成
```

事件语义必须清晰拆分：

```text
Opening: 实例已创建，OnOpen 即将调用
Opened: OnOpen 已调用，但 Enter 可能未完成
EnterCompleted: Enter 动画完成
```

如果只保留一个旧事件 `PageOpened`，必须文档说明它的旧语义，并新增更准确的新事件，避免误用。

### 关闭流程

标准关闭流程：

```text
1. 校验 handle / instanceId
2. 若已关闭或正在关闭，安全返回
3. 标记 IsClosing
4. 关闭输入，避免重复点击
5. 关闭 owner 关联的子 popup / tooltip
6. 播放 Exit 动画
7. 调用 view.OnClose
8. 销毁或回收到对象池
9. 移除 RuntimeState
10. 移除 blocker
11. 重新计算顶层输入
12. 标记 operation 完成
```

事件语义：

```text
Closing: 关闭请求已接受，Exit 即将播放
Closed: OnClose 已调用，实例已从 runtime state 移除
```

### 取消规则

取消场景：

- 打开过程中被关闭。
- Root 被销毁。
- 场景卸载且该 View 不跨场景保留。
- 同一 singleton 被新的打开请求替换。
- TransitionRunner 被 ResetToPage 打断。

取消处理：

- 已创建实例但未完成打开时，必须走清理流程。
- 已注册事件必须解绑。
- 已创建 blocker 必须关闭。
- 正在播放的 DOTween 必须 Kill。
- operation 标记 `IsCancelled=true`，不应假装成功。

## 接口解耦

### 框架对外主接口

```csharp
public interface IUIService
{
    IUIOpenOperation<TView> OpenAsync<TView>(UIOpenOptions options = null)
        where TView : Component, IUIView;

    IUIHandle Open<TView>(UIOpenOptions options = null)
        where TView : Component, IUIView;

    bool Close(IUIHandle handle);
    IUIOperation CloseAsync(IUIHandle handle);

    bool CloseTop(UILayerType layerType);
    int CloseAll(UIViewKind kind);

    bool TryGetOpen<TView>(out TView view)
        where TView : Component, IUIView;
}
```

旧 `IUIManager` 保留为兼容门面：

```csharp
public interface IUIManager
{
    TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase;
    void ReplaceTopPage<TPage>(object payload = null) where TPage : UIPageBase;
    void ResetToPage<TPage>(object payload = null) where TPage : UIPageBase;
    bool ClosePage<TPage>() where TPage : UIPageBase;
}
```

实现上 `UIManager` 可以同时实现 `IUIManager` 和 `IUIService`。

### 内部服务接口

```csharp
public interface IUIViewRegistry
{
    bool TryGetDescriptor(Type viewType, out UIViewDescriptor descriptor);
}

public interface IUIViewFactory
{
    IUIView Create(UIViewDescriptor descriptor, Transform parent);
    void Release(IUIView view, UIViewDescriptor descriptor);
    void Warmup(IReadOnlyList<UIViewDescriptor> descriptors);
}

public interface IUILayerService
{
    Transform GetLayerRoot(UILayerType layerType);
    bool TryGetLayerRoot(UILayerType layerType, out Transform root);
    void BringToFront(Transform target);
}

public interface IUIPlacementService
{
    void Apply(RectTransform target, UIPlacement placement, UICanvasContext canvasContext);
}

public interface IUIInputBlockerService
{
    IUIHandle ShowBlocker(UIBlockerRequest request);
    void HideBlocker(string ownerInstanceId);
}
```

### 业务门面

业务层不应该在各处直接 `UIManager.Instance.Open<T>()`。复杂业务建议建立窄接口：

```csharp
public interface IInventoryPopupService
{
    IUIHandle ShowWeaponOperatePopup(InventoryItemOperateResource resource, UIPlacement placement);
    IUIHandle ShowAccessoryInfoPopup(InventoryItemOperateResource resource, UIPlacement placement);
    void CloseCurrentPopup();
}

public interface ITooltipService
{
    IUIHandle Show(IDescribable describable, UIPlacement placement);
    void HideCurrent();
}
```

`InventoryUI`、`ShopUIPage`、`GamingUIPage` 依赖这些业务门面或 `IUIService`，不要依赖具体 prefab 实例化细节。

## Canvas 与 Camera 支持

### UICanvasProfile

```csharp
[Serializable]
public sealed class UICanvasProfile
{
    public string rootName = "UIRoot";
    public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
    public Camera explicitCamera;
    public string cameraTag = "MainCamera";
    public float planeDistance = 10f;
    public int sortingOrder = 0;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public float matchWidthOrHeight = 0.5f;
    public bool dontDestroyOnLoad = true;
}
```

`UIFrameworkSettings` 持有 `UICanvasProfile`。

### UICanvasContext

运行时只读上下文：

```csharp
public readonly struct UICanvasContext
{
    public Canvas RootCanvas { get; }
    public RectTransform RootRect { get; }
    public Camera UICamera { get; }
    public RenderMode RenderMode { get; }
}
```

规则：

- Overlay 模式下 `UICamera == null`。
- Camera 模式下 `UICamera` 必须可解析，否则启动时报错。
- 所有坐标转换必须使用 `UICanvasContext`，业务脚本不得自己重复判断 `ScreenSpaceOverlay ? null : worldCamera`。

### Camera 解析

解析顺序：

```text
1. explicitCamera
2. Camera.main
3. GameObject.FindGameObjectWithTag(cameraTag).GetComponent<Camera>()
4. 抛出 MissingReferenceException
```

若使用 `Camera.main`，启动时缓存结果，不在热路径重复查找。

## Placement 定位系统

### UIPlacement

```csharp
public sealed class UIPlacement
{
    public UIPlacementMode Mode { get; }
    public Vector2 ScreenPosition { get; }
    public RectTransform AnchorRect { get; }
    public Vector3 WorldPosition { get; }
    public Transform FollowTarget { get; }
    public Vector2 Offset { get; }
    public Vector2 Padding { get; }
    public bool ClampToCanvas { get; }
    public bool FollowWhileOpen { get; }

    public static UIPlacement AtScreen(Vector2 screenPosition);
    public static UIPlacement AtRect(RectTransform anchorRect);
    public static UIPlacement AtWorld(Vector3 worldPosition);
    public static UIPlacement Follow(Transform target);
    public UIPlacement WithOffset(Vector2 offset);
    public UIPlacement WithPadding(Vector2 padding);
    public UIPlacement WithClamp(bool clampToCanvas = true);
}
```

### UIPlacementMode

```csharp
public enum UIPlacementMode
{
    None = 0,
    ScreenPoint = 1,
    RectTransform = 2,
    WorldPoint = 3,
    FollowTransform = 4
}
```

### 坐标转换规则

`UIPlacementService` 负责：

- `ScreenPoint` 转 parent local。
- `RectTransform` 根据目标 Rect 计算屏幕中心或指定锚点。
- `WorldPoint` 使用 `Camera.WorldToScreenPoint` 再转 UI local。
- `FollowTransform` 每帧或 LateUpdate 更新位置，直到 handle 关闭。
- 根据目标 `RectTransform.rect.size`、pivot、padding 进行 Canvas 内夹取。

Tooltip 和 Popup 不再自己实现坐标转换。

### 热路径注意

跟随目标的 Popup/Tooltip 才允许每帧更新位置。普通静态 Popup 打开时定位一次即可。

## Page 规则

Page 是当前框架已有能力，迁移时优先兼容。

Page 默认：

- `kind = Page`
- `layerType = Page`
- `singleton = true`
- `trackInBackStack = true`
- `cacheOnClose = true`
- `blocksLowerLayers = true`
- 不自动创建 blocker

旧 API：

```csharp
OpenPage<TPage>(payload)
ClosePage<TPage>()
ReplaceTopPage<TPage>(payload)
ResetToPage<TPage>(payload)
```

保留，但内部转发到 `IUIService`。

## Modal 规则

Modal 用于确认框、暂停菜单、系统提示等。

Modal 默认：

- `kind = Modal`
- `layerType = Modal`
- `singleton = false` 或按配置
- `trackInBackStack = true`
- `createsBlocker = true`
- `blocksLowerLayers = true`
- `closeOnEscape = true`

Modal blocker：

- 优先使用配置好的 blocker prefab。
- blocker 应位于 Modal layer 内，作为目标 Modal 的前一个 sibling。
- blocker 点击是否关闭由 `UIClosePolicy.closeOnOutsideClick` 决定。
- blocker 颜色和 alpha 来自配置，不在代码里硬编码魔法值。

## Popup 规则

Popup 用于物品操作面板、下拉菜单、右键菜单、局部浮层。

Popup 默认：

- `kind = Popup`
- `layerType = Popup`
- `singleton = false`，但同类型可按需求设为 true。
- `trackInBackStack = false`。
- `cacheOnClose = true`。
- `closeOnOutsideClick = true`。
- `blocksLowerLayers = false`，除非该 Popup 明确需要独占输入。

Popup 打开必须带 `UIPlacement`，否则使用 prefab 当前锚点位置，并输出一次可诊断警告。

`InventoryPopupHostView` 的目标迁移结果：

- 不再手动创建 `InventoryOperatePopupCloseMask`。
- 不再手动 `Instantiate` / `Destroy` `WeaponOperatePopup` 和 `AccessoryInfoPopup`。
- 保存 `IUIHandle currentPopupHandle`。
- 通过 `IInventoryPopupService` 或 `IUIService` 打开 Popup。
- Sell/Merge 事件仍由 `WeaponOperatePopup` 暴露，但生命周期由框架统一关闭和解绑。

## Tooltip 规则

Tooltip 用于轻量信息提示。

Tooltip 默认：

- `kind = Tooltip`
- `layerType = Tooltip`
- `singleton = true`
- `trackInBackStack = false`
- `cacheOnClose = true`
- `receivesInput = false`
- `blocksLowerLayers = false`
- 不创建 blocker

Tooltip 行为：

- 可复用单实例。
- 支持跟随鼠标、触摸点、RectTransform、World Position。
- 默认 Clamp 到 Canvas。
- 隐藏时不销毁，优先回收到池或保持单例 inactive。

`UITooltipPresenter` 的目标迁移结果：

- 只负责渲染 `IDescribable`。
- 不再持有 `ActivePresenter` 静态全局入口。
- 不再自行解析 parent canvas camera。
- 不再自行做屏幕夹取。
- Tooltip 显示隐藏由 `ITooltipService` 负责。

`TooltipHoverTarget` 的目标迁移结果：

- 只负责把 pointer 事件转换成 `ITooltipService.Show/Hide`。
- `dataSourceComponent` 仍可保留，但显示逻辑不直接找 `UITooltipPresenter`。

## Widget / Region 规则

Widget/Region 是页面内子 UI 或全局常驻小 UI。

默认：

- 不进入回退栈。
- 通常不由 `UISurfaceService` 全局打开，除非需要跨页面复用。
- 页面内部 Region 仍可由页面直接持有引用和 Bind/Unbind。

不要把所有子元素都强行纳入全局 UIService。只有具备独立生命周期、独立复用、跨页面或需要全局定位/池化的 Widget 才进入 UI Shell。

## 输入与激活规则

### Activation

运行时对每个 View 维护：

```text
VisualActive: 是否可见
InputActive: 是否接收输入
Focused: 是否为当前焦点 UI
Closing: 是否正在关闭
```

基础规则：

- 正在关闭的 View 不接收输入。
- Tooltip 默认不接收输入。
- 顶层 Modal 接收输入，并冻结其下方 View 输入。
- Popup 是否冻结下方输入由配置决定。
- Page 切换时，只有顶层 Page 或 Modal 默认接收输入。

### CanvasGroup

所有受框架管理的 View 根节点应有 `CanvasGroup`。  
`UIViewBase` 可通过 `[RequireComponent(typeof(CanvasGroup))]` 保证。

设置规则：

```text
VisualActive=false: alpha=0, interactable=false, blocksRaycasts=false
InputActive=false: alpha保持, interactable=false, blocksRaycasts=false
InputActive=true: interactable=true, blocksRaycasts=true
```

Tooltip 这类不接收输入的 View 即使可见也应 `blocksRaycasts=false`。

## 对象池规则

池化对象：

- Page：按配置。
- Modal：常用确认框可池化。
- Popup：建议池化。
- Tooltip：建议单例或池化。
- Widget：按需求。

回收前必须：

- Kill Motion。
- 停止未完成 Coroutine。
- 解绑框架事件。
- 调用业务 `OnClose`。
- 清理 `owner`、payload、placement、handle。
- 设置 inactive。
- 回到对应 layer root 或 pool root。

不要把带有未解绑业务事件的实例直接入池。

## 与 Motion V2 的关系

UI Shell 不重写 Motion V2，只规定接入点：

- View 打开时调用 `PlayEnter`。
- View 关闭时调用 `PlayExit`。
- `UIViewBase` 可继续自动查找 `UISequenceDirector`。
- 如果没有 `UISequenceDirector`，打开/关闭应立即完成。
- 如果 DOTween 被 Kill，异步操作必须能完成或取消，不能永久等待。

建议 `UIViewBase` 兼容现有 `UIPageBase` 的关闭等待管线。

## Runtime State

当前 `UIRuntimeState` 只维护 Page type 到 instance stack 和 backStack。后续应升级为：

```csharp
public sealed class UIRuntimeViewState
{
    public string InstanceId { get; }
    public Type ViewType { get; }
    public UIViewKind Kind { get; }
    public UILayerType LayerType { get; }
    public IUIView View { get; }
    public IUIHandle Handle { get; }
    public UIViewDescriptor Descriptor { get; }
    public Transform Owner { get; }
    public bool IsClosing { get; }
}
```

运行时索引：

```text
Dictionary<string, UIRuntimeViewState> openedByInstance
Dictionary<Type, Stack<string>> openedByType
Dictionary<UILayerType, Stack<string>> focusedByLayer
Stack<string> backStack
Dictionary<string, List<string>> childrenByOwnerInstance
```

Owner 规则：

- Popup/Tooltip 可绑定 owner。
- Owner 关闭时，默认关闭其 children。
- 页面关闭时，应关闭该页面打开的临时 Popup/Tooltip。

## 兼容与迁移策略

### 第一阶段：基础能力补齐

新增：

- `UIViewKind`
- `UIViewBase`
- `IUIView`
- `IUIService`
- `IUIHandle`
- `IUIOperation`
- `UIOpenOptions`
- `UIPlacement`
- `UIPlacementService`
- `UICanvasProfile`
- `UICanvasContext`

改造：

- `UIManager` 继续可用，同时实现或持有 `IUIService`。
- `UIFrameworkSettings` 增加 Canvas profile、默认 layer、blocker 配置。
- `UIPrefabEntry` 增加 `kind`、`inputPolicy`、`closePolicy` 字段，保留旧字段名。

验收：

- 现有 Page 打开关闭行为不回退。
- Overlay 模式正常。
- Camera 模式可通过配置启动。
- 旧 `OpenPage<T>` 调用无需改动。

### 第二阶段：Popup/Tooltip 收编

改造：

- `InventoryPopupHostView`
- `WeaponOperatePopup`
- `AccessoryInfoPopup`
- `UITooltipPresenter`
- `TooltipHoverTarget`

目标：

- Popup 不再由业务手动 Instantiate/Destroy。
- Tooltip 不再使用静态 `ActivePresenter`。
- 关闭遮罩由 `UIInputBlockerService` 统一管理。
- 坐标转换全部走 `UIPlacementService`。

验收：

- 背包中点击武器显示操作 Popup。
- 点击外部关闭 Popup。
- Sell/Merge 事件仍正常。
- Tooltip 在 Overlay 和 Camera 模式下位置正确。
- Tooltip 不挡住下层点击。

### 第三阶段：Modal 与输入策略

新增：

- `UIModalBase`
- blocker prefab/config
- `UIInputBlockerService`

改造：

- 暂停菜单或确认框优先迁移为 Modal。

验收：

- Modal 打开后下层 Page 不再接收输入。
- Modal 关闭后下层 Page 恢复输入。
- 遮罩点击关闭按配置生效。
- Escape 关闭按配置生效。

### 第四阶段：异步语义完善

改造：

- `UITransitionRunner`
- `UIPageBase` / `UIViewBase` 的 `PlayEnter`、`PlayExit`
- `OpenAsync`、`CloseAsync`

验收：

- 可等待 Page 退出动画完成后再打开新 Page。
- 快速连续打开/关闭不会留下孤儿 blocker、孤儿 popup 或无效 tween。
- 关闭中的 UI 不再响应重复点击。

### 第五阶段：清理旧 API 和文档更新

仅在前四阶段稳定后进行：

- 标记旧接口为兼容层。
- 更新 Prefab Catalog 命名。
- 补充 Editor 工具辅助检查配置。
- 补充测试和使用示例。

## 测试策略

### EditMode 测试

优先测试纯 C# 逻辑：

- `UIRuntimeState` 注册、移除、回退栈。
- `UIViewRegistry` 类型解析。
- `UIPlacementService` 的 clamp 计算，可将核心计算拆成纯函数。
- `UIOperation` 完成、取消、重复取消。
- `UIClosePolicy` / `UIInputPolicy` 默认值。

### PlayMode 测试

涉及 Unity 生命周期和 UGUI 的行为用 PlayMode：

- `OpenAsync` 等待 Enter 完成。
- `CloseAsync` 等待 Exit 完成。
- Pool reuse。
- Canvas Overlay / Camera 坐标转换。
- Modal blocker 阻挡点击。
- Tooltip 不阻挡点击。
- Owner 关闭时子 Popup/Tooltip 自动关闭。

### 手动验证

每次完成一个阶段后，在 Unity Editor 验证：

- 主菜单到游戏界面。
- 游戏界面打开暂停菜单。
- 商店界面打开背包侧栏。
- 背包点击武器 Popup。
- 点击外部关闭 Popup。
- 鼠标/触摸 Tooltip。
- 切换 Canvas RenderMode 后重复以上流程。

## 性能约束

- 不在每帧执行 `FindObjectOfType`、`Camera.main`、`GetComponentsInChildren` 等查找。
- Tooltip/Popup 跟随目标时才启用每帧定位。
- 频繁打开的 Popup/Tooltip 必须支持池化或单例复用。
- 打开关闭期间避免重复 `LayoutRebuilder.ForceRebuildLayoutImmediate`，只有内容变化后必要时调用。
- `ApplyActivationForAllViews` 应只遍历当前打开 UI，不扫描场景。
- 对象池最大数量受配置限制，避免长期积累。

## 错误与日志规范

框架层错误必须可定位：

- 缺少 settings：抛 `MissingReferenceException`，包含 `UIManager` 名称。
- 缺少 catalog entry：抛 `KeyNotFoundException`，包含 View 类型全名。
- Prefab 缺少组件：抛 `InvalidOperationException` 或 `MissingComponentException`，包含 prefab 名称和预期组件。
- Camera 模式缺少 camera：抛 `MissingReferenceException`，说明解析顺序。
- Placement 无效：输出 warning，并降级到当前 prefab 位置。

不要静默失败，除非是重复关闭、重复取消这种可预期幂等操作。

## 编码规则

后续实现必须遵守：

- 命名空间沿用 `AXR.Framework.UI`。
- 框架层不引用具体业务类型。
- 业务层不直接访问框架内部 runtime dictionaries。
- 新增配置优先放入 ScriptableObject 或 Serializable 配置类。
- 不破坏已有序列化字段名；必要时使用 `[FormerlySerializedAs]`。
- 不在运行时隐式给业务 Prefab 补组件；缺依赖时明确报错。
- Root Canvas 和 Layer root 属于框架显式装配流程，可由框架创建。
- 新增接口必须有真实替换点，不为每个类机械加接口。
- 复杂流程使用守护子句和小方法拆分。
- Dispose 主要用于纯 C# 生命周期；MonoBehaviour 仍使用 OnEnable/OnDisable/OnDestroy 对称订阅。

## 实用必备能力补充

以下能力属于 UI Shell 的实用基础设施。它们不是第一批接口的最小闭环，但必须纳入整体重构计划，否则后续 Page、Popup、Tooltip、Modal 越迁移越容易出现隐藏成本。

### UI 配置校验器

必须新增 Editor 侧校验工具，例如：

```text
Assets/Scripts/Framework/UI/Core/Editor/UIFrameworkValidator.cs
```

校验目标：

- `UIFrameworkSettings` 是否存在且字段完整。
- Canvas Profile 在 Camera 模式下是否能解析 camera。
- Layer 配置是否包含所有被 Catalog 使用的 `UILayerType`。
- Layer sorting order 是否重复或明显反向。
- Catalog 是否存在重复注册的 View 类型。
- Catalog entry 是否缺 prefab。
- Prefab 根节点是否有 `UIViewBase` 或兼容的 `UIPageBase`。
- Prefab 根节点是否有 `CanvasGroup`。
- Popup/Tooltip prefab 根节点是否有 `RectTransform`。
- Modal 配置需要 blocker 时是否存在 blocker prefab 或默认 blocker 配置。
- Tooltip 是否配置为不阻挡输入。
- `cacheOnClose=true` 时 prefab 是否能安全重置生命周期。
- Motion 引用是否缺失。

工具形式：

- `Tools/Survivors/UI/Validate UI Framework`
- `UIFrameworkSettings` Inspector 上的 `Validate` 按钮。
- 输出可定位的 error/warning 列表。

规则：

- 校验器只报告问题，不偷偷修改 Prefab。
- 可额外提供显式修复按钮，但必须在 Editor 下由用户主动点击。
- 运行时仍要保留必要校验，不能只依赖 Editor 工具。

验收标准：

- 新增或迁移 UI prefab 后，可以一键发现缺组件、缺层级、重复注册、Camera 配置错误。
- 校验输出包含 asset path、prefab name、component name、字段名。

### UIScope / UIHandleGroup

必须新增 Scope 能力，用于管理某个页面或业务流程打开的临时 UI、异步操作和子句柄。

推荐接口：

```csharp
public interface IUIScope : IDisposable
{
    string OwnerInstanceId { get; }
    bool IsDisposed { get; }

    void Track(IUIHandle handle);
    void Track(IUIOperation operation);
    bool Untrack(IUIHandle handle);
    void CloseAll();
    void CancelAllOperations();
}

public interface IUIScopeFactory
{
    IUIScope CreateScope(string ownerInstanceId);
}
```

使用场景：

- Page 打开 Popup、Tooltip、Loading 时，把这些 handle 挂到 Page scope。
- Page 关闭时，scope 自动关闭所有子 UI。
- 异步打开尚未完成时，scope dispose 自动取消 operation。

规则：

- Scope 不拥有业务对象，只拥有 UI handle 和 UI operation。
- Scope 的 `CloseAll` 必须幂等。
- Scope dispose 后继续 Track 应抛出可诊断异常或输出明确错误。
- `UIRuntimeState` 应记录 owner -> children 关系，Scope 是业务层更好用的封装。

验收标准：

- 关闭 `ShopUIPage` 时，它打开的物品 Popup 自动关闭。
- 快速关闭页面时，不留下 Tooltip、Popup、Blocker 或未完成 coroutine。

### Back / Cancel 输入路由

必须新增统一返回/取消输入路由，避免每个页面各自监听 Escape、Android 返回键、手柄 B。

推荐接口：

```csharp
public interface IUIBackHandler
{
    bool CanHandleBack();
    bool HandleBack();
}

public interface IUIBackNavigationService
{
    bool HandleBackRequest();
}
```

默认处理顺序：

```text
1. 顶层 Tooltip：默认不处理
2. 顶层 Popup：若 closeOnEscape=true，则关闭
3. 顶层 Modal：若 closeOnEscape=true，则关闭或交给 IUIBackHandler
4. 顶层 Page：若实现 IUIBackHandler，则调用
5. BackStack：关闭顶层可回退 Page
6. 没有可处理对象：返回 false
```

输入来源：

- Keyboard Escape。
- Android Back。
- Gamepad Cancel / B。
- 后续输入系统接入的取消事件。

规则：

- `UIBackNavigationService` 只负责路由，不直接知道具体业务。
- Page 如需自定义返回行为，实现 `IUIBackHandler`。
- 正在关闭的 View 不处理 Back。
- Modal 优先级高于 Page。

验收标准：

- Popup 打开时按 Escape 先关 Popup，不直接关 Page。
- Modal 打开时按 Escape 按配置关闭 Modal。
- 没有 Popup/Modal 时，Page 的返回逻辑生效。

### Safe Area 与屏幕适配

必须预留并逐步实现 Safe Area 支持，尤其面向移动端横屏。

推荐组件：

```csharp
public sealed class UISafeAreaAdapter : MonoBehaviour
{
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool updateWhenScreenChanges = true;
    [SerializeField] private bool applyHorizontal = true;
    [SerializeField] private bool applyVertical = true;
}
```

推荐服务：

```csharp
public interface IUIScreenMetricsService
{
    Rect SafeArea { get; }
    Vector2Int ScreenSize { get; }
    event Action ScreenMetricsChanged;
}
```

规则：

- HUD、Page 根、Popup 层是否应用 Safe Area 应可配置。
- Tooltip/Popup clamp 时应可选择 clamp 到 Canvas rect 或 safe area rect。
- 屏幕分辨率、方向、安全区变化时刷新适配。
- 不要把 safe area 偏移写死在业务 UI 脚本里。

验收标准：

- 模拟不同分辨率时，HUD 不贴到刘海/圆角不可见区域。
- Popup/Tooltip 的 clamp 可选择避开 safe area。

### Runtime Diagnostics

必须把现有 `UIManager.LogRuntimeDiagnostics()` 升级成标准诊断能力。

推荐能力：

- 打印当前打开的 View 列表。
- 打印 instanceId、type、kind、layer、owner、isClosing、inputActive、visualActive。
- 打印 backStack。
- 打印每个 layer 当前顶层对象。
- 打印 pool count。
- 打印正在执行的 operation。
- 打印 blocker 所属 owner。

推荐接口：

```csharp
public interface IUIDiagnosticsService
{
    string BuildSummary();
    void LogSummary(UnityEngine.Object context = null);
}
```

Editor 可选增强：

- `Tools/Survivors/UI/Log Runtime Diagnostics`
- 自定义 Inspector 按钮。
- 简单 Runtime Debug Overlay，仅 Debug 构建或 Editor 可见。

验收标准：

- 遇到 UI 卡死、点击无响应、Popup 不消失时，可以通过一次日志定位当前 UI 状态。

### 焦点与 Selectable 导航

必须预留键盘/手柄焦点能力，避免 Modal 迁移后无法操作。

推荐接口：

```csharp
public interface IUIFocusTarget
{
    GameObject GetDefaultSelectedObject();
}

public interface IUIFocusService
{
    void PushFocus(IUIView view);
    void PopFocus(IUIView view);
    void RestorePreviousFocus();
}
```

规则：

- Modal 打开后优先选中 `IUIFocusTarget.GetDefaultSelectedObject()`。
- Modal 关闭后恢复之前选中的对象。
- Page 打开后可选择是否设置默认焦点。
- Tooltip 不参与焦点。
- Popup 默认不抢焦点，除非配置要求。

验收标准：

- 暂停菜单打开后，手柄/键盘可以直接操作默认按钮。
- 关闭 Modal 后，焦点回到原来的 Page 控件。

### Loading / Busy 状态

必须提供轻量 Busy 能力，防止异步打开、关闭、加载期间重复点击。

推荐接口：

```csharp
public interface IUIBusyService
{
    IUIHandle ShowBusy(UIBusyOptions options);
    void HideBusy(IUIHandle handle);
}

public sealed class UIBusyOptions
{
    public bool BlocksInput { get; set; } = true;
    public string Message { get; set; }
    public float DelayBeforeShow { get; set; } = 0.15f;
}
```

规则：

- 短操作默认延迟显示，避免闪一下。
- Busy 可以只是透明 blocker，也可以是 Loading Modal。
- Busy handle 必须和 operation/scope 联动，operation 完成或取消时自动关闭。

验收标准：

- 连续点击打开同一 Modal，不会重复创建多个实例。
- 页面切换动画期间，下层按钮不会重复触发。

### Prefab 自动装配辅助

必须提供 Editor 辅助，但运行时不偷偷修 Prefab。

推荐工具：

- `Validate Selected UI Prefab`
- `Prepare Selected UI Prefab`
- `Add Missing CanvasGroup`
- `Add UIViewBase Compatible Components`
- `Create Default Blocker Prefab`

规则：

- 修复操作必须只在 Editor 下执行。
- 操作前输出将要修改的对象和组件。
- 对已序列化字段不随意重命名。
- 批量操作必须可预览。

验收标准：

- 新建一个 Popup prefab 后，可以通过工具快速补齐根节点必要组件。
- 运行时不发生隐式 `AddComponent` 破坏序列化稳定性。

## 推荐 API 示例

### 打开页面

```csharp
uiService.Open<GamingUIPage>(
    UIOpenOptions.WithPayload(gamingContext));
```

### 异步切换页面

```csharp
IUIOperation closeOperation = uiService.CloseAsync(currentHandle);
yield return closeOperation.Wait();

IUIOpenOperation<ShopUIPage> openOperation = uiService.OpenAsync<ShopUIPage>(
    UIOpenOptions.WithPayload(shopContext));
yield return openOperation.Wait();
```

### 显示 Popup

```csharp
IUIHandle handle = uiService.Open<WeaponOperatePopup>(
    UIOpenOptions
        .WithPayload(resource)
        .WithPlacement(UIPlacement.AtScreen(pointerPosition).WithClamp()));
```

### 显示 Tooltip

```csharp
tooltipService.Show(
    describable,
    UIPlacement.AtScreen(eventData.position)
        .WithOffset(new Vector2(18f, -18f))
        .WithPadding(new Vector2(12f, 12f))
        .WithClamp());
```

### 显示 Modal

```csharp
IUIOpenOperation<ConfirmModal> operation = uiService.OpenAsync<ConfirmModal>(
    UIOpenOptions.WithPayload(confirmContext));
yield return operation.Wait();
```

## 后续重构完成标准

当以下条件全部满足，才认为 UI Shell 重构完成：

- 当前所有 Page 仍能通过旧 API 正常打开关闭。
- 新 `IUIService` 可打开 Page、Modal、Popup、Tooltip。
- Overlay 和 Camera 模式都能正常定位 Tooltip/Popup。
- Popup 支持点击外部关闭，不需要业务手动创建遮罩。
- Modal 支持遮罩、输入独占、关闭后恢复下层输入。
- Tooltip 支持单例复用、边界夹取、不阻挡点击。
- 打开和关闭可异步等待动画完成。
- 快速连续打开关闭没有残留实例、残留遮罩、残留 tween、残留事件订阅。
- 业务侧至少完成 `InventoryPopupHostView` 和 `UITooltipPresenter` 的迁移。
- 有覆盖核心状态和异步操作的 EditMode 或 PlayMode 测试。
- 文档中的 API 示例与实际代码一致。

## 重要决策记录

- 第一版不引入 UniTask，使用 Coroutine operation。
- 保留 `UIManager`，将其从 Page Manager 渐进升级为 UI Shell 门面。
- 不强制全局管理所有子元素，只有独立生命周期的 UI 才进入 UI Shell。
- Popup/Tooltip 是第一优先级，因为它们当前重复逻辑最多、Canvas 模式风险最高。
- Motion V2 保留并作为进入/退出动画底座。
- 配置迁移优先追加字段，不破坏已有资产。

## 风险与应对

### 风险：enum 修改破坏已有序列化

应对：迁移期只追加新值，不重排旧值；必要时保留旧名字并在配置中映射到新语义。

### 风险：异步关闭与对象池交错导致事件残留

应对：关闭流程必须集中在 `UISurfaceService`，回收前统一 Kill、Unbind、Clear owner、Clear handle。

### 风险：业务仍绕过服务手动 Instantiate Popup

应对：先迁移 `InventoryPopupHostView`，并在文档和代码注释中明确 Popup 统一由服务创建。

### 风险：Camera 模式坐标偏移

应对：所有位置计算走 `UICanvasContext`；添加 PlayMode 验证 Overlay 和 Camera 两种模式。

### 风险：框架过度抽象影响迭代

应对：第一版只抽象有真实痛点的 Page/Modal/Popup/Tooltip/Widget、异步、Placement、Blocker，不引入主题系统、布局 DSL、Addressables 等额外能力。
