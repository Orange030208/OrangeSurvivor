using System;
using UnityEngine;

public abstract class AttackStrategyBase : IAttackStrategy
{
    protected readonly Enemy owner;
    protected readonly EnemyAttackController attackController;
    protected readonly AttributeManager AttributeManager;

    protected AttackStrategyBase(
        Enemy owner,
        EnemyAttackController attackController,
        AttributeManager AttributeManager,
        string actionId,
        float attackSpeedBenefitRatio)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.attackController = attackController ?? throw new ArgumentNullException(nameof(attackController));
        this.AttributeManager = AttributeManager ?? throw new ArgumentNullException(nameof(AttributeManager));
        ActionId = string.IsNullOrWhiteSpace(actionId)
            ? throw new ArgumentException("Action id cannot be null or whitespace.", nameof(actionId))
            : actionId;
        this.attackController.RegisterBasicAttackSlot(ActionId, attackSpeedBenefitRatio);
    }

    public string ActionId { get; }

    public bool CanUse(Entity target)
    {
        return target != null &&
               attackController.CanUseBasicAttack(ActionId) &&
               IsTargetInRange(target);
    }

    public bool TryExecute(Entity target)
    {
        if (!CanUse(target))
        {
            return false;
        }

        return TryExecuteAfterEntry(target);
    }

    public virtual bool TryExecuteCommitted(Entity target)
    {
        if (target == null || !attackController.CanUseBasicAttack(ActionId))
        {
            return false;
        }

        return TryExecuteAfterEntry(target);
    }

    public void ResetCooldown()
    {
        attackController.ResetBasicAttackCooldown(ActionId);
    }

    protected float ResolveDamage()
    {
        return PropValueUtility.ClampNonNegative(AttributeManager.GetAttributeValue(PropType.Attack));
    }

    public abstract bool IsTargetInRange(Entity target);

    protected abstract bool ExecuteCore(Entity target);

    private bool TryExecuteAfterEntry(Entity target)
    {
        if (!ExecuteCore(target))
        {
            return false;
        }

        attackController.CommitBasicAttackCooldown(ActionId);
        return true;
    }
}
