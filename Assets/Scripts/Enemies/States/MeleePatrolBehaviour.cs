using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class MeleePatrolBehaviour : StateMachineBehaviour
    {
        private MeleeEnemyController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = animator.GetComponentInParent<MeleeEnemyController>();
            controller?.EnterPatrol();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdatePatrol();
        }
    }
}
