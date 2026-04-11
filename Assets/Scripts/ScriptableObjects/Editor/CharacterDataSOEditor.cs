#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CharacterDataSO))]
public class CharacterDataSOEditor : FeatureSourceSOEditorBase
{
    protected override string FeatureListPropertyName => "specialFeatures";
    protected override string FeatureListHeader => "角色特殊能力";
}
#endif
