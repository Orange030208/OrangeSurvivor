using UnityEngine;

public sealed class GamingInputRegionHost
{
    private readonly Component host;
    private MobileJoystick moveJoystick;
    private IPlayerMoveInputReceiver moveInputReceiver;

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

    public void Bind(Player player)
    {
        moveInputReceiver = player != null ? player.GetComponent<IPlayerMoveInputReceiver>() : null;
        ResetInput();
    }

    public void Unbind()
    {
        ResetInput();
        moveInputReceiver = null;
    }

    public void PublishCurrentInput()
    {
        moveInputReceiver?.SetMoveInput(ReadMoveDirection());
    }

    public void ResetInput()
    {
        moveInputReceiver?.SetMoveInput(Vector2.zero);
    }

}
