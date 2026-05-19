from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SOURCE_REFERENCE = ROOT / "Assets/GameContent/UI/MainMenu/V2/Backgrounds/main_menu_neon_city_background_v2.png"
OUT_ROOT = ROOT / "Assets/GameContent/UI/MainMenu/V2/Backgrounds/main_menu_neon_city_background_v2_coarse_ai_layers"
SOURCE_DIR = OUT_ROOT / "source"
LAYERS_DIR = OUT_ROOT / "layers"
PREVIEWS_DIR = OUT_ROOT / "previews"
TARGET_SIZE = (1672, 941)
REMOVE_CHROMA_KEY = Path(r"C:\Users\AXR\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py")


LAYER_ORDER = [
    ("00_backdrop_sky_moon.png", "Alpha", "Opaque backdrop: night sky, stars, pink moon, and distant ambient glow."),
    ("01_neon_city.png", "Alpha", "Transparent full city layer: far skyline, central towers, right towers, neon billboards."),
    ("02_rooftop_foreground.png", "Alpha", "Transparent rooftop foreground: wet floor, reflections, railing, fence, cables, left edge props."),
    ("03_cat_and_props.png", "Alpha", "Transparent right foreground: large cat, pedestal, crates, cans, pipes, nearby props."),
    ("04_neon_fx_overlay.png", "Additive/Screen", "Optional transparent neon bloom and reflection enhancement overlay."),
]


def ensure_dirs() -> None:
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    LAYERS_DIR.mkdir(parents=True, exist_ok=True)
    PREVIEWS_DIR.mkdir(parents=True, exist_ok=True)


def fit_to_canvas(image: Image.Image) -> Image.Image:
    if image.size == TARGET_SIZE:
        return image

    target_w, target_h = TARGET_SIZE
    src_w, src_h = image.size
    target_ratio = target_w / target_h
    src_ratio = src_w / src_h

    if src_ratio > target_ratio:
        new_w = int(src_h * target_ratio)
        left = (src_w - new_w) // 2
        box = (left, 0, left + new_w, src_h)
    else:
        new_h = int(src_w / target_ratio)
        top = (src_h - new_h) // 2
        box = (0, top, src_w, top + new_h)

    return image.crop(box).resize(TARGET_SIZE, Image.Resampling.LANCZOS)


def clean_transparent_rgb(image: Image.Image) -> Image.Image:
    rgba = np.array(image.convert("RGBA"))
    alpha = rgba[:, :, 3]
    rgba[alpha == 0, :3] = 0
    return Image.fromarray(rgba, "RGBA")


def copy_layer(args: argparse.Namespace) -> None:
    ensure_dirs()
    source_path = Path(args.input).resolve()
    if not source_path.exists():
        raise FileNotFoundError(source_path)

    source_out = SOURCE_DIR / f"{args.name}_source.png"
    Image.open(source_path).convert("RGBA").save(source_out)

    layer_out = LAYERS_DIR / f"{args.name}.png"
    if args.opaque:
        image = fit_to_canvas(Image.open(source_out).convert("RGBA"))
        image.putalpha(255)
        image.save(layer_out)
    else:
        keyed_out = OUT_ROOT / f".{args.name}_key_removed.png"
        command = [
            sys.executable,
            str(REMOVE_CHROMA_KEY),
            "--input",
            str(source_out),
            "--out",
            str(keyed_out),
            "--auto-key",
            "border",
            "--soft-matte",
            "--transparent-threshold",
            str(args.transparent_threshold),
            "--opaque-threshold",
            str(args.opaque_threshold),
            "--despill",
        ]
        subprocess.run(command, check=True)
        clean_transparent_rgb(fit_to_canvas(Image.open(keyed_out).convert("RGBA"))).save(layer_out)
        keyed_out.unlink(missing_ok=True)

    print(layer_out)


def create_preview() -> Path:
    base = Image.new("RGBA", TARGET_SIZE, (0, 0, 0, 0))
    for filename, _, _ in LAYER_ORDER:
        path = LAYERS_DIR / filename
        if not path.exists():
            raise FileNotFoundError(path)
        base = Image.alpha_composite(base, Image.open(path).convert("RGBA"))

    output = PREVIEWS_DIR / "preview_coarse_recomposed.png"
    base.save(output)
    return output


