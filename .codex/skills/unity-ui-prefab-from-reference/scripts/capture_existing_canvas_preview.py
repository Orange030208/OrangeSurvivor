#!/usr/bin/env python3
"""Capture a Unity UGUI prefab under an existing scene Canvas."""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import time
from pathlib import Path
from typing import Any


def find_project_root(start: Path) -> Path:
    current = start.resolve()
    for candidate in [current, *current.parents]:
        if (candidate / "Assets").is_dir() and (candidate / "ProjectSettings").is_dir():
            return candidate
    raise RuntimeError("Could not find Unity project root from current directory.")


PROJECT_ROOT = find_project_root(Path.cwd())
UNITY_SKILLS_HELPER = PROJECT_ROOT / ".codex" / "skills" / "unity-skills" / "scripts"
if str(UNITY_SKILLS_HELPER) not in sys.path:
    sys.path.insert(0, str(UNITY_SKILLS_HELPER))

try:
    import unity_skills  # type: ignore
except ImportError as exc:
    raise RuntimeError(f"UnitySkills helper not found at {UNITY_SKILLS_HELPER}") from exc


def call_skill(skill_name: str, retries: int = 6, delay: float = 2.0, **kwargs: Any) -> dict[str, Any]:
    last: dict[str, Any] | None = None
    for _ in range(retries):
        result = unity_skills.call_skill(skill_name, **kwargs)
        if isinstance(result, dict):
            last = result
            if result.get("success"):
                return result
        time.sleep(delay)
    return last or {"success": False, "error": f"No response from {skill_name}"}


def ensure_inside_project(path: Path) -> Path:
    resolved = path.resolve()
    try:
        resolved.relative_to(PROJECT_ROOT)
    except ValueError as exc:
        raise RuntimeError(f"Refusing to touch path outside project: {resolved}") from exc
    return resolved


def remove_empty_assets_screenshots() -> bool:
    screenshots_dir = PROJECT_ROOT / "Assets" / "Screenshots"
    screenshots_meta = PROJECT_ROOT / "Assets" / "Screenshots.meta"

    if screenshots_dir.exists():
        ensure_inside_project(screenshots_dir)
        if any(screenshots_dir.iterdir()):
            return False
        screenshots_dir.rmdir()

    if screenshots_meta.exists() and not screenshots_dir.exists():
        ensure_inside_project(screenshots_meta)
        screenshots_meta.unlink()

    return not screenshots_dir.exists() and not screenshots_meta.exists()


def cleanup_asset_screenshot(asset_path: str, keep_assets_screenshot: bool) -> dict[str, Any]:
    src = ensure_inside_project(PROJECT_ROOT / asset_path)
    kept = src.exists()
    if not keep_assets_screenshot:
        if src.exists():
            src.unlink()
        meta = Path(str(src) + ".meta")
        if meta.exists():
            ensure_inside_project(meta).unlink()
        remove_empty_assets_screenshots()
        kept = False
    return {"assetScreenshot": str(src), "keptInAssets": kept}


def main() -> int:
    parser = argparse.ArgumentParser(description="Capture a prefab under an existing Unity Canvas.")
    parser.add_argument("--prefab", required=True, help="Prefab asset path, e.g. Assets/UI/Menu.prefab")
    parser.add_argument("--canvas", required=True, help="Existing scene Canvas name")
    parser.add_argument("--camera", required=True, help="Existing camera name used by the Canvas")
    parser.add_argument("--output", required=True, help="Output PNG path, preferably under Temp/CodexScreenshots")
    parser.add_argument("--temp-name", default="__CodexUiPrefabPreview", help="Temporary scene instance name")
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    parser.add_argument("--keep-assets-screenshot", action="store_true")
    args = parser.parse_args()

    output = ensure_inside_project(PROJECT_ROOT / args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    asset_screenshot = f"Assets/Screenshots/{output.name}"

    result: dict[str, Any] = {
        "success": False,
        "prefab": args.prefab,
        "canvas": args.canvas,
        "camera": args.camera,
        "tempName": args.temp_name,
        "output": str(output),
    }

    call_skill("gameobject_delete", name=args.temp_name, retries=1)

    canvas_info = call_skill("gameobject_get_info", name=args.canvas)
    canvas_component = call_skill("component_get_properties", name=args.canvas, componentType="Canvas")
    scaler_component = call_skill("component_get_properties", name=args.canvas, componentType="CanvasScaler")
    instantiate = call_skill(
        "prefab_instantiate",
        prefabPath=args.prefab,
        name=args.temp_name,
        parentName=args.canvas,
    )

    if not instantiate.get("success"):
        result.update(
            {
                "canvasInfo": canvas_info,
                "canvasComponent": canvas_component,
                "canvasScaler": scaler_component,
                "instantiate": instantiate,
                "error": "Failed to instantiate prefab under existing Canvas.",
            }
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 1

    cleanup: dict[str, Any] = {}
    try:
        shot = call_skill(
            "camera_screenshot",
            name=args.camera,
            savePath=asset_screenshot,
            width=args.width,
            height=args.height,
        )
        if not shot.get("success"):
            result.update({"screenshot": shot, "error": "camera_screenshot failed."})
            print(json.dumps(result, ensure_ascii=False, indent=2))
            return 1

        source = ensure_inside_project(PROJECT_ROOT / asset_screenshot)
        if not source.exists():
            result.update({"screenshot": shot, "error": f"Screenshot file not found: {source}"})
            print(json.dumps(result, ensure_ascii=False, indent=2))
            return 1

        shutil.copy2(source, output)
        cleanup = cleanup_asset_screenshot(asset_screenshot, args.keep_assets_screenshot)
        call_skill("asset_refresh", retries=3)
    finally:
        deleted = call_skill("gameobject_delete", name=args.temp_name, retries=3)

    residual = call_skill("scene_find_objects", namePattern=args.temp_name, limit=5)
    result.update(
        {
            "success": output.exists() and not residual.get("objects"),
            "canvasInfo": canvas_info,
            "canvasComponent": canvas_component,
            "canvasScaler": scaler_component,
            "instantiate": instantiate,
            "deleteTemp": deleted,
            "residualCheck": residual,
            "cleanup": cleanup,
            "width": args.width,
            "height": args.height,
        }
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result["success"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
