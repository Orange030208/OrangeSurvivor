using UnityEngine;

public static class DamageTextRuntime
{
    private const string DAMAGE_TEXT_PREFAB_RESOURCE_PATH = "Prefabs/Effects/Damage Text";
    private const string DAMAGE_TEXT_CONFIG_RESOURCE_PATH = "Configs/Damage Text Visual Config";
    private const bool DEFAULT_SHOW_ENEMY_DAMAGE_ONLY = true;
    private const bool DEFAULT_HIDE_ZERO_DAMAGE = true;
    private const float DEFAULT_SPAWN_SPREAD_X = 0.18f;

    private static GameObject cachedPrefabObject;
    private static DamageTextFlow cachedPrefabFlow;
    private static DamageTextVisualConfigSO cachedConfig;
    private static bool hasResolvedConfig;
    private static bool isSubscribed;
    private static readonly DamageTextVisualStyle DefaultNormalStyle = DamageTextVisualStyle.CreateDefaultNormal();
    private static readonly DamageTextVisualStyle DefaultCriticalStyle = DamageTextVisualStyle.CreateDefaultCritical();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (isSubscribed)
        {
            GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        cachedPrefabObject = null;
        cachedPrefabFlow = null;
        cachedConfig = null;
        hasResolvedConfig = false;
        isSubscribed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureSubscribed();
        ResolveConfig();
    }

    public static void Show(DamageTextViewData viewData)
    {
        DamageTextVisualConfigSO config = ResolveConfig();
        Show(viewData, config);
    }

    private static void OnEntityDamaged(EntityDamagedEvent damageEvent)
    {
        DamageTextVisualConfigSO config = ResolveConfig();
        if (ShouldSkip(damageEvent, config))
        {
            return;
        }

        HitResult hitResult = damageEvent.HitResult;
        Vector3 spawnPosition = ResolveSpawnPosition(hitResult.HitPoint, config);
        DamageTextViewData viewData = new(hitResult.FinalDamage, hitResult.IsCritical, spawnPosition);
        Show(viewData, config);
    }

    private static void Show(DamageTextViewData viewData, DamageTextVisualConfigSO config)
    {
        DamageTextFlow damageText = CreateInstance(viewData.WorldPosition);
        if (damageText == null)
        {
            return;
        }

        DamageTextVisualStyle style = ResolveStyle(viewData.IsCritical, config);
        damageText.Play(viewData, style);
    }

    private static bool ShouldSkip(EntityDamagedEvent damageEvent, DamageTextVisualConfigSO config)
    {
        bool showEnemyDamageOnly = config != null ? config.ShowEnemyDamageOnly : DEFAULT_SHOW_ENEMY_DAMAGE_ONLY;
        if (showEnemyDamageOnly && damageEvent.Entity is not Enemy)
        {
            return true;
        }

        HitResult hitResult = damageEvent.HitResult;
        if (hitResult.IsCancelled || hitResult.IsDodged || hitResult.IsBlocked)
        {
            return true;
        }

        bool hideZeroDamage = config != null ? config.HideZeroDamage : DEFAULT_HIDE_ZERO_DAMAGE;
        return hideZeroDamage && hitResult.FinalDamage <= 0f;
    }

    private static Vector3 ResolveSpawnPosition(Vector2 hitPoint, DamageTextVisualConfigSO config)
    {
        Vector2 offset = config != null ? config.SpawnOffset : DamageTextVisualConfigSO.DEFAULT_SPAWN_OFFSET;
        float spreadX = config != null ? config.SpawnSpreadX : DEFAULT_SPAWN_SPREAD_X;
        if (spreadX > 0f)
        {
            offset.x += Random.Range(-spreadX, spreadX);
        }

        return hitPoint + offset;
    }

    private static DamageTextFlow CreateInstance(Vector3 position)
    {
        DamageTextFlow prefab = ResolvePrefab();
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(cachedPrefabObject, position, Quaternion.identity);
        return instance.GetComponent<DamageTextFlow>();
    }

    private static DamageTextFlow ResolvePrefab()
    {
        if (cachedPrefabFlow != null)
        {
            return cachedPrefabFlow;
        }

        cachedPrefabObject = Resources.Load<GameObject>(DAMAGE_TEXT_PREFAB_RESOURCE_PATH);
        if (cachedPrefabObject == null)
        {
            Debug.LogWarning($"{nameof(DamageTextRuntime)} 未在 Resources/{DAMAGE_TEXT_PREFAB_RESOURCE_PATH} 找到伤害飘字 prefab。");
            return null;
        }

        cachedPrefabFlow = cachedPrefabObject.GetComponent<DamageTextFlow>();
        if (cachedPrefabFlow == null)
        {
            Debug.LogWarning($"{nameof(DamageTextRuntime)} 的 prefab 缺少 {nameof(DamageTextFlow)} 组件。");
        }

        return cachedPrefabFlow;
    }

    private static DamageTextVisualConfigSO ResolveConfig()
    {
        if (hasResolvedConfig)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<DamageTextVisualConfigSO>(DAMAGE_TEXT_CONFIG_RESOURCE_PATH);
        hasResolvedConfig = true;
        return cachedConfig;
    }

    private static DamageTextVisualStyle ResolveStyle(bool isCritical, DamageTextVisualConfigSO config)
    {
        if (config != null)
        {
            return config.GetStyle(isCritical);
        }

        return isCritical ? DefaultCriticalStyle : DefaultNormalStyle;
    }

    private static void EnsureSubscribed()
    {
        if (isSubscribed)
        {
            return;
        }

        GameEventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        isSubscribed = true;
    }
}