def create_contact_sheet() -> Path:
    thumbs: list[tuple[str, Image.Image]] = []
    for filename, _, _ in LAYER_ORDER:
        image = Image.open(LAYERS_DIR / filename).convert("RGBA")
        checker = Image.new("RGBA", image.size, (18, 20, 30, 255))
        draw = ImageDraw.Draw(checker)
        step = 36
        for y in range(0, image.height, step):
            for x in range(0, image.width, step):
                color = (42, 44, 56, 255) if (x // step + y // step) % 2 else (26, 28, 38, 255)
                draw.rectangle((x, y, x + step, y + step), fill=color)
        thumb = Image.alpha_composite(checker, image)
        thumb.thumbnail((420, 236), Image.Resampling.LANCZOS)
        thumbs.append((filename, thumb.convert("RGB")))

    columns = 1
    cell_w, cell_h = 470, 290
    sheet = Image.new("RGB", (columns * cell_w, len(thumbs) * cell_h), (12, 14, 24))
    draw = ImageDraw.Draw(sheet)
    for index, (filename, thumb) in enumerate(thumbs):
        x = 24
        y = index * cell_h + 18
        draw.text((x, y), filename, fill=(235, 238, 255))
        sheet.paste(thumb, (x, y + 30))

    output = PREVIEWS_DIR / "preview_coarse_layer_contact_sheet.png"
    sheet.save(output)
    return output


def write_manifest(preview: Path, sheet: Path) -> list[dict[str, object]]:
    entries: list[dict[str, object]] = []
    for order, (filename, blend, description) in enumerate(LAYER_ORDER):
        path = LAYERS_DIR / filename
        image = Image.open(path).convert("RGBA")
        alpha = image.getchannel("A").getextrema()
        entries.append(
            {
                "order": order,
                "file": f"layers/{filename}",
                "blend": blend,
                "description": description,
                "size": list(image.size),
                "alphaRange": list(alpha),
            }
        )

    manifest = {
        "sourceReference": str(SOURCE_REFERENCE.relative_to(ROOT)).replace("\\", "/"),
        "outputRoot": str(OUT_ROOT.relative_to(ROOT)).replace("\\", "/"),
        "canvas": {"width": TARGET_SIZE[0], "height": TARGET_SIZE[1]},
        "generationMode": "AI-regenerated coarse layers; not recovered PSD/source layers",
        "layers": entries,
        "preview": str(preview.relative_to(OUT_ROOT)).replace("\\", "/"),
        "contactSheet": str(sheet.relative_to(OUT_ROOT)).replace("\\", "/"),
    }
    (OUT_ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    readme_lines = [
        "# main_menu_neon_city_background_v2 coarse AI layers",
        "",
        "These are AI-regenerated coarse layers based on the original background composition.",
        f"All final assembly layers are normalized to `{TARGET_SIZE[0]}x{TARGET_SIZE[1]}`.",
        "",
        "Assembly order:",
        "",
    ]
    for entry in entries:
        readme_lines.append(
            f"{entry['order']:02d}. `{entry['file']}` - {entry['description']} Blend: {entry['blend']}."
        )
    readme_lines.extend(
        [
            "",
            "Generated files:",
            "- `source/`: copied AI outputs before key removal/normalization.",
            "- `layers/`: final Unity assembly PNGs.",
            "- `previews/preview_coarse_recomposed.png`: default alpha-composited preview.",
            "- `previews/preview_coarse_layer_contact_sheet.png`: per-layer visual check.",
            "- `manifest.json`: machine-readable layer order, blend notes, dimensions, and alpha ranges.",
        ]
    )
    (OUT_ROOT / "README.md").write_text("\n".join(readme_lines) + "\n", encoding="utf-8")
    return entries


def validate(args: argparse.Namespace) -> None:
    ensure_dirs()
    preview = create_preview()
    sheet = create_contact_sheet()
    entries = write_manifest(preview, sheet)
    for entry in entries:
        print(f"{entry['file']} size={entry['size']} alpha={entry['alphaRange']} blend={entry['blend']}")
    print(preview)
    print(sheet)


def main() -> None:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    copy_parser = subparsers.add_parser("copy-layer")
    copy_parser.add_argument("--name", required=True)
    copy_parser.add_argument("--input", required=True)
    copy_parser.add_argument("--opaque", action="store_true")
    copy_parser.add_argument("--transparent-threshold", type=int, default=12)
    copy_parser.add_argument("--opaque-threshold", type=int, default=220)
    copy_parser.set_defaults(func=copy_layer)

    validate_parser = subparsers.add_parser("validate")
    validate_parser.set_defaults(func=validate)

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
