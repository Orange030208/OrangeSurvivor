using System.Collections.Generic;

public interface IHitModifierProvider
{
    public IEnumerable<IHitModifier> GetHitModifiers(HitModifierTiming modifierTiming);
}