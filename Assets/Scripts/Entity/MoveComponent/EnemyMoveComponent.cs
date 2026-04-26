public class EnemyMoveComponent : MoveBase
{
    protected Enemy owner;
    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner as Enemy;

        PropertiesManager propertiesManager = this.owner != null ? this.owner.GetComponent<PropertiesManager>() : null;
        speed = propertiesManager.GetPropValue(PropType.MoveSpeed);
    }
}
