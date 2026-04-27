using UnityEngine;

/// <summary>
/// 刷怪位置策略资源。
/// 用于描述某一波的敌人生成位置如何围绕锚点或边界进行解算。
/// </summary>
public enum SpawnLocationPolicyType
{
    AroundPlayerRing = 0
}

[CreateAssetMenu(fileName = "Spawn Location Policy", menuName = ScriptableObjectMenuPaths.SPAWN_LOCATION_POLICY, order = 0)]
public class SpawnLocationPolicySO : ScriptableObject
{
    private const float MIN_DISTANCE = 0.1f;

    [SerializeField] private SpawnLocationPolicyType policyType = SpawnLocationPolicyType.AroundPlayerRing;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private Vector2 minBounds = new(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new(10f, 10f);

    public SpawnLocationPolicyType PolicyType => policyType;
    public float MinDistance => Mathf.Max(MIN_DISTANCE, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance, maxDistance);
    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;

    private void OnValidate()
    {
        minDistance = Mathf.Max(MIN_DISTANCE, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);

        if (maxBounds.x < minBounds.x)
        {
            maxBounds.x = minBounds.x;
        }

        if (maxBounds.y < minBounds.y)
        {
            maxBounds.y = minBounds.y;
        }
    }
}
