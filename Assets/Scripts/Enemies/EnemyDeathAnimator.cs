using EndlessZ.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Enemies
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyDeathAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator = null;
        [SerializeField] private NavMeshAgent agent = null;
        [SerializeField] private Behaviour[] behavioursToDisable = new Behaviour[0];
        [SerializeField] private Collider[] collidersToDisable = new Collider[0];
        [SerializeField, Min(0f)] private float destroyDelay = 3f;

        private Health health;
        private MeleeEnemyController controller;
        private RangedEnemyController rangedController;
        private bool isDead;
        private bool deathStateEntered;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<MeleeEnemyController>();
            rangedController = GetComponent<RangedEnemyController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
        }

        private void HandleDied(GameObject instigator)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            controller?.MarkDead();
            rangedController?.MarkDead();
        }

        public void EnterDeathState()
        {
            if (deathStateEntered)
            {
                return;
            }

            deathStateEntered = true;

            if (agent != null)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            foreach (Behaviour behaviour in behavioursToDisable)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            foreach (Collider colliderToDisable in collidersToDisable)
            {
                if (colliderToDisable != null)
                {
                    colliderToDisable.enabled = false;
                }
            }

            if (destroyDelay > 0f)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
