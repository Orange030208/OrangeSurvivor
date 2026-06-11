
namespace Orange.UIFramework
{
    using DG.Tweening;

public interface IUISequenceMotion
{
    void PrepareEnter();
    void RefreshDefaults();
    Tween PlayEnter(float delay = 0f);
    Tween PlayExit(float delay = 0f);
    void SetHiddenImmediate();
    void CompleteImmediate();
    void Kill();
}
}
