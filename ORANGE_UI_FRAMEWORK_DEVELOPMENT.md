# OrangeUIFramework 开发文档

## 1. 文档目标

OrangeUIFramework 是面向 Unity 2022.3、UGUI、TextMeshPro 的通用 UI 框架方案。目标不是重写一个复杂“大而全”的 UI 平台，而是解决当前项目 UI 的真实痛点：

- 同时支持 `ScreenSpaceOverlay` 和 `ScreenSpaceCamera` 两种 Canvas 模式。
- 统一管理 Page、Popup、Modal、Tooltip 与页面内部子视图。
- 保留当前 UI 框架已有的 Prefab Catalog、层级、池化、页面生命周期、页面切换和 UIMotion 动画能力。
- 补齐异步打开、关闭、加载、动画等待、取消和 Modal 返回结果。
- 支持多语言文本、参数化文本、语言切换刷新和字体适配。
- 只在资源、目录、命名空间、菜单路径等框架边界使用 `Orange`；类名、字段名、函数名保持自然简洁，避免影响业务开发体验。
- 移除或下沉当前 `Region`、`Contract` 一类没有接入框架核心的无效抽象，让页面内部结构更直接。

本文是开发与迁移蓝图，不要求一次性完成所有功能。实现时应按“先核心闭环，再迁移页面，再补编辑器体验”的顺序推进。

## 2. 当前项目基线

- Unity 版本：`2022.3.62f3c1`。
- UI 技术栈：UGUI、TextMeshPro、DOTween。
- 异步技术栈：OrangeUIFramework 使用 UniTask；当前 `Packages/manifest.json` 已接入 `com.cysharp.unitask`。
- 渲染管线：URP 14。
- 当前没有发现项目级 `asmdef`，不应强行引入程序集拆分；如果后续引入，应先拆 Runtime / Editor / Tests。
- 当前 UI 框架核心位于 `Assets/Scripts/Framework/UI/Core`，命名空间为 `AXR.Framework.UI`。
- 当前业务 UI 位于 `Assets/Scripts/UI`，已有 Shop、Inventory、Tooltip、Sidebar 等页面和子结构。

当前可保留的能力：

- `UIFrameworkSettings`：Root Canvas、层级、池化配置。
- `UIPrefabCatalog` / `UIPrefabEntry`：Prefab 注册、单例、缓存、预热。
- `UIManager`：打开、关闭、替换、重置、BackStack。
- `UIPageBase`：CanvasGroup、生命周期、关闭等待管线、SequenceDirector 接入。
- `UITransitionSequence`：链式关闭、打开、回调编排。
- `UIMotionDefinition` / `UIMotionPlayer` / `UISequenceDirector`：可配置 UI 动画系统。

当前主要不足：

- Canvas 配置只写了 `RenderMode`，没有完整处理 Camera 模式的 `worldCamera`、`planeDistance`、相机来源、Root Canvas 复用规则。
- 框架只真正管理 Page，没有通用 Popup、Modal、Tooltip、页面子视图的生命周期和层级规则。
- `Region` / `Contract` 目录多数是业务页面内部组织，没有被框架核心识别；不应继续作为框架抽象扩展。
- 当前异步语义只体现在关闭等待动画，没有统一 UniTask 版本的 `OpenAsync`、`CloseAsync`、取消、加载等待、Modal 返回结果。
- `UIManager.Update` 对所有打开页面逐帧调用 `HandleTick`，应改为只有实现显式 Tick 能力的视图才进入 Tick 列表。
- Tooltip 依赖静态 `ActivePresenter` 和运行时 `FindFirstObjectByType` 兜底，容易隐藏装配错误，不适合作为框架级方案。
- `UIMotionPlayer.refreshDefaultsOnEnable` 现在不会在每次重新启用时真正刷新默认快照，容易导致池化页面复用后动画起点不准确。
- `SidebarMotion.ConfigureTimings` 是空实现，应删除或接入真正的 Motion 配置，避免误导维护者。
- Catalog 启动时缺少完整校验，Prefab 不含页面组件时可能在运行时空引用。

## 3. 命名与目录约定

框架名称：`OrangeUIFramework`。

允许使用 `Orange` 的位置：

- 根目录或包目录：`Assets/Scripts/OrangeUIFramework/`
- 资源目录：`Assets/ScriptableObjects/OrangeUIFramework/`
- Prefab 目录：`Assets/Prefabs/UI/OrangeUIFramework/`
- 命名空间：`Orange.UIFramework`
- 创建菜单：`Orange/UI Framework/...`
- 资源文件名：`OrangeUIFrameworkSettings.asset`、`OrangeUIViewCatalog.asset`

不应使用 `Orange` 的位置：

- 不要命名为 `OrangeUIManager`、`OrangePageBase`、`OrangePopupBase`。
- 不要命名字段 `orangeSettings`、`orangeCatalog`。
- 不要命名函数 `OpenOrangePageAsync`。

推荐类名：

- `UIManager`
- `IUIManager`
- `UIRoot`
- `ViewBase`
- `PageBase`
- `PopupBase`
- `ModalBase`
- `TooltipBase`
- `ViewPartBase`
- `ViewCatalog`
- `ViewDefinition`
- `UIFrameworkSettings`
- `CanvasProfile`
- `LayerDefinition`
- `ViewHandle`
- `OpenContext`
- `CloseReason`
- `ModalResult<T>`
- `LocalizedText`
- `LocalizationTable`

推荐目录：

```text
Assets/Scripts/OrangeUIFramework/
  Core/
    Runtime/
    Data/
    Loading/
    Navigation/
    Interaction/
  Animation/
    Runtime/
    Data/
  Localization/
    Runtime/
    Data/
  Editor/
  Examples/

Assets/ScriptableObjects/OrangeUIFramework/
  Settings/
  Catalogs/
  Localization/
  Motions/

Assets/Prefabs/UI/
  Pages/
  Popups/
  Modals/
  Tooltips/
  Parts/
```

旧命名空间清理规则：

- 旧 `AXR.Framework.UI` 不再保留运行时代码。仍被业务使用的 UI 动画与点击组件，例如 `UIClickTarget`、`IUIRuntimeMotion`、`UISequenceDirector`、`UIMotionPlayer` 和 Motion Track，已迁入 `Orange.UIFramework` 命名空间。
- 旧页面托管、旧 `UIManager`、旧 Catalog、旧 Navigation 与迁移期桥接 API 不再保留；新业务代码只依赖 `Orange.UIFramework` 下的 `UIManager`、`PageBase`、`PopupBase`、`ModalBase`、`TooltipBase` 和 `ViewPartBase`。
- `UIManager` 仍然是运行时总入口，不应被替换成另一个 `UIService`。

## 4. 设计原则

### 4.1 最小核心

框架核心只关心：

- UI 根节点、Canvas 模式与层级。
- View 的创建、打开、关闭、回收。
- View 生命周期、输入激活、焦点、BackStack。
- 过渡动画等待。
- 异步加载、取消和结果返回。
- 本地化文本刷新。

框架核心不关心：

- 商店购买规则。
- 背包合并规则。
- 角色属性展示规则。
- 具体按钮音效。
- 具体业务状态机。

### 4.2 按真实边界使用接口

应使用接口的地方：

- `IUIManager`：业务只需要打开、关闭 UI 时依赖接口；场景装配、诊断和 Inspector 配置仍由具体 `UIManager` 承担。
- `IViewLoader`：Prefab 同步实例化、Resources、Addressables 后续可替换。
- `ILocalizationService`：UI 文本绑定不应知道语言表来源。
- `IViewTransition`：视图生命周期不应直接依赖 DOTween。
- 页面业务 Presenter/View 分离且存在多个实现或测试替身时的局部接口。只有一个页面、一个实现、只转发事件的情况下不创建接口。

不应使用接口的地方：

- 每个按钮、每个文本、每个子节点都套接口。
- 只有一个实现、没有替换需求的纯数据结构。
- 页面内部简单的子视图访问。
- 为了“架构感”创建空 Region、空 Contract。

### 4.3 页面内部直接组合

页面内部的左右滑动栏、购物栏、列表、详情面板等统一称为 `ViewPart`。它们属于页面私有组合，不进入全局 UI Stack。

示例：

- `ShopPage` 是 Page。
- 复杂且挂在 Prefab 上的子对象可以是 `ViewPartBase`，例如 `InventoryUI`、`BuffBarUI`、`CharacterStatusPanel`。
- 简单且只服务单一页面的列表、侧栏状态和按钮编排优先直接写在 Page 内，不再拆成只转发一层的 `View` / `Host` / `Controller`。
- `ItemOperatePopup` 是 Popup。
- `ConfirmPurchaseModal` 是 Modal。
- `ItemTooltip` 是 Tooltip。

## 5. 运行时对象模型

### 5.1 ViewKind

```csharp
namespace Orange.UIFramework
{
    public enum ViewKind
    {
        Page,
        Popup,
        Modal,
        Tooltip,
        Part
    }
}
```

语义：

- `Page`：全屏或主要界面，进入 BackStack，可替换、重置、返回。
- `Popup`：轻量浮层，可锚定目标，可多个并存，默认不进入 BackStack。
- `Modal`：阻塞式弹窗，有遮罩和焦点，通常返回结果。
- `Tooltip`：瞬态提示，跟随指针或锚点，默认唯一，不抢 BackStack。
- `Part`：页面内部子级，由页面直接持有和管理，不通过全局服务打开。

### 5.2 基础生命周期

所有被框架托管的 View 使用统一生命周期：

```csharp
namespace Orange.UIFramework
{
    public interface IView
    {
        string InstanceId { get; }
        bool IsOpen { get; }

        void Initialize(ViewHandle handle);
        void ApplyInputState(bool interactable, bool blocksRaycasts);
        void Tick(float deltaTime);
    }
}
```

异步生命周期由基类提供，业务只重写必要方法：

```csharp
namespace Orange.UIFramework
{
    public abstract class ViewBase : MonoBehaviour, IView
    {
        protected virtual UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;
        protected virtual UniTask OnOpenedAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        protected virtual UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken) => UniTask.CompletedTask;
        protected virtual void OnClosed(CloseReason reason) { }
        protected virtual void OnInputChanged(bool interactable, bool blocksRaycasts) { }
        protected virtual void OnTick(float deltaTime) { }
    }
}
```

推荐规则：

- `OnOpeningAsync`：绑定上下文、订阅事件、初次渲染、等待入场动画。
- `OnOpenedAsync`：需要在 UI 完整出现后执行的轻量逻辑。
- `OnClosingAsync`：取消业务订阅、等待退场动画、提交 Modal 结果。
- `OnClosed`：清理引用、重置状态、释放临时对象。
- `OnTick`：只有实现了需要逐帧逻辑的视图才加入 Tick 列表。

