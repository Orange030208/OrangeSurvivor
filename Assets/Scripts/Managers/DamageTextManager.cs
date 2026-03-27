using System;
using UnityEngine;

public class DamageTextManager:MonoBehaviour
{
    [SerializeField]private DamageTextFlow DamageTextPrefab;

    private void OnEnable()
    {
        Enemy.onDamageTaken += InstantiateDamageText;
    }

    private void OnDisable()
    {
        Enemy.onDamageTaken -= InstantiateDamageText;
    }

    public void InstantiateDamageText(DamageInfo damageInfo)
    {
        DamageTextFlow damageText = Instantiate(DamageTextPrefab, damageInfo.position + Vector2.up * 1.5f, Quaternion.identity,transform);
        damageText.SetDamage(damageInfo.damage,damageInfo.isCritical);
    }
}