using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
public class DropsDetector : EntityComponentBase
{
    private const float MIN_DETECT_INTERVAL = 0.01f;
    private const string COLLECTOR_LAYER_NAME = "Collector";

    [SerializeField] private float timeToDetect = 0.2f;
    [SerializeField] private int collectLayerMask;
    private Entity owner;
    private float detectTimer;
    private PropertiesManager propertiesManager;
    private float detectRadius;

    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        propertiesManager = GetComponent<PropertiesManager>();
        
        detectTimer = 0;
        collectLayerMask = LayerMask.GetMask(COLLECTOR_LAYER_NAME);
        UpdateRadius();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void OnEnableComponent()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateRadius;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    public override void OnDisableComponent()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateRadius;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
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

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        if (propType == PropType.PickupRadius)
        {
            detectRadius = Mathf.Max(0f, newValue);
        }
    }

    private void UpdateRadius()
    {
        if (propertiesManager == null) return;
        detectRadius = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.PickupRadius));
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
