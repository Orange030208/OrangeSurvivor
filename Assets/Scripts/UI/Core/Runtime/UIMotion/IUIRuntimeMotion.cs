using DG.Tweening;

public interface IUIRuntimeMotion
{
    Tween Play(UIMotionAction action, float delay = 0f);
    void SetImmediate(UIMotionAction action);
    void RefreshDefaults();
    void Kill();
}
