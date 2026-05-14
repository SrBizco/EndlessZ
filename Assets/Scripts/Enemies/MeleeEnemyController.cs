using EndlessZ.Combat;
using EndlessZ.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public sealed class MeleeEnemyController : MonoBehaviour, IEnemyStateController
    {
        private const int MaxTargetHits = 16;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform[] patrolPoints = new Transform[0];

        [Header("Detection")]
        [SerializeField] private LayerMask targetLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float detectionRange = 12f;
        [SerializeField, Min(0f)] private float attackRange = 1.6f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField, Min(0f)] private float patrolPointTolerance = 0.35f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1f;

        [Header("Animator States")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string patrolStateName = "Patrol";
        [SerializeField] private string chaseStateName = "Chase";
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField] private string deathStateName = "Death";
        [SerializeField] private string movementBoolParameter = "";
        [SerializeField, Min(0f)] private float stateTransitionDuration = 0.1f;

        private NavMeshAgent agent;
        private Animator animator;
        private int patrolIndex;
        private float nextAttackTime;
        private EnemyState currentState;
        private readonly Collider[] targetHits = new Collider[MaxTargetHits];
        private int movementBoolHash;
        private bool hasMovementBoolParameter;
        private EnemyVariantProfile variantProfile;
        private MovementSpeedModifierTarget speedModifierTarget;
        private float speedMultiplier = 1f;

        public Transform Target => target;
        public bool HasTarget => target != null && IsTargetAlive(target);
        public bool TargetInDetectionRange => AcquireTarget() && SqrDistanceToTarget() <= detectionRange * detectionRange;
        public bool TargetInAttackRange => HasTarget && SqrDistanceToTarget() <= attackRange * attackRange;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            speedModifierTarget = GetComponent<MovementSpeedModifierTarget>();
            if (speedModifierTarget == null)
            {
                speedModifierTarget = gameObject.AddComponent<MovementSpeedModifierTarget>();
            }

            agent.updateRotation = false;
            CacheAnimatorParameters();
        }

        private void Start()
        {
            RefreshVariantProfile();
        }

        private void Update()
        {
            AcquireTarget();
            ApplyCurrentMovementSpeed();
            UpdateAnimatorMovement();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ChangeState(EnemyState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            animator.CrossFade(GetStateName(state), stateTransitionDuration);
        }

        public void MarkDead()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            ChangeState(EnemyState.Death);
        }

        public void EnterIdle()
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        public void UpdateIdle()
        {
            if (IsDead)
            {
                return;
            }

            if (TargetInDetectionRange)
            {
                ChangeState(EnemyState.Chase);
            }
        }

        public void ExitIdle()
        {
            if (!IsDead)
            {
                agent.isStopped = false;
            }
        }

        public void EnterPatrol()
        {
            if (IsDead)
            {
                return;
            }

            agent.isStopped = false;
            agent.speed = GetModifiedSpeed(patrolSpeed);
            SetNextPatrolDestination();
        }

        public void UpdatePatrol()
        {
            if (IsDead)
            {
                return;
            }

            if (TargetInDetectionRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            if (patrolPoints.Length == 0)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                SetNextPatrolDestination();
            }
        }

        public void EnterChase()
        {
            if (IsDead)
            {
                return;
            }

            agent.isStopped = false;
            agent.speed = GetModifiedSpeed(chaseSpeed);
            AcquireTarget();
        }

        public void UpdateChase()
        {
            if (IsDead)
            {
                return;
            }

            if (!TargetInDetectionRange)
            {
                ChangeState(patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);
                return;
            }

            if (TargetInAttackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (!AcquireTarget())
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            agent.SetDestination(target.position);
            FaceTarget();
        }

        public void EnterAttack()
        {
            if (IsDead)
            {
                return;
            }

            agent.ResetPath();
            agent.isStopped = true;
            AcquireTarget();
        }

        public void UpdateAttack()
        {
            if (IsDead)
            {
                return;
            }

            if (!TargetInDetectionRange)
            {
                ChangeState(patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);
                return;
            }

            if (!TargetInAttackRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            FaceTarget();
            TryAttack();
        }

        public void ExitAttack()
        {
            if (!IsDead)
            {
                agent.isStopped = false;
            }
        }

        public void EnterRetreat()
        {
            if (!IsDead)
            {
                ChangeState(EnemyState.Chase);
            }
        }

        public void UpdateRetreat()
        {
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            if (!HasTarget)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage, gameObject);
            }
        }

        private bool AcquireTarget()
        {
            if (HasTarget && SqrDistanceToTarget() <= detectionRange * detectionRange)
            {
                return true;
            }

            target = null;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRange,
                targetHits,
                targetLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.PositiveInfinity;
            Transform closestTarget = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = targetHits[i];
                if (hit == null)
                {
                    continue;
                }

                Transform candidate = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
                if (!IsTargetAlive(candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.position - transform.position).sqrMagnitude;
                if (sqrDistance >= closestDistance)
                {
                    continue;
                }

                closestDistance = sqrDistance;
                closestTarget = candidate;
            }

            target = closestTarget;
            return target != null;
        }

        private static bool IsTargetAlive(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            IDamageable damageable = candidate.GetComponentInParent<IDamageable>();
            return damageable == null || damageable.IsAlive;
        }

        private void SetNextPatrolDestination()
        {
            if (patrolPoints.Length == 0 || patrolPoints[patrolIndex] == null)
            {
                agent.ResetPath();
                return;
            }

            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        private void FaceTarget()
        {
            if (!HasTarget)
            {
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private float SqrDistanceToTarget()
        {
            return (target.position - transform.position).sqrMagnitude;
        }

        private string GetStateName(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Patrol:
                    return patrolStateName;
                case EnemyState.Idle:
                    return idleStateName;
                case EnemyState.Chase:
                    return chaseStateName;
                case EnemyState.Attack:
                    return attackStateName;
                case EnemyState.Death:
                    return deathStateName;
                default:
                    return idleStateName;
            }
        }

        private void UpdateAnimatorMovement()
        {
            if (animator == null || agent == null || !hasMovementBoolParameter)
            {
                return;
            }

            bool isMoving = agent.hasPath && agent.velocity.sqrMagnitude > 0.01f && !agent.isStopped;
            animator.SetBool(movementBoolHash, isMoving);
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null || string.IsNullOrWhiteSpace(movementBoolParameter))
            {
                hasMovementBoolParameter = false;
                return;
            }

            movementBoolHash = Animator.StringToHash(movementBoolParameter);
            hasMovementBoolParameter = false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == movementBoolHash)
                {
                    hasMovementBoolParameter = true;
                    break;
                }
            }
        }

        private void RefreshVariantProfile()
        {
            variantProfile = GetComponent<EnemyVariantProfile>();
            speedMultiplier = variantProfile != null ? variantProfile.MovementSpeedMultiplier : 1f;
            agent.autoTraverseOffMeshLink = variantProfile != null && variantProfile.CanUseNavMeshLinks;
        }

        private void ApplyCurrentMovementSpeed()
        {
            if (IsDead || agent == null || agent.isStopped)
            {
                return;
            }

            switch (currentState)
            {
                case EnemyState.Patrol:
                    agent.speed = GetModifiedSpeed(patrolSpeed);
                    break;
                case EnemyState.Chase:
                    agent.speed = GetModifiedSpeed(chaseSpeed);
                    break;
            }
        }

        private float GetModifiedSpeed(float baseSpeed)
        {
            float zoneMultiplier = speedModifierTarget != null ? speedModifierTarget.CurrentMultiplier : 1f;
            return baseSpeed * speedMultiplier * zoneMultiplier;
        }
    }
}
