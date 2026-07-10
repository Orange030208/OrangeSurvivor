public class EnemyMoveComponent : MoveBase
{
    protected Enemy owner;
    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner as Enemy;

        AttributeManager AttributeManager = this.owner != null ? this.owner.GetComponent<AttributeManager>() : null;
        speed = PropValueUtility.DistancePointsToWorldUnits(AttributeManager.GetAttributeValue(PropType.MoveSpeed));
    }
}
