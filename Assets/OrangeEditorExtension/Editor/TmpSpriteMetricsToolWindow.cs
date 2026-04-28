#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public sealed class TmpSpriteMetricsToolWindow : EditorWindow
{
    private const float DefaultHorizontalBearingX = 0f;
    private const float DefaultVerticalOffset = 24f;
    private const float DefaultAdvanceOffset = 0f;

    private TMP_SpriteAsset targetSpriteAsset;
    private Vector2 scrollPosition;
    private float horizontalBearingX = DefaultHorizontalBearingX;
    private float verticalOffset = DefaultVerticalOffset;
    private float horizontalAdvanceOffset = DefaultAdvanceOffset;
    private bool useWidthAsAdvance = true;
    private bool showGlyphPreview = true;

    [MenuItem("Tools/Orange Editor/精灵字形度量工具")]
    private static void OpenWindow()
    {
        TmpSpriteMetricsToolWindow window = GetWindow<TmpSpriteMetricsToolWindow>();
        window.titleContent = new GUIContent("TMP 字形度量");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("TMP 精灵字形度量工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "用于批量修正 TMP Sprite Asset 的 glyph metrics。常见用途是调整行内图标的 X/Y 对齐和横向占位。",
            MessageType.Info);

        DrawSpriteAssetField();
        DrawParameterFields();
        DrawPreviewArea();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSpriteAssetField()
    {
        EditorGUILayout.Space(6f);
        targetSpriteAsset = (TMP_SpriteAsset)EditorGUILayout.ObjectField("目标 Sprite Asset", targetSpriteAsset, typeof(TMP_SpriteAsset), false);

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(targetSpriteAsset == null))
        {
            if (GUILayout.Button("定位资源"))
            {
                EditorGUIUtility.PingObject(targetSpriteAsset);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (targetSpriteAsset == null)
        {
            EditorGUILayout.HelpBox($"还未选择 TMP Sprite Asset。", MessageType.Warning);
            return;
        }

        if (targetSpriteAsset.spriteGlyphTable == null || targetSpriteAsset.spriteGlyphTable.Count == 0)
        {
            EditorGUILayout.HelpBox("当前 TMP Sprite Asset 没有可用 glyph。", MessageType.Error);
            return;
        }

        EditorGUILayout.HelpBox($"Glyph 数量: {targetSpriteAsset.spriteGlyphTable.Count}", MessageType.None);
    }

    private void DrawParameterFields()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("参数", EditorStyles.boldLabel);

        horizontalBearingX = EditorGUILayout.FloatField("水平偏移 X", horizontalBearingX);
        verticalOffset = EditorGUILayout.FloatField("垂直偏移 Y", verticalOffset);
        useWidthAsAdvance = EditorGUILayout.Toggle("占位宽度使用图标宽度", useWidthAsAdvance);
        horizontalAdvanceOffset = EditorGUILayout.FloatField("额外占位偏移", horizontalAdvanceOffset);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("推荐预设"))
        {
            ApplyRecommendedPreset();
        }

        if (GUILayout.Button("重置参数"))
        {
            ResetParameters();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreviewArea()
    {
        if (!HasGlyphs(targetSpriteAsset))
        {
            return;
        }

        showGlyphPreview = EditorGUILayout.Foldout(showGlyphPreview, "预览", true);
        if (!showGlyphPreview)
        {
            return;
        }

        TMP_SpriteGlyph glyph = GetFirstValidGlyph(targetSpriteAsset);
        if (glyph == null)
        {
            EditorGUILayout.HelpBox("无法读取有效 glyph。", MessageType.Warning);
            return;
        }

        GlyphMetrics currentMetrics = glyph.metrics;
        GlyphMetrics targetMetrics = BuildExpectedMetrics(currentMetrics, horizontalBearingX, verticalOffset, horizontalAdvanceOffset, useWidthAsAdvance);

        EditorGUILayout.LabelField("当前", FormatMetrics(currentMetrics));
        EditorGUILayout.LabelField("目标", FormatMetrics(targetMetrics));
        EditorGUILayout.HelpBox("垂直偏移 Y 会在 glyph 高度基础上额外抬高图标。正数上移，负数下移。", MessageType.None);
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!HasGlyphs(targetSpriteAsset)))
        {
            if (GUILayout.Button("应用到当前 Sprite Asset", GUILayout.Height(34f)))
            {
                ApplyMetrics(targetSpriteAsset, horizontalBearingX, verticalOffset, horizontalAdvanceOffset, useWidthAsAdvance);
            }
        }
    }

    private void ApplyRecommendedPreset()
    {
        horizontalBearingX = DefaultHorizontalBearingX;
        verticalOffset = DefaultVerticalOffset;
        horizontalAdvanceOffset = DefaultAdvanceOffset;
        useWidthAsAdvance = true;
    }

    private void ResetParameters()
    {
        horizontalBearingX = 0f;
        verticalOffset = 0f;
        horizontalAdvanceOffset = 0f;
        useWidthAsAdvance = true;
    }

    private static bool HasGlyphs(TMP_SpriteAsset spriteAsset)
    {
        return spriteAsset != null && spriteAsset.spriteGlyphTable != null && spriteAsset.spriteGlyphTable.Count > 0;
    }

    private static TMP_SpriteGlyph GetFirstValidGlyph(TMP_SpriteAsset spriteAsset)
    {
        if (!HasGlyphs(spriteAsset))
        {
            return null;
        }

        for (int i = 0; i < spriteAsset.spriteGlyphTable.Count; i++)
        {
            TMP_SpriteGlyph glyph = spriteAsset.spriteGlyphTable[i];
            if (glyph != null)
            {
                return glyph;
            }
        }

        return null;
    }

    private static void ApplyMetrics(
        TMP_SpriteAsset spriteAsset,
        float targetHorizontalBearingX,
        float targetVerticalOffset,
        float targetAdvanceOffset,
        bool useTargetWidthAsAdvance)
    {
        if (!HasGlyphs(spriteAsset))
        {
            Debug.LogError("TMP Sprite Asset 为空，或没有可用 glyph。", spriteAsset);
            return;
        }

        Undo.RecordObject(spriteAsset, "调整 TMP Sprite Asset 字形度量");

        int changedGlyphCount = 0;
        for (int i = 0; i < spriteAsset.spriteGlyphTable.Count; i++)
        {
            TMP_SpriteGlyph glyph = spriteAsset.spriteGlyphTable[i];
            if (glyph == null)
            {
                continue;
            }

            GlyphMetrics currentMetrics = glyph.metrics;
            if (Mathf.Approximately(currentMetrics.width, 0f) || Mathf.Approximately(currentMetrics.height, 0f))
            {
                continue;
            }

            GlyphMetrics targetMetrics = BuildExpectedMetrics(
                currentMetrics,
                targetHorizontalBearingX,
                targetVerticalOffset,
                targetAdvanceOffset,
                useTargetWidthAsAdvance);

            if (MetricsEqual(currentMetrics, targetMetrics))
            {
                continue;
            }

            glyph.metrics = targetMetrics;
            changedGlyphCount++;
        }

        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset), ImportAssetOptions.ForceUpdate);

        Debug.Log(
            $"已更新 TMP Sprite Asset [{spriteAsset.name}] 的 glyph metrics。变更数量: {changedGlyphCount}。 " +
            $"BearingX={targetHorizontalBearingX}, VerticalOffset={targetVerticalOffset}, AdvanceOffset={targetAdvanceOffset}, UseWidthAsAdvance={useTargetWidthAsAdvance}",
            spriteAsset);
    }

    private static GlyphMetrics BuildExpectedMetrics(
        GlyphMetrics currentMetrics,
        float targetHorizontalBearingX,
        float targetVerticalOffset,
        float targetAdvanceOffset,
        bool useTargetWidthAsAdvance)
    {
        float targetAdvance = useTargetWidthAsAdvance ? currentMetrics.width : currentMetrics.horizontalAdvance;
        targetAdvance += targetAdvanceOffset;

        return new GlyphMetrics(
            currentMetrics.width,
            currentMetrics.height,
            targetHorizontalBearingX,
            currentMetrics.height + targetVerticalOffset,
            targetAdvance);
    }

    private static bool MetricsEqual(GlyphMetrics left, GlyphMetrics right)
    {
        return Mathf.Approximately(left.width, right.width)
            && Mathf.Approximately(left.height, right.height)
            && Mathf.Approximately(left.horizontalBearingX, right.horizontalBearingX)
            && Mathf.Approximately(left.horizontalBearingY, right.horizontalBearingY)
            && Mathf.Approximately(left.horizontalAdvance, right.horizontalAdvance);
    }

    private static string FormatMetrics(GlyphMetrics metrics)
    {
        return $"W:{metrics.width:0.##} H:{metrics.height:0.##} BX:{metrics.horizontalBearingX:0.##} BY:{metrics.horizontalBearingY:0.##} ADV:{metrics.horizontalAdvance:0.##}";
    }
}
#endif
