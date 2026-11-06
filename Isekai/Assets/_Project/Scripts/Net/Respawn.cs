using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Server: anything with health comes back to full health after a delay; players also return to their spawn point.
    // (Enemies are moved home by EnemyAI when they revive.)
    [RequireComponent(typeof(NetworkHealth))]
    public class Respawn : NetworkBehaviour
    {
        [SerializeField] private float _respawnSeconds = 5f;

        private NetworkHealth _health;
        private PlayerMovement _movement; // optional
        private float _respawnAt = -1f;

        private void Awake()
        {
            _health = GetComponent<NetworkHealth>();
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned) return;
            if (_health.IsAlive)
            {
                _respawnAt = -1f;
                return;
            }
            if (_respawnAt < 0f)
            {
                _respawnAt = Time.time + _respawnSeconds;
            }
            else if (Time.time >= _respawnAt)
            {
                if (_movement != null) _movement.ServerRespawn();
                _health.ServerRestore();
            }
        }
    }
}
