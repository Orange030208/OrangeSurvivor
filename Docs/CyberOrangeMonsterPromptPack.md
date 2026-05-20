# Cyber Orange Monster Prompt Pack

## 用途

用于生成“可爱赛博朋克橘子怪”系列单图 Sprite。默认目标是 Unity 2D 敌人素材，建议生成正方形大图后统一缩放到 `256x256`，背景使用纯色绿幕再本地去底。

## 基础提示词

```text
Use case: stylized-concept
Asset type: Unity 2D enemy sprite, square source image, final asset should read clearly at 256x256.
Primary request: Cute cyberpunk orange monster with neon graffiti, hand-drawn spray-paint marks, cat symbols, and dreamy night-city mood.
Subject: A front-facing round orange fruit monster, no arms and no legs, cute mischievous or sleepy-cat expression, rounded peel body, simple readable silhouette for a small game sprite. Add hand-painted spray-paint graffiti directly on the peel: cat-face symbol, paw prints, neon strokes, cyber decals, and tiny dreamy night-city motifs. Keep the character adorable, soft, compact, and toy-like, not scary.
Style: 2D hand-painted cartoon game sprite, thick clean dark outline, cel-shaded orange peel, soft peel texture, cyberpunk neon cyan, pink, and violet accents, subtle painted glow only on the character decals, cute collectible enemy style.
Composition: centered single character only, full-body sprite, orthographic front view, generous transparent-safe padding, strong silhouette, readable at 256x256.
Background removal requirement: Create the subject on a perfectly flat solid #00ff00 chroma-key background for background removal. The background must be one uniform color with no shadows, gradients, texture, reflections, floor plane, or lighting variation. Keep the subject fully separated from the background with crisp edges and generous padding. Do not use #00ff00 or green anywhere in the subject. No cast shadow, no contact shadow, no reflection, no watermark, no text, no logo.
```

## 骷髅橘子怪提示词

```text
Skeleton orange monster, cute cyberpunk citrus undead, with neon graffiti, hand-drawn spray-paint marks, cat symbols, and dreamy night-city mood.
Subject: A front-facing round orange fruit monster with a cute skull-like face carved or painted into the peel, no arms and no legs, compact rounded body, simple readable silhouette. The orange peel has bone-like white peel cracks and small skull motifs, but it remains adorable and toy-like, not horror. Add hand-painted spray-paint graffiti directly on the peel: cat-face symbol, paw prints, neon strokes, cyber decals, tiny dreamy night-city motifs, and subtle cyberpunk bone decals. Keep the character soft, cute, collectible, and readable at small size.
```

## 史莱姆橘子怪提示词

```text
Slime orange monster, cute cyberpunk citrus gel creature, with neon graffiti, hand-drawn spray-paint marks, cat symbols, and dreamy night-city mood.
Subject: A front-facing round orange slime monster made of glossy orange citrus jelly, no arms and no legs, squat blob shape with soft drips at the bottom, cute sleepy-cat or mischievous expression, simple readable silhouette. The translucent orange slime body contains floating peel segments and tiny bubbles, while the surface has hand-painted spray-paint graffiti: cat-face symbol, paw prints, neon strokes, cyber decals, tiny dreamy night-city motifs, and small pink/cyan paint drips. Keep it adorable, soft, compact, collectible, and not scary.
```

## 反向约束

```text
Avoid realistic fruit photography, horror monster design, arms, legs, full background scene, complex city backdrop, heavy shadows, floor plane, low-contrast outline, thin details that disappear at 256x256, text, watermark, logo, green subject details, or any background color variation.
```

## 同系列区分规则

```text
When generating multiple enemy roles in the same cyber orange family, each role must be instantly distinguishable at 256x256. Prioritize a unique face language, eye shape, mouth shape, expression, and main silhouette before changing small equipment details. Do not reuse the same face across variants.

Examples:
- Fast melee: narrow diagonal neon visor eyes, sharp mischievous cat grin, lightning or speed-line cheek marks, slimmer forward-leaning silhouette.
- Slow heavy melee: sleepy half-closed eye, thick heavy brow plates, tiny flat grumpy mouth, wide squat armored silhouette.
- Ranged turret: single large target-lens eye, small focused mouth, compact top-mounted energy cannon integrated into the shell.
- Ranged kiter: smug cat-mask face, crescent moon eyes, asymmetrical neon goggle, light hover-thruster or floating ring silhouette.
- Melee charger: angry V-shaped neon eyes, clenched square mouth, clear front impact horn or conical bumper.
```

## 批量生成建议

- 统一生成大正方形源图，再裁切、补透明边距、缩放到 `256x256`。
- 同一系列尽量固定：正面视角、无手脚、厚描边、橙色主体、青/粉/紫霓虹点缀。
- 系列新怪主要改主题特征、表情、顶部装饰、涂鸦符号和夜城贴纸，不要改核心轮廓太多，方便后续共用程序动画。
- 批量生成同一族怪物时，先锁定每个怪的脸部语言和主轮廓，再补机甲件、武器或涂鸦细节；小装备差异不能替代脸部差异。
- 如果后续要接入当前程序动画 Shader，主体最好保持接近圆形，避免细长外扩件被挤压动画拉得过猛。
