#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CuratedWeaponLibraryGenerator
{
    private const string SpritesDir = "Assets/Resources/Sprites/Weapons";
    private const string WeaponsDir = "Assets/Resources/Data/Weapons";
    private const string WeaponSequenceDir = "Assets/Resources/Data/WeaponAttackSequence";
    private const string WeaponListPath = "Assets/Resources/Data/Weapon Data List.asset";
    private const string MeleePrefabPath = "Assets/Resources/Prefabs/Weapons/Melee Weapon.prefab";
    private const string RangePrefabPath = "Assets/Resources/Prefabs/Weapons/Cotton Candy Gun.prefab";
    private const string PhysicalProjectilePath = "Assets/Resources/Data/Projectiles/Projectile1.asset";
    private const string ArcaneProjectilePath = "Assets/Resources/Data/Projectiles/Projectile2.asset";

    [MenuItem("Tools/Weapons/Generate Curated Weapon Library")]
    public static void Generate()
    {
        EnsureFolder(WeaponsDir);
        EnsureFolder(WeaponSequenceDir);

        Weapon meleePrefab = AssetDatabase.LoadAssetAtPath<Weapon>(MeleePrefabPath);
        Weapon rangePrefab = AssetDatabase.LoadAssetAtPath<Weapon>(RangePrefabPath);
        ProjectileDefinitionSO physicalProjectile = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(PhysicalProjectilePath);
        ProjectileDefinitionSO arcaneProjectile = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(ArcaneProjectilePath);
        WeaponDataListSO weaponList = AssetDatabase.LoadAssetAtPath<WeaponDataListSO>(WeaponListPath);

        ValidateDependency(meleePrefab, MeleePrefabPath);
        ValidateDependency(rangePrefab, RangePrefabPath);
        ValidateDependency(physicalProjectile, PhysicalProjectilePath);
        ValidateDependency(arcaneProjectile, ArcaneProjectilePath);
        ValidateDependency(weaponList, WeaponListPath);

        Dictionary<WeaponSequenceId, AttackSequenceDefinitionSO> sequences = GenerateSequences();

        int renamedCount = 0;
        int generatedCount = 0;

        foreach (WeaponSeed seed in WeaponSeeds)
        {
            string spritePath = ResolveSpritePath(seed, ref renamedCount);
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (icon == null)
            {
                Debug.LogWarning($"[CuratedWeaponLibraryGenerator] Missing sprite for {seed.AssetName} at {spritePath}");
                continue;
            }

            string assetPath = $"{WeaponsDir}/{seed.AssetName}.asset";
            WeaponDataSO weaponAsset = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(assetPath);
            if (weaponAsset == null)
            {
                weaponAsset = ScriptableObject.CreateInstance<WeaponDataSO>();
                AssetDatabase.CreateAsset(weaponAsset, assetPath);
            }

            ConfigureWeapon(
                weaponAsset,
                seed,
                icon,
                seed.Kind == WeaponKind.Melee ? meleePrefab : rangePrefab,
                sequences[seed.SequenceId],
                seed.ProjectileId == ProjectileAssetId.Arcane ? arcaneProjectile : physicalProjectile);

            EditorUtility.SetDirty(weaponAsset);
            generatedCount++;
        }

        weaponList.RefreshWeapons();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CuratedWeaponLibraryGenerator] Renamed {renamedCount} sprites, generated or updated {generatedCount} weapon assets.");
    }

    private static Dictionary<WeaponSequenceId, AttackSequenceDefinitionSO> GenerateSequences()
    {
        Dictionary<WeaponSequenceId, AttackSequenceDefinitionSO> result = new()
        {
            [WeaponSequenceId.MeleeQuickSlash] = GetOrCreateSequence("Weapon Sequence - Melee Quick Slash", sequence =>
                WeaponAnimationSequencePresets.ApplyPreset(sequence, WeaponAnimationSequencePresetId.MeleeArcSweep)),
            [WeaponSequenceId.MeleeHeavyCleave] = GetOrCreateSequence("Weapon Sequence - Melee Heavy Cleave", sequence =>
                WeaponAnimationSequencePresets.ApplyPreset(sequence, WeaponAnimationSequencePresetId.MeleeHeavySwing)),
            [WeaponSequenceId.MeleeHalfMoonSweep] = GetOrCreateSequence("Weapon Sequence - Melee Half Moon Sweep", sequence =>
                WeaponAnimationSequencePresets.ApplyPreset(sequence, WeaponAnimationSequencePresetId.MeleeArcSweepHalfMoon)),
            [WeaponSequenceId.MeleePiercingThrust] = GetOrCreateSequence("Weapon Sequence - Melee Piercing Thrust", ApplyMeleePiercingThrust),
            [WeaponSequenceId.RangedSnapShot] = GetOrCreateSequence("Weapon Sequence - Ranged Snap Shot", sequence =>
                WeaponAnimationSequencePresets.ApplyPreset(sequence, WeaponAnimationSequencePresetId.RangedRifleKick)),
            [WeaponSequenceId.RangedChargedShot] = GetOrCreateSequence("Weapon Sequence - Ranged Charged Shot", ApplyRangedChargedShot),
            [WeaponSequenceId.RangedArcanePulse] = GetOrCreateSequence("Weapon Sequence - Ranged Arcane Pulse", ApplyRangedArcanePulse)
        };

        return result;
    }

    private static AttackSequenceDefinitionSO GetOrCreateSequence(string assetName, Action<AttackSequenceDefinitionSO> configure)
    {
        string path = $"{WeaponSequenceDir}/{assetName}.asset";
        AttackSequenceDefinitionSO asset = AssetDatabase.LoadAssetAtPath<AttackSequenceDefinitionSO>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AttackSequenceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, path);
        }

        configure(asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void ApplyMeleePiercingThrust(AttackSequenceDefinitionSO sequence)
    {
        sequence.Overwrite(
            0.82f,
            true,
            new List<WeaponMotionKeyframe>
            {
                K(0f, 0f, 0f, 0f, WeaponMotionEase.Linear),
                K(0.10f, -0.12f, -0.04f, 8f, WeaponMotionEase.InSine),
                K(0.22f, -0.34f, -0.10f, 18f, WeaponMotionEase.InQuad),
                K(0.34f, -0.46f, -0.16f, 28f, WeaponMotionEase.InCubic),
                K(0.48f, -0.18f, 0.34f, 12f, WeaponMotionEase.OutExpo),
                K(0.60f, 0.02f, 0.88f, 2f, WeaponMotionEase.OutExpo),
                K(0.74f, 0.06f, 1.08f, -4f, WeaponMotionEase.OutSine),
                K(0.88f, 0f, 0.36f, -2f, WeaponMotionEase.InOutQuad),
                K(1f, 0f, 0f, 0f, WeaponMotionEase.InOutSine)
            },
            new List<WeaponSequenceEventKeyframe>
            {
                O(0.50f),
                C(0.76f),
                S(0.56f),
                V(0.60f)
            });
    }

    private static void ApplyRangedChargedShot(AttackSequenceDefinitionSO sequence)
    {
        sequence.Overwrite(
            1.08f,
            true,
            new List<WeaponMotionKeyframe>
            {
                K(0f, 0f, 0f, 0f, WeaponMotionEase.Linear),
                K(0.10f, -0.02f, 0.04f, 6f, WeaponMotionEase.InSine),
                K(0.22f, -0.08f, 0.12f, 14f, WeaponMotionEase.InQuad),
                K(0.36f, -0.16f, 0.28f, 22f, WeaponMotionEase.InCubic),
                K(0.50f, -0.18f, 0.34f, 28f, WeaponMotionEase.OutBack),
                K(0.58f, 0.04f, -0.08f, -10f, WeaponMotionEase.OutExpo),
                K(0.66f, 0.10f, -0.16f, -16f, WeaponMotionEase.OutExpo),
                K(0.78f, 0.06f, -0.06f, -8f, WeaponMotionEase.InOutQuad),
                K(0.90f, 0.02f, 0.01f, -2f, WeaponMotionEase.InQuad),
                K(1f, 0f, 0f, 0f, WeaponMotionEase.InOutSine)
            },
            new List<WeaponSequenceEventKeyframe>
            {
                P(0.56f, 0),
                S(0.56f),
                V(0.58f)
            });
    }

    private static void ApplyRangedArcanePulse(AttackSequenceDefinitionSO sequence)
    {
        sequence.Overwrite(
            0.98f,
            true,
            new List<WeaponMotionKeyframe>
            {
                K(0f, 0f, 0f, 0f, WeaponMotionEase.Linear),
                K(0.12f, -0.03f, 0.08f, 8f, WeaponMotionEase.InOutSine),
                K(0.24f, -0.08f, 0.22f, 16f, WeaponMotionEase.InQuad),
                K(0.38f, -0.04f, 0.30f, 18f, WeaponMotionEase.OutSine),
                K(0.48f, 0.02f, 0.12f, 4f, WeaponMotionEase.OutExpo),
                K(0.58f, 0.06f, -0.06f, -8f, WeaponMotionEase.OutElastic),
                K(0.72f, 0.03f, 0.02f, -3f, WeaponMotionEase.InOutQuad),
                K(1f, 0f, 0f, 0f, WeaponMotionEase.InOutSine)
            },
            new List<WeaponSequenceEventKeyframe>
            {
                P(0.46f, 0),
                S(0.44f),
                V(0.50f)
            });
    }

    private static WeaponMotionKeyframe K(float time, float x, float y, float z, WeaponMotionEase ease)
    {
        return new WeaponMotionKeyframe(time, new Vector3(x, y, 0f), new Vector3(0f, 0f, z), ease);
    }

    private static WeaponSequenceEventKeyframe O(float time)
    {
        return WeaponSequenceEventKeyframe.CreateWindowEvent(time, WeaponSequenceEventType.OpenHitWindow, 0);
    }

    private static WeaponSequenceEventKeyframe C(float time)
    {
        return WeaponSequenceEventKeyframe.CreateWindowEvent(time, WeaponSequenceEventType.CloseHitWindow, 0);
    }

    private static WeaponSequenceEventKeyframe P(float time, int eventKey)
    {
        return WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.SpawnProjectile, eventKey);
    }

    private static WeaponSequenceEventKeyframe S(float time)
    {
        return WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.PlaySfx, 0);
    }

    private static WeaponSequenceEventKeyframe V(float time)
    {
        return WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.PlayVfx, 0);
    }

    private static string ResolveSpritePath(WeaponSeed seed, ref int renamedCount)
    {
        string targetPath = $"{SpritesDir}/{seed.AssetName}.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(targetPath) != null)
        {
            return targetPath;
        }

        string sourcePath = $"{SpritesDir}/Iicon_32_{seed.SourceIndex:00}.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath) == null)
        {
            return targetPath;
        }

        string moveError = AssetDatabase.MoveAsset(sourcePath, targetPath);
        if (!string.IsNullOrEmpty(moveError))
        {
            throw new InvalidOperationException($"Failed to rename sprite {sourcePath} -> {targetPath}: {moveError}");
        }

        renamedCount++;
        return targetPath;
    }

    private static void ConfigureWeapon(
        WeaponDataSO asset,
        WeaponSeed seed,
        Sprite icon,
        Weapon weaponPrefab,
        AttackSequenceDefinitionSO attackSequence,
        ProjectileDefinitionSO projectileDefinition)
    {
        WeaponTuning tuning = GetTuning(seed);
        SerializedObject serializedObject = new(asset);

        serializedObject.FindProperty("itemName").stringValue = seed.DisplayName;
        serializedObject.FindProperty("itemIcon").objectReferenceValue = icon;
        serializedObject.FindProperty("itemPrice").intValue = tuning.Price;
        serializedObject.FindProperty("itemType").enumValueIndex = (int)ItemType.Weapon;

        serializedObject.FindProperty("weaponPrefab").objectReferenceValue = weaponPrefab;
        serializedObject.FindProperty("constructionScheme").enumValueIndex = (int)WeaponConstructionScheme.Default;
        serializedObject.FindProperty("attackSequence").objectReferenceValue = attackSequence;
        serializedObject.FindProperty("visualForwardAngle").floatValue = 45f;
        serializedObject.FindProperty("stopAimingWhenAttackReady").boolValue = seed.Kind == WeaponKind.Melee;
        serializedObject.FindProperty("attackSequenceOccupancy").floatValue = tuning.SequenceOccupancy;

        serializedObject.FindProperty("meleeHitBoxSize").vector2Value = tuning.MeleeHitBoxSize;
        serializedObject.FindProperty("meleeHitOffset").vector2Value = tuning.MeleeHitOffset;
        serializedObject.FindProperty("attack").floatValue = tuning.Attack;
        serializedObject.FindProperty("attackSpeed").floatValue = tuning.AttackSpeed;
        serializedObject.FindProperty("criticalChance").floatValue = tuning.CriticalChance;
        serializedObject.FindProperty("criticalPercent").floatValue = tuning.CriticalPercent;
        serializedObject.FindProperty("range").floatValue = tuning.Range;

        ConfigureProjectileList(serializedObject.FindProperty("sequenceProjectileList"), seed, projectileDefinition);
        serializedObject.FindProperty("sequenceSfxList").arraySize = 0;
        serializedObject.FindProperty("sequenceVfxList").arraySize = 0;

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        asset.name = seed.AssetName;
    }

    private static void ConfigureProjectileList(SerializedProperty listProperty, WeaponSeed seed, ProjectileDefinitionSO projectileDefinition)
    {
        if (seed.Kind == WeaponKind.Melee)
        {
            listProperty.arraySize = 0;
            return;
        }

        listProperty.arraySize = 1;
        SerializedProperty element = listProperty.GetArrayElementAtIndex(0);
        element.FindPropertyRelative("spawnPointIndex").intValue = 0;
        element.FindPropertyRelative("projectileDefinition").objectReferenceValue = projectileDefinition;
        element.FindPropertyRelative("burstId").intValue = seed.BurstId;
        element.FindPropertyRelative("firingMode").enumValueIndex = (int)seed.FiringMode;

        SerializedProperty patternProperty = element.FindPropertyRelative("patternConfig");
        patternProperty.FindPropertyRelative("spreadCount").intValue = seed.Pattern.SpreadCount;
        patternProperty.FindPropertyRelative("spreadAngle").floatValue = seed.Pattern.SpreadAngle;
        patternProperty.FindPropertyRelative("burstCount").intValue = seed.Pattern.BurstCount;
        patternProperty.FindPropertyRelative("burstInterval").floatValue = seed.Pattern.BurstInterval;
        patternProperty.FindPropertyRelative("novaCount").intValue = seed.Pattern.NovaCount;
    }

    private static WeaponTuning GetTuning(WeaponSeed seed)
    {
        return seed.SequenceId switch
        {
            WeaponSequenceId.MeleeQuickSlash => new WeaponTuning(18, 14f, 1.20f, 0.08f, 1.8f, 3.4f, 0.84f, new Vector2(0.70f, 1.15f), new Vector2(0f, 0.62f)),
            WeaponSequenceId.MeleeHeavyCleave => new WeaponTuning(26, 24f, 0.72f, 0.10f, 2.2f, 3.6f, 0.88f, new Vector2(1.00f, 1.55f), new Vector2(0f, 0.84f)),
            WeaponSequenceId.MeleeHalfMoonSweep => new WeaponTuning(22, 18f, 0.95f, 0.07f, 2.0f, 4.1f, 0.86f, new Vector2(0.85f, 1.40f), new Vector2(0f, 0.78f)),
            WeaponSequenceId.MeleePiercingThrust => new WeaponTuning(20, 16f, 1.08f, 0.09f, 2.0f, 4.8f, 0.82f, new Vector2(0.55f, 1.65f), new Vector2(0f, 0.92f)),
            WeaponSequenceId.RangedChargedShot => new WeaponTuning(28, 20f, 0.76f, 0.06f, 2.1f, 7.8f, 0.92f, Vector2.one, Vector2.zero),
            WeaponSequenceId.RangedArcanePulse => new WeaponTuning(24, 17f, 0.90f, 0.05f, 1.9f, 7.2f, 0.90f, Vector2.one, Vector2.zero),
            _ => new WeaponTuning(24, 15f, 1.12f, 0.08f, 2.0f, 8.5f, 0.92f, Vector2.one, Vector2.zero)
        };
    }

    private static void ValidateDependency(UnityEngine.Object asset, string path)
    {
        if (asset == null)
        {
            throw new InvalidOperationException($"Required asset is missing: {path}");
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }

    private static readonly WeaponSeed[] WeaponSeeds =
    {
        new(1, "BronzeShortsword", "青铜短剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(2, "IronLongsword", "铁阔剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(3, "DuelRapier", "决斗刺剑", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(4, "RangerSaber", "游侠弯刀", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(5, "FlintlockPistol", "燧发手枪", WeaponKind.Ranged, WeaponSequenceId.RangedSnapShot, ProjectileAssetId.Physical),
        new(6, "NobleRapier", "贵族刺剑", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(7, "Blunderbuss", "喇叭火枪", WeaponKind.Ranged, WeaponSequenceId.RangedChargedShot, ProjectileAssetId.Physical, ProjectileFiringMode.Spread, new ProjectilePatternSeed(4, 12f, 1, 0f, 1)),
        new(8, "Emberblade", "余烬长剑", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(9, "Frostbrand", "霜纹长剑", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(10, "HellfireBlade", "狱火魔刃", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(11, "CrimsonScimitar", "猩红弯刀", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(12, "AzureEdge", "苍蓝之锋", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(13, "ArcaneWand", "奥术短杖", WeaponKind.Ranged, WeaponSequenceId.RangedArcanePulse, ProjectileAssetId.Arcane, ProjectileFiringMode.Burst, new ProjectilePatternSeed(1, 0f, 3, 0.08f, 1), 1),
        new(14, "SunScepter", "曜金权杖", WeaponKind.Ranged, WeaponSequenceId.RangedArcanePulse, ProjectileAssetId.Arcane, ProjectileFiringMode.Spread, new ProjectilePatternSeed(3, 9f, 1, 0f, 1)),
        new(15, "BoneHalberd", "白骨戟", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(16, "Greatsword", "巨刃大剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(17, "CleaverAxe", "裂骨战斧", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(18, "IronSword", "铁剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(19, "HuntingSpear", "猎兵长矛", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(20, "GoldenSword", "鎏金长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(21, "FlameTongue", "炽焰剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(22, "BloodfangSword", "血牙长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(23, "RunicGreatsword", "符文大剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(24, "FlameSpear", "烈焰长枪", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(25, "NeedleRapier", "银针刺剑", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(26, "Leafblade", "叶锋短剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(27, "VerdantSword", "翠脊长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(28, "KnightSword", "骑士佩剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(29, "CrescentBlade", "新月刃", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(30, "TemplarSword", "圣堂十字剑", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(31, "MercenarySword", "佣兵长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(32, "CavalrySaber", "骑兵弯刀", WeaponKind.Melee, WeaponSequenceId.MeleeHalfMoonSweep),
        new(33, "GoldguardSword", "金柄长剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(34, "WingedSword", "翼护长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(35, "AssassinDagger", "刺客短匕", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(36, "SoldierSword", "制式长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(37, "VioletBlade", "暮紫长剑", WeaponKind.Melee, WeaponSequenceId.MeleeQuickSlash),
        new(38, "RoyalGreatsword", "王庭大剑", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave),
        new(39, "HunterDagger", "猎手短匕", WeaponKind.Melee, WeaponSequenceId.MeleePiercingThrust),
        new(40, "PhoenixFlameblade", "凤凰焰刃", WeaponKind.Melee, WeaponSequenceId.MeleeHeavyCleave)
    };

    private enum WeaponKind
    {
        Melee,
        Ranged
    }

    private enum WeaponSequenceId
    {
        MeleeQuickSlash,
        MeleeHeavyCleave,
        MeleeHalfMoonSweep,
        MeleePiercingThrust,
        RangedSnapShot,
        RangedChargedShot,
        RangedArcanePulse
    }

    private enum ProjectileAssetId
    {
        Physical,
        Arcane
    }

    private readonly struct WeaponSeed
    {
        public WeaponSeed(
            int sourceIndex,
            string assetName,
            string displayName,
            WeaponKind kind,
            WeaponSequenceId sequenceId,
            ProjectileAssetId projectileId = ProjectileAssetId.Physical,
            ProjectileFiringMode firingMode = ProjectileFiringMode.Default,
            ProjectilePatternSeed pattern = default,
            int burstId = 0)
        {
            SourceIndex = sourceIndex;
            AssetName = assetName;
            DisplayName = displayName;
            Kind = kind;
            SequenceId = sequenceId;
            ProjectileId = projectileId;
            FiringMode = firingMode;
            Pattern = pattern.IsDefault ? ProjectilePatternSeed.DefaultSingle : pattern;
            BurstId = burstId;
        }

        public int SourceIndex { get; }
        public string AssetName { get; }
        public string DisplayName { get; }
        public WeaponKind Kind { get; }
        public WeaponSequenceId SequenceId { get; }
        public ProjectileAssetId ProjectileId { get; }
        public ProjectileFiringMode FiringMode { get; }
        public ProjectilePatternSeed Pattern { get; }
        public int BurstId { get; }
    }

    private readonly struct WeaponTuning
    {
        public WeaponTuning(int price, float attack, float attackSpeed, float criticalChance, float criticalPercent, float range,
            float sequenceOccupancy, Vector2 meleeHitBoxSize, Vector2 meleeHitOffset)
        {
            Price = price;
            Attack = attack;
            AttackSpeed = attackSpeed;
            CriticalChance = criticalChance;
            CriticalPercent = criticalPercent;
            Range = range;
            SequenceOccupancy = sequenceOccupancy;
            MeleeHitBoxSize = meleeHitBoxSize;
            MeleeHitOffset = meleeHitOffset;
        }

        public int Price { get; }
        public float Attack { get; }
        public float AttackSpeed { get; }
        public float CriticalChance { get; }
        public float CriticalPercent { get; }
        public float Range { get; }
        public float SequenceOccupancy { get; }
        public Vector2 MeleeHitBoxSize { get; }
        public Vector2 MeleeHitOffset { get; }
    }

    private readonly struct ProjectilePatternSeed
    {
        public static ProjectilePatternSeed DefaultSingle => new(1, 0f, 1, 0f, 1);

        public ProjectilePatternSeed(int spreadCount, float spreadAngle, int burstCount, float burstInterval, int novaCount)
        {
            SpreadCount = spreadCount;
            SpreadAngle = spreadAngle;
            BurstCount = burstCount;
            BurstInterval = burstInterval;
            NovaCount = novaCount;
        }

        public int SpreadCount { get; }
        public float SpreadAngle { get; }
        public int BurstCount { get; }
        public float BurstInterval { get; }
        public int NovaCount { get; }
        public bool IsDefault => SpreadCount == 0 && Mathf.Approximately(SpreadAngle, 0f) && BurstCount == 0 &&
                                 Mathf.Approximately(BurstInterval, 0f) && NovaCount == 0;
    }
}
#endif
