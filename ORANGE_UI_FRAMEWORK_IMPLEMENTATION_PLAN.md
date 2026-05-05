# OrangeUIFramework 实施计划与进度记忆

## 1. 文档定位

本文是 OrangeUIFramework 长周期实现任务的执行总控文档。后续每一轮工作都必须先阅读本文，再结合当前代码状态决定下一步。若对话上下文被压缩，本文是恢复任务理解、阶段边界、进度状态和下一步行动的首要依据。

配套设计文档：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`：架构、API、规则、示例与验收标准。
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`：实施顺序、进度记录、提交策略、上下文恢复规则。

强制原则：

- 先完整搭建框架，再迁移现有项目 UI 页面。
- 迁移前必须有可运行的 UIManager、Catalog、Canvas、Layer、Page、Popup、Modal、Tooltip、ViewPart、UIMotion UniTask 适配、定位裁剪、诊断能力。
- 每完成一个可独立回退的阶段都要及时提交，避免大批量修改难以回滚。
- 每轮工作必须详细保存进度，下一轮必须读取进度并分析后再继续。

## 2. 当前基线

- 当前基线提交：`e80b9a0 接入 UniTask 本地包依赖`。
- 当前工作分支：`codex/orange-ui-framework-plan`。
- Unity 版本：`2022.3.62f3c1`。
- 已接入 UniTask 包：`com.cysharp.unitask`。
- 当前主要规划文档：`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 当前仅完成规划，尚未开始框架代码实现。

## 3. 每轮强制工作流程

每一轮开始时必须执行：

1. 查看 Git 状态：`git status --short --branch`。
2. 阅读本文的 `当前进度快照`、`详细进度日志`、`下一轮入口`。
3. 阅读 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 中与本轮目标相关的章节。
4. 如本轮涉及 Unity UI、异步、架构、Inspector、测试，读取对应 Unity Skill 指南。
5. 明确本轮只做一个可提交的目标，避免跨阶段混改。
6. 如果发现用户或其他工具已改动相关文件，先理解并保留，不得随意回退。

每一轮结束前必须执行：

1. 更新本文的 `当前进度快照`。
2. 在 `详细进度日志` 增加一条记录，写明完成内容、修改文件、验证情况、遗留风险。
3. 更新 `下一轮入口`，明确下一轮应先做什么、不能做什么。
4. 运行与本轮改动匹配的验证，无法运行时写明原因。
5. 若达到可独立回退边界，执行 Git 提交。

如果上下文被压缩，恢复顺序：

1. 读本文。
2. 读最新 Git log 与 status。
3. 读 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的相关章节。
4. 只读取当前阶段相关代码，不递归扫描整个项目。
5. 根据 `下一轮入口` 继续，不从头重做方案。

## 4. 提交策略

提交规则：

- 每个提交只覆盖一个阶段或一个清晰子目标。
- 提交前必须确保 `git status --short` 中的变更都是本次目标相关。
- 提交信息使用中文，正文说明原因、关键修改点、影响范围和验证情况。
- 不把大量未验证迁移和框架底层重构塞进同一个提交。
- 出现问题时优先回退最近一个小提交，而不是回退大批量混合修改。

建议提交边界：

1. 文档与实施计划提交。
2. 目录与基础数据类型提交。
3. UIManager Root / Canvas / Layer / Catalog 校验提交。
4. Page 生命周期与 UniTask 打开关闭提交。
5. 异步防重入与 request version 提交。
6. UIMotion UniTask 适配与 `refreshDefaultsOnEnable` 修复提交。
7. Popup / Modal / Tooltip 基础管理提交。
8. FloatingViewPositioner 定位裁剪提交。
9. 运行时诊断提交。
10. 测试提交。
11. 业务页面迁移按页面或功能组分别提交。

## 5. 实施阶段总览

### 阶段 0：规划与准备

目标：

- 完成设计文档与实施计划。
- 确认 UniTask 已在主分支接入。
- 创建独立工作分支。

验收：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 存在且包含框架设计。
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md` 存在且包含进度记忆规则。
- 已提交规划文档。

状态：已完成。

### 阶段 1：框架目录与基础类型

目标：

- 创建 `Assets/Scripts/OrangeUIFramework/` 目录结构。
- 创建核心枚举、上下文、结果、句柄、选项、诊断数据结构。
- 先只搭类型，不迁移业务页面。

