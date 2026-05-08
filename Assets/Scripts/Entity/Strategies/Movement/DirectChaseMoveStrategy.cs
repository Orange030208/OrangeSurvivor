using System;

public sealed class DirectChaseMoveStrategy : IMoveStrategy
{
    private readonly IMovable movable;

    public DirectChaseMoveStrategy(IMovable movable)
    {
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        movable.MoveTo(target.Center);
    }
}
