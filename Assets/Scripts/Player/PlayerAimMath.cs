using UnityEngine;

namespace EndlessZ.Player
{
    public static class PlayerAimMath
    {
        public static Quaternion CalculateAimRotation(Vector3 origin, Vector3 target)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
