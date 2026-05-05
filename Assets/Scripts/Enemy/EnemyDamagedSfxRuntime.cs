using UnityEngine;

/// <summary>
/// 将“敌人实际受到伤害”事件转换为敌人自身配置的受击音效播放请求。
/// </summary>
public static class EnemyDamagedSfxRuntime
{
    private static bool isSubscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (isSubscribed)
        {
            GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        isSubscribed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureSubscribed();
    }

    private static void OnEntityDamaged(EntityDamagedEvent damageEvent)
    {
        if (damageEvent.Entity is not Enemy enemy || enemy.EnemyData == null)
        {
            return;
        }

        HitResult hitResult = damageEvent.HitResult;
        if (hitResult.IsCancelled || hitResult.IsDodged || hitResult.IsBlocked || hitResult.FinalDamage <= 0f)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(enemy.EnemyData.DamagedSfxKey);
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
