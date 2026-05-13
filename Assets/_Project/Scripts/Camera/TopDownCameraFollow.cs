using UnityEngine;

namespace EndlessZ.Cameras
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -10f);
        [SerializeField, Min(0f)] private float followSharpness = 12f;
        [SerializeField, Min(1f)] private float orthographicSize = 8f;

        private UnityEngine.Camera followCamera;

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
            followCamera.orthographic = true;
            followCamera.orthographicSize = orthographicSize;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 targetPosition = CameraFollowMath.CalculateTargetPosition(followTarget.position, offset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSharpness * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation((followTarget.position - transform.position).normalized, Vector3.up);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }
    }
}
