---
name: card-border-from-reference
description: "项目本地的显式调用工作流：根据用户提供的参考图生成透明 Unity 卡牌图或卡牌边框。流程先使用 imagegen 生成基础图，再用确定性 PNG 后处理裁到内容边缘、覆盖缩放到 96x128、叠加 Assets/GameContent/UI/Sprites/Card/CardBorder.png，并清理圆角外溢。仅当用户明确要求使用 $card-border-from-reference，或明确要求这套参考图卡牌边框流程时使用，不要隐式触发。"
---

# Card Border From Reference

## Purpose

Create a transparent 96x128 PNG card image for this Unity project from a user reference image. The card must fill the whole image bounds, keep the reference's border-to-card proportions, avoid empty edge padding except the transparent rounded-corner area, and receive the project border overlay at `Assets/GameContent/UI/Sprites/Card/CardBorder.png`.

## Workflow

1. Require a user-provided reference image. If none is available, ask for it.
2. Before generation, check that `Assets/GameContent/UI/Sprites/Card/CardBorder.png` exists in the project. If it is missing, ask the user to provide the file or confirm an alternate border path.
3. Use the `imagegen` skill/tool to generate the base image from the reference.
4. Prompt image generation with these constraints:
   - transparent background PNG;
   - card or border occupies the full canvas;
   - no outer padding on straight edges;
   - only the four rounded corners may contain transparent empty space;
   - output aspect ratio is 3:4, preferably 96x128 or a larger 3:4 source that can be downscaled cleanly;
   - border thickness and total border/card proportion match the reference image.
5. If the generated image has an opaque or colored background, use imagegen editing/removal to produce a genuinely transparent PNG before post-processing. Do not accept white, black, or checkerboard pixels as fake transparency.
6. Run `scripts/process_card_border.py` on the generated transparent PNG. Use the default border path unless the user confirmed another path.
7. Inspect the final PNG visually or with an alpha check:
   - output size is exactly 96x128;
   - the card reaches all four canvas edges except transparent rounded corners;
   - the border overlay is visible on top;
   - only the small outer silhouette pixels at the four corners are transparent; for the current project border, expect 8 transparent pixels in each top 8x8 corner and 3 transparent pixels in each bottom 8x8 corner;
   - do not clear decorative transparent holes inside the border.

## Post-Processing Script

Use:

```powershell
python .codex\skills\card-border-from-reference\scripts\process_card_border.py `
  --input <generated-transparent.png> `
  --output <final-card.png>
```

Optional arguments:

```powershell
--border Assets\GameContent\UI\Sprites\Card\CardBorder.png
--width 96
--height 128
--alpha-threshold 8
--corner-cut-size 5
```

The script trims the generated image to its visible alpha bounds, resizes it with cover-crop to 96x128, alpha-composites the project border over it, then clears only the small outer corner silhouette pixels. It must not use the entire border alpha as a corner mask, because the border art can contain transparent decorative holes that should leave the generated card visible underneath.

## Failure Handling

- If `CardBorder.png` is absent, stop and ask the user instead of inventing a replacement.
- If the reference image is unclear, ask for a clearer source or an explicit style note before generation.
- If the post-processed card still has visible edge padding, regenerate or edit the base image with stronger "full canvas, no padding" wording, then rerun the script.
- If the border overlay is a different size, the script resizes it to 96x128; mention this in the result because it can slightly alter crisp pixel art borders.
