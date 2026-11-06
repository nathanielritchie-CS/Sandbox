using Isekai.Core.Events;
using Isekai.Data.Stats;
using Isekai.Gameplay.Stats;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    public class PlayerStats : NetworkBehaviour
    {
        [SerializeField] private StatsDefinition _definition;
        [SerializeField] private StatusEventChannel _statusChannel;

        private readonly NetworkVariable<int> _xp = new();

        public int Level => Progress().Level;

        public int Get(StatType type)
        {
            foreach (var entry in _definition.Entries)
                if (entry.Type == type) return Progression.StatAtLevel(entry.Base, entry.PerLevel, Level);
            return 0;
        }

        public override void OnNetworkSpawn()
        {
            _xp.OnValueChanged += OnXpChanged;
            if (IsOwner) Publish();
        }

        public override void OnNetworkDespawn()
        {
            _xp.OnValueChanged -= OnXpChanged;
            if (IsOwner) _statusChannel.Raise(null);
        }

        public void ServerAddXp(int amount)
        {
            if (IsServer && amount > 0) _xp.Value += amount;
        }

        private LevelProgress Progress() => Progression.FromTotalXp(_xp.Value, _definition.XpPerLevel);

        private void OnXpChanged(int previous, int current)
        {
            if (IsOwner) Publish();
        }

        private void Publish()
        {
            var progress = Progress();
            var lines = new StatLine[_definition.Entries.Count];
            for (var i = 0; i < lines.Length; i++)
            {
                var entry = _definition.Entries[i];
                lines[i] = new StatLine(entry.Type.ToString(), Progression.StatAtLevel(entry.Base, entry.PerLevel, progress.Level));
            }
            _statusChannel.Raise(new PlayerStatus(progress.Level, progress.XpIntoLevel, progress.XpToNext, lines));
        }
    }
}
