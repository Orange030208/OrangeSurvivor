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

当前阶段：阶段 12 最终收口；`ShopUIPage`、`GamePauseMenu` 迁移期接入已完成，`GameManager` 已直接使用 Orange UIManager，`MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`GameOverUIPage`、`StageCompleteUIPage` 与 `WaveTransitionUIPage` 已直接继承新 `PageBase`。下一步继续让其余业务页面直接基于新框架运行，并清理旧 UI 托管、旧 Catalog、临时委托和无用抽象。

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
- 已删除旧 `SidebarRegionMotion` / `SidebarRegionMotionGroup` 中无实际效果的空 `ConfigureTimings()` API；业务私有 `GamePauseMenu.PauseMenuPanelBinding.ConfigureTimings()` 暂未迁移，留待业务页面迁移阶段处理。
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
- 已完成 `MenuUIPage` 第一模块迁移：采用迁移期桥接方式，让旧 `AXR.Framework.UI.UIPageBase` 继承新 `Orange.UIFramework.PageBase`，保留旧页面生命周期钩子和 `IUIPage` 兼容面。
- 已补迁 `CharacterSelectUIPage` 主流程漏项：`OrangeUIViewCatalog` 新增 `page.characterSelect`，Prefab 指向 `UI Character Selection.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；该页面是 `GameManager` 从菜单进入游戏前的角色选择必经页。
- 旧 `AXR.Framework.UI.UIManager` 已对注册进 Orange `ViewCatalog` 的页面委托新 `Orange.UIFramework.UIManager` 打开、关闭和查询；未注册页面继续走旧 UIManager，避免一次性迁移所有页面。
- 已在 `Game Scene` 的现有 `UIManager` GameObject 上挂载新 `Orange.UIFramework.UIManager`，并复用现有 Root Canvas。
- 已新增 `OrangeCanvasProfile`、`OrangeUIFrameworkSettings`、`OrangeUIViewCatalog` 资产；当前 Catalog 只注册 `UI Menu.prefab` / `MenuUIPage`，不把其他页面提前纳入新框架。
- 新 `UIManager` 已补充迁移期所需的非泛型 Page API：`OpenPageAsync(Type)`、`OpenPage(Type)`、`ClosePageAsync(Type)`、`IsOpen(Type)`；`ViewHandle` 已暴露非泛型 `View` 引用，供旧 UIManager 委托后返回 `IUIPage`。
- 已新增 `LegacyUIPageBase_CanOpenAndCloseThroughOrangeManagerTypeApi` EditMode 测试，覆盖旧 `UIPageBase` 经 Orange Manager 类型 API 打开 / 关闭的桥接路径。
- 已完成 `GamingUIPage` 第二模块迁移：`OrangeUIViewCatalog` 新增 `page.gaming`，Prefab 指向 `UI Gaming.prefab`，Layer 沿用旧 UI Catalog 的 `Hud` 层；旧 `GameManager` 中 `uiManager.OpenPage<GamingUIPage>(...)` 和 `transition.ClosePage<GamingUIPage>()` 会通过旧 UIManager 委托新 UIManager。
- 已完成 `ShopUIPage` 第三模块迁移：`OrangeUIViewCatalog` 新增 `page.shop`，Prefab 指向 `UI Shop.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；旧 `GameManager` 中 `uiManager.OpenPage<ShopUIPage>(...)` 和 `transition.ClosePage<ShopUIPage>()` 会通过旧 UIManager 委托新 UIManager。
- 已完成 `GamePauseMenu` 第四模块迁移：`OrangeUIViewCatalog` 新增 `page.pause`，Prefab 指向 `UI Pause.prefab`，ViewKind 仍为 Page，Layer 沿用旧 UI Catalog 的 `Popup` 层；旧 `GameManager` 中 `uiManager.OpenPage<GamePauseMenu>(...)`、`transition.ClosePage<GamePauseMenu>()` 和 `uiManager.IsPageOpen<GamePauseMenu>()` 会通过旧 UIManager 委托新 UIManager。
- 已完成 `GameOverUIPage` 第五模块迁移：`OrangeUIViewCatalog` 新增 `page.gameOver`，Prefab 指向 `UI Game Over.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；旧 `GameManager` 中 `uiManager.OpenPage<GameOverUIPage>()` 和 `transition.ClosePage<GameOverUIPage>()` 会通过旧 UIManager 委托新 UIManager。
- 已完成 `StageCompleteUIPage` 第六模块迁移：原项目缺少对应 Prefab 和旧 Catalog 注册，本轮补齐 `UI Stage Complete.prefab`，`OrangeUIViewCatalog` 新增 `page.stageComplete`，Layer 使用 `Default/Page`；旧 `GameManager` 中 `uiManager.OpenPage<StageCompleteUIPage>()` 和 `transition.ClosePage<StageCompleteUIPage>()` 会通过旧 UIManager 委托新 UIManager。
- 已完成 `WaveTransitionUIPage` 第七模块迁移：`OrangeUIViewCatalog` 新增 `page.waveTransition`，Prefab 指向 `UI Wave Transition.prefab`，Layer 沿用旧 UI Catalog 的 `Default/Page` 层；旧 `GameManager` 中 `uiManager.OpenPage<WaveTransitionUIPage>()` 和 `transition.ClosePage<WaveTransitionUIPage>()` 会通过旧 UIManager 委托新 UIManager。
- 已新增真实 `OrangeUIViewCatalog.asset` 校验测试，确认 `MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`ShopUIPage`、`GamePauseMenu`、`GameOverUIPage`、`StageCompleteUIPage` 与 `WaveTransitionUIPage` 均可按类型解析并通过 Catalog 校验。
- 已完成最终收口第一步：`Assets/Scripts/Managers/GameManager.cs` 直接引用 `Orange.UIFramework.UIManager`，页面打开 / 关闭改为 `OpenPageAsync<T>()` 与 `ClosePageAsync(Type)`，状态切换和暂停菜单关闭流程使用 UniTask 顺序等待，并用本地 transition version 避免旧异步结果覆盖新状态。
- 已完成 `MenuUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画；设置侧栏仍沿用旧 `IUIRuntimeMotion` 动画接口，但页面托管已不再依赖旧 `UIPageBase`。
- 已完成 `CharacterSelectUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `GamingUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，旧 `OnPageTick()` 改为 `RequiresTick` + `OnTick()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `GameOverUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `StageCompleteUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 继续复用原 `UISequenceDirector` 入退场动画。
- 已完成 `WaveTransitionUIPage` 直接基类迁移：脚本改为继承 `Orange.UIFramework.PageBase`，旧 `OnPageOpened()` / `OnPageClosed()` 生命周期迁到 `OnOpeningAsync()` / `OnClosed()`，对应 Prefab 增加 `UIMotionTransition` 指向原 `UISequenceDirector`。

未完成：

- 业务迁移前真实场景手动验证清单仍未执行；当前是按用户明确要求跳过门禁后先推进业务迁移，Overlay / Camera 真机运行、真实 Prefab、CanvasScaler、输入模块、DOTween 实际播放和 Inspector 诊断按钮仍需 PlayMode 或手动验证。
- 尚未实现独立 PlayMode 测试场景；是否补最小 PlayMode 场景可在下一轮根据清单执行成本决定，但不能替代真实场景手动验证。
- 除 `MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`GameOverUIPage`、`StageCompleteUIPage` 与 `WaveTransitionUIPage` 外，其余业务页面目前仍继承迁移期旧 `UIPageBase`；旧 `AXR.Framework.UI.UIManager` 仍在代码库中，但已不再作为 `GameManager` 业务入口。页面基类和旧 UIManager 清理尚未完成，尚未达到用户要求的最终形态。
- 尚未清理旧 `AXR.Framework.UI.UIManager`、旧 `UIPrefabCatalog.asset`、旧页面托管、临时非泛型委托、旧 Region / Contract 无用抽象与旧资源引用。

