using UnityEngine;

public sealed class MageEnemyBlackboardSync : EnemyBlackboardSyncBase
{
    public override void InitializeBlackboard()
    {
        if (behaviorController == null)
        {
            return;
        }

        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldRetreat, false);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldUseMelee, false);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldOrbit, false);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldKeepDistance, false);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldBurstProjectile, false);
    }

    public override void RefreshAiFacts(float deltaTime)
    {
        WriteCommonCombatFacts();

        if (behaviorController == null)
        {
            return;
        }

        float distanceToTarget = behaviorController.GetDistanceToTarget();
        float healthRatio = behaviorController.GetHealthRatio();
        BtConfigSO config = behaviorController.BtConfig;

        float meleeEnterDistance = config.MeleeEnterDistance;
        float meleeExitDistance = config.MeleeExitDistance;
        float orbitEnterDistance = config.OrbitEnterDistance;
        float orbitExitDistance = config.OrbitExitDistance;
        float retreatHealthRatio = config.RetreatHealthRatio;

        bool hasValidTarget = behaviorController.HasValidTarget();
        bool shouldRetreat = hasValidTarget && healthRatio <= retreatHealthRatio;
        bool shouldUseMelee = hasValidTarget && distanceToTarget <= meleeEnterDistance;
        bool shouldOrbit = hasValidTarget &&
                           distanceToTarget > meleeExitDistance &&
                           distanceToTarget <= orbitEnterDistance &&
                           !shouldRetreat;
        bool shouldKeepDistance = hasValidTarget && distanceToTarget > orbitEnterDistance && !shouldRetreat;
        bool shouldBurstProjectile = hasValidTarget && (shouldRetreat || distanceToTarget > orbitExitDistance);

        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldRetreat, shouldRetreat);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldUseMelee, shouldUseMelee);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldOrbit, shouldOrbit);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldKeepDistance, shouldKeepDistance);
        behaviorController.SetBlackboardBool(EnemyBlackboardKeys.MageShouldBurstProjectile, shouldBurstProjectile);
    }
}
