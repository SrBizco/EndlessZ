using EndlessZ.Movement;
using UnityEngine;

namespace EndlessZ.Navigation
{
    [RequireComponent(typeof(Collider))]
    public sealed class MovementCostZone : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float speedMultiplier = 0.75f;

        private Collider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        public void Configure(float newSpeedMultiplier)
        {
            speedMultiplier = Mathf.Clamp01(newSpeedMultiplier);
        }

        private void OnTriggerEnter(Collider other)
        {
            MovementSpeedModifierTarget target = other.GetComponentInParent<MovementSpeedModifierTarget>();
            target?.AddModifier(this, speedMultiplier);
        }

        private void OnTriggerExit(Collider other)
        {
            MovementSpeedModifierTarget target = other.GetComponentInParent<MovementSpeedModifierTarget>();
            target?.RemoveModifier(this);
        }
    }
}
