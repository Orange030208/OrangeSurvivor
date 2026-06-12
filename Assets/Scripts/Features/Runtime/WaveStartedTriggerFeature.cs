using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class WaveStartedTriggerFeature : FeatureBase
{
    [Header("波次开始属性修饰")]
    [SerializeField] private List<PropModifierData> waveStartPropertyModifiers = new();
    [SerializeField, Min(0f)] private float propertyModifierDurationSeconds;

    [Header("波次开始附加行为")]
    [SerializeField] private bool resetSameSourceDamageImmunityOnWaveStart = true;

    private string runtimeSourceId;
    private float propertyModifierRemainingSeconds;
    private bool hasTimedPropertyModifier;

    public override string Title => "波次开始触发";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        YokiFrame.EventKit.Type.Register<WaveStartedEvent>(OnWaveStarted);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<WaveStartedEvent>(OnWaveStarted);
        RemoveWaveStartPropertyModifiers();
        runtimeSourceId = null;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!hasTimedPropertyModifier || deltaTime <= 0f)
        {
            return;
        }

        propertyModifierRemainingSeconds = Mathf.Max(0f, propertyModifierRemainingSeconds - deltaTime);
        if (propertyModifierRemainingSeconds > 0f)
        {
            return;
        }

        RemoveWaveStartPropertyModifiers();
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        ApplyWaveStartPropertyModifiers();
        ResetSameSourceImmunityFeatures();
    }

    private void ApplyWaveStartPropertyModifiers()
    {
        if (Context?.PropertiesManager == null || waveStartPropertyModifiers == null || waveStartPropertyModifiers.Count == 0)
        {
            return;
        }

        RemoveWaveStartPropertyModifiers();
        Context.PropertiesManager.AddModifiers(runtimeSourceId, waveStartPropertyModifiers);
        if (propertyModifierDurationSeconds > 0f)
        {
            propertyModifierRemainingSeconds = propertyModifierDurationSeconds;
            hasTimedPropertyModifier = true;
            return;
        }

        propertyModifierRemainingSeconds = 0f;
        hasTimedPropertyModifier = false;
    }

    private void RemoveWaveStartPropertyModifiers()
    {
        if (Context?.PropertiesManager != null && !string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            Context.PropertiesManager.RemoveModifiers(runtimeSourceId);
        }

        propertyModifierRemainingSeconds = 0f;
        hasTimedPropertyModifier = false;
    }

    private void ResetSameSourceImmunityFeatures()
    {
        if (!resetSameSourceDamageImmunityOnWaveStart)
        {
            return;
        }

        FeatureHostSourceHandle sourceHandle = Context?.FeatureHost?.GetInstalledSourceHandle(SourceId);
        if (sourceHandle == null || sourceHandle.RuntimeEffects == null)
        {
            return;
        }

        for (int i = 0; i < sourceHandle.RuntimeEffects.Count; i++)
        {
            if (sourceHandle.RuntimeEffects[i] is IWaveStartResettableFeatureEffect resettableFeature)
            {
                resettableFeature.ResetForWaveStart();
            }
        }
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(WaveStartedTriggerFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(WaveStartedTriggerFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        List<string> parts = new();
        if (waveStartPropertyModifiers != null && waveStartPropertyModifiers.Count > 0)
        {
            string modifierSummary = BuildModifierSummary(waveStartPropertyModifiers);
            parts.Add(propertyModifierDurationSeconds > 0f
                ? $"每波开始时获得{modifierSummary}，持续 {propertyModifierDurationSeconds:0.##} 秒"
                : $"每波开始时获得{modifierSummary}");
        }

        if (resetSameSourceDamageImmunityOnWaveStart)
        {
            parts.Add("每波开始时重置同源次数免伤");
        }

        if (parts.Count == 0)
        {
            return "未配置任何波次开始触发行为。";
        }

        StringBuilder builder = new();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('，');
            }

            builder.Append(parts[i]);
        }

        builder.Append('。');
        return builder.ToString();
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
}
