using EndlessZ.Combat;
using EndlessZ.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public sealed class RangedEnemyController : MonoBehaviour, IEnemyStateController
    {
        private const int MaxTargetHits = 16;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform[] patrolPoints = new Transform[0];
        [SerializeField] private Transform firePoint = null;
        [SerializeField] private GameObject projectilePrefab = null;

        [Header("Detection")]
        [SerializeField] private LayerMask targetLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float detectionRange = 14f;
        [SerializeField, Min(0f)] private float attackRange = 8f;
        [SerializeField, Min(0f)] private float minimumSafeRange = 3.5f;
        [SerializeField, Min(0f)] private float targetAimHeight = 1f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.8f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3f;
        [SerializeField, Min(0f)] private float retreatSpeed = 3.25f;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField, Min(0f)] private float patrolPointTolerance = 0.35f;
        [SerializeField, Min(0.1f)] private float retreatStepDistance = 4f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1.5f;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;
        [SerializeField] private LayerMask projectileHitMask = ~0;

        [Header("Animator States")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string patrolStateName = "Patrol";
        [SerializeField] private string chaseStateName = "Chase";
        [SerializeField] private string retreatStateName = "Retreat";
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField] private string deathStateName = "Death";
        [SerializeField] private string movementBoolParameter = "";
        [SerializeField] private string shootTriggerParameter = "";
        [SerializeField, Min(0f)] private float stateTransitionDuration = 0.1f;

        private readonly Collider[] targetHits = new Collider[MaxTargetHits];
        private NavMeshAgent agent;
        private Animator animator;
        private int patrolIndex;
        private float nextAttackTime;
        private EnemyState currentState;
        private int movementBoolHash;
        private int shootTriggerHash;
        private bool hasMovementBoolParameter;
        private bool hasShootTriggerParameter;
        private EnemyVariantProfile variantProfile;
        private float speedMultiplier = 1f;

        public Transform Target => target;
        public bool HasTarget => target != null && IsTargetAlive(target);
        public bool TargetInDetectionRange => AcquireTarget() && SqrDistanceToTarget() <= detectionRange * detectionRange;
        public bool TargetInAttackRange => HasTarget && SqrDistanceToTarget() <= attackRange * attackRange;
        public bool TargetTooClose => HasTarget && SqrDistanceToTarget() <= minimumSafeRange * minimumSafeRange;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
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
            agent.speed = patrolSpeed * speedMultiplier;
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
            agent.speed = chaseSpeed * speedMultiplier;
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

            if (TargetTooClose)
            {
                ChangeState(EnemyState.Retreat);
                return;
            }

            if (TargetInAttackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (!AcquireTarget())
            {
                ChangeState(patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);
                return;
            }

            agent.SetDestination(target.position);
            FaceTarget();
        }

        public void EnterRetreat()
        {
            if (IsDead)
            {
                return;
            }

            agent.isStopped = false;
            agent.speed = retreatSpeed * speedMultiplier;
            SetRetreatDestination();
        }

        public void UpdateRetreat()
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

            if (!TargetTooClose)
            {
                ChangeState(TargetInAttackRange ? EnemyState.Attack : EnemyState.Chase);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            {
                SetRetreatDestination();
            }

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

            if (TargetTooClose)
            {
                ChangeState(EnemyState.Retreat);
                return;
            }

            if (!TargetInAttackRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            FaceTarget();
            TryShoot();
        }

        public void ExitAttack()
        {
            if (!IsDead)
            {
                agent.isStopped = false;
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextAttackTime || !HasTarget || projectilePrefab == null)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            TriggerShootAnimation();

            Transform originTransform = firePoint != null ? firePoint : transform;
            Vector3 targetPoint = target.position + Vector3.up * targetAimHeight;
            Vector3 direction = targetPoint - originTransform.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = originTransform.forward;
            }

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            GameObject projectileObject = Instantiate(projectilePrefab, originTransform.position, rotation);
            EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();

            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<EnemyProjectile>();
            }

            projectile.Initialize(direction, projectileSpeed, damage, projectileLifetime, projectileHitMask, gameObject);
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

        private void SetRetreatDestination()
        {
            if (!HasTarget)
            {
                return;
            }

            Vector3 awayFromTarget = transform.position - target.position;
            awayFromTarget.y = 0f;

            if (awayFromTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                awayFromTarget = -transform.forward;
            }

            Vector3 desiredPosition = transform.position + awayFromTarget.normalized * retreatStepDistance;
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, retreatStepDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
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
                case EnemyState.Chase:
                    return chaseStateName;
                case EnemyState.Retreat:
                    return retreatStateName;
                case EnemyState.Attack:
                    return attackStateName;
                case EnemyState.Death:
                    return deathStateName;
                case EnemyState.Idle:
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

        private void TriggerShootAnimation()
        {
            if (hasShootTriggerParameter)
            {
                animator.SetTrigger(shootTriggerHash);
            }
        }

        private void CacheAnimatorParameters()
        {
            hasMovementBoolParameter = false;
            hasShootTriggerParameter = false;

            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(movementBoolParameter))
            {
                movementBoolHash = Animator.StringToHash(movementBoolParameter);
            }

            if (!string.IsNullOrWhiteSpace(shootTriggerParameter))
            {
                shootTriggerHash = Animator.StringToHash(shootTriggerParameter);
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (!string.IsNullOrWhiteSpace(movementBoolParameter)
                    && parameter.type == AnimatorControllerParameterType.Bool
                    && parameter.nameHash == movementBoolHash)
                {
                    hasMovementBoolParameter = true;
                }

                if (!string.IsNullOrWhiteSpace(shootTriggerParameter)
                    && parameter.type == AnimatorControllerParameterType.Trigger
                    && parameter.nameHash == shootTriggerHash)
                {
                    hasShootTriggerParameter = true;
                }
            }
        }

        private void RefreshVariantProfile()
        {
            variantProfile = GetComponent<EnemyVariantProfile>();
            speedMultiplier = variantProfile != null ? variantProfile.MovementSpeedMultiplier : 1f;
            agent.autoTraverseOffMeshLink = variantProfile != null && variantProfile.CanUseNavMeshLinks;
        }
    }
}
