public interface IAttackStateProvider
{
    bool CanAttack(in AttackStateContext context);
}