## 6. Canvas 与层级方案

### 6.1 CanvasProfile

`CanvasProfile` 是 ScriptableObject，配置 Canvas 模式和缩放策略。

字段建议：

```csharp
namespace Orange.UIFramework
{
    [CreateAssetMenu(menuName = "Orange/UI Framework/Canvas Profile", fileName = "CanvasProfile")]
    public sealed class CanvasProfile : ScriptableObject
    {
        [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private float planeDistance = 100f;
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
        [SerializeField] private float matchWidthOrHeight = 0.5f;
        [SerializeField] private int rootSortingOrder;
    }
}
```

Overlay 模式：

- `Canvas.renderMode = ScreenSpaceOverlay`
- `Canvas.worldCamera = null`
- 不依赖 UI Camera。
- 适合大多数普通菜单、HUD、结算、背包、商店页面。

Camera 模式：

- `Canvas.renderMode = ScreenSpaceCamera`
- 必须设置 `Canvas.worldCamera`
- 设置 `Canvas.planeDistance`
- 适合需要与相机后处理、相机堆叠、场景遮挡关系、摄像机分辨率策略统一的 UI。

禁止行为：

- Camera 模式下静默使用 `Camera.main`。如果没有显式配置相机，应抛出可定位错误。
- 运行时随意创建多个 Root Canvas 争抢 SortingOrder。
- 每个 Popup 单独创建 Canvas，除非有明确排序或性能原因。

### 6.2 Root 与 Layer

Root 结构：

```text
UIRoot
  Canvas
  CanvasScaler
  GraphicRaycaster
  Layers
    BackgroundLayer
    HudLayer
    PageLayer
    PopupLayer
    ModalMaskLayer
    ModalLayer
    TooltipLayer
    SystemLayer
    DebugLayer
```

推荐层级：

| Layer | Sorting | 用途 | Raycast |
| --- | ---: | --- | --- |
| Background | -300 | 背景装饰、低层 UI | 按配置 |
| Hud | -100 | 战斗 HUD、状态栏 | true |
| Page | 0 | 主界面、菜单、商店、背包 | true |
| Popup | 200 | 操作菜单、信息浮层 | true |
| ModalMask | 300 | Modal 遮罩 | true |
| Modal | 320 | 确认框、设置弹窗 | true |
| Tooltip | 500 | 悬浮提示 | false |
| System | 700 | Loading、Toast、全局提示 | true |
| Debug | 900 | 调试 UI | true |

Modal 打开时：

- 创建或启用一个统一遮罩对象。
- 遮罩在最上层 Modal 下方。
- 只有最顶层 Modal 可交互。
- Modal 关闭后恢复下一个 Modal 或 Page 的输入状态。

Tooltip 打开时：

- 不阻挡 Raycast。
- 默认只保留一个当前 Tooltip。
- 由 UIManager 的 Tooltip API 统一定位和裁剪。

## 7. Catalog 与加载

### 7.1 ViewDefinition

每个可被全局打开的 Page、Popup、Modal、Tooltip 都在 Catalog 注册。

```csharp
namespace Orange.UIFramework
{
    [Serializable]
    public sealed class ViewDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private ViewKind kind;
        [SerializeField] private ViewLayer layer;
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool singleton = true;
        [SerializeField] private bool cacheOnClose = true;
        [SerializeField] private bool trackInBackStack;
        [SerializeField] private bool closeOnBackgroundClick;
        [SerializeField] private int warmupCount;
        [SerializeField] private int maxCachedInstancesOverride = -1;
    }
}
```

规则：

- `id` 用于资源、调试、非泛型 API。
- 泛型 API 通过 Prefab 上的组件类型解析 Definition。
- `trackInBackStack` 默认只对 Page 开启。
- Popup、Tooltip 默认不进入 BackStack。
- Modal 有自己的 ModalStack，不混入 Page BackStack。

### 7.2 IViewLoader

当前项目没有 Addressables，不应强行引入。先提供同步 Prefab Catalog Loader，并用 UniTask 接口包一层，后续可替换。

```csharp
namespace Orange.UIFramework
{
    public interface IViewLoader
    {
        UniTask<ViewBase> LoadAsync(ViewDefinition definition, Transform parent, CancellationToken cancellationToken);
        void Release(ViewBase view, ViewDefinition definition);
    }
}
```

默认实现：

- 从 `ViewDefinition.prefab` 实例化。
- 如果开启池化，优先从池中取。
- `LoadAsync` 可直接返回 `UniTask.FromResult(instance)`。
- 后续接入 Addressables 时，只新增 `AddressableViewLoader`，不改业务页面。

### 7.3 Catalog 校验

编辑器或启动时必须校验：

- Prefab 非空。
- Prefab 根节点包含 `ViewBase` 派生类型。
- `ViewKind` 与组件基类匹配，例如 Modal Prefab 必须继承 `ModalBase`。
- 同一类型不能重复注册为多个 Definition，除非显式允许多皮肤。
- `id` 不能为空且不能重复。
- Camera 模式 CanvasProfile 必须有 UI Camera。
- LayerDefinition 不得重复 Layer。

## 8. UIManager API

`UIManager` 保留为框架运行时总调度器，延续当前项目的核心入口职责：Root Canvas 创建、层级构建、Catalog 注册、池化、打开关闭、焦点、BackStack、诊断都由它负责。业务侧需要解耦时依赖 `IUIManager`，但不要再额外引入一个平行的 `UIService`。

```csharp
namespace Orange.UIFramework
{
    public interface IUIManager
    {
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
    }
}
```

兼容同步调用：

```csharp
public ViewHandle<TPage> OpenPage<TPage>(object payload = null)
    where TPage : PageBase;
```

同步 API 用于兼容当前项目既有调用与已预热的同步 Prefab 场景。新业务默认使用 UniTask 异步 API；旧同步 API 不删除，但不再继续扩展复杂能力。

### 8.1 ViewHandle

`ViewHandle<T>` 是外部关闭、查询实例、等待关闭的安全句柄。

```csharp
namespace Orange.UIFramework
{
    public readonly struct ViewHandle<TView>
        where TView : ViewBase
    {
        public string InstanceId { get; }
        public TView View { get; }
        public UniTask ClosedTask { get; }

        public UniTask CloseAsync(CloseReason reason = CloseReason.Normal, CancellationToken cancellationToken = default);
    }
}
```

实现时可保留一个非泛型 `ViewHandle` 给框架内部 `IView.Initialize(ViewHandle handle)` 使用；业务层默认使用泛型句柄，减少强制转型。

规则：

- 外部不直接 `Destroy(view.gameObject)`。
- 外部关闭必须走 Handle 或 UIManager。
- 池化对象关闭后 Handle 失效，不能缓存长期使用。

## 9. Page 管理

Page 是主界面单位。

默认规则：

- Page 进入 `PageStack`。
- 打开新 Page 时，可选择 `Push`、`ReplaceTop`、`Reset`。
- 只有最顶层 Page 默认可交互。
- 下层 Page 是否可见由 Definition 配置，例如 HUD 可保持显示，菜单可隐藏。
- `CloseTopPageAsync` 只关闭当前顶层 Page，不影响 ModalStack。
- 如果 Modal 打开，Back 优先关闭 Modal，再关闭 Popup，再关闭 Page。

建议 PageBase：

```csharp
namespace Orange.UIFramework
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PageBase : ViewBase
    {
    }
}
```

Page 内部业务建议：

- 页面脚本负责 Inspector 引用、事件转发、子视图组合。
- 页面业务逻辑可以放 Presenter/Controller，但只在页面复杂时使用。
- 简单页面不要强拆 Controller。
- 页面内部子视图不要注册到全局 Catalog。

## 10. Popup 管理

Popup 用于操作菜单、物品详情、局部浮层。

能力：

- 支持锚定 `RectTransform`。
- 支持屏幕坐标打开。
- 支持点击外部关闭。
- 支持多个 Popup 并存，也支持按组互斥。
- 可选择进入轻量 PopupStack，Back 时关闭最后打开的 Popup。
- 可池化。

PopupOptions：

```csharp
namespace Orange.UIFramework
{
    public readonly struct PopupOptions
    {
        public readonly RectTransform Anchor;
        public readonly Vector2 ScreenPosition;
        public readonly Vector2 Offset;
        public readonly bool CloseOnOutsideClick;
        public readonly string GroupId;
        public readonly bool ReplaceSameGroup;
        public readonly bool TrackInStack;
        public readonly float Margin;
        public readonly FloatingViewAnchor PreferredAnchor;
        public readonly bool HasScreenPosition;

        public PopupOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool closeOnOutsideClick = true,
            string groupId = "",
            bool replaceSameGroup = false,
            bool trackInStack = true,
            float margin = 12f,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool useScreenPosition = false)
        {
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            CloseOnOutsideClick = closeOnOutsideClick;
            GroupId = groupId;
            ReplaceSameGroup = replaceSameGroup;
            TrackInStack = trackInStack;
            Margin = margin;
            PreferredAnchor = preferredAnchor;
            HasScreenPosition = screenPosition != default || useScreenPosition;
        }
    }
}
```

规则：

- 物品操作面板推荐使用 Popup。
- Popup 不应持有具体业务 Manager；通过 payload 或页面传入的回调完成交互。
- Popup 不负责冻结页面输入，除非 Definition 明确配置。
- 当前已实现 PopupStack、分组互斥、外部点击关闭、锚点 / 屏幕点定位、边缘裁剪、自动翻转和定位诊断；`TrackInStack` 已保留给后续 Back 行为，当前主要由 PopupStack 维护输入顺序和外部点击顺序。
- Popup 自身不实现坐标换算，必须由 `FloatingViewPositioner` 统一提供，避免业务脚本重复实现。

## 11. Modal 管理

Modal 用于确认、设置、重要提示、阻塞式选择。

能力：

- 打开时自动显示遮罩。
- 顶层 Modal 独占输入。
- 支持返回结果。
- 支持按 Esc / Back 取消。
- 支持遮罩点击取消或不取消。
- 支持多个 Modal 叠加，但默认不鼓励超过两层。
- 当前阶段已实现统一遮罩、顶层 Modal 输入独占、`ViewDefinition.CloseOnBackgroundClick` 控制遮罩点击关闭，以及框架关闭路径兜底完成取消结果，避免 Modal 结果任务悬挂。

ModalBase：

```csharp
namespace Orange.UIFramework
{
    public abstract class ModalBase<TResult> : ViewBase
    {
        protected void SetResult(TResult result);
        protected void Cancel();
    }
}
```

ModalResult：

```csharp
namespace Orange.UIFramework
{
    public readonly struct ModalResult<TResult>
    {
        public bool Confirmed { get; }
        public TResult Value { get; }
    }
}
```

