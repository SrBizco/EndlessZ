using UnityEngine;

namespace EndlessZ.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerAnimatorController : MonoBehaviour
    {
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField, Min(0f)] private float dampTime = 0.08f;

        private Animator animator;
        private Rigidbody body;
        private int speedHash;
        private int moveXHash;
        private int moveYHash;
        private bool hasSpeedParameter;
        private bool hasMoveXParameter;
        private bool hasMoveYParameter;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();
            CacheAnimatorParameters();
        }

        private void Update()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            localVelocity.y = 0f;

            if (hasSpeedParameter)
            {
                animator.SetFloat(speedHash, localVelocity.magnitude, dampTime, Time.deltaTime);
            }

            if (hasMoveXParameter)
            {
                animator.SetFloat(moveXHash, localVelocity.x, dampTime, Time.deltaTime);
            }

            if (hasMoveYParameter)
            {
                animator.SetFloat(moveYHash, localVelocity.z, dampTime, Time.deltaTime);
            }
        }

        private void CacheAnimatorParameters()
        {
            speedHash = Animator.StringToHash(speedParameter);
            moveXHash = Animator.StringToHash(moveXParameter);
            moveYHash = Animator.StringToHash(moveYParameter);

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Float)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(speedParameter) && parameter.nameHash == speedHash)
                {
                    hasSpeedParameter = true;
                }
                else if (!string.IsNullOrWhiteSpace(moveXParameter) && parameter.nameHash == moveXHash)
                {
                    hasMoveXParameter = true;
                }
                else if (!string.IsNullOrWhiteSpace(moveYParameter) && parameter.nameHash == moveYHash)
                {
                    hasMoveYParameter = true;
                }
            }
        }
    }
}
