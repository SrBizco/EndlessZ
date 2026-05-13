using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class MeleeAttackBehaviour : StateMachineBehaviour
    {
        private MeleeEnemyController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = animator.GetComponentInParent<MeleeEnemyController>();
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
