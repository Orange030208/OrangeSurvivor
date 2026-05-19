using UnityEngine;

[CreateAssetMenu(fileName = "CyberOrangeEnemy", menuName = ScriptableObjectMenuPaths.CYBER_ORANGE_ENEMY, order = 5)]
public sealed class CyberOrangeEnemySO : SkeletonEnemySO
{
    public new const string ATTACK_ACTION_ID = "CyberOrange_BodySlam";

    protected override string DefaultAttackActionId => ATTACK_ACTION_ID;
}
