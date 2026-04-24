public class EnemyMoveComponent:MoveBase
{
    protected Enemy owner;
    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner as Enemy;
        speed = this.owner.EnemyData.moveSpeed;
    }
}