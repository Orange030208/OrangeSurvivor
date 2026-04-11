using UnityEngine;

[RequireComponent(typeof(IEntity))]
[RequireComponent(typeof(PropertiesManager))]
public class DropsDetector : MonoBehaviour
{
    private const float MIN_DETECT_INTERVAL = 0.01f;
    private const string COLLECTOR_LAYER_NAME = "Collector";

    [SerializeField] private float timeToDetect = 0.2f;
    [SerializeField] private int collectLayerMask;
    private float detectTimer;
    private IEntity _entity;
    private PropertiesManager propertiesManager;
    private float detectRadius;

    private void Awake()
    {
        _entity = GetComponent<IEntity>();
        propertiesManager = GetComponent<PropertiesManager>();
    }

    private void OnEnable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateRadius;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    private void OnDisable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateRadius;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    private void Start()
    {
        detectTimer = 0;
        collectLayerMask = LayerMask.GetMask(COLLECTOR_LAYER_NAME);
        UpdateRadius();
    }

    private void Update()
    {
        detectTimer -= Time.deltaTime;
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
        if (_entity == null || collectLayerMask == 0 || detectRadius <= 0)
        {
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRadius, collectLayerMask);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Collection collector))
            {
                collector.TryCollect(_entity);
            }
        }
    }
}
