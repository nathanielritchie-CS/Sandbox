using Isekai.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Owner only: passes the local player's health to the HUD.
    [RequireComponent(typeof(NetworkHealth))]
    public class PlayerHealthReporter : NetworkBehaviour
    {
        [SerializeField] private HealthEventChannel _channel;

        private NetworkHealth _health;

        private void Awake() => _health = GetComponent<NetworkHealth>();

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _health.Changed += _channel.Raise;
            _channel.Raise(_health.Current, _health.Max);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _health.Changed -= _channel.Raise;
            _channel.Raise(0, 0);
        }
    }
}
