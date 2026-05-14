using EndlessZ.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessZ.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        private Rigidbody body;
        private MovementSpeedModifierTarget speedModifierTarget;
        private Vector2 moveInput;

        public Vector2 MoveInput => moveInput;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            speedModifierTarget = GetComponent<MovementSpeedModifierTarget>();
            if (speedModifierTarget == null)
            {
                speedModifierTarget = gameObject.AddComponent<MovementSpeedModifierTarget>();
            }

            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            moveInput = ReadMoveInput();
        }

        private void FixedUpdate()
        {
            float modifiedSpeed = moveSpeed * speedModifierTarget.CurrentMultiplier;
            Vector3 velocity = PlayerMovementMath.CalculateVelocity(moveInput, modifiedSpeed);
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            return input;
        }
    }
}
