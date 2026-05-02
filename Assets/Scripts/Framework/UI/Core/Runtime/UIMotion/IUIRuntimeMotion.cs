using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using DG.Tweening;

public interface IUIRuntimeMotion
{
    Tween Play(string clipId, float delay = 0f);
    void SetImmediate(string clipId, bool atEnd = true);
    void RefreshDefaults();
    void Kill();
}
}
