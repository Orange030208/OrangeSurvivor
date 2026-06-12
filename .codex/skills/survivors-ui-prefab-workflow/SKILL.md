---
name: survivors-ui-prefab-workflow
description: "Survivors project workflow for building or revising Unity UGUI or Canvas prefabs under Assets/GameContent/UI/Prefabs from concept art, screenshots, existing assets, or existing prefabs. Use when the user asks to make, remake, align, polish, or verify a UI prefab in this project and cares about preserving logic bindings, reusing local motion patterns, setting intentional anchors and pivots for resolution changes, avoiding unnecessary decorative elements, cleaning temporary files, and validating under a real scene Canvas when possible. Triggers include UI prefab work, pause screen work, anchor or pivot fixes, concept-art-driven UI, and requests to mimic an existing button motion pattern."
---

# Survivors UI Prefab Workflow

## Overview

Use this workflow to turn reference images and existing project assets into maintainable Survivors UGUI prefabs. Keep logic stable, prefer project patterns, and leave the prefab ready for resolution changes rather than only matching one screenshot.

## Ground The Task

- Read the reference image, target prefab, required asset paths, and the scene Canvas that should host final verification.
- Inspect the target prefab hierarchy and serialized components first. Identify fields that must survive unchanged: button references, `Button.onClick`, motion components, popup managers, catalog bindings, nested prefab links, and script-owned roots.
- Inspect only nearby assets: the target prefab, analogous prefabs, referenced sprites/fonts/materials, and any explicitly named source prefab such as `IconFrameButton.prefab`. Do not recurse the whole project just to hunt context.
- Split visual elements into functional and decorative. If the user says decorative background icons are unnecessary, drop them instead of re-creating them.

## Build The Prefab

- Edit the named prefab in place unless the user explicitly wants a new runtime entry.
- Keep stable logical roots. Add or replace visual children under those roots instead of rewriting script-owned structure without a reason.
- Reuse existing project sprites, TMP fonts, materials, motion assets, and prefab patterns before inventing new assets or abstractions.
- Treat source images and `.meta` slicing as read-only unless the task is explicitly about import settings or slicing.
- Use `apply_patch` for manual prefab YAML edits. Use Unity automation when available if component wiring would be fragile by hand.
- If the task requires heavy Inspector installation, `SerializeReference` setup, or manual scene wiring that is risky to automate, stop short of hacks and ask the user to finish that binding in the Editor.

## Reuse Motion And Interaction

- When the user says a button should mimic an existing prefab, inspect that prefab and mirror the full motion stack, not just the visual look: `Button` or `Selectable` settings, `UIMotionPlayer`, `UIMotionTrigger`, bound motion definition asset, target bindings, and trigger clip mapping.
- Reuse the existing motion definition asset when behavior should match exactly. Only create or swap motion assets when the user asks for a different feel.
- Keep hit areas aligned with the visible frame. Do not make decorative images the click target unless that is intentional.

## Set Anchors And Pivots Intentionally

- Set anchors and pivots from layout intent, not from habit.
- Stretch fullscreen roots to the parent Canvas instead of leaving them center-fixed.
- Anchor side-docked panels and buttons to the corresponding edge.
- Anchor top or bottom hero elements to top-center or bottom-center when they should stay glued to those edges across resolutions.
- Choose pivots that match animation and scaling direction. A top banner usually wants a top-center pivot; a left-docked button usually wants a left-side anchor with a pivot that scales cleanly from the frame center.
- Avoid leaving every child at the default center anchor and pivot combination. Resolution changes should not pull the whole layout off intent.
- Use `AspectRatioFitter`, layout groups, or content size fitters only when the runtime behavior is intended and predictable.

## Apply Visual Rules

- Prefer tinting existing white or line-art sprites over generating quick replacement assets.
- If a required icon is missing, prefer an existing local icon, sprite slice, or a clean TMP glyph or text mark before generating a new temporary asset.
- Keep the screen readable before making it busy. Decorative layers are optional; functional hierarchy is not.
- Match typography and color treatment to the local UI family in `Assets/GameContent/UI`.

## Verify The Result

- Preferred path: mount the prefab under the real scene Canvas and capture it with the actual Canvas camera.
- Use `.codex/skills/unity-ui-prefab-from-reference/scripts/capture_existing_canvas_preview.py` when UnitySkills REST is available.
- Inspect `renderMode`, `worldCamera`, and `CanvasScaler` before trusting a screenshot.
- Compare against the reference for structure, alignment, text fit, icon visibility, frame coverage, and touch or click area placement.
- If Unity automation or REST is unavailable, do a file-level fallback check: verify internal prefab `fileID` references, inspect the Unity Editor log for import errors, and state clearly that real-Canvas screenshot verification is blocked.

## Clean Up

- Delete temporary preview instances, temporary Editor scripts, mock previews, and scratch files created during the task.
- Remove `Assets/Screenshots*` and their `.meta` files unless the user explicitly asked to keep them.
- Keep optional proof images only under `Temp/CodexScreenshots/`, and delete them too if they were only temporary reasoning aids.
- Run `git status --short` before finishing and make sure the diff only contains expected prefab or directly related workflow files.

## Acceptance Checklist

- Preserve required logic components and serialized bindings.
- Wire motion and interaction to the intended local pattern.
- Use anchors and pivots intentionally for resolution changes.
- Leave out unnecessary decorative elements.
- Remove temporary files and preview artifacts.
- Report what was verified in Unity and what stayed blocked.
