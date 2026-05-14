#!/usr/bin/env python3
"""Post-process generated transparent card art for the Survivors Unity project."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


DEFAULT_BORDER = Path("Assets/GameContent/UI/Sprites/Card/CardBorder.png")
REFERENCE_TOP_LEFT_CUTS_96X128 = (
    (0, 0), (1, 0), (2, 0), (3, 0),
    (0, 1), (1, 1),
    (0, 2),
    (0, 3),
)
REFERENCE_TOP_RIGHT_CUTS_96X128 = (
    (0, 0), (1, 0), (2, 0), (3, 0),
    (0, 1), (1, 1),
    (0, 2),
    (0, 3),
)
REFERENCE_BOTTOM_LEFT_CUTS_96X128 = (
    (0, 0), (1, 0), (0, 1),
)
REFERENCE_BOTTOM_RIGHT_CUTS_96X128 = (
    (0, 0), (1, 0), (0, 1),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Trim a generated transparent card image, resize it to 96x128, "
            "overlay the project card border, and clear corner leakage."
        )
    )
    parser.add_argument("--input", required=True, type=Path, help="Generated transparent PNG.")
    parser.add_argument("--output", required=True, type=Path, help="Final PNG path.")
    parser.add_argument("--border", type=Path, default=DEFAULT_BORDER, help="Border overlay PNG.")
    parser.add_argument("--width", type=int, default=96, help="Output width in pixels.")
    parser.add_argument("--height", type=int, default=128, help="Output height in pixels.")
    parser.add_argument(
        "--alpha-threshold",
        type=int,
        default=8,
        help="Alpha greater than this value is treated as visible content.",
    )
    parser.add_argument(
        "--corner-cut-size",
        type=int,
        default=5,
        help="Outer-corner cleanup size in source 96x128 pixels; only the silhouette cut is cleared.",
    )
    return parser.parse_args()


def open_rgba(path: Path) -> Image.Image:
    if not path.is_file():
        raise FileNotFoundError(f"Missing required image: {path}")
    return Image.open(path).convert("RGBA")


def visible_bbox(image: Image.Image, alpha_threshold: int) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > alpha_threshold else 0)
    bbox = mask.getbbox()
    if bbox is None:
        raise ValueError("Input image has no visible alpha content.")
    return bbox


def resize_cover(image: Image.Image, width: int, height: int) -> Image.Image:
    source_width, source_height = image.size
    scale = max(width / source_width, height / source_height)
    resized_size = (
        max(1, round(source_width * scale)),
        max(1, round(source_height * scale)),
    )
    resized = image.resize(resized_size, Image.Resampling.LANCZOS)
    left = (resized.width - width) // 2
    top = (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height))


def scaled_corner_cut_offsets(
    reference_offsets: tuple[tuple[int, int], ...],
    width: int,
    height: int,
    corner_cut_size: int,
) -> set[tuple[int, int]]:
    if corner_cut_size <= 0:
        raise ValueError("--corner-cut-size must be positive.")

    scale_x = width / 96
    scale_y = height / 128
    scale = max(scale_x, scale_y)
    max_distance = max(1, round(corner_cut_size * scale))
    offsets: set[tuple[int, int]] = set()

    for x, y in reference_offsets:
        scaled_x = round(x * scale_x)
        scaled_y = round(y * scale_y)
        if scaled_x < max_distance and scaled_y < max_distance:
            offsets.add((scaled_x, scaled_y))

    return offsets


def clear_outer_corner_cuts(image: Image.Image, corner_cut_size: int) -> int:
    pixels = image.load()
    top_left_offsets = scaled_corner_cut_offsets(
        REFERENCE_TOP_LEFT_CUTS_96X128,
        image.width,
        image.height,
        corner_cut_size,
    )
    top_right_offsets = scaled_corner_cut_offsets(
        REFERENCE_TOP_RIGHT_CUTS_96X128,
        image.width,
        image.height,
        corner_cut_size,
    )
    bottom_left_offsets = scaled_corner_cut_offsets(
        REFERENCE_BOTTOM_LEFT_CUTS_96X128,
        image.width,
        image.height,
        corner_cut_size,
    )
    bottom_right_offsets = scaled_corner_cut_offsets(
        REFERENCE_BOTTOM_RIGHT_CUTS_96X128,
        image.width,
        image.height,
        corner_cut_size,
    )
    removed = 0

    corner_sets = (
        (top_left_offsets, lambda offset_x, offset_y: (offset_x, offset_y)),
        (top_right_offsets, lambda offset_x, offset_y: (image.width - 1 - offset_x, offset_y)),
        (bottom_left_offsets, lambda offset_x, offset_y: (offset_x, image.height - 1 - offset_y)),
        (
            bottom_right_offsets,
            lambda offset_x, offset_y: (image.width - 1 - offset_x, image.height - 1 - offset_y),
        ),
    )

    for offsets, to_position in corner_sets:
        for offset_x, offset_y in offsets:
            x, y = to_position(offset_x, offset_y)
            if pixels[x, y][3] > 0:
                pixels[x, y] = (0, 0, 0, 0)
                removed += 1

    return removed


def process(args: argparse.Namespace) -> None:
    if args.width <= 0 or args.height <= 0:
        raise ValueError("--width and --height must be positive.")

    source = open_rgba(args.input)
    border = open_rgba(args.border)

    bbox = visible_bbox(source, args.alpha_threshold)
    cropped = source.crop(bbox)
    fitted = resize_cover(cropped, args.width, args.height)

    border_resized = False
    if border.size != (args.width, args.height):
        border = border.resize((args.width, args.height), Image.Resampling.LANCZOS)
        border_resized = True

    composed = Image.alpha_composite(fitted, border)
    removed = clear_outer_corner_cuts(composed, args.corner_cut_size)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    composed.save(args.output)

    print(f"input={args.input}")
    print(f"output={args.output}")
    print(f"size={args.width}x{args.height}")
    print(f"trim_bbox={bbox}")
    print(f"border={args.border}")
    print(f"border_resized={border_resized}")
    print(f"corner_pixels_removed={removed}")


def main() -> int:
    args = parse_args()
    process(args)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
