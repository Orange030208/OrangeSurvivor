using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class HitResolver
{
    private readonly List<IHitModifier> modifiers =  new List<IHitModifier>();

    public HitResolver(IEnumerable<IHitModifier> modifiers)
    {
        IHitModifier startModifier = new HitStartModifier();
        IHitModifier calcModifier = new HitCalcModifier();
        this.modifiers.Add(startModifier);
        this.modifiers.Add(calcModifier);
        this.modifiers.AddRange(modifiers);
        this.modifiers.Sort(ComparePriority);
    }

    public HitResult Resolve(HitRequest request)
    {
        HitContext context = new HitContext(request);

        ApplyModifiers(context);

        return new HitResult(
            request.Source,
            request.Target,
            context.Damage,
            context.KnockbackStrength,
            request.HitPoint,
            context.HasKnockbackDirection,
            context.KnockbackDirection,
            context.IsCritical,
            context.IsDodged,
            context.IsBlocked,
            context.IsCancelled,
            request.SourceKind,
            request.SourcePosition,
            request.SourceWeapon);
    }

    private void ApplyModifiers(HitContext context)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            IHitModifier modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            modifier.ModifyHit(context);
            if (context.IsCancelled)
            {
                return;
            }
        }
    }

    private static int ComparePriority(IHitModifier left, IHitModifier right)
    {
        return left.HitPriority.CompareTo(right.HitPriority);
    }
}
