using UnityEngine;

[RequireComponent(typeof(IAnimatable))]
public class CoinBrain : EntityBrain
{
    private IAnimatable animatable;
    private Collection collection;
    private EntityAnimationConfig animConfig;

    public override Entity Owner => collection;

    protected override void OnInitialize(Entity owner)
    {
        collection = owner as Collection;
        if (collection == null)
        {
            throw new System.ArgumentException(
                $"{nameof(CoinBrain)} requires a {nameof(Collection)} owner.", nameof(owner));
        }

        animatable = collection.GetComponent<IAnimatable>();
        animConfig = collection.AnimationConfig;
    }

    protected override void OnBrainStart()
    {
        if (animatable == null)
        {
            Debug.LogError($"[CoinBrain] {nameof(IAnimatable)} is missing on {collection.name}.", collection);
            return;
        }

        if (animConfig == null)
        {
            Debug.LogError($"[CoinBrain] {nameof(EntityAnimationConfig)} is missing on {collection.name}.", collection);
            return;
        }

        animatable.PlayState(animConfig.FloatHash);
    }

    public override void StopBrain()
    {
        enabled = false;
    }

    public override void StartBrain()
    {
        enabled = true;
    }
}
