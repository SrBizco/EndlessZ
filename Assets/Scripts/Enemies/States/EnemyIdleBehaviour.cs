using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyIdleBehaviour : EnemyStateBehaviour
    {
        private IEnemyStateController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = GetStateController(animator);
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
