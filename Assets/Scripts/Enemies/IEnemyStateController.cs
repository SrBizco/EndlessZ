namespace EndlessZ.Enemies
{
    public interface IEnemyStateController
    {
        void EnterIdle();
        void UpdateIdle();
        void ExitIdle();
        void EnterPatrol();
        void UpdatePatrol();
        void EnterChase();
        void UpdateChase();
        void EnterRetreat();
        void UpdateRetreat();
        void EnterAttack();
        void UpdateAttack();
        void ExitAttack();
    }
}
