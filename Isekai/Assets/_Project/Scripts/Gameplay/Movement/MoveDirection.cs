using UnityEngine;

namespace Isekai.Gameplay.Movement
{
    public static class MoveDirection
    {
        // input.x = right, input.y = forward, relative to the camera's yaw.
        public static Vector3 FromInput(Vector2 input, float cameraYawDegrees)
        {
            var direction = Quaternion.Euler(0f, cameraYawDegrees, 0f) * new Vector3(input.x, 0f, input.y);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }
    }
}
