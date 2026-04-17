#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(BuffDataSO))]
public class BuffDataSOEditor : FeatureSourceSOEditorBase
{
    protected override string FeatureListPropertyName => "specialFeatures";
    protected override string FeatureListHeader => "Buff 特殊能力";
}
#endif
