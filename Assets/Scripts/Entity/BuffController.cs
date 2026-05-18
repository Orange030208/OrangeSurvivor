using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity), typeof(FeatureHost))]
public class BuffController : EntityComponentBase
{
    private const string BUFF_RUNTIME_SOURCE_PREFIX = "BUFF_RUNTIME";
    private const int SINGLE_STACK_CHANGE_COUNT = 1;

    private FeatureHost featureHost;
    private Entity owner;
    private readonly Dictionary<string, List<BuffRuntimeHandle>> buffStacksById = new();
    private readonly List<BuffApplyRequest> waveStartBuffRequests = new();
    private readonly List<string> emptyBuffIds = new();
    public override Entity Owner => owner;

    public event Action<ActiveBuffViewData[]> OnActiveBuffViewDataChanged;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        featureHost = GetComponent<FeatureHost>();
    }

    public override void OnEnableComponent()
    {
        GameEventBus.Subscribe<ApplyBuffRequestedEvent, string>(owner.RuntimeId, OnApplyBuffRequested);
        GameEventBus.Subscribe<RemoveBuffRequestedEvent, string>(owner.RuntimeId, OnRemoveBuffRequested);
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
    }

    public override void OnDisableComponent()
    {
        GameEventBus.Unsubscribe<ApplyBuffRequestedEvent, string>(owner.RuntimeId, OnApplyBuffRequested);
        GameEventBus.Unsubscribe<RemoveBuffRequestedEvent, string>(owner.RuntimeId, OnRemoveBuffRequested);
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);

        ClearAllBuffs();
    }

    public override void OnTick(float deltaTime)
    {
        bool changed = TickTimedBuffs(deltaTime);
        changed |= RemoveEmptyEntries();
        if (changed)
        {
            PublishViewData();
        }
    }

    public bool ApplyBuff(BuffApplyRequest applyRequest)
    {
        if (featureHost == null || applyRequest.BuffData == null)
        {
            return false;
        }

        BuffDataSO buffData = applyRequest.BuffData;
        string buffId = buffData.BuffId;
        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        List<BuffRuntimeHandle> stacks = GetOrCreateStacks(buffId);
        ResolveDuration(applyRequest, buffData, out BuffDurationPolicy durationPolicy, out float durationSeconds);

        int previousStackCount = stacks.Count;
        bool changed = stacks.Count < buffData.MaxStackCount
            ? InstallStack(buffId, buffData, stacks, durationPolicy, durationSeconds)
            : HandleOverflow(buffData, stacks, durationPolicy, durationSeconds);

        if (!changed)
        {
            return false;
        }

        NotifyStackStateChanged(buffData, previousStackCount, stacks.Count);
        PublishViewData();
        return true;
    }

    public bool RegisterWaveStartBuff(BuffApplyRequest applyRequest, bool applyImmediately)
    {
        if (applyRequest.BuffData == null)
        {
            return false;
        }

        waveStartBuffRequests.Add(applyRequest);
        if (applyImmediately)
        {
            ApplyBuff(applyRequest);
        }

        return true;
    }

    public bool RemoveBuff(string buffId)
    {
        if (!TryGetStacks(buffId, out List<BuffRuntimeHandle> stacks))
        {
            return false;
        }

        bool changed = RemoveAllStacks(buffId, stacks);
        if (changed)
        {
            PublishViewData();
        }

        return changed;
    }

    public bool RemoveSingleStack(string buffId)
    {
        if (!TryGetStacks(buffId, out List<BuffRuntimeHandle> stacks) || stacks.Count == 0)
        {
            return false;
        }

        RemoveStackAt(buffId, stacks, stacks.Count - 1, false);
        PublishViewData();
        return true;
    }

    public void ClearAllBuffs()
    {
        List<string> buffIds = new(buffStacksById.Keys);
        for (int i = 0; i < buffIds.Count; i++)
        {
            RemoveAllStacks(buffIds[i], buffStacksById[buffIds[i]]);
        }
    }

    public ActiveBuffViewData[] BuildActiveBuffViewData()
    {
        ActiveBuffViewData[] viewData = new ActiveBuffViewData[buffStacksById.Count];
        int index = 0;

        foreach (KeyValuePair<string, List<BuffRuntimeHandle>> pair in buffStacksById)
        {
            List<BuffRuntimeHandle> stacks = pair.Value;
            if (stacks == null || stacks.Count == 0)
            {
                continue;
            }

            viewData[index++] = BuildMergedViewData(stacks[0].BuffData, stacks);
        }

        if (index == viewData.Length)
        {
            return viewData;
        }

        ActiveBuffViewData[] trimmed = new ActiveBuffViewData[index];
        Array.Copy(viewData, trimmed, index);
        return trimmed;
    }

    private List<BuffRuntimeHandle> GetOrCreateStacks(string buffId)
    {
        if (!buffStacksById.TryGetValue(buffId, out List<BuffRuntimeHandle> stacks))
        {
            stacks = new List<BuffRuntimeHandle>();
            buffStacksById[buffId] = stacks;
        }

        return stacks;
    }

    private bool TickTimedBuffs(float deltaTime)
    {
        bool changed = false;
        emptyBuffIds.Clear();

        List<string> buffIds = new(buffStacksById.Keys);
        for (int keyIndex = 0; keyIndex < buffIds.Count; keyIndex++)
        {
            string buffId = buffIds[keyIndex];
            if (!buffStacksById.TryGetValue(buffId, out List<BuffRuntimeHandle> stacks))
            {
                continue;
            }

            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                BuffRuntimeHandle handle = stacks[i];
                if (!handle.HasDuration)
                {
                    continue;
                }

                handle.Tick(deltaTime);
                changed = true;
                if (handle.IsExpired)
                {
                    RemoveStackAt(buffId, stacks, i, true);
                }
            }

            if (stacks.Count == 0)
            {
                emptyBuffIds.Add(buffId);
            }
        }

        return changed;
    }

    private bool RemoveEmptyEntries()
    {
        if (emptyBuffIds.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < emptyBuffIds.Count; i++)
        {
            buffStacksById.Remove(emptyBuffIds[i]);
        }

        emptyBuffIds.Clear();
        return true;
    }

    private bool InstallStack(string buffId, BuffDataSO buffData, List<BuffRuntimeHandle> stacks,
        BuffDurationPolicy durationPolicy, float durationSeconds)
    {
        string runtimeSourceId = $"{BUFF_RUNTIME_SOURCE_PREFIX}_{buffId}_{Guid.NewGuid():N}";
        if (!featureHost.InstallFeature(runtimeSourceId, buffData.SpecialFeatures))
        {
            return false;
        }

        stacks.Add(new BuffRuntimeHandle(runtimeSourceId, buffData, durationPolicy, durationSeconds));
        NotifyStackAwareEffects(stacks);
        return true;
    }

    private bool HandleOverflow(BuffDataSO buffData, List<BuffRuntimeHandle> stacks, BuffDurationPolicy durationPolicy,
        float durationSeconds)
    {
        switch (buffData.OverflowMode)
        {
            case BuffOverflowMode.RejectNewStack:
            case BuffOverflowMode.RefreshDurationOnly:
                return RefreshStacks(buffData, stacks, durationSeconds);
            case BuffOverflowMode.ReplaceOldestStack:
                if (stacks.Count == 0)
                {
                    return false;
                }

                RemoveStackAt(buffData.BuffId, stacks, 0, false);
                return InstallStack(buffData.BuffId, buffData, stacks, durationPolicy, durationSeconds);
            default:
                return false;
        }
    }

    private static bool RefreshStacks(BuffDataSO buffData, List<BuffRuntimeHandle> stacks, float durationSeconds)
    {
        return buffData.RefreshMode switch
        {
            BuffRefreshMode.RefreshNewestStack => RefreshNewestStack(stacks, durationSeconds),
            BuffRefreshMode.RefreshAllStacks => RefreshAllStacks(stacks, durationSeconds),
            _ => false
        };
    }

    private static bool RefreshNewestStack(List<BuffRuntimeHandle> stacks, float durationSeconds)
    {
        return stacks != null && stacks.Count > 0 && stacks[stacks.Count - 1].RefreshDuration(durationSeconds);
    }

    private static bool RefreshAllStacks(List<BuffRuntimeHandle> stacks, float durationSeconds)
    {
        bool changed = false;
        for (int i = 0; i < stacks.Count; i++)
        {
            changed |= stacks[i].RefreshDuration(durationSeconds);
        }

        return changed;
    }

    private void RemoveStackAt(string buffId, List<BuffRuntimeHandle> stacks, int stackIndex, bool expired)
    {
        BuffRuntimeHandle handle = stacks[stackIndex];
        BuffDataSO buffData = handle.BuffData;
        int previousStackCount = stacks.Count;

        featureHost.RemoveFeature(handle.RuntimeSourceId);
        stacks.RemoveAt(stackIndex);

        if (stacks.Count > 0)
        {
            NotifyStackAwareEffects(stacks);
        }
        else
        {
            buffStacksById.Remove(buffId);
        }

        PublishStackRemovedEvent(buffData, expired, stacks.Count);
        NotifyStackStateChanged(buffData, previousStackCount, stacks.Count);
    }

    private bool RemoveAllStacks(string buffId, List<BuffRuntimeHandle> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            buffStacksById.Remove(buffId);
            return false;
        }

        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            RemoveStackAt(buffId, stacks, i, false);
        }

        return true;
    }

    private void NotifyStackAwareEffects(List<BuffRuntimeHandle> stacks)
    {
        if (featureHost == null || stacks == null || stacks.Count == 0)
        {
            return;
        }

        int currentStackCount = stacks.Count;
        int maxStackCount = stacks[0].BuffData != null ? stacks[0].BuffData.MaxStackCount : currentStackCount;

        for (int i = 0; i < stacks.Count; i++)
        {
            FeatureHostSourceHandle sourceHandle = featureHost.GetInstalledSourceHandle(stacks[i].RuntimeSourceId);
            if (sourceHandle == null)
            {
                continue;
            }

            List<FeatureBase> effects = sourceHandle.RuntimeEffects;
            for (int j = 0; j < effects.Count; j++)
            {
                if (effects[j] is IBuffStackAwareFeatureEffect stackAwareEffect)
                {
                    stackAwareEffect.OnBuffStackChanged(featureHost.Context, currentStackCount, maxStackCount);
                }
            }
        }
    }

    private void NotifyStackStateChanged(BuffDataSO buffData, int previousStackCount, int currentStackCount)
    {
        if (owner == null || buffData == null || previousStackCount == currentStackCount)
        {
            return;
        }

        GameEventBus.Publish(new BuffStackChangedEvent(owner, buffData, previousStackCount, currentStackCount));
    }

    private void PublishStackRemovedEvent(BuffDataSO buffData, bool expired, int remainingStackCount)
    {
        if (owner == null || buffData == null)
        {
            return;
        }

        if (expired)
        {
            GameEventBus.Publish(new BuffStackExpiredEvent(owner, buffData, SINGLE_STACK_CHANGE_COUNT,
                remainingStackCount));
            return;
        }

        GameEventBus.Publish(new BuffStackRemovedEvent(owner, buffData, SINGLE_STACK_CHANGE_COUNT,
            remainingStackCount));
    }

    private static ActiveBuffViewData BuildMergedViewData(BuffDataSO buffData, List<BuffRuntimeHandle> stacks)
    {
        float remainingDurationSeconds = 0f;
        float totalDurationSeconds = 0f;
        bool hasDuration = false;

        for (int i = 0; i < stacks.Count; i++)
        {
            BuffRuntimeHandle handle = stacks[i];
            if (!handle.HasDuration)
            {
                continue;
            }

            hasDuration = true;
            if (handle.RemainingDurationSeconds > remainingDurationSeconds)
            {
                remainingDurationSeconds = handle.RemainingDurationSeconds;
                totalDurationSeconds = handle.TotalDurationSeconds;
            }
        }

        return BuffRuntimeHandle.CreateMergedViewData(buffData, stacks.Count,
            buffData != null ? buffData.MaxStackCount : 0, hasDuration ? remainingDurationSeconds : 0f,
            hasDuration ? totalDurationSeconds : 0f);
    }

    private static void ResolveDuration(BuffApplyRequest applyRequest, BuffDataSO buffData,
        out BuffDurationPolicy durationPolicy, out float durationSeconds)
    {
        if (applyRequest.OverrideDuration)
        {
            durationPolicy = applyRequest.DurationPolicy;
            durationSeconds = applyRequest.DurationSeconds;
            return;
        }

        durationPolicy = buffData.DurationPolicy;
        durationSeconds = buffData.DurationSeconds;
    }

    private bool TryGetStacks(string buffId, out List<BuffRuntimeHandle> stacks)
    {
        stacks = null;
        return !string.IsNullOrWhiteSpace(buffId) && buffStacksById.TryGetValue(buffId, out stacks);
    }

    private void OnApplyBuffRequested(ApplyBuffRequestedEvent eventData)
    {
        ApplyBuff(eventData.Request);
    }

    private void OnRemoveBuffRequested(RemoveBuffRequestedEvent eventData)
    {
        RemoveBuff(eventData.BuffId);
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        if (waveStartBuffRequests.Count == 0)
        {
            return;
        }

        for (int i = 0; i < waveStartBuffRequests.Count; i++)
        {
            ApplyBuff(waveStartBuffRequests[i]);
        }
    }

    private void PublishViewData()
    {
        ActiveBuffViewData[] viewData = BuildActiveBuffViewData();
        OnActiveBuffViewDataChanged?.Invoke(viewData);
    }
}
