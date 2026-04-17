using System;
using UnityEngine;

public class Candy : Collection
{
    private const int BASE_EXPERIENCE_VALUE = 1;

    [SerializeField] private int experienceValue = BASE_EXPERIENCE_VALUE;

    protected override void OnCollected(IEntity entity)
    {
        PlayerLevel levelComponent = entity.Transform.GetComponent<PlayerLevel>();
        if (levelComponent == null)
        {
            return;
        }

        levelComponent.AddXP(Mathf.Max(BASE_EXPERIENCE_VALUE, experienceValue));
    }
}
