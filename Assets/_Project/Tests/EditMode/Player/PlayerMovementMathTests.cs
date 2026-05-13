using EndlessZ.Player;
using EndlessZ.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace EndlessZ.Tests.Player
{
    public class PlayerMovementMathTests
    {
        [Test]
        public void ClampMoveInput_NormalizesDiagonalInput()
        {
            Vector2 input = new Vector2(1f, 1f);

            Vector2 result = PlayerMovementMath.ClampMoveInput(input);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CalculateVelocity_MapsInputToXZPlane()
        {
            Vector2 input = new Vector2(0.5f, -1f);

            Vector3 velocity = PlayerMovementMath.CalculateVelocity(input, 4f);

            Assert.That(velocity.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(velocity.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(velocity.z, Is.EqualTo(-4f).Within(0.0001f));
        }

        [Test]
        public void CalculateAimRotation_FacesTargetOnXZPlane()
        {
            Vector3 origin = Vector3.zero;
            Vector3 target = new Vector3(10f, 5f, 0f);

            Quaternion rotation = PlayerAimMath.CalculateAimRotation(origin, target);

            Vector3 forward = rotation * Vector3.forward;
            Assert.That(forward.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(forward.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(forward.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void CalculateCameraTarget_AddsOffsetToFollowTarget()
        {
            Vector3 target = new Vector3(2f, 0f, -3f);
            Vector3 offset = new Vector3(0f, 12f, -8f);

            Vector3 cameraTarget = CameraFollowMath.CalculateTargetPosition(target, offset);

            Assert.That(cameraTarget, Is.EqualTo(new Vector3(2f, 12f, -11f)));
        }
    }
}
