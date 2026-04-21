using UnityEngine;

public interface IMovement
{
    public void EnableMovement();
    public void DisableMovement();
    
    public float Speed { get; }
    
    public Vector2 MoveDirection { get; } 
    
    public bool IsMoving { get; }
    
    public static IMovement Empty =  new EmptyMovement();
}

internal class EmptyMovement : IMovement
{
    public void EnableMovement()
    {
        
    }

    public void DisableMovement()
    {
        
    }

    public float Speed => 0;
    public Vector2 MoveDirection => Vector2.zero;
    public bool IsMoving => false;
}