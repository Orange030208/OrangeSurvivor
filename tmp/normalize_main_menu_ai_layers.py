from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from PIL import ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUT_ROOT = ROOT / "Assets/GameContent/UI/MainMenu/V2/Backgrounds/main_menu_neon_city_background_v2_ai_layers"
LAYERS_DIR = OUT_ROOT / "layers"
PREVIEWS_DIR = OUT_ROOT / "previews"
TARGET_SIZE = (1672, 941)


LAYER_ORDER = [
    ("00_sky_base.png", "Alpha", "Opaque night sky base."),
    ("01_full_moon.png", "Alpha", "Transparent neon full moon."),
    ("02_far_city_silhouette.png", "Alpha", "Transparent distant skyline."),
    ("03_mid_city_towers.png", "Alpha", "Transparent central tower cluster."),
    ("04_right_city_and_billboards.png", "Alpha", "Transparent right-side towers and cat billboards."),
    ("05_rooftop_floor.png", "Alpha", "Transparent wet rooftop floor."),
    ("06_railing_cables.png", "Alpha", "Transparent railing, fence, posts, and cables."),
    ("07_cat_pedestal_props.png", "Alpha", "Transparent pedestal and foreground props."),
    ("08_cat_statue.png", "Alpha", "Transparent large cat statue."),
    ("09_neon_glow_overlay.png", "Additive/Screen", "Transparent neon glow overlay."),
]

CAT_TARGET_BOX = (1130, 348, 1488, 805)


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


def reposition_cat_layer() -> None:
    path = LAYERS_DIR / "08_cat_statue.png"
    image = clean_transparent_rgb(fit_to_canvas(Image.open(path).convert("RGBA")))
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("08_cat_statue.png has no visible pixels.")

    cat = image.crop(bbox)
    target_w = CAT_TARGET_BOX[2] - CAT_TARGET_BOX[0]
    target_h = CAT_TARGET_BOX[3] - CAT_TARGET_BOX[1]
    cat.thumbnail((target_w, target_h), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", TARGET_SIZE, (0, 0, 0, 0))
    x = CAT_TARGET_BOX[0] + (target_w - cat.width) // 2
    y = CAT_TARGET_BOX[3] - cat.height
    canvas.alpha_composite(cat, (x, y))
    clean_transparent_rgb(canvas).save(path)


def create_bloom_overlay() -> None:
    skip = {"00_sky_base.png", "09_neon_glow_overlay.png"}
    composite = Image.new("RGBA", TARGET_SIZE, (0, 0, 0, 0))
    for filename, _, _ in LAYER_ORDER:
        if filename in skip:
            continue
        composite = Image.alpha_composite(composite, Image.open(LAYERS_DIR / filename).convert("RGBA"))

    rgba = np.array(composite).astype(np.float32)
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3] / 255.0
    max_channel = rgb.max(axis=2)
    min_channel = rgb.min(axis=2)
    saturation = (max_channel - min_channel) / np.maximum(max_channel, 1.0)
    neon = ((max_channel > 118) & (saturation > 0.28) & (alpha > 0.05)).astype(np.float32)

    glow_alpha = Image.fromarray(np.uint8(neon * 255), "L").filter(ImageFilter.GaussianBlur(12))
    wide_alpha = Image.fromarray(np.uint8(neon * 180), "L").filter(ImageFilter.GaussianBlur(28))
    alpha_arr = np.maximum(np.array(glow_alpha), np.array(wide_alpha)).astype(np.float32) / 255.0
    alpha_arr = np.clip(alpha_arr * 0.42, 0.0, 0.62)

    blurred_rgb = Image.fromarray(np.uint8(np.clip(rgb, 0, 255)), "RGB").filter(ImageFilter.GaussianBlur(9))
    glow_rgb = np.array(blurred_rgb).astype(np.float32)
    glow_rgb[:, :, 0] = np.maximum(glow_rgb[:, :, 0], 210 * alpha_arr)
    glow_rgb[:, :, 1] = np.maximum(glow_rgb[:, :, 1], 60 * alpha_arr)
    glow_rgb[:, :, 2] = np.maximum(glow_rgb[:, :, 2], 220 * alpha_arr)

    out = np.dstack(
        [
            np.uint8(np.clip(glow_rgb, 0, 255)),
            np.uint8(np.clip(alpha_arr, 0, 1) * 255),
        ]
    )
    image = clean_transparent_rgb(Image.fromarray(out, "RGBA"))
    image.save(LAYERS_DIR / "09_neon_glow_overlay.png")


