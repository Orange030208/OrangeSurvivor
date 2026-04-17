#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UIRuntimeMotionBase 的 Inspector 调试绘制辅助。
/// 统一挂在抽象基类上，避免 Unity Inspector 对接口支持不足的问题。
/// </summary>
internal static class UIRuntimeMotionInspectorGUI
{
    private const float SECTION_SPACING = 8f;
    private const int ACTIONS_PER_ROW = 3;

    public static void Draw(Editor editor)
    {
        DrawStringConfig(editor);
        editor.DrawDefaultInspector();

        if (editor.targets == null || editor.targets.Length != 1)
        {
            return;
        }

        if (editor.target is not UIRuntimeMotionBase runtimeMotion)
        {
            return;
        }

        EditorGUILayout.Space(SECTION_SPACING);
        EditorGUILayout.LabelField("Runtime Motion", EditorStyles.boldLabel);

        IReadOnlyList<UIMotionAction> supportedActions = runtimeMotion.GetSupportedActions();
        if (supportedActions.Count == 0)
        {
            EditorGUILayout.HelpBox("当前 motion 没有声明任何支持的 UIMotionAction。", MessageType.Warning);
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可直接预览该 motion 支持的 UIMotionAction。", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawPlayButtons(runtimeMotion, supportedActions);

            EditorGUILayout.Space(SECTION_SPACING);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Defaults"))
                {
                    runtimeMotion.RefreshDefaults();
                    EditorUtility.SetDirty(runtimeMotion);
                }

                if (GUILayout.Button("Kill"))
                {
                    runtimeMotion.Kill();
                    EditorUtility.SetDirty(runtimeMotion);
                }
            }
        }
    }

    private static void DrawStringConfig(Editor editor)
    {
        if (editor.targets == null || editor.targets.Length != 1)
        {
            return;
        }

        if (editor.target is not UIRuntimeMotionBase runtimeMotion)
        {
            return;
        }

        List<string> options = runtimeMotion.GetOptionList();
        if (options == null || options.Count == 0)
        {
            return;
        }

        int currentIndex = Mathf.Max(0, options.IndexOf(runtimeMotion.CurrentConfigOption));
        int nextIndex = EditorGUILayout.Popup("Config Preset", currentIndex, options.ToArray());
        if (nextIndex == currentIndex)
        {
            return;
        }

        string selectedOption = options[nextIndex];
        Undo.RecordObject(runtimeMotion, "Apply Motion Config Preset");
        runtimeMotion.ApplyConfigByString(selectedOption);
        EditorUtility.SetDirty(runtimeMotion);
    }

    private static void DrawPlayButtons(UIRuntimeMotionBase runtimeMotion, IReadOnlyList<UIMotionAction> supportedActions)
    {
        for (int index = 0; index < supportedActions.Count; index += ACTIONS_PER_ROW)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int endIndexExclusive = Mathf.Min(index + ACTIONS_PER_ROW, supportedActions.Count);
                for (int actionIndex = index; actionIndex < endIndexExclusive; actionIndex++)
                {
                    UIMotionAction action = supportedActions[actionIndex];
                    if (GUILayout.Button($"Play/{action}"))
                    {
                        runtimeMotion.Play(action);
                        EditorUtility.SetDirty(runtimeMotion);
                    }
                }
            }
        }
    }
}

[CustomEditor(typeof(UIRuntimeMotionBase), true)]
[CanEditMultipleObjects]
public class UIRuntimeMotionBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        UIRuntimeMotionInspectorGUI.Draw(this);
    }
}
#endif
