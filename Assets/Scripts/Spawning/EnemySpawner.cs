using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Spawning
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        private const int MaxTargetHits = 16;

        [Header("Prefabs")]
        [SerializeField] private GameObject enemyPrefab = null;

        [Header("Target")]
        [SerializeField] private LayerMask targetLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float targetSearchRadius = 100f;

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float minSpawnDistance = 10f;
        [SerializeField, Min(0f)] private float maxSpawnDistance = 18f;
        [SerializeField, Min(0f)] private float navMeshSampleRadius = 3f;
        [SerializeField, Min(1)] private int maxSpawnAttempts = 12;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
        [SerializeField, Min(0)] private int maxAliveEnemies = 12;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<GameObject> aliveEnemies = new List<GameObject>();
        private readonly Collider[] targetHits = new Collider[MaxTargetHits];
        private Transform target;
        private float nextSpawnTime;

        private void Start()
        {
            nextSpawnTime = Time.time + (spawnOnStart ? 0f : spawnInterval);
        }

        private void Update()
        {
            RemoveMissingEnemies();

            if (Time.time < nextSpawnTime || aliveEnemies.Count >= maxAliveEnemies)
            {
                return;
            }

            nextSpawnTime = Time.time + spawnInterval;

            if (!AcquireTarget() || enemyPrefab == null)
            {
                return;
            }

            TrySpawnEnemy();
        }

        private bool AcquireTarget()
        {
            if (target != null)
            {
                return true;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                targetSearchRadius,
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

        private void TrySpawnEnemy()
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                float minDistance = Mathf.Min(minSpawnDistance, maxSpawnDistance);
                float maxDistance = Mathf.Max(minSpawnDistance, maxSpawnDistance);
                float distance = Random.Range(minDistance, maxDistance);
                Vector3 candidate = target.position + new Vector3(randomCircle.x, 0f, randomCircle.y) * distance;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                GameObject enemy = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                aliveEnemies.Add(enemy);
                return;
            }
        }

        private void RemoveMissingEnemies()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                }
            }
        }
    }
}
