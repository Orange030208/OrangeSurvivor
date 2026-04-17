#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuffAssetBatchGenerator
{
    private const string BUFF_OUTPUT_DIRECTORY = "Assets/Resources/Data/Buffs";
    private const string BUFF_ICON_ATLAS_PATH = "Assets/Resources/Sprites/Buffs/buffpack2.png";
    private const float DEFAULT_DURATION_SECONDS = 10f;
    private const int DEFAULT_MAX_STACK_COUNT = 1;

    [MenuItem("Tools/Buffs/Generate Default Buff Assets")]
    public static void GenerateDefaultBuffAssets()
    {
        EnsureOutputDirectory();

        Dictionary<int, Sprite> spritesByIndex = LoadBuffSprites();
        IReadOnlyList<BuffGenerationDefinition> definitions = CreateDefinitions();

        for (int i = 0; i < definitions.Count; i++)
        {
            BuffGenerationDefinition definition = definitions[i];
            BuffDataSO asset = LoadOrCreateAsset(definition);
            ApplyDefinition(asset, definition, spritesByIndex);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"BuffAssetBatchGenerator: 已生成/更新 {definitions.Count} 个 Buff 资产到 {BUFF_OUTPUT_DIRECTORY}");
    }

    private static void EnsureOutputDirectory()
    {
        if (AssetDatabase.IsValidFolder(BUFF_OUTPUT_DIRECTORY))
        {
            return;
        }

        const string resourcesDataDirectory = "Assets/Resources/Data";
        if (!AssetDatabase.IsValidFolder(resourcesDataDirectory))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        }

        AssetDatabase.CreateFolder(resourcesDataDirectory, "Buffs");
    }

    private static Dictionary<int, Sprite> LoadBuffSprites()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BUFF_ICON_ATLAS_PATH);
        Dictionary<int, Sprite> spritesByIndex = new();

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
            {
                continue;
            }

            string[] nameParts = sprite.name.Split('_');
            if (nameParts.Length == 0 || !int.TryParse(nameParts[^1], out int index))
            {
                continue;
            }

            spritesByIndex[index] = sprite;
        }

        return spritesByIndex;
    }

    private static BuffDataSO LoadOrCreateAsset(BuffGenerationDefinition definition)
    {
        string assetPath = $"{BUFF_OUTPUT_DIRECTORY}/{definition.AssetFileName}.asset";
        BuffDataSO asset = AssetDatabase.LoadAssetAtPath<BuffDataSO>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<BuffDataSO>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void ApplyDefinition(BuffDataSO asset, BuffGenerationDefinition definition, IReadOnlyDictionary<int, Sprite> spritesByIndex)
    {
        SerializedObject serializedObject = new(asset);

        serializedObject.FindProperty("buffId").stringValue = definition.BuffId;
        serializedObject.FindProperty("displayName").stringValue = definition.DisplayName;
        serializedObject.FindProperty("icon").objectReferenceValue = spritesByIndex.TryGetValue(definition.IconIndex, out Sprite sprite) ? sprite : null;
        serializedObject.FindProperty("polarity").enumValueIndex = (int)definition.Polarity;
        serializedObject.FindProperty("durationPolicy").enumValueIndex = (int)definition.DurationPolicy;
        serializedObject.FindProperty("durationSeconds").floatValue = definition.DurationSeconds;
        serializedObject.FindProperty("maxStackCount").intValue = definition.MaxStackCount;
        serializedObject.FindProperty("refreshMode").enumValueIndex = (int)definition.RefreshMode;
        serializedObject.FindProperty("overflowMode").enumValueIndex = (int)definition.OverflowMode;

        AssignDescriptionLines(serializedObject.FindProperty("descriptionLines"), definition.DescriptionLines);
        AssignPropertyModifiers(serializedObject.FindProperty("propertyModifiers"), definition.PropertyModifiers);
        ClearSpecialFeatures(serializedObject.FindProperty("specialFeatures"));

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static void AssignDescriptionLines(SerializedProperty property, IReadOnlyList<string> descriptionLines)
    {
        property.arraySize = descriptionLines.Count;
        for (int i = 0; i < descriptionLines.Count; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = descriptionLines[i];
        }
    }

    private static void AssignPropertyModifiers(SerializedProperty property, IReadOnlyList<PropEntry> entries)
    {
        property.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            SerializedProperty entryProperty = property.GetArrayElementAtIndex(i);
            entryProperty.FindPropertyRelative("propType").enumValueIndex = (int)entries[i].propType;
            entryProperty.FindPropertyRelative("modifierType").enumValueIndex = (int)entries[i].modifierType;
            entryProperty.FindPropertyRelative("value").floatValue = entries[i].value;
        }
    }

    private static void ClearSpecialFeatures(SerializedProperty property)
    {
        property.arraySize = 0;
    }

    private static IReadOnlyList<BuffGenerationDefinition> CreateDefinitions()
    {
        return new List<BuffGenerationDefinition>
        {
            new(
                assetFileName: "Buff_00_狂乱",
                buffId: "Buff_Frenzy",
                displayName: "狂乱",
                iconIndex: 0,
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.AttackSpeed, PropModifierType.BasePercent, 0.4f),
                    new(PropType.MoveSpeed, PropModifierType.BasePercent, -0.15f)
                }),
            new(
                assetFileName: "Buff_01_迅捷",
                buffId: "Buff_Swiftness",
                displayName: "迅捷",
                iconIndex: 1,
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.MoveSpeed, PropModifierType.BasePercent, 0.6f),
                    new(PropType.Range, PropModifierType.BasePercent, 0.2f)
                }),
            new(
                assetFileName: "Buff_02_破甲",
                buffId: "Buff_ArmorBreak",
                displayName: "破甲",
                iconIndex: 2,
                descriptionLines: new List<string>
                {
                    "对敌人护甲穿透 + 30%"
                },
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.Attack, PropModifierType.BasePercent, 0.5f)
                }),
            new(
                assetFileName: "Buff_03_屠戮",
                buffId: "Buff_Slaughter",
                displayName: "屠戮",
                iconIndex: 3,
                descriptionLines: new List<string>
                {
                    "对普通敌人伤害 + 80%"
                }),
            new(
                assetFileName: "Buff_04_嗜血",
                buffId: "Buff_Bloodthirst",
                displayName: "嗜血",
                iconIndex: 4,
                descriptionLines: new List<string>
                {
                    "击杀敌人回复 5 点生命值"
                },
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.LifeSteal, PropModifierType.Flat, 0.015f)
                }),
            new(
                assetFileName: "Buff_05_不朽",
                buffId: "Buff_Immortal",
                displayName: "不朽",
                iconIndex: 5,
                descriptionLines: new List<string>
                {
                    "濒死时获得一层吸收 500 点伤害的护盾，冷却 30 秒"
                }),
            new(
                assetFileName: "Buff_06_再生",
                buffId: "Buff_Regeneration",
                displayName: "再生",
                iconIndex: 6,
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.HealthRecoverySpeed, PropModifierType.BasePercent, 1f)
                }),
            new(
                assetFileName: "Buff_07_无敌",
                buffId: "Buff_Invincible",
                displayName: "无敌",
                iconIndex: 7,
                descriptionLines: new List<string>
                {
                    "获得 3 秒无敌时间"
                },
                propertyModifiers: new List<PropEntry>
                {
                    new(PropType.MoveSpeed, PropModifierType.BasePercent, 0.5f)
                })
        };
    }

    private sealed class BuffGenerationDefinition
    {
        public BuffGenerationDefinition(
            string assetFileName,
            string buffId,
            string displayName,
            int iconIndex,
            IReadOnlyList<string> descriptionLines = null,
            IReadOnlyList<PropEntry> propertyModifiers = null,
            BuffPolarity polarity = BuffPolarity.Positive,
            BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed,
            float durationSeconds = DEFAULT_DURATION_SECONDS,
            int maxStackCount = DEFAULT_MAX_STACK_COUNT,
            BuffRefreshMode refreshMode = BuffRefreshMode.RefreshNewestStack,
            BuffOverflowMode overflowMode = BuffOverflowMode.RefreshDurationOnly)
        {
            AssetFileName = assetFileName;
            BuffId = buffId;
            DisplayName = displayName;
            IconIndex = iconIndex;
            DescriptionLines = descriptionLines ?? new List<string>();
            PropertyModifiers = propertyModifiers ?? new List<PropEntry>();
            Polarity = polarity;
            DurationPolicy = durationPolicy;
            DurationSeconds = durationSeconds;
            MaxStackCount = maxStackCount;
            RefreshMode = refreshMode;
            OverflowMode = overflowMode;
        }

        public string AssetFileName { get; }
        public string BuffId { get; }
        public string DisplayName { get; }
        public int IconIndex { get; }
        public IReadOnlyList<string> DescriptionLines { get; }
        public IReadOnlyList<PropEntry> PropertyModifiers { get; }
        public BuffPolarity Polarity { get; }
        public BuffDurationPolicy DurationPolicy { get; }
        public float DurationSeconds { get; }
        public int MaxStackCount { get; }
        public BuffRefreshMode RefreshMode { get; }
        public BuffOverflowMode OverflowMode { get; }
    }
}
#endif
