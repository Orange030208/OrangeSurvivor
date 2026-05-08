using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public abstract class MechaStoneTaskBase : Action
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Owner GameObject. If empty, the current BehaviorTree GameObject is used.")]
    public SharedGameObject ownerObject;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current target GameObject. If empty, the owner Enemy.TargetEntity is used.")]
    public SharedGameObject targetObject;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Mecha Stone data asset. If empty, the owner Enemy.EnemyData is used.")]
    public SharedObject bossDataObject;

    protected Enemy OwnerEnemy { get; private set; }
    protected GolemMechaStoneBossBrain BossBrain { get; private set; }
    protected Entity TargetEntity { get; private set; }
    protected GolemMechaStoneBossSO BossData { get; private set; }
    protected GolemMechaStoneBossAnimationConfig BossAnimationConfig { get; private set; }
    protected IMovable Movable { get; private set; }
    protected IAnimatable Animatable { get; private set; }
    protected IEntityFacingController FacingController { get; private set; }
    protected PropertiesManager PropertiesManager { get; private set; }
    protected HealthComponent HealthComponent { get; private set; }
    protected EnemyAttackController AttackController { get; private set; }

    protected bool HasContext => OwnerEnemy != null && BossBrain != null && BossData != null && BossAnimationConfig != null;
    protected bool HasTarget => TargetEntity != null;
    protected EnemyActionRunner ActionRunner { get; } = new();

    private bool actionLocked;

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
        ReleaseActionLock();
        ownerObject = null;
        targetObject = null;
        bossDataObject = null;
    }

    protected void AcquireActionLock()
    {
        if (actionLocked)
        {
            return;
        }

        BossBrain?.BeginAction();
        actionLocked = true;
    }

    protected void BeginBossAction(EnemyActionDefinition actionDefinition)
    {
        AcquireActionLock();
        if (actionDefinition != null && Animatable != null)
        {
            ActionRunner.Begin(actionDefinition, Animatable);
        }
    }

    protected void TickBossAction(float deltaTime)
    {
        ActionRunner.Tick(deltaTime);
    }

    protected void ReleaseActionLock()
    {
        if (!actionLocked)
        {
            return;
        }

        BossBrain?.EndAction();
        actionLocked = false;
        ActionRunner.Cancel();
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

        BossAnimationConfig = BossData != null
            ? BossData.AnimConfig as GolemMechaStoneBossAnimationConfig
            : null;

        TargetEntity = ResolveTargetEntity();
        Movable = OwnerEnemy.MoveComponent;
        Animatable = OwnerEnemy.AnimComponent;
        FacingController = ownerGameObject.GetComponent<IEntityFacingController>();
        PropertiesManager = OwnerEnemy.PropertiesManager;
        HealthComponent = OwnerEnemy.HealthComponent;
        AttackController = ownerGameObject.GetComponent<EnemyAttackController>();
        return HasContext;
    }

    protected void FaceTarget()
    {
        FacingController?.FaceTarget(TargetEntity);
    }

    protected void StopMoving()
    {
        Movable?.StopMoving();
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
