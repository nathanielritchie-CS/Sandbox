namespace Isekai.Gameplay.Stats
{
    public readonly struct LevelProgress
    {
        public readonly int Level;
        public readonly int XpIntoLevel;
        public readonly int XpToNext;

        public LevelProgress(int level, int xpIntoLevel, int xpToNext)
        {
            Level = level;
            XpIntoLevel = xpIntoLevel;
            XpToNext = xpToNext;
        }
    }
}
