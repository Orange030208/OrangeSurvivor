using UnityEngine;

public struct ShowTooltipRequestedEvent : IGameEvent
{
    public IDescribable Descriptor;
    public Vector2 ScreenPosition;

    public ShowTooltipRequestedEvent(IDescribable descriptor, Vector2 screenPosition)
    {
        Descriptor = descriptor;
        ScreenPosition = screenPosition;
    }
}

public struct HideTooltipRequestedEvent : IGameEvent
{
}
