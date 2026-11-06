using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Server: a dead player comes back at their spawn point with full health after a delay.
    [RequireComponent(typeof(NetworkHealth), typeof(PlayerMovement))]
    public class PlayerRespawn : NetworkBehaviour
    {
        [SerializeField] private float _respawnSeconds = 5f;

        private NetworkHealth _health;
        private PlayerMovement _movement;
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
                _movement.ServerRespawn();
                _health.ServerRestore();
            }
        }
    }
}
