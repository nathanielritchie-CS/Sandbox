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

        // Returns true if this hit killed the target.
        public bool ServerTakeDamage(int damage)
        {
            if (!IsServer || _current.Value <= 0) return false;
            _current.Value = Damage.Apply(_current.Value, damage);
            return _current.Value <= 0;
        }

        public void ServerRestore()
        {
            if (IsServer) _current.Value = _maxHealth;
        }

        private void OnChanged(int previous, int current) => Changed?.Invoke(current, _maxHealth);
    }
}
