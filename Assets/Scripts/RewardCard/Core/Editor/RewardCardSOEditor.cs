#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RewardCardSO))]
public class RewardCardSOEditor : FeatureSourceSOEditorBase
{
    protected override string FeatureListPropertyName => "grantedAbilities";
    protected override string FeatureListHeader => "奖励卡提供能力";
}
#endif
