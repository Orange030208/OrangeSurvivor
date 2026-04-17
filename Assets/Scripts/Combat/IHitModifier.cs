public interface IHitModifier
{
    public int HitPriority { get; }
    public HitModifierTiming HitModifierTiming { get; }
    public void ModifyHit(HitContext hitContext);
}