当前风险：

- 后续实现周期长，必须依赖本文持续记录，否则上下文压缩后容易误迁移旧 UI 或重建无关抽象。
- 框架核心已具备迁移闭环，但真实场景手动验证门禁尚未执行；用户已明确要求先开始迁移，因此当前迁移依赖 EditMode 测试和保守桥接降低风险，后续仍需尽快补真实场景验证。
- 当前迁移策略的桥接范围已收窄：`GameManager` 已直连新 `UIManager`，`MenuUIPage`、`CharacterSelectUIPage`、`GamingUIPage`、`GameOverUIPage`、`StageCompleteUIPage` 与 `WaveTransitionUIPage` 已直接继承新 `PageBase`，但旧 `UIPageBase` 仍暂继承新 `PageBase`，其余业务页面代码暂未直接继承新基类。该桥接只是迁移脚手架，不是最终交付形态；下一阶段必须继续让业务 UI 直接基于 OrangeUIFramework，并清理旧 `AXR.Framework.UI` 托管、旧 Catalog、临时委托和无用资源。
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
3. 确认 `GamingUIPage` 直接基类迁移提交已存在，并检查是否只剩 Unity 导入痕迹或下一步最终收口相关变更。
4. 继续最终收口第二步：迁移下一个低风险业务页面直接继承 `Orange.UIFramework.PageBase`，移除页面脚本对 `AXR.Framework.UI.UIPageBase`、`UIPageOpenContext`、旧 `OnPageOpened()` / `OnPageClosed()` 钩子的依赖，并确保 Prefab 显式挂载 `UIMotionTransition` 继续复用现有动画。
5. 当前阶段已由用户授权跳过真实场景手动验证门禁，但每轮仍必须记录该风险；最终收口完成后必须做一次真实 Play Mode 验收，目标是打开游戏即可直接测试。
6. 每完成一个最终收口模块，必须更新 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 和本文，再执行匹配验证并提交。
7. 验证必须使用当前 worktree：`C:\Users\AXR\.codex\worktrees\f02c\Survivors`。UnitySkills 当前连接主工作区时不能直接用于认定 worktree 结果。
8. 使用 Unity batchmode 验证 worktree 时不要传 `-quit`。
9. 最终收口必须按可回退边界拆分：先业务入口直连新 `Orange.UIFramework.UIManager`，再业务页面直接继承新框架基类，最后删除旧 UIManager / 旧 Catalog / 临时委托 / 无用 Region 与 Contract 抽象，确保打开游戏即可基于新 UI 框架测试。

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
- 删除旧 `SidebarRegionMotion` 与 `SidebarRegionMotionGroup` 的空 `ConfigureTimings()` API，避免继续暴露无效抽象。

