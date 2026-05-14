using UnityEngine;

namespace EndlessZ.Navigation
{
    public sealed class MovementCostZoneManager : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask dirtLayers = default;
        [SerializeField] private LayerMask waterLayers = default;
        [SerializeField] private bool includeInactive = false;

        [Header("Speed Multipliers")]
        [SerializeField, Range(0f, 1f)] private float dirtSpeedMultiplier = 0.75f;
        [SerializeField, Range(0f, 1f)] private float waterSpeedMultiplier = 0.45f;

        private void Start()
        {
            ConfigureSceneZones();
        }

        public void ConfigureSceneZones()
        {
            Collider[] colliders = FindObjectsByType<Collider>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider zoneCollider = colliders[i];
                if (zoneCollider == null)
                {
                    continue;
                }

                if (TryGetSpeedMultiplier(zoneCollider.transform, out float speedMultiplier))
                {
                    ConfigureZone(zoneCollider, speedMultiplier);
                }
            }
        }

        private static void ConfigureZone(Collider zoneCollider, float speedMultiplier)
        {
            MovementCostZone zone = zoneCollider.GetComponent<MovementCostZone>();
            if (zone == null)
            {
                zone = zoneCollider.gameObject.AddComponent<MovementCostZone>();
            }

            zone.Configure(speedMultiplier);
            zoneCollider.isTrigger = true;
            EnsureTriggerRigidbody(zoneCollider.gameObject);
        }

        private static void EnsureTriggerRigidbody(GameObject zoneObject)
        {
            Rigidbody body = zoneObject.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = zoneObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        private static bool IsInLayerMask(int objectLayer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << objectLayer)) != 0;
        }

        private bool TryGetSpeedMultiplier(Transform source, out float speedMultiplier)
        {
            Transform current = source;
            while (current != null)
            {
                int layer = current.gameObject.layer;
                if (IsInLayerMask(layer, dirtLayers))
                {
                    speedMultiplier = dirtSpeedMultiplier;
                    return true;
                }

                if (IsInLayerMask(layer, waterLayers))
                {
                    speedMultiplier = waterSpeedMultiplier;
                    return true;
                }

                current = current.parent;
            }

            speedMultiplier = 1f;
            return false;
        }
    }
}