业务调用：

```csharp
ModalResult<bool> result = await uiManager.ShowModalAsync<ConfirmDialog, bool>(
    new ConfirmDialogPayload("BuyItemTitle", "BuyItemMessage"),
    cancellationToken);

if (!result.Confirmed || !result.Value)
{
    return;
}
```

## 12. Tooltip 管理

Tooltip 用于悬浮说明、属性说明、物品说明。

现有 `UITooltipPresenter.ActivePresenter` 和 `FindFirstObjectByType` 兜底需要废弃为显式服务。

新规则：

- Tooltip Prefab 注册到 Catalog。
- Tooltip 位于 `TooltipLayer`。
- 通过 `ShowTooltipAsync<TTooltip>(payload, options)` 显示。
- 鼠标移动时只更新位置，不重复绑定内容。
- Tooltip 默认不阻挡 Raycast。
- Tooltip 关闭不进入动画等待链路，除非显式配置 Hide 动画。
- Tooltip 与 Popup 的坐标换算、锚点定位、边缘裁剪必须走统一定位工具，禁止每个业务脚本各写一套。
- 当前已实现唯一 Tooltip、指针跟随更新、不阻挡输入、边缘裁剪、自动翻转和定位诊断；指针跟随只更新位置，不重复绑定 payload。

TooltipOptions：

```csharp
namespace Orange.UIFramework
{
    public readonly struct TooltipOptions
    {
        public readonly Vector2 ScreenPosition;
        public readonly RectTransform Anchor;
        public readonly Vector2 Offset;
        public readonly float Margin;
        public readonly bool FollowPointer;
        public readonly FloatingViewAnchor PreferredAnchor;
        public readonly bool HasScreenPosition;

        public TooltipOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool followPointer = false,
            float margin = 12f,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool useScreenPosition = false)
        {
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            FollowPointer = followPointer;
            Margin = margin;
            PreferredAnchor = preferredAnchor;
            HasScreenPosition = screenPosition != default || useScreenPosition;
        }
    }
}
```

定位规则：

- Overlay：`ScreenPointToLocalPointInRectangle(parentRect, screenPosition, null, out localPoint)`。
- Camera：使用 Root Canvas 的 `worldCamera`。
- 自动根据 Tooltip 尺寸和父 Rect 裁剪到屏幕内。
- 对 LayoutGroup 内容，显示前调用一次布局刷新；不要每帧强制 Rebuild。

### 12.1 Popup / Tooltip 定位裁剪工具

必须新增统一工具类，建议命名为 `UIViewPositioner` 或 `FloatingViewPositioner`，由 UIManager 内部持有并复用。

职责：

- 根据 Root Canvas 模式选择正确相机：Overlay 使用 `null`，Camera 使用 `Canvas.worldCamera`。
- 支持 `RectTransform Anchor`、屏幕坐标、偏移、边距。
- 支持锚点优先级，例如右下、右上、左下、左上；当前位置放不下时自动翻转。
- 支持 `TooltipOptions.Margin` 与 `PopupOptions.Margin` 的边缘裁剪。
- 内容变化后可请求重新测量；普通鼠标移动只更新坐标，不强制 Layout Rebuild。
- 返回定位结果，方便诊断和测试。

建议接口：

```csharp
namespace Orange.UIFramework
{
    public readonly struct FloatingViewPlacement
    {
        public bool HasValue { get; }
        public Vector2 RequestedPosition { get; }
        public Vector2 AnchoredPosition { get; }
        public FloatingViewAnchor RequestedAnchor { get; }
        public bool WasFlipped { get; }
        public bool WasClamped { get; }
        public FloatingViewAnchor ResolvedAnchor { get; }
        public Rect LocalRect { get; }
        public Rect BoundsRect { get; }
    }

    public interface IFloatingViewPositioner
    {
        FloatingViewPlacement Place(
            RectTransform view,
            RectTransform layerRoot,
            Canvas rootCanvas,
            RectTransform anchor,
            bool useScreenPosition,
            Vector2 screenPosition,
            Vector2 offset,
            float margin,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool rebuildLayout = false);
    }
}
```

验收标准：

- Overlay 与 Camera 模式下，同一 Tooltip 位置计算一致。
- 鼠标位于屏幕四角时 Tooltip 不出屏。
- Popup 锚定背包格子时，不因分辨率变化漂移。
- `FollowPointer` 只更新位置，不重复绑定 payload。

## 13. ViewPart 管理

ViewPart 是页面内部子级，例如商店左右滑动栏、购物栏、背包列表、角色属性栏。

推荐基类：

```csharp
namespace Orange.UIFramework
{
    public abstract class ViewPartBase : MonoBehaviour
    {
        public virtual void Bind(object context) { }
        public virtual void Unbind() { }
        public virtual UniTask ShowAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public virtual UniTask HideAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public virtual void Refresh() { }
    }
}
```

规则：

- ViewPart 不进 Catalog。
- ViewPart 不直接访问全局 UIManager，除非它负责打开 Popup/Tooltip。
- ViewPart 可以持有 `UIMotionPlayer` 或 `UISequenceDirector`。
- 页面负责调用 `Bind` / `Unbind`。
- 子视图复杂时可以有自己的小 Controller，但不创建无用 Contract。

商店页面结构建议：

```text
ShopPage
  Shop item list fields and render methods
  Properties sidebar motion and describer binding
  Inventory sidebar motion
  InventoryUI (ViewPart)
```

页面直接处理：

- 订阅 `ShopManager.ItemsChanged` / `PurchaseSucceeded` / `PurchaseFailed`。
- 渲染商店卡片列表和复用布局动画。
- 处理刷新、继续、购买、锁定和左右侧栏切换按钮。
- 把 `InventoryOperateManager` 和 `UIManager` 注入内嵌 `InventoryUI`。
- 将 `PropertiesManager` 直接绑定到属性描述器。

禁止拆出的纯包装：

- 只转发按钮事件的 `ShopListView`。
- 只持有两个侧栏的 `ShopSidebarHost`。
- 只包一层 `SidebarMotion` 的 `ShopPropertiesSidebarView` / `ShopInventorySidebarView`。
- 只保存两个 bool 的 `ShopPageState`。
- 把点击事件回传页面或 Controller。

## 14. 异步策略

OrangeUIFramework 使用 UniTask 作为异步模型，统一处理 UI 加载、动画等待、Modal 返回结果、取消和页面切换队列。实现前需要在项目中安装 `Cysharp.Threading.Tasks`，并在 Runtime 脚本中引用 `Cysharp.Threading.Tasks`。

### 14.1 异步来源

需要异步的地方：

- Prefab 加载，当前可同步完成，未来可接 Addressables。
- 页面进入动画等待。
- 页面退出动画等待。
- Modal 等待用户选择结果。
- 语言包切换后等待资源加载或字体替换。
- Loading 页面等待外部流程。

不需要异步的地方：

- 普通按钮点击转发。
- 简单文本设置。
- 列表项同步渲染。
- 纯内存状态更新。

### 14.2 取消归属

取消令牌来源：

- 场景卸载：由 Bootstrap 或 UIManager 创建。
- View 销毁：优先使用 `this.GetCancellationTokenOnDestroy()` 或手动 CTS。
- 打开请求被新请求替代：UIManager 为每个 Stack 操作维护 request version。
- 业务主动取消：调用方传入 `CancellationToken`。

规则：

- `OpenPageAsync` 取消后，如果 Prefab 已实例化但未完成打开，必须关闭并回收。
- `CloseAsync` 取消不应让 View 卡在半关闭状态；关闭一旦开始，除非框架明确支持可逆动画，否则应完成关闭。
- Modal 等待用户结果时，取消等价于 `Cancelled`。

### 14.3 UIManager 异步防重入

UIManager 必须显式处理快速连点、连续页面切换、关闭中重新打开、Modal 等待结果时被取消等场景。不能只靠按钮禁用或业务层约束。

View 实例状态建议：

```csharp
namespace Orange.UIFramework
{
    public enum ViewRuntimePhase
    {
        None,
        Loading,
        Opening,
        Opened,
        Closing,
        Closed,
        Recycled
    }
}
```

UIManager 需要维护：

- `instanceId -> RuntimeView`：记录 View、Definition、Kind、Phase、RequestVersion、CTS。
- `Type -> active instance`：支持 singleton 与重复打开策略。
- `PageStack`、`PopupStack`、`ModalStack`、当前 Tooltip。
- `activeTransitionVersion`：页面替换、重置、关闭全部时使用最新请求覆盖旧请求。

防重入规则：

- 同一 singleton View 正在 `Loading` 或 `Opening` 时，再次打开默认返回同一个 UniTask 或同一个 Handle，不重复实例化。
- `ReplacePageAsync` 连续调用时，只允许最后一次请求成为最终页面；旧请求若尚未完成，应取消或在完成后立即关闭回收。
- View 进入 `Closing` 后，不允许再次对同实例执行 `CloseAsync`；重复关闭返回同一个关闭 UniTask。
- `CloseAllPagesAsync` 开始后，新打开请求必须带更高 request version；旧关闭完成回调不得误关新页面。
- Modal 的结果 UniTask 只能完成一次；确认、取消、外部取消、销毁回收之间必须互斥。
- Popup 分组互斥时，打开同组新 Popup 需要等待旧 Popup 进入关闭流程，但不阻塞无关组 Popup。

建议内部结构：

```csharp
namespace Orange.UIFramework
{
    internal sealed class RuntimeView
    {
        public string InstanceId;
        public ViewBase View;
        public ViewDefinition Definition;
        public ViewRuntimePhase Phase;
        public int RequestVersion;
        public CancellationTokenSource LifetimeCts;
        public UniTaskCompletionSource<ViewHandle> OpenCompletion;
        public UniTaskCompletionSource CloseCompletion;
    }
}
```

验收标准：

- 快速连续点击打开商店，只产生一个商店页面实例。
- 连续 `ReplacePageAsync<A>`、`ReplacePageAsync<B>` 后最终只显示 B。
- 页面关闭动画未完成时再次关闭，不会重复回收或重复触发 `OnClosed`。
- Modal 点击确认和按 Esc 同帧发生时，只返回一个结果。
- 所有被取消的 UniTask 不留下未释放的 CTS 或事件订阅。

### 14.4 DOTween 等待包装

不要让业务代码直接依赖 DOTween 的等待细节。动画层提供统一包装：

```csharp
namespace Orange.UIFramework
{
    public interface IViewTransition
    {
        UniTask PlayEnterAsync(CancellationToken cancellationToken);
        UniTask PlayExitAsync(CancellationToken cancellationToken);
        void SetVisibleImmediate();
        void SetHiddenImmediate();
        void Kill();
    }
}
```

