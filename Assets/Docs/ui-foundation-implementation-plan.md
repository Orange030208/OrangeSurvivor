# AXR UIFoundation Implementation Plan

## 1. Purpose

This document is the execution plan for implementing `AXR.UIFoundation`.

The source of truth for framework behavior is `Assets/Docs/universal-ui-framework-development-guide.md`. This implementation plan defines how to build it safely across a long task, how to preserve working memory, and how to keep rollback points clean.

## 2. Mandatory Working Protocol

Every implementation turn must follow this protocol before editing files:

1. Read `Assets/Docs/universal-ui-framework-development-guide.md`.
2. Read `Assets/Docs/ui-foundation-implementation-plan.md`.
3. Read `Assets/Docs/ui-foundation-implementation-tracker.md`.
4. Inspect `git status --short`.
5. Identify the current phase, completed work, latest test result, known blockers, and the next concrete task.
6. State the next action in the conversation before editing files.

Every implementation turn must follow this protocol before ending:

1. Update `Assets/Docs/ui-foundation-implementation-tracker.md`.
2. Record files changed, public APIs added or changed, tests run, test results, blockers, and the next task.
3. Keep the tracker top section `Current Resume State` accurate and short enough to survive context compression.
4. If a coherent module or milestone is complete and tests pass, create a git commit immediately.

This protocol is mandatory. Do not rely on chat history as the only memory store.

## 3. Git And Rollback Policy

Commit frequently. A commit is required after each coherent milestone:

- Documentation baseline.
- Core types and settings compile.
- Scope/Canvas/Layer compile.
- Surface lifecycle compile.
- Each major service family compiles.
- Localization compile.
- Diagnostics and editor validators compile.
- Each migration sample works.

Commit messages use this format:

```text
Add UIFoundation <milestone>
```

Before each commit:

- Run the fastest available compile or test check.
- Update the tracker with the test result.
- Verify `git status --short` only contains intended files.

Rollback principle:

- Never rewrite unrelated user changes.
- If a milestone fails badly, revert only the latest UIFoundation milestone commit or make a corrective commit.
- Do not mix migration changes with framework foundation commits.

## 4. High-Level Build Order

The project migration must not start until the new framework is structurally complete.

Framework-first order:

1. Documentation and memory-management baseline.
2. Core value types, enums, exceptions, and options.
3. Settings ScriptableObjects and validation helpers.
4. Scope, Canvas, Layer, Bootstrap, and EventSystem creation.
5. Surface view contracts, registry, loader, factory, pooling, Handle versioning.
6. Operation scheduler and lifecycle pipeline.
7. Animation service and existing UIMotion/UISequenceDirector adapters.
8. Page service.
9. Modal service and mask service.
10. Positioning service.
11. Popup service and outside-click service.
12. Tooltip service.
13. Panel/Slot service.
14. Widget factory and virtualized list foundation.
15. Toast service.
16. Input focus and Back handling.
17. Transaction service and route guards.
18. Localization and accessibility.
19. Diagnostics snapshot, overlay, and operation log.
20. Interaction recorder.
21. Editor validators and preview/debug windows.
22. Legacy adapter.
23. Framework test pass.
24. Only after all above: project migration samples.

## 5. Phase Detail

### Phase 0: Baseline Documentation Commit

Deliverables:

- `universal-ui-framework-development-guide.md`
- `ui-foundation-implementation-plan.md`
- `ui-foundation-implementation-tracker.md`

Validation:

- Confirm docs exist.
- Confirm no Theme/skin requirement remains.
- Commit the docs as the starting rollback point.

### Phase 1: Core Types And Settings

Deliverables:

- `AXR.UIFoundation` namespace.
- `UIScopeId`, `UISurfaceKey`, `UISurfaceId`, `UIHandle`, `UIHandle<TView>`.
- Surface enums, layer enums, policies, operation enums.
- `UIFoundationSettings` and child settings assets.
- Configuration exception types.

Validation:

- Unity compile check.
- EditMode tests for identity equality, empty value validation, and default settings.

Commit after compile.

### Phase 2: Scope, Canvas, Layer, Bootstrap

Deliverables:

- `UIFoundationBootstrap`.
- `IUIScopeService`, `IUIScope`, `UIScopeService`, `UIScope`.
- `IUICanvasHost`, `UICanvasHost`.
- `IUILayerService`, `UILayerService`.
- Root Canvas, SafeArea, layer roots, EventSystem creation.

Validation:

- Overlay Scope creation.
- Camera Scope fails when camera missing.
- Layer order snapshot test.

Commit after compile and tests.

### Phase 3: Surface Lifecycle Foundation

Deliverables:

- `IUISurfaceView`, `IUIBindable<T>`, `IUIAsyncPrepare<T>`, `IUICloseGuard`, `IUIResultProvider<T>`, `IUIReuseHandler`.
- `UISurfaceCatalog`, `UISurfaceEntry`, loader interfaces.
- `DirectReferenceSurfaceLoader`, `ResourcesSurfaceLoader`.
- `UISurfaceRegistry`, `UISurfaceInstance`, `UISurfaceFactory`, pooling.
- Handle version invalidation.

Validation:

- Catalog duplicate-key test.
- Spawn/recycle test.
- Invalid handle test.

Commit after compile and tests.

### Phase 4: Operations, Animation, And Lifecycle Pipeline

Deliverables:

- `IUIOperationScheduler`.
- Open/close operation lifecycle.
- Cancellation behavior.
- `IUIAnimationService`.
- CanvasGroup fallback adapter.
- Existing `UIMotionPlayer` and `UISequenceDirector` adapters.

Validation:

- Open waits for Show.
- Close waits for Hide.
- Close cancels opening.
- Reduced Motion path.

Commit after compile and tests.

### Phase 5: Surface Services

Deliverables:

- `IUIService` aggregate.
- `IUIPageService`, `IUIModalService`, `IUIPopupService`, `IUITooltipService`, `IUIPanelService`, `IUIWidgetFactory`, `IUIToastService`.
- Page stack, Modal stack, Popup owner index, Tooltip singleton, Panel slot tree, Toast queue.

Validation:

- Page open/replace/reset tests.
- Modal result test.
- Popup owner close test.
- Tooltip singleton refresh test.
- Panel slot replace/stack tests.
- Toast merge/priority tests.

Commit after compile and tests.

### Phase 6: Positioning And Input

Deliverables:

- `UIPositioningService`.
- Placement models for screen point, RectTransform, Transform, world position.
- BestFit, SafeArea clamp, follow scheduler.
- Focus service, Mask service, OutsideClick service, Back input handling.

Validation:

- Overlay placement test.
- Camera placement test.
- Outside-click close test.
- Modal input-block test.

Commit after compile and tests.

### Phase 7: Localization And Accessibility

Deliverables:

- `IUILocalizationService`.
- Locale settings, string tables, sprite tables, font tables.
- Named-argument formatting and fallback chain.
- `UILocalizedText`, `UILocalizedImage`, `IUILocalizationTarget`.
- Font scale, reduced motion integration, screen-reader labels.

Validation:

- Locale switch updates opened surfaces.
- Missing key diagnostics.
- Named parameter formatting.
- RTL mirror flag behavior.

Commit after compile and tests.

### Phase 8: Advanced Runtime

Deliverables:

- UI transaction service.
- Route guards.
- Diagnostics snapshot, log, overlay.
- Interaction recorder.

Validation:

- Transaction rollback.
- Route guard allow/deny/redirect/delay.
- Diagnostics snapshot contains stacks and operations.
- Recorder captures and replays basic operations.

Commit after compile and tests.

### Phase 9: Editor Tooling

Deliverables:

- Settings validator.
- Catalog validator.
- Prefab validator.
- Layer preview.
- Runtime debugger.
- Route graph window.
- Localization preview.
- Animation profile preview.

Validation:

- Editor compile.
- Validator tests for missing prefab, duplicate key, missing localization key, missing camera.

Commit after compile and tests.

### Phase 10: Legacy Adapter

Deliverables:

- Adapter entry points for old framework compatibility.
- Reuse existing UIMotion assets as animation backend.
- No behavior change to old framework by default.

Validation:

- Old UI code still compiles.
- Adapter can open a simple Page through new service.

Commit after compile and tests.

### Phase 11: Migration Samples

Migration starts only after the full framework is compiled, tested, and committed.

Order:

1. Tooltip.
2. Inventory item Popup.
3. Confirm Modal.
4. Shop Panels.
5. Page flow.

Validation:

- No static Tooltip Presenter.
- No business-owned Popup mask.
- No direct business Instantiate/Destroy for migrated framework surfaces.

Commit each migration sample separately.

## 6. Tracker Requirements

The tracker must always contain:

- `Current Resume State`
- `Milestone Checklist`
- `Latest Git State`
- `Completed Work Log`
- `Current Public API`
- `Known Decisions`
- `Known Blockers`
- `Test History`
- `Next Step`

The tracker must be updated in every implementation turn, even when no code change is completed.

## 7. Recovery Instructions After Context Compression

When context is compressed, the next agent must:

1. Read the top `Current Resume State` in the tracker.
2. Read the current milestone section in this plan.
3. Inspect the files listed in the latest tracker entry.
4. Run `git status --short`.
5. Continue only from the recorded `Next Step`.

If the tracker conflicts with repository reality, repository reality wins, and the tracker must be corrected before implementation continues.
