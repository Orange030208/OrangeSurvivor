#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AccessoryDataSO))]
public class AccessoryDataSOEditor : FeatureSourceSOEditorBase
{
    protected override string FeatureListPropertyName => "specialFeatures";
    protected override string FeatureListHeader => "饰品特殊能力";
}
#endif
