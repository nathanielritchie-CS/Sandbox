namespace Isekai.Core.Events
{
    // What the status window shows. Built by the Net layer, read by UI.
    public sealed class PlayerStatus
    {
        public readonly string Name;
        public readonly int Level;
        public readonly int XpIntoLevel;
        public readonly int XpToNext;
        public readonly int UnspentPoints;
        public readonly StatLine[] Stats;

        public PlayerStatus(string name, int level, int xpIntoLevel, int xpToNext, int unspentPoints, StatLine[] stats)
        {
            Name = name;
            Level = level;
            XpIntoLevel = xpIntoLevel;
            XpToNext = xpToNext;
            UnspentPoints = unspentPoints;
            Stats = stats;
        }
    }
}
