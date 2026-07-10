using System.Collections.Generic;
using UnityEngine;

public static class RunProgressionEnemyScaling
{
    private const string SOURCE_ID = "RUN_PROGRESSION_ENEMY_SCALE";
    private const float MIN_EFFECTIVE_MULTIPLIER_DELTA = 0.0001f;

    public static string SourceId => SOURCE_ID;

    public static RunProgressionEnemyScale ApplyTagPressure(
        RunProgressionEnemyScale scale,
        WaveEnemyTag enemyTags,
        IReadOnlyList<RunProgressionTagPressureRule> tagPressureRules)
    {
        if (tagPressureRules == null)
        {
            return scale;
        }

        for (int i = 0; i < tagPressureRules.Count; i++)
        {
            RunProgressionTagPressureRule rule = tagPressureRules[i];
            if (rule.tag == WaveEnemyTag.None || (enemyTags & rule.tag) == 0)
            {
                continue;
            }

            IReadOnlyList<RunProgressionPropMultiplier> propMultipliers = rule.propMultipliers;
            if (propMultipliers == null)
            {
                continue;
            }

            for (int multiplierIndex = 0; multiplierIndex < propMultipliers.Count; multiplierIndex++)
            {
                RunProgressionPropMultiplier propMultiplier = propMultipliers[multiplierIndex];
                scale.MultiplyMultiplier(propMultiplier.propType, propMultiplier.multiplier);
            }
        }

        return scale;
    }

    public static List<PropModifierData> BuildModifiers(RunProgressionEnemyScale scale)
    {
        List<PropModifierData> modifiers = new();
        IReadOnlyList<RunProgressionPropMultiplier> propMultipliers = scale.PropMultipliers;
        for (int i = 0; i < propMultipliers.Count; i++)
        {
            RunProgressionPropMultiplier propMultiplier = propMultipliers[i];
            AddMultiplier(modifiers, propMultiplier.propType, propMultiplier.multiplier);
        }

        return modifiers;
    }

    private static void AddMultiplier(List<PropModifierData> modifiers, PropType propType, float multiplier)
    {
        if (modifiers == null || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
        {
            return;
        }

        float delta = multiplier - 1f;
        if (Mathf.Abs(delta) <= MIN_EFFECTIVE_MULTIPLIER_DELTA)
        {
            return;
        }

        modifiers.Add(new PropModifierData(propType, PropModifierType.FinalMultiplier, Mathf.RoundToInt(delta * 100f)));
    }
}
