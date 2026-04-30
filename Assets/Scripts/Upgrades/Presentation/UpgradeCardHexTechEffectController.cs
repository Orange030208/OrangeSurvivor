using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UpgradeCardHexTechEffectController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const string EFFECT_MATERIAL_PATH = "Materials/UI/UpgradeCardRarityEffect";
    private const string SHAPE_MASK_PATH = "Sprites/UI/HexTch/ParticleHexSoft_64x64";
    private const string FLOW_TEXTURE_PATH = "Sprites/UI/HexTch/BorderFlowBrushed_64x64";
    private const string NOISE_TEXTURE_PATH = "Sprites/UI/HexTch/SeamlessCloudNoise_256x256";
    private const string LINEAR_MASK_PATH = "Sprites/UI/HexTch/MaskLinearVertical_128x128";
    private const string RADIAL_MASK_PATH = "Sprites/UI/HexTch/MaskRadial_128x128";
    private const string PARTICLE_TEXTURE_PATH = "Sprites/UI/HexTch/ParticleRoundSoft_32x32";

    private static readonly int PrimaryColorId = Shader.PropertyToID("_PrimaryColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int InteractionBrightnessId = Shader.PropertyToID("_InteractionBrightness");
    private static readonly int InteractionFlowMultiplierId = Shader.PropertyToID("_InteractionFlowMultiplier");
    private static readonly int InteractionGlowMultiplierId = Shader.PropertyToID("_InteractionGlowMultiplier");
    private static readonly int SelectedAmountId = Shader.PropertyToID("_SelectedAmount");
    private static readonly int ShapeMaskTexId = Shader.PropertyToID("_ShapeMaskTex");
    private static readonly int FlowTexId = Shader.PropertyToID("_FlowTex");
    private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
    private static readonly int LinearMaskTexId = Shader.PropertyToID("_LinearMaskTex");
    private static readonly int RadialMaskTexId = Shader.PropertyToID("_RadialMaskTex");

    [Header("材质目标")]
    [SerializeField] private UpgradeCardRarityPresenter rarityPresenter;
    [SerializeField] private Graphic[] fallbackGraphics = Array.Empty<Graphic>();
    [SerializeField] private Material effectMaterialTemplate;

    [Header("状态过渡")]
    [Min(0.01f)]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private CardVisualState defaultState = new(1f, 1f, 1f, 0f);
    [SerializeField] private CardVisualState hoverState = new(1.22f, 1.7f, 1.35f, 0f);
    [SerializeField] private CardVisualState selectedState = new(1.45f, 2.35f, 1.75f, 1f);
    [Min(0f)]
    [SerializeField] private float clickBurstDuration = 0.18f;

    [Header("UGUI粒子")]
    [SerializeField] private UpgradeCardHexTechParticleGraphic orbitParticles;
    [SerializeField] private UpgradeCardHexTechParticleGraphic clickBurstParticles;

    private readonly List<RuntimeMaterialBinding> fallbackBindings = new();
    private readonly List<Material> workingMaterials = new();
    private UpgradeCardRarity currentRarity;
    private UpgradeCardRarityPresentationProfile currentProfile;
    private CardVisualState currentState;
    private CardVisualState targetState;
    private bool isHovered;
    private bool isSelected;
    private float burstTimeLeft;

    public UpgradeCardRarity CurrentRarity => currentRarity;

    public void ApplyRarity(UpgradeCardRarityPresentationProfile profile)
    {
        currentProfile = profile;
        currentRarity = profile.Rarity;
        EnsureBindings();
        RefreshWorkingMaterials();
        ApplyHexTechTextures();
        EnsureParticleGraphics();
        ConfigureParticleGraphics();
        ApplyParticleColors(profile);
        ApplyStateImmediately(ResolveTargetState());
        orbitParticles.PlayLoop();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        targetState = ResolveTargetState();
    }

    public void PlayClickBurst()
    {
        burstTimeLeft = clickBurstDuration;
        targetState = selectedState;

        if (clickBurstParticles == null)
        {
            return;
        }

        ApplyParticleColors(currentProfile);
        clickBurstParticles.PlayBurst();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetState = ResolveTargetState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetState = ResolveTargetState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        PlayClickBurst();
    }

    private void Awake()
    {
        EnsureBindings();
        EnsureParticleGraphics();
        ConfigureParticleGraphics();
    }

    private void OnEnable()
    {
        targetState = ResolveTargetState();
        ApplyStateImmediately(targetState);
        if (orbitParticles != null)
        {
            orbitParticles.PlayLoop();
        }
    }

    private void Update()
    {
        if (burstTimeLeft > 0f)
        {
            burstTimeLeft -= Time.unscaledDeltaTime;
            if (burstTimeLeft <= 0f)
            {
                targetState = ResolveTargetState();
            }
        }

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        currentState = CardVisualState.Lerp(currentState, targetState, t);
        ApplyState(currentState);
    }

    private void OnDisable()
    {
        if (orbitParticles != null)
        {
            orbitParticles.Stop();
        }

        if (clickBurstParticles != null)
        {
            clickBurstParticles.Stop();
        }
    }

    private void OnDestroy()
    {
        ReleaseFallbackBindings();
    }

    private void EnsureBindings()
    {
        if (rarityPresenter == null)
        {
            rarityPresenter = GetComponent<UpgradeCardRarityPresenter>();
        }

        if (effectMaterialTemplate == null)
        {
            effectMaterialTemplate = Resources.Load<Material>(EFFECT_MATERIAL_PATH);
        }

        if (rarityPresenter != null && rarityPresenter.ConfiguredTargetCount > 0)
        {
            ReleaseFallbackBindings();
            return;
        }

        if (fallbackBindings.Count > 0 || effectMaterialTemplate == null)
        {
            return;
        }

        Graphic[] graphics = ResolveFallbackGraphics();
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            Material originalMaterial = graphic.material;
            Material material = new(effectMaterialTemplate)
            {
                name = $"{effectMaterialTemplate.name} ({graphic.name})"
            };

            graphic.material = material;
            fallbackBindings.Add(new RuntimeMaterialBinding(graphic, originalMaterial, material));
        }
    }

    private Graphic[] ResolveFallbackGraphics()
    {
        if (fallbackGraphics != null && fallbackGraphics.Length > 0)
        {
            return fallbackGraphics;
        }

        Graphic selfGraphic = GetComponent<Graphic>();
        return selfGraphic != null ? new[] { selfGraphic } : Array.Empty<Graphic>();
    }

    private void RefreshWorkingMaterials()
    {
        workingMaterials.Clear();

        if (rarityPresenter != null)
        {
            IReadOnlyList<Material> presenterMaterials = rarityPresenter.RuntimeMaterials;
            for (int i = 0; i < presenterMaterials.Count; i++)
            {
                AddMaterialIfValid(presenterMaterials[i]);
            }
        }

        for (int i = 0; i < fallbackBindings.Count; i++)
        {
            AddMaterialIfValid(fallbackBindings[i].RuntimeMaterial);
        }
    }

    private void AddMaterialIfValid(Material material)
    {
        if (material == null || workingMaterials.Contains(material))
        {
            return;
        }

        workingMaterials.Add(material);
    }

    private void ApplyHexTechTextures()
    {
        Sprite shapeMask = Resources.Load<Sprite>(SHAPE_MASK_PATH);
        Sprite flowTexture = Resources.Load<Sprite>(FLOW_TEXTURE_PATH);
        Sprite noiseTexture = Resources.Load<Sprite>(NOISE_TEXTURE_PATH);
        Sprite linearMask = Resources.Load<Sprite>(LINEAR_MASK_PATH);
        Sprite radialMask = Resources.Load<Sprite>(RADIAL_MASK_PATH);

        for (int i = 0; i < workingMaterials.Count; i++)
        {
            Material material = workingMaterials[i];
            SetTexture(material, ShapeMaskTexId, shapeMask);
            SetTexture(material, FlowTexId, flowTexture);
            SetTexture(material, NoiseTexId, noiseTexture);
            SetTexture(material, LinearMaskTexId, linearMask);
            SetTexture(material, RadialMaskTexId, radialMask);
        }
    }

    private static void SetTexture(Material material, int propertyId, Sprite sprite)
    {
        if (material == null || sprite == null || !material.HasProperty(propertyId))
        {
            return;
        }

        material.SetTexture(propertyId, sprite.texture);
    }

    private void ApplyStateImmediately(CardVisualState state)
    {
        currentState = state;
        targetState = state;
        ApplyState(state);
    }

    private void ApplyState(CardVisualState state)
    {
        for (int i = 0; i < workingMaterials.Count; i++)
        {
            Material material = workingMaterials[i];
            SetFloatIfPresent(material, InteractionBrightnessId, state.Brightness);
            SetFloatIfPresent(material, InteractionFlowMultiplierId, state.FlowMultiplier);
            SetFloatIfPresent(material, InteractionGlowMultiplierId, state.GlowMultiplier);
            SetFloatIfPresent(material, SelectedAmountId, state.SelectedAmount);
        }
    }

    private CardVisualState ResolveTargetState()
    {
        if (isSelected)
        {
            return selectedState;
        }

        return isHovered ? hoverState : defaultState;
    }

    private void EnsureParticleGraphics()
    {
        if (orbitParticles == null)
        {
            orbitParticles = CreateParticleGraphic("Card HexTech Orbit Particles");
        }

        if (clickBurstParticles == null)
        {
            clickBurstParticles = CreateParticleGraphic("Card HexTech Burst Particles");
        }
    }

    private UpgradeCardHexTechParticleGraphic CreateParticleGraphic(string objectName)
    {
        GameObject particleObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = particleObject.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.SetAsLastSibling();
        UpgradeCardHexTechParticleGraphic particleGraphic = particleObject.AddComponent<UpgradeCardHexTechParticleGraphic>();
        particleGraphic.raycastTarget = false;
        return particleGraphic;
    }

    private void ConfigureParticleGraphics()
    {
        Sprite particleSprite = Resources.Load<Sprite>(PARTICLE_TEXTURE_PATH);
        if (orbitParticles != null)
        {
            orbitParticles.Configure(UpgradeCardHexTechParticleGraphic.ParticleMode.Orbit, particleSprite);
        }

        if (clickBurstParticles != null)
        {
            clickBurstParticles.Configure(UpgradeCardHexTechParticleGraphic.ParticleMode.Burst, particleSprite);
        }
    }

    private void ApplyParticleColors(UpgradeCardRarityPresentationProfile profile)
    {
        Color primaryColor = ResolveProfileColor(profile, PrimaryColorId, Color.white);
        Color accentColor = ResolveProfileColor(profile, AccentColorId, primaryColor);

        if (orbitParticles != null)
        {
            orbitParticles.SetColors(primaryColor, accentColor);
        }

        if (clickBurstParticles != null)
        {
            clickBurstParticles.SetColors(accentColor, Color.white);
        }
    }

    private static Color ResolveProfileColor(UpgradeCardRarityPresentationProfile profile, int propertyId, Color fallback)
    {
        UpgradeCardShaderParameter[] parameters = profile.ShaderParameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            UpgradeCardShaderParameter parameter = parameters[i];
            if (parameter.Type == UpgradeCardShaderParameter.ParameterType.Color &&
                Shader.PropertyToID(parameter.PropertyName) == propertyId)
            {
                return parameter.ColorValue;
            }
        }

        return fallback;
    }

    private static void SetFloatIfPresent(Material material, int propertyId, float value)
    {
        if (material != null && material.HasProperty(propertyId))
        {
            material.SetFloat(propertyId, value);
        }
    }

    private void ReleaseFallbackBindings()
    {
        for (int i = 0; i < fallbackBindings.Count; i++)
        {
            RuntimeMaterialBinding binding = fallbackBindings[i];
            if (binding.Target != null)
            {
                binding.Target.material = binding.OriginalMaterial;
            }

            if (binding.RuntimeMaterial != null)
            {
                DestroyMaterial(binding.RuntimeMaterial);
            }
        }

        fallbackBindings.Clear();
    }

    private static void DestroyMaterial(Material material)
    {
        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }

    [Serializable]
    private struct CardVisualState
    {
        [SerializeField] private float brightness;
        [SerializeField] private float flowMultiplier;
        [SerializeField] private float glowMultiplier;
        [SerializeField] private float selectedAmount;

        public CardVisualState(float brightness, float flowMultiplier, float glowMultiplier, float selectedAmount)
        {
            this.brightness = Mathf.Max(0f, brightness);
            this.flowMultiplier = Mathf.Max(0f, flowMultiplier);
            this.glowMultiplier = Mathf.Max(0f, glowMultiplier);
            this.selectedAmount = Mathf.Clamp01(selectedAmount);
        }

        public float Brightness => Mathf.Max(0f, brightness);
        public float FlowMultiplier => Mathf.Max(0f, flowMultiplier);
        public float GlowMultiplier => Mathf.Max(0f, glowMultiplier);
        public float SelectedAmount => Mathf.Clamp01(selectedAmount);

        public static CardVisualState Lerp(CardVisualState from, CardVisualState to, float t)
        {
            t = Mathf.Clamp01(t);
            return new CardVisualState(
                Mathf.Lerp(from.Brightness, to.Brightness, t),
                Mathf.Lerp(from.FlowMultiplier, to.FlowMultiplier, t),
                Mathf.Lerp(from.GlowMultiplier, to.GlowMultiplier, t),
                Mathf.Lerp(from.SelectedAmount, to.SelectedAmount, t));
        }
    }

    private readonly struct RuntimeMaterialBinding
    {
        public RuntimeMaterialBinding(Graphic target, Material originalMaterial, Material runtimeMaterial)
        {
            Target = target;
            OriginalMaterial = originalMaterial;
            RuntimeMaterial = runtimeMaterial;
        }

        public Graphic Target { get; }
        public Material OriginalMaterial { get; }
        public Material RuntimeMaterial { get; }
    }
}
