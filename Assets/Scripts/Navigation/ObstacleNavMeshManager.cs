using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace EndlessZ.Navigation
{
    public sealed class ObstacleNavMeshManager : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask obstacleLayers = default;
        [SerializeField] private LayerMask jumpObstacleLayers = default;
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

        [Header("Jump Links")]
        [SerializeField] private bool generateForwardJumpLink = true;
        [SerializeField] private bool generateSideJumpLink = true;
        [SerializeField, Min(0f)] private float jumpLinkPadding = 0.6f;
        [SerializeField, Min(0f)] private float jumpLinkVerticalOffset = 0f;
        [SerializeField, Min(0.01f)] private float jumpLinkNavMeshSampleRadius = 1.5f;
        [SerializeField] private bool jumpLinksBidirectional = true;
        [SerializeField] private float jumpLinkCostOverride = -1f;

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
                if (obstacleCollider == null || !TryGetConfiguredObstacleObject(obstacleCollider.transform, out GameObject obstacleObject))
                {
                    continue;
                }

                ConfigureObstacle(obstacleObject, obstacleCollider);
            }
        }

        private bool TryGetConfiguredObstacleObject(Transform source, out GameObject obstacleObject)
        {
            Transform current = source;
            while (current != null)
            {
                if (ShouldConfigureObstacle(current.gameObject.layer))
                {
                    obstacleObject = current.gameObject;
                    return true;
                }

                current = current.parent;
            }

            obstacleObject = null;
            return false;
        }

        private bool ShouldConfigureObstacle(int objectLayer)
        {
            return IsInLayerMask(objectLayer, obstacleLayers) || IsJumpObstacleLayer(objectLayer);
        }

        private bool IsJumpObstacleLayer(int objectLayer)
        {
            return IsInLayerMask(objectLayer, jumpObstacleLayers);
        }

        private static bool IsInLayerMask(int objectLayer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << objectLayer)) != 0;
        }

        private void ConfigureObstacle(GameObject obstacleObject, Collider obstacleCollider)
        {
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

            Bounds bounds = fitToCollider
                ? GetColliderLocalBounds(obstacleCollider, obstacleObject.transform)
                : new Bounds(Vector3.zero, fallbackSize);
            obstacle.center = bounds.center + centerOffset;
            obstacle.size = bounds.size;
            obstacle.carving = carve;
            obstacle.carveOnlyStationary = carveOnlyStationary;
            obstacle.carvingMoveThreshold = carvingMoveThreshold;
            obstacle.carvingTimeToStationary = carvingTimeToStationary;
            obstacle.enabled = true;

            if (IsJumpObstacleLayer(obstacleObject.layer))
            {
                ConfigureJumpLinks(obstacleObject, obstacleCollider, bounds);
            }
        }

        private void ConfigureJumpLinks(GameObject obstacleObject, Collider obstacleCollider, Bounds localBounds)
        {
            if (generateForwardJumpLink)
            {
                ConfigureJumpLink(obstacleObject, obstacleCollider, localBounds, "Generated_JumpLink_Forward", Vector3.forward, localBounds.extents.z);
            }

            if (generateSideJumpLink)
            {
                ConfigureJumpLink(obstacleObject, obstacleCollider, localBounds, "Generated_JumpLink_Side", Vector3.right, localBounds.extents.x);
            }
        }

        private void ConfigureJumpLink(GameObject obstacleObject, Collider obstacleCollider, Bounds localBounds, string linkName, Vector3 localAxis, float localExtent)
        {
            Transform obstacleTransform = obstacleObject.transform;
            Transform linkRoot = GetOrCreateChild(obstacleTransform, linkName);
            Transform start = GetOrCreateChild(linkRoot, "Start");
            Transform end = GetOrCreateChild(linkRoot, "End");

            Vector3 localCenter = localBounds.center;
            float halfDistance = Mathf.Max(0.01f, localExtent + jumpLinkPadding);
            Vector3 startWorld = obstacleTransform.TransformPoint(localCenter - localAxis * halfDistance);
            Vector3 endWorld = obstacleTransform.TransformPoint(localCenter + localAxis * halfDistance);
            startWorld.y = obstacleCollider.bounds.min.y + jumpLinkVerticalOffset;
            endWorld.y = obstacleCollider.bounds.min.y + jumpLinkVerticalOffset;

            if (NavMesh.SamplePosition(startWorld, out NavMeshHit startHit, jumpLinkNavMeshSampleRadius, NavMesh.AllAreas))
            {
                startWorld = startHit.position;
            }

            if (NavMesh.SamplePosition(endWorld, out NavMeshHit endHit, jumpLinkNavMeshSampleRadius, NavMesh.AllAreas))
            {
                endWorld = endHit.position;
            }

            start.position = startWorld;
            end.position = endWorld;

            NavMeshLink link = linkRoot.GetComponent<NavMeshLink>();
            if (link == null)
            {
                link = linkRoot.gameObject.AddComponent<NavMeshLink>();
            }

            link.activated = false;
            link.startTransform = start;
            link.endTransform = end;
            link.bidirectional = jumpLinksBidirectional;
            link.costModifier = jumpLinkCostOverride;
            link.activated = true;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private Bounds GetColliderLocalBounds(Collider obstacleCollider, Transform boundsSpace)
        {
            if (boundsSpace == obstacleCollider.transform && obstacleCollider is BoxCollider boxCollider)
            {
                return new Bounds(boxCollider.center, boxCollider.size);
            }

            if (boundsSpace == obstacleCollider.transform && obstacleCollider is CapsuleCollider capsuleCollider)
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

            return GetWorldBoundsAsLocalBounds(obstacleCollider, boundsSpace);
        }

        private static Bounds GetWorldBoundsAsLocalBounds(Collider obstacleCollider, Transform boundsSpace)
        {
            Bounds worldBounds = obstacleCollider.bounds;
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

            Bounds localBounds = new Bounds(boundsSpace.InverseTransformPoint(worldCorners[0]), Vector3.zero);
            for (int i = 1; i < worldCorners.Length; i++)
            {
                localBounds.Encapsulate(boundsSpace.InverseTransformPoint(worldCorners[i]));
            }

            return localBounds;
        }
    }
}
