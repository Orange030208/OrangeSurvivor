#!/usr/bin/env python3
"""Normalize a transparent sprite atlas into a fixed grid."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Detect visible components in a transparent atlas, group them into a row/column grid, "
            "and export a centered PNG for Unity slicing."
        )
    )
    parser.add_argument("--input", required=True, type=Path, help="Source transparent atlas.")
    parser.add_argument("--output", required=True, type=Path, help="Normalized PNG atlas.")
    parser.add_argument("--rows", required=True, type=int, help="Grid row count.")
    parser.add_argument("--cols", required=True, type=int, help="Grid column count.")
    parser.add_argument("--cell-size", type=int, default=128, help="Output cell size in pixels.")
    parser.add_argument(
        "--content-scale",
        type=float,
        default=0.94,
        help="Maximum fraction of the cell reserved for the icon content.",
    )
    parser.add_argument(
        "--alpha-threshold",
        type=int,
        default=0,
        help="Alpha greater than this value counts as visible content.",
    )
    parser.add_argument(
        "--min-component-pixels",
        type=int,
        default=16,
        help="Discard connected components smaller than this size.",
    )
    parser.add_argument(
        "--connectivity",
        choices=(4, 8),
        default=4,
        type=int,
        help="Pixel connectivity used for component detection.",
    )
    return parser.parse_args()


def open_rgba(path: Path) -> Image.Image:
    if not path.is_file():
        raise FileNotFoundError(f"Missing input image: {path}")
    return Image.open(path).convert("RGBA")


def neighbor_offsets(connectivity: int) -> tuple[tuple[int, int], ...]:
    if connectivity == 8:
        return (
            (-1, -1), (0, -1), (1, -1),
            (-1, 0), (1, 0),
            (-1, 1), (0, 1), (1, 1),
        )
    return ((-1, 0), (1, 0), (0, -1), (0, 1))


def extract_components(
    image: Image.Image,
    alpha_threshold: int,
    min_component_pixels: int,
    connectivity: int,
) -> list[dict[str, object]]:
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    visited = bytearray(width * height)
    offsets = neighbor_offsets(connectivity)
    components: list[dict[str, object]] = []

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if visited[index] or pixels[x, y] <= alpha_threshold:
                continue

            queue = deque([(x, y)])
            visited[index] = 1
            min_x = max_x = x
            min_y = max_y = y
            count = 0

            while queue:
                current_x, current_y = queue.popleft()
                count += 1
                if current_x < min_x:
                    min_x = current_x
                if current_x > max_x:
                    max_x = current_x
                if current_y < min_y:
                    min_y = current_y
                if current_y > max_y:
                    max_y = current_y

                for offset_x, offset_y in offsets:
                    neighbor_x = current_x + offset_x
                    neighbor_y = current_y + offset_y
                    if not (0 <= neighbor_x < width and 0 <= neighbor_y < height):
                        continue
                    neighbor_index = neighbor_y * width + neighbor_x
                    if visited[neighbor_index] or pixels[neighbor_x, neighbor_y] <= alpha_threshold:
                        continue
                    visited[neighbor_index] = 1
                    queue.append((neighbor_x, neighbor_y))

            if count >= min_component_pixels:
                components.append(
                    {
                        "bbox": (min_x, min_y, max_x + 1, max_y + 1),
                        "count": count,
                        "cx": (min_x + max_x + 1) / 2.0,
                        "cy": (min_y + max_y + 1) / 2.0,
                    }
                )

    return components


def weighted_kmeans_1d(values: list[float], weights: list[int], count: int) -> list[float]:
    if count <= 0:
        raise ValueError("Grid dimensions must be positive.")
    if not values:
        raise ValueError("No visible components were detected.")

    paired = sorted(zip(values, weights), key=lambda pair: pair[0])
    sorted_values = [value for value, _ in paired]
    sorted_weights = [weight for _, weight in paired]

    if len(sorted_values) < count:
        minimum = sorted_values[0]
        maximum = sorted_values[-1]
        if minimum == maximum:
            return [minimum for _ in range(count)]
        step = (maximum - minimum) / max(count - 1, 1)
        return [minimum + step * index for index in range(count)]

    centers = [sorted_values[round(index * (len(sorted_values) - 1) / (count - 1))] for index in range(count)]
    for _ in range(64):
        groups: list[list[tuple[float, int]]] = [[] for _ in range(count)]
        for value, weight in zip(sorted_values, sorted_weights):
            nearest = min(range(count), key=lambda index: abs(value - centers[index]))
            groups[nearest].append((value, weight))

        updated: list[float] = []
        for index, group in enumerate(groups):
            if not group:
                updated.append(centers[index])
                continue
            total_weight = sum(weight for _, weight in group)
            updated.append(sum(value * weight for value, weight in group) / total_weight)

        if all(abs(previous - current) < 1e-6 for previous, current in zip(centers, updated)):
            centers = updated
            break
        centers = updated

    return sorted(centers)


def build_cell_map(
    components: list[dict[str, object]],
    rows: int,
    cols: int,
) -> dict[tuple[int, int], list[dict[str, object]]]:
    x_centers = weighted_kmeans_1d(
        [float(component["cx"]) for component in components],
        [int(component["count"]) for component in components],
        cols,
    )
    y_centers = weighted_kmeans_1d(
        [float(component["cy"]) for component in components],
        [int(component["count"]) for component in components],
        rows,
    )

    cell_map: dict[tuple[int, int], list[dict[str, object]]] = {
        (row, col): [] for row in range(rows) for col in range(cols)
    }
    for component in components:
        column = min(range(cols), key=lambda index: abs(float(component["cx"]) - x_centers[index]))
        row = min(range(rows), key=lambda index: abs(float(component["cy"]) - y_centers[index]))
        cell_map[(row, column)].append(component)

    return cell_map


def resize_to_cell(image: Image.Image, cell_size: int, content_scale: float) -> tuple[Image.Image, tuple[int, int]]:
    if not (0 < content_scale <= 1.0):
        raise ValueError("--content-scale must be within (0, 1].")

    target_extent = max(1, round(cell_size * content_scale))
    source_width, source_height = image.size
    scale = min(target_extent / source_width, target_extent / source_height)
    resized_size = (
        max(1, round(source_width * scale)),
        max(1, round(source_height * scale)),
    )
    resized = image.resize(resized_size, Image.Resampling.LANCZOS)
    offset = ((cell_size - resized.width) // 2, (cell_size - resized.height) // 2)
    return resized, offset


def normalize(args: argparse.Namespace) -> None:
    if args.rows <= 0 or args.cols <= 0:
        raise ValueError("--rows and --cols must be positive.")
    if args.cell_size <= 0:
        raise ValueError("--cell-size must be positive.")

    source = open_rgba(args.input)
    components = extract_components(
        source,
        alpha_threshold=args.alpha_threshold,
        min_component_pixels=args.min_component_pixels,
        connectivity=args.connectivity,
    )
    if len(components) < args.rows * args.cols:
        print(
            f"warning: detected {len(components)} components for {args.rows * args.cols} cells; "
            "empty tiles may remain if the source atlas is incomplete."
        )

    cell_map = build_cell_map(components, args.rows, args.cols)

    atlas = Image.new("RGBA", (args.cols * args.cell_size, args.rows * args.cell_size), (0, 0, 0, 0))
    report: list[str] = []

    for row in range(args.rows):
        for col in range(args.cols):
            grouped_components = cell_map[(row, col)]
            if not grouped_components:
                report.append(f"cell[{row},{col}] empty")
                continue

            min_x = min(int(component["bbox"][0]) for component in grouped_components)
            min_y = min(int(component["bbox"][1]) for component in grouped_components)
            max_x = max(int(component["bbox"][2]) for component in grouped_components)
            max_y = max(int(component["bbox"][3]) for component in grouped_components)
            tile = source.crop((min_x, min_y, max_x, max_y))
            fitted, offset = resize_to_cell(tile, args.cell_size, args.content_scale)

            cell_image = Image.new("RGBA", (args.cell_size, args.cell_size), (0, 0, 0, 0))
            cell_image.alpha_composite(fitted, offset)
            atlas.alpha_composite(cell_image, (col * args.cell_size, row * args.cell_size))
            report.append(
                f"cell[{row},{col}] bbox=({min_x}, {min_y}, {max_x}, {max_y}) "
                f"fragments={len(grouped_components)} out={fitted.size} offset={offset}"
            )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(args.output)

    print(f"input={args.input}")
    print(f"output={args.output}")
    print(f"size={atlas.size[0]}x{atlas.size[1]}")
    print(f"components={len(components)}")
    for line in report:
        print(line)


def main() -> int:
    args = parse_args()
    normalize(args)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
