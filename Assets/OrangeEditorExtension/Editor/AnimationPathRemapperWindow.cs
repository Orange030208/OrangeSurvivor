#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class AnimationPathRemapperWindow : EditorWindow
{
    private const float MinWindowWidth = 620f;
    private const float MinWindowHeight = 500f;
    private const float BindingListHeight = 200f;
    private const float ButtonHeight = 36f;

    private AnimatorController targetController;
    private GameObject oldHierarchyReference;
    private GameObject newHierarchyReference;
    private string manualOldPath = string.Empty;
    private string manualNewPath = string.Empty;
    private string selectedOldPath = string.Empty;
    private Vector2 bindingListScroll;
    private Vector2 mainScroll;
    private List<ClipBindingInfo> allBindings = new();
    private bool hasScanned;

    private struct ClipBindingInfo
    {
        public string clipName;
        public string path;
        public string propertyName;
        public bool isObjectRef;
    }

    [MenuItem("Tools/Animation/路径引用迁移工具")]
    private static void OpenWindow()
    {
        AnimationPathRemapperWindow window = GetWindow<AnimationPathRemapperWindow>();
        window.titleContent = new GUIContent("动画路径迁移");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        window.Show();
    }

    private void OnGUI()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        DrawHeader();
        DrawControllerSection();
        DrawOldPathSection();
        DrawNewPathSection();
        DrawBindingList();
        DrawReplaceSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("动画路径引用迁移工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "当 AnimatorController 中的 AnimationClip 因为 Hierarchy 层级调整导致关键帧引用 Missing 时，" +
            "一键将所有绑定从旧路径迁移到新路径。",
            MessageType.Info);
    }

    private void DrawControllerSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("第1步：选择 AnimatorController", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        targetController = (AnimatorController)EditorGUILayout.ObjectField(
            targetController, typeof(AnimatorController), false);

        using (new EditorGUI.DisabledScope(targetController == null))
        {
            if (GUILayout.Button("扫描全部绑定", GUILayout.Width(120f), GUILayout.Height(20f)))
            {
                ScanAllBindings();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (targetController == null)
        {
            EditorGUILayout.HelpBox("请拖入需要修复的 AnimatorController。", MessageType.Warning);
        }
    }

    private void DrawOldPathSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("第2步：指定旧路径（要迁移走的路径）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "方式A：从场景 Hierarchy 拖入仍存在的旧层级中的任意 GameObject。\n" +
            "方式B：如果旧节点已被删除，直接在下方的文本框中手动输入路径。\n" +
            "提示：下方的绑定列表中每条路径都可点击自动填入。",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        oldHierarchyReference = (GameObject)EditorGUILayout.ObjectField(
            "拖入旧层级 GameObject", oldHierarchyReference, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && oldHierarchyReference != null)
        {
            manualOldPath = CalculatePath(oldHierarchyReference);
        }

        EditorGUI.BeginChangeCheck();
        manualOldPath = EditorGUILayout.TextField("或手动输入旧路径", manualOldPath);
        if (EditorGUI.EndChangeCheck())
        {
            oldHierarchyReference = null;
            selectedOldPath = string.Empty;
        }
    }

    private void DrawNewPathSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("第3步：指定新路径（要迁移到的路径）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "方式A：从场景 Hierarchy 拖入新层级中对应的 GameObject。\n" +
            "方式B：或手动输入目标路径。",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        newHierarchyReference = (GameObject)EditorGUILayout.ObjectField(
            "拖入新层级 GameObject", newHierarchyReference, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && newHierarchyReference != null)
        {
            manualNewPath = CalculatePath(newHierarchyReference);
        }

        EditorGUI.BeginChangeCheck();
        manualNewPath = EditorGUILayout.TextField("或手动输入新路径", manualNewPath);
        if (EditorGUI.EndChangeCheck())
        {
            newHierarchyReference = null;
        }
    }

    private void DrawBindingList()
    {
        if (!hasScanned || allBindings.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            $"动画绑定明细（{allBindings.Select(b => b.clipName).Distinct().Count()} 个 Clip，" +
            $"{allBindings.Count} 条绑定）",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "每行格式：层级路径 → 属性名。\n" +
            "\"(Root)\" 表示绑定在 Animator 所在根节点上。拖动 GameObject 或手动输入旧路径后，" +
            "匹配的绑定会高亮显示。",
            MessageType.None);

        bindingListScroll = EditorGUILayout.BeginScrollView(
            bindingListScroll, GUILayout.Height(BindingListHeight));

        string currentClipName = null;
        string resolvedOldPath = ResolveOldPath();

        foreach (ClipBindingInfo binding in allBindings)
        {
            if (binding.clipName != currentClipName)
            {
                currentClipName = binding.clipName;
                EditorGUILayout.LabelField($"● {currentClipName}", EditorStyles.boldLabel);
            }

            string pathDisplay = string.IsNullOrEmpty(binding.path) ? "(Root)" : binding.path;
            string fullDisplay = $"{pathDisplay}  →  {binding.propertyName}";

            bool isMatch = IsBindingPathMatch(binding.path, resolvedOldPath);
            bool isSelected = binding.path == selectedOldPath;

            Color origBg = GUI.backgroundColor;
            Color origContent = GUI.contentColor;

            if (isMatch)
            {
                GUI.backgroundColor = new Color(0.65f, 0.85f, 1f, 0.35f);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(binding.path == selectedOldPath);
            if (GUILayout.Button("▸", GUILayout.Width(22f), GUILayout.Height(16f)))
            {
                selectedOldPath = binding.path;
                manualOldPath = binding.path;
                oldHierarchyReference = null;
            }
            EditorGUI.EndDisabledGroup();

            if (isSelected)
            {
                EditorGUILayout.LabelField($"✓ {fullDisplay}", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"  {fullDisplay}");
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = origBg;
            GUI.contentColor = origContent;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawReplaceSection()
    {
        EditorGUILayout.Space(12f);

        string oldPath = ResolveOldPath();
        string newPath = ResolveNewPath();
        string oldDisplay = string.IsNullOrEmpty(oldPath) ? "(Root)" : oldPath;
        string newDisplay = string.IsNullOrEmpty(newPath) ? "(Root)" : newPath;

        bool canReplace = targetController != null
            && !string.IsNullOrEmpty(newPath)
            && oldPath != newPath;

        if (canReplace)
        {
            int matchCount = CountBindingsMatching(oldPath);
            if (matchCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"即将把 {matchCount} 条绑定从 \"{oldDisplay}\" 迁移到 \"{newDisplay}\"",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"没有找到匹配 \"{oldDisplay}\" 的绑定。请检查旧路径是否正确，或先点击「扫描全部绑定」。",
                    MessageType.Warning);
            }
        }
        else if (targetController != null && string.IsNullOrEmpty(newPath))
        {
            EditorGUILayout.HelpBox(
                "请指定新路径（拖入 GameObject 或手动输入）。",
                MessageType.Warning);
        }
        else if (targetController != null && oldPath == newPath)
        {
            EditorGUILayout.HelpBox(
                "旧路径和新路径相同，无需迁移。",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!canReplace))
        {
            if (GUILayout.Button("一键迁移路径引用", GUILayout.Height(ButtonHeight)))
            {
                ExecuteRemap(oldPath, newPath);
            }
        }
    }

    private string ResolveOldPath()
    {
        if (oldHierarchyReference != null)
        {
            return CalculatePath(oldHierarchyReference);
        }

        return manualOldPath ?? string.Empty;
    }

    private string ResolveNewPath()
    {
        if (newHierarchyReference != null)
        {
            return CalculatePath(newHierarchyReference);
        }

        return manualNewPath ?? string.Empty;
    }

    private static string CalculatePath(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        Transform root = gameObject.transform.root;
        return AnimationUtility.CalculateTransformPath(gameObject.transform, root);
    }

    private void ScanAllBindings()
    {
        allBindings.Clear();
        selectedOldPath = string.Empty;
        hasScanned = true;

        if (targetController == null)
        {
            return;
        }

        AnimationClip[] clips = targetController.animationClips;
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[AnimationPathRemapper] AnimatorController 中没有任何 AnimationClip。");
            return;
        }

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                allBindings.Add(new ClipBindingInfo
                {
                    clipName = clip.name,
                    path = binding.path,
                    propertyName = binding.propertyName,
                    isObjectRef = false
                });
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                allBindings.Add(new ClipBindingInfo
                {
                    clipName = clip.name,
                    path = binding.path,
                    propertyName = binding.propertyName,
                    isObjectRef = true
                });
            }
        }

        allBindings = allBindings
            .OrderBy(b => b.clipName, StringComparer.Ordinal)
            .ThenBy(b => b.path, StringComparer.Ordinal)
            .ThenBy(b => b.propertyName, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsBindingPathMatch(string bindingPath, string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            return string.IsNullOrEmpty(bindingPath);
        }

        return bindingPath == targetPath
            || bindingPath.StartsWith(targetPath + "/", StringComparison.Ordinal);
    }

    private int CountBindingsMatching(string targetOldPath)
    {
        if (targetController == null)
        {
            return 0;
        }

        int count = 0;
        AnimationClip[] clips = targetController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (IsBindingPathMatch(binding.path, targetOldPath))
                {
                    count++;
                }
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (IsBindingPathMatch(binding.path, targetOldPath))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void ExecuteRemap(string sourcePath, string destPath)
    {
        if (targetController == null || string.IsNullOrEmpty(destPath))
        {
            return;
        }

        AnimationClip[] clips = targetController.animationClips;
        if (clips == null || clips.Length == 0)
        {
            EditorUtility.DisplayDialog("动画路径迁移", "AnimatorController 中没有任何 AnimationClip。", "OK");
            return;
        }

        int totalChanged = 0;
        try
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    "动画路径迁移",
                    $"处理中 {clip.name}（{i + 1}/{clips.Length}）",
                    (float)(i + 1) / clips.Length);

                totalChanged += RemapBindingsInClip(clip, sourcePath, destPath);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ScanAllBindings();

        string sourceDisplay = string.IsNullOrEmpty(sourcePath) ? "(Root)" : sourcePath;
        string destDisplay = string.IsNullOrEmpty(destPath) ? "(Root)" : destPath;
        EditorUtility.DisplayDialog(
            "动画路径迁移",
            $"替换完成！\n修改了 {totalChanged} 条绑定\n\n{sourceDisplay}\n  ↓\n{destDisplay}",
            "OK");
    }

    private static int RemapBindingsInClip(AnimationClip clip, string sourcePath, string destPath)
    {
        int changedCount = 0;

        EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding binding in floatBindings)
        {
            string replacedPath = TryReplacePath(binding.path, sourcePath, destPath);
            if (replacedPath == null)
            {
                continue;
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                continue;
            }

            Undo.RecordObject(clip, "Remap Animation Path");

            AnimationUtility.SetEditorCurve(clip, binding, null);

            EditorCurveBinding newBinding = binding;
            newBinding.path = replacedPath;
            AnimationUtility.SetEditorCurve(clip, newBinding, curve);

            changedCount++;
        }

        EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (EditorCurveBinding binding in objectBindings)
        {
            string replacedPath = TryReplacePath(binding.path, sourcePath, destPath);
            if (replacedPath == null)
            {
                continue;
            }

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keyframes == null || keyframes.Length == 0)
            {
                continue;
            }

            Undo.RecordObject(clip, "Remap Animation Path");

            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            EditorCurveBinding newBinding = binding;
            newBinding.path = replacedPath;
            AnimationUtility.SetObjectReferenceCurve(clip, newBinding, keyframes);

            changedCount++;
        }

        if (changedCount > 0)
        {
            EditorUtility.SetDirty(clip);
        }

        return changedCount;
    }

    private static string TryReplacePath(string bindingPath, string sourcePath, string destPath)
    {
        if (bindingPath == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(sourcePath))
        {
            return string.IsNullOrEmpty(bindingPath) ? destPath : null;
        }

        if (bindingPath == sourcePath)
        {
            return destPath;
        }

        if (bindingPath.StartsWith(sourcePath + "/", StringComparison.Ordinal))
        {
            return destPath + bindingPath.Substring(sourcePath.Length);
        }

        return null;
    }
}
#endif
