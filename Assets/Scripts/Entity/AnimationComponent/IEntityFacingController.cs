using UnityEngine;

public interface IEntityFacingController
{
    void FaceDefault();
    void FaceTarget(Entity target);
    void FaceDirection(Vector2 direction);
    void FaceMoveDirection(IMovable movable);
}
