
namespace Orange.UIFramework
{
    using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIMotionPlayer))]
public sealed class UIMotionPlayerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UIMotionPlayer player = (UIMotionPlayer)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Preview", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Show"))
            {
                Preview(player, UIMotionClipIds.SHOW);
            }

            if (GUILayout.Button("Hide"))
            {
                Preview(player, UIMotionClipIds.HIDE);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Hover In"))
            {
                Preview(player, UIMotionClipIds.HOVER_IN);
            }

            if (GUILayout.Button("Click Pulse"))
            {
                Preview(player, UIMotionClipIds.CLICK_PULSE);
            }
        }

        if (GUILayout.Button("Refresh Defaults"))
        {
            player.RefreshDefaults();
            EditorUtility.SetDirty(player);
        }

        if (GUILayout.Button("Stop All"))
        {
            player.Kill();
        }
    }

    private static void Preview(UIMotionPlayer player, string clipId)
    {
        if (player == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            player.SetImmediate(clipId);
            EditorUtility.SetDirty(player);
            return;
        }

        player.Play(clipId);
    }
}
}
