using EndlessZ.Combat;
using UnityEngine;

namespace EndlessZ.Enemies
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyVariantProfile : MonoBehaviour
    {
        [Header("Variant")]
        [SerializeField, Min(0.1f)] private float movementSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float healthMultiplier = 1f;
        [SerializeField] private bool canUseNavMeshLinks = false;

        [Header("Auto Balance")]
        [SerializeField] private bool reduceHealthWhenFaster = true;
        [SerializeField, Min(0.1f)] private float minimumHealthMultiplier = 0.35f;

        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public bool CanUseNavMeshLinks => canUseNavMeshLinks;
        public float HealthMultiplier => reduceHealthWhenFaster
            ? Mathf.Max(minimumHealthMultiplier, healthMultiplier / movementSpeedMultiplier)
            : healthMultiplier;

        public void Configure(float newMovementSpeedMultiplier, float newHealthMultiplier, bool newReduceHealthWhenFaster, float newMinimumHealthMultiplier)
        {
            movementSpeedMultiplier = Mathf.Max(0.1f, newMovementSpeedMultiplier);
            healthMultiplier = Mathf.Max(0.1f, newHealthMultiplier);
            reduceHealthWhenFaster = newReduceHealthWhenFaster;
            minimumHealthMultiplier = Mathf.Max(0.1f, newMinimumHealthMultiplier);
        }

        private void Start()
        {
            Health health = GetComponent<Health>();
            health.SetMaxHealthMultiplier(HealthMultiplier);
        }
    }
}
