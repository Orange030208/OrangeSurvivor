using System.Collections.Generic;
using UnityEngine;

public static class HitService
{
    /// <summary>
    /// 计算伤害信息，但是不应用到对应实体上
    /// </summary>
    public static HitResult Resolve(HitRequest request)
    {
        List<IHitModifier> sourceModifiers = CollectModifiers(request.Source,HitModifierTiming.Deal);
        List<IHitModifier> targetModifiers = CollectModifiers(request.Target, HitModifierTiming.Receive);
        sourceModifiers.AddRange(targetModifiers);
        HitResolver resolver = new HitResolver(sourceModifiers);
        return resolver.Resolve(request);
    }

    /// <summary>
    /// 计算伤害信息并应用到对应实体上
    /// </summary>
    public static HitResult Apply(HitRequest request)
    {
        HitResult result = Resolve(request);

        if (request.Target != null && request.Target.TryGetComponent(out HealthComponent healthComponent))
        {
            if (healthComponent.TryApplyHitResult(result, out HitResult appliedResult))
            {
                if (appliedResult.DamageSource is IDamageDealtNotifier notifier)
                {
                    notifier.NotifyDamageDealt(appliedResult);
                }

                return appliedResult;
            }
        }

        return result;
    }

    private static List<IHitModifier> CollectModifiers(Entity entity,HitModifierTiming modifierTiming)
    {
        List<IHitModifier> modifiers = new List<IHitModifier>();
        if (entity == null)
        {
            return modifiers;
        }

        IHitModifierProvider[] providers = entity.GetComponents<IHitModifierProvider>();
        for (int i = 0; i < providers.Length; i++)
        {
            IHitModifierProvider provider = providers[i];
            if (provider == null)
            {
                continue;
            }

            IEnumerable<IHitModifier> providedModifiers = provider.GetHitModifiers(modifierTiming);
            if (providedModifiers == null)
            {
                continue;
            }

            modifiers.AddRange(providedModifiers);
        }

        return modifiers;
    }
}
