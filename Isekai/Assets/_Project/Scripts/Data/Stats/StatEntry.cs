using System;
using UnityEngine;

namespace Isekai.Data.Stats
{
    [Serializable]
    public struct StatEntry
    {
        [SerializeField] private StatType _type;
        [SerializeField] private int _base;
        [SerializeField] private int _perLevel;

        public StatType Type => _type;
        public int Base => _base;
        public int PerLevel => _perLevel;
    }
}
