using UnityEngine;

[CreateAssetMenu(fileName = "Content Tag", menuName = ScriptableObjectMenuPaths.CONTENT_TAG, order = 0)]
public class ContentTagSO : ScriptableObject
{
    private const string TAG_ID_PREFIX = "Tag_";

    [SerializeField] private string tagId = TAG_ID_PREFIX;
    [SerializeField] private string displayName;

    public string TagId => string.IsNullOrWhiteSpace(tagId) ? name : tagId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TagId : displayName;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(tagId))
        {
            tagId = TAG_ID_PREFIX;
        }
    }

    public void InitializeRuntime(string runtimeTagId)
    {
        tagId = string.IsNullOrWhiteSpace(runtimeTagId) ? TAG_ID_PREFIX : runtimeTagId;
        displayName = runtimeTagId;
    }
}
