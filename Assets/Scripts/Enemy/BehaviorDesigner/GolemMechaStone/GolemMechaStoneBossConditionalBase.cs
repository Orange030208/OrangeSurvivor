using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public abstract class GolemMechaStoneBossConditionalBase : Conditional
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Boss owner GameObject. If empty, the current BehaviorTree GameObject is used.")]
    public SharedGameObject ownerObject;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current target GameObject. If empty, the owner Enemy.TargetEntity is used.")]
    public SharedGameObject targetObject;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Boss data asset. If empty, the owner Enemy.EnemyData is used.")]
    public SharedObject bossDataObject;

    protected Enemy OwnerEnemy { get; private set; }
    protected GolemMechaStoneBossBrain BossBrain { get; private set; }
    protected Entity TargetEntity { get; private set; }
    protected GolemMechaStoneBossSO BossData { get; private set; }
    protected EnemyAttackController AttackController { get; private set; }
    protected HealthComponent HealthComponent { get; private set; }

    protected bool HasContext => OwnerEnemy != null && BossBrain != null && BossData != null;
    protected bool HasTarget => TargetEntity != null;

    public override void OnAwake()
    {
        RefreshContext();
    }

    public override void OnStart()
    {
        RefreshContext();
    }

    public override void OnReset()
    {
        ownerObject = null;
        targetObject = null;
        bossDataObject = null;
    }

    protected bool RefreshContext()
    {
        GameObject configuredOwner = ownerObject != null ? ownerObject.Value : null;
        GameObject ownerGameObject = GetDefaultGameObject(configuredOwner);
        if (ownerGameObject == null)
        {
            return false;
        }

        OwnerEnemy = ownerGameObject.GetComponent<Enemy>();
        if (OwnerEnemy == null)
        {
            return false;
        }

        BossBrain = ownerGameObject.GetComponent<GolemMechaStoneBossBrain>();
        if (BossBrain == null)
        {
            return false;
        }

        BossData = bossDataObject != null ? bossDataObject.Value as GolemMechaStoneBossSO : null;
        if (BossData == null)
        {
            BossData = OwnerEnemy.EnemyData as GolemMechaStoneBossSO;
        }

        TargetEntity = ResolveTargetEntity();
        AttackController = ownerGameObject.GetComponent<EnemyAttackController>();
        HealthComponent = OwnerEnemy.HealthComponent;
        return HasContext;
    }
    protected float HealthRatio()
    {
        if (HealthComponent == null || HealthComponent.MaxHealth <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01(HealthComponent.CurrentHealth / HealthComponent.MaxHealth);
    }

    private Entity ResolveTargetEntity()
    {
        Entity target = null;
        GameObject configuredTarget = targetObject != null ? targetObject.Value : null;
        if (configuredTarget != null)
        {
            target = configuredTarget.GetComponent<Entity>();
        }

        if (target == null)
        {
            target = OwnerEnemy.TargetEntity;
        }

        return target;
    }
}