建议文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewKind.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewRuntimePhase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/OpenContext.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/CloseReason.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewHandle.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PopupOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/TooltipOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ModalResult.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`

验收：

- Unity 编译通过。
- 类型命名不滥用 `Orange`。
- 不改旧 UI 页面。

### 阶段 2：配置资产与 Catalog 校验

目标：

- 实现 `UIFrameworkSettings`、`CanvasProfile`、`ViewCatalog`、`ViewDefinition`、`LayerDefinition`。
- 支持 Overlay / Camera Canvas 配置。
- 加入启动期校验和编辑器校验入口。

要求：

- Camera 模式必须显式配置 `worldCamera`。
- Catalog 重复 id、重复类型、Prefab 缺 ViewBase、Kind 与基类不匹配都要报错。
- 暂不接业务页面迁移。

验收：

- 可创建 Settings / CanvasProfile / ViewCatalog 资产。
- 缺相机、缺 Prefab、重复 id 能给出明确错误。

### 阶段 3：UIManager Root / Canvas / Layer

目标：

- 新建或迁移 `UIManager`，保留它作为运行时总调度器。
- 支持从配置创建或复用 Root Canvas。
- 构建标准层级：Background、Hud、Page、Popup、ModalMask、Modal、Tooltip、System、Debug。
- 保留同步兼容 API 的基础入口。

要求：

- 不引入 `UIService` 平行入口。
- 示例和业务优先直接引用 `UIManager`。
- `IUIManager` 仅作为可选接口边界。

验收：

- Overlay 模式能创建完整 Root / Layer。
- Camera 模式能绑定 UI Camera。
- `LogRuntimeDiagnostics()` 至少能输出 Root 与 Layer 状态。

### 阶段 4：ViewBase / PageBase 生命周期与 UniTask API

目标：

- 实现 `ViewBase`、`PageBase`、`PopupBase`、`ModalBase<TResult>`、`TooltipBase`、`ViewPartBase`。
- 在 UIManager 上实现 `OpenPageAsync`、`ReplacePageAsync`、`ResetToPageAsync`、`CloseTopPageAsync`、`CloseAllPagesAsync`。
- 使用 UniTask 和 CancellationToken。

要求：

- 旧同步 API 可保留，但新能力以 UniTask 为主。
- 只有需要 Tick 的 View 进入 Tick 列表。
- 不迁移业务页面。

验收：

- 一个测试 Page 可打开、关闭、替换、重置。
- 关闭等待生命周期执行顺序正确。

### 阶段 5：异步防重入与 request version

目标：

- 实现 `ViewRuntimePhase` 状态机。
- 实现 request version，避免旧异步请求覆盖新请求。
- 防止重复关闭、重复回收、Modal 多次完成。

验收：

- 快速连续打开同一 singleton 只产生一个实例。
- 连续 Replace 最终只显示最后请求页面。
- 关闭中重复关闭不重复触发 `OnClosed`。
- 取消请求不泄漏 CTS 和事件。

### 阶段 6：UIMotion UniTask 适配与快照修复

目标：

- 沿用现有 UIMotion 系统。
- 实现 `IViewTransition` 或等价适配层，包装 DOTween 为 UniTask。
- 修复 `UIMotionPlayer.refreshDefaultsOnEnable` 池化复用后动画起点不准。
- 删除或实现空的 Timing API。

验收：

- Enter / Exit 可被 UniTask 等待。
- 池化 Popup / Page 二次打开时动画起点正确。
- Hover / Click 后关闭回收，再打开不沿用错误 Scale / Alpha / Position。

### 阶段 7：Popup / Modal / Tooltip 管理

目标：

- UIManager 管理 PopupStack、ModalStack、当前 Tooltip。
- Modal 支持遮罩、输入独占、结果返回。
- Popup 支持锚定、外部点击关闭、分组互斥。
- Tooltip 支持指针跟随、不阻挡输入。

验收：

- ConfirmDialog 返回确认 / 取消。
- 物品操作 Popup 可按组互斥。
- Tooltip 不使用静态 ActivePresenter 或 `FindFirstObjectByType` 常规兜底。

### 阶段 8：FloatingViewPositioner 定位裁剪

目标：

- 实现统一定位裁剪工具。
- 支持 Overlay / Camera 模式。
- 支持屏幕坐标、Anchor、偏移、边距、自动翻转、裁剪结果诊断。

验收：

- 屏幕四角 Tooltip 不出屏。
- Camera Canvas 下 Popup 不漂移。
- 分辨率变化后锚定位置正确。

### 阶段 9：本地化基础能力

目标：

- 实现 `ILocalizationService`、`LocalizedText`、可替换语言表来源。
- 支持语言切换刷新和参数化文本。

验收：

- 已打开 UI 能响应语言切换。
- 参数化文本正确替换。
- 不强制依赖 Unity Localization 包。

### 阶段 10：运行时诊断与调试入口

目标：

- 实现 `LogRuntimeDiagnostics()`。
- 实现 `GetRuntimeDiagnostics()` 结构化快照。
- UIManager Inspector 增加诊断按钮。

验收：

- 能输出 PageStack、PopupStack、ModalStack、Tooltip、池化数量、异步请求状态、定位裁剪结果。
- 能定位卡在 Opening / Closing 的 View。
- 诊断不依赖全局 Find 扫场景。

### 阶段 11：测试

目标：

- 给纯逻辑写 EditMode 测试。
- 给 Unity 生命周期、Canvas、输入、动画等待写 PlayMode 测试。

优先测试：

- Catalog 校验。
- RuntimeState / request version。
- FloatingViewPositioner。
- Modal 结果互斥。
- UIMotion refresh defaults。
- Pooling 二次打开。

验收：

- 关键测试通过。
- 无法自动化的场景写入手动验证清单。

### 阶段 12：项目页面迁移

目标：

- 在框架能力完整后，才迁移现有项目页面。
- 迁移按页面或功能组拆提交。

顺序：

1. `MenuUIPage`
2. `CharacterSelectUIPage`
3. `GamingUIPage`
4. `ShopUIPage`
5. `GamePauseMenu`
6. `GameOverUIPage`
7. `StageCompleteUIPage`
8. `WaveTransitionUIPage`

要求：

- 不把旧 `Regions` 目录整体搬进框架。
- 商店、背包等业务内部可保留子视图类，但按 `ViewPart` 简化。
- 每迁移一个页面，必须保留可回退提交。

## 6. 当前进度快照

当前阶段：阶段 12 最终收口；阶段 12 既定业务页面与补漏页面 `BookUIPage` 均已完成直接基类迁移。`GameManager`、升级卡测试场景生成模块、`Game Scene`、`UI Test Scene` 与 `Upgrade Card Test Scene` 已直接使用 Orange `UIManager`、Orange Settings 和 Orange `ViewCatalog`，`GameManager` 与 `UpgradeCardTestSceneController` 不再通过 `FindFirstObjectByType<UIManager>()` 兜底，缺少 UIManager 会直接暴露装配错误。旧 `AXR.Framework.UI` 页面托管、旧 `UIManager`、旧 `UIPageBase`、旧 Navigation、旧 `UIPrefabCatalog` / `UIFrameworkSettings` 资源和新 `UIManager` 迁移期非泛型 Type API 已清理；商店页面内部 `IPageController`、`IShopPageView`、`ISidebarRegion`、`SidebarRegionGroup`、未使用 `SidebarMotionGroup` 已收口删除；背包页面内部 `IInventoryRegionView`、`InventoryRegionController`、`InventoryRegionState` 已收口删除，`InventoryUI` 直接组合 Facade、列表子视图和 Orange Popup Host；页面私有子视图已从 `Region` 命名和 `Assets/Scripts/UI/Regions` 目录收口到普通 `View` / `Host` / `Binder` 命名；`UIPageContextFactory` 与页面 payload 装配已改为由 `GameManager` 显式提供 Player / InventoryOperateManager / ShopManager / StageCompleteSummaryManager，并删除两个 Resolving Facade；战斗 HUD Buff Tooltip 已从页面内 Presenter 注入链路迁入 Orange Tooltip 管理；背包物品操作浮层已迁入 Orange Popup 管理并删除旧未引用操作容器；未使用的 `IPlayerHudFacade`、无额外语义的 `IPageContext` / `IInventoryUiFacadeHost` 空标记接口和只转发释放的 `PageContextBinding` 已删除，原 `UI/Contracts` 已按职责拆为 `UI/Contexts`、`UI/Facades`、`UI/Snapshots`，只保留页面 payload、Inventory / Shop Facade、背包快照等真实业务边界；业务子视图已不再直接读取 `UIManager.Instance`，由 `ViewHandle.Owner` / `ViewBase.OwnerUIManager` 传递显式 UIManager；旧动画 / 点击组件和 Motion 资产类型记录已迁入 `Orange.UIFramework`，旧 `AXR.Framework.UI` 命名空间不再保留运行时代码；UIMotion 自定义 Inspector 已显式继承 `UnityEditor.Editor`，避免与 `Orange.UIFramework.Editor` 命名空间发生基类解析冲突；2026-05-06 已完成阶段 12 静态收口扫描，当前业务 UI 运行时代码未发现旧托管、空接口、未登记全局 View、Missing Script 或旧 UI 资源引用残留。

已完成：

- 主分支 UniTask 包已更新到当前工作树。
- 已创建工作分支 `codex/orange-ui-framework-plan`。
- 已完成 OrangeUIFramework 设计文档初稿。
- 已明确保留 `UIManager` 作为运行时总入口。
- 已明确示例代码直接引用 `UIManager`。
- 已明确加入异步防重入、FloatingViewPositioner、运行时诊断、UIMotion refresh defaults 修复。
- 已提交规划文档，最新规划提交为 `78dbb47 规划 OrangeUIFramework 框架实施方案`。
- 已创建 `Assets/Scripts/OrangeUIFramework/Core/Runtime/`。
- 已新增基础运行时类型：`ViewKind`、`ViewRuntimePhase`、`CloseReason`、`OpenContext`、`ViewHandle`、`PopupOptions`、`TooltipOptions`、`ModalResult`、`UIRuntimeDiagnostics`。
- 为保证阶段 1 可编译闭环，已同步新增最小基类与接口：`IView`、`ViewBase`、`PageBase`、`PopupBase`、`ModalBase<TResult>`、`TooltipBase`、`ViewPartBase`。这些类型只提供后续阶段所需的基础扩展点，完整生命周期和 UIManager 接入仍留到阶段 4。
- 已完成阶段 2 数据资产与校验脚本：`UIFrameworkSettings`、`CanvasProfile`、`ViewCatalog`、`ViewDefinition`、`LayerDefinition`、`ViewLayer`、`ValidationReport`。
- `CanvasProfile` 已支持 Overlay / Camera 配置校验，Camera 模式缺少 `uiCamera` 会报错，WorldSpace 会报错。
- `ViewCatalog` 已校验空 id、重复 id、缺 Prefab、Prefab 根节点缺 `ViewBase`、重复 View 类型、`ViewKind` 与基类不匹配，以及禁止把 `ViewPart` 注册到全局 Catalog。
- `UIFrameworkSettings` 已提供默认标准层级：Background、Hud、Page、Popup、ModalMask、Modal、Tooltip、System、Debug，并校验重复 Layer。
- 已新增新框架 `UIManager` 与 `IUIManager`，保留 `UIManager` 作为具体运行时入口。
- `UIManager` 已支持启动期调用 Settings / Catalog 校验、创建或复用 Root Canvas、应用 Overlay / Camera CanvasProfile、创建标准 Layer 根节点。
- `UIManager` 已提供 `TryGetLayerRoot()`、`GetRuntimeDiagnostics()`、`LogRuntimeDiagnostics()`，可输出 Root、Canvas 模式、相机、Layer 数量、Layer SortingOrder 与 Raycaster 状态。
- 已新增 `Core/Loading/IViewLoader` 与默认 `PrefabViewLoader`，当前以 Catalog Prefab 实例化为默认加载方式，不引入 Addressables。
- `IView` / `ViewBase` 已补齐 `RequiresTick`、`Phase`、`InputActive`、`BlocksRaycasts` 运行时状态。
- `ViewBase` 已提供内部 `OpenInternalAsync()` / `CloseInternalAsync()` 生命周期入口，业务侧继续只重写 `OnOpeningAsync()`、`OnOpenedAsync()`、`OnClosingAsync()`、`OnClosed()`、`OnTick()`。
- `UIManager` 已实现 Page 的 `OpenPageAsync()`、`ReplacePageAsync()`、`ResetToPageAsync()`、`CloseTopPageAsync()`、`CloseAllPagesAsync()` 与同步兼容 `OpenPage()`。
- `UIManager` 已维护 PageStack、运行时实例表、单例实例表、按类型对象池，以及只包含 `RequiresTick` View 的 Tick 列表。
- `UIManager.GetRuntimeDiagnostics()` / `LogRuntimeDiagnostics()` 已包含当前打开 View 与对象池快照。
- `UIManager` 已为 Page 操作加入串行通道，`OpenPageAsync`、`ReplacePageAsync`、`ResetToPageAsync`、`CloseTopPageAsync`、`CloseAllPagesAsync` 不会交叉修改 PageStack。
- `requestVersion` 已作为 Page 操作版本号使用，连续 Replace / Reset / Close / Open 时旧请求会被标记为过期，过期请求不会覆盖最新请求。
- 关闭流程已改为一旦开始就使用框架内部关闭令牌完成关闭，避免调用方取消导致 View 卡在半关闭状态。
- 同实例重复关闭会等待已有关闭任务，不会重复触发 `OnClosed()`、重复回收或重复完成 `ClosedTask`。
- View 诊断已包含每个打开 View 的 request version。
- 已新增 `Assets/Scripts/OrangeUIFramework/Motions/Runtime/`，包含 `IViewTransition`、`UIMotionTransition` 与 DOTween -> UniTask 等待包装。
- `ViewBase` 已接入可选 `IViewTransition`：打开时等待 `PlayEnterAsync()`，关闭时等待 `PlayExitAsync()`，无动画组件时直接完成。
- 旧 `UIMotionPlayer.refreshDefaultsOnEnable` 已修复为每次启用均刷新快照。
- `IUISequenceMotion` 已补充 `RefreshDefaults()`，`UISequenceDirector` 会向子 Motion 传播刷新；`UIMotionTransition` 会在入场前刷新快照再采样 Hidden 状态。
- 已删除旧 `SidebarMotion` / `SidebarMotionGroup` 中无实际效果的空 `ConfigureTimings()` API；业务私有 `GamePauseMenu.PauseMenuPanelBinding.ConfigureTimings()` 暂未迁移，留待业务页面迁移阶段处理。
- `UIManager` 已实现 `ShowPopupAsync()`、`ShowModalAsync()`、`ShowTooltipAsync()`、`UpdateTooltipPosition()`、`HideTooltip()`，不再抛阶段性未实现异常。
- `UIManager` 已维护 `PopupStack`、`ModalStack` 和当前 Tooltip，并复用 `ViewBase` 生命周期、动画等待、关闭任务复用、池化回收链路。
- Popup 已支持分组互斥、外部点击关闭、锚点 / 屏幕点基础定位；默认不冻结 Page 输入，只让栈顶 Popup 自身可交互。
- Modal 已支持统一遮罩、遮罩点击按 `ViewDefinition.CloseOnBackgroundClick` 关闭、顶层 Modal 输入独占、关闭兜底完成取消结果。
- Tooltip 已支持唯一实例、指针跟随更新位置、不阻挡输入，并废弃静态 ActivePresenter / 全局 Find 作为框架路径。
- `PopupOptions.trackInStack` 构造赋值 bug 已修复，字段保留给后续 Back 行为使用；当前阶段 PopupStack 主要用于输入顺序、外部点击和分组关闭。
- 已新增 `FloatingViewPositioner` 定位裁剪工具，统一处理 Popup / Tooltip 的 Overlay / Camera 坐标换算、边缘裁剪、自动翻转和定位结果诊断。
- `PopupOptions` 与 `TooltipOptions` 已支持 `Margin` 和 `PreferredAnchor`，可控制边缘留白与默认展开方向。
- `UIManager` 已移除 Popup / Tooltip 内部临时定位计算，改为调用 `IFloatingViewPositioner`，并在 `ViewDiagnostics` / `LogRuntimeDiagnostics()` 输出定位坐标、ResolvedAnchor、是否翻转、是否裁剪。
- 已新增不依赖 Unity Localization 包的本地化模块：`ILocalizationService`、`LocalizationTable`、`LocalizationService`、`LocalizedText`。
- `LocalizationTable` 支持 ScriptableObject 语言表、重复 key 校验和 `CreateAssetMenu` 创建入口。
- `LocalizationService` 支持当前语言、默认语言回退、语言切换事件、参数化文本替换和运行时校验。
- `LocalizedText` 支持 TMP_Text 自动绑定、语言切换自动刷新、运行时设置 key、设置参数、清理参数。
- `UIRuntimeDiagnostics` 已扩展为结构化快照，包含 PageStack、PopupStack、ModalStack、Tooltip、ModalMask、Popup 外部点击拦截器、输入焦点、异步请求状态、池化数量和定位裁剪详情。
- `UIManager.GetRuntimeDiagnostics()` 已从自身维护的运行时状态生成完整诊断，不依赖全局 `FindObjectsOfType` 扫场景；Opening / Closing / Failed 的 View 也会通过生命周期追踪表进入诊断快照。
- `UIManager.LogRuntimeDiagnostics()` 已输出 Stack 顺序、顶层标记、请求版本、输入状态、遮罩状态、外部点击拦截器状态、定位请求/结果矩形与对象池数量。
- 已新增 `Assets/Scripts/OrangeUIFramework/Editor/UIManagerEditor.cs`，在 `UIManager` Inspector 中提供 `Log Runtime Diagnostics` 按钮，复用运行时同一诊断入口。
- 已新增 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/` 第一批 EditMode 测试，覆盖 `ViewCatalog` 校验、`OpenContext` payload、本地化参数替换 / 默认语言回退 / 缺 key 回退、`FloatingViewPositioner` 自动翻转和边界裁剪。
- 已完成阶段 11 第二批运行时 EditMode 测试：覆盖 Page 打开关闭与池化复用、连续 Replace request version、重复关闭保护、Popup 外部点击、Modal 输入独占 / 结果互斥 / 遮罩兜底取消、Tooltip 边界定位诊断、运行时诊断快照、`UIMotionPlayer.refreshDefaultsOnEnable` 真实 Motion 复用起点。
- 可挂载的测试 View 桩已移动到 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/`，并使用 `UNITY_EDITOR` 编译保护；测试方法和测试装配 Harness 仍保留在 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/`。
- 已在 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划` 中形成业务迁移前必须执行的真实场景手动验证清单，明确它是阶段 12 前的强制门禁，而不是普通建议项。
- 手动验证清单已覆盖 worktree 验证前置条件、Overlay / Camera Canvas、真实 Prefab、Catalog、Page 生命周期、异步防重入、UIMotion / DOTween、Popup、Modal、Tooltip、ViewPart、本地化、分辨率、真实 EventSystem 输入、诊断入口、退出与回收。
- 2026-05-05 用户明确要求“开始迁移”，并要求每迁移完一个模块更新文档和提交；该指令视为对迁移前真实场景手动验证门禁的显式跳过，风险已记录。
- 已完成 `MenuUIPage` 第一模块迁移：该阶段曾采用迁移期桥接方式降低一次性迁移风险；当前旧页面基类桥接已在最终收口中删除。
- 已补迁 `CharacterSelectUIPage` 主流程漏项：`OrangeUIViewCatalog` 新增 `page.characterSelect`，Prefab 指向 `UI Character Selection.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；该页面是 `GameManager` 从菜单进入游戏前的角色选择必经页。
- 旧 `AXR.Framework.UI.UIManager` 曾对注册进 Orange `ViewCatalog` 的页面委托新 `Orange.UIFramework.UIManager` 打开、关闭和查询；当前旧 UIManager 已在最终收口中删除。
- 已在 `Game Scene` 的现有 `UIManager` GameObject 上挂载新 `Orange.UIFramework.UIManager`，并复用现有 Root Canvas。
- 已新增 `OrangeCanvasProfile`、`OrangeUIFrameworkSettings`、`OrangeUIViewCatalog` 资产；当前 Catalog 只注册 `UI Menu.prefab` / `MenuUIPage`，不把其他页面提前纳入新框架。
- 新 `UIManager` 曾补充迁移期非泛型 Page API：`OpenPageAsync(Type)`、`OpenPage(Type)`、`ClosePageAsync(Type)`、`IsOpen(Type)`；当前这些迁移 API 已删除，只保留泛型业务入口。
- 曾新增 `LegacyUIPageBase_CanOpenAndCloseThroughOrangeManagerTypeApi` EditMode 测试覆盖旧基类桥接路径；当前旧基类和该测试已删除。
- 已完成 `GamingUIPage` 第二模块迁移：`OrangeUIViewCatalog` 新增 `page.gaming`，Prefab 指向 `UI Gaming.prefab`，Layer 沿用旧 UI Catalog 的 `Hud` 层；当前 `GameManager` 已直连新 UIManager。
- 已完成 `ShopUIPage` 第三模块迁移：`OrangeUIViewCatalog` 新增 `page.shop`，Prefab 指向 `UI Shop.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；当前 `GameManager` 已直连新 UIManager。
- 已完成 `GamePauseMenu` 第四模块迁移：`OrangeUIViewCatalog` 新增 `page.pause`，Prefab 指向 `UI Pause.prefab`，ViewKind 仍为 Page，Layer 沿用旧 UI Catalog 的 `Popup` 层；当前 `GameManager` 已直连新 UIManager。
- 已完成 `GameOverUIPage` 第五模块迁移：`OrangeUIViewCatalog` 新增 `page.gameOver`，Prefab 指向 `UI Game Over.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；当前 `GameManager` 已直连新 UIManager。
- 已完成 `StageCompleteUIPage` 第六模块迁移：原项目缺少对应 Prefab 和旧 Catalog 注册，本轮补齐 `UI Stage Complete.prefab`，`OrangeUIViewCatalog` 新增 `page.stageComplete`，Layer 使用 `Default/Page`；当前 `GameManager` 已直连新 UIManager。
- 已完成 `WaveTransitionUIPage` 第七模块迁移：`OrangeUIViewCatalog` 新增 `page.waveTransition`，Prefab 指向 `UI Wave Transition.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；当前 `GameManager` 已直连新 UIManager。
- 已新增真实 `OrangeUIViewCatalog.asset` 校验测试，确认 `MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`ShopUIPage`、`GamePauseMenu`、`GameOverUIPage`、`StageCompleteUIPage` 与 `WaveTransitionUIPage` 均可按类型解析并通过 Catalog 校验。
- 已完成最终收口第一步：`Assets/Scripts/Managers/GameManager.cs` 直接引用 `Orange.UIFramework.UIManager`，页面打开 / 关闭改为 `OpenPageAsync<T>()` 与 `ClosePageAsync<T>()`，状态切换和暂停菜单关闭流程使用 UniTask 顺序等待，并用本地 transition version 避免旧异步结果覆盖新状态。
- 已完成 `MenuUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画；设置侧栏仍沿用旧 `IUIRuntimeMotion` 动画接口，但页面托管已不再依赖旧 `UIPageBase`。
- 已完成 `CharacterSelectUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `GamingUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，旧 `OnPageTick()` 改为 `RequiresTick` + `OnTick()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `ShopUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，打开上下文改为直接读取 `OpenContext` payload，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `GamePauseMenu` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，旧 `HasAdditionalCloseWaitActions()` / `PlayAdditionalCloseWaitActions()` 关闭等待扩展点迁到 `OnClosingAsync()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `GameOverUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `StageCompleteUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `WaveTransitionUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 指向原 `UISequenceDirector`。
- 已完成阶段 12 清单外补漏页面 `BookUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，`UI Gold Book.prefab` 增加 `UIMotionTransition` 并移除旧 `autoPlaySequenceDirector` 字段，`OrangeUIViewCatalog` 新增 `page.goldBook`，Catalog 校验测试增加类型断言。
- 已完成升级卡测试场景生成模块迁移：`UpgradeCardSystemBuilder` 不再写旧 `UIPrefabCatalog.asset`，改为写 `OrangeUIViewCatalog.asset` 并在测试场景中挂载新 `Orange.UIFramework.UIManager`；`UpgradeCardTestSceneController` 改为依赖新 `UIManager` 并使用 `ResetToPageAsync<WaveTransitionUIPage>()`。
- 已完成旧 UI 页面托管清理：删除旧 `AXR.Framework.UI.UIManager`、旧 `UIPageBase`、旧 `IUIPage` / `IUIManager`、旧 `UIPageOpenContext` / `UIPageEventArgs` / `UIRuntimeState`、旧 Navigation、旧 `UIPrefabCatalog` / `UIPrefabEntry` / `UILayerType` / `UIFrameworkSettings` 和旧 `UIManagerEditor`。
- 已删除旧资源 `UIFrameworkSettings.asset`、`UIPrefabCatalog.asset`、`UIPrefabCatalogTest.asset`，并将 `Game Scene`、`UI Test Scene`、`Upgrade Card Test Scene` 的旧 UIManager 组件引用收口到新 Orange `UIManager`。
- 新 `IUIManager` / `UIManager` 已补正式 `ClosePageAsync<TPage>()`，`GameManager` 和 `InventoryUI` 不再依赖旧 Type API 或旧 `UILayerType.Popup`；迁移期 `OpenPageAsync(Type)`、`OpenPage(Type)`、`ClosePageAsync(Type)`、`IsOpen(Type)` 和旧基类兼容测试已删除。
- 已完成商店页面内部局部无用抽象收口：`ShopUIPage` 直接持有 `ShopPageController`，`ShopPageController` 直接持有 `ShopUIPage`，商店侧栏 Host 直接操作 `ShopPropertiesSidebarView` 与 `ShopInventorySidebarView`，删除只服务该页面的 `IPageController`、`IShopPageView`、`ISidebarRegion`、`SidebarRegionGroup` 和未使用 `SidebarMotionGroup`。
- 已完成 UI 页面上下文装配收口：`GameManager` 主场景序列化引用 `InventoryOperateManager` 与 `ShopManager`，打开 `GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 时显式创建 payload；`UIPageContextFactory` 不再扫描场景，不再创建延迟解析 Facade；三个页面缺少 payload 时直接抛出明确异常；独立 `InventoryUI` 不再退回到 Resolving Facade。
- 已完成战斗 HUD Buff Tooltip 显式注入收口：`UITooltipPresenter.ActivePresenter` 已删除，`TooltipHoverTarget` 不再全局查找 Presenter，改由 `GamingHudView` 注入到 `BuffBarUI` 并配置到动态生成的 Buff 图标；该阶段仍保留页面内 Presenter 视觉实现。
- 已完成描述 Tooltip 迁入 Orange Tooltip：`UITooltipPresenter` 重命名为 `DescribableTooltip` 并继承 `TooltipBase`，`TooltipHoverTarget` 改用 `UIManager.ShowTooltipAsync<DescribableTooltip>()` / `UpdateTooltipPosition()` / `HideTooltip()`，独立 `Tooltip.prefab` 注册到 `OrangeUIViewCatalog`，`UI Gaming.prefab` 删除旧内嵌 Tooltip PrefabInstance 和页面字段引用，Tooltip 定位、唯一实例、裁剪、诊断与池化交由 Orange TooltipStack 处理。
- 已完成 StageComplete 结算页上下文显式化：新增 `StageCompletePageContext`，`GameManager` 通过显式引用的 `StageCompleteSummaryManager` 创建快照 payload 打开 `StageCompleteUIPage`，页面不再持有或查找 `StageCompleteSummaryManager`。
- 已完成旧动画 / 点击组件命名空间迁移：`UIClickTarget`、`IUIRuntimeMotion`、`IUISequenceMotion`、`UISequenceDirector`、`UIMotionPlayer`、Motion Track 与对应编辑器脚本迁入 `Assets/Scripts/OrangeUIFramework/` 和 `Orange.UIFramework`，并同步更新 Motion 资产中的 `SerializeReference` 类型记录。
- 已完成 UI 子视图 `Region` 命名与目录收口：`GamingHudView`、`GamingInputView`、`ShopListView`、`ShopSidebarHost`、`ShopPropertiesSidebarView`、`ShopInventorySidebarView`、`SidebarMotion`、`SidebarToggleView`、`InventoryListView`、`InventoryOperatePopupHost`、`InventoryUiBinder` 等仍作为页面私有协作对象保留，但不再放在 `Assets/Scripts/UI/Regions`，也不再使用会被误解为框架抽象的 `Region` 命名。
- 已删除未实现、未引用的 `IPlayerHudFacade` 死接口；保留 `IInventoryUiFacade`、`IShopUiFacade`、`IInventoryFacadeContext`、页面 Context 和 Snapshot 作为当前真实跨系统边界。
- 已完成 `UI/Contracts` 目录命名收口：有效页面 payload 移入 `Assets/Scripts/UI/Contexts`，Inventory / Shop Facade 移入 `Assets/Scripts/UI/Facades`，背包快照和操作 payload 移入 `Assets/Scripts/UI/Snapshots`，不再保留容易被误解为无效抽象集合的 `Contracts` 目录。
- 已完成业务子视图 UIManager 入口显式化：`ViewHandle` 携带 Owner UIManager，`ViewBase` 暴露 `OwnerUIManager` 给页面，`GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 在打开时把 UIManager 注入背包和 Tooltip 子视图；业务 UI 目录不再直接读取 `UIManager.Instance`。
- 已修复 UIMotion 编辑器基类解析风险：`UIMotionDefinitionEditor` 与 `UIMotionPlayerEditor` 显式继承 `UnityEditor.Editor`，避免 `Orange.UIFramework.Editor` 子命名空间被误解析为基类类型。
- 已完成页面 Context 空标记接口收口：删除无额外语义的 `IPageContext`，`GamingPageContext`、`PauseMenuContext`、`ShopPageContext` 直接实现 `IDisposable` 托管 Facade 生命周期，`StageCompletePageContext` 保持纯 payload，`PageContextBinding` 只依赖标准 `IDisposable`。
- 已完成页面 Context 释放辅助类收口：删除只转发 `Dispose + null` 的 `PageContextBinding`，`GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 在 `OnClosed()` 中直接释放并清空当前上下文。
- 已完成背包 Facade Host 空标记接口收口：删除 `IInventoryUiFacadeHost`，`InventoryUI` 不再通过父级扫描判断页面宿主，而是根据外部配置的 Facade 或本地显式 `InventoryOperateManager` 决定是否启动。
- 已完成业务容器未消费接口收口：删除仅出现在实现声明中的 `IContainerQualityRender` 和 `IConfigurable<T>`，`UIContainerBase`、`InventoryOperatePopupBase` 继续保留具体 `RenderQuality()` / `Configure()` 方法。
- 已完成阶段 12 静态收口扫描：旧 `AXR.Framework.UI`、旧 `UIPageBase`、旧 Catalog / Navigation / Type API、旧 `Region` / `Contract` 目录、页面手工 Tooltip / Popup 托管、业务 UI 中的 `UIManager.Instance` 直接读取、无消费空接口和 Missing Script 均未发现运行时代码或真实资源残留；`OrangeUIViewCatalog.asset` 当前注册 9 个 Page、2 个 Inventory Popup 和 1 个 Tooltip。

未完成：

- 业务迁移前真实场景手动验证清单仍未执行；当前是按用户明确要求跳过门禁后先推进业务迁移并完成静态收口，Overlay / Camera 真机运行、真实 Prefab、CanvasScaler、输入模块、DOTween 实际播放和 Inspector 诊断按钮仍需 PlayMode 或手动验证。
- 尚未实现独立 PlayMode 测试场景；是否补最小 PlayMode 场景可在下一轮根据清单执行成本决定，但不能替代真实场景手动验证。
- `UI/Contexts`、`UI/Facades` 与 `UI/Snapshots` 当前仍有业务调用链，继续收口时只能按引用链逐项核查；不能一刀切删除。`UI/Facades` 当前保留 Inventory / Shop 相关接口和 Manager Facade，它们是业务 Manager 到 UI 的真实边界，不按桥接层处理。

当前风险：

- 后续实现周期长，必须依赖本文持续记录，否则上下文压缩后容易误迁移旧 UI 或重建无关抽象。
- 框架核心已具备迁移闭环，但真实场景手动验证门禁尚未执行；用户已明确要求先开始迁移，因此当前迁移依赖 EditMode 测试和保守桥接降低风险，后续仍需尽快补真实场景验证。
- 当前旧页面托管桥接、商店页面内部局部接口、上下文工厂延迟解析 Facade、Tooltip 静态 Presenter / 全局查找、页面内 Tooltip Presenter 注入链路、StageComplete 页面级 Manager 查找、旧动画 / 点击命名空间已删除。后续风险集中在业务 Manager 层仍存在非 UI 框架范围的场景查找，以及最终真实 Play Mode 验收尚未执行。
- 阶段 12 静态收口已完成，未发现业务 UI 运行时代码层面的旧框架残留；剩余主要风险从“迁移遗漏”转为“真实 Play Mode 场景装配、Prefab 引用、输入事件和动画播放是否符合预期”。
- 当前 worktree 仍有无关 Unity 自动导入痕迹和第三方插件删除状态：`Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset`、`Assets/Tabsil/Mineral/Scripts/Editor/*.cs` 及其 `.meta`。这些变更不属于 Orange UI 迁移，后续提交必须继续排除，除非用户明确要求处理。
- 用户最新要求是不在每个模块迁移时花过多时间做完整测试验证；后续单模块只做最小必要验证，重点保证 Catalog 可解析、Unity 编译 / 关键 EditMode 不破坏。完整真实 Play Mode 验收放到全部业务页面迁移和旧资源清理完成后执行，目标是打开游戏即可测试。
- UnitySkills 当前连接的是主工作区 `E:\AXR_Projects\unity\Survivors`，不是本 worktree；验证本 worktree 必须显式使用 `-projectPath C:\Users\AXR\.codex\worktrees\f02c\Survivors` 的 Unity batchmode 或确认 Editor 已打开该 worktree。
- Unity 2022.3.62f3c1 + `com.unity.test-framework@1.1.33` 命令行运行测试时不要同时传 `-quit`；该版本会警告 `Running tests from command line arguments will not work when "quit" is specified.`，并可能只完成导入后退出不生成 XML。当前可靠命令是使用 `-batchmode -nographics -projectPath ... -runTests -testPlatform EditMode -testResults ... -logFile ...`，让 Test Runner 的 ExitCallbacks 自行退出。
- Stage 4 的同步兼容 `OpenPage()` 只适合已同步完成的旧式调用；默认新业务仍应使用 UniTask 异步 API。
- Stage 6 只完成动画等待和快照修复，尚未通过 Unity PlayMode 验证实际 Prefab 上的 DOTween 行为；需要 Unity Editor 刷新后检查编译，并在测试阶段补动画等待与池化复用测试。
- Stage 7 暂未实现全局 Back 顺序，`PopupOptions.TrackInStack` 已保留但未作为 Back 行为入口；后续如接 Back 需优先 Modal，再 Popup，再 Page。
- Stage 8 定位算法已做文件级检查，但尚未在 Unity PlayMode 下验证不同 pivot、LayoutGroup、Canvas Scaler、Camera Canvas 和分辨率变化场景；阶段 11 需要补 `FloatingViewPositioner` EditMode 测试和 PlayMode 边界验证。
- Stage 9 只提供框架级本地化基础能力，尚未迁移现有业务页面硬编码文本，也未实现字体按语言自动切换；业务迁移阶段再逐页接入。
- Stage 10 已完成结构化诊断和 Inspector 入口，但尚未在 Unity Editor 中点击按钮验证实际日志输出；阶段 11 或手动验证时需补。
- Stage 11/12 已有 21 个 EditMode 测试通过，但其中运行时测试仍是轻量 Unity 对象 / 手动按钮触发，不等同于真实 PlayMode 场景、真实 Prefab 和完整 EventSystem 输入验证。
- 业务迁移前手动验证清单尚未执行；已按用户明确要求跳过门禁开始迁移，因此必须在后续迁移中持续记录该风险，并优先补真实场景验证。

## 7. 下一轮入口

下一轮必须先做：

1. 读取本文 `当前进度快照` 和 `详细进度日志`。
2. 读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `22. 迁移计划`、`23. 测试计划` 和迁移期记录。
3. 确认最新提交包含阶段 12 静态收口记录，并检查工作树是否只剩无关 Unity 自动导入痕迹或用户另行处理的第三方插件删除状态。
4. 进入真实场景验收准备：优先打开当前 worktree 项目，检查 `Game Scene`、`UI Test Scene`、`Upgrade Card Test Scene` 是否能基于 Orange `UIManager` 正常进入 Play Mode。
5. 按 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划` 执行手动清单的高优先级路径：主菜单 -> 角色选择 -> 战斗 HUD -> 暂停菜单 -> 商店 / 背包 Popup -> Buff Tooltip -> 波次奖励 -> 结算 / 游戏结束。
6. 验收时必须确认旧 `AXR.Framework.UI` 命名空间、旧 `Assets/Scripts/Framework` 目录和旧 Motion 资产类型记录没有因为 Unity 导入或资源合并回流；`UIClickTarget`、`IUIRuntimeMotion`、`UISequenceDirector`、`UIMotionPlayer` 等动画 / 点击组件现在属于 `Orange.UIFramework`。
7. 验证必须使用当前 worktree：`C:\Users\AXR\.codex\worktrees\f02c\Survivors`。UnitySkills 当前连接主工作区时不能直接用于认定 worktree 结果。
8. 使用 Unity batchmode 验证 worktree 时不要传 `-quit`。
9. 如果真实场景验收发现问题，按页面或功能模块拆小修复、更新两份文档并单独提交；不要重新引入旧 UI 托管、兼容桥接层或 `UIService` 平行入口。
10. 如果验收无阻断问题，更新两份文档记录真实场景通过项、未覆盖项和剩余建议，再提交最终验收记录。

下一轮禁止：

- 禁止一次性删除大批旧 UI 资源或脚本而不先确认引用链。
- 禁止跳过文档进度更新和单模块提交。
- 禁止新建 `UIService` 平行入口。
- 禁止把旧 `Regions` / `Contracts` 整体搬进框架。
- 禁止一次性大改旧 UI 框架和业务页面。

## 8. 详细进度日志

### 2026-05-04 阶段 0 规划

完成内容：

- 新增 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，整理 OrangeUIFramework 设计方案。
- 根据用户反馈修正方案：保留 `UIManager`，使用 UniTask，示例直接引用 `UIManager`。
- 从主分支更新 UniTask 包依赖。
- 新增本文作为长周期实现的计划与进度记忆文档。

修改文件：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已确认当前工作树基线包含 `com.cysharp.unitask`。
- 已检查示例代码不再使用 `IUIManager` 转型。

下一步：

- 提交规划文档。
- 压缩上下文后，从阶段 1 开始搭建框架目录与基础类型。

### 2026-05-04 阶段 1 框架目录与基础类型

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Core/Runtime/` 目录，作为新框架 Runtime 基础类型目录。
- 新增核心枚举与上下文：`ViewKind`、`ViewRuntimePhase`、`CloseReason`、`OpenContext`。
- 新增外部句柄与异步结果类型：`ViewHandle`、`ViewHandle<TView>`、`ModalResult`、`ModalResult<TResult>`。
- 新增浮层选项与诊断快照：`PopupOptions`、`TooltipOptions`、`UIRuntimeDiagnostics`、`ViewDiagnostics`、`PoolDiagnostics`。
- 补充最小 `IView`、`ViewBase`、`PageBase`、`PopupBase`、`ModalBase<TResult>`、`TooltipBase`、`ViewPartBase`，用于保证基础句柄泛型约束和后续阶段代码可以稳定接续。
- `PopupOptions` 和 `TooltipOptions` 已处理 `default(...)` 语义，避免 UIManager 后续 API 默认参数产生反向行为。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewKind.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewRuntimePhase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/CloseReason.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/OpenContext.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewHandle.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PopupOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/TooltipOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ModalResult.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/IView.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PageBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PopupBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ModalBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/TooltipBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewPartBase.cs`

验证情况：

- 已确认 UniTask 包依赖存在于 `Packages/manifest.json`。
- 已检查新增类型均位于 `Orange.UIFramework` 命名空间，类名和字段名未滥用 `Orange`。
- 已检查本轮没有修改旧 UI 页面和旧 UIManager 业务调用。
- 当前工作树没有 Unity 生成的 `.csproj`，因此未能通过命令行执行完整 C# 编译；下一轮如 Unity Editor 已刷新，应优先检查编译错误。

遗留风险：

- `ViewBase` 当前只是最小骨架，完整异步生命周期、阶段状态机和 UIManager 调用链仍需阶段 4 和阶段 5 完成。
- `ModalBase<TResult>` 当前只提供结果互斥基础，遮罩、关闭时结果兜底和 ModalStack 仍需阶段 7 完成。

下一步：

- 提交阶段 1。
- 进入阶段 2，优先实现 `UIFrameworkSettings`、`CanvasProfile`、`ViewCatalog`、`ViewDefinition`、`LayerDefinition` 与校验结果结构。

### 2026-05-05 阶段 2 配置资产与 Catalog 校验

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Core/Data/` 目录，承载框架配置与校验数据结构。
- 新增 `CanvasProfile`，支持 `ScreenSpaceOverlay` 与 `ScreenSpaceCamera` 配置，并校验 Camera 模式必须显式配置 `uiCamera`。
- 新增 `UIFrameworkSettings`，集中配置实例 id 前缀、Root 名称、DontDestroyOnLoad、UniTask/动画时间尺度策略、池化容量和标准 UI Layer。
- 新增 `ViewLayer`、`LayerDefinition`，默认提供 Background、Hud、Page、Popup、ModalMask、Modal、Tooltip、System、Debug 层级及推荐 SortingOrder。
- 新增 `ViewCatalog`、`ViewDefinition`，支持按 id 和类型查找 Definition，并提供启动期可复用的结构化校验。
- 新增 `ValidationSeverity`、`ValidationMessage`、`ValidationReport`，让 Settings、CanvasProfile、Catalog 校验可以被 UIManager 和 Editor 共同复用。
- 为 `CanvasProfile`、`UIFrameworkSettings`、`ViewCatalog` 增加 `Log Validation Report` 上下文菜单，便于 Inspector 手动检查。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewLayer.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/ValidationSeverity.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/ValidationMessage.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/ValidationReport.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/LayerDefinition.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/CanvasProfile.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/ViewDefinition.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/ViewCatalog.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Data/UIFrameworkSettings.cs`

验证情况：

- 已检查所有新增 `.cs` 均有对应 `.meta`。
- 已检查 `CreateAssetMenu` 菜单路径使用 `Orange/UI Framework/...`，类名和字段名没有滥用 `Orange`。
- 已检查阶段 2 未修改旧 `AXR.Framework.UI` 框架、旧 UIManager 调用和业务 UI 页面。
- 已进行文件级检查：Settings、CanvasProfile、Catalog 均提供结构化 `Validate()`；Catalog 覆盖重复 id、重复类型、Prefab 缺组件、Kind 与基类不匹配；Settings 覆盖重复 Layer；CanvasProfile 覆盖 Camera 模式缺相机。
- 当前工作树仍没有 Unity 生成的 `.csproj`，未能在命令行执行完整 C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- 阶段 2 只提供数据资产和校验，不创建实际资源资产；Overlay / Camera Profile 和 Catalog asset 可在 Unity Editor 中按需要创建。
- `allowDuplicateViewType` 仅为多皮肤扩展保留，阶段 3/4 的默认 UIManager 查找仍应优先使用唯一类型。
- `ViewPart` 被明确禁止注册到全局 Catalog；页面内部子视图仍需在业务 Prefab 内直接组合。

下一步：

- 提交阶段 2。
- 进入阶段 3，优先实现新 `UIManager` 与 `IUIManager`，完成 Settings/Catalog 校验、Root Canvas 创建/复用、Overlay/Camera 配置应用、标准 Layer 构建和基础 `LogRuntimeDiagnostics()`。

### 2026-05-05 阶段 3 UIManager Root / Canvas / Layer

完成内容：

- 新增新框架 `IUIManager`，声明 Page、Popup、Modal、Tooltip 的 UniTask API，作为可选解耦接口。
- 新增新框架 `UIManager`，保留为具体运行时总入口，不引入 `UIService` 平行入口。
- `UIManager.Awake()` 完成单例保护、Settings/Catalog 启动期校验、Root Canvas 创建或复用、Layer 根节点构建。
- Root Canvas 支持 `ScreenSpaceOverlay` 与 `ScreenSpaceCamera`：Camera 模式使用 `CanvasProfile.UICamera`，不会静默使用 `Camera.main`。
- Root Canvas 自动确保 `CanvasScaler` 和 `GraphicRaycaster`，并按 `CanvasProfile` 应用 reference resolution、match、sorting order、plane distance。
- 标准 Layer 创建在 `UIRoot/Layers` 下，每层独立 `Canvas` 覆盖排序并按 `LayerDefinition.BlocksRaycasts` 配置 `GraphicRaycaster`。
- 扩展 `UIRuntimeDiagnostics`，新增 Root 和 Layer 诊断字段。
- `UIManager` 新增 `TryGetLayerRoot()`、`GetRuntimeDiagnostics()`、`LogRuntimeDiagnostics()`，可输出 Root、CanvasMode、Camera、RequestVersion、Layer 名称/排序/射线/激活状态。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/IUIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`

验证情况：

- 已检查所有新增 `.cs` 均有对应 `.meta`。
- 已检查 `UIManager` 使用 `Orange.UIFramework` 命名空间，具体类名不滥用 `Orange`。
- 已检查阶段 3 未修改旧 `AXR.Framework.UI` 框架、旧 UIManager 调用和业务 UI 页面。
- 已进行文件级检查：`UIManager` 包含 Root Canvas 创建/复用、Overlay/Camera 应用、Layer 构建、启动期校验和诊断输出。
- 当前工作树仍没有 Unity 生成的 `.csproj`，未能在命令行执行完整 C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- `UIManager` 的打开关闭 API 目前只声明并显式抛出阶段性未实现异常，避免在阶段 3 提供半成品行为；阶段 4 会实现 Page 生命周期和 UniTask API。
- 当前诊断只覆盖 Root 和 Layer；PageStack、PopupStack、ModalStack、Tooltip、池化数量、异步请求状态需阶段 4、7、10 继续补齐。
- `requestVersion` 字段已进入诊断结构，但正式递增和防旧请求覆盖需阶段 5 实现。

下一步：

- 提交阶段 3。
- 进入阶段 4，优先实现 `IViewLoader` 默认 Prefab Loader、`ViewBase` 内部异步生命周期入口、PageStack 与 `OpenPageAsync` / `ReplacePageAsync` / `ResetToPageAsync` / `CloseTopPageAsync` / `CloseAllPagesAsync`。

### 2026-05-05 阶段 4 ViewBase / PageBase 生命周期与 UniTask API

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Core/Loading/`，提供 `IViewLoader` 与默认 `PrefabViewLoader`。
- `PrefabViewLoader` 当前只负责从 `ViewDefinition.Prefab` 同步实例化并用 UniTask API 返回实例，后续 Addressables 等异步资源方案可通过替换 `IViewLoader` 接入。
- 扩展 `IView`，加入 `RequiresTick`、`Phase`、`InputActive`、`BlocksRaycasts`，用于 UIManager Tick 过滤和运行时诊断。
- 扩展 `ViewBase`，实现初始化、打开、关闭、回收的内部生命周期入口，并将业务扩展点保持在 `OnOpeningAsync()`、`OnOpenedAsync()`、`OnClosingAsync()`、`OnClosed()`、`OnTick()`。
- `ModalBase<TResult>` 在每次 `Initialize()` 时重置结果源，避免池化复用后沿用上一次 Modal 结果。
- `UIManager` 接入默认 Loader、PageStack、运行时实例表、单例实例表、对象池与按需 Tick 列表。
- `UIManager` 实现 `OpenPageAsync()`、`ReplacePageAsync()`、`ResetToPageAsync()`、`CloseTopPageAsync()`、`CloseAllPagesAsync()` 与同步兼容 `OpenPage()`。
- `UIManager` 的 Page 打开流程会创建 `OpenContext` 并传入 request version；正式旧请求拦截留到阶段 5。
- `UIManager` 的关闭流程会等待 `ViewBase.CloseInternalAsync()`，然后移除运行时状态并按配置回收或释放 Prefab。
- `GetRuntimeDiagnostics()` 和 `LogRuntimeDiagnostics()` 已输出打开 View 与对象池快照，便于后续定位 Opening / Closing 状态。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Loading/IViewLoader.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Loading/PrefabViewLoader.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/IView.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ModalBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 相关生命周期、Loader、UIManager API 章节。
- 已读取 `unity-script` 与 `unity-async` 技能说明，确认本轮使用 UniTask 的生命周期和取消模型符合项目已接入依赖。
- 已确认本轮未修改旧 `AXR.Framework.UI` 框架、旧 UIManager 调用和业务 UI 页面。
- 已确认所有新增 `.cs` 均有对应 `.meta`。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 已检查本地 UniTask 包 `E:\unity\Packages\UniTask` 支持 `UniTaskCompletionSource.TrySetResult()`、`TrySetException()`。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- 阶段 4 只完成基础 Page 生命周期；连续 Replace / Reset、旧请求覆盖、新旧关闭并发、关闭中重复调用等问题必须在阶段 5 收口。
- 同步兼容 `OpenPage()` 只应作为迁移期入口，业务异步流程仍应优先使用 UniTask API。
- 当前 `OnOpeningAsync()` / `OnClosingAsync()` 尚未接 UIMotion 动画等待，阶段 6 会通过动画适配层补齐。

下一步：

- 提交阶段 4。
- 进入阶段 5，优先实现异步防重入、request version 旧请求拦截、关闭任务复用和取消清理。

### 2026-05-05 阶段 5 异步防重入与 request version

完成内容：

- `UIManager` 新增 Page 操作串行通道，所有 Page 打开、替换、重置、关闭顶层、关闭全部操作都会按顺序修改 PageStack，避免并发交叉写运行时状态。
- Page 操作进入时递增 `requestVersion`，并将版本写入 `OpenContext` 与 `RuntimeView`。
- `ReplacePageAsync()` / `ResetToPageAsync()` 会在关键步骤检查版本；如果已有更新请求进入，旧请求会以 `OperationCanceledException` 结束，不再继续覆盖最新请求。
- 过期或取消的打开请求如果已经实例化 View，会执行关闭生命周期并按池化策略回收或释放，避免残留半打开实例。
- `CloseRuntimeViewAsync()` 改为关闭开始后使用框架内部取消策略完成关闭，调用方取消不会让 View 卡在半关闭状态。
- 同一个 RuntimeView 重复关闭会等待已存在的关闭任务，不会重复执行 `OnClosed()`、重复回收或重复完成 `ClosedTask`。
- 单例 View 如果正在关闭，再次打开会等待旧实例关闭完成后再按当前请求创建新实例。
- `ViewDiagnostics` 增加 `RequestVersion` 字段，`LogRuntimeDiagnostics()` 输出每个打开 View 所属请求版本。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree。
- 已读取本文当前进度、下一轮入口和阶段 5 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的异步策略、request version、防重入、取消归属和诊断章节。
- 已确认本轮未修改旧 `AXR.Framework.UI` 框架、旧 UIManager 调用和业务 UI 页面。
- 已检查本地 UniTask 包提供 `AttachExternalCancellation()`，可用于重复关闭等待时响应调用方取消。
- 已确认本轮无新增脚本，因此无需新增 `.meta`。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- Stage 5 目前只覆盖 Page 操作；Popup 分组互斥、Modal 多次完成、Tooltip 唯一实例仍需阶段 7 处理。
- 关闭流程已按“关闭一旦开始必须完成”处理，后续 UIMotion 适配需要遵守这个约束，不能让退出动画取消后遗留半关闭状态。
- 当前没有 PlayMode 测试验证快速连续 Replace 的最终页面状态，阶段 11 需要补测试。

下一步：

- 提交阶段 5。
- 进入阶段 6，沿用旧 UIMotion 动画系统，提供 UniTask 等待适配，并修复 `UIMotionPlayer.refreshDefaultsOnEnable` 池化复用后动画起点不准的问题。

### 2026-05-05 阶段 6 UIMotion UniTask 适配与快照修复

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Motions/Runtime/`，作为新框架动画适配层目录。
- 新增 `IViewTransition`，定义 `PlayEnterAsync()`、`PlayExitAsync()`、`SetVisibleImmediate()`、`SetHiddenImmediate()`、`Kill()`，让 `ViewBase` 不直接依赖 DOTween。
- 新增 `DOTweenUniTaskExtensions.WaitForCompletionAsync()`，把 DOTween `Tween` 包装为 UniTask，保留旧 `onComplete` / `onKill` 回调，并处理完成、Kill、取消和已完成 Tween。
- 新增 `UIMotionTransition`，适配旧 `AXR.Framework.UI.IUISequenceMotion`，可自动查找 `UIMotionPlayer` 或 `UISequenceDirector`，不复制旧 UIMotion 系统。
- `ViewBase` 已接入可选 `IViewTransition`，打开会等待入场动画，关闭会等待退场动画；没有动画组件时保持原有 CompletedTask 行为。
- 关闭完成后 `ViewBase` 会把动画状态恢复到可作为下一次打开基准的可见状态，再禁用对象，避免池化对象下一次启用时先捕获 Hide 后状态。
- 修复旧 `UIMotionPlayer.OnEnable()`：只要 `refreshDefaultsOnEnable == true` 就调用 `RefreshDefaults()`，不再受 `defaultsCaptured` 阻挡。
- `IUISequenceMotion` 补充 `RefreshDefaults()`，`UISequenceDirector` 向 enter / exit 组内 Motion 传播刷新；`UIMotionTransition.PlayEnterAsync()` 会先刷新快照再采样 Hidden 起点。
- 删除旧 `SidebarMotion` 与 `SidebarMotionGroup` 的空 `ConfigureTimings()` API，避免继续暴露无效抽象。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/IViewTransition.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/DOTweenUniTaskExtensions.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/UIMotionTransition.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewBase.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/IUISequenceMotion.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/UISequenceDirector.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/V2/UIMotionPlayer.cs`
- `Assets/Scripts/UI/Regions/SidebarMotion.cs`
- `Assets/Scripts/UI/Regions/SidebarMotionGroup.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree。
- 已读取本文当前进度、下一轮入口和阶段 6 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `IViewTransition`、UIMotion、`refreshDefaultsOnEnable`、Timing API 章节。
- 已读取旧 `UIMotionPlayer`、`UISequenceDirector`、`IUIRuntimeMotion`、`IUISequenceMotion`、Motion Track 与快照注册表，确认采用最小适配而非复制整套旧动画系统。
- 已检查 `IUISequenceMotion` 实现点只有旧 `UIMotionPlayer` 和 `UISequenceDirector`，新增 `RefreshDefaults()` 不会漏实现。
- 已确认旧 `SidebarMotion` / `SidebarMotionGroup` 的 `ConfigureTimings()` 已无外部调用；仍存在的 `GamePauseMenu.PauseMenuPanelBinding.ConfigureTimings()` 是业务私有空方法，留待业务迁移阶段处理。
- 已确认所有新增脚本和目录均有 `.meta`。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- `UIMotionTransition` 当前依赖旧 `AXR.Framework.UI.IUISequenceMotion`，这是过渡期最小适配；后续如果把 UIMotion 正式迁入新命名空间，需要保留同等接口语义。
- DOTween 等待包装通过回调链完成 UniTask，需 PlayMode 验证 AutoKill / 非 AutoKill / 手动 Kill / 取消等待几类行为。
- `UISequenceDirector.useUnscaledTime` 与新 `UIFrameworkSettings.UseUnscaledTime` 的统一策略尚未落地，本轮只保持旧动画系统现状。

下一步：

- 提交阶段 6。
- 进入阶段 7，实现 Popup / Modal / Tooltip 基础管理，优先复用当前 `UIManager` 的 RuntimeView 创建、关闭、回收链路，并保持动画等待、request version 和关闭任务复用语义。

### 2026-05-05 阶段 7 Popup / Modal / Tooltip 基础管理

完成内容：

- `UIManager` 新增 `popupStack`、`modalStack`、`currentTooltip`，并为 Popup、Modal、Tooltip 分别加入串行操作通道，避免显示、隐藏和分组关闭交叉修改运行时状态。
- `ShowPopupAsync()` 已接入框架生命周期：按 Catalog 创建或复用 Popup，支持 `PopupOptions.GroupId` + `ReplaceSameGroup` 分组互斥，支持外部点击关闭，支持锚点 / 屏幕点基础定位。
- `ShowModalAsync()` 已接入 ModalStack 和结果等待，打开后释放 Modal 操作锁以允许多层 Modal，关闭时兜底完成取消结果，避免调用方取消或外部关闭导致结果任务悬挂。
- `ShowTooltipAsync()` 已接入唯一 Tooltip 管理，显示新 Tooltip 前关闭旧 Tooltip，`UpdateTooltipPosition()` 支持指针跟随更新，`HideTooltip()` 走同一串行关闭链路。
- `UIManager` 新增统一 Modal 遮罩和 Popup 外部点击透明拦截对象，由标准 Layer 创建，Modal 遮罩点击按 `ViewDefinition.CloseOnBackgroundClick` 判断是否关闭。
- `RefreshInputState()` 已统一处理 Page / Popup / Modal / Tooltip 输入状态：Modal 顶层独占输入，Popup 只让栈顶 Popup 可交互且不默认冻结 Page，Tooltip 不阻挡输入。
- `IsOpen<TView>()` 改为从全部已打开 View 运行时表判断，覆盖 Page、Popup、Modal、Tooltip。
- `ModalBase<TResult>` 新增内部 `IModalView` 接口，由 `UIManager` 在框架关闭路径兜底完成 Modal 结果。
- 修复 `PopupOptions` 构造函数中 `trackInStack` 字段未写入实例字段的问题。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ModalBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PopupOptions.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree。
- 已读取本文当前进度、下一轮入口和阶段 7 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 Popup、Modal、Tooltip、输入焦点、定位裁剪和诊断相关章节。
- 已读取 `unity-script`、`unity-async`、`unity-project-scout` 技能说明，确认本轮继续使用 UniTask、显式生命周期归属和有限范围代码读取。
- 已确认本轮不迁移业务页面，不引入 `UIService`，不搬迁旧 `Regions` / `Contracts`。
- 已检查 `UIManager` 中 `CreateStageNotImplementedException`、`NotImplementedException`、`ApplyPageInputState` 均无残留。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- 阶段 7 只做基础定位，Popup / Tooltip 的边缘裁剪、自动翻转、布局重测和定位诊断必须在阶段 8 通过 `FloatingViewPositioner` 收口。
- Popup 外部点击透明拦截器当前只覆盖顶层 Popup，并且 Popup 默认不冻结 Page；如业务需要阻塞式轻浮层，应通过 Modal 或后续明确配置实现。
- `PopupOptions.TrackInStack` 当前保留给 Back 行为，尚未接入全局 Back 顺序；后续 Back 入口需遵守 Modal -> Popup -> Page 的关闭优先级。

下一步：

- 提交阶段 7。
- 进入阶段 8，实现 `FloatingViewPositioner` 定位裁剪，优先替换 `UIManager` 当前 `ApplyPopupPosition()` / `ApplyTooltipPosition()` 内部基础坐标换算。

### 2026-05-05 阶段 8 FloatingViewPositioner 定位裁剪

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Core/Positioning/` 目录，承载 Popup / Tooltip 统一定位裁剪工具。
- 新增 `FloatingViewAnchor`，定义 BottomRight、TopRight、BottomLeft、TopLeft、Center，作为浮层相对锚点或屏幕点的展开方向。
- 新增 `FloatingViewPlacement`，记录请求坐标、最终 anchoredPosition、请求 Anchor、实际 Anchor、是否翻转、是否裁剪、最终本地矩形与边界矩形。
- 新增 `IFloatingViewPositioner` 与默认 `FloatingViewPositioner`，统一处理 Overlay / Camera 模式相机选择、`RectTransform` Anchor、屏幕坐标、偏移、边距、自动翻转和边缘裁剪。
- `FloatingViewPositioner` 使用 Root Canvas 模式决定 Camera：Overlay 使用 `null`，ScreenSpaceCamera 使用 `Canvas.worldCamera`；屏幕点 `(0, 0)` 也被视为有效输入，支持屏幕四角测试。
- `PopupOptions` 与 `TooltipOptions` 补充 `Margin` 和 `PreferredAnchor`，让调用方控制边缘留白与首选展开方向。
- `UIManager.ApplyPopupPosition()` / `ApplyTooltipPosition()` 已替换为 `floatingViewPositioner.Place()`，不再保留内联坐标换算方法。
- `RuntimeView` 记录最近一次 `FloatingViewPlacement`，`ViewDiagnostics` 与 `LogRuntimeDiagnostics()` 会输出定位结果，便于定位出屏、翻转和裁剪问题。
- Tooltip 指针跟随更新时会保留当前 Margin 与 PreferredAnchor，只更新屏幕坐标和位置，不重复绑定 payload。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Positioning.meta`
- `Assets/Scripts/OrangeUIFramework/Core/Positioning/FloatingViewAnchor.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Positioning/FloatingViewPlacement.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Positioning/IFloatingViewPositioner.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Positioning/FloatingViewPositioner.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/PopupOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/TooltipOptions.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree，并确认阶段 7 提交 `12b8ec5` 已存在。
- 已读取本文当前进度、下一轮入口和阶段 8 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `12.1 Popup / Tooltip 定位裁剪工具` 章节。
- 已确认本轮不迁移业务页面，不引入 `UIService`，不修改旧 UIManager 业务调用，不搬迁旧 `Regions` / `Contracts`。
- 已检查新增 Positioning 目录与所有新增 `.cs` 均有对应 `.meta`。
- 已搜索现有项目 `new PopupOptions()` / `new TooltipOptions()` 调用，除新框架自身默认值和 UIManager 指针跟随外无业务调用，新增可选参数未破坏现有业务代码。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- 定位算法尚未通过 Unity PlayMode 覆盖实际 CanvasScaler、Camera Canvas、LayoutGroup 内容刷新、不同 pivot 和分辨率变化；阶段 11 需要补测试。
- `FloatingViewPositioner` 当前以 Popup 打开时 `rebuildLayout: true`、Tooltip 指针跟随时 `rebuildLayout: false` 为默认策略；如果 Tooltip 内容动态变化，需要业务刷新内容后重新 Show 或后续提供显式重测入口。
- 当前诊断已记录定位结果，但尚未实现完整阶段 10 的 PageStack、PopupStack、ModalStack、异步请求状态结构化调试面板。

下一步：

- 提交阶段 8。
- 进入阶段 9，实现本地化基础能力，优先提供不依赖 Unity Localization 包的 `ILocalizationService`、语言表资产和 `LocalizedText`。

### 2026-05-05 阶段 9 本地化基础能力

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Localization/`，拆分 `Runtime` 与 `Data`，作为新框架本地化模块目录。
- 新增 `ILocalizationService`，定义当前语言、语言切换事件、`SetLanguageAsync()`、`GetText()` 和参数化文本接口。
- 新增 `LocalizationEntry` 与 `LocalizationTable`，以 ScriptableObject 保存单语言 `key -> value` 表，提供 `Orange/UI Framework/Localization Table` 创建菜单和重复 key 校验。
- 新增 `LocalizationService` 运行时组件，实现 `ILocalizationService`，维护语言表索引、当前语言、默认语言回退、语言切换通知、参数化文本 `{key}` 替换。
- 新增 `LocalizedText`，绑定 `TMP_Text`，支持显式引用 `LocalizationService` 或使用 `LocalizationService.Current`，可在语言切换、启用、设置 key、设置参数时刷新文本。
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 已补充当前实现落地规则，明确阶段 9 不迁移业务页面文本、不强制引入 Unity Localization 包、不做字体自动切换。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Localization.meta`
- `Assets/Scripts/OrangeUIFramework/Localization/Runtime.meta`
- `Assets/Scripts/OrangeUIFramework/Localization/Data.meta`
- `Assets/Scripts/OrangeUIFramework/Localization/Runtime/ILocalizationService.cs`
- `Assets/Scripts/OrangeUIFramework/Localization/Runtime/LocalizationService.cs`
- `Assets/Scripts/OrangeUIFramework/Localization/Runtime/LocalizedText.cs`
- `Assets/Scripts/OrangeUIFramework/Localization/Data/LocalizationEntry.cs`
- `Assets/Scripts/OrangeUIFramework/Localization/Data/LocalizationTable.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree，并确认阶段 8 提交 `8978674` 已存在。
- 已读取本文当前进度、下一轮入口和阶段 9 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的本地化方案与 `LocalizedText` 示例章节。
- 已读取 `unity-script`、`unity-async`、`unity-inspector`、`unity-scriptableobject` 技能说明，确认本轮使用 TMP 绑定、ScriptableObject 配置资产和最小 Inspector 字段。
- 已确认 `Packages/manifest.json` 包含 `com.unity.textmeshpro`，且本轮不引入 Unity Localization 包。
- 已搜索项目中现有 TMP 使用与 `LocalizedText` / `Localization` 命名，未发现旧本地化系统冲突；本轮未迁移业务页面。
- 已检查新增 Localization 目录和所有新增 `.cs` 均有对应 `.meta`。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- `LocalizationService` 当前使用场景组件和 `Current` 作为默认解析路径，适合框架基础与迁移期；若后续需要多实例或测试注入，需要在 Bootstrap 阶段明确服务装配策略。
- 参数替换当前只支持 `{name}` 简单占位符，不支持复数、格式化表达式或富文本模板逻辑。
- 阶段 9 未迁移业务页面硬编码文本，也未做字体按语言自动切换；这些留到业务迁移或后续真实需求。

下一步：

- 提交阶段 9。
- 进入阶段 10，实现运行时诊断增强，补齐 Stack、Modal 遮罩、输入状态、异步请求和定位裁剪结果的结构化快照。

### 2026-05-05 阶段 10 运行时诊断与调试入口

完成内容：

- 扩展 `UIRuntimeDiagnostics`，新增 PageStack、PopupStack、ModalStack、Tooltip、操作状态、Modal 遮罩、Popup 外部点击拦截器和输入焦点结构化诊断。
- `ViewDiagnostics` 已补齐定位请求坐标、最终坐标、请求锚点、最终锚点、是否翻转、是否裁剪、最终矩形与边界矩形。
- `UIManager` 新增生命周期追踪表，`GetRuntimeDiagnostics()` 不只输出已进入 Stack 的 View，也能定位卡在 `Opening`、`Closing` 或 `Failed` 的运行时实例。
- `LogRuntimeDiagnostics()` 已输出 Stack 顺序、顶层标记、请求版本、输入状态、遮罩状态、外部点击拦截器状态、Tooltip 状态、定位结果和对象池数量。
- 新增 `Assets/Scripts/OrangeUIFramework/Editor/UIManagerEditor.cs`，在 `UIManager` Inspector 提供 `Log Runtime Diagnostics` 按钮，按钮复用 `LogRuntimeDiagnostics()`，不引入第二套诊断逻辑。
- 已更新 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的错误与诊断章节，记录当前落地结构和规则。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIRuntimeDiagnostics.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Editor.meta`
- `Assets/Scripts/OrangeUIFramework/Editor/UIManagerEditor.cs`
- `Assets/Scripts/OrangeUIFramework/Editor/UIManagerEditor.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree，并确认阶段 9 提交 `631af32` 已存在。
- 已读取本文当前进度、下一轮入口和阶段 10 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `19. 错误与诊断` 章节。
- 已读取 `unity-project-scout`、`unity-script`、`unity-inspector` 技能说明，确认本轮只做诊断结构与 Inspector 调试入口。
- 已确认本轮不迁移业务页面，不引入 `UIService`，不修改旧 UIManager 业务调用，不搬迁旧 `Regions` / `Contracts`。
- 已检查新增 Editor 目录与脚本均有 `.meta`。
- 已对本轮代码文件执行关键词静态检查，未发现 `TODO`、`NotImplementedException`、`UIService`、`FindObjectsOfType`、`FindFirstObjectByType`。
- 已执行 `git diff --check`，仅有 Windows 换行风格提示，无空白错误。
- 当前工作树仍没有 Unity 生成的 `.csproj`，无法通过命令行执行完整 Unity C# 编译；需要 Unity Editor 刷新后检查编译结果。

遗留风险：

- `SemaphoreSlim.CurrentCount == 0` 只能表达当前操作通道是否被占用，不能表达等待队列长度；本轮没有为了诊断引入额外队列状态，避免增加运行时复杂度。
- Inspector 按钮尚未在 Unity Editor 中实际点击验证，需 Unity 刷新编译后手动确认日志输出。
- `OpenViews` 现在按生命周期追踪表输出 live tracked views，用于定位 Opening / Closing；如果后续测试希望区分“已进入 Stack”和“生命周期追踪实例”，可使用 `PageStack` / `PopupStack` / `ModalStack` 字段区分。

下一步：

- 提交阶段 10。
- 进入阶段 11，优先补 EditMode 测试：Catalog 校验、`OpenContext.GetPayload<T>()`、`LocalizationService.GetText()` 参数替换、`FloatingViewPositioner` 边界裁剪；再评估 PlayMode 测试对 Page / Popup / Modal / Tooltip / UIMotion 的覆盖。

### 2026-05-05 阶段 11 第一批 EditMode 测试

完成内容：

- 新增 `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/`，作为 OrangeUIFramework 的第一批 Unity Test Runner EditMode 测试目录。
- 新增测试支撑 `TestReflection` 和测试 View 类型 `TestPageView`、`OtherTestPageView`、`TestPopupView`，只用于构造框架数据校验所需的最小 Prefab 根对象。
- 新增 `ViewCatalogEditModeTests`，覆盖重复 id、Kind 与基类不匹配、禁止注册 `Part`、Prefab 根节点缺少 `ViewBase`。
- 新增 `OpenContextEditModeTests`，覆盖 `GetPayload<T>()` 和 `TryGetPayload<T>()` 的正确类型 / 错误类型返回。
- 新增 `LocalizationServiceEditModeTests`，覆盖 `{cost}` 参数替换、当前语言缺 key 时默认语言回退、缺失 key 返回 key。
- 新增 `FloatingViewPositionerEditModeTests`，覆盖首选方向出界自动翻转，以及超大浮层边界裁剪和安全边距边界。
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 已补充当前已落地测试范围和下一批测试优先级。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Tests.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/TestReflection.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/TestReflection.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/TestViews.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/TestViews.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/OpenContextEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/OpenContextEditModeTests.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/LocalizationServiceEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/LocalizationServiceEditModeTests.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/FloatingViewPositionerEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/FloatingViewPositionerEditModeTests.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree，并确认阶段 10 提交 `deb4989` 已存在。
- 已读取本文当前进度、下一轮入口和阶段 11 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划`。
- 已读取 `unity-test`、`unity-testability`、`unity-project-scout`、`unity-script` 技能说明，确认第一批测试优先覆盖不依赖真实场景 / Prefab / 动画的 EditMode 规则。
- 已确认项目没有 asmdef，`Packages/manifest.json` 包含 `com.unity.test-framework`，现有 `Assets/Scripts/Upgrades/Tests` 更像手动测试脚本而非 Test Runner 用例。
- 已检查新增 Tests 目录和所有新增 `.cs` 均有对应 `.meta`。
- 已执行测试源码 ASCII 检查、关键词静态检查和 `git diff --check`，无问题。
- 已通过 UnitySkills 调用当前打开的 Unity Editor 执行 `debug_check_compilation`，结果 `success=true`、`isCompiling=false`、`isUpdating=false`。
- 已通过 UnitySkills 按具体测试方法名运行新增 10 个 EditMode 测试方法，均为 `1/1 passed`：4 个 Catalog 校验测试、1 个 OpenContext 测试、3 个 LocalizationService 测试、2 个 FloatingViewPositioner 测试。

遗留风险：

- UnitySkills 的 `test_list` 当前返回 0，`test_run` 无过滤和按类名运行的汇总都只显示 `1/1 passed`，发现列表 / 汇总粒度不可靠；本轮通过逐个方法名运行确认新增测试实际可执行。
- 本轮尚未补 PlayMode 测试，Page / Popup / Modal / Tooltip / UIMotion / 诊断按钮仍需真实 Unity 生命周期和 Prefab 级验证。
- 本轮测试使用反射填充私有序列化字段，只限测试装配 ScriptableObject / MonoBehaviour 的私有配置，不进入运行时代码。

下一步：

- 提交阶段 11 第一批 EditMode 测试。
- 继续阶段 11 第二批测试，优先补 PlayMode / 运行时覆盖：Page 打开关闭、Popup 外部点击关闭、Modal 遮罩阻挡与结果互斥、Tooltip 屏幕边缘裁剪、UIMotion refresh defaults 池化复用、`GetRuntimeDiagnostics()` Stack / 输入 / 遮罩快照。

### 2026-05-05 阶段 11 第二批运行时 EditMode 测试

完成内容：

- 新增 `RuntimeTestViews`，提供可挂载到测试 GameObject 的 Page / Popup / Modal / Tooltip 测试 View 桩，覆盖生命周期计数、延迟打开 / 关闭、Modal 确认 / 取消触发与静态状态重置。
- 新增 `UIManagerRuntimeTestHarness`，用运行时临时 `CanvasProfile`、`UIFrameworkSettings`、`ViewCatalog` 和 Prefab GameObject 装配真实 `UIManager`，避免依赖项目业务 Prefab。
- 新增 `UIManagerRuntimeEditModeTests`，覆盖 Page 打开关闭和池化复用、连续 Replace request version、重复关闭保护、Popup 外部点击、Modal 结果互斥与下层输入阻断、遮罩点击兜底取消、Tooltip 边界定位诊断和运行时诊断快照。
- 新增 `UIMotionPlayerEditModeTests`，通过旧动画系统真实 `UIMotionPlayer`、`UIMotionDefinition`、`UIMoveMotionTrack` 验证 `refreshDefaultsOnEnable` 修复：禁用后改变位置，再启用并 `SetHiddenImmediate()` 会以新坐标作为默认起点。
- 将第一批测试使用的 `TestViews` 从 `EditMode/Editor/` 移到 `EditMode/`，并使用 `UNITY_EDITOR` 包裹；原因是 Unity 不允许把 `Editor` 程序集中的 `MonoBehaviour` 脚本通过 `AddComponent<T>()` 挂到 GameObject 上。
- `TestReflection` 补充 `using System;`，修复 `MissingFieldException` / `MissingMethodException` 引用缺失。
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 已记录第二批测试覆盖范围和测试目录规则。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/TestViews.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/TestViews.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/RuntimeTestViews.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/RuntimeTestViews.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/TestReflection.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeEditModeTests.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeTestHarness.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeTestHarness.cs.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIMotionPlayerEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIMotionPlayerEditModeTests.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree，并确认上一提交为 `d566586 补充 UI 框架第一批 EditMode 测试`。
- 已读取本文当前进度、下一轮入口和阶段 11 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划`。
- 已读取 `unity-project-scout`、`unity-async`、`unity-test`、`unity-testability` 技能说明，确认本轮用 EditMode 覆盖可在轻量 Unity 对象中验证的运行时逻辑，不迁移业务页面。
- 发现 UnitySkills 当前连接主工作区 `E:\AXR_Projects\unity\Survivors`，不是 worktree；因此改用 Unity batchmode 显式指定 `-projectPath C:\Users\AXR\.codex\worktrees\f02c\Survivors` 验证。
- worktree 缺少 `.gitignore` 忽略的 Behavior Designer / DOTween DLL 时会导致 Unity 编译失败；本轮仅从主工作区复制这些 ignored DLL 到 worktree 用于本地验证，未纳入 Git。
- 发现 `com.unity.test-framework@1.1.33` 命令行带 `-quit` 时不会实际进入 Test Runner 并生成 XML；已改用不带 `-quit` 的命令让 Test Runner 自行退出。
- 初次真实运行测试得到 `total=19 passed=10 failed=9`，失败原因是测试 View 桩在 `Editor` 目录下无法挂载；移动到 `EditMode/` 并加 `UNITY_EDITOR` 后重新验证通过。
- 已执行 Unity batchmode EditMode 全量测试：`total=19 passed=19 failed=0 skipped=0 result=Passed`，结果文件为 `Logs/OrangeUIFrameworkEditModeTests-Runtime-7.xml`，日志为 `Logs/OrangeUIFrameworkEditModeTests-Runtime-7.log`。

遗留风险：

- 本轮第二批仍属于 EditMode 运行时覆盖，尚未覆盖真实 PlayMode 场景、真实 Prefab、真实 EventSystem 指针输入、CanvasScaler 动态分辨率和 Camera Canvas。
- `UIMotionPlayer` 测试验证了默认快照刷新和 `SetHiddenImmediate()` 起点，但未验证 DOTween 实际播放时长、取消等待和暂停时 unscaled time 策略。
- Unity batchmode 会生成 ignored 的 `Library/`、`Logs/`、`Temp/`、`UserSettings/` 以及本地复制的 ignored DLL；后续提交不得包含这些内容。
- Unity 批处理触发了 `ProjectSettings/ProjectSettings.asset` 与 `ProjectSettings/ShaderGraphSettings.asset` 的行尾/导入痕迹，本轮不应纳入提交。

下一步：

- 提交阶段 11 第二批运行时 EditMode 测试。
- 继续阶段 11 收尾，优先决定是否补最小 PlayMode 测试场景；如不补，应至少形成手动验证清单并明确阶段 12 业务迁移前必须完成的真实场景验证项。

### 2026-05-05 阶段 11 业务迁移前真实场景手动验证清单

完成内容：

- 在 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划` 中补充“手动验证（业务迁移前强制门禁）”，明确清单是阶段 12 业务页面迁移前必须完成的门槛。
- 清单明确要求验证结果必须回写本文详细进度日志，记录验证日期、场景、Canvas 模式、分辨率、通过 / 失败项、失败日志或截图位置、处理结论。
- 清单明确验证必须在当前 worktree `C:\Users\AXR\.codex\worktrees\f02c\Survivors` 执行，不能误用主工作区 `E:\AXR_Projects\unity\Survivors`。
- 清单覆盖 Overlay / Camera Canvas、真实 Prefab、Catalog、Page 生命周期、异步防重入、UIMotion / DOTween、Popup、Modal、Tooltip、ViewPart、本地化、分辨率、真实 EventSystem 输入、诊断入口、退出与回收。
- 更新本文 `当前进度快照`、`当前风险` 和 `下一轮入口`，把下一步改为先执行真实场景手动验证或补最小 PlayMode 场景降低验证成本，清单未通过前不进入业务迁移。

修改文件：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree。
- 已读取本文当前进度、下一轮入口和阶段 11 日志，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `23. 测试计划`。
- 已读取 `unity-testability` 和 `unity-validation` 技能说明，确认真实场景手动验证应覆盖无法由 EditMode 轻量对象充分证明的 Unity-facing 链路。
- 本轮只修改文档，不改框架代码、不新增 Unity 资产、不迁移业务页面。
- 尚未执行清单中的真实场景手动验证；该清单是后续阶段 12 前必须执行的验证任务。

遗留风险：

- 清单已经形成但尚未执行；Overlay / Camera 真实 Canvas、真实 Prefab、真实 EventSystem 输入、CanvasScaler、DOTween 实际播放和 Inspector 诊断按钮仍未被真实场景验收。
- 若下一轮选择先补 PlayMode 测试，PlayMode 测试只能降低手动验证成本，不能替代真实场景手动验证。

下一步：

- 提交本轮文档变更。
- 下一轮必须优先执行业务迁移前真实场景手动验证清单，或先补最小 PlayMode 场景辅助验证；清单通过并记录前，禁止迁移 `MenuUIPage`、`GamingUIPage`、`ShopUIPage` 等业务页面。

### 2026-05-05 阶段 12 MenuUIPage 迁移

完成内容：

- 按用户明确指示开始业务页面迁移，并记录“真实场景手动验证清单尚未执行但已被用户要求跳过”的风险。
- 为旧 `AXR.Framework.UI.UIPageBase` 增加迁移期桥接：旧页面基类现在继承 `Orange.UIFramework.PageBase`，保留 `IUIPage`、`OnPageOpened()`、`OnPageClosed()`、`OnActivationChanged()`、`OnPageTick()` 等旧业务扩展点。
- `UIPageBase` 新增 Orange 生命周期桥接：`OnOpeningAsync()` 转换为旧 `UIPageOpenContext` 后调用 `HandleOpen()` 和 `PlayOpenTransition()`；`OnClosingAsync()` 等待旧关闭等待管线；`OnClosed()` 调用旧 `HandleClose()`。
- `UIPageBase` 补充 `CanvasGroup`、`RectTransform`、`UISequenceDirector` 懒解析，修复通过 Orange Manager 测试路径打开旧页面时 `Awake` 之外生命周期拿不到旧私有引用的问题。
- `UIPageBase` 移除关闭等待管线中的 0 秒 DOTween 占位；没有 SequenceDirector 或额外关闭等待动作时会同步完成，避免 EditMode / batchmode 中关闭桥接卡住。
- 新 `Orange.UIFramework.UIManager` 增加非泛型迁移入口：`OpenPageAsync(Type)`、`OpenPage(Type)`、`ClosePageAsync(Type)`、`IsOpen(Type)`，并让非泛型 `ViewHandle` 保存 `ViewBase View`。
- 旧 `AXR.Framework.UI.UIManager` 对已注册到 Orange `ViewCatalog` 的页面委托新 UIManager 打开、关闭和查询；未注册页面继续走旧 UIManager。
- 新增 `Assets/Resources/Data/UI/OrangeCanvasProfile.asset`、`OrangeUIFrameworkSettings.asset`、`OrangeUIViewCatalog.asset`，当前 Catalog 仅注册 `UI Menu.prefab` / `MenuUIPage`。
- 在 `Assets/Scenes/Game Scene.unity` 的现有 `UIManager` GameObject 上挂载新 `Orange.UIFramework.UIManager`，复用现有 Root Canvas。
- 新增 EditMode 测试 `LegacyUIPageBase_CanOpenAndCloseThroughOrangeManagerTypeApi`，验证旧页面基类可通过 Orange Manager 类型 API 打开、传 payload、关闭并更新打开状态。

修改文件：

- `Assets/Scripts/Framework/UI/Core/Runtime/UIPageBase.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewHandle.cs`
- `Assets/Scenes/Game Scene.unity`
- `Assets/Resources/Data/UI/OrangeCanvasProfile.asset`
- `Assets/Resources/Data/UI/OrangeCanvasProfile.asset.meta`
- `Assets/Resources/Data/UI/OrangeUIFrameworkSettings.asset`
- `Assets/Resources/Data/UI/OrangeUIFrameworkSettings.asset.meta`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset.meta`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/RuntimeTestViews.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeTestHarness.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 `unity-project-scout`、`unity-script`、`unity-async`、`unity-test` 技能说明。
- 已确认 UnitySkills 当前仍连接主工作区，因此本轮使用 Unity batchmode 显式指定 `-projectPath C:\Users\AXR\.codex\worktrees\f02c\Survivors` 验证 worktree。
- 首次新增桥接测试时曾失败：`LegacyRuntimeTestPageViewPrefab(Clone)` 缺旧 `UIPageBase` 私有 `CanvasGroup` 引用。已通过旧基类懒解析修复。
- 随后一次测试卡在 Test Runner 开始后未生成 XML；定位为旧关闭等待管线的 0 秒 DOTween 占位在 EditMode / batchmode 下可能不推进。已移除该占位，并只停止指向当前 worktree 的 Unity batchmode 进程。
- 已执行 Unity batchmode EditMode 测试：`Logs/OrangeUIFrameworkEditModeTests-MenuMigration-4.xml`，结果 `total=20 passed=20 failed=0 skipped=0 result=Passed`。
- 测试 XML 生成后 Unity batchmode 进程未自动退出；已只停止当前 worktree 的测试进程，未处理主工作区 Unity Editor 和 ImportWorker。

遗留风险：

- 本轮没有直接改 `Assets/Scripts/UI/Instances/MenuUIPage.cs`，而是通过旧 `UIPageBase` 继承新 `PageBase` 的桥接方式迁移；这是为了降低第一模块迁移风险，后续可在页面业务依赖清理后再做彻底继承切换。
- `MenuUIPage` 尚未在真实 Play Mode 中手动验证按钮输入、DOTween 实际播放、CanvasScaler、EventSystem 和 Inspector 诊断按钮。
- 当前 `OrangeUIViewCatalog` 只注册 `MenuUIPage`，后续迁移页面必须逐个注册，不能批量塞入 Catalog。
- Unity 批处理仍可能留下 `ProjectSettings/ProjectSettings.asset` 的行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `MenuUIPage` 迁移。
- 继续阶段 12 第二个模块 `GamingUIPage`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 GamingUIPage 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第二个页面 `GamingUIPage`。
- 读取并确认 `GamingUIPage` 仍继承旧 `UIPageBase`，依赖 `GamingPageContext`、`GamingHudView`、`GamingInputView`、`InventoryUiBinder`、`UITooltipPresenter` 和移动摇杆等战斗 HUD 业务组件；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
- 确认旧 `GameManager` 打开 / 关闭入口仍是 `uiManager.OpenPage<GamingUIPage>(UIPageContextFactory.CreateGamingPageContext(player))` 与 `transition.ClosePage<GamingUIPage>()`，因此页面注册进 Orange Catalog 后会自动由旧 UIManager 委托新 UIManager。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Gaming.prefab` 位于 `Hud` 层；新 `OrangeUIViewCatalog.asset` 新增 `page.gaming`，`ViewKind.Page`，`ViewLayer.Hud`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Gaming.prefab`。
- 新增真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedMenuAndGamingPages`，验证 `OrangeUIViewCatalog.asset` 可以通过校验，并可按类型解析 `MenuUIPage` 与 `GamingUIPage`。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `MenuUIPage` 迁移提交 `ef881ce` 已存在。
- 已读取 `GamingUIPage.cs`、`GamingPageContext.cs`、`GameManager.cs` 中的打开 / 关闭调用、旧 `UIPrefabCatalog.asset` 和 `UI Gaming.prefab` 元数据。
- 已确认 `UI Gaming.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`GamingUIPage` 和 `UISequenceDirector`，Prefab GUID 为 `75c86628c871ecb47b8145cd95a3dd60`。
- 已使用 Unity batchmode 显式指定 worktree `-projectPath C:\Users\AXR\.codex\worktrees\f02c\Survivors` 运行 EditMode 测试：`Logs/OrangeUIFrameworkEditModeTests-GamingMigration-1.xml`，结果 `total=21 passed=21 failed=0 skipped=0 result=Passed`。

遗留风险：

- `GamingUIPage` 尚未在真实 Play Mode 中验证移动摇杆输入、HUD 刷新、BuffBar、Tooltip、暂停按钮和 Inventory UI 事件解绑。
- `GamingUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；这是为了保持战斗 HUD 业务依赖稳定。
- Unity 批处理仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GamingUIPage` 迁移。
- 继续阶段 12 第三个模块 `ShopUIPage`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 ShopUIPage 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第三个页面 `ShopUIPage`。
- 读取并确认 `ShopUIPage` 仍继承旧 `UIPageBase`，依赖 `ShopPageContext`、`ShopPageController`、`ShopListView`、`ShopSidebarHost`、`InventoryUiBinder` 和背包 / 属性侧栏等商店业务组件；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
- 确认旧 `GameManager` 打开 / 关闭入口是 `uiManager.OpenPage<ShopUIPage>(UIPageContextFactory.CreateShopPageContext(player))` 与 `transition.ClosePage<ShopUIPage>()`，因此页面注册进 Orange Catalog 后会自动由旧 UIManager 委托新 UIManager。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Shop.prefab` 位于默认页面层；新 `OrangeUIViewCatalog.asset` 新增 `page.shop`，`ViewKind.Page`，`ViewLayer.Page`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Shop.prefab`。
- 将真实 Catalog 资产测试重命名为 `OrangeCatalog_RegistersMigratedBusinessPages`，并增加 `ShopUIPage` 类型解析、Id 和 Layer 断言。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `GamingUIPage` 迁移提交 `8b6169e` 已存在。
- 已读取 `ShopUIPage.cs`、`GameManager.cs` 中的打开 / 关闭调用、旧 `UIPrefabCatalog.asset` 记录和 `UI Shop.prefab` 元数据。
- 已确认 `UI Shop.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`ShopUIPage` 和 `UISequenceDirector`，Prefab GUID 为 `20c94ac16a11dc54b9e607c6eff07912`。
- 已执行 `git diff --check -- Assets/Resources/Data/UI/OrangeUIViewCatalog.asset Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs ORANGE_UI_FRAMEWORK_DEVELOPMENT.md ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 已两次尝试使用 Unity batchmode 运行单个 Catalog EditMode 测试；Unity 只完成脚本编译、资源导入和 Domain Reload，进程 ExitCode 为 0，但没有生成 Test Runner XML，因此本轮不把该测试记为已通过。
- 本轮按用户要求不做耗时完整回归；提交前采用脚本编译 / 导入成功与 `git diff --check` 作为最小验证，完整 Play Mode 验收留到全部业务页面迁移和旧资源清理完成后。

遗留风险：

- `ShopUIPage` 尚未在真实 Play Mode 中验证商店刷新、购买、锁定、继续按钮、左右侧栏切换、背包绑定、属性展示和事件解绑。
- `ShopUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；这是为了保持商店业务依赖稳定，最终收口时必须清理该脚手架。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `ShopUIPage` 迁移。
- 继续阶段 12 第四个模块 `GamePauseMenu`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 GamePauseMenu 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第四个页面 `GamePauseMenu`。
- 读取并确认 `GamePauseMenu` 仍继承旧 `UIPageBase`，依赖 `PauseMenuContext`、`InventoryUiBinder`、`GameEventBus`、`PauseMenuPanelBinding` 和旧 `IUIRuntimeMotion` 侧栏动画；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
- 确认旧 `GameManager` 打开 / 关闭 / 查询入口是 `uiManager.OpenPage<GamePauseMenu>(UIPageContextFactory.CreatePauseMenuContext(player))`、`transition.ClosePage<GamePauseMenu>()` 与 `uiManager.IsPageOpen<GamePauseMenu>()`，因此页面注册进 Orange Catalog 后会自动由旧 UIManager 委托新 UIManager。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Pause.prefab` 位于 Popup 层；新 `OrangeUIViewCatalog.asset` 新增 `page.pause`，`ViewKind.Page`，`ViewLayer.Popup`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Pause.prefab`。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，增加 `GamePauseMenu` 类型解析、Id 和 Layer 断言。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `ShopUIPage` 迁移提交 `49c8402` 已存在。
- 已读取 `GamePauseMenu.cs`、`PauseMenuContext.cs`、`GameManager.cs` 中的打开 / 关闭 / 查询调用、旧 `UIPrefabCatalog.asset` 记录和 `UI Pause.prefab` 元数据。
- 已确认 `UI Pause.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`GamePauseMenu` 和 `UISequenceDirector`，Prefab GUID 为 `a3b9b07b8f661b249b31e24e92419cff`。
- 已确认本轮只做 Catalog 接入和资产断言，不执行耗时完整回归；提交前采用脚本静态检查与 `git diff --check` 作为最小验证。

遗留风险：

- `GamePauseMenu` 尚未在真实 Play Mode 中验证暂停恢复、返回主菜单、背包绑定、设置侧栏显示 / 隐藏、时间缩放为 0 时的 UI 动画和事件解绑。
- `GamePauseMenu` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；这是为了保持暂停菜单业务依赖稳定，最终收口时必须清理该脚手架。
- `PauseMenuPanelBinding.ConfigureTimings()` 仍是旧业务私有空方法；当前旧 `IUIRuntimeMotion` 没有公开运行时覆盖 Clip 时长 / Ease 的接口，本轮不通过反射或修改共享动画资产强行接入，最终收口时需要删除该空 API 或给 UIMotion 提供明确的 Timing 覆盖能力。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GamePauseMenu` 迁移。
- 继续阶段 12 第五个模块 `GameOverUIPage`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 GameOverUIPage 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第五个页面 `GameOverUIPage`。
- 读取并确认 `GameOverUIPage` 仍继承旧 `UIPageBase`，只负责重启和返回主菜单按钮事件绑定；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
- 确认旧 `GameManager` 打开 / 关闭入口是 `uiManager.OpenPage<GameOverUIPage>()` 与 `transition.ClosePage<GameOverUIPage>()`，因此页面注册进 Orange Catalog 后会自动由旧 UIManager 委托新 UIManager。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Game Over.prefab` 位于默认页面层；新 `OrangeUIViewCatalog.asset` 新增 `page.gameOver`，`ViewKind.Page`，`ViewLayer.Page`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Game Over.prefab`。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，增加 `GameOverUIPage` 类型解析、Id 和 Layer 断言。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `GamePauseMenu` 迁移提交 `91ceb0f` 已存在。
- 已读取 `GameOverUIPage.cs`、`GameManager.cs` 中的打开 / 关闭调用、旧 `UIPrefabCatalog.asset` 记录和 `UI Game Over.prefab` 元数据。
- 已确认 `UI Game Over.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`GameOverUIPage` 和 `UISequenceDirector`，Prefab GUID 为 `e6ab8e1c786b9d4409cedc1cc5d99d58`。
- 已确认本轮只做 Catalog 接入和资产断言，不执行耗时完整回归；提交前采用脚本静态检查与 `git diff --check` 作为最小验证。

遗留风险：

- `GameOverUIPage` 尚未在真实 Play Mode 中验证重启、返回主菜单按钮、GameOver 状态切换、事件解绑和退场动画。
- `GameOverUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；这是为了保持结算页业务依赖稳定，最终收口时必须清理该脚手架。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GameOverUIPage` 迁移。
- 继续阶段 12 第六个模块 `StageCompleteUIPage`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 StageCompleteUIPage 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第六个页面 `StageCompleteUIPage`。
- 读取并确认 `StageCompleteUIPage` 继承旧 `UIPageBase`，负责重启 / 返回主菜单按钮事件，以及从 `StageCompleteSummaryManager` 创建通关结算快照并渲染波数、生存时间、击杀、金币、角色和主武器信息。
- 确认旧 `GameManager` 已有打开 / 关闭入口：`uiManager.OpenPage<StageCompleteUIPage>()` 与 `transition.ClosePage<StageCompleteUIPage>()`，但原项目没有 `StageCompleteUIPage` 对应 Prefab，也没有旧 `UIPrefabCatalog.asset` 注册。
- 补齐真实业务资源 `Assets/Resources/Prefabs/New UI/Pages/UI Stage Complete.prefab`，使用现有结算页按钮与动画结构作为基础，根节点脚本替换为 `StageCompleteUIPage`，按钮引用绑定到重启 / 主菜单按钮。
- 为 `StageCompleteUIPage` 增加可选 `summaryText` 字段，在没有六个细分 TMP 字段的最小 Prefab 中仍能显示完整通关快照；原有细分字段保留，后续更精细 Prefab 可直接逐项绑定。
- 新 `OrangeUIViewCatalog.asset` 新增 `page.stageComplete`，`ViewKind.Page`，`ViewLayer.Page`，Prefab 指向 `UI Stage Complete.prefab`。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，增加 `StageCompleteUIPage` 类型解析、Id 和 Layer 断言。

修改文件：

- `Assets/Scripts/UI/Instances/StageCompleteUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Stage Complete.prefab`
- `Assets/Resources/Prefabs/New UI/Pages/UI Stage Complete.prefab.meta`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `GameOverUIPage` 迁移提交 `d427761` 已存在。
- 已读取 `StageCompleteUIPage.cs`、`StageCompleteUIPage.cs.meta`、`GameManager.cs` 中的打开 / 关闭调用、旧 `UIPrefabCatalog.asset` 和现有 UI Prefab 列表。
- 已确认 `StageCompleteUIPage` 脚本 GUID `b8a0dd6e4c5b4d4bb36a2e9f3c6f0c1a` 原先没有出现在任何 Prefab / Scene / Catalog 中，因此本轮补 Prefab 是修复真实资源缺口，而不是额外冗余功能。
- 曾尝试使用 Unity batchmode 生成 Prefab，但 Unity 多次只完成导入 / 编译或停在初始化扩展，没有进入生成方法；按用户要求不继续在测试 / 工具启动上耗时，改用现有稳定 Prefab 结构派生最小可运行资源。
- 已确认新 Prefab 根节点引用 `StageCompleteUIPage`，Root fileID 与 Catalog 注册一致，按钮引用和 `summaryText` 已绑定；提交前采用静态资源检查与 `git diff --check` 作为最小验证。

遗留风险：

- `UI Stage Complete.prefab` 当前复用 `UI Game Over.prefab` 的按钮和动画结构，并用一个汇总 TMP 文本显示完整快照；视觉排版不是最终精细版，全部迁移完成后的 Play Mode 验收需要重点检查通关结算布局。
- `StageCompleteUIPage.ResolveSummaryManager()` 仍有 `FindFirstObjectByType<StageCompleteSummaryManager>()` 兜底；这是既有业务依赖，最终收口时应改为明确上下文 / 场景引用 / 服务注入，避免隐藏装配错误。
- `StageCompleteUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；最终收口时必须清理该脚手架。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `StageCompleteUIPage` 迁移。
- 继续阶段 12 第七个模块 `WaveTransitionUIPage`，先读取脚本、Prefab、旧 Catalog 注册和旧 UIManager 调用入口，再按同样规则更新文档、验证并提交。

### 2026-05-05 阶段 12 WaveTransitionUIPage 迁移

完成内容：

- 按阶段 12 单模块迁移策略迁移第七个页面 `WaveTransitionUIPage`。
- 读取并确认 `WaveTransitionUIPage` 仍继承旧 `UIPageBase`，负责订阅波次过渡状态、升级选项变化和饰品选择事件，并驱动 `WaveTransitionUpgradeCardGroup` 与 `AccessoryOperateContainer` 的显示 / 清理。
- 确认旧 `GameManager` 打开 / 关闭入口是 `uiManager.OpenPage<WaveTransitionUIPage>()` 与 `transition.ClosePage<WaveTransitionUIPage>()`，因此页面注册进 Orange Catalog 后会自动由旧 UIManager 委托新 UIManager。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Wave Transition.prefab` 位于默认页面层；新 `OrangeUIViewCatalog.asset` 新增 `page.waveTransition`，`ViewKind.Page`，`ViewLayer.Page`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Wave Transition.prefab`。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，增加 `WaveTransitionUIPage` 类型解析、Id 和 Layer 断言。
- 更新开发文档和实施计划，记录阶段 12 既定业务页面已全部完成迁移期接入，下一阶段必须进入最终收口，不能继续扩大桥接范围。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `StageCompleteUIPage` 迁移提交 `cc86422` 已存在。
- 已读取 `WaveTransitionUIPage.cs`、`WaveTransitionUIPage.cs.meta`、`GameManager.cs` 中的打开 / 关闭调用、旧 `UIPrefabCatalog.asset` 记录和 `UI Wave Transition.prefab` 元数据。
- 已确认 `UI Wave Transition.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`WaveTransitionUIPage` 和 `UISequenceDirector`，Prefab GUID 为 `7a7ebd4679f38d64d85b9322f65b5589`，根 fileID 为 `5453402992543493149`。
- 已确认本轮只做 Catalog 接入和资产断言，不执行耗时完整回归；提交前采用静态资源检查与 `git diff --check` 作为最小验证。

遗留风险：

- `WaveTransitionUIPage` 尚未在真实 Play Mode 中验证波次过渡状态快照、升级卡刷新、升级选择、饰品选择、宝箱容器显示 / 隐藏和事件解绑。
- `WaveTransitionManager.RefreshUpgradeCards()` 仍通过 `FindFirstObjectByType<WaveTransitionUpgradeCardGroup>()` 兜底查找 UI 组件；这是既有业务依赖，最终收口时应改成明确上下文 / 事件 / 页面引用，不应让业务管理器隐藏依赖 UI 场景对象。
- `WaveTransitionUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；最终收口阶段必须清理该脚手架。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `WaveTransitionUIPage` 迁移。
- 进入最终收口第一步：业务入口直接依赖 `Orange.UIFramework.UIManager`，再逐步让业务页面直接基于新框架类型，之后删除旧 `AXR.Framework.UI.UIManager`、旧 Catalog、迁移期委托和无用抽象。

### 2026-05-05 阶段 12 CharacterSelectUIPage 补迁

完成内容：

- 补迁主流程漏掉的 `CharacterSelectUIPage`。该页面是 `GameManager` 从 `Menu` 进入 `CharacterSelection` 再进入 `Game` 的必经页面，最终移除旧 UI 桥接前必须由 `OrangeUIViewCatalog` 注册。
- 读取并确认 `CharacterSelectUIPage` 仍继承旧 `UIPageBase`，负责角色列表、角色详情、确认 / 返回按钮事件和 `CharacterSelectionManager` 服务订阅；本轮只做 Catalog 接入，不改业务脚本生命周期。
- 确认旧 `UIPrefabCatalog.asset` 中 `UI Character Selection.prefab` 位于默认页面层；新 `OrangeUIViewCatalog.asset` 新增 `page.characterSelect`，`ViewKind.Page`，`ViewLayer.Page`，Prefab 指向 `Assets/Resources/Prefabs/New UI/Pages/UI Character Selection.prefab`。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，增加 `CharacterSelectUIPage` 类型解析、Id 和 Layer 断言。
- 更新开发文档和实施计划，把 `CharacterSelectUIPage` 标记为主流程漏项补迁，避免下一轮最终收口时遗漏菜单到战斗的入口链路。

修改文件：

- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `WaveTransitionUIPage` 迁移提交 `f96e1e2` 已存在。
- 已读取 `CharacterSelectUIPage.cs`、旧 `UIPrefabCatalog.asset` 记录和 `UI Character Selection.prefab` 元数据。
- 已确认 `UI Character Selection.prefab` 根节点具备 `RectTransform`、`CanvasGroup`、`CharacterSelectUIPage` 和 `UISequenceDirector`，Prefab GUID 为 `02f7f2fe1141a9b4cb409ce8b556f135`，根 fileID 为 `3816208830851736021`。
- 本轮按用户要求不执行耗时完整回归；提交前采用静态资源检查与 `git diff --check` 作为最小验证。

遗留风险：

- `CharacterSelectUIPage` 尚未在真实 Play Mode 中验证角色列表渲染、角色详情刷新、确认进入游戏、返回菜单、按钮事件解绑和退场动画。
- `CharacterSelectUIPage` 仍通过迁移期旧 `UIPageBase` 桥接，而不是直接继承新 `Orange.UIFramework.PageBase`；最终收口阶段必须清理该脚手架。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `CharacterSelectUIPage` 补迁。
- 进入最终收口第一步：业务入口直接依赖 `Orange.UIFramework.UIManager`，再逐步让业务页面直接基于新框架类型，之后删除旧 `AXR.Framework.UI.UIManager`、旧 Catalog、迁移期委托和无用抽象。

### 2026-05-05 阶段 12 最终收口：GameManager 直连 Orange UIManager

完成内容：

- 将主业务入口 `GameManager` 从旧 `AXR.Framework.UI.UIManager` 切换为新 `Orange.UIFramework.UIManager`。
- 移除 `GameManager` 对旧 `BeginTransition()`、`IUITransitionSequence`、同步 `OpenPage<T>()`、旧 `IsPageOpen<T>()` 的依赖。
- 页面状态切换改为 UniTask 顺序流程：先等待当前状态页面 `ClosePageAsync(Type)` 完成，再执行 `ApplyStateTransition()`，最后等待目标页面 `OpenPageAsync<T>()` 完成。
- 暂停菜单继续 / 返回菜单流程改为先等待 `GamePauseMenu` 关闭，再执行 `SetPaused(false)` 或 `ReturnToMenu()`，保持旧过渡序列的顺序语义。
- 新增 `stateTransitionVersion` 本地版本号，用于屏蔽连点、事件重入或对象禁用后旧异步状态切换继续打开页面。

修改文件：

- `Assets/Scripts/Managers/GameManager.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`、Git 状态和 `GameManager` / 新旧 UIManager 相关代码。
- 已确认 `GameManager.cs` 中不再残留 `using AXR.Framework.UI`、`BeginTransition()`、`IUITransitionSequence`、旧同步 `OpenPage<T>()`、旧 `ClosePage<T>()` 或旧 `IsPageOpen<T>()` 调用。
- 已执行 `git diff --check -- Assets/Scripts/Managers/GameManager.cs`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 按用户要求不做耗时完整回归；本轮采用静态编译风险检查和调用面检查作为最小验证。

遗留风险：

- 业务页面仍继承迁移期旧 `UIPageBase`，旧页面生命周期钩子仍需迁到新 `OnOpeningAsync()` / `OnClosingAsync()` / `OnClosed()`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、迁移期非泛型 Type API 和旧 Region / Contract 抽象尚未删除。
- `GameManager` 的 UniTask 页面切换尚未在真实 Play Mode 中验证菜单、角色选择、战斗、暂停、商店、波次过渡、结算和场景重载流程。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GameManager` 直连 Orange UIManager。
- 进入最终收口第二步：业务页面直接继承新框架 `PageBase`，先迁移低风险页面并提交，再逐步清理旧 `UIPageBase` 与旧 UIManager。

### 2026-05-05 阶段 12 最终收口：GameOverUIPage 直接继承 PageBase

完成内容：

- 迁移低风险页面 `GameOverUIPage`，作为业务页面直接继承新框架基类的第一批收口模块。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 保留重启和返回主菜单按钮事件绑定 / 解绑语义，不引入额外业务抽象。
- `UI Game Over.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。

修改文件：

- `Assets/Scripts/UI/Instances/GameOverUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Game Over.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态。
- 已读取新 `ViewBase` / `PageBase` / `UIMotionTransition`，确认直接迁移后动画入口依赖 `IViewTransition`，因此 Prefab 需要显式挂载 `UIMotionTransition`。
- 已确认 `GameOverUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`。
- 已确认 `UI Game Over.prefab` 根节点仍保留 `CanvasGroup`、`GameOverUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已执行 `git diff --check`，修复 Prefab 新增 YAML 空值行的尾随空格后无空白错误。

遗留风险：

- `GameOverUIPage` 尚未在真实 Play Mode 中验证重启、返回主菜单、入场动画、退场动画和按钮事件解绑。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GameOverUIPage` 直接基类迁移。
- 继续迁移下一个低风险页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `StageCompleteUIPage` 或 `MenuUIPage`，并同步处理 Prefab 的 `UIMotionTransition`。

### 2026-05-05 阶段 12 最终收口：StageCompleteUIPage 直接继承 PageBase

完成内容：

- 迁移低风险结算页面 `StageCompleteUIPage`，作为第二个直接继承新框架基类的业务页面。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 保留重启、返回主菜单按钮事件绑定 / 解绑，以及通关结算快照渲染逻辑，不引入新的业务抽象。
- `UI Stage Complete.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。

修改文件：

- `Assets/Scripts/UI/Instances/StageCompleteUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Stage Complete.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态。
- 已读取 `StageCompleteUIPage` 脚本和 `UI Stage Complete.prefab` 根节点组件，确认页面结构与 `GameOverUIPage` 类似，可按同一低风险路径迁移。
- 已确认 `StageCompleteUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`。
- 已确认 `UI Stage Complete.prefab` 根节点仍保留 `CanvasGroup`、`StageCompleteUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `StageCompleteUIPage` 尚未在真实 Play Mode 中验证结算快照渲染、重启、返回主菜单、入场动画、退场动画和按钮事件解绑。
- `StageCompleteUIPage.ResolveSummaryManager()` 仍有 `FindFirstObjectByType<StageCompleteSummaryManager>()` 兜底；这是既有业务依赖，后续业务依赖清理时应改为明确上下文 / 场景引用 / 服务注入。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `StageCompleteUIPage` 直接基类迁移。
- 继续迁移下一个业务页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `MenuUIPage` 或 `CharacterSelectUIPage`，并同步处理 Prefab 的 `UIMotionTransition`。

### 2026-05-05 阶段 12 最终收口：MenuUIPage 直接继承 PageBase

完成内容：

- 迁移主菜单页面 `MenuUIPage`，将前期迁移期桥接收口为业务页面直接继承新框架基类。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 保留开始按钮、设置按钮事件绑定 / 解绑，以及设置侧栏显示隐藏逻辑，不引入新的业务抽象。
- `UI Menu.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。
- 设置侧栏仍保留旧 `IUIRuntimeMotion` / `UIMotionClipIds` 动画接口依赖；这是当前老动画系统沿用的一部分，不再承担页面托管桥接职责。

修改文件：

- `Assets/Scripts/UI/Instances/MenuUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Menu.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态。
- 已读取 `MenuUIPage` 脚本和 `UI Menu.prefab` 差异，确认迁移只覆盖页面基类、生命周期和动画适配组件。
- 已确认 `MenuUIPage.cs` 不再残留 `UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`；`AXR.Framework.UI` 命名空间仅用于设置侧栏旧动画接口。
- 已确认 `UI Menu.prefab` 根节点仍保留 `CanvasGroup`、`MenuUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `MenuUIPage` 尚未在真实 Play Mode 中验证开始按钮、设置按钮、设置侧栏动画、入场动画、退场动画和按钮事件解绑。
- 设置侧栏仍依赖旧 `IUIRuntimeMotion` 命名空间；后续清理旧动画命名空间或迁移侧栏子视图时，需要单独收口，避免把旧 UI 托管和可沿用动画系统混为一谈。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `MenuUIPage` 直接基类迁移。
- 继续迁移下一个业务页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `CharacterSelectUIPage` 或 `WaveTransitionUIPage`，并同步处理 Prefab 的 `UIMotionTransition`。

### 2026-05-05 阶段 12 最终收口：CharacterSelectUIPage 直接继承 PageBase

完成内容：

- 迁移角色选择页面 `CharacterSelectUIPage`，覆盖菜单进入游戏前的主流程必经页面。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 保留角色选择服务解析 / 订阅、角色列表渲染、角色详情刷新、确认 / 返回按钮事件绑定与清理逻辑，不引入新的业务抽象。
- `UI Character Selection.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。

修改文件：

- `Assets/Scripts/UI/Instances/CharacterSelect/CharacterSelectUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Character Selection.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `MenuUIPage` 直接基类迁移提交 `1086ce0` 已存在。
- 已读取 `CharacterSelectUIPage`、`CharacterListController`、`CharacterInfoCard` 和 `UI Character Selection.prefab` 根节点组件，确认本轮只迁移页面生命周期和动画适配，不改角色选择业务数据流。
- 已确认 `CharacterSelectUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`。
- 已确认 `UI Character Selection.prefab` 根节点仍保留 `CanvasGroup`、`CharacterSelectUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `CharacterSelectUIPage` 尚未在真实 Play Mode 中验证角色列表渲染、角色详情刷新、确认进入游戏、返回菜单、入场动画、退场动画和事件解绑。
- `ResolveSelectionService()` 仍使用 `CharacterSelectionManager.Instance` 作为兜底，这是既有业务服务入口；后续业务依赖清理时可改为明确场景引用或打开 payload，但本轮不扩大改动范围。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `CharacterSelectUIPage` 直接基类迁移。
- 继续迁移下一个业务页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `WaveTransitionUIPage` 或 `GamingUIPage`，并同步处理 Prefab 的 `UIMotionTransition`。

### 2026-05-05 阶段 12 最终收口：WaveTransitionUIPage 直接继承 PageBase

完成内容：

- 迁移波次过渡页面 `WaveTransitionUIPage`，覆盖波次结束后的升级选择和宝箱饰品选择界面。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 保留升级选项、饰品选择和波次过渡阶段事件订阅 / 解绑，以及升级卡组和宝箱容器显示清理逻辑，不引入新的业务抽象。
- `UI Wave Transition.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍走统一动画适配入口。

修改文件：

- `Assets/Scripts/UI/Instances/WaveTransitionUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Wave Transition.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `CharacterSelectUIPage` 直接基类迁移提交 `a3f5b3d` 已存在。
- 已读取 `WaveTransitionUIPage` 和 `UI Wave Transition.prefab` 根节点组件，确认本轮只迁移页面生命周期和动画适配，不改升级卡、饰品选择业务数据流。
- 已确认 `WaveTransitionUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`。
- 已确认 `UI Wave Transition.prefab` 根节点仍保留 `CanvasGroup`、`WaveTransitionUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已确认该 Prefab 当前 `UISequenceDirector` enter / exit 组为空；`UIMotionTransition` 仍作为统一适配入口保留，后续若补动画无需再改页面脚本。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `WaveTransitionUIPage` 尚未在真实 Play Mode 中验证波次状态快照、升级卡刷新、升级选择、饰品选择、宝箱容器显示 / 隐藏、入退场路径和事件解绑。
- `WaveTransitionManager.RefreshUpgradeCards()` 仍通过 `FindFirstObjectByType<WaveTransitionUpgradeCardGroup>()` 兜底查找 UI 组件；这是既有业务依赖，后续业务依赖清理时应改成明确上下文 / 事件 / 页面引用。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `WaveTransitionUIPage` 直接基类迁移。
- 继续迁移下一个业务页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `GamingUIPage`，需要同步处理 `RequiresTick` / `OnTick()` 和上下文解析。

### 2026-05-05 阶段 12 最终收口：GamingUIPage 直接继承 PageBase

完成内容：

- 迁移战斗 HUD 页面 `GamingUIPage`，覆盖战斗中波次、计时、金币、角色状态、Buff、背包、摇杆输入和暂停按钮。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 旧 `OnPageTick(float)` 迁移为新框架 `RequiresTick => true` + `OnTick(float)`，确保只有战斗 HUD 这种需要逐帧输入发布的页面进入新 `UIManager` Tick 列表。
- 打开上下文改为直接从 `OpenContext.GetPayload<GamingPageContext>()` 读取，不再依赖旧 `UIPageOpenContext`；关闭仍复用 `PageContextBinding.Release()` 释放业务上下文。
- `UI Gaming.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。

修改文件：

- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Gaming.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `WaveTransitionUIPage` 直接基类迁移提交 `7cd8552` 已存在。
- 已读取 `GamingUIPage`、新 `UIManager` Tick 注册逻辑、`OpenContext`、`GamingPageContext`、`PageContextBinding` 和 `UI Gaming.prefab` 根节点组件，确认本轮只迁移页面生命周期、Tick 入口和动画适配。
- 已确认 `GamingUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`、`OnPageTick()`。
- 已确认 `GamingUIPage.cs` 包含 `RequiresTick => true` 和 `OnTick(float)`，可继续发布摇杆输入。
- 已确认 `UI Gaming.prefab` 根节点仍保留 `CanvasGroup`、`GamingUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `GamingUIPage` 尚未在真实 Play Mode 中验证 HUD 文本刷新、角色状态、背包 Facade、BuffBar、Tooltip、暂停按钮、摇杆输入发布、入场动画、退场动画和事件解绑。
- `UIPageContextFactory` 仍存在 `FindFirstObjectByType` 兜底解析 Player、InventoryOperateManager、ShopManager；这是既有业务上下文装配方式，后续业务依赖清理时应改为明确 payload / 场景引用 / 服务注入。
- 其他业务页面仍继承旧 `UIPageBase`，需要逐页迁移生命周期并为对应 Prefab 补 `UIMotionTransition`。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GamingUIPage` 直接基类迁移。
- 继续迁移下一个业务页面直接继承 `Orange.UIFramework.PageBase`，优先考虑 `ShopUIPage`，需要重点确认 Shop 页面上下文和侧栏 Motion 仍只作为业务子视图保留。

### 2026-05-05 阶段 12 最终收口：ShopUIPage 直接继承 PageBase

完成内容：

- 迁移商店页面 `ShopUIPage`，覆盖商店商品刷新、购买 / 锁定、继续按钮、属性侧栏和背包侧栏所在页面。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 打开上下文改为直接从 `OpenContext.GetPayload<ShopPageContext>()` 读取，不再依赖旧 `UIPageOpenContext`；无 payload 时继续使用 `UIPageContextFactory.CreateShopPageContext()` 作为既有业务兜底。
- 保留 `ShopPageController`、`ShopListView`、`ShopSidebarHost`、`InventoryUiBinder` 和商店内部侧栏 Motion 逻辑，作为页面私有业务子视图继续运行，不注册到全局 Catalog。
- `UI Shop.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，确保直接使用新 `ViewBase` 生命周期后仍能复用老动画系统。

修改文件：

- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Shop.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `GamingUIPage` 直接基类迁移提交 `5ce914a` 已存在。
- 已读取 `ShopUIPage`、`OpenContext`、`ViewBase`、`PageBase`、`UIMotionTransition`、`ShopPageContext`、`UIPageContextFactory`、`ShopListView`、`ShopSidebarHost` 和 `UI Shop.prefab` 根节点组件，确认本轮只迁移页面生命周期、上下文读取和动画适配。
- 已确认 `ShopUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`。
- 已确认 `UI Shop.prefab` 根节点仍保留 `CanvasGroup`、`ShopUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`；旧序列化字段 `autoPlaySequenceDirector` 已从页面组件移除。
- 已执行 `git diff --check -- Assets/Scripts/UI/Instances/ShopUIPage.cs "Assets/Resources/Prefabs/New UI/Pages/UI Shop.prefab" ORANGE_UI_FRAMEWORK_DEVELOPMENT.md ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- `ShopUIPage` 尚未在真实 Play Mode 中验证商店刷新、购买、锁定、继续按钮、左右侧栏切换、背包绑定、属性展示、入场动画、退场动画和事件解绑。
- `ShopSidebarHost` / `ShopListView` 仍沿用旧 `AXR.Framework.UI` 下的业务动画接口与点击组件；这些属于当前老动画系统和业务 UI 子视图沿用，不再承担页面托管桥接职责，后续清理旧命名空间时需要单独收口。
- `UIPageContextFactory` 仍存在 `FindFirstObjectByType` 兜底解析 Player、InventoryOperateManager、ShopManager；这是既有业务上下文装配方式，后续业务依赖清理时应改为明确 payload / 场景引用 / 服务注入。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `ShopUIPage` 直接基类迁移。
- 继续迁移最后一个仍处于迁移期接入状态的业务页面 `GamePauseMenu`，需要重点处理旧 `HasAdditionalCloseWaitActions()` / `PlayAdditionalCloseWaitActions()` 关闭等待扩展点，并删除或收口业务私有空 `ConfigureTimings()`。

### 2026-05-05 阶段 12 最终收口：GamePauseMenu 直接继承 PageBase

完成内容：

- 迁移暂停菜单 `GamePauseMenu`，覆盖战斗中暂停、继续游戏、返回主菜单、设置侧栏和背包绑定所在页面。
- 脚本从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- 旧 `OnPageOpened(UIPageOpenContext)` / `OnPageClosed()` 生命周期迁移为新 `OnOpeningAsync(OpenContext, CancellationToken)` / `OnClosed(CloseReason)`。
- 旧 `HasAdditionalCloseWaitActions()` / `PlayAdditionalCloseWaitActions()` 页面关闭等待扩展点迁移为 `OnClosingAsync(CloseReason, CancellationToken)`，关闭时先取消内容面板切换并等待内容面板隐藏动画完成，再交给新 `ViewBase` 继续执行页面退场动画。
- 打开上下文改为直接从 `OpenContext.GetPayload<PauseMenuContext>()` 读取，不再依赖旧 `UIPageOpenContext`；无 payload 时继续使用 `UIPageContextFactory.CreatePauseMenuContext()` 作为既有业务兜底。
- 删除业务私有空 `PauseMenuPanelBinding.ConfigureTimings()` 以及未生效的 `slideDuration` 序列化字段，避免继续暴露无效 Timing API。
- `UI Pause.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，并移除旧页面组件上的 `autoPlaySequenceDirector` 与 `slideDuration` 序列化字段。

修改文件：

- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Pause.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `ShopUIPage` 直接基类迁移提交 `b28a651` 已存在。
- 已读取 `GamePauseMenu.cs`、`PauseMenuContext.cs`、旧 `UIPageBase` 关闭等待管线、`DOTweenUniTaskExtensions`、`UI Pause.prefab` 根节点组件和新 `ViewBase` 生命周期，确认本轮只迁移页面生命周期、关闭等待、无效 Timing API 和动画适配。
- 已确认 `GamePauseMenu.cs` 不再残留 `UIPageBase`、`UIPageOpenContext`、`OnPageOpened()`、`OnPageClosed()`、`HasAdditionalCloseWaitActions()`、`PlayAdditionalCloseWaitActions()`、`ConfigureTimings()`、`slideDuration` 或 `OnValidate()`。
- 已确认 `GamePauseMenu.cs` 中 `AXR.Framework.UI` 仅用于暂停栏业务子视图动画接口 `IUIRuntimeMotion` / `UIMotionClipIds`，不再承担页面托管桥接职责。
- 已确认 `UI Pause.prefab` 根节点仍保留 `CanvasGroup`、`GamePauseMenu`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`；旧序列化字段 `autoPlaySequenceDirector` 和 `slideDuration` 已从页面组件移除。
- 已执行 `git diff --check -- Assets/Scripts/UI/Instances/GamePauseMenu.cs "Assets/Resources/Prefabs/New UI/Pages/UI Pause.prefab"`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 额外静态扫描 `Assets/Scripts/UI/Instances` 发现阶段 12 清单外的 `BookUIPage` 仍继承旧 `UIPageBase`，已记录为下一轮入口。

遗留风险：

- `GamePauseMenu` 尚未在真实 Play Mode 中验证暂停恢复、返回主菜单、背包绑定、设置侧栏显示 / 隐藏、时间缩放为 0 时的 UI 动画、关闭等待顺序、页面退场动画和事件解绑。
- 暂停栏内容面板仍沿用旧 `IUIRuntimeMotion` / `UIMotionClipIds` 动画接口；这是老动画系统沿用部分，不再是页面托管桥接，后续清理旧命名空间时需要单独处理。
- `BookUIPage` 仍继承旧 `UIPageBase`，需要判断其是否仍有有效业务入口、Prefab 或 Catalog 注册，并迁移或清理。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。
- Unity 批处理或 Editor 导入仍可能留下 `ProjectSettings/ProjectSettings.asset` 行尾 / 导入痕迹；提交时不得纳入无关导入变更。

下一步：

- 提交 `GamePauseMenu` 直接基类迁移。
- 核查并处理 `BookUIPage`：先查 Prefab、Catalog、业务调用和资源引用，再决定迁移到新 `PageBase` 还是作为旧无用资源清理，完成后继续进入旧 UIManager / 旧 Catalog / 临时委托清理。

### 2026-05-05 阶段 12 最终收口：BookUIPage 补漏直接继承 PageBase

完成内容：

- 核查阶段 12 清单外仍继承旧 `UIPageBase` 的 `BookUIPage`。
- 确认 `BookUIPage` 挂载在真实 Prefab `Assets/Resources/Prefabs/New UI/Pages/UI Gold Book.prefab` 根节点，且旧 `UIPrefabCatalog.asset` 和 `UpgradeCardSystemBuilder.ConfigureUIPrefabCatalog()` 均包含 `UI Gold Book.prefab` 条目，因此不能直接当作无用脚本删除。
- 将 `BookUIPage` 从旧 `AXR.Framework.UI.UIPageBase` 改为直接继承 `Orange.UIFramework.PageBase`。
- `UI Gold Book.prefab` 根节点新增 `UIMotionTransition`，显式指向现有 `UISequenceDirector`，并移除旧页面组件上的 `autoPlaySequenceDirector` 字段。
- `OrangeUIViewCatalog.asset` 新增 `page.goldBook`，Layer 使用 `Page`，Prefab 指向 `UI Gold Book.prefab`，确保后续如需打开该页面可由新 UIManager 按类型解析。
- 扩展真实 Catalog 资产测试 `OrangeCatalog_RegistersMigratedBusinessPages`，加入 `BookUIPage` 类型解析、Id 和 Layer 断言。

修改文件：

- `Assets/Scripts/UI/Instances/Book/BookUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Pages/UI Gold Book.prefab`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/ViewCatalogEditModeTests.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `GamePauseMenu` 直接基类迁移提交 `27137c6` 已存在。
- 已读取 `BookUIPage.cs`、`BookUIPage.cs.meta`、`UI Gold Book.prefab` 根节点组件、旧 `UIPrefabCatalog.asset`、新 `OrangeUIViewCatalog.asset` 和 `UpgradeCardSystemBuilder.ConfigureUIPrefabCatalog()` 中的旧构建逻辑。
- 已确认 `BookUIPage.cs` 不再残留 `AXR.Framework.UI`、`UIPageBase`、无用 `DG.Tweening` using 或无用 `UnityEngine` using。
- 已确认 `UI Gold Book.prefab` 根节点仍保留 `CanvasGroup`、`BookUIPage`、原 `UISequenceDirector`，并新增 `UIMotionTransition` 指向 `UISequenceDirector`；旧序列化字段 `autoPlaySequenceDirector` 已从页面组件移除。
- 已确认 `OrangeUIViewCatalog.asset` 新增 `page.goldBook`，`ViewCatalogEditModeTests` 已增加 `BookUIPage` 断言。

遗留风险：

- 没有找到运行时代码直接调用 `OpenPage<BookUIPage>()` 或资源加载 `UI Gold Book.prefab`；目前判断它是旧升级卡测试 / 生成流程遗留页面，迁移是为了清除旧基类和旧 Catalog 残留，不改变业务入口。
- `UpgradeCardSystemBuilder` 仍使用旧 `UIPrefabCatalog` 和旧 `UIManager` 类型生成测试场景；旧托管清理阶段需要迁移或删除这段编辑器生成逻辑。
- `BookUIPage` 尚未在真实 Play Mode 中验证金书页面动画和打开 / 关闭流程。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧 `UIPageBase` 和迁移期非泛型 Type API 尚未删除。

下一步：

- 提交 `BookUIPage` 补漏直接基类迁移。
- 进入旧 UI 托管清理：先核查 `AXR.Framework.UI.UIManager`、`UIPageBase`、`IUIPage`、`UIPageOpenContext`、旧 `UIPrefabCatalog.asset`、旧 `UIFrameworkSettings.asset`、`UpgradeCardSystemBuilder` 旧生成逻辑和新 `UIManager` 迁移期非泛型 Type API 引用链，再分批清理。

### 2026-05-05 阶段 12 最终收口：升级卡测试场景生成模块接入新 UIManager

完成内容：

- 迁移升级卡测试场景生成模块，解除它对旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog`、旧 `UIPrefabEntry` 和旧 `UILayerType` 的依赖。
- `UpgradeCardSystemBuilder` 改为加载 `Assets/Resources/Data/UI/OrangeUIFrameworkSettings.asset` 和 `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`。
- `UpgradeCardSystemBuilder.ConfigureViewCatalog()` 改为直接写入新 `ViewCatalog` 的 `ViewDefinition` 列表，补齐 `page.gameOver`、`page.stageComplete` 和 `page.goldBook`，Layer 使用新框架 `ViewLayer`。
- `UpgradeCardSystemBuilder.BuildTestScene()` 在生成测试场景时挂载新 `Orange.UIFramework.UIManager`，并设置新 Settings / ViewCatalog。
- `UpgradeCardTestSceneController` 改为引用 `Orange.UIFramework.UIManager`，打开升级页时使用 `ResetToPageAsync<WaveTransitionUIPage>()`，避免继续调用旧同步 `ResetToPage<T>()`。

修改文件：

- `Assets/Scripts/Editor/Upgrades/UpgradeCardSystemBuilder.cs`
- `Assets/Scripts/Upgrades/Tests/UpgradeCardTestSceneController.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认 `BookUIPage` 补漏迁移提交 `513afe7` 已存在。
- 已读取 `UpgradeCardSystemBuilder.cs`、`UpgradeCardTestSceneController.cs`、新 `UIManager`、`ViewCatalog`、`ViewDefinition`、`UIFrameworkSettings`、`OrangeUIViewCatalog.asset` 和 `OrangeUIFrameworkSettings.asset`。
- 已确认 `UpgradeCardSystemBuilder.cs` 与 `UpgradeCardTestSceneController.cs` 不再残留 `AXR.Framework.UI`、`UIPrefabCatalog`、`UIPrefabEntry`、`UILayerType`、`UIPageBase`、`UIPageOpenContext` 或旧同步 `ResetToPage<T>()`。
- 已确认 `UpgradeCardSystemBuilder` 写入的新 Catalog 条目与当前 `OrangeUIViewCatalog.asset` 已注册页面一致，且新增生成逻辑覆盖 `GameOverUIPage`、`StageCompleteUIPage` 与 `BookUIPage`，避免重建升级卡系统时回退到旧 Catalog 内容。
- 已执行 `git diff --check -- Assets/Scripts/Editor/Upgrades/UpgradeCardSystemBuilder.cs Assets/Scripts/Upgrades/Tests/UpgradeCardTestSceneController.cs ORANGE_UI_FRAMEWORK_DEVELOPMENT.md ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- 本轮未运行 Unity Play Mode，也未实际执行 `Survivors/Upgrades/Rebuild Upgrade Card System` 菜单；测试场景生成后的 Inspector 引用和升级页打开流程仍需最后统一手动验收。
- `UpgradeCardTestSceneController` 的 `Start()` 仍是协程入口，内部以 `Forget()` 触发异步页面打开；这是测试脚本的低风险过渡写法，后续若要严控测试流程可整体改为 UniTask 生命周期。
- 旧 `AXR.Framework.UI.UIManager`、旧 `UIPageBase`、旧 `UIPrefabCatalog.asset`、旧 `UIFrameworkSettings.asset` 和迁移期非泛型 Type API 仍未删除。
- `AXR.Framework.UI` 命名空间下仍有 `UIClickTarget`、`IUIRuntimeMotion`、`UISequenceDirector`、`UIMotionPlayer` 等业务仍在使用的动画 / 点击组件，不能和旧页面托管一起误删。

下一步：

- 提交升级卡测试场景生成模块接入新 UIManager。
- 继续旧 UI 托管清理：优先按引用链删除旧 `UIManager`、旧 `UIPageBase`、旧 `IUIPage`、旧 `UIPageOpenContext`、旧 Navigation、旧 Catalog 资源和新 `UIManager` 迁移期非泛型 Type API，同时保留老动画系统与点击组件直到业务子视图完成单独收口。

### 2026-05-05 阶段 12 最终收口：清理旧 UI 页面托管与旧 Catalog

完成内容：

- 删除旧 `AXR.Framework.UI.UIManager`、旧 `UIPageBase`、旧 `IUIPage`、旧 `IUIManager`、旧 `UIPageOpenContext`、旧 `UIPageEventArgs`、旧 `UIRuntimeState`、旧 Navigation 过渡序列、旧 `UIFrameworkSettings`、旧 `UIPrefabCatalog`、旧 `UIPrefabEntry`、旧 `UILayerType` 和旧 `UIManagerEditor`。
- 删除旧资源 `Assets/Resources/Data/UI/UIFrameworkSettings.asset`、`UIPrefabCatalog.asset`、`UIPrefabCatalogTest.asset` 及对应 `.meta`。
- `Game Scene` 移除同一 UIManager 对象上的旧 UIManager 组件，仅保留新 `Orange.UIFramework.UIManager`。
- `UI Test Scene` 与 `Upgrade Card Test Scene` 的 UIManager 组件从旧脚本 GUID 切换为新 `Orange.UIFramework.UIManager`，并改为引用 `OrangeUIFrameworkSettings.asset` 与 `OrangeUIViewCatalog.asset`。
- 新 `IUIManager` / `UIManager` 增加正式 `ClosePageAsync<TPage>()`，`GameManager` 改为泛型关闭，不再调用迁移期 `ClosePageAsync(Type)`。
- 移除新 `UIManager` 的迁移期非泛型 Page API：`OpenPageAsync(Type)`、`OpenPage(Type)`、`ClosePageAsync(Type)`、`IsOpen(Type)` 以及旧基类桥接测试。
- `InventoryUI` 的 Popup 层解析改为新 `ViewLayer.Popup`；`PageContextBinding` 删除旧 `UIPageOpenContext` 解析入口，仅保留上下文释放。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/IUIManager.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scripts/UI/Contracts/Contexts/PageContextBinding.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/RuntimeTestViews.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeEditModeTests.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIManagerRuntimeTestHarness.cs`
- `Assets/Scenes/Game Scene.unity`
- `Assets/Scenes/UI Test Scene.unity`
- `Assets/Scenes/Upgrade Card Test Scene.unity`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/Framework/UI/Core/Runtime/UIManager.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIPageBase.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/IUIPage.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/IUIManager.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIPageOpenContext.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIPageEventArgs.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIRuntimeState.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/Navigation/`
- `Assets/Scripts/Framework/UI/Core/Data/UIFrameworkSettings.cs`
- `Assets/Scripts/Framework/UI/Core/Data/UILayerDefinition.cs`
- `Assets/Scripts/Framework/UI/Core/Data/UILayerType.cs`
- `Assets/Scripts/Framework/UI/Core/Data/UIPrefabCatalog.cs`
- `Assets/Scripts/Framework/UI/Core/Data/UIPrefabEntry.cs`
- `Assets/Scripts/Framework/UI/Core/Editor/UIManagerEditor.cs`
- `Assets/Resources/Data/UI/UIFrameworkSettings.asset`
- `Assets/Resources/Data/UI/UIPrefabCatalog.asset`
- `Assets/Resources/Data/UI/UIPrefabCatalogTest.asset`

验证情况：

- 已按本轮强制流程重新读取本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和 Git 状态，并确认升级卡测试场景生成模块迁移提交 `8ff16c9` 已存在。
- 已静态扫描确认业务代码不再引用 `UIPageBase`、`UIPageOpenContext`、`IUIPage`、旧 `UIPrefabCatalog`、旧 `UIPrefabEntry`、旧 `UILayerType`、旧 `BeginTransition`、旧同步 `ResetToPage<T>()` 或新 `UIManager` 迁移期 Type API。
- 已静态扫描确认 `Assets/Scenes/*.unity`、Prefab 和资产中不再引用旧 UIManager 脚本 GUID `70a84d527ff33784cad51715be1b7076`，也不再引用旧 `UIFrameworkSettings.asset` / `UIPrefabCatalog.asset` GUID；唯一命中 `OrangeUIFrameworkSettings` 是新资源名本身。
- 已保留 `UIClickTarget`、`IUIRuntimeMotion`、`IUISequenceMotion`、`UISequenceDirector`、`UIMotionPlayer`、Motion V2 Track、`UISidebarEdgeDirection` 与 `UIFrameworkConstants`，避免破坏老动画系统和业务点击组件。
- 未执行完整 Unity Play Mode；按用户要求本轮只做最小静态验证，真实游戏打开测试留到最终统一验收。

遗留风险：

- `Game Scene`、`UI Test Scene`、`Upgrade Card Test Scene` 的 YAML 已按字段名和 GUID 静态收口，但尚未在 Unity Editor 中打开场景验证 Inspector 显示与运行时初始化。
- 旧 `AXR.Framework.UI` 命名空间仍存在动画 / 点击组件，后续如果要完全迁入 Orange 命名空间，需要单独处理脚本 GUID、Prefab 组件和 Motion 资源引用，不能和业务迁移混在一个提交里。
- `Assets/Scripts/UI/Regions` 与 `Assets/Scripts/UI/Contracts` 仍需继续核查，区分有效业务子视图与无用抽象。
- 真实场景手动验证清单仍未执行，最终收口完成后必须补一次 Play Mode 验收，确认打开游戏即可测试。

下一步：

- 提交旧 UI 页面托管与旧 Catalog 清理。
- 继续最终收口，优先核查 `Assets/Scripts/UI/Regions`、`Assets/Scripts/UI/Contracts` 和仍使用 `FindFirstObjectByType` 的 UI 装配入口，删除无用抽象，保留仍有 Prefab / 业务调用的子视图组合。

### 2026-05-05 阶段 12 最终收口：收口商店页面局部无用抽象

完成内容：

- 将 `ShopUIPage` 的控制器字段从通用 `IPageController` 改为具体 `ShopPageController`，删除只服务单一页面的 `IPageController`。
- 将 `ShopPageController` 的视图依赖从 `IShopPageView` 改为具体 `ShopUIPage`，删除只服务商店页面的 `IShopPageView`。
- 将 `ShopPropertiesSidebarView` 与 `ShopInventorySidebarView` 从 `ISidebarRegion` 接口实现改为普通页面内部子视图。
- `ShopSidebarHost` 直接组合 `ShopPropertiesSidebarView` 与 `ShopInventorySidebarView`，删除 `SidebarRegionGroup`。
- 删除未被任何代码引用的 `SidebarMotionGroup`。
- 保留 `SidebarMotion`、`SidebarToggleView`、`PropertiesDescriberBinding`、`Inventory` 子视图和老动画 / 点击组件，因为它们仍有真实业务调用链。

修改文件：

- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopPageController.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopSidebarHost.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopPropertiesSidebarView.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopInventorySidebarView.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/UI/Contracts/IPageController.cs`
- `Assets/Scripts/UI/Pages/Shop/IShopPageView.cs`
- `Assets/Scripts/UI/Regions/ISidebarRegion.cs`
- `Assets/Scripts/UI/Regions/SidebarRegionGroup.cs`
- `Assets/Scripts/UI/Regions/SidebarMotionGroup.cs`

验证情况：

- 已按下一轮入口核查 `Assets/Scripts/UI/Regions` 与 `Assets/Scripts/UI/Contracts` 的文件列表、类型定义和引用链。
- 已确认 `Inventory` 区域子视图、`PropertiesDescriberBinding`、`SidebarMotion`、`SidebarToggleView`、Context、Facade 与 Snapshot 仍有业务调用链，本轮未删除。
- 已静态扫描确认 `IPageController`、`IShopPageView`、`ISidebarRegion`、`SidebarRegionGroup`、`SidebarMotionGroup` 已无脚本引用。
- 本轮未运行 Unity Play Mode；按用户要求只做最小静态验证，商店刷新、购买、锁定、侧栏切换和背包绑定留到最终统一验收。

遗留风险：

- `ShopPageController` 现在直接依赖 `ShopUIPage`，这是有意收口单页面局部抽象；如果后续需要测试纯控制器，需要以页面级集成测试或重新引入有明确价值的局部接口处理。
- `UIPageContextFactory`、`ResolvingInventoryUiFacade`、`ResolvingShopUiFacade` 仍通过运行时查找兜底解析业务对象，后续需要优先改为明确 payload / 场景引用 / 服务注入。
- 老动画 / 点击组件仍在旧 `AXR.Framework.UI` 命名空间中，尚未迁入 Orange 命名空间。

下一步：

- 提交商店页面局部无用抽象收口。
- 继续最终收口，优先处理 `UIPageContextFactory` 和 Resolving Facade 中的 `FindFirstObjectByType` 隐藏依赖，目标是让业务 UI 打开时依赖明确 payload 或场景装配。

### 2026-05-05 阶段 12 最终收口：显式装配页面上下文依赖

完成内容：

- 将 `GameManager` 扩展为战斗 / 商店 / 暂停页面上下文的显式装配入口，新增序列化引用 `InventoryOperateManager` 与 `ShopManager`。
- `Game Scene` 的 `GameManager` 已绑定主场景真实 `InventoryOperateManager` 与 `ShopManager` 组件，避免打开业务页面时由 UI 层扫描场景。
- `UIPageContextFactory` 改为只接受显式 `Player`、`InventoryOperateManager`、`ShopManager` 参数，内部只创建 `ManagerInventoryUiFacade` / `ManagerShopUiFacade`，并保留 `InventoryOperateManager.Bind(player)` 绑定逻辑。
- `GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 不再在 payload 缺失时自行调用工厂兜底；缺少对应 Context 时直接抛出明确异常，暴露装配错误。
- `InventoryUI` 独立运行时不再退回到 `ResolvingInventoryUiFacade`；没有外部配置 Facade 且没有显式 `InventoryOperateManager` 时直接抛出明确异常。
- 删除延迟扫描场景的 `ResolvingInventoryUiFacade` 与 `ResolvingShopUiFacade` 及 `.meta` 文件。

修改文件：

- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/UI/Contracts/Contexts/UIPageContextFactory.cs`
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scenes/Game Scene.unity`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/UI/Contracts/Facades/ResolvingInventoryUiFacade.cs`
- `Assets/Scripts/UI/Contracts/Facades/ResolvingInventoryUiFacade.cs.meta`
- `Assets/Scripts/UI/Contracts/Facades/ResolvingShopUiFacade.cs`
- `Assets/Scripts/UI/Contracts/Facades/ResolvingShopUiFacade.cs.meta`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并确认当前仅有无关 `ProjectSettings/ProjectSettings.asset` 未提交变更需排除。
- 已读取 `GameManager`、`UIPageContextFactory`、两个 Manager Facade、两个 Resolving Facade、三个业务页面和 `InventoryUI` 的当前实现，确认本轮只收口上下文装配与延迟解析 Facade。
- 已静态扫描确认 C# 代码中不再残留 `ResolvingInventoryUiFacade`、`ResolvingShopUiFacade`、无参 `CreateGamingPageContext()` / `CreatePauseMenuContext()` / `CreateShopPageContext()`、`FindFirstObjectByType<InventoryOperateManager>`、`FindFirstObjectByType<ShopManager>` 或页面 payload 兜底调用。
- 已静态扫描 `Assets/Scripts/UI`，当前剩余 `FindFirstObjectByType` 只在 `TooltipHoverTarget` 解析 `UITooltipPresenter`、`StageCompleteUIPage` 解析 `StageCompleteSummaryManager`，已记录为下一轮候选收口点。
- 已确认 `Game Scene` 中 `GameManager` 绑定 `inventoryOperateManager: {fileID: 1941351701}` 与 `shopManager: {fileID: 59292223}`。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。

遗留风险：

- 本轮未运行 Unity Play Mode，也未打开 Unity Editor 检查 `GameManager` Inspector 新字段；主场景 YAML 已静态绑定，仍需最终真实场景验收确认打开游戏后页面上下文装配正确。
- `Weapon Test Scene` 中存在旧 `GameManager` 场景对象但未绑定新增业务 Manager 字段；该场景原本也缺 UIManager 引用，当前不作为本轮目标，若后续要恢复测试场景可单独整理。
- `TooltipHoverTarget` 仍有 `UITooltipPresenter` 的 `FindFirstObjectByType` 兜底，`StageCompleteUIPage` 仍有 `StageCompleteSummaryManager` 兜底，需要后续按真实引用链逐个收口。
- 老动画 / 点击组件仍在旧 `AXR.Framework.UI` 命名空间中，尚未迁入 Orange 命名空间。

下一步：

- 提交显式装配页面上下文依赖。
- 继续最终收口，优先处理 UI 目录剩余 `FindFirstObjectByType`：先核查 `TooltipHoverTarget` 与 `UITooltipPresenter` 的 Prefab / 页面引用关系，再决定接入 Orange Tooltip API 还是改为显式页面注入；同时评估 `StageCompleteUIPage` 的 `StageCompleteSummaryManager` 是否可由 `GameManager` 或页面 payload 显式传入。

### 2026-05-05 阶段 12 最终收口：移除 Tooltip 全局 Presenter 兜底

完成内容：

- 删除 `UITooltipPresenter.ActivePresenter` 静态入口和启用 / 禁用时维护全局 Presenter 的逻辑。
- `TooltipHoverTarget` 不再通过静态 Presenter 或 `FindFirstObjectByType<UITooltipPresenter>()` 兜底；只接受显式字段、页面父级或运行时注入的 Presenter。
- `GamingHudView` 在绑定 HUD 时将当前页面的 `UITooltipPresenter` 注入 `BuffBarUI`，解绑时清空。
- `BuffBarUI` 保存当前 Tooltip Presenter，并在动态实例化 Buff 图标时配置其 `TooltipHoverTarget`。
- `UITooltipPresenter` 在显示 / 隐藏时强制 `interactable = false`、`blocksRaycasts = false`，避免 Tooltip 遮挡真实输入。

修改文件：

- `Assets/Scripts/UI/Instances/Child/TooltipHoverTarget.cs`
- `Assets/Scripts/UI/Instances/Child/UITooltipPresenter.cs`
- `Assets/Scripts/UI/Instances/Child/BuffBarUI.cs`
- `Assets/Scripts/UI/Pages/GamingHudView.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已重新读取 Git 状态、本文尾部和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 相关迁移记录，确认本轮只处理 Tooltip 全局查找链路。
- 已读取 `TooltipHoverTarget`、`UITooltipPresenter`、`BuffBarUI`、`GamingHudView`，并通过 Prefab GUID 静态确认 `TooltipHoverTarget` 挂在 `BuffIconItem.prefab`，`UITooltipPresenter` 存在于 `UI Gaming.prefab` 和独立 `Tooltip.prefab`。
- 已静态扫描 `Assets/Scripts/UI`，确认不再残留 `ActivePresenter` 或 `FindFirstObjectByType<UITooltipPresenter>()`；当前 UI 目录唯一剩余 `FindFirstObjectByType` 是 `StageCompleteUIPage` 解析 `StageCompleteSummaryManager`。
- 本轮未运行 Play Mode；Buff Tooltip 的指针按下、移动、抬起、退出和不阻挡输入仍需最终真实场景验收。

遗留风险：

- 现有 Tooltip 仍使用业务侧 `UITooltipPresenter` 视觉实现，而不是全量改成 Orange `ShowTooltipAsync<TTooltip>()`；本轮目标是先删除静态 / 全局查找兜底，避免扩大到 Prefab 结构重做。
- `Assets/Resources/Prefabs/New UI/Container/Tooltip.prefab` 中仍挂载 `UITooltipPresenter`，当前未接入 `OrangeUIViewCatalog`；后续如要将业务 Tooltip 完全纳入 Orange Tooltip Stack，需要单独迁移该 Prefab 与调用 API。
- `StageCompleteUIPage` 仍有 `StageCompleteSummaryManager` 的场景扫描兜底，下一轮优先处理。

下一步：

- 提交 Tooltip 全局 Presenter 兜底移除。
- 继续最终收口，优先处理 `StageCompleteUIPage` 的 `StageCompleteSummaryManager` 依赖，把结算摘要来源改为显式场景引用或页面 payload。

### 2026-05-06 阶段 12 最终收口：StageComplete 结算上下文显式化

完成内容：

- 新增 `StageCompletePageContext`，以页面 payload 形式携带 `StageCompleteSnapshot`。
- `UIPageContextFactory` 新增 `CreateStageCompletePageContext(StageCompleteSummaryManager)`，负责从显式 Manager 创建结算快照。
- `GameManager` 新增序列化字段 `StageCompleteSummaryManager stageCompleteSummaryManager`，打开 `StageCompleteUIPage` 时通过工厂传入 payload。
- `Game Scene` 的 `GameManager` 已绑定真实 `StageCompleteSummaryManager` 组件 `{fileID: 1566269878}`。
- `StageCompleteUIPage` 删除 `summaryManager` 序列化字段和 `FindFirstObjectByType<StageCompleteSummaryManager>()` 兜底，只从 `OpenContext` 获取 `StageCompletePageContext`，缺失时抛出明确异常。

修改文件：

- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/UI/Contracts/Contexts/UIPageContextFactory.cs`
- `Assets/Scripts/UI/Contracts/Contexts/StageCompletePageContext.cs`
- `Assets/Scripts/UI/Contracts/Contexts/StageCompletePageContext.cs.meta`
- `Assets/Scripts/UI/Instances/StageCompleteUIPage.cs`
- `Assets/Scenes/Game Scene.unity`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已在 2026-05-06 重新读取 Git 状态、本文尾部和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，确认本轮从中断后的 StageComplete 改动恢复。
- 已读取 `GameManager`、`UIPageContextFactory`、`StageCompletePageContext`、`StageCompleteUIPage`、`StageCompleteSummaryManager` 和 `Game Scene` 相关 YAML，确认页面层不再直接解析 Manager。
- 已静态扫描 `Assets/Scripts/UI`，确认不再残留 `FindFirstObjectByType`、`ActivePresenter`、`Resolving*Facade`、旧 `UIPageBase` / `UIPageOpenContext` / `UIPrefabCatalog` 或迁移期 Type API。
- 已确认 `Game Scene` 中 `GameManager` 新字段 `stageCompleteSummaryManager` 指向主场景真实 `StageCompleteSummaryManager` 组件。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 按用户要求未执行完整 Play Mode；本轮保留最小静态验证，最终打开游戏测试时需要重点确认通关页摘要数字和按钮事件。

遗留风险：

- `StageCompleteSummaryManager` 作为业务统计 Manager 内部仍会在缺少缓存时查找 Player；本轮只收口 UI 页面和 Orange 打开 payload，不扩大到玩法统计 Manager 重构。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前有 Unity 自动生成 / 导入痕迹，均不属于本模块，提交时必须排除。
- 真实 Play Mode 尚未执行，`StageCompletePageContext` payload 链路需最终在完整游戏流程里验证。

下一步：

- 提交 StageComplete 结算上下文显式化。
- 继续最终收口，先做 UI 目录残留扫描，重点确认是否仍有旧页面托管类型、旧 Catalog 资源引用、桥接层、`FindFirstObjectByType` 和迁移期 API；若无可继续清理无用旧资源或进入最终手动验收准备。

### 2026-05-06 阶段 12 最终收口：背包操作浮层迁入 Orange Popup

完成内容：

- 新增 `InventoryOperatePopupBase`，把背包物品操作浮层抽为基于 `Orange.UIFramework.PopupBase` 的业务 Popup 基类，并在 `OnOpeningAsync()` 中从 `OpenContext.Payload` 读取 `InventoryItemOperateResource`。
- `WeaponOperatePopup` 与 `AccessoryInfoPopup` 改为继承 `InventoryOperatePopupBase`，继续保留原有展示、出售、合并事件和品质渲染逻辑。
- `InventoryOperatePopupHost` 不再持有操作浮层 Prefab、Popup Layer Root、自建透明关闭遮罩或直接 `Instantiate` / `Destroy`；现在通过 `UIManager.ShowPopupAsync()` 打开，通过 `ViewHandle.CloseAsync()` 关闭，并使用 Orange Popup 的外部点击关闭、分组互斥、PopupStack、输入焦点和池化链路。
- `InventoryUI` 删除 `weaponPopupPrefab`、`accessoryPopupPrefab`、`closeInventoryItemOperatePanelButtons` 序列化字段和 Popup Layer 解析逻辑，只负责持有列表子视图、Facade 和 `InventoryOperatePopupHost`。
- `OrangeUIViewCatalog.asset` 注册 `popup.inventory.weaponOperate` 与 `popup.inventory.accessoryInfo` 两个 Popup 条目。
- `Weapon Operate Popup.prefab` 与 `Accessory Info Popup.prefab` 根节点补充 `CanvasGroup`，满足 `ViewBase` 生命周期要求。
- `UI Shop.prefab` 清理已删除的背包操作浮层旧序列化字段引用。
- 删除无脚本 / 资源引用的旧 `InventoryItemOperateContainer` 及 `.meta`，避免迁移后留下第二套背包操作浮层实现。

修改文件：

- `Assets/Scripts/UI/Instances/Container/InventoryOperatePopupBase.cs`
- `Assets/Scripts/UI/Instances/Container/InventoryOperatePopupBase.cs.meta`
- `Assets/Scripts/UI/Instances/Container/WeaponOperatePopup.cs`
- `Assets/Scripts/UI/Instances/Container/AccessoryInfoPopup.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scripts/UI/Regions/Inventory/InventoryOperatePopupHost.cs`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `Assets/Resources/Prefabs/New UI/Pages/Shop/Weapon Operate Popup.prefab`
- `Assets/Resources/Prefabs/New UI/Pages/Shop/Accessory Info Popup.prefab`
- `Assets/Resources/Prefabs/New UI/Pages/UI Shop.prefab`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/UI/Instances/Container/InventoryItemOperateContainer.cs`
- `Assets/Scripts/UI/Instances/Container/InventoryItemOperateContainer.cs.meta`

验证情况：

- 已按本轮强制流程重新读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity 脚本相关 Skill 说明。
- 已静态扫描确认 `InventoryItemOperateContainer`、其脚本 GUID、`InventoryOperatePopupCloseMask`、`weaponPopupPrefab`、`accessoryPopupPrefab`、`closeInventoryItemOperatePanelButtons` 不再存在于运行时 Prefab / 资产引用中；预览脚本中的同名局部变量仅用于 `Resources.Load` 样式预览，未纳入运行时托管链路。
- 已确认两个操作 Popup Prefab 在 `OrangeUIViewCatalog.asset` 中注册为 `ViewKind.Popup`，并且根节点已有 `CanvasGroup` 与对应 `PopupBase` 派生脚本，满足 Catalog 校验的关键条件。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 按用户要求未执行完整 Play Mode；背包点击打开操作浮层、外部点击关闭、出售 / 合并事件回调、重复打开切换和池化复用需最终真实场景验收。

遗留风险：

- `InventoryOperatePopupHost` 使用 `UniTaskVoid` 承接 UI 点击触发的异步打开 / 关闭，异常会记录到日志；真实输入连点、页面关闭与 Popup 关闭竞态仍需 Play Mode 验证。
- 背包操作 Popup 当前使用屏幕中心定位，没有恢复旧手工 Popup 的具体锚定偏移；如后续需要锚定到背包格子，需要让 `InventoryListView` 将被点击格子的 `RectTransform` 或屏幕坐标随 entryId 一并传给 `InventoryOperatePopupHost`。
- `ItemQualityPreviewSceneController` 仍直接 `Resources.Load` 并实例化操作 Popup 用于样式预览；这不是运行时 UI 框架入口，暂未迁移。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交背包操作浮层迁入 Orange Popup。
- 继续最终收口，优先扫描仍直接手工实例化业务浮层或仍未接入 Orange Catalog 的 Popup / Tooltip 资源；同时保留 `UIClickTarget`、`IUIRuntimeMotion`、`UISequenceDirector`、`UIMotionPlayer` 等老动画 / 点击组件，除非单独迁移并同步修 Prefab / 资源引用。

### 2026-05-06 阶段 12 最终收口：描述 Tooltip 迁入 Orange Tooltip

完成内容：

- 将旧 `UITooltipPresenter` 重命名为 `DescribableTooltip`，并改为继承 `Orange.UIFramework.TooltipBase`，在 `OnOpeningAsync()` 中从 `OpenContext.Payload` 读取 `IDescribable` 并刷新图标、标题和描述列表。
- `TooltipHoverTarget` 不再持有或解析 Presenter，也不再调用页面内 `Present()` / `HideImmediate()`；现在直接通过 `UIManager.ShowTooltipAsync<DescribableTooltip>()` 打开，通过 `UIManager.UpdateTooltipPosition()` 跟随指针，通过 `UIManager.HideTooltip()` 关闭。
- `TooltipHoverTarget` 对打开中的异步请求做了轻量防重入：同一次按压只发起一个打开请求，移动时只更新 pending 位置；若松手早于打开完成，会在拿到 `ViewHandle` 后立即以 `CloseReason.Cancel` 关闭，避免 Tooltip 残留。
- `BuffBarUI`、`GamingHudView`、`GamingUIPage` 删除 Tooltip Presenter 注入字段和绑定链路，战斗 HUD 不再内嵌和管理 Tooltip 实例。
- `Tooltip.prefab` 根节点继续复用原视觉结构，但脚本已是 `DescribableTooltip`，旧 `screenOffset` / `screenPadding` 字段已清理，定位偏移和边距改由 `TooltipOptions` 提供。
- `OrangeUIViewCatalog.asset` 新增 `tooltip.describable`，注册独立 `Tooltip.prefab` 到 `ViewKind.Tooltip` / `ViewLayer.Tooltip`。
- `UI Gaming.prefab` 删除旧内嵌 `Tooltip.prefab` 实例和 `tooltipPresenter` 序列化字段引用，避免运行时存在第二套 Tooltip 管理路径。

修改文件：

- `Assets/Scripts/UI/Instances/Child/DescribableTooltip.cs`
- `Assets/Scripts/UI/Instances/Child/DescribableTooltip.cs.meta`
- `Assets/Scripts/UI/Instances/Child/TooltipHoverTarget.cs`
- `Assets/Scripts/UI/Instances/Child/BuffBarUI.cs`
- `Assets/Scripts/UI/Pages/GamingHudView.cs`
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Resources/Prefabs/New UI/Container/Tooltip.prefab`
- `Assets/Resources/Prefabs/New UI/Pages/UI Gaming.prefab`
- `Assets/Resources/Data/UI/OrangeUIViewCatalog.asset`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并在提交前先提交上一轮背包操作 Popup 模块。
- 已静态扫描确认 `UITooltipPresenter`、`SetTooltipPresenter`、`HideImmediate()`、`tooltipPresenter` 字段、`UI Gaming.prefab` 内嵌 Tooltip PrefabInstance、旧 Tooltip 实例 fileID 均不再残留于业务脚本和运行时资源。
- 已确认 `Tooltip.prefab` 根节点脚本 GUID 仍指向重命名后的 `DescribableTooltip`，并且 `OrangeUIViewCatalog.asset` 已注册 `tooltip.describable`。
- 已确认 `DescribableTooltip` 继承 `TooltipBase`，满足 `ViewCatalog` 对 `ViewKind.Tooltip` 的基类校验要求。
- 本轮按用户要求未执行完整 Play Mode；Buff Tooltip 指针按下、移动、松手、退出、快速点按取消、边缘裁剪、Camera Canvas 下定位和池化复用需最终真实场景验收。

遗留风险：

- `TooltipHoverTarget` 使用 `UniTaskVoid` 承接 UI 指针触发的异步打开，异常会记录到日志；真实输入连点、页面关闭与 Tooltip 关闭竞态仍需 Play Mode 验证。
- 当前 Tooltip 显示方式沿用旧交互语义：按下显示、移动跟随、松手或退出隐藏；如果后续希望 PC Hover 即显示，需要单独调整输入语义并验证不会影响移动端长按。
- `ItemQualityPreviewSceneController` 仍直接加载并实例化部分 UI Prefab 做样式预览；这不是运行时 UI 框架入口，暂未迁移。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交描述 Tooltip 迁入 Orange Tooltip。
- 继续最终收口，优先扫描仍未接入 Orange Catalog 的 Modal / Popup / Tooltip 资源、仍手工管理全局浮层的业务入口，以及 `Assets/Scripts/UI/Regions` / `Assets/Scripts/UI/Contracts` 中是否还有可删除的无用抽象；继续保留老动画 / 点击组件，除非单独迁移并同步修 Prefab / 资源引用。

### 2026-05-06 阶段 12 最终收口：旧动画与点击组件迁入 Orange 命名空间

完成内容：

- 将仍被业务使用的 `UIClickTarget`、`IUIRuntimeMotion`、`IUISequenceMotion`、`UISequenceDirector`、`UIMotionPlayer`、Motion Track、`UISidebarEdgeDirection` 和对应编辑器脚本从 `Assets/Scripts/Framework/UI` 移入 `Assets/Scripts/OrangeUIFramework/`。
- 这些类型统一改为 `Orange.UIFramework` 命名空间，类名、字段名和函数名保持不变，业务脚本继续直接使用熟悉的 `UIClickTarget` / `UIMotionPlayer` 等类型。
- `UIMotionDefinition` 的创建菜单从旧 `Survivors/Systems/UI/Motion Definition` 收口到 `Orange/UI Framework/Motion Definition`。
- `Arrow Left Button Motion.asset`、`Arrow Right Button Motion.asset`、`Rect Button Motion.asset` 的 `SerializeReference` 类型记录从 `AXR.Framework.UI` 更新为 `Orange.UIFramework`，避免迁移后 Motion Track 丢类型。
- 删除旧 `Assets/Scripts/Framework` 目录剩余空 `.meta`，避免仓库继续保留旧 UI 框架目录。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Data/UIFrameworkConstants.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIClickTarget.cs`
- `Assets/Scripts/OrangeUIFramework/Editor/UIMotionDefinitionEditor.cs`
- `Assets/Scripts/OrangeUIFramework/Editor/UIMotionPlayerEditor.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/UISidebarEdgeDirection.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/UIMotion/**`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/UIMotionTransition.cs`
- `Assets/Scripts/OrangeUIFramework/Tests/EditMode/Editor/UIMotionPlayerEditModeTests.cs`
- `Assets/Scripts/UI/**` 中引用旧命名空间的业务脚本
- `Assets/Resources/Data/UI/Motion/V2/Generated/Arrow Left Button Motion.asset`
- `Assets/Resources/Data/UI/Motion/V2/Generated/Arrow Right Button Motion.asset`
- `Assets/Resources/Data/UI/Motion/V2/Generated/Rect Button Motion.asset`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/Framework.meta`
- `Assets/Scripts/Framework/UI.meta`
- `Assets/Scripts/Framework/UI/Core.meta`
- `Assets/Scripts/Framework/UI/Core/Data.meta`
- `Assets/Scripts/Framework/UI/Core/Editor.meta`
- `Assets/Scripts/Framework/UI/Core/Runtime.meta`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity 项目 / 脚本 / 资产相关 Skill 说明。
- 已静态扫描 `Assets`，确认不再残留 `AXR.Framework.UI` 字符串。
- 已静态扫描 Motion 资产，确认三个生成的 Motion Definition 资产中 104 处 managed-reference 类型记录均为 `ns: Orange.UIFramework`，不再引用旧命名空间。
- 已确认业务脚本中的旧 `using AXR.Framework.UI` 已替换为 `using Orange.UIFramework`，新 `UIMotionTransition` 与测试脚本不再保留同命名空间冗余 using。
- 已尝试执行 `dotnet build .\Assembly-CSharp.csproj --no-restore -v:minimal`，失败原因是被 `.gitignore` 忽略的 Unity 生成 `.csproj` 尚未随本轮脚本移动 / 删除重新生成，仍引用旧 `Assets/Scripts/Framework/UI/**`、旧 `UITooltipPresenter.cs` 和旧 `InventoryItemOperateContainer.cs` 路径；该结果不能作为 Unity 编译是否通过的判定。
- 本轮尚未运行 Unity Editor 重新生成工程或 Play Mode；按用户要求只做最小静态验证，真实按钮点击、卡牌 Motion、侧栏 Motion、页面入退场动画和 `refreshDefaultsOnEnable` 复用行为留到最终真实场景验收。

遗留风险：

- 由于 `SerializeReference` 类型记录发生命名空间迁移，必须在 Unity Editor 打开项目后确认三个 Motion Definition 资产 Inspector 中 Track 列表不丢类型。
- 本轮保留脚本 GUID 和类名，Prefab 上的 `MonoScript` 引用应稳定；仍需 Unity 导入后确认没有 Missing Script。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交旧动画与点击组件迁入 Orange 命名空间。
- 继续最终收口，优先扫描仍未接入 Orange Catalog 的 Modal / Popup / Tooltip 资源、仍手工管理浮层的业务入口，以及 `Assets/Scripts/UI/Regions` / `Assets/Scripts/UI/Contracts` 中是否还有可删除的无用抽象；不要再引入旧 `AXR.Framework.UI` 命名空间或旧 `Assets/Scripts/Framework` 目录。

### 2026-05-06 阶段 12 最终收口：收口背包页面局部 Region 抽象

完成内容：

- 删除只被 `InventoryUI` 单点使用的 `IInventoryRegionView`、`InventoryRegionController`、`InventoryRegionState`，不再保留一套没有接入 Orange 框架核心的背包 Region 抽象。
- `InventoryUI` 直接负责 Facade 会话生命周期：订阅 / 解绑 `SnapshotChanged`、`OperatePanelOpened`、`OperatePanelShouldClose`，在启用时请求快照，在禁用或释放外部 Facade 时统一清理。
- 原 `InventoryRegionState` 的选中项、当前操作项、快照同步、物品存在性判断逻辑内聚到 `InventoryUI`，避免跨三个局部类型转发同一状态。
- `InventoryListView` 与 `InventoryOperatePopupHost` 保留为具体页面子视图协作对象；背包物品操作浮层仍通过 Orange `UIManager.ShowPopupAsync()` 打开，不回退到手工实例化。

修改文件：

- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/UI/Regions/Inventory/IInventoryRegionView.cs`
- `Assets/Scripts/UI/Regions/Inventory/IInventoryRegionView.cs.meta`
- `Assets/Scripts/UI/Regions/Inventory/InventoryRegionController.cs`
- `Assets/Scripts/UI/Regions/Inventory/InventoryRegionController.cs.meta`
- `Assets/Scripts/UI/Regions/Inventory/InventoryRegionState.cs`
- `Assets/Scripts/UI/Regions/Inventory/InventoryRegionState.cs.meta`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity UI / 架构 / 脚本 Skill 说明。
- 已静态扫描确认 `InventoryRegionController`、`InventoryRegionState`、`IInventoryRegionView` 不再残留于 `Assets/Scripts`、Prefab 和 Scene 引用中。
- 已检查 `InventoryUI` 当前实现，确认外部页面仍通过 `ConfigureFacade()` / `ReleaseConfiguredFacade()` 与 `InventoryUiBinder` 装配，不需要修改 Prefab 序列化字段。
- 本轮按用户要求未执行完整 Play Mode；背包页面打开、物品点击、Popup 重开、出售 / 合并、背包快照刷新后恢复或关闭当前 Popup 仍需最终真实场景验收。

遗留风险：

- `InventoryUI` 现在承担原控制器和状态类职责，逻辑更直接，但后续如果背包交互继续膨胀，应优先拆成真实可复用子视图或纯状态测试对象，而不是恢复旧 Region 接口。
- `InventoryOperatePopupHost` 仍使用 `UIManager.Instance` 作为新框架全局入口，属于当前 Popup / Tooltip 子视图打开路径；如后续要求全部入口显式注入，可单独做 UIManager 引用传递模块。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交背包页面局部 Region 抽象收口。
- 继续最终收口，优先处理 `GameManager` 与 `UpgradeCardTestSceneController` 中对 `FindFirstObjectByType<UIManager>()` 的兜底，主场景和升级卡测试场景已显式绑定 `uiManager`，可改为缺失即报错；同时继续评估 `ItemQualityPreviewSceneController` 是否作为预览工具保留或隔离。

### 2026-05-06 阶段 12 最终收口：UIManager 入口显式装配

完成内容：

- `GameManager.ResolveSceneReferences()` 不再通过 `FindFirstObjectByType<UIManager>()` 查找 UIManager；主业务入口必须由场景 Inspector 显式绑定 Orange `UIManager`。
- `UpgradeCardTestSceneController` 不再在启动时查找 UIManager，启动第一步先校验显式 `uiManager` 引用，避免配置错误时继续实例化测试玩家并打开升级卡页面。
- 缺少 UIManager 时的错误信息改为明确要求 explicit reference，避免把场景装配错误隐藏成运行时全局查找结果。

修改文件：

- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/Upgrades/Tests/UpgradeCardTestSceneController.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已静态扫描确认 `Assets/Scripts`、Prefab 和 Scene 中不再残留 `FindFirstObjectByType<UIManager>()`。
- 已确认 `Game Scene.unity` 的 `GameManager.uiManager` 指向 `{fileID: 2066110832}`，且场景中存在 `UIManager` 对象；`Upgrade Card Test Scene.unity` 的 `UpgradeCardTestSceneController.uiManager` 指向 `{fileID: 450740442}`。
- 已确认 `Weapon Test Scene.unity` 中 `GameManager.uiManager` 当前仍为空且场景内没有 `UIManager` 对象；该测试场景原本也无法通过旧 UIManager 查找兜底获得 UIManager，本轮不改无关测试场景装配。
- 本轮按用户要求未执行完整 Play Mode；主场景启动、升级卡测试场景打开升级页面和 Weapon Test Scene 是否仍需要 UIManager 需最终真实场景验收。

遗留风险：

- `GameManager` 仍会为 `MapGenerator` 做场景查找兜底，这属于地图 / 玩法装配，不在本轮 UIManager 收口范围内。
- `TooltipHoverTarget` 与 `InventoryOperatePopupHost` 仍使用 `UIManager.Instance` 作为新框架子视图打开 Popup / Tooltip 的运行时入口；这不是旧桥接层，但如果后续要求所有业务子视图显式注入 UIManager，需要单独改页面绑定链路。
- `Weapon Test Scene.unity` 仍有空 `GameManager.uiManager` 字段；如果该场景需要打开业务 UI，后续要补 Orange UIManager 与 Catalog / Settings 装配。

下一步：

- 提交 UIManager 入口显式装配。
- 继续最终收口，优先评估 `ItemQualityPreviewSceneController`：它仍直接 `Resources.Load` 并实例化部分 UI Prefab 做品质样式预览，需要决定是明确标记为预览工具保留，还是迁移 / 隔离避免被误认为运行时 UI 框架入口。

### 2026-05-06 阶段 12 最终收口：隔离物品品质 UI 预览工具

完成内容：

- `ItemQualityPreviewSceneController` 明确收口为 `Item Quality Preview` 场景专用的视觉预览工具，不作为运行时业务 UI 框架入口。
- 新增预览场景名校验：只有当前激活场景名为 `Item Quality Preview` 时才会构建 Canvas、加载资源并实例化预览对象；误挂到其他场景时会输出警告并停止执行。
- 保留该工具对 `InventoryItem`、`ShopItemContainer`、`WeaponOperatePopup`、`AccessoryInfoPopup` 的直接 `Resources.Load` / `Instantiate`，用途仅为品质表现视觉对照；运行时背包操作 Popup 仍由 `InventoryOperatePopupHost` 通过 Orange `UIManager.ShowPopupAsync()` 打开。

修改文件：

- `Assets/Scripts/UI/Preview/ItemQualityPreviewSceneController.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已确认 `ItemQualityPreviewSceneController` 只被 `Item Quality Preview.unity` 引用，未出现在业务场景、业务 Prefab 或运行时页面引用链中。
- 已静态扫描 UI 目录的 `Resources.Load` / `Instantiate` / `Destroy`，确认该预览脚本之外的运行时直接实例化主要是列表项、Buff 图标、商店卡片、背包格子等页面内部子视图，不是全局 Page / Popup / Modal / Tooltip 托管入口。
- 本轮按用户要求未执行完整 Play Mode；`Item Quality Preview` 场景样式展示仍需手动打开场景验证。

遗留风险：

- 该预览工具仍直接实例化 Orange Popup 派生 Prefab，因此只适合视觉静态检查，不应拿它验证 PopupStack、外部点击关闭、遮罩或 UIManager 生命周期。
- 如果后续把预览工具纳入自动化验收，应另建专用 Preview / Editor 工具目录或测试场景说明，不要把它并入运行时 UI 打开链路。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交物品品质 UI 预览工具隔离。
- 继续最终收口，先做一次 `Assets/Scripts/UI` 与 `Assets/Resources/Prefabs/New UI` 的剩余抽象和旧资源扫描：重点确认是否还有旧页面托管资源、旧 Catalog 引用、无用 Contract / Region 类型或未说明的全局浮层入口。

### 2026-05-06 阶段 12 最终收口：收口 UI 子视图 Region 命名与目录

完成内容：

- 将仍有真实业务调用链的页面私有子视图从 `Region` 命名收口为普通职责命名，不再让页面内部结构看起来像 Orange 框架级抽象。
- `GamingHudRegionHost` / `GamingInputRegionHost` 改为 `GamingHudView` / `GamingInputView`，`GamingUIPage` 同步改用 `hudView` / `inputView` 字段语义。
- `ShopListRegionView`、`ShopSidebarRegionHost`、`ShopPropertiesRegionView`、`ShopInventoryRegionView` 改为 `ShopListView`、`ShopSidebarHost`、`ShopPropertiesSidebarView`、`ShopInventorySidebarView`，`ShopUIPage` 和侧栏协作代码同步改名。
- `SidebarRegionMotion` / `SidebarToggleRegionView` 改为 `SidebarMotion` / `SidebarToggleView`，保留旧动画系统能力但移出无效 Region 命名。
- `InventoryListRegionView`、`InventoryPopupHostView`、`InventoryUiHostBinding` 改为 `InventoryListView`、`InventoryOperatePopupHost`、`InventoryUiBinder`，并移入 `Assets/Scripts/UI/Instances/Child/`。
- `PropertiesDescriberBinding` 移入 `Assets/Scripts/UI/Pages/Shop/`，作为商店页面内部绑定对象，而不是 `UI/Regions` 下的框架候选抽象。
- 删除空的 `Assets/Scripts/UI/Regions` 目录 `.meta`，当前业务 UI 不再保留 `Regions` 目录。

修改文件：

- `Assets/Scripts/UI/Instances/Child/InventoryListView.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryOperatePopupHost.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUiBinder.cs`
- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Pages/GamingHudView.cs`
- `Assets/Scripts/UI/Pages/GamingInputView.cs`
- `Assets/Scripts/UI/Pages/SidebarMotion.cs`
- `Assets/Scripts/UI/Pages/SidebarToggleView.cs`
- `Assets/Scripts/UI/Pages/Shop/PropertiesDescriberBinding.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopInventorySidebarView.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopListView.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopPropertiesSidebarView.cs`
- `Assets/Scripts/UI/Pages/Shop/ShopSidebarHost.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

删除范围：

- `Assets/Scripts/UI/Regions.meta`
- `Assets/Scripts/UI/Regions/Inventory.meta`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity 脚本 / 资源 Skill 说明。
- 已静态扫描 `Assets/Scripts/UI`、`Assets/Resources/Prefabs/New UI`、`Assets/Scenes`，确认 `RegionHost`、`RegionView`、`RegionMotion`、`ToggleRegion`、`InventoryUiHostBinding`、`InventoryListRegionView`、`InventoryPopupHostView`、`ShopListRegionView`、`ShopSidebarRegionHost`、`GamingHudRegionHost`、`GamingInputRegionHost`、`ShopPropertiesRegionView`、`ShopInventoryRegionView`、`SidebarRegionMotion`、`SidebarToggleRegionView` 不再残留于业务脚本和运行时资源。
- 已确认重命名后的脚本类名与文件名一致，`.meta` 通过 `git mv` 保留，Prefab 上的 MonoScript GUID 不应丢失。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 本轮按用户要求未执行完整 Unity Play Mode；商店侧栏、战斗 HUD、背包列表、背包操作 Popup 与暂停菜单内容面板仍需最终真实场景验收。

遗留风险：

- 这是命名和目录收口模块，行为目标是不变；仍需 Unity Editor 导入后确认没有脚本重命名导致的 Missing Script 或 Inspector 字段显示异常。
- 文档历史日志中仍可能有旧 Region 名称作为历史上下文；当前进度快照和下一轮入口以本条记录为准。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交 UI 子视图 Region 命名与目录收口。
- 继续最终收口，优先扫描 `Assets/Scripts/UI/Contracts` 与业务 UI Prefab / Scene 中是否还有无效抽象、旧 UI 资源引用或未迁入 Orange Catalog 的 Page / Popup / Modal / Tooltip；不要恢复 `Assets/Scripts/UI/Regions` 目录。

### 2026-05-06 阶段 12 最终收口：删除未使用 HUD Facade 抽象

完成内容：

- 删除 `IPlayerHudFacade`，该接口没有实现类、没有页面调用、没有 Prefab 或 Scene 引用，属于旧 HUD 解耦设想留下的未集成抽象。
- 重新记录 `UI/Contracts` 当前边界：保留页面 payload、`IInventoryUiFacade`、`IShopUiFacade`、`IInventoryFacadeContext`、`IInventoryUiFacadeHost`、Manager Facade 和背包快照，它们仍承担 GameManager / Manager 层到业务 UI 的真实数据边界。

删除范围：

- `Assets/Scripts/UI/Contracts/Facades/IPlayerHudFacade.cs`
- `Assets/Scripts/UI/Contracts/Facades/IPlayerHudFacade.cs.meta`

修改文件：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity 架构 / 脚本 Skill 说明。
- 已通过 `git grep` 扫描 `Assets/Scripts`、Prefab 和 Scene，确认 `IPlayerHudFacade` 除自身定义外没有任何引用。
- 已扫描 `Assets/Scripts/UI/Contracts`，确认 Inventory / Shop Facade、页面 Context、Snapshot 当前仍有业务调用链，本轮未删除。
- 本轮按用户要求未执行完整 Play Mode；这是死接口删除，不影响运行时 Prefab 装配。

遗留风险：

- `UI/Contracts` 仍需要最终命名和目录归属评估，但不能为了清理 Contract 字样删除真实跨系统边界。
- `InventoryUI` 在没有外部注入 Facade 时仍可通过显式 `InventoryOperateManager` 字段创建 `ManagerInventoryUiFacade`，这是独立背包 UI 的运行路径；如果最终要求所有页面子视图都走页面 payload 注入，需要单独迁移 Inspector 绑定。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交未使用 HUD Facade 抽象清理。
- 继续最终收口，优先检查业务 UI Prefab / Scene 是否仍引用旧 UI 资源或存在未迁入 Orange Catalog 的 Page / Popup / Modal / Tooltip；再评估 `UI/Contracts` 是否需要仅做目录命名收口，而不是删除有效边界。

### 2026-05-06 阶段 12 最终收口：收口 Contracts 目录命名

完成内容：

- 将原 `Assets/Scripts/UI/Contracts/Contexts` 移为 `Assets/Scripts/UI/Contexts`，保留 `GamingPageContext`、`ShopPageContext`、`PauseMenuContext`、`StageCompletePageContext`、`UIPageContextFactory`、`PageContextBinding` 等页面 payload 与装配工具。
- 将原 `Assets/Scripts/UI/Contracts/Facades` 移为 `Assets/Scripts/UI/Facades`，保留 `IInventoryUiFacade`、`IShopUiFacade`、`IInventoryFacadeContext`、`IInventoryUiFacadeHost`、`ManagerInventoryUiFacade`、`ManagerShopUiFacade` 等真实跨系统边界。
- 将原 `Assets/Scripts/UI/Contracts/Snapshots` 移为 `Assets/Scripts/UI/Snapshots`，保留 `InventoryUIItemSnapshot` 与 `InventoryItemOperateResource`。
- 删除空的 `Assets/Scripts/UI/Contracts.meta`，当前业务 UI 不再保留 `Contracts` 目录；这一步只收口目录语义，不改类名、不改 API、不删除有效边界。

移动范围：

- `Assets/Scripts/UI/Contracts/Contexts/**` -> `Assets/Scripts/UI/Contexts/**`
- `Assets/Scripts/UI/Contracts/Facades/**` -> `Assets/Scripts/UI/Facades/**`
- `Assets/Scripts/UI/Contracts/Snapshots/**` -> `Assets/Scripts/UI/Snapshots/**`

删除范围：

- `Assets/Scripts/UI/Contracts.meta`

修改文件：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并在删除 `IPlayerHudFacade` 后重新核查 `UI/Contracts` 的有效调用链。
- 已通过 `git mv` 移动目录和 `.meta`，保留脚本 GUID，降低 Unity 脚本引用丢失风险。
- 已静态扫描业务 UI 运行时脚本、New UI Prefab、Scene 和 Orange Catalog，确认当前全局 `PageBase`、`PopupBase`、`TooltipBase` 派生业务类型均已有 Orange Catalog 注册或作为页面内部子视图保留；本轮没有新增未登记全局视图。
- 已确认类型名和 API 未修改，因此业务脚本引用不需要 using 或命名空间调整。
- 本轮按用户要求未执行完整 Play Mode；Unity Editor 导入后仍需确认移动目录不会触发脚本引用异常。

遗留风险：

- 文档历史日志仍保留旧 `Assets/Scripts/UI/Contracts/**` 路径作为历史迁移记录；当前进度快照和本条日志之后应以 `UI/Contexts`、`UI/Facades`、`UI/Snapshots` 为准。
- `UI/Facades` 仍保留 Inventory / Shop Manager Facade，这是当前业务 UI 与 Manager 解耦边界，不应作为“桥接层”删除，除非后续把页面 payload 和 Manager 调用链整体重构。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交 Contracts 目录命名收口。
- 继续最终收口，优先处理业务 UI Prefab / Scene 和代码中仍存在的 `UIManager.Instance` 入口；如果目标是完全显式装配，需要为 Tooltip / Inventory Popup Host 设计页面注入链路，避免新增平行服务或兼容层。

### 2026-05-06 阶段 12 最终收口：显式注入子视图 UIManager

完成内容：

- `ViewHandle` 增加 `Owner`，由 `UIManager` 创建运行时 ViewHandle 时写入当前 Manager；泛型 `ViewHandle<TView>` 同步暴露 `Owner`。
- `ViewBase` 增加受保护的 `OwnerUIManager`，让页面能把自身所属 UIManager 传给页面内部子视图，而不需要子视图回读全局单例。
- `InventoryOperatePopupHost` 改为通过 `ConfigureUIManager()` 接收显式 UIManager，缺失时抛出明确装配错误，不再访问 `UIManager.Instance`。
- `InventoryUI` 与 `InventoryUiBinder` 支持把 UIManager 注入背包操作 Popup Host；`GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 在打开时传入 `OwnerUIManager`。
- `TooltipHoverTarget` 改为通过 `ConfigureUIManager()` 接收显式 UIManager，缺失时抛出明确装配错误，不再访问 `UIManager.Instance`。
- `BuffBarUI` 在生成 / 复用 Buff 图标时把页面注入的 UIManager 传给图标上的 `TooltipHoverTarget`；`GamingHudView` 负责把 `GamingUIPage.OwnerUIManager` 转交给 `BuffBarUI`。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewHandle.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewBase.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/UIManager.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryOperatePopupHost.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scripts/UI/Instances/Child/InventoryUiBinder.cs`
- `Assets/Scripts/UI/Instances/Child/TooltipHoverTarget.cs`
- `Assets/Scripts/UI/Instances/Child/BuffBarUI.cs`
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Scripts/UI/Pages/GamingHudView.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取相关脚本。
- 已静态扫描 `Assets/Scripts/UI` 与 `Assets/Scripts/OrangeUIFramework`，确认业务 UI 代码不再直接访问 `UIManager.Instance`；当前唯一剩余命中是框架 `UIManager` 自身保留的静态属性定义。
- 已确认 `BuffIconItem.prefab` 根节点挂载 `TooltipHoverTarget`，`BuffBarUI` 生成 Buff 图标后会把页面 UIManager 注入该组件。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 本轮按用户要求未执行完整 Play Mode；背包 Popup 打开、Tooltip 按下 / 移动 / 松开和页面关闭期间的竞态仍需最终真实场景验收。

遗留风险：

- `UIManager.Instance` 仍作为框架自身的可选全局入口保留，兼容已有框架入口语义；本轮目标是业务 UI 不再依赖它。
- 如果独立场景中直接启用 `InventoryUI` 而没有 Page 注入 UIManager，则打开背包操作 Popup 会明确报缺少 UIManager；当前运行时业务页面都会在打开时注入。
- `Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset` 以及两个 UIMotion Editor 文件当前仍有未提交工作树差异，不属于本模块，提交时必须排除或单独处理。

下一步：

- 提交子视图 UIManager 显式注入。
- 继续最终收口，优先排查剩余无关工作树差异和最终 Play Mode 前的静态风险；如 UIMotion Editor 差异确属编译修复，应单独成模块并更新文档。

### 2026-05-06 阶段 12 最终收口：修复 UIMotion 编辑器基类消歧

完成内容：

- `UIMotionDefinitionEditor` 从继承 `Editor` 改为显式继承 `UnityEditor.Editor`。
- `UIMotionPlayerEditor` 从继承 `Editor` 改为显式继承 `UnityEditor.Editor`。
- 该修复避免在 `Orange.UIFramework` 命名空间内由于存在 `Orange.UIFramework.Editor` 子命名空间，导致 `Editor` 被解析成命名空间而不是 Unity 编辑器基类。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Editor/UIMotionDefinitionEditor.cs`
- `Assets/Scripts/OrangeUIFramework/Editor/UIMotionPlayerEditor.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已静态检查 `Assets/Scripts/OrangeUIFramework/Editor`，确认当前存在 `Orange.UIFramework.Editor` 命名空间，因此显式 `UnityEditor.Editor` 是必要消歧。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；Unity Editor 导入后仍需确认两个自定义 Inspector 可正常打开。

遗留风险：

- 两个 Editor 文件仍位于 `namespace Orange.UIFramework`，不是本轮要做的命名空间重排；如后续希望统一 Editor 脚本命名空间，可单独迁移并验证自定义 Inspector。
- `Assets/Resources/DOTweenSettings.asset` 与 `ProjectSettings/ProjectSettings.asset` 当前仍有 Unity 自动生成 / 导入痕迹，不属于本模块，提交时必须排除。

下一步：

- 提交 UIMotion 编辑器基类消歧。
- 继续最终收口，当前工作树应只剩 Unity 自动导入痕迹；随后进入最终静态风险扫描和真实 Play Mode 验收准备。

### 2026-05-06 阶段 12 最终收口：删除页面 Context 空标记接口

完成内容：

- 删除无额外语义的 `IPageContext` 空标记接口，不再为页面 payload 保留旧 Contract 式抽象。
- `GamingPageContext`、`PauseMenuContext`、`ShopPageContext` 直接实现 `IDisposable`，继续负责释放由页面上下文持有的 Inventory / Shop Facade。
- `StageCompletePageContext` 改为纯 payload 类型，不再实现空 `Dispose()`。
- `PageContextBinding.Release()` 改为只约束标准 `IDisposable`，保留页面关闭时统一释放需要托管资源的上下文能力。

修改文件：

- `Assets/Scripts/UI/Contexts/GamingPageContext.cs`
- `Assets/Scripts/UI/Contexts/PauseMenuContext.cs`
- `Assets/Scripts/UI/Contexts/ShopPageContext.cs`
- `Assets/Scripts/UI/Contexts/StageCompletePageContext.cs`
- `Assets/Scripts/UI/Contexts/PageContextBinding.cs`
- `Assets/Scripts/UI/Contexts/IPageContext.cs`
- `Assets/Scripts/UI/Contexts/IPageContext.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程读取 Git 状态、本文和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity 项目侦察、脚本和资源 Skill 说明。
- 已通过 `git grep` 扫描 `Assets/Scripts`，确认 `IPageContext` 不再有运行时代码引用。
- 已静态核查 `GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 仍通过 `PageContextBinding.Release()` 释放持有 Facade 的页面上下文；`StageCompleteUIPage` 只读取快照 payload，不需要释放链路。
- 已执行 `git diff --check`，仅出现 Git 对 LF/CRLF 转换的提示，没有空白错误。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；页面打开 / 关闭后的 Facade 释放仍需最终真实场景验收。

遗留风险：

- `UI/Facades` 中 Inventory / Shop 相关接口和 Manager Facade 仍是当前 Manager 到业务 UI 的真实边界，本轮不删除。
- `Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset` 以及 Tabsil/Mineral 插件删除状态当前仍是无关工作树差异，不属于本模块，提交时必须排除。

下一步：

- 提交页面 Context 空标记接口清理。
- 继续最终静态风险扫描，重点确认业务 UI Prefab / Scene 中是否还有旧资源引用、Missing Script 风险或未说明的手工全局浮层入口；不要把无关 Unity 自动文件和第三方插件删除纳入 UI 迁移提交。

### 2026-05-06 阶段 12 最终收口：删除页面 Context 释放辅助类

完成内容：

- 在删除 `IPageContext` 后继续核查 `PageContextBinding`，确认它只剩 `Dispose + null` 转发逻辑，没有框架边界或业务抽象价值。
- `GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 在 `OnClosed()` 中直接 `Dispose()` 当前页面上下文并置空。
- 删除 `PageContextBinding.cs` 与 `.meta`，避免旧 Context 管理辅助类继续作为无效抽象保留。
- 保留 `GamingPageContext`、`PauseMenuContext`、`ShopPageContext` 对 Facade 生命周期的直接 `IDisposable` 实现，不改变页面 payload 装配和 UIManager 打开链路。

修改文件：

- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Scripts/UI/Contexts/PageContextBinding.cs`
- `Assets/Scripts/UI/Contexts/PageContextBinding.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已通过 `git grep` 确认 `PageContextBinding` 运行时代码引用只在三个页面和自身定义中出现，适合内联删除。
- 已静态核查三个页面的释放顺序保持不变：先解绑 UI / Controller / Inventory，再释放当前 Context。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；战斗 HUD、商店和暂停菜单关闭后的 Facade 释放仍需最终真实场景验收。

遗留风险：

- 文档历史日志中仍保留 `PageContextBinding` 作为历史迁移记录；当前进度快照和本条记录之后以已删除为准。
- `Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset` 以及 Tabsil/Mineral 插件删除状态当前仍是无关工作树差异，不属于本模块，提交时必须排除。

下一步：

- 提交页面 Context 释放辅助类清理。
- 继续最终静态风险扫描，重点确认业务 UI Prefab / Scene 中是否还有旧脚本 GUID、旧资源引用、Missing Script 风险或未说明的手工全局浮层入口。

### 2026-05-06 阶段 12 最终收口：删除背包 Facade Host 空标记接口

完成内容：

- 删除无额外语义的 `IInventoryUiFacadeHost` 空标记接口，不再让 `InventoryUI` 通过扫描父级组件判断自己是否处于页面宿主下。
- `GamingUIPage`、`ShopUIPage`、`GamePauseMenu` 不再实现该空接口；页面仍通过 `InventoryUiBinder.Bind()` 显式注入 `IInventoryUiFacade` 和所属 `UIManager`。
- `InventoryUI` 删除 `GetComponentsInParent<MonoBehaviour>()` 父级扫描和 `requiresExternalFacadeConfiguration` 状态，启动条件改为：已有外部配置 Facade 时启动；没有外部 Facade 但显式绑定了本地 `InventoryOperateManager` 时独立启动；两者都没有时等待外部配置。
- 保留 `IInventoryUiFacade`、`IInventoryFacadeContext`、`ManagerInventoryUiFacade` 作为真实 Manager 到 UI 的业务边界。

修改文件：

- `Assets/Scripts/UI/Instances/Child/InventoryUI.cs`
- `Assets/Scripts/UI/Instances/GamingUIPage.cs`
- `Assets/Scripts/UI/Instances/ShopUIPage.cs`
- `Assets/Scripts/UI/Instances/GamePauseMenu.cs`
- `Assets/Scripts/UI/Facades/IInventoryUiFacadeHost.cs`
- `Assets/Scripts/UI/Facades/IInventoryUiFacadeHost.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已通过 `git grep` 确认改动前 `IInventoryUiFacadeHost` 只在空接口、自身父级扫描和三个页面继承列表中出现；删除后 `Assets/Scripts` 下已无该接口运行时代码引用。
- 已静态扫描真实 Prefab / Scene 的 `InventoryUI.inventoryOperateManager` 绑定：`UI Shop.prefab` 中该字段为空，因此页面内 InventoryUI 会等待页面 payload 注入；`Game Scene` 中的命中是 `GameManager.inventoryOperateManager`，不是 InventoryUI 本地绑定。
- 已确认 `InventoryUI` 是 `UI Shop.prefab` 子级上的组件，当前没有其他业务 Prefab / Scene 直接挂载该脚本作为独立背包入口。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；商店 / 暂停 / 战斗页面背包启动时序仍需最终真实场景验收。

遗留风险：

- 若后续新增独立 `InventoryUI` Prefab，必须显式配置 `InventoryOperateManager` 或调用 `ConfigureFacade()`；没有父级空接口兜底后，缺依赖会等待外部配置而不是通过父级扫描推断。
- `Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset` 以及 Tabsil/Mineral 插件删除状态当前仍是无关工作树差异，不属于本模块，提交时必须排除。

下一步：

- 提交背包 Facade Host 空标记接口清理。
- 继续最终静态风险扫描，重点检查业务 UI Prefab / Scene 的旧脚本 GUID、旧资源引用、Missing Script 风险和剩余 UI 空接口；不处理非 UI 框架范围的 Manager 层场景查找。

### 2026-05-06 阶段 12 最终收口：删除业务容器未消费接口

完成内容：

- 核查 `IContainerQualityRender` 与 `IConfigurable<T>` 的调用链，确认它们只出现在容器基类实现声明中，没有任何代码按接口类型消费。
- 删除 `IContainerQualityRender.cs` 与 `.meta`。
- 删除 `UIContainerBase.cs` 文件末尾的 `IConfigurable<T>` 接口声明。
- `UIContainerBase<T, K>` 与 `InventoryOperatePopupBase` 不再实现上述未消费接口，但继续保留具体 `RenderQuality()`、`RenderItemQuality()`、`Configure()` 方法，现有子类和调用点不需要改调用方式。

修改文件：

- `Assets/Scripts/UI/Instances/Container/UIContainerBase.cs`
- `Assets/Scripts/UI/Instances/Container/InventoryOperatePopupBase.cs`
- `Assets/Scripts/UI/Instances/Container/IContainerQualityRender.cs`
- `Assets/Scripts/UI/Instances/Container/IContainerQualityRender.cs.meta`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程重新读取 Git 状态、本文、`ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`。
- 已通过 `git grep` 确认改动前 `IContainerQualityRender` 与 `IConfigurable<` 没有接口消费调用链，只存在实现声明和接口定义；删除后 `Assets/Scripts` 下已无这两个接口的运行时代码引用。
- 已确认 `Configure()` 调用点仍是具体类型调用，例如商店物品、升级卡、背包物品、品质预览和 Orange Popup 打开 payload；本轮不改变这些调用。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；容器渲染与 Popup payload 配置仍需最终真实场景验收。

遗留风险：

- `UIContainerBase` 和 `InventoryOperatePopupBase` 仍是业务 UI 容器基类，不属于 Orange 全局框架抽象；后续若要进一步拆分，需要围绕真实重复逻辑和 Prefab 继承关系单独评估。
- `Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset` 以及 Tabsil/Mineral 插件删除状态当前仍是无关工作树差异，不属于本模块，提交时必须排除。

下一步：

- 提交业务容器未消费接口清理。
- 继续最终静态风险扫描；若业务 UI 运行时代码已无旧托管 / 空接口 / 未迁入全局 View 残留，则更新文档记录静态收口点并准备最终真实场景验收。

### 2026-05-06 阶段 12 最终收口：完成业务 UI 静态收口扫描

完成内容：

- 按强制流程重新读取 Git 状态、本文和 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，并读取 Unity Script / Project Scout 技能说明，确认本轮只做最终静态收口记录，不改代码、不改 Prefab、不改场景。
- 扫描 `Assets/Scripts`、`Assets/Resources`、`Assets/Scenes`、`Assets/Prefabs`、`Packages`、`ProjectSettings` 中旧 UI 托管关键字，未发现真实旧 `AXR.Framework.UI`、旧 `UIPageBase`、旧 Catalog / Navigation / Type API、旧 `Region` / `Contract` 目录残留；`UIPageContextFactory` 命中只来自当前有效类名，不是旧 `IPageContext` 引用。
- 扫描真实资源中的 Missing Script，未发现 `m_Script: {fileID: 0}`。
- 扫描业务 `PageBase` / `PopupBase` / `TooltipBase` 派生类型并核对 `OrangeUIViewCatalog.asset`，确认当前 9 个业务 Page、2 个 Inventory Popup 和 1 个描述 Tooltip 已注册；测试桩不进入业务 Catalog。
- 扫描接口定义，确认剩余接口属于 Orange 框架扩展点或 Inventory / Shop 真实业务边界，没有发现新的空标记接口或无消费业务接口。
- 扫描 `UIManager.Instance`、`FindFirstObjectByType<UIManager>`、旧资源名和旧 `Assets/Scripts/Framework` 目录，确认业务 UI 不再直接读取全局 UIManager，旧 Framework 目录不存在；剩余 `FindFirstObjectByType` 命中位于非 UI 框架范围的业务 Manager、测试控制器或预览工具。

修改文件：

- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已执行 `git grep -n -E "AXR\\.Framework\\.UI|UIPageBase|UIPageOpenContext|UIPrefabCatalog|UIPrefabEntry|UILayerType|BeginTransition|FindFirstObjectByType<UIManager>|ActivePresenter|IPageContext|IInventoryUiFacadeHost|PageContextBinding|IContainerQualityRender|IConfigurable<" -- Assets/Scripts Assets/Resources Assets/Scenes Packages ProjectSettings`，结果只命中 `UIPageContextFactory` 子串，不是旧接口或旧托管残留。
- 已执行 `git grep -n "m_Script: {fileID: 0" -- Assets/Resources Assets/Scenes Assets/Prefabs`，未发现 Missing Script。
- 已执行 `git grep -n -E "class .*:.*(PageBase|PopupBase|TooltipBase|ModalBase)" -- Assets/Scripts/UI Assets/Scripts/OrangeUIFramework`，业务派生类型与 Catalog 注册关系符合预期。
- 已执行 `git grep -n -E "^[[:space:]]*(public |internal |private |protected )?interface " -- Assets/Scripts/UI Assets/Scripts/OrangeUIFramework`，剩余接口均有真实边界。
- 已执行 `git grep -n -E "UIManager\\.Instance|FindFirstObjectByType|FindAnyObjectByType|FindObjectOfType|Resources\\.Load<.*(UIManager|UIFrameworkSettings|ViewCatalog|UIPrefabCatalog)|AXR\\.Framework\\.UI" -- Assets/Scripts Assets/Resources Assets/Scenes Assets/Prefabs`，确认 UI 框架范围无旧入口残留。
- 已执行 `git diff --check -- ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`，仅有 LF/CRLF 转换提示，无空白错误。
- 本轮按用户要求未执行完整 Unity 编译和 Play Mode；当前结论是静态收口完成，不等同于真实场景验收通过。

遗留风险：

- 真实场景手动验证仍未执行，必须在当前 worktree 中打开 Unity 后验证主菜单、角色选择、战斗 HUD、暂停、商店 / 背包 Popup、Buff Tooltip、波次奖励、结算和游戏结束链路。
- 当前 worktree 仍有无关 Unity 自动文件和第三方插件删除状态：`Assets/Resources/DOTweenSettings.asset`、`ProjectSettings/ProjectSettings.asset`、`Assets/Tabsil/Mineral/Scripts/Editor/*.cs` 及其 `.meta`；本轮不纳入提交。
- 非 UI 框架范围的业务 Manager 仍存在 `FindFirstObjectByType` 等场景查找，这不是本轮 Orange UI 迁移收口目标；若后续要收口，需要按业务系统单独规划。

下一步：

- 提交阶段 12 静态收口记录。
- 进入真实场景验收准备，按开发文档 `23. 测试计划` 优先验证核心玩法路径，发现问题后按页面或功能模块拆小修复、更新文档并提交。