`UIMotionTransition` 内部可继续使用 `UIMotionPlayer` 和 `UISequenceDirector`。

当前实现落地规则：

- 新框架在 `Assets/Scripts/OrangeUIFramework/Motions/Runtime/` 提供 `IViewTransition`、`UIMotionTransition` 和 DOTween -> UniTask 等待包装。
- `ViewBase` 只查找 `IViewTransition`，不直接引用 DOTween；没有动画组件时打开/关闭直接返回完成。
- `UIMotionTransition` 适配旧 `AXR.Framework.UI.IUISequenceMotion`，现阶段沿用旧 `UIMotionPlayer` / `UISequenceDirector`，不复制整套 UIMotion 到新命名空间。
- `UIMotionTransition.PlayEnterAsync()` 会在播放 Hidden -> Show 前调用 `RefreshDefaults()`，再执行 `SetHiddenImmediate()`，避免池化对象把上一轮 Hide 后的位置、Scale、Alpha 当成新的 Initial。
- DOTween 等待包装需要保留已有 `onComplete` / `onKill` 回调，等待完成、Kill、取消均能完成 UniTask；取消等待时 Kill 当前 Tween。

## 15. 本地化方案

不引入 Unity Localization 包作为强依赖，避免把 UI 框架绑死到某个数据来源。框架只定义服务和绑定组件，语言数据可来自 ScriptableObject、CSV、JSON 或后续包适配。

### 15.1 ILocalizationService

```csharp
namespace Orange.UIFramework
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        event Action LanguageChanged;

        UniTask SetLanguageAsync(string language, CancellationToken cancellationToken = default);
        string GetText(string key);
        string GetText(string key, IReadOnlyDictionary<string, object> args);
    }
}
```

### 15.2 LocalizedText

```csharp
namespace Orange.UIFramework
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool refreshOnEnable = true;

        public void SetKey(string newKey);
        public void SetArgs(IReadOnlyDictionary<string, object> args);
        public void Refresh();
    }
}
```

规则：

- Prefab 上固定文本使用 `LocalizedText`。
- 动态业务文本由页面或子视图调用 `localization.GetText(key, args)`。
- 不要在业务中硬编码中文、英文句子。
- 文本 Key 使用业务语义，例如 `shop.reroll.cost`，不是 UI 节点路径。
- 语言切换触发所有激活 `LocalizedText.Refresh()`。
- 字体资产按语言配置，可选 `LocalizedFontBinder`，不要散落在业务页面里手动换字体。

当前实现落地规则：

- 框架新增 `Assets/Scripts/OrangeUIFramework/Localization/`，包含 Runtime 与 Data 两层，不依赖 Unity Localization 包。
- `LocalizationTable` 是 ScriptableObject，使用 `Orange/UI Framework/Localization Table` 菜单创建，按语言保存 `key -> value` 文本。
- `LocalizationService` 是场景运行时组件，实现 `ILocalizationService`，维护当前语言、语言表索引、语言切换事件和默认语言回退。
- `LocalizedText` 绑定 `TMP_Text`，可显式引用 `LocalizationService`；未绑定时使用 `LocalizationService.Current` 作为迁移期默认路径。
- 未找到 key 时返回 key 本身，便于在界面上直接暴露缺失项。
- 参数化文本使用 `{name}` 占位符做简单替换，当前不做复杂格式化表达式。
- 阶段 9 只提供框架能力，不批量替换现有业务页面硬编码文本；业务迁移阶段再逐页接入 `LocalizedText`。

### 15.3 参数化文本

示例：

```text
shop.reroll.cost = Reroll ({cost})
item.level = Lv.{level}
```

业务：

```csharp
rerollCostText.text = localization.GetText(
    "shop.reroll.cost",
    new Dictionary<string, object> { ["cost"] = rerollCost });
```

## 16. 动画系统方案

### 16.1 继续沿用的部分

继续使用：

- `UIMotionDefinition`
- `UIMotionClipDefinition`
- `UIMotionTrackDefinition`
- `UIMotionPlayer`
- `UISequenceDirector`
- `UIMotionPlayerEditor`

原因：

- 已经支持多 Track、多 Clip、Channel 冲突、Show/Hide/Hover/Click 等动作。
- 与 DOTween 接入成熟。
- Inspector 可配置，适合 UI 设计迭代。
- 页面和子视图都能复用。

### 16.2 需要修正的点

必须修正：

- `UIMotionPlayer.refreshDefaultsOnEnable`：当前初始化后 `defaultsCaptured` 为 true，重新启用不会刷新快照，池化页面复用后动画起点可能沿用上一次关闭或交互后的状态。必须修复。
- `SidebarMotion.ConfigureTimings`：空实现应删除，或改为真正修改可运行配置；保留空 API 会误导调用方。
- `UISequenceDirector` 的 `useUnscaledTime` 与全局 Settings 可能冲突。建议由 ViewDefinition 或 Transition 配置统一决定，局部可覆盖。
- `UISequenceDirector` 引用缺失现在运行时抛错，建议增加编辑器校验和按钮自动收集。
- `UIManager` 关闭等待应迁移到 `IViewTransition.PlayExitAsync`，避免页面基类直接理解 DOTween。
- `SetImmediate` 不应创建不必要的 Sequence；Immediate 采样应只写目标属性，减少无意义 GC。
- 进入动画是否等待应由 API 决定：UniTask 版 `OpenPageAsync` 默认等待入场完成，`OpenPageImmediate` 或 `OpenPageAsync(..., waitForTransition:false)` 可跳过。

`refreshDefaultsOnEnable` 修复要求：

- 字段语义保持为“每次启用时以当前 RectTransform / Graphic / CanvasGroup 状态作为新的 Initial 快照”。
- `OnEnable` 中只要 `refreshDefaultsOnEnable == true` 就调用 `RefreshDefaults()`，不再受 `defaultsCaptured` 阻挡。
- `InitializeIfNeeded()` 只负责建立目标映射和首次快照，不再把“已经初始化”误当成“不需要刷新默认值”。
- 池化 View 从关闭态重新打开时，应在播放 `Hidden -> Show` 前刷新默认快照，避免隐藏态覆盖设计态。
- `IUISequenceMotion` 需要暴露 `RefreshDefaults()`，让 `UISequenceDirector` 可以把刷新传播到实际子 Motion。
- 如果某个动画需要固定 Prefab 原始值作为起点，应新增明确选项，例如 `captureDefaultsOnAwakeOnly` 或在 MotionDefinition 中声明，不要复用 `refreshDefaultsOnEnable` 表达相反语义。

建议实现方向：

```csharp
private void OnEnable()
{
    InitializeIfNeeded();
    if (refreshDefaultsOnEnable)
    {
        RefreshDefaults();
    }
}
```

验收标准：

- 同一个池化 Popup 打开、关闭、移动位置后再次打开，Show 动画起点基于本次打开前的正确位置。
- Hover / Click 动画播放后关闭回收，再打开不会把交互后的 Scale、Alpha、Position 当成错误初始值。
- `RefreshDefaults` 不在每帧调用，只在启用、显式刷新、布局稳定后调用。

### 16.3 View 与动画的关系

Prefab 挂载建议：

```text
ShopPageRoot
  CanvasGroup
  ShopPage
  UIMotionPlayer
  UISequenceDirector
  Content
    ShopItemContainer instances
    PropertiesSidebar (UIMotionPlayer)
    InventorySidebar (UIMotionPlayer + InventoryUI)
```

ViewBase 不直接调用 DOTween：

- ViewBase 查找 `IViewTransition`。
- `UIMotionTransition` 适配 `UIMotionPlayer` / `UISequenceDirector`。
- 没有动画组件时返回 CompletedTask。

这样业务页面可以完全不知道动画系统实现。

## 17. 输入、焦点与返回键

输入状态分三层：

- Visual：是否可见。
- Interactable：Selectable 是否可交互。
- BlocksRaycasts：是否阻挡点击。

规则：

- 顶层 Modal 独占输入。
- 没有 Modal 时，最顶层 Popup 可按配置抢占输入。
- 没有 Popup 时，最顶层 Page 可输入。
- Tooltip 永远不抢占输入。
- 下层 Page 可以保持显示，但默认不可交互。

Back / Esc 优先级：

1. 顶层 Modal，若允许取消则关闭。
2. 顶层 Popup，若 `CloseOnBack` 为 true 则关闭。
3. 顶层 Page，若 BackStack 数量大于 1 则关闭。
4. 交给游戏层处理，例如暂停、退出确认。

## 18. 性能要求

必须做到：

- Catalog 启动后建立 Type / Id 字典，打开 UI 不做全局查找。
- Tooltip 不使用 `FindFirstObjectByType` 作为常规路径。
- 只有实现 Tick 的 View 加入 Tick 列表。
- 高频列表项使用池化，例如商店商品、背包格子、Buff 图标。
- 关闭后清理事件订阅，池化对象不得残留旧回调。
- `LayoutRebuilder.ForceRebuildLayoutImmediate` 只在内容变更后调用，不在 Update 调用。
- Popup/Tooltip 定位计算只在位置变化或内容变化时执行。
- 语言切换统一刷新，不逐帧检查语言。
- Catalog 校验尽量放编辑器或启动阶段，运行中错误要明确。

不做的优化：

- 不为所有 UI 强制拆分独立 Canvas。
- 不默认接 Addressables。
- 不做复杂 UI 虚拟路由。
- 不为简单列表强行虚拟化，只有大量动态元素时再加。

## 19. 错误与诊断

框架必须提供 `LogRuntimeDiagnostics()`，并建议同时提供结构化快照，便于编辑器窗口、自动化测试和运行时调试复用。

- 当前 Canvas 模式。
- 当前 UI Camera。
- 当前 PageStack。
- 当前 PopupStack。
- 当前 ModalStack。
- 当前 Tooltip。
- 当前池化数量。
- 当前异步请求状态：request version、各操作通道是否被占用、Opening / Closing / Failed 数量。
- 当前 request version。
- 每个生命周期追踪 View 的 `InstanceId`、类型、Kind、Layer、Phase、InputActive、BlocksRaycasts。
- 当前 Modal 遮罩状态：是否存在、是否显示、是否拦截射线、顶层 Modal、点击遮罩是否可关闭。
- 当前 Popup 外部点击拦截器状态：是否显示、是否拦截射线、顶层 Popup、点击外部是否可关闭。
- 当前输入焦点状态：顶层 Page / Popup / Modal、输入激活数量、射线阻挡数量、Tooltip 是否错误阻挡输入。
- 当前 Tooltip / Popup 定位结果：请求坐标、最终坐标、请求锚点、最终锚点、是否翻转、是否裁剪、最终矩形与边界矩形。

建议结构：

