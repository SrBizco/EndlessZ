using EndlessZ.Combat;
using UnityEngine;

namespace EndlessZ.Weapons
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 12f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.01f)] private float lifetime = 4f;
        [SerializeField, Min(0f)] private float radius = 0.12f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private GameObject impactVfxPrefab = null;
        [SerializeField, Min(0f)] private float impactVfxLifetime = 2f;

        private GameObject owner;
        private Vector3 direction;
        private float remainingLifetime;
        private bool initialized;

        private void Awake()
        {
            direction = transform.forward;
            remainingLifetime = lifetime;
        }

        private void OnEnable()
        {
            remainingLifetime = lifetime;
        }

        private void Update()
        {
            if (!initialized)
            {
                direction = transform.forward;
                initialized = true;
            }

            float distance = speed * Time.deltaTime;
            Vector3 origin = transform.position;
            Vector3 displacement = direction * distance;

            if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, distance, hitMask, QueryTriggerInteraction.Ignore))
            {
                Hit(hit);
                return;
            }

            transform.position = origin + displacement;
            remainingLifetime -= Time.deltaTime;

            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Vector3 newDirection, float newSpeed, float newDamage, float newLifetime, LayerMask newHitMask, GameObject newOwner)
        {
            direction = newDirection.sqrMagnitude > Mathf.Epsilon ? newDirection.normalized : transform.forward;
            speed = newSpeed;
            damage = newDamage;
            lifetime = newLifetime;
            hitMask = newHitMask;
            owner = newOwner;
            remainingLifetime = lifetime;
            initialized = true;
        }

        private void Hit(RaycastHit hit)
        {
            if (impactVfxPrefab != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                GameObject impact = Instantiate(impactVfxPrefab, hit.point, impactRotation);

                if (impactVfxLifetime > 0f)
                {
                    Destroy(impact, impactVfxLifetime);
                }
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage, owner != null ? owner : gameObject);
            }

            Destroy(gameObject);
        }
    }
}
