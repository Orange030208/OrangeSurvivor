using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackController : EntityComponentBase, IProjectileLauncher
{
    /// <summary>
    /// 只记录运行时动作冷却，不负责执行动作行为。
    /// </summary>
    [Serializable]
    private sealed class SkillSlot
    {
        [SerializeField] private string skillId;

        private float cooldownTimer;

        public SkillSlot(string skillId)
        {
            this.skillId = skillId;
        }

        public string SkillId => skillId;
        public bool CanUse => cooldownTimer <= 0f;

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= deltaTime;
            }
        }

        public void CommitCooldown(float cooldown)
        {
            cooldownTimer = Mathf.Max(0f, cooldown);
        }

        public void ResetCooldown()
        {
            cooldownTimer = 0f;
        }
    }

    [Header("Runtime Cooldowns")]
    [Tooltip("运行时动作冷却槽。通常不需要在 Prefab 上手动填，Controller 会按字符串 ID 自动注册槽。")]
    [SerializeField] private List<SkillSlot> skillSlots = new();

    [Header("Runtime References")]
    [SerializeField] private Transform firePoint;

    private readonly Dictionary<string, SkillSlot> skillSlotLookup = new(StringComparer.Ordinal);
    private LayerMask attackLayer;
    private Entity owner;
    private PropertiesManager propertiesManager;
    private float attackInterval;

    public override Entity Owner => owner;
    public float AttackInterval => attackInterval;
    public LayerMask AttackLayer
    {
        get => attackLayer;
        set => attackLayer = value;
    }

    public Transform FirePoint => firePoint != null ? firePoint : transform;
    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        attackLayer = LayerMask.GetMask("Player");
        propertiesManager = owner.GetComponent<PropertiesManager>();
        if (propertiesManager == null)
        {
            throw new MissingComponentException($"{nameof(EnemyAttackController)} requires a {nameof(PropertiesManager)} on {owner.name}.");
        }

        RebuildLookup();
        RefreshRuntimeStats();
    }

    public override void OnEnableComponent()
    {
        BindProperties();
    }

    public override void OnDisableComponent()
    {
        UnbindProperties();
    }

    public override void OnTick(float deltaTime)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            skillSlots[i]?.Tick(deltaTime);
        }
    }

    public bool CanUseSkill(string skillId)
    {
        return IsValidSkillId(skillId) && GetOrCreateSkillSlot(skillId).CanUse;
    }

    public bool CanUseRuntimeAction(string actionId)
    {
        return CanUseSkill(actionId);
    }

    public void CommitSkillCooldown(string skillId, float cooldown)
    {
        if (IsValidSkillId(skillId))
        {
            GetOrCreateSkillSlot(skillId).CommitCooldown(cooldown);
        }
    }

    public void CommitRuntimeCooldown(string actionId, float cooldown)
    {
        CommitSkillCooldown(actionId, cooldown);
    }

    public void ResetSkillCooldown(string skillId)
    {
        if (IsValidSkillId(skillId))
        {
            GetOrCreateSkillSlot(skillId).ResetCooldown();
        }
    }

    public void ResetRuntimeCooldown(string actionId)
    {
        ResetSkillCooldown(actionId);
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        projectile.Launch(context);
    }

    private void RebuildLookup()
    {
        skillSlotLookup.Clear();

        for (int i = 0; i < skillSlots.Count; i++)
        {
            SkillSlot slot = skillSlots[i];
            if (slot == null || !IsValidSkillId(slot.SkillId))
            {
                continue;
            }

            if (!skillSlotLookup.ContainsKey(slot.SkillId))
            {
                skillSlotLookup.Add(slot.SkillId, slot);
            }
        }
    }

    private SkillSlot GetOrCreateSkillSlot(string skillId)
    {
        if (skillSlotLookup.TryGetValue(skillId, out SkillSlot slot))
        {
            return slot;
        }

        return RegisterRuntimeSkillSlot(skillId);
    }

    private SkillSlot RegisterRuntimeSkillSlot(string skillId)
    {
        SkillSlot slot = new SkillSlot(skillId);
        skillSlotLookup.Add(skillId, slot);
        skillSlots.Add(slot);
        return slot;
    }

    private static bool IsValidSkillId(string skillId)
    {
        return !string.IsNullOrWhiteSpace(skillId);
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.AttackSpeed)
        {
            RefreshRuntimeStats();
        }
    }

    private void OnAllPropertiesChanged()
    {
        RefreshRuntimeStats();
    }

    private void RefreshRuntimeStats()
    {
        float attackSpeed = Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f);
        attackInterval = 1f / attackSpeed;
    }

    private void BindProperties()
    {
        UnbindProperties();
        propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    private void UnbindProperties()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }
}
