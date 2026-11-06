using UnityEngine;

namespace Isekai.Gameplay.Combat
{
    public static class Damage
    {
        // Negative damage is ignored so a bad value can never heal.
        public static int Apply(int currentHealth, int damage) => Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        // Defense removes flat damage, but a hit always does at least 1.
        public static int Mitigate(int rawDamage, int defense) => Mathf.Max(1, rawDamage - Mathf.Max(0, defense));
    }
}
