using UnityEngine;

namespace Isekai.Gameplay.Movement
{
    public static class MovementValidator
    {
        // Horizontal only: falling can legitimately exceed sprint speed.
        public static bool IsWithinSpeed(Vector3 from, Vector3 to, float elapsedSeconds, float maxSpeed)
        {
            var horizontal = new Vector2(to.x - from.x, to.z - from.z);
            return horizontal.magnitude <= maxSpeed * elapsedSeconds;
        }
    }
}
