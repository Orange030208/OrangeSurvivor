#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public sealed class PixelTextureBatchWindow : EditorWindow
{
    private enum SliceMode { None, GridByCellSize, GridByCellCount }
    private enum SpriteModeOption { Single, Multiple }

    private const float DefaultPpu = 16f;
    private const float DefaultPivot = 0.5f;
    private const float DropAreaHeight = 72f;

    private readonly List<Texture2D> textures = new();
    private Vector2 scrollPosition;
    private float pixelsPerUnit = DefaultPpu;
    private FilterMode filterMode = FilterMode.Point;
    private SpriteModeOption spriteMode = SpriteModeOption.Multiple;
    private SliceMode sliceMode = SliceMode.None;
    private bool applyPivot = true;
    private Vector2 pivot = new(DefaultPivot, DefaultPivot);
    private Vector2Int cellSize = new(16, 16);
    private Vector2Int cellCount = new(4, 4);
    private Vector2Int offset = Vector2Int.zero;
    private Vector2Int padding = Vector2Int.zero;

    [MenuItem("Tools/Orange Editor/纹理批处理工具")]
    private static void OpenWindow()
    {
        PixelTextureBatchWindow w = GetWindow<PixelTextureBatchWindow>();
        w.titleContent = new GUIContent("Pixel Texture Batch");
        w.minSize = new Vector2(680f, 560f);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Pixel Texture Batch Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("统一拖入 Texture 或文件夹。拖入文件夹时会自动收集其中所有 Texture2D。支持设置 Single/Multiple、PPU、Filter Mode，以及 Multiple 模式下的网格切图与子图 Pivot。", MessageType.Info);

        DrawDropArea();
        DrawToolbar();
        DrawTextureList();

        pixelsPerUnit = Mathf.Max(0.01f, EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit));
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
        spriteMode = (SpriteModeOption)EditorGUILayout.EnumPopup("Sprite Import Mode", spriteMode);

        applyPivot = EditorGUILayout.Toggle("Apply Pivot", applyPivot);
        using (new EditorGUI.DisabledScope(!applyPivot))
        {
            pivot = EditorGUILayout.Vector2Field("Pivot", pivot);
            pivot.x = Mathf.Clamp01(pivot.x);
            pivot.y = Mathf.Clamp01(pivot.y);
        }

        if (spriteMode == SpriteModeOption.Multiple)
        {
            sliceMode = (SliceMode)EditorGUILayout.EnumPopup("Slice Mode", sliceMode);
            if (sliceMode != SliceMode.None)
            {
                offset = ClampNonNegative(EditorGUILayout.Vector2IntField("Offset", offset));
                padding = ClampNonNegative(EditorGUILayout.Vector2IntField("Padding", padding));
                if (sliceMode == SliceMode.GridByCellSize)
                {
                    cellSize = ClampMinOne(EditorGUILayout.Vector2IntField("Cell Size", cellSize));
                }
                else
                {
                    cellCount = ClampMinOne(EditorGUILayout.Vector2IntField("Cell Count", cellCount));
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Single 模式下不会切图，但会应用 PPU、Filter Mode，并可设置主 Sprite 的 Pivot。", MessageType.None);
        }

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(textures.Count == 0))
        {
            if (GUILayout.Button("Apply", GUILayout.Height(36f)))
            {
                Apply();
            }
        }

        EditorGUILayout.HelpBox($"当前待处理纹理数: {textures.Count}", MessageType.None);
    }

    private void DrawDropArea()
    {
        Rect area = GUILayoutUtility.GetRect(0f, DropAreaHeight, GUILayout.ExpandWidth(true));
        GUI.Box(area, "拖拽 Texture 或文件夹到这里，或使用按钮从当前选中添加");
        Event e = Event.current;
        if (!area.Contains(e.mousePosition)) return;
        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddObjects(DragAndDrop.objectReferences);
            e.Use();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected", GUILayout.Height(26f))) AddObjects(Selection.objects);
        if (GUILayout.Button("Clear", GUILayout.Width(100f), GUILayout.Height(26f))) textures.Clear();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTextureList()
    {
        if (textures.Count == 0)
        {
            EditorGUILayout.HelpBox("还没有添加任何 Texture 或文件夹内容。", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200f));
        int removeIndex = -1;
        for (int i = 0; i < textures.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            textures[i] = (Texture2D)EditorGUILayout.ObjectField(textures[i], typeof(Texture2D), false);
            if (GUILayout.Button("Ping", GUILayout.Width(60f))) EditorGUIUtility.PingObject(textures[i]);
            if (GUILayout.Button("X", GUILayout.Width(28f))) removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) textures.RemoveAt(removeIndex);
        EditorGUILayout.EndScrollView();
    }

    private void AddObjects(UnityEngine.Object[] objects)
    {
        foreach (UnityEngine.Object obj in objects)
        {
            if (obj is Texture2D texture)
            {
                AddTexture(texture);
                continue;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrWhiteSpace(path) && AssetDatabase.IsValidFolder(path))
            {
                AddTexturesFromFolder(path);
            }
        }
    }

    private void AddTexture(Texture2D texture)
    {
        if (texture != null && !textures.Contains(texture)) textures.Add(texture);
    }

    private void AddTexturesFromFolder(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            AddTexture(texture);
        }
    }

    private void Apply()
    {
        List<Texture2D> list = textures.Where(t => t != null).Distinct().OrderBy(t => AssetDatabase.GetAssetPath(t), StringComparer.OrdinalIgnoreCase).ToList();
        int ok = 0, fail = 0, sprites = 0;
        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                Texture2D t = list[i];
                string path = AssetDatabase.GetAssetPath(t);
                EditorUtility.DisplayProgressBar("Pixel Texture Batch Tool", $"Processing {path} ({i + 1}/{list.Count})", (float)(i + 1) / list.Count);
                try { sprites += ApplyToTexture(t); ok++; }
                catch (Exception ex) { fail++; Debug.LogError($"[PixelTextureBatchWindow] Failed to process {path}.\n{ex}"); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Pixel Texture Batch Tool", $"处理完成。\n成功: {ok}\n失败: {fail}\n总 Sprite 数: {sprites}\nSprite Mode: {spriteMode}\nPPU: {pixelsPerUnit:0.###}\nFilter Mode: {filterMode}", "OK");
    }

    // Single / Multiple 共用一套入口，避免导入逻辑分散。
    private int ApplyToTexture(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("找不到 TextureImporter：" + path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = spriteMode == SpriteModeOption.Single ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;

        if (spriteMode == SpriteModeOption.Single)
        {
            if (applyPivot)
            {
                TextureImporterSettings importerSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(importerSettings);
                importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                importerSettings.spritePivot = GetClampedPivot();
                importer.SetTextureSettings(importerSettings);
            }
            importer.SaveAndReimport();
            return 1;
        }

        SpriteDataProviderFactories factory = new();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("无法获取 SpriteEditorDataProvider：" + importer.assetPath);

        provider.InitSpriteEditorDataProvider();
        SpriteRect[] rects = sliceMode == SliceMode.None ? provider.GetSpriteRects() ?? Array.Empty<SpriteRect>() : BuildSpriteRects(texture);
        if (applyPivot) ApplyPivot(rects, GetClampedPivot());
        if (rects.Length > 0)
        {
            provider.SetSpriteRects(rects);
            provider.Apply();
        }
        importer.SaveAndReimport();
        return CountImportedSprites(path);
    }

    private SpriteRect[] BuildSpriteRects(Texture2D texture)
    {
        List<RectInt> rects = sliceMode == SliceMode.GridByCellSize ? BuildGridByCellSizeRects(texture.width, texture.height) : BuildGridByCellCountRects(texture.width, texture.height);
        Vector2 p = GetClampedPivot();
        return rects.Select((r, i) => new SpriteRect
        {
            name = $"{texture.name}_{i:D4}",
            rect = new Rect(r.x, r.y, r.width, r.height),
            alignment = SpriteAlignment.Custom,
            pivot = applyPivot ? p : new Vector2(DefaultPivot, DefaultPivot)
        }).ToArray();
    }

    // 这里的“锚点”对应 Sprite 的 pivot；统一切到 Custom，避免预设对齐覆盖。
    private static void ApplyPivot(SpriteRect[] rects, Vector2 p)
    {
        for (int i = 0; i < rects.Length; i++)
        {
            SpriteRect r = rects[i];
            r.alignment = SpriteAlignment.Custom;
            r.pivot = p;
            rects[i] = r;
        }
    }

    private List<RectInt> BuildGridByCellSizeRects(int w, int h)
    {
        List<RectInt> rects = new();
        int sx = cellSize.x + padding.x, sy = cellSize.y + padding.y;
        for (int y = offset.y; y + cellSize.y <= h; y += sy)
        for (int x = offset.x; x + cellSize.x <= w; x += sx)
            rects.Add(new RectInt(x, h - y - cellSize.y, cellSize.x, cellSize.y));
        return rects;
    }

    private List<RectInt> BuildGridByCellCountRects(int w, int h)
    {
        int cw = Mathf.Max(1, (w - offset.x * 2 - padding.x * (cellCount.x - 1)) / cellCount.x);
        int ch = Mathf.Max(1, (h - offset.y * 2 - padding.y * (cellCount.y - 1)) / cellCount.y);
        List<RectInt> rects = new();
        for (int row = 0; row < cellCount.y; row++)
        for (int col = 0; col < cellCount.x; col++)
        {
            int x = offset.x + col * (cw + padding.x), y = offset.y + row * (ch + padding.y);
            if (x + cw <= w && y + ch <= h) rects.Add(new RectInt(x, h - y - ch, cw, ch));
        }
        return rects;
    }

    private Vector2 GetClampedPivot() => new(Mathf.Clamp01(pivot.x), Mathf.Clamp01(pivot.y));
    private static int CountImportedSprites(string path) => AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().Count();
    private static Vector2Int ClampNonNegative(Vector2Int v) => new(Mathf.Max(0, v.x), Mathf.Max(0, v.y));
    private static Vector2Int ClampMinOne(Vector2Int v) => new(Mathf.Max(1, v.x), Mathf.Max(1, v.y));
}
#endif
