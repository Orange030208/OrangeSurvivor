#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BatchNameEditorWindow : EditorWindow
{
    private enum CaseMode
    {
        None,
        Lower,
        Upper,
        Title
    }

    private enum NumberPlacement
    {
        Prefix,
        Suffix
    }

    private const float MinWindowWidth = 720f;
    private const float MinWindowHeight = 560f;
    private const float DropAreaHeight = 64f;
    private const float ListHeight = 250f;

    private readonly List<UnityEngine.Object> targets = new();
    private Vector2 mainScroll;
    private Vector2 listScroll;

    private bool useBaseName;
    private string baseName = "NewName";
    private string prefix = string.Empty;
    private string suffix = string.Empty;
    private bool useReplace;
    private string replaceFrom = string.Empty;
    private string replaceTo = string.Empty;
    private bool trimWhitespace = true;
    private CaseMode caseMode = CaseMode.None;

    private bool useNumbering = true;
    private NumberPlacement numberPlacement = NumberPlacement.Suffix;
    private int numberStart = 1;
    private int numberStep = 1;
    private int numberPadding = 2;
    private string numberSeparator = "_";

    [MenuItem("Tools/Orange Editor/Batch Name Editor")]
    private static void OpenWindow()
    {
        BatchNameEditorWindow window = GetWindow<BatchNameEditorWindow>();
        window.titleContent = new GUIContent("Batch Name Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        window.Show();
    }

    private void OnGUI()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        DrawHeader();
        DrawDropArea();
        DrawToolbar();
        DrawRuleSection();
        DrawTargetList();
        DrawApplySection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Batch Name Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Batch rename selected assets or scene objects. Asset renaming uses AssetDatabase, scene object renaming uses Undo.",
            MessageType.Info);
    }

    private void DrawDropArea()
    {
        Rect area = GUILayoutUtility.GetRect(0f, DropAreaHeight, GUILayout.ExpandWidth(true));
        GUI.Box(area, "Drag assets or scene objects here");

        Event e = Event.current;
        if (!area.Contains(e.mousePosition))
        {
            return;
        }

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
            return;
        }

        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddTargets(DragAndDrop.objectReferences);
            e.Use();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Selected", GUILayout.Height(26f)))
        {
            AddTargets(Selection.objects);
        }

        using (new EditorGUI.DisabledScope(targets.Count == 0))
        {
            if (GUILayout.Button("Sort By Name", GUILayout.Width(120f), GUILayout.Height(26f)))
            {
                SortTargetsByName();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(90f), GUILayout.Height(26f)))
            {
                targets.Clear();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRuleSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);

        useBaseName = EditorGUILayout.Toggle("Use Base Name", useBaseName);
        using (new EditorGUI.DisabledScope(!useBaseName))
        {
            baseName = EditorGUILayout.TextField("Base Name", baseName);
        }

        prefix = EditorGUILayout.TextField("Prefix", prefix);
        suffix = EditorGUILayout.TextField("Suffix", suffix);

        useReplace = EditorGUILayout.Toggle("Find And Replace", useReplace);
        using (new EditorGUI.DisabledScope(!useReplace))
        {
            replaceFrom = EditorGUILayout.TextField("Find", replaceFrom);
            replaceTo = EditorGUILayout.TextField("Replace With", replaceTo);
        }

        trimWhitespace = EditorGUILayout.Toggle("Trim Whitespace", trimWhitespace);
        caseMode = (CaseMode)EditorGUILayout.EnumPopup("Case", caseMode);

        EditorGUILayout.Space(6f);
        useNumbering = EditorGUILayout.Toggle("Use Numbering", useNumbering);
        using (new EditorGUI.DisabledScope(!useNumbering))
        {
            numberPlacement = (NumberPlacement)EditorGUILayout.EnumPopup("Number Placement", numberPlacement);
            numberStart = EditorGUILayout.IntField("Start", numberStart);
            numberStep = Mathf.Max(1, EditorGUILayout.IntField("Step", numberStep));
            numberPadding = Mathf.Clamp(EditorGUILayout.IntField("Padding", numberPadding), 0, 12);
            numberSeparator = EditorGUILayout.TextField("Separator", numberSeparator);
        }
    }

    private void DrawTargetList()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Targets ({targets.Count})", EditorStyles.boldLabel);

        if (targets.Count == 0)
        {
            EditorGUILayout.HelpBox("No targets. Add current selection or drag objects into the window.", MessageType.Warning);
            return;
        }

        List<RenamePreview> previews = BuildPreviews();
        HashSet<string> duplicateKeys = FindDuplicateAssetKeys(previews);
        int removeIndex = -1;

        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(ListHeight));

        for (int i = 0; i < previews.Count; i++)
        {
            RenamePreview preview = previews[i];
            bool hasError = !string.IsNullOrEmpty(preview.error) || duplicateKeys.Contains(preview.assetKey);

            EditorGUILayout.BeginHorizontal();
            targets[i] = EditorGUILayout.ObjectField(targets[i], typeof(UnityEngine.Object), true);
            GUILayout.Label(preview.originalName, GUILayout.Width(170f));
            GUILayout.Label("->", GUILayout.Width(22f));

            GUIStyle previewStyle = hasError ? EditorStyles.boldLabel : EditorStyles.label;
            GUILayout.Label(preview.newName, previewStyle, GUILayout.Width(190f));

            if (GUILayout.Button("Ping", GUILayout.Width(52f)))
            {
                EditorGUIUtility.PingObject(targets[i]);
            }

            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            if (hasError)
            {
                string message = string.IsNullOrEmpty(preview.error) ? "Duplicate asset name in the same folder." : preview.error;
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
        }

        EditorGUILayout.EndScrollView();

        if (removeIndex >= 0)
        {
            targets.RemoveAt(removeIndex);
        }
    }

    private void DrawApplySection()
    {
        EditorGUILayout.Space(10f);

        List<RenamePreview> previews = BuildPreviews();
        bool canApply = previews.Count > 0 && previews.All(p => string.IsNullOrEmpty(p.error)) && FindDuplicateAssetKeys(previews).Count == 0;

        using (new EditorGUI.DisabledScope(!canApply))
        {
            if (GUILayout.Button("Apply Rename", GUILayout.Height(36f)))
            {
                ApplyRename(previews);
            }
        }
    }

    private void AddTargets(UnityEngine.Object[] objects)
    {
        foreach (UnityEngine.Object obj in objects)
        {
            if (obj == null || targets.Contains(obj))
            {
                continue;
            }

            targets.Add(obj);
        }
    }

    private void SortTargetsByName()
    {
        targets.Sort((a, b) => string.Compare(GetSortKey(a), GetSortKey(b), StringComparison.OrdinalIgnoreCase));
    }

    private List<RenamePreview> BuildPreviews()
    {
        List<RenamePreview> previews = new();

        int number = numberStart;
        for (int i = 0; i < targets.Count; i++)
        {
            UnityEngine.Object target = targets[i];
            RenamePreview preview = BuildPreview(target, number);
            previews.Add(preview);

            if (target != null)
            {
                number += numberStep;
            }
        }

        return previews;
    }

    private RenamePreview BuildPreview(UnityEngine.Object target, int number)
    {
        if (target == null)
        {
            return RenamePreview.Invalid("Missing target.");
        }

        string originalName = target.name;
        string newName = useBaseName ? baseName : originalName;

        if (useReplace && !string.IsNullOrEmpty(replaceFrom))
        {
            newName = newName.Replace(replaceFrom, replaceTo);
        }

        newName = prefix + newName + suffix;

        if (trimWhitespace)
        {
            newName = newName.Trim();
        }

        newName = ApplyCase(newName);

        if (useNumbering)
        {
            string numberText = numberPadding > 0 ? number.ToString($"D{numberPadding}") : number.ToString();
            newName = numberPlacement == NumberPlacement.Prefix
                ? numberText + numberSeparator + newName
                : newName + numberSeparator + numberText;
        }

        string error = ValidateName(target, newName);
        string assetKey = GetAssetKey(target, newName);
        return new RenamePreview(originalName, newName, error, assetKey);
    }

    private string ApplyCase(string value)
    {
        switch (caseMode)
        {
            case CaseMode.Lower:
                return value.ToLowerInvariant();
            case CaseMode.Upper:
                return value.ToUpperInvariant();
            case CaseMode.Title:
                return string.Join(" ", value.Split(' ').Select(ToTitleWord));
            default:
                return value;
        }
    }

    private static string ToTitleWord(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToUpperInvariant();
        }

        return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
    }

    private static string ValidateName(UnityEngine.Object target, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return "Name cannot be empty.";
        }

        string assetPath = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return newName.IndexOfAny(invalidChars) >= 0 ? "Asset name contains invalid file name characters." : string.Empty;
    }

    private static HashSet<string> FindDuplicateAssetKeys(List<RenamePreview> previews)
    {
        Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

        foreach (RenamePreview preview in previews)
        {
            if (string.IsNullOrEmpty(preview.assetKey))
            {
                continue;
            }

            counts.TryGetValue(preview.assetKey, out int count);
            counts[preview.assetKey] = count + 1;
        }

        HashSet<string> duplicates = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (pair.Value > 1)
            {
                duplicates.Add(pair.Key);
            }
        }

        return duplicates;
    }

    private static string GetAssetKey(UnityEngine.Object target, string newName)
    {
        string assetPath = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        string folder = Path.GetDirectoryName(assetPath) ?? string.Empty;
        string extension = Path.GetExtension(assetPath);
        return Path.Combine(folder, newName + extension).Replace('\\', '/');
    }

    private static string GetSortKey(UnityEngine.Object target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        string path = AssetDatabase.GetAssetPath(target);
        return string.IsNullOrEmpty(path) ? target.name : path;
    }

    private void ApplyRename(List<RenamePreview> previews)
    {
        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < previews.Count; i++)
            {
                UnityEngine.Object target = i >= 0 && i < targets.Count ? targets[i] : null;
                if (target == null)
                {
                    continue;
                }

                RenamePreview preview = previews[i];
                string assetPath = AssetDatabase.GetAssetPath(target);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    string error = AssetDatabase.RenameAsset(assetPath, preview.newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"Failed to rename asset '{assetPath}' to '{preview.newName}': {error}");
                    }

                    continue;
                }

                Undo.RecordObject(target, "Batch Rename Objects");
                target.name = preview.newName;
                EditorUtility.SetDirty(target);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private readonly struct RenamePreview
    {
        public readonly string originalName;
        public readonly string newName;
        public readonly string error;
        public readonly string assetKey;

        public RenamePreview(string originalName, string newName, string error, string assetKey)
        {
            this.originalName = originalName;
            this.newName = newName;
            this.error = error;
            this.assetKey = assetKey;
        }

        public static RenamePreview Invalid(string error)
        {
            return new RenamePreview("Missing", string.Empty, error, string.Empty);
        }
    }
}
#endif
