using EndlessZ.Combat;
using EndlessZ.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessZ.Weapons
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform firePoint = null;
        [SerializeField] private PlayerAim aimSource = null;
        [SerializeField] private GameObject muzzleVfxPrefab = null;
        [SerializeField] private GameObject hitVfxPrefab = null;

        [Header("Weapon")]
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0.01f)] private float range = 50f;
        [SerializeField, Min(0.01f)] private float fireRate = 4f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float muzzleVfxLifetime = 2f;
        [SerializeField, Min(0f)] private float hitVfxLifetime = 2f;
        [SerializeField] private bool drawDebugRay = false;

        [Header("Animation")]
        [SerializeField] private Animator animator = null;
        [SerializeField] private string shootTriggerParameter = "Shoot";

        private float nextAllowedFireTime;
        private int shootTriggerHash;
        private bool hasShootTriggerParameter;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }

            if (aimSource == null)
            {
                aimSource = GetComponentInParent<PlayerAim>();
            }

            CacheAnimatorParameters();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                return;
            }

            TryFire();
        }

        public bool TryFire()
        {
            if (Time.time < nextAllowedFireTime)
            {
                return false;
            }

            nextAllowedFireTime = Time.time + 1f / fireRate;

            Transform shotOrigin = firePoint != null ? firePoint : transform;
            Vector3 origin = shotOrigin.position;
            Vector3 direction = GetShotDirection(origin, shotOrigin);
            Quaternion shotRotation = Quaternion.LookRotation(direction, Vector3.up);

            TriggerShootAnimation();
            SpawnVfx(muzzleVfxPrefab, shotOrigin.position, shotRotation, muzzleVfxLifetime);
            ApplyHit(origin, direction);

            return true;
        }

        private Vector3 GetShotDirection(Vector3 origin, Transform fallbackDirection)
        {
            if (aimSource != null && aimSource.TryGetAimDirectionFrom(origin, out Vector3 aimDirection))
            {
                return aimDirection;
            }

            Vector3 direction = fallbackDirection.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return transform.forward;
            }

            return direction.normalized;
        }

        private void ApplyHit(Vector3 origin, Vector3 direction)
        {
            if (drawDebugRay)
            {
                Debug.DrawRay(origin, direction * range, Color.red, 0.15f);
            }

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            Quaternion hitRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
            SpawnVfx(hitVfxPrefab, hit.point, hitRotation, hitVfxLifetime);

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            damageable.TakeDamage(damage, gameObject);
        }

        private static void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, position, rotation);

            if (lifetime > 0f)
            {
                Destroy(instance, lifetime);
            }
        }

        private void TriggerShootAnimation()
        {
            if (!hasShootTriggerParameter)
            {
                return;
            }

            animator.SetTrigger(shootTriggerHash);
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null || string.IsNullOrWhiteSpace(shootTriggerParameter))
            {
                hasShootTriggerParameter = false;
                return;
            }

            shootTriggerHash = Animator.StringToHash(shootTriggerParameter);
            hasShootTriggerParameter = false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == shootTriggerHash)
                {
                    hasShootTriggerParameter = true;
                    break;
                }
            }
        }
    }
}
