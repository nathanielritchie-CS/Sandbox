using Isekai.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Owner only: writes the local player's health line for the HUD.
    [RequireComponent(typeof(NetworkHealth))]
    public class PlayerHealthReporter : NetworkBehaviour
    {
        [SerializeField] private TextEventChannel _channel;

        private NetworkHealth _health;

        private void Awake() => _health = GetComponent<NetworkHealth>();

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _health.Changed += Show;
            Show(_health.Current, _health.Max);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _health.Changed -= Show;
            _channel.Raise(null);
        }

        private void Show(int current, int max) =>
            _channel.Raise(current > 0 ? "HP " + current + " / " + max : "You died. Respawning...");
    }
}
