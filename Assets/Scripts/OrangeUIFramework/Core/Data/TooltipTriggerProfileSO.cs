using Orange.UIFramework;
using UnityEngine;

[CreateAssetMenu(fileName = "Tooltip Trigger Profile", menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "UI/Tooltip Trigger Profile", order = 0)]
public sealed class TooltipTriggerProfileSO : ScriptableObject
{
    [SerializeField] private TooltipTriggerMode triggerMode = TooltipTriggerMode.HoverAndLongPress;
    [Min(0f)]
    [SerializeField] private float hoverDelay = 0.05f;
    [Min(0f)]
    [SerializeField] private float longPressDelay = 0.45f;
    [SerializeField] private bool followPointer = true;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);
    [Min(0f)]
    [SerializeField] private float margin = 12f;
    [SerializeField] private FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight;
    [SerializeField] private bool allowPin;
    [SerializeField] private bool allowInteractiveTransient;

    public TooltipTriggerMode TriggerMode => triggerMode;
    public float HoverDelay => hoverDelay;
    public float LongPressDelay => longPressDelay;
    public bool FollowPointer => followPointer;
    public Vector2 Offset => offset;
    public float Margin => margin;
    public FloatingViewAnchor PreferredAnchor => preferredAnchor;
    public bool AllowPin => allowPin;
    public bool AllowInteractiveTransient => allowInteractiveTransient;

    public TooltipPlacementOptions CreatePlacement(Vector2 screenPosition)
    {
        return new TooltipPlacementOptions(
            screenPosition: screenPosition,
            offset: offset,
            followPointer: followPointer,
            margin: margin,
            preferredAnchor: preferredAnchor,
            useScreenPosition: true);
    }
}
