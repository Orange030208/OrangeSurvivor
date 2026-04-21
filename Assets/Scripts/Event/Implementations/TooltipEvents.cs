using UnityEngine;

public struct ShowTooltipRequestedEvent : IGameEvent
{
    public DisplayDocument Document;
    public Vector2 ScreenPosition;

    public ShowTooltipRequestedEvent(DisplayDocument document, Vector2 screenPosition)
    {
        Document = document;
        ScreenPosition = screenPosition;
    }
}

public struct HideTooltipRequestedEvent : IGameEvent
{
}
