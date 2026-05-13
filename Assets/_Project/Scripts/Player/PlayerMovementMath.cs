using UnityEngine;

namespace EndlessZ.Player
{
    public static class PlayerMovementMath
    {
        public static Vector2 ClampMoveInput(Vector2 input)
        {
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        public static Vector3 CalculateVelocity(Vector2 input, float moveSpeed)
        {
            Vector2 clampedInput = ClampMoveInput(input);
            return new Vector3(clampedInput.x, 0f, clampedInput.y) * moveSpeed;
        }
    }
}
