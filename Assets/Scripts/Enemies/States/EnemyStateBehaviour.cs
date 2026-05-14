using EndlessZ.Enemies;
using UnityEngine;

namespace EndlessZ.Enemies.States
{
    public abstract class EnemyStateBehaviour : StateMachineBehaviour
    {
        protected static IEnemyStateController GetStateController(Animator animator)
        {
            MonoBehaviour[] behaviours = animator.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEnemyStateController controller)
                {
                    return controller;
                }
            }

            return null;
        }
    }
}
