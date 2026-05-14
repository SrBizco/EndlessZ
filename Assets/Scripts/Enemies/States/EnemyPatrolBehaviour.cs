using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyPatrolBehaviour : EnemyStateBehaviour
    {
        private IEnemyStateController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = GetStateController(animator);
            controller?.EnterPatrol();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdatePatrol();
        }
    }
}
