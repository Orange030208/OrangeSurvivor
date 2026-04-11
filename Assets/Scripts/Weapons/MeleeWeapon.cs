using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [SerializeField] private Transform hitDetectionTransform;
    [SerializeField] private BoxCollider2D hitCollider;
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private readonly Dictionary<int, HashSet<HealthComponent>> hitWindowTargets = new();
    private readonly HashSet<int> activeHitWindows = new();
    private MeleeWeaponAttackExecutor attackExecutor;
    private Enemy pendingTarget;

    protected override void Awake()
    {
        base.Awake();
        attackExecutor = new MeleeWeaponAttackExecutor(hitDetectionTransform, hitCollider, enemyLayerMask);

        if (sequenceBridge == null)
        {
            sequenceBridge = GetComponentInChildren<WeaponSequenceBridge>();
        }

        if (attackSequence == null && WeaponData != null)
        {
            attackSequence = WeaponData.AttackSequence;
        }

        if (sequenceBridge != null)
        {
            sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
            sequenceBridge.SequenceCompleted += FinishAttackSequence;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ForceResetAttackState();
    }

    private void OnDestroy()
    {
        if (sequenceBridge == null)
        {
            return;
        }

        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
    }

    protected override bool CanStartAttack()
    {
        return !IsAttacking && (sequenceBridge == null || !sequenceBridge.IsPlaying);
    }

    protected override void TickWeapon(float deltaTime)
    {
        base.TickWeapon(deltaTime);

        if (activeHitWindows.Count == 0)
        {
            return;
        }

        foreach (int windowId in activeHitWindows)
        {
            if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
            {
                continue;
            }

            attackExecutor.ExecuteAttack(BuildAttackContext(pendingTarget, hitDetectionTransform), hitTargets);
        }
    }

    protected override void BeginAttack(Enemy target)
    {
        IsAttacking = true;
        pendingTarget = target;
        activeHitWindows.Clear();
        hitWindowTargets.Clear();

        if (sequenceBridge != null && attackSequence != null)
        {
            sequenceBridge.Play(attackSequence);
            return;
        }

        OpenHitWindow(0);
        CloseHitWindow(0);
        FinishAttackSequence();
    }

    public void OpenHitWindow(int windowId)
    {
        activeHitWindows.Add(windowId);
        if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
        {
            hitTargets = new HashSet<HealthComponent>();
            hitWindowTargets[windowId] = hitTargets;
        }
        else
        {
            hitTargets.Clear();
        }
    }

    public void CloseHitWindow(int windowId)
    {
        activeHitWindows.Remove(windowId);
    }

    public void FinishAttackSequence()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        pendingTarget = null;
        CompleteAttackCycle();
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventContext eventContext)
    {
        switch (eventContext.EventType)
        {
            case WeaponSequenceEventType.OpenHitWindow:
                OpenHitWindow(eventContext.WindowId);
                break;
            case WeaponSequenceEventType.CloseHitWindow:
                CloseHitWindow(eventContext.WindowId);
                break;
            case WeaponSequenceEventType.PlaySfx:
                break;
            case WeaponSequenceEventType.PlayVfx:
                break;
        }
    }

    private void ForceResetAttackState()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        pendingTarget = null;
        CompleteAttackCycle();
        sequenceBridge?.Stop(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (hitDetectionTransform == null || hitCollider == null)
        {
            return;
        }

        float range = Application.isPlaying ? RuntimeStats.Range : 0.5f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(hitDetectionTransform.position, hitDetectionTransform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitCollider.size);
        Gizmos.matrix = previous;
    }
}
