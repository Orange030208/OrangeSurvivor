using UnityEngine;

[RequireComponent(typeof(AttributeManager))]
public class DropsDetector : EntityComponentBase
{
    private const float MIN_DETECT_INTERVAL = 0.01f;
    private const string COLLECTOR_LAYER_NAME = "Collector";

    [SerializeField] private float timeToDetect = 0.2f;
    [SerializeField] private int collectLayerMask;
    private Entity owner;
    private float detectTimer;
    private AttributeManager AttributeManager;
    private float detectRadius;

    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        AttributeManager = GetComponent<AttributeManager>();
        
        detectTimer = 0;
        collectLayerMask = LayerMask.GetMask(COLLECTOR_LAYER_NAME);
        UpdateRadius();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void OnEnableComponent()
    {
        if (AttributeManager != null)
        {
            AttributeManager.OnAttributesChanged += UpdateRadius;
            AttributeManager.SubscribeAttributeChanged(PropType.PickupRadius, OnPickupRadiusChanged);
        }
    }

    public override void OnDisableComponent()
    {
        if (AttributeManager != null)
        {
            AttributeManager.OnAttributesChanged -= UpdateRadius;
            AttributeManager.UnsubscribeAttributeChanged(PropType.PickupRadius, OnPickupRadiusChanged);
        }
    }

    public override void OnTick(float deltaTime)
    {
        detectTimer -= deltaTime;
        if (detectTimer <= 0)
        {
            Detect();
            detectTimer = Mathf.Max(timeToDetect, MIN_DETECT_INTERVAL);
        }
    }

    private void OnPickupRadiusChanged(int newValue)
    {
        RefreshDetectRadius(newValue);
    }

    private void UpdateRadius()
    {
        if (AttributeManager == null) return;
        RefreshDetectRadius(AttributeManager.GetAttributeValue(PropType.PickupRadius));
    }

    private void RefreshDetectRadius(float radiusPoints)
    {
        detectRadius = PropValueUtility.DistancePointsToNonNegativeWorldUnits(radiusPoints);
    }

    private void Detect()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(owner.Center, detectRadius, collectLayerMask);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Collection collector))
            {
                collector.TryCollect(owner);
            }
        }
    }
}
