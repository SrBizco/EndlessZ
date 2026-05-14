using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyAttackBehaviour : EnemyStateBehaviour
    {
        private IEnemyStateController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = GetStateController(animator);
            controller?.EnterAttack();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdateAttack();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.ExitAttack();
        }
    }
}
