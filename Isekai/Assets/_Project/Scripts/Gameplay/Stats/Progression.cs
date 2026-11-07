using UnityEngine;

namespace Isekai.Gameplay.Stats
{
    public static class Progression
    {
        // Going from level L to L + 1 costs xpPerLevel * L XP. Levels start at 1.
        public static LevelProgress FromTotalXp(int totalXp, int xpPerLevel)
        {
            var step = Mathf.Max(1, xpPerLevel);
            var level = 1;
            var remaining = Mathf.Max(0, totalXp);
            while (remaining >= step * level)
            {
                remaining -= step * level;
                level++;
            }
            return new LevelProgress(level, remaining, step * level);
        }

        public static int StatAtLevel(int baseValue, int perLevel, int level) => baseValue + perLevel * (level - 1);

        // Stat points to spend: pointsPerLevel for every level gained (none at level 1).
        public static int PointsEarned(int level, int pointsPerLevel) => Mathf.Max(0, level - 1) * Mathf.Max(0, pointsPerLevel);
    }
}
