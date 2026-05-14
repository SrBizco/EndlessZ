using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Navigation
{
    public sealed class ObstacleNavMeshManager : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private bool includeInactive = false;

        [Header("Obstacle Defaults")]
        [SerializeField] private bool fitToCollider = true;
        [SerializeField, Min(0.01f)] private Vector3 fallbackSize = new Vector3(1f, 2f, 1f);
        [SerializeField] private Vector3 centerOffset = Vector3.zero;

        [Header("Carving")]
        [SerializeField] private bool carve = true;
        [SerializeField] private bool carveOnlyStationary = true;
        [SerializeField, Min(0f)] private float carvingMoveThreshold = 0.1f;
        [SerializeField, Min(0f)] private float carvingTimeToStationary = 0.5f;

        private readonly HashSet<GameObject> configuredObjects = new HashSet<GameObject>();

        private void Start()
        {
            StartCoroutine(ConfigureObstaclesAfterSceneStart());
        }

        private IEnumerator ConfigureObstaclesAfterSceneStart()
        {
            yield return null;
            ConfigureSceneObstacles();
        }

        public void ConfigureSceneObstacles()
        {
            configuredObjects.Clear();

            Collider[] colliders = FindObjectsByType<Collider>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider obstacleCollider = colliders[i];
                if (obstacleCollider == null || !IsObstacleLayer(obstacleCollider.gameObject.layer))
                {
                    continue;
                }

                ConfigureObstacle(obstacleCollider);
            }
        }

        private bool IsObstacleLayer(int objectLayer)
        {
            return (obstacleLayers.value & (1 << objectLayer)) != 0;
        }

        private void ConfigureObstacle(Collider obstacleCollider)
        {
            GameObject obstacleObject = obstacleCollider.gameObject;
            if (!configuredObjects.Add(obstacleObject))
            {
                return;
            }

            NavMeshObstacle obstacle = obstacleObject.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = obstacleObject.AddComponent<NavMeshObstacle>();
            }

            obstacle.enabled = false;
            obstacle.shape = NavMeshObstacleShape.Box;

            Bounds bounds = fitToCollider ? GetColliderLocalBounds(obstacleCollider) : new Bounds(Vector3.zero, fallbackSize);
            obstacle.center = bounds.center + centerOffset;
            obstacle.size = bounds.size;
            obstacle.carving = carve;
            obstacle.carveOnlyStationary = carveOnlyStationary;
            obstacle.carvingMoveThreshold = carvingMoveThreshold;
            obstacle.carvingTimeToStationary = carvingTimeToStationary;
            obstacle.enabled = true;
        }

        private Bounds GetColliderLocalBounds(Collider obstacleCollider)
        {
            if (obstacleCollider is BoxCollider boxCollider)
            {
                return new Bounds(boxCollider.center, boxCollider.size);
            }

            if (obstacleCollider is CapsuleCollider capsuleCollider)
            {
                float diameter = capsuleCollider.radius * 2f;
                Vector3 size = new Vector3(diameter, capsuleCollider.height, diameter);

                if (capsuleCollider.direction == 0)
                {
                    size = new Vector3(capsuleCollider.height, diameter, diameter);
                }
                else if (capsuleCollider.direction == 2)
                {
                    size = new Vector3(diameter, diameter, capsuleCollider.height);
                }

                return new Bounds(capsuleCollider.center, size);
            }

            return GetWorldBoundsAsLocalBounds(obstacleCollider);
        }

        private static Bounds GetWorldBoundsAsLocalBounds(Collider obstacleCollider)
        {
            Bounds worldBounds = obstacleCollider.bounds;
            Transform obstacleTransform = obstacleCollider.transform;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] worldCorners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            Bounds localBounds = new Bounds(obstacleTransform.InverseTransformPoint(worldCorners[0]), Vector3.zero);
            for (int i = 1; i < worldCorners.Length; i++)
            {
                localBounds.Encapsulate(obstacleTransform.InverseTransformPoint(worldCorners[i]));
            }

            return localBounds;
        }
    }
}