```csharp
namespace Orange.UIFramework
{
    public readonly struct UIRuntimeDiagnostics
    {
        public string CanvasMode { get; }
        public string CameraName { get; }
        public string RootName { get; }
        public bool RootActive { get; }
        public int RequestVersion { get; }
        public IReadOnlyList<LayerDiagnostics> Layers { get; }
        public IReadOnlyList<ViewStackDiagnostics> PageStack { get; }
        public IReadOnlyList<ViewStackDiagnostics> PopupStack { get; }
        public IReadOnlyList<ViewStackDiagnostics> ModalStack { get; }
        public IReadOnlyList<ViewDiagnostics> OpenViews { get; }
        public IReadOnlyList<PoolDiagnostics> Pools { get; }
        public string CurrentTooltipInstanceId { get; }
        public TooltipDiagnostics Tooltip { get; }
        public UIOperationDiagnostics Operations { get; }
        public UIBlockerDiagnostics ModalMask { get; }
        public UIBlockerDiagnostics PopupOutsideClickBlocker { get; }
        public UIInputDiagnostics Input { get; }
    }

    public readonly struct ViewDiagnostics
    {
        public string InstanceId { get; }
        public string ViewTypeName { get; }
        public ViewKind Kind { get; }
        public ViewRuntimePhase Phase { get; }
        public string LayerName { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
        public bool HasPlacement { get; }
        public Vector2 RequestedPosition { get; }
        public Vector2 AnchoredPosition { get; }
        public FloatingViewAnchor RequestedAnchor { get; }
        public FloatingViewAnchor ResolvedAnchor { get; }
        public bool PlacementWasFlipped { get; }
        public bool PlacementWasClamped { get; }
    }

    public readonly struct ViewStackDiagnostics
    {
        public int Index { get; }
        public bool IsTop { get; }
        public string InstanceId { get; }
        public string ViewId { get; }
        public string ViewTypeName { get; }
        public ViewKind Kind { get; }
        public ViewRuntimePhase Phase { get; }
        public int RequestVersion { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
        public bool Closing { get; }
    }

    public readonly struct UIOperationDiagnostics
    {
        public int RequestVersion { get; }
        public bool PageOperationBusy { get; }
        public bool PopupOperationBusy { get; }
        public bool ModalOperationBusy { get; }
        public bool TooltipOperationBusy { get; }
        public int TrackedViewCount { get; }
        public int OpeningViewCount { get; }
        public int ClosingViewCount { get; }
        public int FailedViewCount { get; }
    }
}
```

规则：

- `LogRuntimeDiagnostics()` 输出人类可读日志。
- `GetRuntimeDiagnostics()` 返回结构化快照。
- 诊断代码不得依赖 `FindObjectsOfType` 扫场景，应从 UIManager 自己维护的运行时状态生成。
- 诊断输出必须能定位异步卡住的请求，例如哪个 View 停在 `Opening` 或 `Closing`；因此 `OpenViews` 实际输出当前生命周期内仍被 `UIManager` 追踪的运行时实例，而不只限于已经进入 Stack 的 View。
- 编辑器下可加 Inspector 按钮，运行时 Debug 菜单可调用同一份快照。
- 当前实现已在 `UIManager` Inspector 中提供 `Log Runtime Diagnostics` 按钮；按钮只调用同一份 `LogRuntimeDiagnostics()`，不维护第二套调试路径。

典型错误信息：

```text
UIManager.OpenPageAsync failed: page type 'ShopPage' is not registered in ViewCatalog.
BuildRoot failed: ScreenSpaceCamera mode requires a uiCamera in CanvasProfile.
ShowModalAsync failed: prefab 'ConfirmDialog' does not contain ModalBase<TResult>.
ViewCatalog contains duplicate id 'shop.page'.
```

## 20. 示例：Bootstrap 与挂载

### 20.1 场景对象

场景中创建：

```text
GameSystems
  UIManager
```

`UIManager` 组件 Inspector：

| 字段 | 绑定 |
| --- | --- |
| Settings | `OrangeUIFrameworkSettings.asset` |
| Catalog | `OrangeUIViewCatalog.asset` |
| Canvas Profile | `OverlayCanvasProfile.asset` 或 `CameraCanvasProfile.asset` |
| Existing Root Canvas | 可空；为空则启动时创建 |
| Localization Service | 场景中的 `LocalizationService` 或 Bootstrap 注入 |

Overlay 模式：

- `CanvasProfile.renderMode = ScreenSpaceOverlay`
- `uiCamera = null`

Camera 模式：

- `CanvasProfile.renderMode = ScreenSpaceCamera`
- `uiCamera = UICamera`
- `planeDistance = 100`

### 20.2 Catalog 示例

`OrangeUIViewCatalog.asset`：

| Id | Kind | Layer | Prefab | Singleton | Cache |
| --- | --- | --- | --- | --- | --- |
| `page.shop` | Page | Page | `UI Shop.prefab` | true | true |
| `popup.inventory.weaponOperate` | Popup | Popup | `Weapon Operate Popup.prefab` | false | true |
| `popup.inventory.accessoryInfo` | Popup | Popup | `Accessory Info Popup.prefab` | false | true |
| `modal.confirm` | Modal | Modal | `ConfirmDialog.prefab` | false | true |
| `tooltip.describable` | Tooltip | Tooltip | `Tooltip.prefab` | true | true |

### 20.3 Page Prefab 挂载

`UI Shop.prefab`：

```text
ShopPage
  RectTransform
  CanvasGroup
  ShopPage
  UIMotionPlayer
  UISequenceDirector
  Content
    ShopItemParent
      ShopItemContainer instances
    PropertiesSidebar
      UIMotionPlayer
      PropertiesDescriber
    InventorySidebar
      UIMotionPlayer
      InventoryUI
```

`ShopUIPage` 直接持有商店列表、属性侧栏、背包侧栏和按钮字段。只有真正挂载、可复用或有独立生命周期的子对象保留为 `ViewPartBase`，例如 `InventoryUI` 和卡片容器。

### 20.4 Popup Prefab 挂载

`ItemOperatePopup.prefab`：

```text
ItemOperatePopup
  RectTransform
  CanvasGroup
  ItemOperatePopup
  UIMotionPlayer
  Content
    SellButton
    MergeButton
    DetailText
```

### 20.5 Modal Prefab 挂载

`ConfirmDialog.prefab`：

```text
ConfirmDialog
  RectTransform
  CanvasGroup
  ConfirmDialog
  UIMotionPlayer
  Content
    TitleText
      LocalizedText
    MessageText
      LocalizedText
    ConfirmButton
    CancelButton
```

### 20.6 Tooltip Prefab 挂载

`ItemTooltip.prefab`：

```text
ItemTooltip
  RectTransform
  CanvasGroup
  ItemTooltip
  UIMotionPlayer
  Content
    Icon
    TitleText
    DescriptionText
```

## 21. 示例代码

### 21.1 打开商店页面

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopEntryPoint : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Player player;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private InventoryOperateManager inventoryOperateManager;

    private void Awake()
    {
        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(ShopEntryPoint)} requires a {nameof(UIManager)} reference.");
        }
    }

    public async UniTask OpenShopAsync(CancellationToken cancellationToken)
    {
        if (player == null || shopManager == null || inventoryOperateManager == null)
        {
            throw new MissingReferenceException($"{nameof(ShopEntryPoint)} requires explicit shop dependencies.");
        }

        inventoryOperateManager.Bind(player);
        ShopPageContext payload = new ShopPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<PropertiesManager>(),
            shopManager,
            inventoryOperateManager);
        await uiManager.ReplacePageAsync<ShopUIPage>(payload, cancellationToken);
    }
}
```

### 21.2 ShopUIPage

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopUIPage : PageBase
{
    [SerializeField] private ShopItemListUI itemList;
    [SerializeField] private ShopPropertiesPanel propertiesPanel;
    [SerializeField] private ShopInventoryPanel inventoryPanel;
    [SerializeField] private UIClickTarget rerollButton;
    [SerializeField] private UIClickTarget continueButton;

    private ShopPageContext payload;

    protected override async UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        payload = context.GetPayload<ShopPageContext>();
        if (payload == null)
        {
            throw new ArgumentException($"{nameof(ShopUIPage)} requires {nameof(ShopPageContext)}.");
        }

        payload.ShopManager.ItemsChanged += RenderSnapshot;
        rerollButton.OnClicked += OnRerollRequested;
        continueButton.OnClicked += OnContinueRequested;
        inventoryPanel.BeginSession(payload.InventoryOperateManager, OwnerUIManager);
        payload.ShopManager.RequestSnapshot();
        await base.OnOpeningAsync(context, cancellationToken);
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        payload.ShopManager.ItemsChanged -= RenderSnapshot;
        rerollButton.OnClicked -= OnRerollRequested;
        continueButton.OnClicked -= OnContinueRequested;
        inventoryPanel.EndSession();
        payload = null;
        return base.OnClosingAsync(reason, cancellationToken);
    }

    private void RenderSnapshot(ShopSnapshot snapshot)
    {
        itemList.Render(snapshot.Items, snapshot.Reason);
    }

    private void OnBuyRequested(int index)
    {
        payload.ShopManager.RequestBuyItem(index);
    }

    private void OnRerollRequested()
    {
        payload.ShopManager.RequestReroll();
    }

    private void OnContinueRequested()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }
}
```

### 21.3 页面子视图：左右滑动栏

```csharp
using Orange.UIFramework;
using UnityEngine;

public sealed class InventoryPanel : ViewPartBase
{
    [SerializeField] private InventoryUI inventoryUI;

    public void Bind(InventoryOperateManager manager, UIManager uiManager)
    {
        inventoryUI.ConfigureSession(manager, uiManager);
    }

    public override void Unbind()
    {
        inventoryUI.ReleaseSession();
    }
}
```

说明：左右侧栏只有按钮和显示状态时直接由 Page 持有 `IUIRuntimeMotion` 控制，不再单独创建 `ShopSidebarView` / `SidebarToggleView` 这类只转发一层的脚本。只有像 `InventoryUI` 这样有独立生命周期、Popup 入口或可复用 UI 逻辑的子对象才保留为 `ViewPartBase`。

