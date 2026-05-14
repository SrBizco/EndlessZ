using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyChaseBehaviour : EnemyStateBehaviour
    {
        private IEnemyStateController controller;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller = GetStateController(animator);
            controller?.EnterChase();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            controller?.UpdateChase();
        }
    }
}
