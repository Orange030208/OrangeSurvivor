public static class UIMotionActionMapper
{
    public static UIMotionAction ToLegacyAction(UIVisibilityMotion motion)
    {
        return motion switch
        {
            UIVisibilityMotion.Visible => UIMotionAction.Common,
            UIVisibilityMotion.Hidden => UIMotionAction.Hide,
            UIVisibilityMotion.Enter => UIMotionAction.Show,
            UIVisibilityMotion.Exit => UIMotionAction.Hide,
            _ => UIMotionAction.Common
        };
    }

    public static UIMotionAction ToLegacyAction(UIInteractionMotion motion)
    {
        return motion switch
        {
            UIInteractionMotion.Normal => UIMotionAction.Common,
            UIInteractionMotion.Hover => UIMotionAction.Enter,
            UIInteractionMotion.Unhover => UIMotionAction.Exit,
            UIInteractionMotion.Pressed => UIMotionAction.Press,
            UIInteractionMotion.Released => UIMotionAction.Release,
            UIInteractionMotion.ClickPulse => UIMotionAction.Emphasis,
            _ => UIMotionAction.Common
        };
    }

    public static UIInteractionMotion ToInteractionMotion(UIMotionAction action, UIMotionEvent fallbackEvent)
    {
        return action switch
        {
            UIMotionAction.Common => UIInteractionMotion.Normal,
            UIMotionAction.Enter => UIInteractionMotion.Hover,
            UIMotionAction.Exit => UIInteractionMotion.Unhover,
            UIMotionAction.Press => UIInteractionMotion.Pressed,
            UIMotionAction.Release => UIInteractionMotion.Released,
            UIMotionAction.Emphasis => UIInteractionMotion.ClickPulse,
            _ => ToInteractionMotion(fallbackEvent)
        };
    }

    public static UIInteractionMotion ToInteractionMotion(UIMotionEvent motionEvent)
    {
        return motionEvent switch
        {
            UIMotionEvent.PointerEnter => UIInteractionMotion.Hover,
            UIMotionEvent.PointerExit => UIInteractionMotion.Unhover,
            UIMotionEvent.PointerDown => UIInteractionMotion.Pressed,
            UIMotionEvent.PointerUp => UIInteractionMotion.Released,
            UIMotionEvent.PointerClick => UIInteractionMotion.ClickPulse,
            _ => UIInteractionMotion.Normal
        };
    }
}
