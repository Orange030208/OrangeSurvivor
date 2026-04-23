using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class EnemyBlackboardSyncBase : MonoBehaviour
{
    protected EnemyBehaviorController behaviorController;

    public void Bind(EnemyBehaviorController controller)
    {
        behaviorController = controller;
    }

    public virtual void InitializeBlackboard()
    {
    }

    public abstract void RefreshAiFacts(float deltaTime);

    protected void WriteCommonCombatFacts()
    {
        if (behaviorController == null)
        {
            return;
        }

        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.FactHasValidTarget, behaviorController.HasValidTarget());
        behaviorController.SetBlackboardFloat(EnemyBlackboardKeys.FactDistanceToTarget, behaviorController.GetDistanceToTarget());
        behaviorController.SetBlackboardFloat(EnemyBlackboardKeys.FactHealthRatio, behaviorController.GetHealthRatio());
        behaviorController.SetBlackboardString(
            EnemyBlackboardKeys.FactCurrentMovePresetId,
            behaviorController.ActiveMovePreset != null ? behaviorController.ActiveMovePreset.MoveId : string.Empty);
        behaviorController.SetBlackboardString(
            EnemyBlackboardKeys.FactCurrentAttackPresetId,
            behaviorController.ActiveAttackPreset != null ? behaviorController.ActiveAttackPreset.AttackId : string.Empty);
        behaviorController.SetBlackboardFloat(
            EnemyBlackboardKeys.FactAttackRange,
            behaviorController.ActiveAttackPreset != null ? behaviorController.ActiveAttackPreset.MaxRange : 0f);
    }
}
