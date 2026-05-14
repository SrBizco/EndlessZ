using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public sealed class EnemyDeathBehaviour : StateMachineBehaviour
    {
        private EnemyDeathAnimator deathAnimator;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            deathAnimator = animator.GetComponentInParent<EnemyDeathAnimator>();
            deathAnimator?.EnterDeathState();
        }
    }
}
