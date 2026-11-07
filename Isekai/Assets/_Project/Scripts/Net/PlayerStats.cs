using System;
using Isekai.Core.Events;
using Isekai.Data.Stats;
using Isekai.Gameplay.Players;
using Isekai.Gameplay.Stats;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    public class PlayerStats : NetworkBehaviour
    {
        [SerializeField] private StatsDefinition _definition;
        [SerializeField] private StatusEventChannel _statusChannel;
        [SerializeField] private TextEventChannel _nameChannel;  // the name typed on the title screen
        [SerializeField] private TextEventChannel _spendChannel; // the status window asks for a point: the stat's name
        [SerializeField] private ParticleSystem _levelUpBurst; // optional: plays on every peer when this player levels up

        private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        private readonly NetworkVariable<int> _xp = new();
        private readonly NetworkList<int> _spent = new(); // points spent, indexed by StatType
        private readonly NetworkVariable<FixedString32Bytes> _name = new();

        public int Level => Progress().Level;
        public string DisplayName => _name.Value.ToString();

        public int Get(StatType type)
        {
            foreach (var entry in _definition.Entries)
                if (entry.Type == type) return Progression.StatAtLevel(entry.Base, entry.PerLevel, Level) + Spent(type);
            return 0;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                for (var i = _spent.Count; i < StatCount; i++) _spent.Add(0);
                _name.Value = new FixedString32Bytes(PlayerName.Default);
            }
            _xp.OnValueChanged += OnXpChanged;
            if (!IsOwner) return;
            _spent.OnListChanged += OnSpentChanged;
            _name.OnValueChanged += OnNameChanged;
            _spendChannel.Raised += RequestPoint;
            SetNameRpc(_nameChannel.Current ?? "");
            Publish();
        }

        public override void OnNetworkDespawn()
        {
            _xp.OnValueChanged -= OnXpChanged;
            if (!IsOwner) return;
            _spent.OnListChanged -= OnSpentChanged;
            _name.OnValueChanged -= OnNameChanged;
            _spendChannel.Raised -= RequestPoint;
            _statusChannel.Raise(null);
        }

        public void ServerAddXp(int amount)
        {
            if (IsServer && amount > 0) _xp.Value += amount;
        }

        // The server cleans whatever the client typed.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNameRpc(string typed) => _name.Value = new FixedString32Bytes(PlayerName.Clean(typed));

        // Owner: the status window's + button names a stat.
        private void RequestPoint(string statName)
        {
            if (Enum.TryParse(statName, out StatType stat)) SpendPointRpc(stat);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SpendPointRpc(StatType stat)
        {
            var index = (int)stat;
            if (index < 0 || index >= _spent.Count || UnspentPoints() <= 0 || !HasEntry(stat)) return;
            _spent[index]++;
        }

        private bool HasEntry(StatType type)
        {
            foreach (var entry in _definition.Entries)
                if (entry.Type == type) return true;
            return false;
        }

        private int Spent(StatType type) => (int)type < _spent.Count ? _spent[(int)type] : 0;

        private int UnspentPoints()
        {
            var spent = 0;
            foreach (var points in _spent) spent += points;
            return Progression.PointsEarned(Level, _definition.PointsPerLevel) - spent;
        }

        private LevelProgress Progress() => Progression.FromTotalXp(_xp.Value, _definition.XpPerLevel);

        private void OnXpChanged(int previous, int current)
        {
            if (IsOwner) Publish();
            if (_levelUpBurst != null && LevelAt(current) > LevelAt(previous)) _levelUpBurst.Play();
        }

        private void OnSpentChanged(NetworkListEvent<int> change) => Publish();

        private void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current) => Publish();

        private int LevelAt(int xp) => Progression.FromTotalXp(xp, _definition.XpPerLevel).Level;

        private void Publish()
        {
            var progress = Progress();
            var lines = new StatLine[_definition.Entries.Count];
            for (var i = 0; i < lines.Length; i++)
            {
                var type = _definition.Entries[i].Type;
                lines[i] = new StatLine(type.ToString(), Get(type));
            }
            _statusChannel.Raise(new PlayerStatus(DisplayName, progress.Level, progress.XpIntoLevel, progress.XpToNext, UnspentPoints(), lines));
        }
    }
}
