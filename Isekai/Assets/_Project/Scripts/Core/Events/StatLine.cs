namespace Isekai.Core.Events
{
    public readonly struct StatLine
    {
        public readonly string Name;
        public readonly int Value;

        public StatLine(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}
