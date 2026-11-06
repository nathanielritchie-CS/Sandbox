using UnityEngine;

namespace Isekai.Gameplay.Combat
{
    public static class MeleeReach
    {
        // Horizontal only, so a target's height doesn't change the result.
        public static bool IsInReach(Vector3 origin, Vector3 forward, Vector3 target, float range, float arcDegrees)
        {
            var toTarget = new Vector3(target.x - origin.x, 0f, target.z - origin.z);
            if (toTarget.magnitude > range) return false;
            if (toTarget.sqrMagnitude < 0.0001f) return true;
            var flatForward = new Vector3(forward.x, 0f, forward.z);
            return Vector3.Angle(flatForward, toTarget) <= arcDegrees * 0.5f;
        }
    }
}
