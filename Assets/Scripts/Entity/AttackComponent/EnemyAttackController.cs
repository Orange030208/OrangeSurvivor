using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackController : EntityComponentBase, IProjectileLauncher
{
    /// <summary>
    /// 技能槽只记录显式技能冷却，不受 AttackSpeed 影响。
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

    /// <summary>
    /// 普通攻击槽通过 AttackSpeed 与攻击自身收益系数换算攻击间隔。
    /// </summary>
    [Serializable]
    private sealed class BasicAttackSlot
    {
        [SerializeField] private string attackId;
        [SerializeField] private float attackSpeedBenefitRatio = 1f;
        [SerializeField, Min(0f)] private float attackInterval = 1f;

        private float cooldownTimer;

        public BasicAttackSlot(string attackId, float attackSpeedBenefitRatio)
        {
            this.attackId = attackId;
            SetAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        }

        public string AttackId => attackId;
        public bool CanUse => cooldownTimer <= 0f;

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
            }
        }

        public void SetAttackSpeedBenefitRatio(float value)
        {
            attackSpeedBenefitRatio = value;
        }

        public void RefreshInterval(float attackSpeedPoints)
        {
            float oldInterval = attackInterval;
            attackInterval = PropValueUtility.AttackSpeedPointsToAttackInterval(attackSpeedPoints * attackSpeedBenefitRatio);

            if (oldInterval <= 0f)
            {
                cooldownTimer = Mathf.Min(cooldownTimer, attackInterval);
                return;
            }

            float remainingRatio = Mathf.Clamp01(cooldownTimer / oldInterval);
            cooldownTimer = remainingRatio > 0f ? attackInterval * remainingRatio : 0f;
        }

        public void CommitCooldown()
        {
            cooldownTimer = Mathf.Max(0f, attackInterval);
        }

        public void ResetCooldown()
        {
            cooldownTimer = 0f;
        }
    }

    [Header("运行时冷却")]
    [Tooltip("技能冷却槽。通常不需要在预制体上手动填，控制器会按字符串标识自动注册槽。")]
    [SerializeField] private List<SkillSlot> skillSlots = new();
    [Tooltip("普通攻击冷却槽。由攻击策略注册，间隔会跟随攻击速度与攻击收益系数刷新。")]
    [SerializeField] private List<BasicAttackSlot> basicAttackSlots = new();

    private readonly Dictionary<string, SkillSlot> skillSlotLookup = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BasicAttackSlot> basicAttackSlotLookup = new(StringComparer.Ordinal);
    private LayerMask attackLayer;
    private Entity owner;
    private AttributeManager AttributeManager;

    public override Entity Owner => owner;
    public LayerMask AttackLayer
    {
        get => attackLayer;
        set => attackLayer = value;
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        attackLayer = LayerMask.GetMask("Player");
        AttributeManager = owner.GetComponent<AttributeManager>();
        if (AttributeManager == null)
        {
            throw new MissingComponentException($"{nameof(EnemyAttackController)} requires a {nameof(AttributeManager)} on {owner.name}.");
        }

        RebuildLookup();
        RefreshBasicAttackSlots();
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

        for (int i = 0; i < basicAttackSlots.Count; i++)
        {
            basicAttackSlots[i]?.Tick(deltaTime);
        }
    }

    public bool CanUseSkill(string skillId)
    {
        return IsValidSkillId(skillId) && GetOrCreateSkillSlot(skillId).CanUse;
    }

    public void CommitSkillCooldown(string skillId, float cooldown)
    {
        if (IsValidSkillId(skillId))
        {
            GetOrCreateSkillSlot(skillId).CommitCooldown(cooldown);
        }
    }

    public void ResetSkillCooldown(string skillId)
    {
        if (IsValidSkillId(skillId))
        {
            GetOrCreateSkillSlot(skillId).ResetCooldown();
        }
    }

    public void RegisterBasicAttackSlot(string attackId, float attackSpeedBenefitRatio)
    {
        if (!IsValidAttackId(attackId))
        {
            return;
        }

        BasicAttackSlot slot = GetOrCreateBasicAttackSlot(attackId, attackSpeedBenefitRatio);
        slot.SetAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        slot.RefreshInterval(ResolveAttackSpeed());
    }

    public bool CanUseBasicAttack(string attackId)
    {
        return IsValidAttackId(attackId) &&
               TryGetBasicAttackSlot(attackId, out BasicAttackSlot slot) &&
               slot.CanUse;
    }

    public void CommitBasicAttackCooldown(string attackId)
    {
        if (IsValidAttackId(attackId) && TryGetBasicAttackSlot(attackId, out BasicAttackSlot slot))
        {
            slot.CommitCooldown();
        }
    }

    public void ResetBasicAttackCooldown(string attackId)
    {
        if (IsValidAttackId(attackId) && TryGetBasicAttackSlot(attackId, out BasicAttackSlot slot))
        {
            slot.ResetCooldown();
        }
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        projectile.Launch(context);
    }

    private void RebuildLookup()
    {
        skillSlotLookup.Clear();
        basicAttackSlotLookup.Clear();

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

        for (int i = 0; i < basicAttackSlots.Count; i++)
        {
            BasicAttackSlot slot = basicAttackSlots[i];
            if (slot == null || !IsValidAttackId(slot.AttackId))
            {
                continue;
            }

            if (!basicAttackSlotLookup.ContainsKey(slot.AttackId))
            {
                basicAttackSlotLookup.Add(slot.AttackId, slot);
            }
        }
    }

    private SkillSlot GetOrCreateSkillSlot(string skillId)
    {
        if (skillSlotLookup.TryGetValue(skillId, out SkillSlot slot))
        {
            return slot;
        }

        return RegisterSkillSlot(skillId);
    }

    private SkillSlot RegisterSkillSlot(string skillId)
    {
        SkillSlot slot = new SkillSlot(skillId);
        skillSlotLookup.Add(skillId, slot);
        skillSlots.Add(slot);
        return slot;
    }

    private BasicAttackSlot GetOrCreateBasicAttackSlot(string attackId, float attackSpeedBenefitRatio)
    {
        if (TryGetBasicAttackSlot(attackId, out BasicAttackSlot slot))
        {
            return slot;
        }

        slot = new BasicAttackSlot(attackId, attackSpeedBenefitRatio);
        basicAttackSlotLookup.Add(attackId, slot);
        basicAttackSlots.Add(slot);
        return slot;
    }

    private bool TryGetBasicAttackSlot(string attackId, out BasicAttackSlot slot)
    {
        if (basicAttackSlotLookup.TryGetValue(attackId, out slot))
        {
            return true;
        }

        for (int i = 0; i < basicAttackSlots.Count; i++)
        {
            BasicAttackSlot candidate = basicAttackSlots[i];
            if (candidate == null || !string.Equals(candidate.AttackId, attackId, StringComparison.Ordinal))
            {
                continue;
            }

            slot = candidate;
            basicAttackSlotLookup[attackId] = candidate;
            return true;
        }

        return false;
    }

    private static bool IsValidSkillId(string skillId)
    {
        return !string.IsNullOrWhiteSpace(skillId);
    }

    private static bool IsValidAttackId(string attackId)
    {
        return !string.IsNullOrWhiteSpace(attackId);
    }

    private void OnAttackSpeedChanged(int newValue)
    {
        RefreshBasicAttackSlots();
    }

    private void RefreshBasicAttackSlots()
    {
        float attackSpeedPoints = ResolveAttackSpeed();
        for (int i = 0; i < basicAttackSlots.Count; i++)
        {
            basicAttackSlots[i]?.RefreshInterval(attackSpeedPoints);
        }
    }

    private float ResolveAttackSpeed()
    {
        return AttributeManager != null
            ? AttributeManager.GetAttributeValue(PropType.AttackSpeed)
            : PropValueUtility.ATTACK_SPEED_POINTS_PER_ATTACK_PER_SECOND;
    }

    private void BindProperties()
    {
        if (AttributeManager == null)
        {
            return;
        }

        UnbindProperties();
        AttributeManager.SubscribeAttributeChanged(PropType.AttackSpeed, OnAttackSpeedChanged);
    }

    private void UnbindProperties()
    {
        if (AttributeManager == null)
        {
            return;
        }

        AttributeManager.UnsubscribeAttributeChanged(PropType.AttackSpeed, OnAttackSpeedChanged);
    }
}
