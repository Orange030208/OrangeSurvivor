using System;
using UnityEngine;

public class DamageTextManager:MonoBehaviour
{
    [SerializeField]private DamageTextFlow DamageTextPrefab;

    private void OnEnable()
    {
        MeleeEnemy.onDamageTaken += InstantiateDamageText;
    }

    private void OnDisable()
    {
        MeleeEnemy.onDamageTaken -= InstantiateDamageText;
    }

    public void InstantiateDamageText(int damage,Vector2 position)
    {
        DamageTextFlow damageText = Instantiate(DamageTextPrefab, position + Vector2.up * 1.5f, Quaternion.identity,transform);
        damageText.SetDamage(damage);
    }
}