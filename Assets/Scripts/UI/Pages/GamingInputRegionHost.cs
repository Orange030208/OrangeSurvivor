using UnityEngine;

public sealed class GamingInputRegionHost
{
    private readonly Component host;
    private MobileJoystick moveJoystick;

    public GamingInputRegionHost(Component host, MobileJoystick moveJoystick)
    {
        this.host = host;
        this.moveJoystick = moveJoystick;
    }

    public void WarmUp()
    {
        if (moveJoystick == null && host != null)
        {
            moveJoystick = host.GetComponentInChildren<MobileJoystick>(true);
        }
    }

    public Vector2 ReadMoveDirection()
    {
        return moveJoystick != null ? moveJoystick.GetMoveDirection() : Vector2.zero;
    }

    public void PublishCurrentInput()
    {
        GameEventBus.Publish(new PlayerMoveInputChangedEvent(ReadMoveDirection()));
    }

    public void ResetInput()
    {
        GameEventBus.Publish(new PlayerMoveInputChangedEvent(Vector2.zero));
    }
}
