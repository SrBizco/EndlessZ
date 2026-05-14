using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyRetreatBehaviour : EnemyStateBehaviour
    {
        private IEnemyStateController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = GetStateController(animator);
            controller?.EnterRetreat();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdateRetreat();
        }
    }
}
