using UnityEngine;

public static class DamageTextRuntime
{
    private const bool DEFAULT_SHOW_ENEMY_DAMAGE_ONLY = true;
    private const bool DEFAULT_HIDE_ZERO_DAMAGE = true;
    private const float DEFAULT_SPAWN_SPREAD_X = 0.18f;

    private static DamageTextFlow cachedPrefabFlow;
    private static bool isSubscribed;
    private static readonly DamageTextVisualStyle DefaultNormalStyle = DamageTextVisualStyle.CreateDefaultNormal();
    private static readonly DamageTextVisualStyle DefaultCriticalStyle = DamageTextVisualStyle.CreateDefaultCritical();
    private static readonly DamageTextVisualStyle DefaultPlayerDamagedStyle = DamageTextVisualStyle.CreateDefaultPlayerDamaged();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (isSubscribed)
        {
            GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        cachedPrefabFlow = null;
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
        Show(viewData, damageEvent.Entity, config);
    }

    private static void Show(DamageTextViewData viewData, DamageTextVisualConfigSO config)
    {
        Show(viewData, null, config);
    }

    private static void Show(DamageTextViewData viewData, Entity damagedEntity, DamageTextVisualConfigSO config)
    {
        DamageTextFlow damageText = CreateInstance(viewData.WorldPosition);
        if (damageText == null)
        {
            return;
        }

        DamageTextVisualStyle style = ResolveStyle(viewData.IsCritical, damagedEntity, config);
        damageText.Play(viewData, style);
    }

    private static bool ShouldSkip(EntityDamagedEvent damageEvent, DamageTextVisualConfigSO config)
    {
        bool showEnemyDamageOnly = config != null ? config.ShowEnemyDamageOnly : DEFAULT_SHOW_ENEMY_DAMAGE_ONLY;
        if (showEnemyDamageOnly && damageEvent.Entity is not Enemy && damageEvent.Entity is not Player)
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

        return Object.Instantiate(prefab, position, Quaternion.identity);
    }

    private static DamageTextFlow ResolvePrefab()
    {
        if (cachedPrefabFlow != null)
        {
            return cachedPrefabFlow;
        }

        cachedPrefabFlow = GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.DamageTextPrefab
            : null;
        if (cachedPrefabFlow == null)
        {
            Debug.LogWarning($"{nameof(DamageTextRuntime)} 未在 {nameof(GameContentCatalogSO)} 中找到伤害飘字 prefab。");
            return null;
        }

        return cachedPrefabFlow;
    }

    private static DamageTextVisualConfigSO ResolveConfig()
    {
        return GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.DamageTextVisualConfig
            : null;
    }

    private static DamageTextVisualStyle ResolveStyle(bool isCritical, Entity damagedEntity, DamageTextVisualConfigSO config)
    {
        if (damagedEntity is Player)
        {
            return config != null ? config.GetPlayerDamagedStyle() : DefaultPlayerDamagedStyle;
        }

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
