using System;
using System.Collections.Generic;
using System.Text;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class StateScopedPropertyModifierFeature : FeatureBase
{
    [SerializeField] private GameState targetState = GameState.Shop;
    [SerializeField] private List<PropModifierData> modifiers = new();

    private string runtimeSourceId;
    private bool isApplied;

    public override string Title => "状态属性加成";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
        ApplyForCurrentState();
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged);
        RemoveModifiers();
        runtimeSourceId = null;
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == targetState)
        {
            ApplyModifiers();
            return;
        }

        if (eventData.OldState == targetState)
        {
            RemoveModifiers();
        }
    }

    private void ApplyForCurrentState()
    {
        if (GameServices.TryGet(out IGameFlowController gameFlowController) &&
            gameFlowController.CurrentGameState == targetState)
        {
            ApplyModifiers();
            return;
        }

    }

    private void ApplyModifiers()
    {
        if (isApplied || Context?.PropertiesManager == null || modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        Context.PropertiesManager.AddModifiers(runtimeSourceId, modifiers);
        isApplied = true;
    }

    private void RemoveModifiers()
    {
        if (!isApplied || Context?.PropertiesManager == null)
        {
            return;
        }

        Context.PropertiesManager.RemoveModifiers(runtimeSourceId);
        isApplied = false;
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(StateScopedPropertyModifierFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(StateScopedPropertyModifierFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return $"处于{GetGameStateDisplayName(targetState)}时未配置任何属性加成。";
        }

        return $"处于{GetGameStateDisplayName(targetState)}时，{BuildModifierSummary(modifiers)}。";
    }

    private static string BuildModifierSummary(IReadOnlyList<PropModifierData> propertyModifiers)
    {
        StringBuilder builder = new();
        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('，');
            }

            builder.Append(propertyModifiers[i].GetAutoDescription());
        }

        return builder.ToString();
    }

    private static string GetGameStateDisplayName(GameState gameState)
    {
        return gameState switch
        {
            GameState.Menu => "主菜单",
            GameState.Game => "战斗中",
            GameState.GameOver => "结算失败界面",
            GameState.StageComplete => "通关结算界面",
            GameState.Shop => "商店",
            _ => gameState.ToString()
        };
    }
}
