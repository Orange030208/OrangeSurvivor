#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(UpgradeCardSO))]
public class UpgradeCardSOEditor : FeatureSourceSOEditorBase
{
    protected override string FeatureListPropertyName => "specialFeatures";
    protected override string FeatureListHeader => "升级卡特殊能力";
}
#endif
