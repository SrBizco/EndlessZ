using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessZ.Player
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera aimCamera;
        [SerializeField, Min(0f)] private float rotationSharpness = 18f;

        private readonly Plane aimPlane = new Plane(Vector3.up, Vector3.zero);

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
                return;
            }

            Quaternion targetRotation = PlayerAimMath.CalculateAimRotation(transform.position, mouseWorldPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * Time.deltaTime);
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
