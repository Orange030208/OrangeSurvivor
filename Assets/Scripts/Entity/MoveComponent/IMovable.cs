using UnityEngine;

public interface IMovable
{
    public void EnableMovement();
    public void DisableMovement();

    public void MoveTo(Vector2 position);
    
    public void StopMoving();
    
    public float Speed { get; }
    
    public Vector2 MoveDirection { get; } 
    
    public bool IsMoving { get; }
    
    public PropertiesManager PropertiesManager { get; }
    
    public static IMovable Empty =  new EmptyMovable();
}

internal class EmptyMovable : IMovable
{
    public void EnableMovement()
    {
        
    }

    public void DisableMovement()
    {
        
    }

    public void MoveTo(Vector2 position)
    {
        
    }

    public void StopMoving()
    {
        
    }

    public float Speed => 0;
    public Vector2 MoveDirection => Vector2.zero;
    public bool IsMoving => false;
    public PropertiesManager PropertiesManager { get;}
}