修改文件：

- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/IViewTransition.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/DOTweenUniTaskExtensions.cs`
- `Assets/Scripts/OrangeUIFramework/Motions/Runtime/UIMotionTransition.cs`
- `Assets/Scripts/OrangeUIFramework/Core/Runtime/ViewBase.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/IUISequenceMotion.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/UISequenceDirector.cs`
- `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/V2/UIMotionPlayer.cs`
- `Assets/Scripts/UI/Regions/SidebarRegionMotion.cs`
- `Assets/Scripts/UI/Regions/SidebarRegionMotionGroup.cs`
- `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md`
- `ORANGE_UI_FRAMEWORK_IMPLEMENTATION_PLAN.md`

验证情况：

- 已按本轮强制流程执行 `git status --short --branch`，确认处于 `codex/orange-ui-framework-plan` worktree。
- 已读取本文当前进度、下一轮入口和阶段 6 目标，并读取 `ORANGE_UI_FRAMEWORK_DEVELOPMENT.md` 的 `IViewTransition`、UIMotion、`refreshDefaultsOnEnable`、Timing API 章节。
- 已读取旧 `UIMotionPlayer`、`UISequenceDirector`、`IUIRuntimeMotion`、`IUISequenceMotion`、Motion Track 与快照注册表，确认采用最小适配而非复制整套旧动画系统。
- 已检查 `IUISequenceMotion` 实现点只有旧 `UIMotionPlayer` 和 `UISequenceDirector`，新增 `RefreshDefaults()` 不会漏实现。
- 已确认旧 `SidebarRegionMotion` / `SidebarRegionMotionGroup` 的 `ConfigureTimings()` 已无外部调用；仍存在的 `GamePauseMenu.PauseMenuPanelBinding.ConfigureTimings()` 是业务私有空方法，留待业务迁移阶段处理。
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
- 读取并确认 `GamingUIPage` 仍继承旧 `UIPageBase`，依赖 `GamingPageContext`、`GamingHudRegionHost`、`GamingInputRegionHost`、`InventoryUiHostBinding`、`UITooltipPresenter` 和移动摇杆等战斗 HUD 业务组件；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
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
- 读取并确认 `ShopUIPage` 仍继承旧 `UIPageBase`，依赖 `ShopPageContext`、`ShopPageController`、`ShopListRegionView`、`ShopSidebarRegionHost`、`InventoryUiHostBinding` 和背包 / 属性侧栏等商店业务组件；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
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
- 读取并确认 `GamePauseMenu` 仍继承旧 `UIPageBase`，依赖 `PauseMenuContext`、`InventoryUiHostBinding`、`GameEventBus`、`PauseMenuPanelBinding` 和旧 `IUIRuntimeMotion` 侧栏动画；本轮不改业务脚本，继续通过旧页面基类桥接新 `PageBase`。
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
