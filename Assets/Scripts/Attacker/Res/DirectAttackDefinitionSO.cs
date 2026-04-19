using UnityEngine;

[CreateAssetMenu(fileName = "Direct Attack Definition", menuName = "SO/Attack/Direct Attack Definition", order = 0)]
public class DirectAttackDefinitionSO : AttackDefinitionSO
{
    public override AttackType Type => AttackType.Direct;
}
