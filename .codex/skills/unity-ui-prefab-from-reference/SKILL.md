---
name: unity-ui-prefab-from-reference
description: "根据参考图和已提供的 UI 资源构建或调整 Unity UGUI/Canvas 预制体，并把预制体临时挂到真实场景 Canvas 下截图验证，再按截图迭代。适用于让 Codex 根据概念图、截图、精灵目录、字体、图标、边框等创建、重做、对齐或微调 Unity UI 预制体，也适用于要求在 Canvas 中截图对比预制体的任务。"
---

# Unity UI Prefab From Reference

## Purpose

Use this skill to turn a UI reference image plus project UI assets into a Unity UGUI prefab. The workflow protects logic bindings while using screenshot feedback from the real scene Canvas to converge on the visual layout.

This skill is for UGUI/Canvas prefabs only. Do not use it for UI Toolkit.

## Core Rules

- Read the reference image, target prefab, material/icon/font paths, and the intended scene Canvas before editing.
- Preserve logic components, `Button.onClick`, serialized field references, settings panels, motion bindings, catalog registration, and nested prefab links unless the user explicitly asks to change them.
- Use the user's supplied sprites, fonts, and icon atlases as read-only inputs. Do not edit source images or `.meta` slicing unless the task is explicitly about asset slicing/import settings.
- If the user specifies a TMP font asset, assign it to every relevant `TextMeshProUGUI` text and use its own material reference.
- Treat a temporary Canvas as diagnostic only. The final visual proof must mount the prefab under an existing scene Canvas.
- Clean temporary GameObjects, Editor scripts, screenshots under `Assets/`, and screenshot `.meta` files before completion. Keep optional screenshots under `Temp/CodexScreenshots/`.

## Workflow

1. **Ground the task**
   - Inspect the reference image and list the intended visual regions: background, frames, logo/title, button groups, icon slots, labels, panels, and decorative slots.
   - Inspect target prefab hierarchy and components. Identify what must not move or be deleted because code or motion references it.
   - Inspect candidate assets with `rg --files`, `.meta` GUIDs, and image viewing. Prefer existing project patterns over inventing new structure.
   - Ask only for product preferences that cannot be discovered, such as whether to replace the runtime catalog entry or which visual variant to prefer.

2. **Build or adjust the prefab**
   - Clone an existing prefab only when the user asks for a new runtime entry; otherwise edit the named target prefab.
   - Keep stable named objects that scripts reference. Add visual children or sibling groups for new art instead of replacing logical roots.
   - Use `apply_patch` for manual YAML edits. Use Unity Editor automation when component wiring or prefab serialization would be fragile by hand.
   - For icon atlases, use existing slices by name/fileID when available. If color is missing, use a white slice and tint the `Image`.
   - Keep placeholder decoration slots when the user says decoration will be filled later.

3. **Screenshot loop**
   - Inspect the host Canvas before the final screenshot: `renderMode`, `worldCamera`, `CanvasScaler.referenceResolution`, `matchWidthOrHeight`, and active camera.
   - Temporarily instantiate the prefab under the existing scene Canvas with a unique temp name, then capture with the Canvas camera for `ScreenSpaceCamera` or `WorldSpace` canvases.
   - Use `scripts/capture_existing_canvas_preview.py` when UnitySkills is available. `scene_screenshot` is only auxiliary because it can capture the wrong Game View context.
   - Open the screenshot and compare against the reference for scale, position, hierarchy, text fitting, font, icon visibility, frame coverage, background, and interaction hit areas.
   - Adjust the prefab and repeat until the screenshot is visually close and there is no obvious clipping, overlap, missing icon, wrong font, or Canvas scale mismatch.

4. **Finish**
   - Remove temporary preview instances and scripts. Verify no temp object remains in the scene.
   - Move useful screenshots to `Temp/CodexScreenshots/` and remove `Assets/Screenshots*`.
   - Run `git status --short` for the target prefab and skill/script paths; report only expected changes.
   - Run automated tests only when logic, catalog entries, scene assets, or test files changed. Pure visual prefab work normally uses screenshot verification instead.

## Screenshot Helper

Use:

```powershell
python .codex\skills\unity-ui-prefab-from-reference\scripts\capture_existing_canvas_preview.py `
  --prefab Assets/GameContent/UI/Prefabs/Pages/UI Menu Cyber.prefab `
  --canvas Canvas `
  --camera "Main Camera" `
  --output Temp/CodexScreenshots/CyberMainMenuExistingCanvas.png
```

Optional:

```powershell
--temp-name __CodexUiPrefabPreview
--width 1920
--height 1080
--keep-assets-screenshot
```

The helper uses UnitySkills REST to instantiate the prefab under the existing Canvas, capture with the named camera, delete the temporary instance, copy the screenshot to `Temp/CodexScreenshots/`, remove `Assets/Screenshots*` by default, refresh the AssetDatabase, and print JSON.

## Acceptance Checklist

- The target prefab uses the requested assets and fonts.
- The target prefab still has required logic components and serialized references.
- The final proof screenshot was captured from the real scene Canvas, not only a temporary Canvas.
- The temporary preview instance no longer exists in the scene.
- `Assets/Screenshots` and `Assets/Screenshots.meta` are absent unless the user explicitly asked to keep them.
- The final response lists affected files, concrete visual changes, impact scope, screenshot/verification status, and the most useful next step.
