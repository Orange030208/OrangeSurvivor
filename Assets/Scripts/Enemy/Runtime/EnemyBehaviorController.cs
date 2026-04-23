using System;
using BehaviorDesigner.Runtime;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyBehaviorController : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyComponentRegistry componentRegistry;
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private BehaviorTree behaviorTree;
    [SerializeField] private EnemyBlackboardSyncBase blackboardSync;

    private EnemySO runtimeDefinition;
    private BehaviorSetSO behaviorSet;
    private BtConfigSO btConfig;
    private MovePresetSO activeMovePreset;
    private AttackPresetSO activeAttackPreset;

    public EnemySO RuntimeDefinition => runtimeDefinition;
    public BehaviorSetSO BehaviorSet => behaviorSet;
    public BtConfigSO BtConfig => btConfig;
    public MovePresetSO ActiveMovePreset => activeMovePreset;
    public AttackPresetSO ActiveAttackPreset => activeAttackPreset;
    public BehaviorTree BehaviorTree => behaviorTree;
    public EnemyBlackboardSyncBase BlackboardSync => blackboardSync;
    public MoveBase ActiveMovement => enemy != null ? enemy.CombatController?.ActiveMovement : null;
    public AttackBase ActiveAttack => enemy != null ? enemy.CombatController?.ActiveAttack : null;

    private void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        if (componentRegistry == null)
        {
            componentRegistry = GetComponent<EnemyComponentRegistry>();
        }

        if (healthComponent == null)
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        if (behaviorTree == null)
        {
            behaviorTree = GetComponent<BehaviorTree>();
        }

        if (blackboardSync == null)
        {
            blackboardSync = GetComponent<EnemyBlackboardSyncBase>();
        }

        componentRegistry?.Rebuild();
    }

    public void Configure(EnemySO definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (enemy == null)
        {
            throw new InvalidOperationException($"{nameof(EnemyBehaviorController)} requires {nameof(Enemy)}.");
        }

        if (componentRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(EnemyBehaviorController)} requires {nameof(EnemyComponentRegistry)}.");
        }

        runtimeDefinition = definition;
        behaviorSet = definition.BehaviorSet;
        btConfig = definition.BtConfig;

        componentRegistry.Rebuild();
        ApplyBehaviorSet();
        InitializeBlackboardState();
    }

    public bool HasValidTarget()
    {
        return enemy != null && enemy.TargetEntity != null;
    }

    public float GetDistanceToTarget()
    {
        if (!HasValidTarget())
        {
            return float.PositiveInfinity;
        }

        return Vector2.Distance(enemy.Center, enemy.TargetEntity.Center);
    }

    private bool TrySetActiveMovePreset(MovePresetSO preset)
    {
        if (preset == null || !preset.AvailableAtRuntime)
        {
            return false;
        }

        if (activeMovePreset == preset)
        {
            return true;
        }

        MoveBase move = componentRegistry.GetOrAddMove(preset.GetComponentType());
        preset.ApplyTo(move, runtimeDefinition);
        enemy.SetActiveMovement(move);

        if (preset.StopOnSwitch)
        {
            move.StopImmediately();
        }

        activeMovePreset = preset;
        UpdateCurrentPresetBlackboard();
        RefreshConcurrentCombatPolicy();
        return true;
    }

    private bool TrySetActiveAttackPreset(AttackPresetSO preset)
    {
        if (preset == null || !preset.AvailableAtRuntime)
        {
            return false;
        }

        if (activeAttackPreset == preset)
        {
            return true;
        }

        AttackBase attack = componentRegistry.GetOrAddAttack(preset.GetComponentType());
        preset.ApplyTo(attack, runtimeDefinition);
        enemy.SetActiveAttack(attack);
        enemy.SetAttackRange(preset.MaxRange);
        enemy.SetAttackEnabled(true);
        activeAttackPreset = preset;
        UpdateCurrentPresetBlackboard();
        RefreshConcurrentCombatPolicy();
        return true;
    }

    private bool TryGetActiveChaseMove(out ChaseMove chaseMove)
    {
        chaseMove = ActiveMovement as ChaseMove;
        return chaseMove != null;
    }

    private bool TryGetActiveKeepDistanceMove(out KeepDistanceMove keepDistanceMove)
    {
        keepDistanceMove = ActiveMovement as KeepDistanceMove;
        return keepDistanceMove != null;
    }

    private bool TryGetActiveOrbitMove(out OrbitMove orbitMove)
    {
        orbitMove = ActiveMovement as OrbitMove;
        return orbitMove != null;
    }

    private bool TryGetActiveDirectAttack(out DirectAttack directAttack)
    {
        directAttack = ActiveAttack as DirectAttack;
        return directAttack != null;
    }

    private bool TryGetActiveProjectileAttack(out ProjectileAttack projectileAttack)
    {
        projectileAttack = ActiveAttack as ProjectileAttack;
        return projectileAttack != null;
    }

    public bool RefreshAiFacts(float deltaTime = 0f)
    {
        if (blackboardSync == null)
        {
            return false;
        }

        blackboardSync.Bind(this);
        blackboardSync.RefreshAiFacts(deltaTime);
        return true;
    }

    public bool ApplyDesiredCombatState()
    {
        if (behaviorTree == null)
        {
            return false;
        }

        bool applied = false;

        string desiredMovePresetId = GetBlackboardString(EnemyBlackboardKeys.DesiredMovePresetId, string.Empty);
        if (!string.IsNullOrWhiteSpace(desiredMovePresetId) &&
            behaviorSet != null &&
            behaviorSet.TryGetMovementPreset(desiredMovePresetId, out MovePresetSO movePreset))
        {
            applied |= TrySetActiveMovePreset(movePreset);
        }

        string desiredAttackPresetId = GetBlackboardString(EnemyBlackboardKeys.DesiredAttackPresetId, string.Empty);
        if (!string.IsNullOrWhiteSpace(desiredAttackPresetId) &&
            behaviorSet != null &&
            behaviorSet.TryGetAttackPreset(desiredAttackPresetId, out AttackPresetSO attackPreset))
        {
            applied |= TrySetActiveAttackPreset(attackPreset);
        }

        float desiredChaseStopDistance = GetBlackboardFloat(EnemyBlackboardKeys.DesiredChaseStopDistance, EnemyBlackboardKeys.NoFloatOverride);
        if (desiredChaseStopDistance >= 0f && TryGetActiveChaseMove(out ChaseMove chaseMove))
        {
            chaseMove.SetStopDistance(desiredChaseStopDistance);
            applied = true;
        }

        float desiredKeepDistance = GetBlackboardFloat(EnemyBlackboardKeys.DesiredKeepDistance, EnemyBlackboardKeys.NoFloatOverride);
        if (desiredKeepDistance >= 0f && TryGetActiveKeepDistanceMove(out KeepDistanceMove keepDistanceMove))
        {
            keepDistanceMove.SetDesiredDistance(desiredKeepDistance);
            applied = true;
        }

        float desiredOrbitRadius = GetBlackboardFloat(EnemyBlackboardKeys.DesiredOrbitRadius, EnemyBlackboardKeys.NoFloatOverride);
        if (desiredOrbitRadius >= 0f && TryGetActiveOrbitMove(out OrbitMove orbitMove))
        {
            orbitMove.SetOrbitRadius(desiredOrbitRadius);
            applied = true;
        }

        float desiredDirectAttackFrequency = GetBlackboardFloat(EnemyBlackboardKeys.DesiredDirectAttackFrequency, EnemyBlackboardKeys.NoFloatOverride);
        if (desiredDirectAttackFrequency >= 0f && TryGetActiveDirectAttack(out DirectAttack directAttack))
        {
            directAttack.SetAttackFrequency(desiredDirectAttackFrequency);
            applied = true;
        }

        float desiredProjectileAttackFrequency = GetBlackboardFloat(EnemyBlackboardKeys.DesiredProjectileAttackFrequency, EnemyBlackboardKeys.NoFloatOverride);
        if (desiredProjectileAttackFrequency >= 0f && TryGetActiveProjectileAttack(out ProjectileAttack projectileAttack))
        {
            projectileAttack.SetAttackFrequency(desiredProjectileAttackFrequency);
            applied = true;
        }

        int desiredProjectileFiringMode = GetBlackboardInt(EnemyBlackboardKeys.DesiredProjectileFiringMode, EnemyBlackboardKeys.NoIntOverride);
        if (desiredProjectileFiringMode >= 0 && TryGetActiveProjectileAttack(out ProjectileAttack projectileAttackForMode))
        {
            projectileAttackForMode.SetFiringMode((ProjectileFiringMode)desiredProjectileFiringMode);
            applied = true;
        }

        UpdateCurrentPresetBlackboard();
        ResetDesiredCombatState();
        return applied;
    }

    public bool QueueDesiredMovePreset(MovePresetSO preset)
    {
        if (preset == null)
        {
            return false;
        }

        SetBlackboardString(EnemyBlackboardKeys.DesiredMovePresetId, preset.MoveId);
        return true;
    }

    public bool QueueDesiredAttackPreset(AttackPresetSO preset)
    {
        if (preset == null)
        {
            return false;
        }

        SetBlackboardString(EnemyBlackboardKeys.DesiredAttackPresetId, preset.AttackId);
        return true;
    }

    public void QueueDesiredChaseStopDistance(float value)
    {
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredChaseStopDistance, Mathf.Max(0f, value));
    }

    public void QueueDesiredKeepDistance(float value)
    {
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredKeepDistance, Mathf.Max(0f, value));
    }

    public void QueueDesiredOrbitRadius(float value)
    {
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredOrbitRadius, Mathf.Max(0f, value));
    }

    public void QueueDesiredDirectAttackFrequency(float value)
    {
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredDirectAttackFrequency, Mathf.Max(0f, value));
    }

    public void QueueDesiredProjectileAttackFrequency(float value)
    {
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredProjectileAttackFrequency, Mathf.Max(0f, value));
    }

    public void QueueDesiredProjectileFiringMode(ProjectileFiringMode mode)
    {
        SetBlackboardInt(EnemyBlackboardKeys.DesiredProjectileFiringMode, (int)mode);
    }

    public void SetBlackboardBool(string variableName, bool value)
    {
        SetSharedVariable(variableName, () => new SharedBool { Value = value }, variable =>
        {
            variable.Value = value;
        });
    }

    public void SetBlackboardFloat(string variableName, float value)
    {
        SetSharedVariable(variableName, () => new SharedFloat { Value = value }, variable =>
        {
            variable.Value = value;
        });
    }

    public void SetBlackboardInt(string variableName, int value)
    {
        SetSharedVariable(variableName, () => new SharedInt { Value = value }, variable =>
        {
            variable.Value = value;
        });
    }

    public void SetBlackboardString(string variableName, string value)
    {
        SetSharedVariable(variableName, () => new SharedString { Value = value ?? string.Empty }, variable =>
        {
            variable.Value = value ?? string.Empty;
        });
    }

    public bool TryGetBlackboardBool(string variableName, out bool value)
    {
        if (TryGetVariable<SharedBool>(variableName, out SharedBool variable))
        {
            value = variable.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetBlackboardFloat(string variableName, out float value)
    {
        if (TryGetVariable<SharedFloat>(variableName, out SharedFloat variable))
        {
            value = variable.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetBlackboardInt(string variableName, out int value)
    {
        if (TryGetVariable<SharedInt>(variableName, out SharedInt variable))
        {
            value = variable.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetBlackboardString(string variableName, out string value)
    {
        if (TryGetVariable<SharedString>(variableName, out SharedString variable))
        {
            value = variable.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public float GetHealthRatio()
    {
        if (healthComponent == null || healthComponent.MaxHealth <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Clamp01(healthComponent.CurrentHealth / healthComponent.MaxHealth);
    }

    private void ApplyBehaviorSet()
    {
        if (behaviorSet == null)
        {
            enemy.SetActiveMovement(null);
            enemy.SetActiveAttack(null);
            activeMovePreset = null;
            activeAttackPreset = null;
            RefreshConcurrentCombatPolicy();
            return;
        }

        if (behaviorSet.PreloadAllMovementComponents)
        {
            MovePresetSO[] movementPresets = behaviorSet.MovementPresets;
            for (int i = 0; i < movementPresets.Length; i++)
            {
                MovePresetSO preset = movementPresets[i];
                if (preset == null)
                {
                    continue;
                }

                MoveBase move = componentRegistry.GetOrAddMove(preset.GetComponentType());
                preset.ApplyTo(move, runtimeDefinition);
                move.enabled = false;
                move.DisableMovement();
            }
        }

        if (behaviorSet.PreloadAllAttackComponents)
        {
            AttackPresetSO[] attackPresets = behaviorSet.AttackPresets;
            for (int i = 0; i < attackPresets.Length; i++)
            {
                AttackPresetSO preset = attackPresets[i];
                if (preset == null)
                {
                    continue;
                }

                AttackBase attack = componentRegistry.GetOrAddAttack(preset.GetComponentType());
                preset.ApplyTo(attack, runtimeDefinition);
                attack.enabled = false;
            }
        }

        MovePresetSO defaultMove = behaviorSet.GetDefaultMovementPreset();
        if (defaultMove != null)
        {
            TrySetActiveMovePreset(defaultMove);
        }
        else
        {
            enemy.SetActiveMovement(null);
        }

        AttackPresetSO defaultAttack = behaviorSet.GetDefaultAttackPreset();
        if (defaultAttack != null)
        {
            TrySetActiveAttackPreset(defaultAttack);
        }
        else
        {
            enemy.SetActiveAttack(null);
            enemy.SetAttackEnabled(false);
        }

        RefreshConcurrentCombatPolicy();
    }

    private void InitializeBlackboardState()
    {
        if (behaviorTree == null)
        {
            return;
        }

        if (blackboardSync == null)
        {
            blackboardSync = GetComponent<EnemyBlackboardSyncBase>();
        }

        blackboardSync?.Bind(this);
        SyncConfigToBlackboard();
        UpdateCurrentPresetBlackboard();
        ResetDesiredCombatState();
        blackboardSync?.InitializeBlackboard();
        RefreshAiFacts();
    }

    private void SyncConfigToBlackboard()
    {
        SetBlackboardString(EnemyBlackboardKeys.ConfigDefaultMovePresetId, behaviorSet != null ? behaviorSet.DefaultMovementId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigDefaultAttackPresetId, behaviorSet != null ? behaviorSet.DefaultAttackId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigMeleeMovePresetId, btConfig != null ? btConfig.MeleeMovePresetId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigMeleeAttackPresetId, btConfig != null ? btConfig.MeleeAttackPresetId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigOrbitMovePresetId, btConfig != null ? btConfig.OrbitMovePresetId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigOrbitAttackPresetId, btConfig != null ? btConfig.OrbitAttackPresetId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigRetreatMovePresetId, btConfig != null ? btConfig.RetreatMovePresetId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.ConfigRetreatAttackPresetId, btConfig != null ? btConfig.RetreatAttackPresetId : string.Empty);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigMeleeEnterDistance, btConfig != null ? btConfig.MeleeEnterDistance : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigMeleeExitDistance, btConfig != null ? btConfig.MeleeExitDistance : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigOrbitEnterDistance, btConfig != null ? btConfig.OrbitEnterDistance : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigOrbitExitDistance, btConfig != null ? btConfig.OrbitExitDistance : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigRetreatDesiredDistance, btConfig != null ? btConfig.RetreatDesiredDistance : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigRetreatHealthRatio, btConfig != null ? btConfig.RetreatHealthRatio : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigEnragedHealthRatio, btConfig != null ? btConfig.EnragedHealthRatio : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigStyleSwitchCooldown, btConfig != null ? btConfig.StyleSwitchCooldown : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigBurstWindow, btConfig != null ? btConfig.BurstWindow : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigOrbitDuration, btConfig != null ? btConfig.OrbitDuration : 0f);
        SetBlackboardFloat(EnemyBlackboardKeys.ConfigTargetLostDelay, btConfig != null ? btConfig.TargetLostDelay : 0f);
        SetBlackboardBool(EnemyBlackboardKeys.ConfigRequireLineOfSightForRanged, btConfig != null && btConfig.RequireLineOfSightForRanged);
    }

    private void UpdateCurrentPresetBlackboard()
    {
        SetBlackboardString(EnemyBlackboardKeys.FactCurrentMovePresetId, activeMovePreset != null ? activeMovePreset.MoveId : string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.FactCurrentAttackPresetId, activeAttackPreset != null ? activeAttackPreset.AttackId : string.Empty);
    }

    private void ResetDesiredCombatState()
    {
        SetBlackboardString(EnemyBlackboardKeys.DesiredMovePresetId, string.Empty);
        SetBlackboardString(EnemyBlackboardKeys.DesiredAttackPresetId, string.Empty);
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredChaseStopDistance, EnemyBlackboardKeys.NoFloatOverride);
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredKeepDistance, EnemyBlackboardKeys.NoFloatOverride);
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredOrbitRadius, EnemyBlackboardKeys.NoFloatOverride);
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredDirectAttackFrequency, EnemyBlackboardKeys.NoFloatOverride);
        SetBlackboardFloat(EnemyBlackboardKeys.DesiredProjectileAttackFrequency, EnemyBlackboardKeys.NoFloatOverride);
        SetBlackboardInt(EnemyBlackboardKeys.DesiredProjectileFiringMode, EnemyBlackboardKeys.NoIntOverride);
    }

    private void RefreshConcurrentCombatPolicy()
    {
        if (enemy == null)
        {
            return;
        }

        bool moveAllowsConcurrentAttack = activeMovePreset == null || activeMovePreset.AllowConcurrentAttack;
        bool attackAllowsConcurrentMove = activeAttackPreset == null || !activeAttackPreset.BlocksMovementWhenExecuting;
        enemy.SetAllowMoveWhileAttacking(moveAllowsConcurrentAttack && attackAllowsConcurrentMove);
    }

    private string GetBlackboardString(string variableName, string fallbackValue)
    {
        return TryGetBlackboardString(variableName, out string value) ? value : fallbackValue;
    }

    private float GetBlackboardFloat(string variableName, float fallbackValue)
    {
        return TryGetBlackboardFloat(variableName, out float value) ? value : fallbackValue;
    }

    private int GetBlackboardInt(string variableName, int fallbackValue)
    {
        return TryGetBlackboardInt(variableName, out int value) ? value : fallbackValue;
    }

    private void SetSharedVariable<TVariable>(string variableName, Func<TVariable> factory, Action<TVariable> applyValue)
        where TVariable : SharedVariable
    {
        if (behaviorTree == null || string.IsNullOrWhiteSpace(variableName))
        {
            return;
        }

        if (TryGetVariable(variableName, out TVariable variable))
        {
            applyValue(variable);
            return;
        }

        TVariable createdVariable = factory();
        createdVariable.Name = variableName;
        behaviorTree.SetVariable(variableName, createdVariable);
    }

    private bool TryGetVariable<TVariable>(string variableName, out TVariable variable)
        where TVariable : SharedVariable
    {
        if (behaviorTree == null || string.IsNullOrWhiteSpace(variableName))
        {
            variable = null;
            return false;
        }

        variable = behaviorTree.GetVariable(variableName) as TVariable;
        return variable != null;
    }
}
