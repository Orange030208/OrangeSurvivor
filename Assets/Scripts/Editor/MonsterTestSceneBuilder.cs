#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MonsterTestSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Monster Test Scene.unity";
    private const string ENEMY_DATA_FOLDER = "Assets/Resources/Data/Enemies";
    private const string TARGET_SPRITE_PATH = "Assets/Resources/Sprites/Characters/Test.png";

    [MenuItem("Tools/Testing/Create Monster Test Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateSceneNotes();

        GameObject runtimeRoot = new("Monster Test Runtime");
        GameObject spawnedEnemiesRoot = new("Spawned Enemies");
        spawnedEnemiesRoot.transform.SetParent(runtimeRoot.transform);

        MonsterTestTarget target = CreateTarget(runtimeRoot.transform);
        Transform[] spawnPoints = CreateSpawnPoints(runtimeRoot.transform);

        GameObject controllerObject = new("Monster Test Controller", typeof(MonsterTestSceneController));
        controllerObject.transform.SetParent(runtimeRoot.transform);
        ConfigureController(
            controllerObject.GetComponent<MonsterTestSceneController>(),
            target,
            spawnedEnemiesRoot.transform,
            spawnPoints);

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controllerObject;
        Debug.Log($"Created monster test scene at {SCENE_PATH}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
    }

    private static void CreateSceneNotes()
    {
        GameObject notes = new("Scene Notes");
        CreateLabel(
            "Monster Test Scene\nPlay: auto-spawns one configured enemy.\nContext menu on Monster Test Controller: spawn/reset enemies.\nNo WaveManager, GameManager, UI, or map modules are required.",
            new Vector3(-5.4f, 4.8f, 0f),
            0.28f,
            notes.transform);
    }

    private static MonsterTestTarget CreateTarget(Transform parent)
    {
        GameObject targetObject = new("Test Target", typeof(CircleCollider2D), typeof(HealthComponent), typeof(MonsterTestTarget));
        targetObject.transform.SetParent(parent);
        targetObject.transform.position = Vector3.zero;

        CircleCollider2D circleCollider = targetObject.GetComponent<CircleCollider2D>();
        circleCollider.radius = 0.45f;
        circleCollider.isTrigger = true;

        ConfigureHealth(targetObject.GetComponent<HealthComponent>(), 1000000f);

        GameObject visualObject = new("Visual", typeof(SpriteRenderer), typeof(EntityRenderer));
        visualObject.transform.SetParent(targetObject.transform);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TARGET_SPRITE_PATH);
        spriteRenderer.color = new Color(0.35f, 0.85f, 1f, 1f);
        spriteRenderer.sortingOrder = 10;

        MonsterTestTarget target = targetObject.GetComponent<MonsterTestTarget>();
        SerializedObject serializedTarget = new(target);
        serializedTarget.FindProperty("circleCollider").objectReferenceValue = circleCollider;
        serializedTarget.FindProperty("entityRenderer").objectReferenceValue = visualObject.GetComponent<EntityRenderer>();
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();

        CreateLabel("Test Target", new Vector3(0.45f, 0.65f, 0f), 0.2f, targetObject.transform);
        return target;
    }

    private static Transform[] CreateSpawnPoints(Transform parent)
    {
        GameObject spawnRoot = new("Spawn Points");
        spawnRoot.transform.SetParent(parent);

        Vector3[] positions =
        {
            new(-3f, 0f, 0f),
            new(3f, 0f, 0f),
            new(0f, 3f, 0f),
            new(0f, -3f, 0f),
        };

        Transform[] spawnPoints = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject spawnPoint = new($"Spawn Point {i + 1}");
            spawnPoint.transform.SetParent(spawnRoot.transform);
            spawnPoint.transform.position = positions[i];
            spawnPoints[i] = spawnPoint.transform;

            CreateLabel($"Spawn {i + 1}", positions[i] + new Vector3(0.2f, 0.35f, 0f), 0.18f, spawnRoot.transform);
        }

        return spawnPoints;
    }

    private static void ConfigureController(
        MonsterTestSceneController controller,
        MonsterTestTarget target,
        Transform spawnRoot,
        Transform[] spawnPoints)
    {
        EnemySO[] testEnemies = FindAssets<EnemySO>(ENEMY_DATA_FOLDER);

        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("target").objectReferenceValue = target;
        serializedController.FindProperty("defaultEnemyIndex").intValue = 0;
        serializedController.FindProperty("spawnRoot").objectReferenceValue = spawnRoot;
        serializedController.FindProperty("spawnOnStart").boolValue = true;
        serializedController.FindProperty("keepSimulationRunning").boolValue = true;

        SerializedProperty enemiesProperty = serializedController.FindProperty("testEnemies");
        enemiesProperty.arraySize = testEnemies.Length;
        for (int i = 0; i < testEnemies.Length; i++)
        {
            enemiesProperty.GetArrayElementAtIndex(i).objectReferenceValue = testEnemies[i];
        }

        SerializedProperty spawnPointsProperty = serializedController.FindProperty("spawnPoints");
        spawnPointsProperty.arraySize = spawnPoints.Length;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureHealth(HealthComponent healthComponent, float maxHealth)
    {
        SerializedObject serializedHealth = new(healthComponent);
        serializedHealth.FindProperty("defaultMaxHealth").floatValue = maxHealth;
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLabel(string text, Vector3 position, float characterSize, Transform parent)
    {
        GameObject labelObject = new($"Label - {text.Split('\n')[0]}", typeof(TextMesh));
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = position;

        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.characterSize = characterSize;
        textMesh.color = new Color(0.85f, 0.9f, 0.95f, 1f);
    }

    private static T FindFirstAsset<T>(string folder) where T : UnityEngine.Object
    {
        T[] assets = FindAssets<T>(folder);
        return assets.Length > 0 ? assets[0] : null;
    }

    private static T[] FindAssets<T>(string folder) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        List<T> assets = new(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        assets.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
        return assets.ToArray();
    }
}
#endif
