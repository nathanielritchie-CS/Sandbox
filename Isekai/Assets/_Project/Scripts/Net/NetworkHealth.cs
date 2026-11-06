using System;
using Isekai.Gameplay.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    public class NetworkHealth : NetworkBehaviour
    {
        [SerializeField] private int _maxHealth = 100;

        private readonly NetworkVariable<int> _current = new();
        private float _invulnerableUntil;

        // (current, max), raised on every peer.
        public event Action<int, int> Changed;

        public int Current => _current.Value;
        public int Max => _maxHealth;
        public bool IsAlive => _current.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _current.Value = _maxHealth;
            _current.OnValueChanged += OnChanged;
            Changed?.Invoke(_current.Value, _maxHealth);
        }

        public override void OnNetworkDespawn() => _current.OnValueChanged -= OnChanged;

        // Returns true if this hit killed the target. No damage while invulnerable.
        public bool ServerTakeDamage(int damage)
        {
            if (!IsServer || _current.Value <= 0 || Time.time < _invulnerableUntil) return false;
            _current.Value = Damage.Apply(_current.Value, damage);
            return _current.Value <= 0;
        }

        // Server: ignore damage for a moment (a dodge roll).
        public void ServerSetInvulnerable(float seconds) => _invulnerableUntil = Time.time + seconds;

        public void ServerRestore()
        {
            if (IsServer) _current.Value = _maxHealth;
        }

        private void OnChanged(int previous, int current) => Changed?.Invoke(current, _maxHealth);
    }
}
