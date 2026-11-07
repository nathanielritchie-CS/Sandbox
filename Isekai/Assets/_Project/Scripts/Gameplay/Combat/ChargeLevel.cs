using UnityEngine;

namespace Isekai.Gameplay.Combat
{
    // A charged attack's damage after charging this long: nothing before ready, then minDamage rising linearly to maxDamage at full.
    public static class ChargeLevel
    {
        public static int Damage(float seconds, float readySeconds, float fullSeconds, int minDamage, int maxDamage)
        {
            if (seconds < readySeconds) return 0;
            var t = fullSeconds > readySeconds ? (seconds - readySeconds) / (fullSeconds - readySeconds) : 1f;
            return Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, t)); // Lerp clamps t, so holding past full adds nothing
        }
    }
}
