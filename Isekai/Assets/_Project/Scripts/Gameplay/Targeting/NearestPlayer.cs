using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Gameplay.Targeting
{
    public static class NearestPlayer
    {
        // Horizontal distance only, like MeleeReach. Null if nobody is within range.
        public static ulong? Pick(Vector3 origin, IReadOnlyList<PlayerCandidate> candidates, float range)
        {
            ulong? best = null;
            var bestSqr = range * range;
            for (var i = 0; i < candidates.Count; i++)
            {
                var offset = new Vector2(candidates[i].Position.x - origin.x, candidates[i].Position.z - origin.z);
                var sqr = offset.sqrMagnitude;
                if (sqr > bestSqr || (best.HasValue && sqr >= bestSqr)) continue;
                best = candidates[i].Id;
                bestSqr = sqr;
            }
            return best;
        }
    }
}
