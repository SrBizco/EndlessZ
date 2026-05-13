using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class MeleeIdleBehaviour : StateMachineBehaviour
    {
        private MeleeEnemyController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = animator.GetComponentInParent<MeleeEnemyController>();
            controller?.EnterIdle();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdateIdle();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.ExitIdle();
        }
    }
}
