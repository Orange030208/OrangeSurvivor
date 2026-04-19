#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WeaponAtlasBatchGenerator
{
    private const string AtlasPath = "Assets/Kawaii Survivor/Sprites/Weapons/32x32_PixelWeapons_Free.png";
    private const string WeaponsDir = "Assets/Resources/Data/Weapons";
    private const string MeleePrefabPath = "Assets/Resources/Prefabs/Weapons/Melee Weapon.prefab";
    private const string RangePrefabPath = "Assets/Resources/Prefabs/Weapons/Cotton Candy Gun.prefab";
    private const string MeleeSequencePath = "Assets/Resources/Data/Weapons/Weapon Attack Sequence.asset";
    private const string RangeSequencePath = "Assets/Resources/Data/Weapons/Gun Attack Sequence 1.asset";
    private const string ProjectileDefinitionPath = "Assets/Resources/Data/Projectiles/Projectile Common.asset";

    private static readonly string[] ArmorKeywords =
    {
        "Shield", "Helmet", "Chestplate", "Leggings", "Boots", "Greave", "Chain"
    };

    private static readonly string[] RangeKeywords =
    {
        "Bow", "Crossbow", "Pistol", "Shotgun", "Gun", "Scepter", "Staff", "Tome", "Orb"
    };

    [MenuItem("Tools/Weapons/Generate Kawaii Survivor Weapon SOs")]
    public static void Generate()
    {
        EnsureDirectory(WeaponsDir);

        Dictionary<int, Sprite> sprites = LoadSpritesByIndex();
        Weapon meleePrefab = AssetDatabase.LoadAssetAtPath<Weapon>(MeleePrefabPath);
        Weapon rangePrefab = AssetDatabase.LoadAssetAtPath<Weapon>(RangePrefabPath);
        AttackSequenceDefinitionSO meleeSequence = AssetDatabase.LoadAssetAtPath<AttackSequenceDefinitionSO>(MeleeSequencePath);
        AttackSequenceDefinitionSO rangeSequence = AssetDatabase.LoadAssetAtPath<AttackSequenceDefinitionSO>(RangeSequencePath);
        ProjectileDefinitionSO projectile = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(ProjectileDefinitionPath);

        int generated = 0;
        int skipped = 0;
        foreach (WeaponEntry entry in ParseEntries())
        {
            if (!TryClassify(entry.EnglishName, out WeaponKind kind))
            {
                skipped++;
                continue;
            }

            if (!sprites.TryGetValue(entry.Index, out Sprite icon))
            {
                Debug.LogWarning($"[WeaponAtlasBatchGenerator] Missing sprite for index {entry.Index}: {entry.EnglishName}");
                skipped++;
                continue;
            }

            string path = $"{WeaponsDir}/{entry.EnglishName}.asset";
            WeaponDataSO asset = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<WeaponDataSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Configure(asset, entry, icon, kind, meleePrefab, rangePrefab, meleeSequence, rangeSequence, projectile);
            EditorUtility.SetDirty(asset);
            generated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshWeaponLists();
        Debug.Log($"[WeaponAtlasBatchGenerator] Generated/updated {generated} weapon assets, skipped {skipped} non-weapon entries.");
    }

    private static void Configure(
        WeaponDataSO asset,
        WeaponEntry entry,
        Sprite icon,
        WeaponKind kind,
        Weapon meleePrefab,
        Weapon rangePrefab,
        AttackSequenceDefinitionSO meleeSequence,
        AttackSequenceDefinitionSO rangeSequence,
        ProjectileDefinitionSO projectile)
    {
        SerializedObject so = new(asset);
        so.FindProperty("itemName").stringValue = entry.ChineseName;
        so.FindProperty("itemIcon").objectReferenceValue = icon;
        so.FindProperty("itemPrice").intValue = 10;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.Weapon;
        so.FindProperty("weaponPrefab").objectReferenceValue = kind == WeaponKind.Melee ? meleePrefab : rangePrefab;
        so.FindProperty("constructionScheme").enumValueIndex = (int)WeaponConstructionScheme.Default;
        so.FindProperty("attackSequence").objectReferenceValue = kind == WeaponKind.Melee ? meleeSequence : rangeSequence;
        so.FindProperty("visualForwardAngle").floatValue =45f;
        so.FindProperty("stopAimingWhenAttackReady").boolValue = kind == WeaponKind.Melee;
        so.FindProperty("attackSequenceOccupancy").floatValue = 0.8f;

        SerializedProperty projectiles = so.FindProperty("projectileDefinitions");
        projectiles.arraySize = kind == WeaponKind.Range ? 1 : 0;
        if (kind == WeaponKind.Range)
        {
            projectiles.GetArrayElementAtIndex(0).objectReferenceValue = projectile;
        }

        so.FindProperty("meleeHitBoxSize").vector2Value = kind == WeaponKind.Melee ? new Vector2(0.6f, 1.4f) : new Vector2(1f, 1f);
        so.FindProperty("meleeHitOffset").vector2Value = kind == WeaponKind.Melee ? new Vector2(0f, 0.7f) : Vector2.zero;
        so.FindProperty("attack").floatValue = 10f;
        so.FindProperty("attackSpeed").floatValue = 1f;
        so.FindProperty("criticalChance").floatValue = 0.05f;
        so.FindProperty("criticalPercent").floatValue = 2f;
        so.FindProperty("range").floatValue = kind == WeaponKind.Melee ? 3f : 8f;
        so.ApplyModifiedPropertiesWithoutUndo();
        asset.name = entry.EnglishName;
    }

    private static Dictionary<int, Sprite> LoadSpritesByIndex()
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(AtlasPath)
            .OfType<Sprite>()
            .Select(sprite => new KeyValuePair<int, Sprite>(ParseSpriteIndex(sprite.name), sprite))
            .Where(x => x.Key >= 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private static int ParseSpriteIndex(string name)
    {
        const string prefix = "32x32_PixelWeapons_Free_";
        return name.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(name.Substring(prefix.Length), out int index)
            ? index
            : -1;
    }

    private static bool TryClassify(string englishName, out WeaponKind kind)
    {
        if (ContainsAny(englishName, ArmorKeywords))
        {
            kind = default;
            return false;
        }

        kind = ContainsAny(englishName, RangeKeywords) ? WeaponKind.Range : WeaponKind.Melee;
        return true;
    }

    private static bool ContainsAny(string text, IEnumerable<string> keywords)
    {
        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<WeaponEntry> ParseEntries()
    {
        string[] lines = RawEntries.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            yield return new WeaponEntry(int.Parse(parts[0]), parts[1], parts[2]);
        }
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }

    private static void RefreshWeaponLists()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponDataListSO"))
        {
            WeaponDataListSO list = AssetDatabase.LoadAssetAtPath<WeaponDataListSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (list != null) list.RefreshWeapons();
        }
    }

    private readonly struct WeaponEntry
    {
        public WeaponEntry(int index, string chineseName, string englishName)
        {
            Index = index;
            ChineseName = chineseName;
            EnglishName = englishName;
        }

        public int Index { get; }
        public string ChineseName { get; }
        public string EnglishName { get; }
    }

    private enum WeaponKind { Melee, Range }

    private const string RawEntries = @"0|木短矛|Wooden Short Spear
1|木长枪|Wooden Spear
2|木权杖|Wooden Scepter
3|木短弓|Wooden Shortbow
4|木盾|Wooden Shield
5|木三叉戟|Wooden Trident
6|木短剑|Wooden Shortsword
7|木长剑|Wooden Longsword
8|木战锤|Wooden Warhammer
9|长木弓|Wooden Longbow
10|圆木盾|Wooden Round Shield
11|骑士木盾|Knight Wooden Shield
12|木巨锤|Wooden Great Hammer
13|铁短剑|Iron Shortsword
14|铁长枪|Iron Spear
15|铁权杖|Iron Scepter
16|铁弓|Iron Bow
17|铁盾|Iron Shield
18|铁护胫|Iron Greave
19|铁弩|Iron Crossbow
20|铁头盔|Iron Helmet
21|铁胸甲|Iron Chestplate
22|铁护腿|Iron Leggings
23|铁靴子|Iron Boots
24|钢短剑|Steel Shortsword
25|钢刺剑|Steel Rapier
26|钢权杖|Steel Scepter
27|钢弓|Steel Bow
28|白银胸甲|Silver Chestplate
29|钢圆盾|Steel Round Shield
30|燧发手枪|Flintlock Pistol
31|黑铁短剑|Dark Iron Shortsword
32|黑铁长枪|Dark Iron Spear
33|黑铁权杖|Dark Iron Scepter
34|黑铁弓|Dark Iron Bow
35|黑曜石盾|Obsidian Shield
36|黑铁十字盾|Dark Iron Cross Shield
37|黑铁骑枪|Dark Iron Lance
38|黄金弯刀|Golden Scimitar
39|黄金长剑|Golden Longsword
40|黄金权杖|Golden Scepter
41|黄金弓|Golden Bow
42|黄金盾|Golden Shield
43|黄金圣盾|Golden Holy Shield
44|神圣锁链|Holy Chain
45|钻石短剑|Diamond Shortsword
46|钻石战斧|Diamond Battleaxe
47|钻石法杖|Diamond Staff
48|钻石弓|Diamond Bow
49|钻石盾|Diamond Shield
50|冰晶魔典|Ice Crystal Tome
51|冰晶法杖|Ice Crystal Staff
52|魔钢短剑|Demonic Steel Shortsword
53|魔钢战斧|Demonic Steel Battleaxe
54|魔钢战戟|Demonic Steel Halberd
55|魔钢弓|Demonic Steel Bow
56|魔钢胸甲|Demonic Steel Chestplate
57|魔能手枪|Demonic Pistol
58|翡翠短剑|Emerald Shortsword
59|翡翠战斧|Emerald Battleaxe
60|自然法杖|Nature Staff
61|自然弓|Nature Bow
62|翡翠盾|Emerald Shield
63|丛林圆盾|Jungle Round Shield
64|毒刺长矛|Poison Spear
65|炼狱短剑|Inferno Shortsword
66|炼狱战斧|Inferno Battleaxe
67|炼狱权杖|Inferno Scepter
68|炼狱弓|Inferno Bow
69|炼狱胸甲|Inferno Chestplate
70|炼狱巨斧|Inferno Great Axe
71|圣愈短剑|Holy Healing Shortsword
72|圣愈法杖|Holy Healing Staff
73|圣愈权杖|Holy Healing Scepter
74|圣愈弓|Holy Healing Bow
75|圣愈盾|Holy Healing Shield
76|圣愈匕首|Holy Healing Dagger
77|海晶短剑|Sea Crystal Shortsword
78|海晶法杖|Sea Crystal Staff
79|海晶权杖|Sea Crystal Scepter
80|海晶弓|Sea Crystal Bow
81|虚空之眼盾|Void Eye Shield
82|海晶战戟|Sea Crystal Halberd
83|青铜短剑|Bronze Shortsword
84|青铜战斧|Bronze Battleaxe
85|青铜权杖|Bronze Scepter
86|青铜弓|Bronze Bow
87|黑金纹章盾|Black Gold Emblem Shield
88|左轮手枪|Revolver
89|双管霰弹枪|Double-barreled Shotgun
90|暗影宝珠|Shadow Orb";
}
#endif
