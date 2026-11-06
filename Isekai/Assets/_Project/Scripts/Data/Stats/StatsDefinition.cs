using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Data.Stats
{
    [CreateAssetMenu(menuName = "Isekai/Stats/Stats Definition")]
    public class StatsDefinition : ScriptableObject
    {
        [SerializeField] private int _xpPerLevel = 100;
        [SerializeField] private StatEntry[] _entries;

        public int XpPerLevel => _xpPerLevel;
        public IReadOnlyList<StatEntry> Entries => _entries;
    }
}
