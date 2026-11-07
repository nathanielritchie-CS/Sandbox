using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Gameplay.Targeting
{
    public static class LockOnPicker
    {
        // The candidate closest to the middle of the screen: smallest angle off the camera's centre line (at most maxAngle),
        // within range of the player (horizontal distance, like MeleeReach). -1 if none; a tie keeps the first.
        public static int Pick(Vector3 viewOrigin, Vector3 viewForward, Vector3 player, IReadOnlyList<Vector3> candidates, float range, float maxAngle)
        {
            var best = -1;
            var bestAngle = maxAngle;
            for (var i = 0; i < candidates.Count; i++)
            {
                var offset = new Vector2(candidates[i].x - player.x, candidates[i].z - player.z);
                if (offset.sqrMagnitude > range * range) continue;
                var angle = Vector3.Angle(viewForward, candidates[i] - viewOrigin);
                if (angle > bestAngle || (best >= 0 && angle >= bestAngle)) continue;
                best = i;
                bestAngle = angle;
            }
            return best;
        }
    }
}
