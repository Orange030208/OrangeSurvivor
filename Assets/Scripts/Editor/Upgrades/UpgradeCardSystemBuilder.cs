#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Orange.UIFramework;

public static class UpgradeCardSystemBuilder
{
    private const string AUTO_BUILD_SESSION_KEY = "Survivors.UpgradeCardSystemBuilder.AutoBuilt";
    private const string CARD_FOLDER = GameContentAssetPaths.UpgradeCards;
    private const string POOL_FOLDER = GameContentAssetPaths.UpgradePools;
    private const string POOL_PATH = GameContentAssetPaths.UpgradeCardPool;
    private const string RARITY_PRESENTATION_CATALOG_PATH = GameContentAssetPaths.CardQualityPresentationCatalog;
    private const string TEST_SCENE_PATH = "Assets/Scenes/Upgrade Card Test Scene.unity";
    private const string PLAYER_PREFAB_PATH = GameContentAssetPaths.DefaultPlayerPrefab;
    private const string TEST_CHARACTER_DATA_PATH = GameContentAssetPaths.CharactersData + "/Character1.asset";
    private const string UI_FRAMEWORK_SETTINGS_PATH = GameContentAssetPaths.UIFrameworkSettings;
    private const string UI_VIEW_CATALOG_PATH = GameContentAssetPaths.UIViewCatalog;
    private const string AUDIO_BUS_SETTINGS_PATH = GameContentAssetPaths.AudioBusSettings;
    private const string WOOD_BLOCK_SFX_PATH = GameContentAssetPaths.WoodBlockSfx;
    private const string SWIPE_SFX_PATH = GameContentAssetPaths.SwipeSfx;
    private const string SLAP_SFX_PATH = GameContentAssetPaths.SlapSfx;
    private const string UI_PAGE_FOLDER = GameContentAssetPaths.UIViewPages;
    private const string UI_CONTAINER_FOLDER = GameContentAssetPaths.UIViewContainers;

    [MenuItem("Survivors/Upgrades/Rebuild Upgrade Card System")]
    public static void RebuildUpgradeCardSystem()
    {
        EnsureFolders();
        UpgradeCardSO[] cards = BuildCards();
        ContentPoolSO pool = BuildPool(cards);
        BuildRarityPresentationCatalog();
        ConfigureAudioBusSettings();
        ConfigureViewCatalog();
        BuildTestScene(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UpgradeCardSystemBuilder] Upgrade card system assets and test scene rebuilt.");
    }

    [MenuItem("Survivors/Upgrades/Rebuild Upgrade Cards Only")]
    public static void RebuildUpgradeCardsOnly()
    {
        EnsureFolders();
        UpgradeCardSO[] cards = BuildCards();
        BuildPool(cards);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UpgradeCardSystemBuilder] {cards.Length} upgrade cards rebuilt.");
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

            if (AssetDatabase.LoadAssetAtPath<ContentPoolSO>(POOL_PATH) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(TEST_SCENE_PATH) != null)
            {
                return;
            }

