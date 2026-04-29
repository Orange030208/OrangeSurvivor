#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UpgradeCardSystemBuilder
{
    private const string AUTO_BUILD_SESSION_KEY = "Survivors.UpgradeCardSystemBuilder.AutoBuilt";
    private const string CARD_FOLDER = "Assets/Resources/Data/UpgradeCards/Cards";
    private const string POOL_PATH = "Assets/Resources/Data/UpgradeCards/Pool/Default Upgrade Card Pool.asset";
    private const string RARITY_PRESENTATION_CATALOG_PATH = "Assets/Resources/Data/UpgradeCards/Presentation/Upgrade Card Rarity Presentation Catalog.asset";
    private const string TEST_SCENE_PATH = "Assets/Scenes/Upgrade Card Test Scene.unity";
    private const string PLAYER_PREFAB_PATH = "Assets/Resources/Prefabs/Player/Character.prefab";
    private const string TEST_CHARACTER_DATA_PATH = "Assets/Resources/Data/Characters/Character1.asset";
    private const string UI_FRAMEWORK_SETTINGS_PATH = "Assets/Resources/Data/UI/UIFrameworkSettings.asset";
    private const string UI_PREFAB_CATALOG_PATH = "Assets/Resources/Data/UI/UIPrefabCatalog.asset";
    private const string AUDIO_SFX_CATALOG_PATH = "Assets/Resources/Data/Audios/Audio Sfx Catalog.asset";
    private const string WOOD_BLOCK_SFX_PATH = "Assets/Resources/Audios/VFX/WoodBlock1.wav";
    private const string SWIPE_SFX_PATH = "Assets/Resources/Audios/VFX/Swipe.wav";
    private const string SLAP_SFX_PATH = "Assets/Resources/Audios/VFX/Slap.wav";
    private const string RARITY_EFFECT_SHADER_PATH = "Assets/Resources/Shaders/UI/UpgradeCardRarityEffect.shader";
    private const string RARITY_EFFECT_MATERIAL_PATH = "Assets/Resources/Materials/UI/UpgradeCardRarityEffect.mat";
    private const string NEW_UI_PAGE_FOLDER = "Assets/Resources/Prefabs/New UI/Pages";
    private const string UPGRADE_CONTAINER_PREFAB_PATH = "Assets/Resources/Prefabs/New UI/Pages/WaveTransition/Upgrade Container.prefab";

    [MenuItem("Survivors/Upgrades/Rebuild Upgrade Card System")]
    public static void RebuildUpgradeCardSystem()
    {
        EnsureFolders();
        UpgradeCardSO[] cards = BuildCards();
        UpgradeCardPoolSO pool = BuildPool(cards);
        BuildRarityPresentationCatalog();
        ConfigureAudioSfxCatalog();
        ConfigureUpgradeContainerPrefab();
        ConfigureUIPrefabCatalog();
        BuildTestScene(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UpgradeCardSystemBuilder] Upgrade card system assets and test scene rebuilt.");
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildWhenEditorImports()
    {
        if (SessionState.GetBool(AUTO_BUILD_SESSION_KEY, false))
        {
            return;
        }

        SessionState.SetBool(AUTO_BUILD_SESSION_KEY, true);
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UpgradeCardPoolSO>(POOL_PATH) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(TEST_SCENE_PATH) != null)
            {
                return;
            }

            RebuildUpgradeCardSystem();
        };
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources/Data/UpgradeCards");
        EnsureFolder(CARD_FOLDER);
        EnsureFolder("Assets/Resources/Data/UpgradeCards/Pool");
        EnsureFolder("Assets/Resources/Data/UpgradeCards/Presentation");
        EnsureFolder("Assets/Resources/Materials/UI");
        EnsureFolder("Assets/Resources/Shaders/UI");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static UpgradeCardSO[] BuildCards()
    {
        List<UpgradeCardSO> cards = new()
        {
            CreatePropertyCard("attack_training", "攻击训练", UpgradeCardRarity.Common, 120,
                new[] { UpgradeCardTag.Attack }, "攻击力 +5。",
                new PropModifierData(PropType.Attack, PropModifierType.Add, 5f)),
            CreatePropertyCard("quick_strike", "快速出手", UpgradeCardRarity.Common, 115,
                new[] { UpgradeCardTag.AttackSpeed }, "攻击速度 +10%。",
                new PropModifierData(PropType.AttackSpeed, PropModifierType.BonusMultiplier, 0.1f)),
            CreatePropertyCard("tough_body", "坚韧体魄", UpgradeCardRarity.Common, 110,
                new[] { UpgradeCardTag.Defense }, "最大生命 +28。",
                new PropModifierData(PropType.MaxHealth, PropModifierType.Add, 28f)),
            CreatePropertyCard("light_steps", "轻盈脚步", UpgradeCardRarity.Common, 95,
                new[] { UpgradeCardTag.MoveSpeed }, "移动速度 +0.8。",
                new PropModifierData(PropType.MoveSpeed, PropModifierType.Add, 0.8f)),
            CreatePropertyCard("eagle_sense", "鹰眼感知", UpgradeCardRarity.Common, 90,
                new[] { UpgradeCardTag.Attack }, "攻击范围 +15%。",
                new PropModifierData(PropType.AttackRange, PropModifierType.BonusMultiplier, 0.15f)),
            CreatePropertyCard("battle_scavenger", "战场拾荒", UpgradeCardRarity.Common, 90,
                new[] { UpgradeCardTag.Pickup }, "拾取范围 +22%。",
                new PropModifierData(PropType.PickupRadius, PropModifierType.BonusMultiplier, 0.22f)),
            CreatePropertyCard("armor_reinforcement", "护甲强化", UpgradeCardRarity.Common, 90,
                new[] { UpgradeCardTag.Defense }, "护甲 +3。",
                new PropModifierData(PropType.Armor, PropModifierType.Add, 3f)),
            CreatePropertyCard("life_recovery", "生命恢复", UpgradeCardRarity.Common, 75,
                new[] { UpgradeCardTag.Recovery }, "生命恢复速度 +1。",
                new PropModifierData(PropType.HealthRecoverySpeed, PropModifierType.Add, 1f)),
            CreatePropertyCard("critical_basics", "暴击入门", UpgradeCardRarity.Rare, 80,
                new[] { UpgradeCardTag.Critical }, "暴击率 +6%。",
                new PropModifierData(PropType.CriticalChance, PropModifierType.Add, 0.06f)),
            CreatePropertyCard("heavy_critical", "重击训练", UpgradeCardRarity.Rare, 70,
                new[] { UpgradeCardTag.Critical, UpgradeCardTag.Attack }, "暴击伤害 +18%。",
                new PropModifierData(PropType.CriticalPercent, PropModifierType.Add, 0.18f)),
            CreatePropertyCard("lifesteal_instinct", "吸血本能", UpgradeCardRarity.Rare, 65,
                new[] { UpgradeCardTag.Recovery, UpgradeCardTag.Attack }, "生命偷取 +4%。",
                new PropModifierData(PropType.LifeSteal, PropModifierType.Add, 0.04f)),
            CreatePropertyCard("glass_cannon", "玻璃大炮", UpgradeCardRarity.Epic, 40,
                new[] { UpgradeCardTag.Attack }, "攻击力 +25%，最大生命 -12%。",
                new PropModifierData(PropType.Attack, PropModifierType.FinalMultiplier, 0.25f),
                new PropModifierData(PropType.MaxHealth, PropModifierType.FinalMultiplier, -0.12f)),
            CreateEffectCard("weapon_focus", "武器专注", UpgradeCardRarity.Rare, 75,
                new[] { UpgradeCardTag.Weapon }, "随机一把已装备武器等级 +1。",
                new UpgradeRandomEquippedWeaponCardEffect(1)),
            CreateEffectCard("field_supplies", "战地补给", UpgradeCardRarity.Common, 70,
                new[] { UpgradeCardTag.Economy }, "立即获得 18 金币。",
                new GrantCurrencyUpgradeCardEffect(18)),
            CreateEffectCard("bargain_instinct", "砍价直觉", UpgradeCardRarity.Rare, 55,
                new[] { UpgradeCardTag.Economy }, "商店价格降低 10%，获得 1 次免费刷新。",
                new EconomyBonusUpgradeCardEffect(0.1f, 1, 0)),
            CreateEffectCard("gold_contract", "赏金契约", UpgradeCardRarity.Epic, 35,
                new[] { UpgradeCardTag.Economy }, "每波结束获得 25 金币。",
                new EconomyBonusUpgradeCardEffect(0f, 0, 25)),
            CreateEffectCard("battle_frenzy", "战斗兴奋", UpgradeCardRarity.Rare, 60,
                new[] { UpgradeCardTag.AttackSpeed }, "每波开始获得短暂狂乱 Buff。",
                new WaveStartBuffUpgradeCardEffect(LoadBuff("Buff_00_狂乱"), 8f, true)),
            CreateEffectCard("swift_start", "疾行开局", UpgradeCardRarity.Rare, 55,
                new[] { UpgradeCardTag.MoveSpeed }, "每波开始获得短暂迅捷 Buff。",
                new WaveStartBuffUpgradeCardEffect(LoadBuff("Buff_01_迅捷"), 8f, true)),
            CreateEffectCard("new_weapon_cache", "武器补给箱", UpgradeCardRarity.Epic, 30,
                new[] { UpgradeCardTag.Weapon }, "获得一把随机 1 级武器。",
                new AddRandomWeaponUpgradeCardEffect(null, 1)),
            CreateFeatureCard("sure_critical", "精准杀意", UpgradeCardRarity.Legendary, 10,
                new[] { UpgradeCardTag.Critical, UpgradeCardTag.Attack }, "所有命中强制视为暴击。",
                new ForceCriticalFeatureEffect())
        };

        ApplyCardTuning(cards);
        return cards.ToArray();
    }

    private static void ApplyCardTuning(IReadOnlyList<UpgradeCardSO> cards)
    {
        WeaponDataSO rangerSaber = LoadWeapon("RangerSaber");

        SetCardTuning(cards, "attack_training", 5, EmptyConditions());
        SetCardTuning(cards, "quick_strike", 5, EmptyConditions());
        SetCardTuning(cards, "tough_body", 4, MutuallyExclusive("glass_cannon"));
        SetCardTuning(cards, "light_steps", 4, EmptyConditions());
        SetCardTuning(cards, "eagle_sense", 4, EmptyConditions());
        SetCardTuning(cards, "battle_scavenger", 4, EmptyConditions());
        SetCardTuning(cards, "armor_reinforcement", 4, EmptyConditions());
        SetCardTuning(cards, "life_recovery", 3, EmptyConditions());
        SetCardTuning(cards, "field_supplies", 4, EmptyConditions());

        SetCardTuning(cards, "critical_basics", 3, EmptyConditions());
        SetCardTuning(cards, "heavy_critical", 2, RequiredTags(1, new UpgradeCardTagPickRequirement(UpgradeCardTag.Critical, 1)));
        SetCardTuning(cards, "lifesteal_instinct", 2, RequiredTags(2, new UpgradeCardTagPickRequirement(UpgradeCardTag.Attack, 1)));
        SetCardTuning(cards, "weapon_focus", 3, RequiredWeapon(1, rangerSaber));
        SetCardTuning(cards, "bargain_instinct", 2, RequiredTags(1, new UpgradeCardTagPickRequirement(UpgradeCardTag.Economy, 1)));
        SetCardTuning(cards, "battle_frenzy", 2, RequiredTags(2, new UpgradeCardTagPickRequirement(UpgradeCardTag.AttackSpeed, 1)));
        SetCardTuning(cards, "swift_start", 2, RequiredTags(2, new UpgradeCardTagPickRequirement(UpgradeCardTag.MoveSpeed, 1)));

        SetCardTuning(cards, "glass_cannon", 1, MutuallyExclusive("tough_body"));
        SetCardTuning(cards, "gold_contract", 1, RequiredTags(3, new UpgradeCardTagPickRequirement(UpgradeCardTag.Economy, 1)));
        SetCardTuning(cards, "new_weapon_cache", 2, MinWave(2));
        SetCardTuning(cards, "sure_critical", 1, new UpgradeCardOfferConditions(
            6,
            new[]
            {
                new UpgradeCardTagPickRequirement(UpgradeCardTag.Attack, 2),
                new UpgradeCardTagPickRequirement(UpgradeCardTag.Critical, 2)
            },
            rangerSaber != null ? new[] { rangerSaber } : Array.Empty<WeaponDataSO>(),
            Array.Empty<string>()));
    }

    private static UpgradeCardSO CreatePropertyCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        int baseWeight,
        UpgradeCardTag[] tags,
        string description,
        params PropModifierData[] modifiers)
    {
        return SaveCard(cardId, title, rarity, baseWeight, tags, description, modifiers, Array.Empty<FeatureEffectBase>());
    }

    private static UpgradeCardSO CreateEffectCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        int baseWeight,
        UpgradeCardTag[] tags,
        string description,
        params FeatureEffectBase[] effects)
    {
        return SaveCard(cardId, title, rarity, baseWeight, tags, description, Array.Empty<PropModifierData>(), effects);
    }

    private static UpgradeCardSO CreateFeatureCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        int baseWeight,
        UpgradeCardTag[] tags,
        string description,
        params FeatureEffectBase[] features)
    {
        return SaveCard(cardId, title, rarity, baseWeight, tags, description, Array.Empty<PropModifierData>(), features);
    }

    private static UpgradeCardSO SaveCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        int baseWeight,
        UpgradeCardTag[] tags,
        string description,
        IReadOnlyList<PropModifierData> modifiers,
        IReadOnlyList<FeatureEffectBase> features)
    {
        string path = $"{CARD_FOLDER}/{cardId}.asset";
        UpgradeCardSO card = AssetDatabase.LoadAssetAtPath<UpgradeCardSO>(path);
        if (card == null)
        {
            card = ScriptableObject.CreateInstance<UpgradeCardSO>();
            AssetDatabase.CreateAsset(card, path);
        }

        card.InitializeRuntime(cardId, title, rarity, baseWeight, tags, description, modifiers, features);
        EditorUtility.SetDirty(card);
        return card;
    }

    private static UpgradeCardRarityPresentationCatalogSO BuildRarityPresentationCatalog()
    {
        UpgradeCardRarityPresentationCatalogSO catalog =
            AssetDatabase.LoadAssetAtPath<UpgradeCardRarityPresentationCatalogSO>(RARITY_PRESENTATION_CATALOG_PATH);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UpgradeCardRarityPresentationCatalogSO>();
            AssetDatabase.CreateAsset(catalog, RARITY_PRESENTATION_CATALOG_PATH);
        }

        catalog.InitializeRuntime(new[]
        {
            UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(UpgradeCardRarity.Common),
            UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(UpgradeCardRarity.Rare),
            UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(UpgradeCardRarity.Epic),
            UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(UpgradeCardRarity.Legendary)
        });
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static UpgradeCardPoolSO BuildPool(IReadOnlyList<UpgradeCardSO> cards)
    {
        UpgradeCardPoolSO pool = AssetDatabase.LoadAssetAtPath<UpgradeCardPoolSO>(POOL_PATH);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<UpgradeCardPoolSO>();
            AssetDatabase.CreateAsset(pool, POOL_PATH);
        }

        pool.InitializeRuntime(cards);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void ConfigureUIPrefabCatalog()
    {
        UIPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<UIPrefabCatalog>(UI_PREFAB_CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogWarning($"[UpgradeCardSystemBuilder] Missing UIPrefabCatalog at {UI_PREFAB_CATALOG_PATH}.");
            return;
        }

        var entries = new List<UIPrefabEntry>
        {
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Menu.prefab", UILayerType.Default),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Character Selection.prefab", UILayerType.Default),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Gaming.prefab", UILayerType.SceneOverlay),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Wave Transition.prefab", UILayerType.Default),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Shop.prefab", UILayerType.Default),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Pause.prefab", UILayerType.Popup),
            CreateUIPrefabEntry($"{NEW_UI_PAGE_FOLDER}/UI Gold Book.prefab", UILayerType.Default)
        };

        SetPrivateField(catalog, "entries", entries);
        EditorUtility.SetDirty(catalog);
    }

    private static void ConfigureAudioSfxCatalog()
    {
        AudioSfxCatalogSO catalog = AssetDatabase.LoadAssetAtPath<AudioSfxCatalogSO>(AUDIO_SFX_CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogWarning($"[UpgradeCardSystemBuilder] Missing AudioSfxCatalog at {AUDIO_SFX_CATALOG_PATH}.");
            return;
        }

        AudioClip woodBlock = AssetDatabase.LoadAssetAtPath<AudioClip>(WOOD_BLOCK_SFX_PATH);
        AudioClip swipe = AssetDatabase.LoadAssetAtPath<AudioClip>(SWIPE_SFX_PATH);
        AudioClip slap = AssetDatabase.LoadAssetAtPath<AudioClip>(SLAP_SFX_PATH);
        AudioSfxEntry[] existingEntries = GetPrivateField<AudioSfxEntry[]>(catalog, "entries") ?? Array.Empty<AudioSfxEntry>();
        List<AudioSfxEntry> entries = new(existingEntries);

        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardCommonReveal, woodBlock, 0.95f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardRareReveal, swipe, 1.08f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardEpicReveal, slap, 1.08f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardLegendaryReveal, swipe, 0.82f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardRareSelected, swipe, 1.04f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardEpicSelected, slap, 1.04f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardLegendarySelected, slap, 0.88f);

        SetPrivateField(catalog, "entries", entries.ToArray());
        EditorUtility.SetDirty(catalog);
    }

    private static void UpsertSfxEntry(List<AudioSfxEntry> entries, AudioSfxKey key, AudioClip clip, float pitch)
    {
        if (entries == null || clip == null)
        {
            return;
        }

        AudioSfxEntry entry = null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].SfxKey == key)
            {
                entry = entries[i];
                break;
            }
        }

        if (entry == null)
        {
            entry = new AudioSfxEntry();
            entries.Add(entry);
        }

        SetPrivateField(entry, "sfxKey", key);
        SetPrivateField(entry, "clip", clip);
        SetPrivateField(entry, "busType", AudioBusType.Sfx);
        SetPrivateField(entry, "playbackMode", AudioPlaybackMode.OneShot);
        SetPrivateField(entry, "pitch", pitch);
        entry.OnValidate();
    }

    private static void ConfigureUpgradeContainerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(UPGRADE_CONTAINER_PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"[UpgradeCardSystemBuilder] Missing upgrade container prefab at {UPGRADE_CONTAINER_PREFAB_PATH}.");
            return;
        }

        try
        {
            UIUpgradeContainer container = prefabRoot.GetComponent<UIUpgradeContainer>();
            if (container == null)
            {
                Debug.LogWarning("[UpgradeCardSystemBuilder] Upgrade container prefab is missing UIUpgradeContainer.");
                return;
            }

            UpgradeCardRarityPresenter presenter = prefabRoot.GetComponent<UpgradeCardRarityPresenter>();
            if (presenter == null)
            {
                presenter = prefabRoot.AddComponent<UpgradeCardRarityPresenter>();
            }

            Material rarityEffectMaterial = EnsureRarityEffectMaterial();
            Image background = prefabRoot.GetComponent<Image>();
            Image rarityBackground = EnsureRarityImageLayer(prefabRoot.transform, "Rarity Background", 0, 0f);
            Image rarityBorder = EnsureRarityImageLayer(prefabRoot.transform, "Rarity Border", 1, 0.85f);
            Image rarityGlow = EnsureRarityImageLayer(prefabRoot.transform, "Rarity Glow", 2, 1.25f);

            presenter.ConfigureTargets(new[]
            {
                UpgradeCardRarityShaderTarget.Create(
                    "Card Background",
                    background,
                    rarityEffectMaterial,
                    0.75f),
                UpgradeCardRarityShaderTarget.Create(
                    "Rarity Background",
                    rarityBackground,
                    rarityEffectMaterial,
                    0.55f,
                    UpgradeCardShaderParameter.Float("_GlowIntensity", 0.2f, true),
                    UpgradeCardShaderParameter.Float("_BorderGlow", 0.25f, true)),
                UpgradeCardRarityShaderTarget.Create(
                    "Rarity Border",
                    rarityBorder,
                    rarityEffectMaterial,
                    1.1f,
                    UpgradeCardShaderParameter.Float("_GlowIntensity", 0.85f, true),
                    UpgradeCardShaderParameter.Float("_BorderWidth", 0.1f),
                    UpgradeCardShaderParameter.Float("_BorderGlow", 1f, true)),
                UpgradeCardRarityShaderTarget.Create(
                    "Rarity Glow",
                    rarityGlow,
                    rarityEffectMaterial,
                    1.35f,
                    UpgradeCardShaderParameter.Float("_GlowIntensity", 1.2f, true),
                    UpgradeCardShaderParameter.Float("_BorderWidth", 0.16f),
                    UpgradeCardShaderParameter.Float("_BorderGlow", 1.4f, true))
            });

            SetPrivateField(container, "rarityPresenter", presenter);
            SetPrivateField(container, "playRevealSfx", true);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, UPGRADE_CONTAINER_PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Material EnsureRarityEffectMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RARITY_EFFECT_SHADER_PATH);
        if (shader == null)
        {
            Debug.LogWarning($"[UpgradeCardSystemBuilder] Missing rarity effect shader at {RARITY_EFFECT_SHADER_PATH}.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(RARITY_EFFECT_MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "UpgradeCardRarityEffect"
            };
            AssetDatabase.CreateAsset(material, RARITY_EFFECT_MATERIAL_PATH);
        }

        if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetFloat("_Rarity", 0f);
        material.SetFloat("_EffectIntensity", 0.5f);
        material.SetFloat("_GlowIntensity", 0.5f);
        material.SetFloat("_PixelGrid", 48f);
        material.SetFloat("_FlowSpeed", 0.9f);
        material.SetFloat("_BorderWidth", 0.08f);
        material.SetFloat("_BorderGlow", 0.65f);
        material.SetFloat("_EnergyDensity", 12f);
        material.SetFloat("_PulseSpeed", 1.4f);
        material.SetColor("_PrimaryColor", Color.white);
        material.SetColor("_SecondaryColor", new Color(0.2f, 0.2f, 0.2f, 1f));
        material.SetColor("_AccentColor", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Image EnsureRarityImageLayer(
        Transform root,
        string childName,
        int siblingIndex,
        float alpha)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child = childObject.transform;
            child.SetParent(root, false);
        }

        child.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, root.childCount - 1));
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = child.GetComponent<Image>();
        if (image == null)
        {
            image = child.gameObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        return image;
    }

    private static UIPrefabEntry CreateUIPrefabEntry(string prefabPath, UILayerType layerType)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[UpgradeCardSystemBuilder] Missing UI prefab at {prefabPath}.");
        }

        return new UIPrefabEntry
        {
            layerType = layerType,
            prefab = prefab,
            singleton = true,
            cacheOnClose = true,
            trackInBackStack = true,
            warmupCount = 0,
            maxCachedInstancesOverride = 0
        };
    }

    private static void BuildTestScene(UpgradeCardPoolSO pool)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Upgrade Card Test Scene";

        GameObject mainCamera = new GameObject("Main Camera");
        Camera camera = mainCamera.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        mainCamera.tag = "MainCamera";

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        GameObject uiManagerObject = new GameObject("UI Manager");
        UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
        SetPrivateField(uiManager, "settings", AssetDatabase.LoadAssetAtPath<UIFrameworkSettings>(UI_FRAMEWORK_SETTINGS_PATH));
        SetPrivateField(uiManager, "catalog", AssetDatabase.LoadAssetAtPath<UIPrefabCatalog>(UI_PREFAB_CATALOG_PATH));

        GameObject systems = new GameObject("Upgrade Card Test Systems");
        systems.AddComponent<WaveTransitionManager>();
        systems.AddComponent<ShopManager>();
        UpgradeCardTestSceneController controller = systems.AddComponent<UpgradeCardTestSceneController>();
        SetPrivateField(controller, "uiManager", uiManager);
        SetPrivateField(controller, "playerPrefab", AssetDatabase.LoadAssetAtPath<Player>(PLAYER_PREFAB_PATH));
        SetPrivateField(controller, "testCharacterData", AssetDatabase.LoadAssetAtPath<CharacterDataSO>(TEST_CHARACTER_DATA_PATH));
        SetPrivateField(controller, "initialUpgradePoints", 3);
        SetPrivateField(controller, "testWaveNumber", 3);
        SetPrivateField(controller, "initialGold", 80);
        SetPrivateField(controller, "runSelfTestOnStart", true);
        SetPrivateField(controller, "selfTestTimeoutSeconds", 3f);

        EditorSceneManager.SaveScene(scene, TEST_SCENE_PATH);
        EditorSceneManager.OpenScene(TEST_SCENE_PATH);
        if (pool != null)
        {
            EditorUtility.SetDirty(pool);
        }
    }

    private static BuffDataSO LoadBuff(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:BuffDataSO", new[] { "Assets/Resources/Data/Buffs" });
        if (guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<BuffDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static WeaponDataSO LoadWeapon(string assetName)
    {
        string path = $"Assets/Resources/Data/Weapons/{assetName}.asset";
        return AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
    }

    private static void SetCardTuning(
        IReadOnlyList<UpgradeCardSO> cards,
        string cardId,
        int maxPickCount,
        UpgradeCardOfferConditions conditions)
    {
        UpgradeCardSO card = FindCard(cards, cardId);
        if (card == null)
        {
            return;
        }

        card.SetMaxPickCount(maxPickCount);
        card.SetOfferConditions(conditions);
        EditorUtility.SetDirty(card);
    }

    private static UpgradeCardSO FindCard(IReadOnlyList<UpgradeCardSO> cards, string cardId)
    {
        if (cards == null || string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (card != null && string.Equals(card.CardId, cardId, StringComparison.Ordinal))
            {
                return card;
            }
        }

        return null;
    }

    private static UpgradeCardOfferConditions EmptyConditions()
    {
        return UpgradeCardOfferConditions.Empty();
    }

    private static UpgradeCardOfferConditions MinWave(int minWave)
    {
        return new UpgradeCardOfferConditions(
            minWave,
            Array.Empty<UpgradeCardTagPickRequirement>(),
            Array.Empty<WeaponDataSO>(),
            Array.Empty<string>());
    }

    private static UpgradeCardOfferConditions RequiredTags(int minWave, params UpgradeCardTagPickRequirement[] requirements)
    {
        return new UpgradeCardOfferConditions(
            minWave,
            requirements,
            Array.Empty<WeaponDataSO>(),
            Array.Empty<string>());
    }

    private static UpgradeCardOfferConditions RequiredWeapon(int minWave, WeaponDataSO weapon)
    {
        return new UpgradeCardOfferConditions(
            minWave,
            Array.Empty<UpgradeCardTagPickRequirement>(),
            weapon != null ? new[] { weapon } : Array.Empty<WeaponDataSO>(),
            Array.Empty<string>());
    }

    private static UpgradeCardOfferConditions MutuallyExclusive(params string[] mutuallyExclusiveCardIds)
    {
        return new UpgradeCardOfferConditions(
            1,
            Array.Empty<UpgradeCardTagPickRequirement>(),
            Array.Empty<WeaponDataSO>(),
            mutuallyExclusiveCardIds);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        if (target == null)
        {
            return default;
        }

        Type type = target.GetType();
        while (type != null)
        {
            System.Reflection.FieldInfo field = type.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                object value = field.GetValue(target);
                return value is T typedValue ? typedValue : default;
            }

            type = type.BaseType;
        }

        return default;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        Type type = target.GetType();
        while (type != null)
        {
            System.Reflection.FieldInfo field = type.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                if (target is UnityEngine.Object unityObject)
                {
                    EditorUtility.SetDirty(unityObject);
                }

                return;
            }

            type = type.BaseType;
        }
    }
}
#endif
