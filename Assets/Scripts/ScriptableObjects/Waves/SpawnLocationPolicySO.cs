using UnityEngine;

/// <summary>
/// 刷怪位置策略资源。
/// 用于描述某一波的敌人生成位置如何围绕锚点或边界进行解算。
/// </summary>
public enum SpawnLocationPolicyType
{
    AroundPlayerRing = 0,
    RandomInsideMap = 1,
    RandomMapEdge = 2
}

[CreateAssetMenu(fileName = "Spawn Location Policy", menuName = ScriptableObjectMenuPaths.SPAWN_LOCATION_POLICY, order = 0)]
public class SpawnLocationPolicySO : ScriptableObject
{
    private const float MIN_DISTANCE = 0.1f;
    private const float MIN_BOUNDS_PADDING = 0f;
    private const int MIN_RESOLVE_ATTEMPTS = 1;
    private const float MIN_SPAWN_CLEARANCE = 0f;
    private const string DEFAULT_OBSTACLE_LAYER_NAME = "Wall";

    [SerializeField] private SpawnLocationPolicyType policyType = SpawnLocationPolicyType.AroundPlayerRing;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float boundsPadding = 1f;
    [SerializeField] private int resolveAttempts = 16;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float spawnClearance = 0.1f;
    [SerializeField] private Vector2 minBounds = new(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new(10f, 10f);

    public SpawnLocationPolicyType PolicyType => policyType;
    public float MinDistance => Mathf.Max(MIN_DISTANCE, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance, maxDistance);
    public float BoundsPadding => Mathf.Max(MIN_BOUNDS_PADDING, boundsPadding);
    public int ResolveAttempts => Mathf.Max(MIN_RESOLVE_ATTEMPTS, resolveAttempts);
    public LayerMask ObstacleLayerMask => obstacleLayerMask.value != 0
        ? obstacleLayerMask
        : LayerMask.GetMask(DEFAULT_OBSTACLE_LAYER_NAME);
    public float SpawnClearance => Mathf.Max(MIN_SPAWN_CLEARANCE, spawnClearance);
    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;

    private void OnValidate()
    {
        minDistance = Mathf.Max(MIN_DISTANCE, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        boundsPadding = Mathf.Max(MIN_BOUNDS_PADDING, boundsPadding);
        resolveAttempts = Mathf.Max(MIN_RESOLVE_ATTEMPTS, resolveAttempts);
        spawnClearance = Mathf.Max(MIN_SPAWN_CLEARANCE, spawnClearance);

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
