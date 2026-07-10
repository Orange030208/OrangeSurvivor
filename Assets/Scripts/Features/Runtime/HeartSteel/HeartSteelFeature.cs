using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HeartSteelFeature : FeatureBase
{
    private const int DEFAULT_HIT_BUFFER_SIZE = 64;

    [SerializeField] private string targetWeaponId;
    [SerializeField, Min(0f)] private float requiredDwellSeconds = 2f;
    [SerializeField, Min(0f)] private float lingerSeconds = 0.5f;
    [SerializeField, Min(1)] private int stacksPerTrigger = 1;
    [SerializeField, Min(1)] private int maxStacks = 10;
    [SerializeField, Min(0f)] private float maxHealthPerStack = 1f;
    [SerializeField, Min(0f)] private float cooldownSeconds = 5f;

    private readonly Dictionary<Weapon, WeaponRuntimeState> weaponStates = new();
    private readonly List<Weapon> staleWeapons = new();
    private readonly List<IHeartSteelDwellTuningProvider> dwellTuningProviders = new();
    private readonly List<IHeartSteelStackGainHandler> stackGainHandlers = new();
    private readonly Collider2D[] hitBuffer = new Collider2D[DEFAULT_HIT_BUFFER_SIZE];
    private WeaponsHolder weaponsHolder;
    private AttributeManager AttributeManager;
    private string runtimeSourcePrefix;

    public HeartSteelFeature()
    {
    }

    public HeartSteelFeature(
        string targetWeaponId,
        float requiredDwellSeconds,
        float lingerSeconds,
        int stacksPerTrigger,
        int maxStacks,
        float maxHealthPerStack,
        float cooldownSeconds)
    {
        this.targetWeaponId = targetWeaponId;
        this.requiredDwellSeconds = Mathf.Max(0f, requiredDwellSeconds);
        this.lingerSeconds = Mathf.Max(0f, lingerSeconds);
        this.stacksPerTrigger = Mathf.Max(1, stacksPerTrigger);
        this.maxStacks = Mathf.Max(1, maxStacks);
        this.maxHealthPerStack = Mathf.Max(0f, maxHealthPerStack);
        this.cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
    }

    public override string Title => "心之钢";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        weaponStates.Clear();
        staleWeapons.Clear();
        weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        AttributeManager = Context?.AttributeManager;
        runtimeSourcePrefix = ResolveRuntimeSourcePrefix();

        if (weaponsHolder == null)
        {
            Debug.LogWarning($"[{nameof(HeartSteelFeature)}] Missing {nameof(WeaponsHolder)} on owner.");
            return;
        }

        weaponsHolder.OnWeaponsChanged -= SyncWeaponSubscriptions;
        weaponsHolder.OnWeaponsChanged += SyncWeaponSubscriptions;
        SyncWeaponSubscriptions();
    }

    public override void OnUninstall()
    {
        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged -= SyncWeaponSubscriptions;
        }

        ClearAllWeaponStates();
        dwellTuningProviders.Clear();
        stackGainHandlers.Clear();
        weaponsHolder = null;
        AttributeManager = null;
        runtimeSourcePrefix = null;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f || weaponStates.Count == 0)
        {
            return;
        }

        staleWeapons.Clear();
        foreach (KeyValuePair<Weapon, WeaponRuntimeState> pair in weaponStates)
        {
            Weapon weapon = pair.Key;
            if (!IsWeaponMatch(weapon))
            {
                staleWeapons.Add(weapon);
                continue;
            }

            TickWeaponState(weapon, pair.Value, deltaTime);
        }

        RemoveStaleWeapons();
    }

    private float RequiredDwellSeconds => Mathf.Max(0f, requiredDwellSeconds);
    private float LingerSeconds => Mathf.Max(0f, lingerSeconds);
    private int StacksPerTrigger => Mathf.Max(1, stacksPerTrigger);
    private int MaxStacks => Mathf.Max(1, maxStacks);
    private float MaxHealthPerStack => Mathf.Max(0f, maxHealthPerStack);
    private float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);

    private void SyncWeaponSubscriptions()
    {
        if (weaponsHolder == null)
        {
            return;
        }

        HashSet<Weapon> currentMatches = new();
        IReadOnlyList<Weapon> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            Weapon weapon = equippedWeapons[i];
            if (!IsWeaponMatch(weapon))
            {
                continue;
            }

            currentMatches.Add(weapon);
            EnsureWeaponState(weapon);
        }

        staleWeapons.Clear();
        foreach (Weapon weapon in weaponStates.Keys)
        {
            if (!currentMatches.Contains(weapon))
            {
                staleWeapons.Add(weapon);
            }
        }

        RemoveStaleWeapons();
    }

    private void EnsureWeaponState(Weapon weapon)
    {
        if (weaponStates.ContainsKey(weapon))
        {
            return;
        }

        WeaponRuntimeState state = new(BuildWeaponModifierSourceId(weapon));
        weaponStates.Add(weapon, state);
        weapon.DamageDealt += OnWeaponDamageDealt;
    }

    private void RemoveStaleWeapons()
    {
        for (int i = 0; i < staleWeapons.Count; i++)
        {
            RemoveWeaponState(staleWeapons[i]);
        }

        staleWeapons.Clear();
    }

    private void RemoveWeaponState(Weapon weapon)
    {
        if (!weaponStates.TryGetValue(weapon, out WeaponRuntimeState state))
        {
            return;
        }

        if (weapon != null)
        {
            weapon.DamageDealt -= OnWeaponDamageDealt;
        }

        RemoveMaxHealthModifier(state);
        weaponStates.Remove(weapon);
    }

    private void ClearAllWeaponStates()
    {
        staleWeapons.Clear();
        foreach (Weapon weapon in weaponStates.Keys)
        {
            staleWeapons.Add(weapon);
        }

        RemoveStaleWeapons();
        weaponStates.Clear();
        staleWeapons.Clear();
    }

    private void TickWeaponState(Weapon weapon, WeaponRuntimeState state, float deltaTime)
    {
        state.CooldownRemaining = Mathf.Max(0f, state.CooldownRemaining - deltaTime);
        HeartSteelDwellSettings dwellSettings = ResolveDwellSettings();
        MarkAllTargetsUnseen(state);

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            weapon.transform.position,
            Mathf.Max(0f, weapon.Range),
            hitBuffer,
            weapon.TargetLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Entity target = ResolveTargetEntity(hitBuffer[i]);
            if (target == null || target == Context?.OwnerEntity)
            {
                continue;
            }

            TargetRuntimeState targetState = GetOrCreateTargetState(state, target);
            if (targetState.SeenThisTick)
            {
                continue;
            }

            targetState.SeenThisTick = true;
            targetState.DwellSeconds += deltaTime;
            targetState.LingerRemaining = dwellSettings.LingerSeconds;
            if (targetState.DwellSeconds >= dwellSettings.RequiredDwellSeconds)
            {
                targetState.IsPrimed = true;
            }
        }

        CleanupTargets(state, deltaTime);
    }

    private void OnWeaponDamageDealt(HitResult result)
    {
        if (result.DamageSource is not Weapon weapon ||
            result.Target == null ||
            !weaponStates.TryGetValue(weapon, out WeaponRuntimeState state) ||
            state.CooldownRemaining > 0f)
        {
            return;
        }

        if (!state.Targets.TryGetValue(result.Target, out TargetRuntimeState targetState) || !targetState.IsPrimed)
        {
            return;
        }

        int oldStacks = state.Stacks;
        state.Stacks = Mathf.Min(MaxStacks, state.Stacks + StacksPerTrigger);
        int gainedStacks = Mathf.Max(0, state.Stacks - oldStacks);
        state.CooldownRemaining = CooldownSeconds;
        state.Targets.Remove(result.Target);
        ApplyMaxHealthModifier(state);
        if (gainedStacks > 0)
        {
            NotifyStackGainHandlers(weapon, result, oldStacks, state.Stacks, gainedStacks);
        }
    }

    private void ApplyMaxHealthModifier(WeaponRuntimeState state)
    {
        if (AttributeManager == null || MaxHealthPerStack <= 0f || state.Stacks <= 0)
        {
            return;
        }

        AttributeManager.AddModifier(
            state.ModifierSourceId,
            new PropModifierData(
                PropType.MaxHealth,
                PropModifierType.Add,
                Mathf.RoundToInt(state.Stacks * MaxHealthPerStack)));
    }

    private void RemoveMaxHealthModifier(WeaponRuntimeState state)
    {
        if (AttributeManager == null || state == null)
        {
            return;
        }

        AttributeManager.RemoveModifiers(state.ModifierSourceId);
    }

    private static void MarkAllTargetsUnseen(WeaponRuntimeState state)
    {
        foreach (TargetRuntimeState targetState in state.Targets.Values)
        {
            targetState.SeenThisTick = false;
        }
    }

    private void CleanupTargets(WeaponRuntimeState state, float deltaTime)
    {
        state.TargetsToRemove.Clear();
        foreach (KeyValuePair<Entity, TargetRuntimeState> pair in state.Targets)
        {
            Entity target = pair.Key;
            TargetRuntimeState targetState = pair.Value;
            if (target == null)
            {
                state.TargetsToRemove.Add(target);
                continue;
            }

            if (targetState.SeenThisTick)
            {
                continue;
            }

            targetState.LingerRemaining -= deltaTime;
            if (targetState.LingerRemaining <= 0f)
            {
                state.TargetsToRemove.Add(target);
            }
        }

        for (int i = 0; i < state.TargetsToRemove.Count; i++)
        {
            state.Targets.Remove(state.TargetsToRemove[i]);
        }
    }

    private static TargetRuntimeState GetOrCreateTargetState(WeaponRuntimeState state, Entity target)
    {
        if (!state.Targets.TryGetValue(target, out TargetRuntimeState targetState))
        {
            targetState = new TargetRuntimeState();
            state.Targets.Add(target, targetState);
        }

        return targetState;
    }

    private static Entity ResolveTargetEntity(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        if (collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return healthComponent.GetComponent<Entity>();
        }

        return collider.GetComponent<Entity>();
    }

    private bool IsWeaponMatch(Weapon weapon)
    {
        return weapon != null &&
               weapon.WeaponData != null &&
               !string.IsNullOrWhiteSpace(targetWeaponId) &&
               string.Equals(weapon.WeaponData.WeaponId, targetWeaponId, StringComparison.Ordinal);
    }

    private string ResolveRuntimeSourcePrefix()
    {
        string source = string.IsNullOrWhiteSpace(SourceId) ? nameof(HeartSteelFeature) : SourceId;
        return $"{source}:{nameof(HeartSteelFeature)}:{GetHashCode()}";
    }

    private string BuildWeaponModifierSourceId(Weapon weapon)
    {
        string prefix = string.IsNullOrWhiteSpace(runtimeSourcePrefix)
            ? ResolveRuntimeSourcePrefix()
            : runtimeSourcePrefix;
        int weaponInstanceId = weapon != null ? weapon.GetInstanceID() : 0;
        return $"{prefix}:Weapon:{weaponInstanceId}";
    }

    private void NotifyStackGainHandlers(
        Weapon weapon,
        HitResult triggerHitResult,
        int oldStacks,
        int newStacks,
        int gainedStacks)
    {
        FeatureHost featureHost = Context?.FeatureHost;
        if (featureHost == null)
        {
            return;
        }

        stackGainHandlers.Clear();
        featureHost.CollectRuntimeEffects(stackGainHandlers);
        if (stackGainHandlers.Count == 0)
        {
            return;
        }

        HeartSteelStackGainContext stackGainContext = new(
            Context.OwnerEntity,
            weapon,
            triggerHitResult.Target,
            triggerHitResult,
            oldStacks,
            newStacks,
            gainedStacks,
            ResolveCurrentMaxHealth());

        for (int i = 0; i < stackGainHandlers.Count; i++)
        {
            IHeartSteelStackGainHandler handler = stackGainHandlers[i];
            if (handler != null && handler.AppliesTo(stackGainContext.WeaponId))
            {
                handler.OnHeartSteelStacksGained(stackGainContext);
            }
        }

        stackGainHandlers.Clear();
    }

    private float ResolveCurrentMaxHealth()
    {
        HealthComponent healthComponent = Context?.HealthComponent;
        if (healthComponent != null)
        {
            return healthComponent.MaxHealth;
        }

        return AttributeManager != null
            ? AttributeManager.GetAttributeValue(PropType.MaxHealth)
            : 0f;
    }

    private HeartSteelDwellSettings ResolveDwellSettings()
    {
        HeartSteelDwellSettings settings = new(RequiredDwellSeconds, LingerSeconds);
        FeatureHost featureHost = Context?.FeatureHost;
        if (featureHost == null)
        {
            return settings;
        }

        dwellTuningProviders.Clear();
        featureHost.CollectRuntimeEffects(dwellTuningProviders);
        for (int i = 0; i < dwellTuningProviders.Count; i++)
        {
            IHeartSteelDwellTuningProvider provider = dwellTuningProviders[i];
            if (provider != null && provider.AppliesTo(targetWeaponId))
            {
                settings = provider.Apply(settings);
            }
        }

        return settings;
    }

    private string BuildDescription()
    {
        if (string.IsNullOrWhiteSpace(targetWeaponId))
        {
            return "未配置目标武器。";
        }

        return $"{targetWeaponId} 的敌人在攻击范围内停留 {RequiredDwellSeconds:0.##} 秒后，" +
               $"下一次受到该武器有效伤害时获得 {StacksPerTrigger} 层；" +
               $"每层最大生命 +{MaxHealthPerStack:0.##}，最多 {MaxStacks} 层，冷却 {CooldownSeconds:0.##} 秒。";
    }

    private sealed class WeaponRuntimeState
    {
        public WeaponRuntimeState(string modifierSourceId)
        {
            ModifierSourceId = modifierSourceId;
        }

        public string ModifierSourceId { get; }
        public int Stacks { get; set; }
        public float CooldownRemaining { get; set; }
        public Dictionary<Entity, TargetRuntimeState> Targets { get; } = new();
        public List<Entity> TargetsToRemove { get; } = new();
    }

    private sealed class TargetRuntimeState
    {
        public float DwellSeconds { get; set; }
        public float LingerRemaining { get; set; }
        public bool IsPrimed { get; set; }
        public bool SeenThisTick { get; set; }
    }
}