### 21.4 Popup：物品操作面板

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class ItemOperatePopup : PopupBase
{
    [SerializeField] private ClickTarget sellButton;
    [SerializeField] private ClickTarget mergeButton;

    private ItemOperatePayload payload;

    public event Action<string> SellRequested;
    public event Action<string> MergeRequested;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        payload = context.GetPayload<ItemOperatePayload>();
        if (payload == null)
        {
            throw new ArgumentException($"{nameof(ItemOperatePopup)} requires {nameof(ItemOperatePayload)}.");
        }

        sellButton.Clicked += OnSellClicked;
        mergeButton.Clicked += OnMergeClicked;
        Render(payload);
        return base.OnOpeningAsync(context, cancellationToken);
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        sellButton.Clicked -= OnSellClicked;
        mergeButton.Clicked -= OnMergeClicked;
        SellRequested = null;
        MergeRequested = null;
        payload = null;
        return base.OnClosingAsync(reason, cancellationToken);
    }

    private void Render(ItemOperatePayload data)
    {
        // 设置图标、名称、品质、按钮状态。
    }

    private void OnSellClicked()
    {
        SellRequested?.Invoke(payload.EntryId);
    }

    private void OnMergeClicked()
    {
        MergeRequested?.Invoke(payload.EntryId);
    }
}
```

业务打开：

```csharp
PopupOptions options = new PopupOptions(anchor: itemRect, closeOnOutsideClick: true, groupId: "inventory.item", replaceSameGroup: true);
ViewHandle<ItemOperatePopup> popup = await uiManager.ShowPopupAsync<ItemOperatePopup>(payload, options, cancellationToken);
popup.View.SellRequested += inventoryFacade.RequestSell;
popup.View.MergeRequested += inventoryFacade.RequestMerge;
```

### 21.5 Modal：确认购买

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;

public sealed class ConfirmDialog : ModalBase<bool>
{
    [SerializeField] private LocalizedText titleText;
    [SerializeField] private LocalizedText messageText;
    [SerializeField] private ClickTarget confirmButton;
    [SerializeField] private ClickTarget cancelButton;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ConfirmDialogPayload payload = context.GetPayload<ConfirmDialogPayload>();
        titleText.SetKey(payload.TitleKey);
        messageText.SetKey(payload.MessageKey);

        confirmButton.Clicked += OnConfirmClicked;
        cancelButton.Clicked += OnCancelClicked;
        return base.OnOpeningAsync(context, cancellationToken);
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        confirmButton.Clicked -= OnConfirmClicked;
        cancelButton.Clicked -= OnCancelClicked;
        return base.OnClosingAsync(reason, cancellationToken);
    }

    private void OnConfirmClicked()
    {
        SetResult(true);
    }

    private void OnCancelClicked()
    {
        Cancel();
    }
}
```

业务调用：

```csharp
ModalResult<bool> result = await uiManager.ShowModalAsync<ConfirmDialog, bool>(
    new ConfirmDialogPayload("shop.buy.title", "shop.buy.message"),
    cancellationToken);

if (result.Confirmed && result.Value)
{
    shopFacade.ConfirmBuy();
}
```

### 21.6 Tooltip：物品说明

```csharp
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private RectTransform anchor;

    private ItemTooltipPayload payload;

    private void Awake()
    {
        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(ItemTooltipTarget)} requires a {nameof(UIManager)} reference.");
        }
    }

    public void SetPayload(ItemTooltipPayload newPayload)
    {
        payload = newPayload;
    }

    public async void OnPointerEnter(PointerEventData eventData)
    {
        if (payload == null)
        {
            return;
        }

        TooltipOptions options = new TooltipOptions(
            screenPosition: eventData.position,
            anchor: anchor,
            offset: new Vector2(18f, -18f),
            followPointer: true);

        await uiManager.ShowTooltipAsync<ItemTooltip>(payload, options);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        uiManager.UpdateTooltipPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        uiManager.HideTooltip();
    }
}
```

### 21.7 LocalizedText 使用

Prefab：

```text
RerollCostText
  TextMeshProUGUI
  LocalizedText
    Key = shop.reroll.cost
```

代码：

```csharp
using System.Collections.Generic;

rerollCostLocalizedText.SetArgs(new Dictionary<string, object>
{
    ["cost"] = rerollCost
});
```

## 22. 迁移计划

### 阶段 1：搭建新框架骨架

- 新建 `Assets/Scripts/OrangeUIFramework`。
- 新建或迁移 `UIManager`、`IUIManager`、`ViewBase`、`PageBase`、`PopupBase`、`ModalBase`、`TooltipBase`。
- 新建 `ViewCatalog`、`ViewDefinition`、`CanvasProfile`。
- 新建 Overlay / Camera 两套 CanvasProfile。
- 安装 UniTask 依赖，并统一使用 `Cysharp.Threading.Tasks`。
- 把现有 UIMotion 复制或迁移到新命名空间，保留运行逻辑。
- 写 Catalog 校验。

验收：

- 能打开、关闭一个测试 Page。
- Overlay 和 Camera 模式都能运行。
- Camera 模式缺相机时能给出明确错误。

### 阶段 2：补齐异步与动画等待

- 实现 `OpenPageAsync`、`CloseAsync`、`ReplacePageAsync`。
- 实现 UIManager 异步防重入状态机和 request version。
- 实现 `IViewTransition`。
- 将 UIMotion 接入 `PlayEnterAsync` / `PlayExitAsync`。
- 修复 `refreshDefaultsOnEnable`。
- 删除或实现空的 Timing API。

验收：

- 页面入场可等待。
- 关闭等待退场完成再回收。
- 快速连续打开页面不会出现旧请求覆盖新请求。
- 同一 View 关闭中重复关闭不会重复触发回收和 `OnClosed`。
- 池化 View 二次打开时动画起点正确。

### 阶段 3：Popup / Modal / Tooltip

- 实现 PopupLayer、ModalLayer、TooltipLayer。
- 实现统一 Popup / Tooltip 定位裁剪工具。
- 实现遮罩、焦点、外部点击关闭。
- 实现 Modal 返回结果。
- 替换 `UITooltipPresenter.ActivePresenter` 方案。

验收：

- 物品操作 Popup 可锚定背包格子。
- ConfirmDialog 可返回确认/取消。
- Tooltip 在 Overlay 和 Camera 模式下定位正确并裁剪。
- 屏幕四角、不同分辨率、Camera Canvas 下 Popup / Tooltip 不漂移、不出屏。

### 阶段 4：多语言

- 实现 `ILocalizationService`。
- 实现 `LocalizedText`。
- 建立第一批语言表。
- 替换核心页面硬编码文本。

验收：

- 运行时切换语言，已打开 UI 自动刷新。
- 参数化文本正常显示。
- TMP 字体 fallback 正常。

### 阶段 5：诊断与调试

- 实现 `LogRuntimeDiagnostics()`。
- 实现 `GetRuntimeDiagnostics()` 结构化快照。
- 在 `UIManager` Inspector 增加运行时诊断按钮。
- 输出 PageStack、PopupStack、ModalStack、Tooltip、池化数量、异步请求状态、定位裁剪结果。

验收：

- 能定位卡在 `Opening` / `Closing` 的 View。
- 能看到当前 Modal 遮罩是否拦截输入。
- 能看到 Tooltip / Popup 是否发生裁剪和最终坐标。

### 阶段 6：迁移业务页面

迁移优先级：

1. `MenuUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；设置侧栏仍沿用旧 `IUIRuntimeMotion` 动画接口，但页面托管已不依赖旧 `AXR.Framework.UI.UIPageBase`。
2. `CharacterSelectUIPage`：已完成直接基类迁移。它是 `GameManager` 从菜单进入游戏的主流程必经页；脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；角色选择管理器由 `GameManager` 通过 `OpenContext` payload 显式传入，页面不再保留空的 Inspector Manager 字段或 `CharacterSelectionManager.Instance` 兜底。
3. `GamingUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，旧 `OnPageTick()` 改为 `RequiresTick` + `OnTick()`；Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；战斗 HUD 文本、角色状态、Buff、暂停按钮和摇杆输入由页面直接编排，Buff Tooltip 已改由 Orange Tooltip 托管；该页面 Prefab 没有真实背包子部件，因此已移除无效 `InventoryUI` 字段和背包上下文依赖。
4. `ShopUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，打开上下文直接读取 `OpenContext` payload；Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；商店列表、属性侧栏、背包侧栏仍作为页面内部业务子视图保留，不注册进全局 Catalog。
5. `GamePauseMenu`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosingAsync()` / `OnClosed()`；关闭时等待 `SettingsPanelManager.HideAsync()`，Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；暂停页当前只管理按钮和设置面板真实子部件，不再保留没有 Prefab 对应的背包字段或 `PauseMenuContext`。
6. `GameOverUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；该页面不再依赖旧 `AXR.Framework.UI.UIPageBase`。
7. `StageCompleteUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，Prefab 显式挂载 `UIMotionTransition` 复用现有 `UISequenceDirector` 动画；当前 Prefab 仍复用结算页按钮与动画结构，并用汇总文本显示通关快照，最终 Play Mode 验收需要重点检查视觉排版。
8. `WaveTransitionUIPage`：已完成直接基类迁移。脚本已改为继承 `Orange.UIFramework.PageBase`，生命周期使用 `OnOpeningAsync()` / `OnClosed()`，Prefab 显式挂载 `UIMotionTransition` 指向现有 `UISequenceDirector`；该页面包含升级卡片组和宝箱饰品选择容器，最终 Play Mode 验收需要重点检查波次切换状态快照、升级卡刷新、饰品选择和事件解绑。

迁移时：

