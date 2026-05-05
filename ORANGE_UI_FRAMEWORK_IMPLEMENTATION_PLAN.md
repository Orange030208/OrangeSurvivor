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
2. `GamingUIPage`
3. `ShopUIPage`
4. `GamePauseMenu`
5. `GameOverUIPage`
6. `StageCompleteUIPage`
7. `WaveTransitionUIPage`

要求：

- 不把旧 `Regions` 目录整体搬进框架。
- 商店、背包等业务内部可保留子视图类，但按 `ViewPart` 简化。
- 每迁移一个页面，必须保留可回退提交。

## 6. 当前进度快照

当前阶段：阶段 5，异步防重入与 request version 已完成，准备进入阶段 6。

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

未完成：

- 尚未接入动画等待、Popup / Modal / Tooltip 管理、定位裁剪、本地化与测试。
- `UIManager` 的 Popup / Modal / Tooltip API 目前仍显式抛出阶段性未实现异常，需阶段 7 完成。

当前风险：

- 后续实现周期长，必须依赖本文持续记录，否则上下文压缩后容易误迁移旧 UI 或重建无关抽象。
- 业务迁移必须等待框架核心完成，否则会让旧问题带入新框架。
- 当前环境未生成 Unity `.csproj`，本轮只能做文件级和命名级检查，完整编译仍需 Unity Editor 刷新后验证。
- Stage 4 的同步兼容 `OpenPage()` 只适合已同步完成的旧式调用；默认新业务仍应使用 UniTask 异步 API。
- Stage 5 只处理 Page 操作防重入；Popup 分组互斥、Modal 结果互斥和 Tooltip 唯一实例仍需阶段 7 分别实现。

## 7. 下一轮入口

下一轮必须先做：

1. 读取本文 `当前进度快照` 和 `详细进度日志`。
2. 确认阶段 5 提交已存在。
3. 从阶段 6 开始，沿用现有 UIMotion 系统，接入 UniTask 等待适配，并修复 `refreshDefaultsOnEnable` 池化复用后动画起点不准的问题。
4. 不迁移任何现有业务页面。
5. 不修改旧 UIManager 业务调用，除非后续迁移阶段明确需要。
6. 阶段 6 重点读取旧 `Assets/Scripts/Framework/UI/Core/Runtime/UIMotion/`，先判断复制、适配还是最小迁移；不得为了动画接入引入平行 UI 服务。

下一轮禁止：

- 禁止直接迁移 `ShopUIPage`、`GamingUIPage` 等业务页面。
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
