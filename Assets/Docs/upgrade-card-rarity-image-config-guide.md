# 升级卡牌稀有度图片配置指南

这套稀有度表现的核心思路是：卡牌预制体持有多层 UI Image，稀有度系统只往这些 Image 的材质里写 shader 参数。不要为了 Common/Rare/Epic/Legendary 各做一套不同颜色图片，颜色、流光、脉冲都交给 shader。

## 默认层级

`Upgrade Container` 根节点上会有这些稀有度相关层：

- `Rarity Background`：低透明度的整卡背景层，用来做底色、背景流光、稀有度氛围。
- `Rarity Border`：边框层，用来做像素硬边、边框扫光、史诗能量边。
- `Rarity Glow`：光效层，用来做外发光、传说脉冲、稀有度反馈增强。
- `Card Background`：根节点自身的 Image，也会吃 shader 参数，但它应该保持克制，避免压住文字和图标。

这些层都会被 `UpgradeCardRarityPresenter.shaderTargets` 持有。运行时选择不同稀有度时，Presenter 会把 Catalog 里的参数写入这些目标材质。

## 图片应该怎么做

推荐把图片当成“白色遮罩”来做，而不是当成最终颜色图来做：

- 背景图：白色或浅灰的整张卡牌形状，外面透明。
- 边框图：中间透明，只留下 2-4 像素宽的白色边框。
- 光效图：和卡牌轮廓相近，可以略大一点，白色或灰白，外面透明。

为什么用白色？因为 shader 会在运行时根据稀有度把白色区域染成蓝色、紫色、金色等。如果图片本身已经带很强的颜色，shader 颜色会和原图混在一起，后续会很难调。

## 导入设置

像素风 UI 图片建议这样设置：

- `Texture Type`：Sprite (2D and UI)
- `Sprite Mode`：Single，除非它是图集
- `Pixels Per Unit`：保持项目内统一，UGUI 常用 100 就可以
- `Filter Mode`：Point (no filter)
- `Compression`：None 或 High Quality
- `Generate Mip Maps`：关闭
- `Alpha Is Transparency`：有透明区域就开启
- `Mesh Type`：Full Rect，UI 遮罩和边框更稳定

如果你看到边框发糊，优先检查三个地方：Filter Mode 是否是 Point、是否开了压缩、图片是不是被非整数比例缩放了。

## 替换图片步骤

1. 打开 `Assets/Resources/Prefabs/New UI/Pages/WaveTransition/Upgrade Container.prefab`。
2. 选中 `Rarity Background`、`Rarity Border` 或 `Rarity Glow`。
3. 在 Image 组件里把 `Source Image` 换成你的 sprite。
4. Image 的颜色建议保持白色，只调整 Alpha 控制该层强弱。
5. 不要给每个稀有度手动换不同材质。材质模板和参数由 `UpgradeCardRarityPresenter` 管。

如果你之后会经常跑 `Survivors/Upgrades/Rebuild Upgrade Card System`，请保留这几个层的名字，构建器会按名字维护它们。

## 常调参数

稀有度数据在：

`Assets/Resources/Data/UpgradeCards/Presentation/Upgrade Card Rarity Presentation Catalog.asset`

常用参数：

- `_PrimaryColor`：主色。
- `_SecondaryColor`：暗部底色。
- `_AccentColor`：扫光、边框、高亮颜色。
- `_EffectIntensity`：整体效果强度。
- `_GlowIntensity`：发光强度。
- `_PixelGrid`：像素网格密度，越小越粗颗粒。
- `_FlowSpeed`：背景流光和边框扫光速度。
- `_BorderWidth`：shader 边框宽度。
- `_BorderGlow`：边框高亮强度。
- `_EnergyDensity`：Epic 以上能量块密度。
- `_PulseSpeed`：Legendary 脉冲速度。

新手调参顺序建议：先调 `_EffectIntensity`，再调 `_GlowIntensity`，最后调 `_BorderGlow`。强度舒服以后，再去调颜色和速度。

## 当前效果分层

Common：比较安静，轻微像素底纹。

Rare：有清晰边框扫光。

Epic：边框附近会出现像素能量块。

Legendary：会有更明显的边框脉冲和角落闪光。

如果画面太吵，先降低 `Rarity Glow` 这层 Image 的 Alpha，再降低 `_GlowIntensity`。
