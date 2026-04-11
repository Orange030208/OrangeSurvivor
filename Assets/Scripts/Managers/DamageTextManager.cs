using System;
using UnityEngine;

public class DamageTextManager:MonoBehaviour
{
    [SerializeField]private DamageTextFlow DamageTextPrefab;

    private void OnEnable()
    {
        GameEventBus.Subscribe<EntityDamagedEvent>(InstantiateDamageText);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EntityDamagedEvent>(InstantiateDamageText);
    }

    public void InstantiateDamageText(EntityDamagedEvent damageEvent)
    {
        if (damageEvent.Entity is not Enemy)
        {
            return;
        }

        DamageInfo damageInfo = damageEvent.DamageInfo;
        DamageTextFlow damageText = Instantiate(DamageTextPrefab, damageInfo.position + Vector2.up * 1.5f, Quaternion.identity,transform);
        damageText.SetDamage(damageInfo.damage,damageInfo.isCritical);
    }
}
