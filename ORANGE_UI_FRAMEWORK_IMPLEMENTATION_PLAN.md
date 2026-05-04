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

状态：进行中。

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

当前阶段：阶段 0，规划与准备。

已完成：

- 主分支 UniTask 包已更新到当前工作树。
- 已创建工作分支 `codex/orange-ui-framework-plan`。
- 已完成 OrangeUIFramework 设计文档初稿。
- 已明确保留 `UIManager` 作为运行时总入口。
- 已明确示例代码直接引用 `UIManager`。
- 已明确加入异步防重入、FloatingViewPositioner、运行时诊断、UIMotion refresh defaults 修复。

未完成：

- 尚未实现任何框架代码。
- 尚未创建 `Assets/Scripts/OrangeUIFramework/`。
- 尚未提交本轮规划文档。

当前风险：

- 后续实现周期长，必须依赖本文持续记录，否则上下文压缩后容易误迁移旧 UI 或重建无关抽象。
- 业务迁移必须等待框架核心完成，否则会让旧问题带入新框架。

## 7. 下一轮入口

下一轮必须先做：

1. 读取本文 `当前进度快照` 和 `详细进度日志`。
2. 确认规划提交已存在。
3. 从阶段 1 开始，创建 `Assets/Scripts/OrangeUIFramework/` 目录与基础类型。
4. 不迁移任何现有业务页面。
5. 不修改旧 UIManager 业务调用，除非是阶段 3 明确迁移框架入口时需要。

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
