#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ItemQualityPreviewSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Item Quality Preview.unity";

    [MenuItem("Tools/Preview/Create Item Quality Preview Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject previewRoot = new("Item Quality Preview Root", typeof(RectTransform), typeof(ItemQualityPreviewSceneController));
        ItemQualityPreviewSceneController controller = previewRoot.GetComponent<ItemQualityPreviewSceneController>();
        controller.RebuildPreview();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created item quality preview scene at {SCENE_PATH}");
    }
}
#endif
