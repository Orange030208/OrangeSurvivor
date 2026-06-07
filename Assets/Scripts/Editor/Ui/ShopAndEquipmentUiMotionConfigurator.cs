using System;
using System.Collections.Generic;
using System.IO;
using Orange.UIFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ShopAndEquipmentUiMotionConfigurator
{
    private const string MENU_PATH = "Tools/UI/Configure Shop And Equipment Motion";
    private const string SHOP_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Shop/UI Shop.prefab";
    private const string EQUIPMENT_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Equipment/Equipment.prefab";
    private const string MOTION_FOLDER_PATH = "Assets/GameContent/UI/Data/Motion";

    private const string SHOP_ROOT_MOTION_PATH = MOTION_FOLDER_PATH + "/UI Menu Root Motion.asset";
    private const string SHOP_TITLE_MOTION_PATH = MOTION_FOLDER_PATH + "/Shop Title Container Motion.asset";
    private const string SHOP_INVENTORY_MOTION_PATH = MOTION_FOLDER_PATH + "/Shop Inventory Motion.asset";
    private const string CYBER_BUTTON_MOTION_PATH = MOTION_FOLDER_PATH + "/Cyber Button Motion.asset";
    private const string EQUIPMENT_LEFT_PANEL_MOTION_PATH = MOTION_FOLDER_PATH + "/Equipment Left Panel Motion.asset";
    private const string EQUIPMENT_STAGE_MOTION_PATH = MOTION_FOLDER_PATH + "/Equipment Stage Elastic Motion.asset";
    private const string EQUIPMENT_INFO_MOTION_PATH = MOTION_FOLDER_PATH + "/Equipment Right Panel Motion.asset";
    private const string EQUIPMENT_TOP_BUTTON_MOTION_PATH = MOTION_FOLDER_PATH + "/Equipment Top Button Motion.asset";
    private const string EQUIPMENT_ACTIONS_MOTION_PATH = MOTION_FOLDER_PATH + "/Equipment Bottom Actions Motion.asset";

    private static readonly string[] ReferenceScanRoots =
    {
        "Assets",
        "Packages",
        "ProjectSettings"
    };

    [MenuItem(MENU_PATH)]
    public static void Configure()
    {
        int changedPrefabCount = 0;
        int deletedMotionAssetCount = 0;

        changedPrefabCount += ConfigureShopPrefab() ? 1 : 0;
        changedPrefabCount += ConfigureEquipmentPrefab() ? 1 : 0;
        deletedMotionAssetCount = DeleteUnusedMotionAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[ShopAndEquipmentUiMotionConfigurator] Completed. ChangedPrefabs={changedPrefabCount}, DeletedMotionAssets={deletedMotionAssetCount}");
    }

    private static bool ConfigureShopPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SHOP_PREFAB_PATH);
        bool changed = false;

        try
        {
            UISequenceDirector director = GetOrAddComponent<UISequenceDirector>(root, ref changed);
            UIMotionTransition transition = GetOrAddComponent<UIMotionTransition>(root, ref changed);
            changed |= ConfigureTransition(transition, director);

            RectTransform visualRoot = RequireRectTransform(FindByName(root.transform, "VisualRoot"), "Shop VisualRoot");
            RectTransform bar = RequireRectTransform(FindByName(visualRoot, "Bar"), "Shop Bar");
            RectTransform cat = RequireRectTransform(FindByName(bar, "Cat"), "Shop Cat");
            RectTransform pad = RequireRectTransform(FindByName(bar, "Pad"), "Shop Pad");
            RectTransform deco1 = RequireRectTransform(FindByName(visualRoot, "Deco1"), "Shop Deco1");
            RectTransform deco2 = RequireRectTransform(FindByName(visualRoot, "Deco2"), "Shop Deco2");
            RectTransform standee = RequireRectTransform(FindByName(visualRoot, "ShopStandee"), "Shop Standee");
            RectTransform content = RequireRectTransform(FindByName(visualRoot, "Content"), "Shop Content");
            RectTransform buttons = RequireRectTransform(FindByName(visualRoot, "Buttons"), "Shop Buttons");
            RectTransform reroll = RequireRectTransform(FindByName(visualRoot, "RerollContainer"), "Shop RerollContainer");

            EnsureCanvasGroup(root, ref changed);
            EnsureCanvasGroup(content.gameObject, ref changed);
            EnsureCanvasGroup(buttons.gameObject, ref changed);
            EnsureCanvasGroup(reroll.gameObject, ref changed);
            EnsureCanvasGroup(standee.gameObject, ref changed);
            EnsureCanvasGroup(bar.gameObject, ref changed);
            EnsureCanvasGroup(pad.gameObject, ref changed);
            EnsureCanvasGroup(deco1.gameObject, ref changed);
            EnsureCanvasGroup(deco2.gameObject, ref changed);

            changed |= ConfigureMotionPlayer(root, SHOP_ROOT_MOTION_PATH, bindSelfTo: visualRoot);
            changed |= ConfigureMotionPlayer(content.gameObject, SHOP_INVENTORY_MOTION_PATH);
            changed |= ConfigureMotionPlayer(buttons.gameObject, EQUIPMENT_ACTIONS_MOTION_PATH);
            changed |= ConfigureMotionPlayer(reroll.gameObject, SHOP_TITLE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(standee.gameObject, SHOP_TITLE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(bar.gameObject, SHOP_TITLE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(pad.gameObject, SHOP_TITLE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(deco1.gameObject, SHOP_TITLE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(deco2.gameObject, SHOP_TITLE_MOTION_PATH);

            changed |= EnsurePulseBreathing(cat.gameObject);

            changed |= ConfigureSequenceDirector(
                director,
                new[]
                {
                    CreateGroup("Root", 0f, 0f, true, root.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Props", 0.03f, 0.05f, true,
                        standee.GetComponent<UIMotionPlayer>(),
                        bar.GetComponent<UIMotionPlayer>(),
                        deco1.GetComponent<UIMotionPlayer>(),
                        deco2.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Inventory", 0.06f, 0f, true, content.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Actions", 0.08f, 0.04f, true,
                        buttons.GetComponent<UIMotionPlayer>(),
                        reroll.GetComponent<UIMotionPlayer>())
                },
                new[]
                {
                    CreateGroup("Actions", 0f, 0.03f, true,
                        buttons.GetComponent<UIMotionPlayer>(),
                        reroll.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Inventory", 0f, 0f, true, content.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Props", 0.02f, 0.03f, true,
                        standee.GetComponent<UIMotionPlayer>(),
                        bar.GetComponent<UIMotionPlayer>(),
                        deco1.GetComponent<UIMotionPlayer>(),
                        deco2.GetComponent<UIMotionPlayer>()),
                    CreateGroup("Root", 0.02f, 0f, true, root.GetComponent<UIMotionPlayer>())
                });

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, SHOP_PREFAB_PATH);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return changed;
    }

    private static bool ConfigureEquipmentPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(EQUIPMENT_PREFAB_PATH);
        bool changed = false;

        try
        {
            UISequenceDirector director = GetOrAddComponent<UISequenceDirector>(root, ref changed);
            UIMotionTransition transition = GetOrAddComponent<UIMotionTransition>(root, ref changed);
            changed |= ConfigureTransition(transition, director);

            RectTransform visualRoot = RequireRectTransform(FindByName(root.transform, "VisualRoot"), "Equipment VisualRoot");
            RectTransform allEquipmentPanel = RequireRectTransform(FindByName(visualRoot, "AllEquipmentPanel"), "Equipment AllEquipmentPanel");
            RectTransform selectedStage = RequireRectTransform(FindByName(visualRoot, "SelectedItemStage"), "Equipment SelectedItemStage");
            RectTransform infoPanel = RequireRectTransform(FindByName(visualRoot, "InfoPanel"), "Equipment InfoPanel");
            RectTransform closeButton = RequireRectTransform(FindByName(visualRoot, "CloseButton"), "Equipment CloseButton");
            RectTransform weaponTab = FindByName(visualRoot, "WeaponTab");
            RectTransform accessoryTab = FindByName(visualRoot, "AccessoryTab");

            EnsureCanvasGroup(root, ref changed);
            EnsureCanvasGroup(allEquipmentPanel.gameObject, ref changed);
            EnsureCanvasGroup(selectedStage.gameObject, ref changed);
            EnsureCanvasGroup(infoPanel.gameObject, ref changed);
            EnsureCanvasGroup(closeButton.gameObject, ref changed);

            GameObject actionButtons = FindActionButtonsRoot(visualRoot);
            if (actionButtons != null)
            {
                EnsureCanvasGroup(actionButtons, ref changed);
                changed |= ConfigureMotionPlayer(actionButtons, EQUIPMENT_ACTIONS_MOTION_PATH);
            }

            changed |= ConfigureMotionPlayer(root, SHOP_ROOT_MOTION_PATH, bindSelfTo: visualRoot);
            changed |= ConfigureMotionPlayer(allEquipmentPanel.gameObject, EQUIPMENT_LEFT_PANEL_MOTION_PATH);
            changed |= ConfigureMotionPlayer(selectedStage.gameObject, EQUIPMENT_STAGE_MOTION_PATH);
            changed |= ConfigureMotionPlayer(infoPanel.gameObject, EQUIPMENT_INFO_MOTION_PATH);
            changed |= ConfigureMotionPlayer(closeButton.gameObject, EQUIPMENT_TOP_BUTTON_MOTION_PATH, bindSelfTo: FindChild(closeButton, "VisualRoot"));
            if (weaponTab != null)
            {
                EnsureCanvasGroup(weaponTab.gameObject, ref changed);
                changed |= ConfigureMotionPlayer(weaponTab.gameObject, EQUIPMENT_TOP_BUTTON_MOTION_PATH, bindSelfTo: FindChild(weaponTab, "VisualRoot"));
            }

            if (accessoryTab != null)
            {
                EnsureCanvasGroup(accessoryTab.gameObject, ref changed);
                changed |= ConfigureMotionPlayer(accessoryTab.gameObject, EQUIPMENT_TOP_BUTTON_MOTION_PATH, bindSelfTo: FindChild(accessoryTab, "VisualRoot"));
            }

            changed |= ConfigureSequenceDirector(
                director,
                new[]
                {
                    // Keep the popup on one synchronized start so all regions appear together.
                    CreateGroup("EquipmentPopup", 0f, 0f, true,
                        root.GetComponent<UIMotionPlayer>(),
                        allEquipmentPanel.GetComponent<UIMotionPlayer>(),
                        selectedStage.GetComponent<UIMotionPlayer>(),
                        infoPanel.GetComponent<UIMotionPlayer>(),
                        weaponTab != null ? weaponTab.GetComponent<UIMotionPlayer>() : null,
                        accessoryTab != null ? accessoryTab.GetComponent<UIMotionPlayer>() : null,
                        closeButton.GetComponent<UIMotionPlayer>(),
                        actionButtons != null ? actionButtons.GetComponent<UIMotionPlayer>() : null)
                },
                new[]
                {
                    CreateGroup("EquipmentPopup", 0f, 0f, true,
                        root.GetComponent<UIMotionPlayer>(),
                        allEquipmentPanel.GetComponent<UIMotionPlayer>(),
                        selectedStage.GetComponent<UIMotionPlayer>(),
                        infoPanel.GetComponent<UIMotionPlayer>(),
                        weaponTab != null ? weaponTab.GetComponent<UIMotionPlayer>() : null,
                        accessoryTab != null ? accessoryTab.GetComponent<UIMotionPlayer>() : null,
                        closeButton.GetComponent<UIMotionPlayer>(),
                        actionButtons != null ? actionButtons.GetComponent<UIMotionPlayer>() : null)
                });

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, EQUIPMENT_PREFAB_PATH);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return changed;
    }

    private static int DeleteUnusedMotionAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:UIMotionDefinition", new[] { MOTION_FOLDER_PATH });
        int deletedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrWhiteSpace(assetPath) || IsMotionAssetReferenced(assetPath, guids[i]))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deletedCount++;
            }
        }

        return deletedCount;
    }

    private static bool ConfigureTransition(UIMotionTransition transition, MonoBehaviour motionSource)
    {
        SerializedObject serializedObject = new(transition);
        SerializedProperty motionSourceProperty = serializedObject.FindProperty("motionSource");
        bool changed = motionSourceProperty.objectReferenceValue != motionSource;
        if (changed)
        {
            motionSourceProperty.objectReferenceValue = motionSource;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transition);
        }

        return changed;
    }

    private static bool ConfigureMotionPlayer(GameObject gameObject, string definitionAssetPath, Transform bindSelfTo = null)
    {
        bool changed = false;
        UIMotionPlayer player = gameObject.GetComponent<UIMotionPlayer>();
        if (player == null)
        {
            player = gameObject.AddComponent<UIMotionPlayer>();
            changed = true;
        }

        SerializedObject serializedObject = new(player);
        SerializedProperty definitionProperty = serializedObject.FindProperty("definition");
        UIMotionDefinition definition = AssetDatabase.LoadAssetAtPath<UIMotionDefinition>(definitionAssetPath);
        if (definition == null)
        {
            throw new MissingReferenceException($"Missing UIMotionDefinition at '{definitionAssetPath}'.");
        }

        if (definitionProperty.objectReferenceValue != definition)
        {
            definitionProperty.objectReferenceValue = definition;
            changed = true;
        }

        if (bindSelfTo != null)
        {
            SerializedProperty bindings = serializedObject.FindProperty("targets").FindPropertyRelative("bindings");
            changed |= SetBinding(bindings, UIMotionTargetKeys.SELF, bindSelfTo);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player);
        return changed;
    }

    private static bool ConfigureSequenceDirector(
        UISequenceDirector director,
        SequenceGroupDefinition[] enterGroups,
        SequenceGroupDefinition[] exitGroups)
    {
        bool changed = false;
        SerializedObject serializedObject = new(director);
        SerializedProperty enterProperty = serializedObject.FindProperty("enterGroups");
        SerializedProperty exitProperty = serializedObject.FindProperty("exitGroups");

        changed |= WriteGroups(enterProperty, enterGroups);
        changed |= WriteGroups(exitProperty, exitGroups);

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(director);
        return changed;
    }

    private static bool IsMotionAssetReferenced(string assetPath, string assetGuid)
    {
        string normalizedAssetPath = NormalizePath(assetPath);

        foreach (string root in ReferenceScanRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = NormalizePath(files[i]);
                if (string.Equals(filePath, normalizedAssetPath, StringComparison.OrdinalIgnoreCase)
                    || filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string content;
                try
                {
                    content = File.ReadAllText(filePath);
                }
                catch (Exception)
                {
                    continue;
                }

                if (content.IndexOf(assetGuid, StringComparison.OrdinalIgnoreCase) >= 0
                    || content.IndexOf(normalizedAssetPath, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool WriteGroups(SerializedProperty groupsProperty, SequenceGroupDefinition[] definitions)
    {
        bool changed = groupsProperty.arraySize != definitions.Length;
        groupsProperty.arraySize = definitions.Length;

        for (int i = 0; i < definitions.Length; i++)
        {
            SerializedProperty groupProperty = groupsProperty.GetArrayElementAtIndex(i);
            SequenceGroupDefinition definition = definitions[i];

            changed |= SetString(groupProperty.FindPropertyRelative("name"), definition.Name);
            changed |= SetFloat(groupProperty.FindPropertyRelative("startDelay"), definition.StartDelay);
            changed |= SetFloat(groupProperty.FindPropertyRelative("stagger"), definition.Stagger);
            changed |= SetBool(groupProperty.FindPropertyRelative("playTogether"), definition.PlayTogether);

            List<MonoBehaviour> motions = definition.Motions;
            SerializedProperty motionsProperty = groupProperty.FindPropertyRelative("motions");
            if (motionsProperty.arraySize != motions.Count)
            {
                motionsProperty.arraySize = motions.Count;
                changed = true;
            }

            for (int motionIndex = 0; motionIndex < motions.Count; motionIndex++)
            {
                SerializedProperty motionProperty = motionsProperty.GetArrayElementAtIndex(motionIndex);
                if (motionProperty.objectReferenceValue != motions[motionIndex])
                {
                    motionProperty.objectReferenceValue = motions[motionIndex];
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static SequenceGroupDefinition CreateGroup(string name, float startDelay, float stagger, bool playTogether, params MonoBehaviour[] motions)
    {
        List<MonoBehaviour> list = new();
        for (int i = 0; i < motions.Length; i++)
        {
            if (motions[i] != null)
            {
                list.Add(motions[i]);
            }
        }

        return new SequenceGroupDefinition(name, startDelay, stagger, playTogether, list);
    }

    private static bool SetBinding(SerializedProperty bindings, string key, Transform target)
    {
        for (int i = 0; i < bindings.arraySize; i++)
        {
            SerializedProperty element = bindings.GetArrayElementAtIndex(i);
            SerializedProperty keyProperty = element.FindPropertyRelative("key");
            if (!string.Equals(keyProperty.stringValue, key, StringComparison.Ordinal))
            {
                continue;
            }

            SerializedProperty targetProperty = element.FindPropertyRelative("target");
            if (targetProperty.objectReferenceValue == target)
            {
                return false;
            }

            targetProperty.objectReferenceValue = target;
            return true;
        }

        int newIndex = bindings.arraySize;
        bindings.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newElement = bindings.GetArrayElementAtIndex(newIndex);
        newElement.FindPropertyRelative("key").stringValue = key;
        newElement.FindPropertyRelative("target").objectReferenceValue = target;
        return true;
    }

    private static bool EnsurePulseBreathing(GameObject gameObject)
    {
        if (gameObject.GetComponent<PulseBreathingMotion>() != null)
        {
            return false;
        }

        gameObject.AddComponent<PulseBreathingMotion>();
        return true;
    }

    private static void EnsureCanvasGroup(GameObject gameObject, ref bool changed)
    {
        if (gameObject.GetComponent<CanvasGroup>() != null)
        {
            return;
        }

        gameObject.AddComponent<CanvasGroup>();
        changed = true;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject, ref bool changed) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        component = gameObject.AddComponent<T>();
        changed = true;
        return component;
    }

    private static T GetComponentIfPresent<T>(GameObject gameObject) where T : Component
    {
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }

    private static RectTransform FindByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, StringComparison.Ordinal))
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private static GameObject FindActionButtonsRoot(Transform visualRoot)
    {
        RectTransform sellButton = FindByName(visualRoot, "SellButton");
        RectTransform mergeButton = FindByName(visualRoot, "MergeButton");

        if (sellButton != null && sellButton.parent != null && sellButton.parent == mergeButton?.parent)
        {
            return sellButton.parent.gameObject;
        }

        return null;
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        return parent != null ? FindByName(parent, childName) : null;
    }

    private static RectTransform RequireRectTransform(Transform transform, string label)
    {
        if (transform is RectTransform rectTransform)
        {
            return rectTransform;
        }

        throw new MissingReferenceException($"Missing RectTransform for {label}.");
    }

    private static bool SetString(SerializedProperty property, string value)
    {
        if (property.stringValue == value)
        {
            return false;
        }

        property.stringValue = value;
        return true;
    }

    private static bool SetFloat(SerializedProperty property, float value)
    {
        if (Mathf.Approximately(property.floatValue, value))
        {
            return false;
        }

        property.floatValue = value;
        return true;
    }

    private static bool SetBool(SerializedProperty property, bool value)
    {
        if (property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private readonly struct SequenceGroupDefinition
    {
        public SequenceGroupDefinition(string name, float startDelay, float stagger, bool playTogether, List<MonoBehaviour> motions)
        {
            Name = name;
            StartDelay = startDelay;
            Stagger = stagger;
            PlayTogether = playTogether;
            Motions = motions;
        }

        public string Name { get; }
        public float StartDelay { get; }
        public float Stagger { get; }
        public bool PlayTogether { get; }
        public List<MonoBehaviour> Motions { get; }
    }
}
