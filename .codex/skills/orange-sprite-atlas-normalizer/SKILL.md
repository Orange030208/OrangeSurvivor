---
name: orange-sprite-atlas-normalizer
description: "通过检测可见组件、重组为固定行列网格、将每个切片居中到统一单元格中，并导出便于切图的 PNG，来规范透明精灵/图标图集。适用于间距不均、图标偏心、子图碎片化或需要确定性网格切片（Unity Sprite Mode = Multiple）的 Sprite Sheet、图标图集或 UI 资源表。"
---

# Orange Sprite Atlas Normalizer

## Workflow

1. Confirm the source image is a transparent atlas or sprite sheet with the intended icons already present.
2. Prefer explicit `rows`, `cols`, and `cell-size` when the target grid is known.
3. Run `scripts/normalize_sprite_atlas.py` to detect visible components, group them into the requested grid, and export a centered PNG atlas.
4. Inspect the output visually and confirm the cell count, spacing, and icon order are correct for Unity grid slicing.

## Script

Use:

```powershell
python .codex\skills\orange-sprite-atlas-normalizer\scripts\normalize_sprite_atlas.py `
  --input <source.png> `
  --output <atlas.png> `
  --rows 4 `
  --cols 7 `
  --cell-size 128
```

Recommended defaults for Unity UI atlases:
- Use `--cell-size 128` for square UI icons.
- Keep `--content-scale 0.94` unless the source needs tighter or looser padding.
- Lower `--alpha-threshold` only when the source has faint anti-aliased edges that should count as foreground.
- Raise `--min-component-pixels` only when the image has noisy single-pixel debris.

## Output Rules

- Preserve the original reading order by assigning components to the nearest row and column bands.
- Merge multiple fragments that belong to the same cell before resizing.
- Center each tile inside its cell and keep the output transparent.
- Write the final atlas as a PNG so Unity can import it as `Sprite Mode = Multiple`.