            RebuildUpgradeCardSystem();
        };
    }

    private static void EnsureFolders()
    {
        EnsureFolder(GameContentAssetPaths.Root);
        EnsureFolder(GameContentAssetPaths.Upgrades);
        EnsureFolder(CARD_FOLDER);
        EnsureFolder(POOL_FOLDER);
        EnsureFolder(GameContentAssetPaths.UIMaterials);
        EnsureFolder(GameContentAssetPaths.UIShaders);
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
            CreatePropertyCard("attack_training", "攻击训练", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Attack }, "伤害提升 +8%。",
                new PropModifierData(PropType.Damage, PropModifierType.Add, 8f)),
            CreatePropertyCard("quick_strike", "快速出手", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.AttackSpeed }, "攻击速度 +10%。",
                new PropModifierData(PropType.AttackSpeed, PropModifierType.BonusMultiplier, 10f)),
            CreatePropertyCard("tough_body", "坚韧体魄", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Defense }, "最大生命 +28。",
                new PropModifierData(PropType.MaxHealth, PropModifierType.Add, 28f)),
            CreatePropertyCard("light_steps", "轻盈脚步", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.MoveSpeed }, "移动速度 +8。",
                new PropModifierData(PropType.MoveSpeed, PropModifierType.Add, 8f)),
            CreatePropertyCard("eagle_sense", "鹰眼感知", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Attack }, "攻击范围 +15%。",
                new PropModifierData(PropType.AttackRange, PropModifierType.BonusMultiplier, 15f)),
            CreatePropertyCard("battle_scavenger", "战场拾荒", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Pickup }, "拾取范围 +22%。",
                new PropModifierData(PropType.PickupRadius, PropModifierType.BonusMultiplier, 22f)),
            CreatePropertyCard("armor_reinforcement", "护甲强化", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Defense }, "护甲 +3。",
                new PropModifierData(PropType.Armor, PropModifierType.Add, 3f)),
            CreatePropertyCard("life_recovery", "生命恢复", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Recovery }, "生命恢复速度 +10。",
                new PropModifierData(PropType.HealthRecoverySpeed, PropModifierType.Add, 10f)),
            CreatePropertyCard("critical_basics", "暴击入门", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Critical }, "暴击率 +6%。",
                new PropModifierData(PropType.CriticalChance, PropModifierType.Add, 6f)),
            CreatePropertyCard("heavy_critical", "重击训练", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Critical, UpgradeCardTag.Attack }, "暴击伤害 +18%。",
                new PropModifierData(PropType.CriticalPercent, PropModifierType.Add, 18f)),
            CreatePropertyCard("lifesteal_instinct", "吸血本能", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Recovery, UpgradeCardTag.Attack }, "生命偷取 +4%。",
                new PropModifierData(PropType.LifeSteal, PropModifierType.Add, 4f)),
            CreatePropertyCard("learning_curve", "学习曲线", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Pickup }, "经验获取 +12%。",
                new PropModifierData(PropType.ExperienceGain, PropModifierType.Add, 12f)),
            CreatePropertyCard("magnetic_belt", "磁吸腰带", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Pickup }, "拾取范围 +18%，幸运 +3。",
                new PropModifierData(PropType.PickupRadius, PropModifierType.BonusMultiplier, 18f),
                new PropModifierData(PropType.Luck, PropModifierType.Add, 3f)),
            CreatePropertyCard("steady_breath", "稳定呼吸", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Critical }, "暴击率 +3%，攻击范围 +8%。",
                new PropModifierData(PropType.CriticalChance, PropModifierType.Add, 3f),
                new PropModifierData(PropType.AttackRange, PropModifierType.BonusMultiplier, 8f)),
            CreatePropertyCard("patched_armor", "拼接护甲", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Defense }, "护甲 +2，伤害减免 +2%。",
                new PropModifierData(PropType.Armor, PropModifierType.Add, 2f),
                new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 2f)),
            CreatePropertyCard("long_barrel", "加长枪管", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Ranged, UpgradeCardTag.Projectile }, "弹速 +18%，攻击范围 +12%。",
                new PropModifierData(PropType.ProjectileSpeed, PropModifierType.BonusMultiplier, 18f),
                new PropModifierData(PropType.AttackRange, PropModifierType.BonusMultiplier, 12f)),
            CreatePropertyCard("close_quarters", "贴身短打", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Melee, UpgradeCardTag.Defense }, "伤害提升 +10%，护甲 +2，攻击范围 -10%。",
                new PropModifierData(PropType.Damage, PropModifierType.Add, 10f),
                new PropModifierData(PropType.Armor, PropModifierType.Add, 2f),
                new PropModifierData(PropType.AttackRange, PropModifierType.BonusMultiplier, -10f)),
            CreatePropertyCard("momentum_engine", "动能引擎", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.AttackSpeed, UpgradeCardTag.MoveSpeed }, "攻击速度 +14%，移动速度 +5。",
                new PropModifierData(PropType.AttackSpeed, PropModifierType.BonusMultiplier, 14f),
                new PropModifierData(PropType.MoveSpeed, PropModifierType.Add, 5f)),
            CreatePropertyCard("harvest_route", "收割路线", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Pickup, UpgradeCardTag.Economy }, "拾取范围 +25%，经验获取 +10%。",
                new PropModifierData(PropType.PickupRadius, PropModifierType.BonusMultiplier, 25f),
                new PropModifierData(PropType.ExperienceGain, PropModifierType.Add, 10f)),
            CreatePropertyCard("sniper_stance", "狙击架势", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Ranged, UpgradeCardTag.Critical }, "攻击范围 +35%，暴击率 +8%，移动速度 -8%。",
                new PropModifierData(PropType.AttackRange, PropModifierType.FinalMultiplier, 35f),
                new PropModifierData(PropType.CriticalChance, PropModifierType.Add, 8f),
                new PropModifierData(PropType.MoveSpeed, PropModifierType.BonusMultiplier, -8f)),
            CreatePropertyCard("overloaded_magazine", "过载弹匣", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Projectile, UpgradeCardTag.AttackSpeed }, "投射物数量 +1，攻击速度 -12%，弹速 -10%。",
                new PropModifierData(PropType.ProjectileCount, PropModifierType.Add, 1f),
                new PropModifierData(PropType.AttackSpeed, PropModifierType.BonusMultiplier, -12f),
                new PropModifierData(PropType.ProjectileSpeed, PropModifierType.BonusMultiplier, -10f)),
            CreatePropertyCard("blood_pact", "鲜血契约", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Attack, UpgradeCardTag.Recovery, UpgradeCardTag.LowHealth }, "伤害提升 +18%，生命偷取 +3%，最大生命 -10%。",
                new PropModifierData(PropType.Damage, PropModifierType.Add, 18f),
                new PropModifierData(PropType.LifeSteal, PropModifierType.Add, 3f),
                new PropModifierData(PropType.MaxHealth, PropModifierType.FinalMultiplier, -10f)),
            CreatePropertyCard("guardian_oath", "守护誓约", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Defense, UpgradeCardTag.Recovery }, "伤害减免 +5%，治疗效果 +15%，伤害提升 -8%。",
                new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 5f),
                new PropModifierData(PropType.HealingPower, PropModifierType.Add, 15f),
                new PropModifierData(PropType.Damage, PropModifierType.Add, -8f)),
            CreatePropertyCard("glass_cannon", "玻璃大炮", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Attack }, "伤害提升 +25%，最大生命 -12%。",
                new PropModifierData(PropType.Damage, PropModifierType.Add, 25f),
                new PropModifierData(PropType.MaxHealth, PropModifierType.FinalMultiplier, -12f)),
            CreateEffectCard("weapon_focus", "武器专注", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Weapon }, "随机一把已装备武器等级 +1。",
                new UpgradeRandomEquippedWeaponCard(1)),
            CreateEffectCard("field_supplies", "战地补给", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Economy }, "立即获得 18 金币。",
                new GrantCurrencyCard(18)),
            SaveCard("lucky_stipend", "幸运津贴", UpgradeCardRarity.Common,
                new[] { UpgradeCardTag.Economy, UpgradeCardTag.Pickup }, "立即获得 10 金币，幸运 +4。",
                new[]
                {
                    new PropModifierData(PropType.Luck, PropModifierType.Add, 4f)
                },
                new FeatureEffectBase[]
                {
                    new GrantCurrencyCard(10)
                }),
            CreateEffectCard("bargain_instinct", "砍价直觉", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Economy }, "商店价格降低 10%，获得 1 次免费刷新。",
                new EconomyBonusCard(10f, 1, 0)),
            CreateEffectCard("reroll_coupon", "改签券", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Economy }, "获得 2 次免费刷新。",
                new EconomyBonusCard(0f, 2, 0)),
            SaveCard("king_ransom", "国王赎金", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Economy }, "立即获得 35 金币，商店价格降低 6%。",
                Array.Empty<PropModifierData>(),
                new FeatureEffectBase[]
                {
                    new GrantCurrencyCard(35),
                    new EconomyBonusCard(6f, 0, 0)
                }),
            CreateEffectCard("gold_contract", "赏金契约", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Economy }, "每波结束获得 5 金币。",
                new EconomyBonusCard(0f, 0, 5)),
            CreateEffectCard("battle_frenzy", "战斗兴奋", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.AttackSpeed }, "每波开始获得短暂狂乱 Buff。",
                new WaveStartBuffCard(LoadBuff("Buff_00_狂乱"), 8f, true)),
            CreateEffectCard("swift_start", "疾行开局", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.MoveSpeed }, "每波开始获得短暂迅捷 Buff。",
                new WaveStartBuffCard(LoadBuff("Buff_01_迅捷"), 8f, true)),
            CreateEffectCard("first_aid_protocol", "急救协议", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Recovery }, "立即获得 10 秒再生 Buff。",
                new ApplyBuffCard(LoadBuff("Buff_06_再生"), 10f)),
            CreateEffectCard("bloodthirst_dose", "嗜血剂量", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Recovery, UpgradeCardTag.Attack }, "立即获得 9 秒嗜血 Buff。",
                new ApplyBuffCard(LoadBuff("Buff_04_嗜血"), 9f)),
            CreateEffectCard("slaughter_rhythm", "屠戮节拍", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Attack, UpgradeCardTag.AttackSpeed }, "每波开始获得 7 秒屠戮 Buff。",
                new WaveStartBuffCard(LoadBuff("Buff_03_屠戮"), 7f, true)),
            SaveCard("emergency_core", "应急核心", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.LowHealth, UpgradeCardTag.AreaDamage, UpgradeCardTag.Defense }, "最大生命 +18。生命较低时触发一次范围爆炸。",
                new[]
                {
                    new PropModifierData(PropType.MaxHealth, PropModifierType.Add, 18f)
                },
                new FeatureEffectBase[]
                {
                    new LowHealthExplosionFeature()
                }),
            CreateEffectCard("new_weapon_cache", "武器补给箱", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Weapon }, "获得一把随机 1 级武器。",
                new AddRandomWeaponCard(null, 1)),
            CreateEffectCard("arsenal_drop", "军械空投", UpgradeCardRarity.Legendary,
                new[] { UpgradeCardTag.Weapon }, "获得一把随机 2 级武器。",
                new AddRandomWeaponCard(null, 2)),
            CreateEffectCard("duelist_blade", "决斗者刀刃", UpgradeCardRarity.Rare,
                new[] { UpgradeCardTag.Weapon, UpgradeCardTag.Melee }, "获得一把 2 级猎人匕首。",
                new AddRandomWeaponCard(LoadWeapon("HunterDagger"), 2)),
            CreateEffectCard("sun_scepter_cache", "日冕权杖", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Weapon, UpgradeCardTag.Ranged }, "获得一把 2 级日冕权杖。",
                new AddRandomWeaponCard(LoadWeapon("SunScepter"), 2)),
            CreateEffectCard("weapon_overclock", "武器超频", UpgradeCardRarity.Epic,
                new[] { UpgradeCardTag.Weapon, UpgradeCardTag.AttackSpeed }, "随机一把已装备武器等级 +2。",
                new UpgradeRandomEquippedWeaponCard(2)),
            CreateFeatureCard("sure_critical", "精准杀意", UpgradeCardRarity.Legendary,
                new[] { UpgradeCardTag.Critical, UpgradeCardTag.Attack }, "所有命中强制视为暴击。",
                new ForceCriticalFeature()),
            CreateEffectCard("immortal_second_wind", "不朽回响", UpgradeCardRarity.Legendary,
                new[] { UpgradeCardTag.Defense, UpgradeCardTag.LowHealth }, "每波开始获得 5 秒不朽 Buff。",
                new WaveStartBuffCard(LoadBuff("Buff_05_不朽"), 5f, true))
        };

        return cards.ToArray();
    }

    private static UpgradeCardSO CreatePropertyCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        UpgradeCardTag[] tags,
        string description,
        params PropModifierData[] modifiers)
    {
        return SaveCard(cardId, title, rarity, tags, description, modifiers, Array.Empty<FeatureEffectBase>());
    }

    private static UpgradeCardSO CreateEffectCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        UpgradeCardTag[] tags,
        string description,
        params FeatureEffectBase[] effects)
    {
        return SaveCard(cardId, title, rarity, tags, description, Array.Empty<PropModifierData>(), effects);
    }

    private static UpgradeCardSO CreateFeatureCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
        UpgradeCardTag[] tags,
        string description,
        params FeatureEffectBase[] features)
    {
        return SaveCard(cardId, title, rarity, tags, description, Array.Empty<PropModifierData>(), features);
    }

    private static UpgradeCardSO SaveCard(
        string cardId,
        string title,
        UpgradeCardRarity rarity,
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

        card.InitializeRuntime(cardId, title, rarity, tags, description, modifiers, features);
        EditorUtility.SetDirty(card);
        return card;
    }

    private static CardQualityPresentationCatalogSO BuildRarityPresentationCatalog()
    {
        CardQualityPresentationCatalogSO catalog =
            AssetDatabase.LoadAssetAtPath<CardQualityPresentationCatalogSO>(RARITY_PRESENTATION_CATALOG_PATH);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CardQualityPresentationCatalogSO>();
            AssetDatabase.CreateAsset(catalog, RARITY_PRESENTATION_CATALOG_PATH);
        }

        catalog.InitializeRuntime(new[]
        {
            CardQualityPresentationCatalogSO.CreateBuiltinProfile(CardQuality.Common),
            CardQualityPresentationCatalogSO.CreateBuiltinProfile(CardQuality.Rare),
            CardQualityPresentationCatalogSO.CreateBuiltinProfile(CardQuality.Epic),
            CardQualityPresentationCatalogSO.CreateBuiltinProfile(CardQuality.Legendary)
        });
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static ContentPoolSO BuildPool(IReadOnlyList<UpgradeCardSO> cards)
    {
        UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(POOL_PATH);
        ContentPoolSO pool = existingAsset as ContentPoolSO;
        if (existingAsset != null && pool == null)
        {
            AssetDatabase.DeleteAsset(POOL_PATH);
        }

        pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(POOL_PATH);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<ContentPoolSO>();
            AssetDatabase.CreateAsset(pool, POOL_PATH);
        }

        List<ContentPoolEntry> entries = new();
        for (int i = 0; cards != null && i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (card == null)
            {
                continue;
            }

            ContentPoolEntry entry = UpgradeCardContentPoolTuningUtility.CreateEntry(card);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        pool.Initialize(ContentPoolPurpose.UpgradeCard, entries, 3, false);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void ConfigureViewCatalog()
    {
        ViewCatalog catalog = AssetDatabase.LoadAssetAtPath<ViewCatalog>(UI_VIEW_CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogWarning($"[UpgradeCardSystemBuilder] Missing ViewCatalog at {UI_VIEW_CATALOG_PATH}.");
            return;
        }

        var views = new List<ViewDefinition>
        {
            CreateViewDefinition("page.menu", $"{UI_PAGE_FOLDER}/UI Menu.prefab", ViewLayer.Page),
            CreateViewDefinition("page.characterSelect", $"{UI_PAGE_FOLDER}/UI Character Selection.prefab", ViewLayer.Page),
            CreateViewDefinition("page.gaming", $"{UI_PAGE_FOLDER}/UI Gaming.prefab", ViewLayer.Hud),
            CreateViewDefinition("page.shop", $"{UI_PAGE_FOLDER}/UI Shop.prefab", ViewLayer.Page),
            CreateViewDefinition("page.pause", $"{UI_PAGE_FOLDER}/UI Pause.prefab", ViewLayer.Popup),
            CreateViewDefinition("page.gameOver", $"{UI_PAGE_FOLDER}/UI Game Over.prefab", ViewLayer.Page),
            CreateViewDefinition("page.stageComplete", $"{UI_PAGE_FOLDER}/UI Stage Complete.prefab", ViewLayer.Page),
            CreateViewDefinition(
                "popup.rewardSelection",
                $"{UI_PAGE_FOLDER}/UI Reward Selection.prefab",
                ViewLayer.Popup,
                ViewKind.Popup,
                trackInBackStack: false),
            CreateViewDefinition("page.goldBook", $"{UI_PAGE_FOLDER}/UI Gold Book.prefab", ViewLayer.Page),
            CreateViewDefinition(
                "popup.inventory.weaponOperate",
                $"{UI_PAGE_FOLDER}/Shop/Weapon Operate Popup.prefab",
                ViewLayer.Popup,
                ViewKind.Popup,
                singleton: false,
                trackInBackStack: true,
                maxCachedInstancesOverride: 1),
            CreateViewDefinition(
                "popup.inventory.accessoryInfo",
                $"{UI_PAGE_FOLDER}/Shop/Accessory Info Popup.prefab",
                ViewLayer.Popup,
                ViewKind.Popup,
                singleton: false,
                trackInBackStack: true,
                maxCachedInstancesOverride: 1),
            CreateViewDefinition(
                "tooltip.describable",
                $"{UI_CONTAINER_FOLDER}/Tooltip.prefab",
                ViewLayer.Tooltip,
                ViewKind.Tooltip,
                trackInBackStack: false,
                maxCachedInstancesOverride: 1)
        };

        SetPrivateField(catalog, "views", views);
        EditorUtility.SetDirty(catalog);
    }

    private static void ConfigureAudioBusSettings()
    {
        AudioBusSettingsSO settings = AssetDatabase.LoadAssetAtPath<AudioBusSettingsSO>(AUDIO_BUS_SETTINGS_PATH);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<AudioBusSettingsSO>();
            AssetDatabase.CreateAsset(settings, AUDIO_BUS_SETTINGS_PATH);
        }

        AudioClip woodBlock = AssetDatabase.LoadAssetAtPath<AudioClip>(WOOD_BLOCK_SFX_PATH);
        AudioClip swipe = AssetDatabase.LoadAssetAtPath<AudioClip>(SWIPE_SFX_PATH);
        AudioClip slap = AssetDatabase.LoadAssetAtPath<AudioClip>(SLAP_SFX_PATH);
        AudioSfxGroupSettings[] groups = GetPrivateField<AudioSfxGroupSettings[]>(settings, "sfxGroups") ?? Array.Empty<AudioSfxGroupSettings>();
        List<AudioSfxGroupSettings> groupList = new(groups);
        AudioSfxGroupSettings uiGroup = FindOrCreateSfxGroup(groupList, AudioConstants.UI_SFX_GROUP_ID);
        if (uiGroup == null)
        {
            return;
        }

        AudioSfxEntry[] existingEntries = GetPrivateField<AudioSfxEntry[]>(uiGroup, "sfxEntries") ?? Array.Empty<AudioSfxEntry>();
        List<AudioSfxEntry> entries = new(existingEntries);

        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardCommonReveal, woodBlock, 0.95f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardRareReveal, swipe, 1.08f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardEpicReveal, slap, 1.08f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardLegendaryReveal, swipe, 0.82f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardRareSelected, swipe, 1.04f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardEpicSelected, slap, 1.04f);
        UpsertSfxEntry(entries, AudioSfxKey.UpgradeCardLegendarySelected, slap, 0.88f);

        SetPrivateField(uiGroup, "sfxEntries", entries.ToArray());
        SetPrivateField(settings, "sfxGroups", groupList.ToArray());
        EditorUtility.SetDirty(settings);
    }

    private static AudioSfxGroupSettings FindOrCreateSfxGroup(List<AudioSfxGroupSettings> groups, string groupId)
    {
        if (groups == null)
        {
            return null;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            AudioSfxGroupSettings group = groups[i];
            if (group != null && group.GroupId == groupId)
            {
                return group;
            }
        }

        AudioSfxGroupSettings newGroup = new AudioSfxGroupSettings(groupId, 1f, 12, 10, 80, false);
        groups.Add(newGroup);
        return newGroup;
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
        SetPrivateField(entry, "groupId", AudioConstants.UI_SFX_GROUP_ID);
        entry.OnValidate();
    }

    private static ViewDefinition CreateViewDefinition(
        string id,
        string prefabPath,
        ViewLayer layer,
        ViewKind kind = ViewKind.Page,
        bool singleton = true,
        bool cacheOnClose = true,
        bool trackInBackStack = true,
        bool closeOnBackgroundClick = false,
        int warmupCount = 0,
        int maxCachedInstancesOverride = -1,
        bool allowDuplicateViewType = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[UpgradeCardSystemBuilder] Missing UI prefab at {prefabPath}.");
        }

        ViewDefinition definition = new ViewDefinition();
        SetPrivateField(definition, "id", id);
        SetPrivateField(definition, "kind", kind);
        SetPrivateField(definition, "layer", layer);
        SetPrivateField(definition, "prefab", prefab);
        SetPrivateField(definition, "singleton", singleton);
        SetPrivateField(definition, "cacheOnClose", cacheOnClose);
        SetPrivateField(definition, "trackInBackStack", trackInBackStack);
        SetPrivateField(definition, "closeOnBackgroundClick", closeOnBackgroundClick);
        SetPrivateField(definition, "warmupCount", warmupCount);
        SetPrivateField(definition, "maxCachedInstancesOverride", maxCachedInstancesOverride);
        SetPrivateField(definition, "allowDuplicateViewType", allowDuplicateViewType);
        return definition;
    }

    private static void BuildTestScene(ContentPoolSO pool)
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
        SetPrivateField(uiManager, "catalog", AssetDatabase.LoadAssetAtPath<ViewCatalog>(UI_VIEW_CATALOG_PATH));

        GameObject systems = new GameObject("Upgrade Card Test Systems");
        systems.AddComponent<RewardSelectionManager>();
        systems.AddComponent<ShopManager>();
        UpgradeCardTestSceneController controller = systems.AddComponent<UpgradeCardTestSceneController>();
        SetPrivateField(controller, "playerPrefab", LoadPrefabComponent<Player>(PLAYER_PREFAB_PATH));
        SetPrivateField(controller, "testCharacterData", AssetDatabase.LoadAssetAtPath<CharacterDataSO>(TEST_CHARACTER_DATA_PATH));
        SetPrivateField(controller, "initialUpgradePoints", 3);
        SetPrivateField(controller, "testWaveNumber", 3);
        SetPrivateField(controller, "initialGold", 80);

        EditorSceneManager.SaveScene(scene, TEST_SCENE_PATH);
        EditorSceneManager.OpenScene(TEST_SCENE_PATH);
        if (pool != null)
        {
            EditorUtility.SetDirty(pool);
        }
    }

    private static BuffDataSO LoadBuff(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:BuffDataSO", new[] { GameContentAssetPaths.CombatBuffs });
        if (guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<BuffDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static WeaponDataSO LoadWeapon(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<WeaponDataSO>($"{GameContentAssetPaths.WeaponsData}/{assetName}.asset");
    }

    private static T LoadPrefabComponent<T>(string path) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<T>() : null;
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

