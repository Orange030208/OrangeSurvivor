public interface IBuffStackAwareFeatureEffect
{
    void OnBuffStackChanged(FeatureContext context, int currentStackCount, int maxStackCount);
}
