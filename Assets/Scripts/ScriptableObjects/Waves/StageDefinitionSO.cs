using UnityEngine;

/// <summary>
/// 整个关卡的波次配置入口。
/// 运行时会从这里按顺序读取每一波的定义并构建关卡波次列表。
/// </summary>
[CreateAssetMenu(fileName = "Stage Definition", menuName = ScriptableObjectMenuPaths.STAGE_DEFINITION, order = 0)]
public class StageDefinitionSO : ScriptableObject
{
    [Header("标识")]
    [SerializeField] private string stageId = "Stage_001";
    [SerializeField] private string displayName = "Stage 1";

    [Header("波次")]
    // 按数组顺序决定关卡中的波次顺序。
    [SerializeField] private WaveDefinitionSO[] waves;

    public string StageId => stageId;
    public string DisplayName => displayName;
    public WaveDefinitionSO[] Waves => waves;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            stageId = "Stage_001";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = stageId;
        }
    }
}
