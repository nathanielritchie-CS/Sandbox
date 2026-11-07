using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Data.Stats
{
    [CreateAssetMenu(menuName = "Isekai/Stats/Stats Definition")]
    public class StatsDefinition : ScriptableObject
    {
        [SerializeField] private int _xpPerLevel = 100;
        [SerializeField] private int _pointsPerLevel = 3; // stat points to spend in the status window, per level gained
        [SerializeField] private StatEntry[] _entries;

        public int XpPerLevel => _xpPerLevel;
        public int PointsPerLevel => _pointsPerLevel;
        public IReadOnlyList<StatEntry> Entries => _entries;
    }
}
