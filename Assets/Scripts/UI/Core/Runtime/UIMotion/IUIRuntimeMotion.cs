using DG.Tweening;

public interface IUIRuntimeMotion
{
    Tween Play(UIMotionAction action, float delay = 0f);
    Tween PlayVisibility(UIVisibilityMotion motion, float delay = 0f);
    Tween PlayInteraction(UIInteractionMotion motion, float delay = 0f);
    void SetImmediate(UIMotionAction action);
    void SetVisibilityImmediate(UIVisibilityMotion motion);
    void SetInteractionImmediate(UIInteractionMotion motion);
    void RefreshDefaults();
    void Kill();
}
