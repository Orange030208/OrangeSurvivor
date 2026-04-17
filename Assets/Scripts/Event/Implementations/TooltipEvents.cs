using UnityEngine;

public struct ShowTooltipRequestedEvent : IGameEvent
{
    public TooltipDisplayData Data;
    public Vector2 ScreenPosition;

    public ShowTooltipRequestedEvent(TooltipDisplayData data, Vector2 screenPosition)
    {
        Data = data;
        ScreenPosition = screenPosition;
    }
}

public struct HideTooltipRequestedEvent : IGameEvent
{
}