def normalize_layers() -> list[dict[str, object]]:
    reposition_cat_layer()
    create_bloom_overlay()
    entries: list[dict[str, object]] = []
    for index, (filename, blend, description) in enumerate(LAYER_ORDER):
        path = LAYERS_DIR / filename
        if not path.exists():
            raise FileNotFoundError(path)

        image = clean_transparent_rgb(fit_to_canvas(Image.open(path).convert("RGBA")))
        image.save(path)
        alpha = image.getextrema()[3]
        entries.append(
            {
                "order": index,
                "file": filename,
                "blend": blend,
                "description": description,
                "size": list(image.size),
                "alphaRange": list(alpha),
            }
        )
    return entries


def composite_preview() -> Path:
    base = Image.new("RGBA", TARGET_SIZE, (0, 0, 0, 0))
    for filename, blend, _ in LAYER_ORDER:
        layer = Image.open(LAYERS_DIR / filename).convert("RGBA")
        if blend.startswith("Additive"):
            base = Image.alpha_composite(base, layer)
        else:
            base = Image.alpha_composite(base, layer)
    output = PREVIEWS_DIR / "preview_ai_layers_recomposed.png"
    base.save(output)
    return output


def contact_sheet() -> Path:
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
        thumb.thumbnail((360, 204), Image.Resampling.LANCZOS)
        thumbs.append((filename, thumb.convert("RGB")))

    columns = 2
    cell_w, cell_h = 420, 250
    rows = (len(thumbs) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_w, rows * cell_h), (12, 14, 24))
    draw = ImageDraw.Draw(sheet)
    for index, (filename, thumb) in enumerate(thumbs):
        col = index % columns
        row = index // columns
        x = col * cell_w + 20
        y = row * cell_h + 18
        draw.text((x, y), filename, fill=(235, 238, 255))
        sheet.paste(thumb, (x, y + 28))

    output = PREVIEWS_DIR / "preview_ai_layer_contact_sheet.png"
    sheet.save(output)
    return output


def write_manifest(entries: list[dict[str, object]], preview: Path, sheet: Path) -> None:
    manifest = {
        "sourceReference": "Assets/GameContent/UI/MainMenu/V2/Backgrounds/main_menu_neon_city_background_v2.png",
        "outputRoot": str(OUT_ROOT.relative_to(ROOT)).replace("\\", "/"),
        "canvas": {"width": TARGET_SIZE[0], "height": TARGET_SIZE[1]},
        "generationMode": "AI regenerated layers, not extracted original PSD layers",
        "layers": entries,
        "preview": str(preview.relative_to(OUT_ROOT)).replace("\\", "/"),
        "contactSheet": str(sheet.relative_to(OUT_ROOT)).replace("\\", "/"),
    }
    (OUT_ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    lines = [
        "# AI generated main menu background layers",
        "",
        "These layers are AI-regenerated from the original composition target, not recovered from hidden PSD data.",
        f"All final assembly layers are normalized to `{TARGET_SIZE[0]}x{TARGET_SIZE[1]}`.",
        "",
        "Assembly order:",
        "",
    ]
    for entry in entries:
        lines.append(
            f"{entry['order']:02d}. `layers/{entry['file']}` - {entry['description']} Blend: {entry['blend']}."
        )
    lines.extend(
        [
            "",
            "Generated files:",
            "- `source/`: copied original AI outputs before normalization/key removal.",
            "- `layers/`: final Unity assembly layers.",
            f"- `{preview.relative_to(OUT_ROOT).as_posix()}`: default alpha-composited preview.",
            f"- `{sheet.relative_to(OUT_ROOT).as_posix()}`: per-layer contact sheet.",
            "- `manifest.json`: machine-readable layer order, size, alpha range, and blend notes.",
        ]
    )
    (OUT_ROOT / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    PREVIEWS_DIR.mkdir(parents=True, exist_ok=True)
    entries = normalize_layers()
    preview = composite_preview()
    sheet = contact_sheet()
    write_manifest(entries, preview, sheet)
    for entry in entries:
        print(f"{entry['file']} size={entry['size']} alpha={entry['alphaRange']} blend={entry['blend']}")
    print(preview)
    print(sheet)


if __name__ == "__main__":
    main()
