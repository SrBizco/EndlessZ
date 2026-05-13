using UnityEngine;

namespace EndlessZ.Cameras
{
    public static class CameraFollowMath
    {
        public static Vector3 CalculateTargetPosition(Vector3 followTargetPosition, Vector3 offset)
        {
            return followTargetPosition + offset;
        }
    }
}
