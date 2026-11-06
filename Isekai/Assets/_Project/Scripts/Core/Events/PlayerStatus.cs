namespace Isekai.Core.Events
{
    // What the status window shows. Built by the Net layer, read by UI.
    public sealed class PlayerStatus
    {
        public readonly int Level;
        public readonly int XpIntoLevel;
        public readonly int XpToNext;
        public readonly StatLine[] Stats;

        public PlayerStatus(int level, int xpIntoLevel, int xpToNext, StatLine[] stats)
        {
            Level = level;
            XpIntoLevel = xpIntoLevel;
            XpToNext = xpToNext;
            Stats = stats;
        }
    }
}
