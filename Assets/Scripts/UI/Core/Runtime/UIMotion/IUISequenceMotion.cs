using DG.Tweening;

public interface IUISequenceMotion
{
    void PrepareEnter();
    Tween PlayEnter(float delay = 0f);
    Tween PlayExit(float delay = 0f);
    Tween PlayVisibility(UIVisibilityMotion motion, float delay = 0f);
    void SetHiddenImmediate();
    void SetVisibilityImmediate(UIVisibilityMotion motion);
    void CompleteImmediate();
    void Kill();
}