- 不把旧 `Regions` 目录整体搬进框架。
- 商店、背包等业务内部可保留子视图类，但按 `ViewPart` 简化。
- 旧 `IPageContext` 空标记接口已删除；页面上下文现在直接作为 `OpenContext` payload 传递，只有需要释放 Facade 的上下文才实现 `IDisposable`。
- 不为 UI 调用 `ShopManager`、`InventoryOperateManager` 这类直接业务 Manager 再包 Facade；只有存在多个实现、跨场景替换或测试替身的真实边界才考虑接口。
- 每个迁移批次必须更新本文和 `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`，执行匹配的轻量验证并提交；不要为单个小接口或小包装类频繁提交。
- 迁移期旧页面基类和旧 UIManager 只允许作为临时脚手架出现，不是最终交付形态。当前已完成脚手架清理：业务页面直接基于 `Orange.UIFramework` 下的 `UIManager`、`PageBase`、`PopupBase`、`ModalBase`、`TooltipBase` 和 `ViewPartBase`，旧 `AXR.Framework.UI` 页面托管、旧 Catalog、旧 Navigation、临时非泛型 Type API 和旧资源已删除。
- 当前阶段 12 的既定业务页面已全部接入 `OrangeUIViewCatalog`。最终收口已完成旧页面托管清理：`GameManager` 业务入口直接引用新 `Orange.UIFramework.UIManager`，页面切换改为 UniTask 顺序等待，不再依赖旧 `AXR.Framework.UI.UIManager.BeginTransition()`；`MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`ShopUIPage`、`GamePauseMenu`、`GameOverUIPage`、`StageCompleteUIPage`、`WaveTransitionUIPage` 与阶段清单外 `BookUIPage` 已直接继承新 `PageBase`；升级卡测试场景生成模块和测试场景已改为挂载新 `Orange.UIFramework.UIManager`。`GameManager` 与 `UpgradeCardTestSceneController` 已删除 `FindFirstObjectByType<UIManager>()` 兜底，UIManager 必须由场景显式绑定。
- 商店页面内部不再保留只服务单一页面的 `IPageController`、`IShopPageView`、`ISidebarRegion`、`SidebarRegionGroup`、`SidebarMotionGroup`、`ShopPageController`、`ShopListView`、`ShopItemGroupView`、`ShopSidebarHost`、`ShopPropertiesSidebarView`、`ShopInventorySidebarView`、`ShopPageState`、`SidebarMotion`、`SidebarToggleView` 和 `PropertiesDescriberBinding`。`ShopUIPage` 作为页面编排层直接订阅 `ShopManager`，并显式管理 `ShopItemListUI`、`ShopPropertiesPanel`、`ShopInventoryPanel` 三个真实 `ViewPartBase`；商品列表复用 / 位移动画、属性侧栏 Motion / 属性刷新、库存侧栏 Motion / 背包绑定分别下沉到对应 ViewPart。
- 背包页面内部不再保留只服务 `InventoryUI` 的 `IInventoryRegionView`、`InventoryRegionController`、`InventoryRegionState`、`InventoryListView`、`InventoryOperatePopupHost` 和 `InventoryUiBinder`。`InventoryUI` 已迁为 `ViewPartBase`，只接受外部页面通过 `ConfigureSession()` 注入的 `InventoryOperateManager` 与所属 `UIManager`，不再保留本地序列化 Manager 兜底；它维护当前选中 / 操作项状态，直接渲染背包列表并通过 Orange `UIManager.ShowPopupAsync()` 打开背包物品操作 Popup；背包列表和操作浮层仍是页面私有子 UI，不进入全局 Page Stack。
- 页面私有子视图已从 `Region` 命名和 `Assets/Scripts/UI/Regions` 目录中收口出来，并继续删除只转发一层的商店 / 战斗 / 背包包装类。当前 `GamingUIPage` 直接编排 HUD 文本、角色状态、Buff、摇杆输入和暂停按钮；`ShopUIPage` 管理真实商店 ViewPart；`InventoryUI` 只由真实存在背包子部件的商店库存面板持有和会话注入；`BuffBarUI` 通过 `BeginSession()` / `EndSession()` 接收 Player 与所属 `UIManager`，动态 Buff Tooltip 不再依赖全局入口。
- 页面上下文装配已从 UI 层延迟解析改为业务入口显式装配：`GameManager` 在主场景序列化引用 `CharacterSelectionManager`、`InventoryOperateManager`、`ShopManager` 与 `StageCompleteSummaryManager`；角色选择页直接接收 `CharacterSelectionManager` payload，`GamingPageContext` 只携带战斗 HUD 实际使用的 Player / Wallet，`ShopPageContext` 负责商店与背包会话，`StageCompletePageContext` 负责结算快照，暂停页不再需要 payload。页面本身只接受 `OpenContext` payload，缺失时直接抛出可定位异常，不再自行 `FindFirstObjectByType` 或 Singleton 兜底，也不再保留 `ResolvingInventoryUiFacade` / `ResolvingShopUiFacade` 这类延迟解析桥接层。
- 战斗 HUD 的 Buff Tooltip 已从静态 `UITooltipPresenter.ActivePresenter` / 全局查找、页面内 Presenter 注入链路迁入 Orange Tooltip 管理：`UITooltipPresenter` 重命名为 `DescribableTooltip` 并继承 `TooltipBase`，`TooltipHoverTarget` 直接调用 `UIManager.ShowTooltipAsync<DescribableTooltip>()` / `UpdateTooltipPosition()` / `HideTooltip()`，独立 `Tooltip.prefab` 已注册到 `OrangeUIViewCatalog`，`UI Gaming.prefab` 不再内嵌旧 Tooltip 实例；Tooltip 的唯一实例、指针跟随、边缘裁剪、Raycast 不阻挡、诊断和池化由框架处理。
- 背包物品操作浮层已从页面内部手工 `Instantiate` / `Destroy` 和自建透明关闭遮罩迁移到 Orange Popup 管理：`WeaponOperatePopup` 与 `AccessoryInfoPopup` 继承 `PopupBase`，由 `InventoryUI` 调用 `UIManager.ShowPopupAsync()` 打开并用 `ViewHandle.CloseAsync()` 关闭；两个 Prefab 已注册到 `OrangeUIViewCatalog`，外部点击关闭、PopupStack、池化和输入焦点交由框架处理。
- `ItemQualityPreviewSceneController` 是 `Item Quality Preview` 独立场景的视觉预览工具，不属于运行时 UI 框架入口。该工具只允许在预览场景中直接 `Resources.Load` / `Instantiate` 背包格子、商店卡片和操作 Popup 做品质表现对照；误挂到其他场景会警告并停止执行。运行时 Popup / Tooltip / Page 仍必须走 Orange `UIManager`。
- `IPlayerHudFacade` 已确认没有实现、调用或 Prefab / Scene 引用并删除；`IInventoryUiFacade`、`IShopUiFacade`、`IInventoryFacadeContext`、`ICharacterSelectionService` 和两个 Manager Facade 也已重新判定为绕路抽象并删除。当前保留的是 `UI/Contexts` 与 `UI/Snapshots` 下的页面 payload 和背包快照等真实业务数据边界。
- 原 `UI/Contracts` 目录已按职责拆分并继续收口：页面 payload 位于 `UI/Contexts`，背包快照和操作 payload 位于 `UI/Snapshots`，`UI/Facades` 目录已删除。无额外语义的 `IPageContext`、`IInventoryUiFacadeHost` 空标记接口和只转发释放的 `PageContextBinding` 已删除；页面上下文不再持有需要释放的 Facade，业务页面关闭时只清理自身绑定。
- 业务容器侧未被接口化消费的 `IContainerQualityRender` 与 `IConfigurable<T>` 已删除，`UIContainerBase` 和 `InventoryOperatePopupBase` 保留具体 `RenderQuality()` / `Configure()` 方法供子类和调用点直接使用。
- 业务子视图不再直接读取 `UIManager.Instance` 打开 Popup / Tooltip。`ViewHandle` 记录所属 `UIManager`，`ViewBase.OwnerUIManager` 由 Page 显式注入给 `InventoryUI`、`BuffBarUI` 和 `TooltipHoverTarget`，缺少装配时直接报错。设置面板、Buff、角色选择卡片 / `CharacterListUI`、描述器 `Describer`、背包物品、属性容器、升级卡片组、卡片动效和品质表现等挂在 Prefab 上的业务 UI 组件已迁入 `ViewPartBase`。
- 升级卡刷新链路已从 `WaveTransitionManager.FindFirstObjectByType<WaveTransitionUpgradeCardGroup>()` 改为事件协作：Manager 发布 `UpgradeCardsRefreshOutRequestedEvent`，`WaveTransitionUIPage` 调用自身持有的 `WaveTransitionUpgradeCardGroup.PlayRefreshOutAsync()` 后发布 `UpgradeCardsRefreshOutCompletedEvent`，Manager 再重新发牌。业务 Manager 不再查找 UI ViewPart；升级卡点击提交、卡片选择动效和刷新退场等待已从 Coroutine / IEnumerator 收口为 UniTask，`WaveTransitionUIPage` 使用页面生命周期 CTS 防止池化关闭后动画任务继续跨页面持有状态。
- 设置面板当前由 `MenuUIPage` / `GamePauseMenu` 显式持有 `SettingsPanelManager`，显示 / 隐藏、交互开关、关闭等待和音量 PlayerPrefs 均由该 `ViewPartBase` 自身管理。设置面板 Motion 在 `Awake()` 中先刷新默认起点再采样隐藏态，Prefab 上关闭该面板自身的 `refreshDefaultsOnEnable`，避免复用后把隐藏态误采为默认起点；卡片表现边界保持为业务侧 `CardMotionController` / `CardQualityVisualController` 调用 Orange `UIMotionPlayer`，不把卡片品质、浮动、指针倾斜等业务表现语义塞进 Orange Motion 核心。
- 最新资源补漏已补齐卡片品质表现的真实绑定：`Upgrade Container`、`Inventory Item`、`Weapon Operate Popup`、`Accessory Info Popup` 均显式挂载并绑定 `CardQualityVisualController`；`Inventory Item.prefab` 的旧 `colorDependencyGraphics` 与 `tooltipHoverTarget` 序列化残留已清理，`UI Stage Complete.prefab` 的旧 `summaryManager` 残留也已移除，品质表现不再依赖静默 `GetComponent` 兜底。
- 最终静态收口已删除 `UIPageContextFactory` 和旧 `Assets/Scripts/UI/Pages.meta` 空目录残留；`GameManager` 直接在业务入口创建 `GamingPageContext`、`ShopPageContext`、`StageCompletePageContext`，并在缺少显式场景引用时抛出可定位异常。`UpgradeCardSystemBuilder.ConfigureViewCatalog()` 已补齐 `popup.inventory.weaponOperate`、`popup.inventory.accessoryInfo` 和 `tooltip.describable`，不会在重建升级卡系统时把 `OrangeUIViewCatalog.asset` 回写成只包含 Page 的不完整配置。
- `OrangeUIViewCatalog` 的 EditMode 断点已覆盖所有业务 Page、背包操作 Popup 与描述 Tooltip 的注册、Kind、Layer、单例和池化配置；旧 UI 托管关键字、Facade / Region / Contract 包装、旧空字段和 Missing Script 静态扫描均无运行时代码或真实资源命中。剩余工作应转为真实场景手动验证，而不是继续补兼容层或桥接抽象。
- 旧 `AXR.Framework.UI` 命名空间已清空：`UIClickTarget`、`IUIRuntimeMotion`、`UISequenceDirector`、`UIMotionPlayer`、Motion Track、对应编辑器脚本和 Motion 资产类型记录已迁入 `Orange.UIFramework`；脚本类名保持不变，避免影响业务开发体验。
- `UIMotionDefinitionEditor` 与 `UIMotionPlayerEditor` 已显式继承 `UnityEditor.Editor`，避免 `Orange.UIFramework.Editor` 子命名空间与 Unity 编辑器基类名发生解析冲突。
- 2026-05-06 原静态扫描只证明旧 `AXR.Framework.UI`、旧 `UIPageBase`、旧 Catalog / Navigation / Type API、旧 `Region` / `Contract` 目录、页面手工 Tooltip / Popup 托管、业务 UI 直接读取 `UIManager.Instance` 和 Missing Script 等表层残留未命中；它不能等同于业务 UI 深度迁移完成。后续已继续收口 Facade 绕路和业务 `ViewPartBase` 迁移。
- 当前真实场景手动验证清单尚未执行；用户已明确要求先开始迁移，因此 `MenuUIPage` 已在记录风险后推进。后续迁移仍应尽快补真实场景验证，不能把 EditMode 测试等同于完整 Play Mode 验收。
- 按用户最新要求，迁移过程不再对每个模块执行耗时完整回归；每个模块保留最小必要验证，重点保证 Catalog 可解析、Unity 编译 / 关键 EditMode 不破坏，并在全部迁移完成后做一次真实 Play Mode 验收，目标是打开游戏即可直接测试。

## 23. 测试计划

EditMode 测试：

- `ViewCatalog` 重复 Id 校验。
- `ViewCatalog` Prefab 类型校验。
- `UIRuntimeState` PageStack、ModalStack、PopupStack。
- `ViewRuntimePhase` 状态转换与重复关闭保护。
- request version 只允许最新页面切换请求生效。
- `OpenContext.GetPayload<T>()`。
- `LocalizationService.GetText()` 参数替换。
- `FloatingViewPositioner` 在 Overlay / Camera 模式下的坐标转换和边缘裁剪。

当前已落地的 EditMode 测试分为两类：测试用例位于 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/`，需要被 `AddComponent<T>()` 挂载到测试 GameObject 上的 View 桩位于 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/`，并使用 `UNITY_EDITOR` 限定，避免进入正式 Player 构建。

第一批规则测试覆盖：

- `ViewCatalog` 重复 Id、Kind 与基类不匹配、禁止注册 `Part`、Prefab 根节点缺 `ViewBase`。
- `OpenContext.GetPayload<T>()` 与 `TryGetPayload<T>()`。
- `LocalizationService.GetText()` 参数替换、默认语言回退、缺 key 返回 key。
- `FloatingViewPositioner` 首选方向出界时自动翻转，以及超大浮层边界裁剪。

第二批运行时 EditMode 测试覆盖：

- `UIManager` Page 打开关闭后 PageStack 更新、关闭后池化、二次打开复用同一实例。
- 连续 `ReplacePageAsync` 时 request version 只允许最新请求生效，旧请求取消后不覆盖新页面。
- 同一 `ViewHandle` 重复关闭只触发一次关闭生命周期和一次回收。
- Popup 外部点击关闭顶层 Popup。
- Modal 结果只完成一次，Modal 打开时阻断下层 Page 输入，遮罩点击可兜底返回取消结果。
- Tooltip 靠近屏幕边缘时记录裁剪 / 翻转定位诊断。
- `GetRuntimeDiagnostics()` 输出 PageStack、PopupStack、ModalStack、Tooltip、遮罩、外部点击拦截器和输入焦点状态。
- `UIMotionPlayer.refreshDefaultsOnEnable` 使用真实 `UIMotionPlayer` + `UIMoveMotionTrack` 验证池化复用后重新启用会以当前坐标作为动画默认起点。

PlayMode 测试：

- Overlay 模式打开/关闭 Page。
- Camera 模式打开/关闭 Page。
- Modal 遮罩阻挡下层输入。
- Tooltip 屏幕边缘裁剪。
- Popup 点击外部关闭。
- 页面关闭等待动画完成后回收。
- 池化对象二次打开不会残留旧事件。
- 池化对象二次打开时 UIMotion 默认快照正确刷新。
- 快速连续 `ReplacePageAsync` 后最终只保留最后请求页面。
- Modal 确认、取消、销毁、外部取消不会重复完成结果。
- `LogRuntimeDiagnostics()` 与 `GetRuntimeDiagnostics()` 能输出当前运行时状态。

手动验证（业务迁移前强制门禁）：

业务页面迁移开始前必须完成以下真实场景手动验证。此清单不是建议项；任一阻断问题未解决前，不进入阶段 12。验证结果必须记录到 `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md` 的详细进度日志，至少包含验证日期、Unity 场景、Canvas 模式、分辨率、通过 / 失败项、失败日志或截图位置、处理结论。若用户明确要求跳过某项，必须记录跳过原因和对应风险。

验证前置条件：

- 必须在当前 worktree 项目中验证：`C:\Users\AXR\.codex\worktrees\f02c\Survivors`，不能误用主工作区 `E:\AXR_Projects\unity\Survivors`。
- Unity Editor 编译无错误；现有 OrangeUIFramework EditMode 测试重新通过，当前基线应为 `total=19 passed=19 failed=0 skipped=0`。
- 使用真实 `UIManager`、`UIFrameworkSettings`、`ViewCatalog`、`CanvasProfile`、Prefab、`EventSystem`、`CanvasScaler`、`GraphicRaycaster` 和 `UIMotionPlayer`；不能只用测试 Harness 或临时纯代码对象替代。
- 验证场景必须包含 Overlay 和 Camera 两套配置。Camera 模式必须显式绑定 `uiCamera`，不得依赖 `Camera.main` 静默兜底。
- 验证 Prefab 不直接迁移现有业务页面逻辑，但需要覆盖业务迁移会依赖的真实结构：Page、页面子级 `ViewPart`、Popup、Modal、Tooltip、入场 / 退场动画、TMP 文本、本地化组件。

迁移前必须逐项验证：

- Root 与 Canvas：Overlay 模式能创建或复用 Root Canvas，标准 Layer 全部存在，SortingOrder、Raycaster、CanvasScaler 参数正确；Camera 模式能绑定指定 UI Camera，`planeDistance` 正确，Root 不漂移、不重复创建。
- Catalog 与 Prefab：Catalog 校验能发现缺 Prefab、缺 `ViewBase`、Kind 与基类不匹配、重复 id；所有真实验证 Prefab 根节点具备 `RectTransform`、`CanvasGroup` 和对应 View 脚本，无 Missing Script、无丢失引用。
- Page 生命周期：真实 Page 能打开、替换、重置、关闭；`PageStack` 顺序正确；只有顶层 Page 可交互；关闭必须等待退场动画完成后再回收；二次打开池化实例不会残留旧事件、旧 payload、旧输入状态。
- 异步与防重入：快速连续点击打开、关闭、Replace、Reset 时最终只保留最后一次请求的页面；关闭中重复关闭不重复触发 `OnClosed()`；打开中取消、关闭中退出播放或切场景不会让 View 卡在 `Opening` / `Closing`。
- UIMotion 与池化复用：真实 DOTween 入场 / 退场动画能被 UniTask 等待；暂停时间缩放为 0 时 UI 动画仍按预期播放；复用后 `refreshDefaultsOnEnable` 会重新采样起点，Hover / Click / Alpha / Scale / Position 不沿用上次关闭前的错误状态。
- Popup：锚定到格子、按钮或列表项时位置正确；屏幕坐标打开正确；点击外部按配置关闭；同组互斥只保留最新 Popup；多 Popup 场景只让栈顶处理外部点击；关闭回收后再次打开不重复订阅业务事件。
- Modal：Modal 打开时遮罩显示并阻挡下层 Page / Popup 输入；只有最顶层 Modal 可交互；确认、取消、遮罩点击、外部取消、对象销毁都只能完成一次结果；多层 Modal 关闭后下层 Modal 或 Page 输入恢复正确。
- Tooltip：指针进入、移动、退出流程正确；Tooltip 跟随指针但不阻挡 Raycast；屏幕四角和边缘会自动翻转 / 裁剪；Camera Canvas 下不漂移；内容变化或语言切换后尺寸与位置不会溢出。
- ViewPart：使用类似商店页面的真实结构验证左右滑动栏、购物栏、列表和详情区；这些子级只由 Page 持有和绑定，不注册进全局 Catalog；Page 关闭时子级解绑一次，重新打开后事件和动画状态正确。
- 本地化：打开中的 Page、Modal、Tooltip 切换语言后文本刷新；参数化文本正确替换；缺 key 能回退为 key 并输出可定位日志；TMP 字体 fallback 在中英文和数字混排下显示正常。
- 分辨率与边界：至少验证 `1920x1080`、`1366x768`、`2560x1440`、竖向或超宽窗口；窗口尺寸变化后 Popup / Tooltip 重新定位，CanvasScaler 不导致文本、按钮、遮罩、浮层越界或点击区域错位。
- 输入链路：真实 `EventSystem` 下鼠标点击、指针移动、滚动列表、按钮连点、外部点击关闭都正确；Modal 打开时下层按钮、滚动和拖拽不响应；Tooltip 不抢占点击。
- 诊断入口：Play Mode 中点击 `UIManager` Inspector 的 `Log Runtime Diagnostics` 按钮，日志必须包含 PageStack、PopupStack、ModalStack、Tooltip、ModalMask、外部点击拦截器、输入焦点、池化数量、定位裁剪结果和卡住的 Opening / Closing View。
- 退出与回收：关闭所有 UI、退出 Play Mode、重新进入 Play Mode 后无异常日志；对象池数量符合预期；场景中没有残留多余 Root、Layer、遮罩、外部点击拦截器或未关闭 Tooltip。

通过标准：

- 全部必测项通过，且验证记录已写入实施计划。
- 失败项必须先修复并提交，再重新执行相关手动验证。
- 原规则要求只有清单通过后才能开始迁移 `MenuUIPage`、`GamingUIPage`、`ShopUIPage` 等现有业务页面；2026-05-05 用户明确要求先开始迁移，因此当前已作为例外记录风险并完成 `MenuUIPage` 迁移期接入。
- 该例外不取消清单本身：后续仍需尽快执行真实场景手动验证，且每个业务模块迁移完成后都必须记录验证情况和遗留风险。

## 24. 不应加入的冗余功能

不做：

- UI Toolkit 支持。
- Addressables 依赖。
- 自动生成所有页面代码。
- 可视化路由图。
- 复杂状态机。
- 全局事件总线。
- 每个按钮的通用 Command 系统。
- 多皮肤主题系统。
- 世界空间 UI。
- 运行时热更新 UI Prefab。

这些能力只有在项目出现真实需求后再加。

## 25. 最终完成标准

OrangeUIFramework 完成时应满足：

- 一个 `UIManager` 能在 Overlay 和 Camera 模式下构建 Root 与 Layer。
- Catalog 能注册 Page、Popup、Modal、Tooltip。
- Page 支持 Push、Replace、Reset、Back。
- Popup 支持锚定、外部点击关闭、分组互斥。
- Modal 支持遮罩、输入独占、异步结果。
- Tooltip 支持指针跟随、屏幕裁剪、不阻挡输入。
- ViewPart 能作为页面内部子视图稳定使用。
- UIMotion 能作为默认动画系统继续使用，并修复已知缺陷。
- 多语言文本能通过 `LocalizedText` 和 `ILocalizationService` 刷新。
- 旧 UI 页面可以逐步迁移，不需要一次性推倒。
- 框架代码中类名、字段名、函数名不滥用 `Orange`。
