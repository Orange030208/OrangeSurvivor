using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// UI 运行时动效抽象基类。
/// 统一把运行时动效约束落到可序列化的 MonoBehaviour 基类上，
/// 避免在 Inspector、GetComponent 与字段引用中依赖接口类型中转。
/// </summary>
public abstract class UIRuntimeMotionBase : MonoBehaviour, IUIRuntimeMotion, IStringConfig
{
    [SerializeField] private string currentConfigOption = string.Empty;

    public string CurrentConfigOption => currentConfigOption;

    public abstract Tween Play(UIMotionAction action, float delay = 0f);
    public abstract void SetImmediate(UIMotionAction action);
    public abstract void RefreshDefaults();
    public abstract void Kill();

    public virtual Tween PlayVisibility(UIVisibilityMotion motion, float delay = 0f)
    {
        return Play(UIMotionActionMapper.ToLegacyAction(motion), delay);
    }

    public virtual Tween PlayInteraction(UIInteractionMotion motion, float delay = 0f)
    {
        return Play(UIMotionActionMapper.ToLegacyAction(motion), delay);
    }

    public virtual void SetVisibilityImmediate(UIVisibilityMotion motion)
    {
        SetImmediate(UIMotionActionMapper.ToLegacyAction(motion));
    }

    public virtual void SetInteractionImmediate(UIInteractionMotion motion)
    {
        SetImmediate(UIMotionActionMapper.ToLegacyAction(motion));
    }

    // 扩展说明：子类通过该接口显式声明支持的动作，供 Inspector 与调用方做安全过滤。
    public virtual bool SupportsAction(UIMotionAction action)
    {
        return false;
    }

    public virtual List<string> GetOptionList()
    {
        return new List<string>();
    }

    public virtual void ApplyConfigByString(string selectedOption)
    {
        currentConfigOption = selectedOption ?? string.Empty;
    }

    public IReadOnlyList<UIMotionAction> GetSupportedActions()
    {
        List<UIMotionAction> supportedActions = new();
        UIMotionAction[] actions = (UIMotionAction[])Enum.GetValues(typeof(UIMotionAction));
        for (int i = 0; i < actions.Length; i++)
        {
            UIMotionAction action = actions[i];
            if (SupportsAction(action))
            {
                supportedActions.Add(action);
            }
        }

        return supportedActions;
    }

    protected void SetCurrentConfigOption(string selectedOption)
    {
        currentConfigOption = selectedOption ?? string.Empty;
    }
}
