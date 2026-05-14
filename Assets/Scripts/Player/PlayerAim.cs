using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessZ.Player
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera aimCamera;
        [SerializeField, Min(0f)] private float rotationSharpness = 18f;

        private readonly Plane aimPlane = new Plane(Vector3.up, Vector3.zero);

        public bool HasAimPoint { get; private set; }
        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPosition))
            {
                HasAimPoint = false;
                return;
            }

            AimPoint = mouseWorldPosition;
            HasAimPoint = true;

            Quaternion targetRotation = PlayerAimMath.CalculateAimRotation(transform.position, mouseWorldPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * Time.deltaTime);
        }

        public bool TryGetAimDirectionFrom(Vector3 origin, out Vector3 direction)
        {
            direction = default;

            if (!HasAimPoint)
            {
                return false;
            }

            direction = AimPoint - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = default;

            if (aimCamera == null || Mouse.current == null)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray ray = aimCamera.ScreenPointToRay(screenPosition);

            if (!aimPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }
    }
}
