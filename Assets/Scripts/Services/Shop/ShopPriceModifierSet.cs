using System;
using System.Collections.Generic;

/// <summary>
/// 货架级价格修饰集合。不同来源独立登记，避免多个商店构筑互相覆盖。
/// </summary>
public sealed class ShopPriceModifierSet
{
    private readonly Dictionary<string, float> multipliers = new();

    public float EffectiveMultiplier
    {
        get
        {
            float result = 1f;
            foreach (KeyValuePair<string, float> pair in multipliers)
            {
                result *= pair.Value > 0f ? pair.Value : 1f;
            }

            return result;
        }
    }

    public void SetMultiplier(string sourceId, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("价格修饰来源不能为空。", nameof(sourceId));
        }

        multipliers[sourceId] = multiplier > 0f ? multiplier : 1f;
    }

    public void RemoveMultiplier(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        multipliers.Remove(sourceId);
    }

    public void Clear()
    {
        multipliers.Clear();
    }
}
