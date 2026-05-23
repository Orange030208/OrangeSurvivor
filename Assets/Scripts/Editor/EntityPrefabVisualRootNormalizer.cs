#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class EntityPrefabVisualRootNormalizer
{
    private const string MENU_PATH = "Tools/Orange/Content/Normalize Entity Prefab Visual Roots";
    private const string VISUAL_ROOT_NAME = "VisualRoot";
    private const string RENDER_NODE_NAME = "Render";

    private static readonly string[] PREFAB_PATHS =
    {
        "Assets/GameContent/Characters/Prefabs/Character.prefab",
        "Assets/GameContent/Characters/Prefabs/TestCharacter.prefab",
        "Assets/GameContent/Enemies/Prefabs/CyberOrangeCharger.prefab",
        "Assets/GameContent/Enemies/Prefabs/CyberOrangeFastMelee.prefab",
        "Assets/GameContent/Enemies/Prefabs/CyberOrangeKitingRanged.prefab",
        "Assets/GameContent/Enemies/Prefabs/CyberOrangeRanged.prefab",
        "Assets/GameContent/Enemies/Prefabs/CyberOrangeSlowMelee.prefab",
        "Assets/GameContent/Collections/Prefabs/CollectionBase.prefab",
        "Assets/GameContent/Collections/Prefabs/Coin.prefab",
        "Assets/GameContent/Collections/Prefabs/CommonChest.prefab",
        "Assets/GameContent/Collections/Prefabs/GoldChest.prefab",
        "Assets/GameContent/Collections/Prefabs/BossChest.prefab",
    };

    [MenuItem(MENU_PATH)]
    public static void NormalizeAll()
    {
        int changedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < PREFAB_PATHS.Length; i++)
        {
            string prefabPath = PREFAB_PATHS[i];
            try
            {
                if (NormalizePrefab(prefabPath))
                {
                    changedCount++;
                }
            }
            catch (Exception exception)
            {
                skippedCount++;
                Debug.LogError(
                    $"[{nameof(EntityPrefabVisualRootNormalizer)}] Failed to normalize {prefabPath}: {exception}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[{nameof(EntityPrefabVisualRootNormalizer)}] Normalized entity prefabs. Changed: {changedCount}, skipped: {skippedCount}, total: {PREFAB_PATHS.Length}.");
    }

    public static bool NormalizePrefab(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogWarning(
                $"[{nameof(EntityPrefabVisualRootNormalizer)}] Missing prefab asset at {prefabPath}.");
            return false;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            bool changed = NormalizeLoadedPrefab(contents, prefabPath);
            if (!changed)
            {
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath, out bool success);
            if (!success)
            {
                throw new InvalidOperationException($"Unity failed to save prefab asset at {prefabPath}.");
            }

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    public static bool NormalizeLoadedPrefab(GameObject contents, string prefabPath)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }

        SpriteRenderer spriteRenderer = ResolvePrimarySpriteRenderer(contents);
        if (spriteRenderer == null)
        {
            Debug.LogWarning(
                $"[{nameof(EntityPrefabVisualRootNormalizer)}] {prefabPath} has no {nameof(SpriteRenderer)}.");
            return false;
        }

        Transform root = contents.transform;
        Transform renderTransform = spriteRenderer.transform;
        if (renderTransform == root)
        {
            throw new InvalidOperationException(
                $"{prefabPath} must keep the {nameof(SpriteRenderer)} on a child GameObject before visual root normalization.");
        }

        if (renderTransform.GetComponent<Collider2D>() != null)
        {
            throw new InvalidOperationException(
                $"{prefabPath} has a collider on the visual node '{renderTransform.name}'. Keep collision components on the entity root.");
        }

        bool changed = false;
        Transform visualRoot = EnsureVisualRoot(root, contents.layer, ref changed);
        changed |= EnsureRenderNode(renderTransform, visualRoot);
        changed |= BindEntityRenderer(contents, spriteRenderer);
        changed |= BindProceduralVisualRoot(contents, visualRoot);

        if (changed)
        {
            EditorUtility.SetDirty(contents);
        }

        return changed;
    }

    private static SpriteRenderer ResolvePrimarySpriteRenderer(GameObject contents)
    {
        EntityRenderer entityRenderer = contents.GetComponent<EntityRenderer>();
        if (entityRenderer != null && entityRenderer.SpriteRenderer != null)
        {
            return entityRenderer.SpriteRenderer;
        }

        Transform existingVisualRoot = contents.transform.Find(VISUAL_ROOT_NAME);
        if (existingVisualRoot != null)
        {
            SpriteRenderer visualRootRenderer = existingVisualRoot.GetComponentInChildren<SpriteRenderer>(true);
            if (visualRootRenderer != null)
            {
                return visualRootRenderer;
            }
        }

        return contents.GetComponentInChildren<SpriteRenderer>(true);
    }

    private static Transform EnsureVisualRoot(Transform root, int rootLayer, ref bool changed)
    {
        Transform visualRoot = root.Find(VISUAL_ROOT_NAME);
        if (visualRoot == null)
        {
            GameObject visualRootObject = new(VISUAL_ROOT_NAME);
            visualRoot = visualRootObject.transform;
            visualRoot.SetParent(root, false);
            changed = true;
        }

        if (visualRoot.gameObject.layer != rootLayer)
        {
            visualRoot.gameObject.layer = rootLayer;
            changed = true;
        }

        if (visualRoot.localPosition != Vector3.zero)
        {
            visualRoot.localPosition = Vector3.zero;
            changed = true;
        }

        if (visualRoot.localRotation != Quaternion.identity)
        {
            visualRoot.localRotation = Quaternion.identity;
            changed = true;
        }

        if (visualRoot.localScale != Vector3.one)
        {
            visualRoot.localScale = Vector3.one;
            changed = true;
        }

        if (visualRoot.GetSiblingIndex() != 0)
        {
            visualRoot.SetSiblingIndex(0);
            changed = true;
        }

        return visualRoot;
    }

    private static bool EnsureRenderNode(Transform renderTransform, Transform visualRoot)
    {
        bool changed = false;
        Vector3 localPosition = renderTransform.localPosition;
        Quaternion localRotation = renderTransform.localRotation;
        Vector3 localScale = renderTransform.localScale;

        if (renderTransform.parent != visualRoot)
        {
            renderTransform.SetParent(visualRoot, false);
            renderTransform.localPosition = localPosition;
            renderTransform.localRotation = localRotation;
            renderTransform.localScale = localScale;
            changed = true;
        }

        if (renderTransform.name != RENDER_NODE_NAME)
        {
            renderTransform.name = RENDER_NODE_NAME;
            changed = true;
        }

        return changed;
    }

    private static bool BindEntityRenderer(GameObject contents, SpriteRenderer spriteRenderer)
    {
        EntityRenderer entityRenderer = contents.GetComponent<EntityRenderer>();
        if (entityRenderer == null)
        {
            return false;
        }

        SerializedObject serializedObject = new(entityRenderer);
        SerializedProperty spriteRendererProperty = serializedObject.FindProperty("spriteRenderer");
        if (spriteRendererProperty == null || spriteRendererProperty.objectReferenceValue == spriteRenderer)
        {
            return false;
        }

        spriteRendererProperty.objectReferenceValue = spriteRenderer;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(entityRenderer);
        return true;
    }

    private static bool BindProceduralVisualRoot(GameObject contents, Transform visualRoot)
    {
        ProceduralEntityAnimationComponent proceduralAnimation =
            contents.GetComponent<ProceduralEntityAnimationComponent>();
        if (proceduralAnimation == null)
        {
            return false;
        }

        SerializedObject serializedObject = new(proceduralAnimation);
        SerializedProperty visualRootProperty = serializedObject.FindProperty("visualRoot");
        if (visualRootProperty == null)
        {
            throw new MissingFieldException(nameof(ProceduralEntityAnimationComponent), "visualRoot");
        }

        if (visualRootProperty.objectReferenceValue == visualRoot)
        {
            return false;
        }

        visualRootProperty.objectReferenceValue = visualRoot;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(proceduralAnimation);
        return true;
    }
}
#endif
