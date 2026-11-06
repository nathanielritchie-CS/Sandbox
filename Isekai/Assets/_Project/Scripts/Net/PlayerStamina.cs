using Isekai.Core.Events;
using Isekai.Data.Movement;
using Isekai.Gameplay.Movement;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Server-owned stamina: sprinting and rolling spend it, it refills after a pause. The owner's HUD gets (current, max).
    public class PlayerStamina : NetworkBehaviour
    {
        [SerializeField] private MobilityDefinition _definition;
        [SerializeField] private HealthEventChannel _channel;

        private readonly NetworkVariable<int> _current = new();
        private StaminaPool _pool;

        public int Current => _current.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _pool = new StaminaPool(_definition.MaxStamina, _definition.RegenPerSecond, _definition.RegenDelay);
                Publish();
            }
            if (!IsOwner) return;
            _current.OnValueChanged += Report;
            Report(0, _current.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _current.OnValueChanged -= Report;
            _channel.Raise(0, 0);
        }

        private void Update()
        {
            if (!IsServer || _pool == null) return;
            _pool.Tick(Time.deltaTime);
            Publish();
        }

        public bool ServerTrySpend(float amount)
        {
            var spent = _pool.TrySpend(amount);
            Publish();
            return spent;
        }

        public void ServerDrain(float amount)
        {
            _pool.Drain(amount);
            Publish();
        }

        private void Publish() => _current.Value = Mathf.FloorToInt(_pool.Current);

        private void Report(int previous, int current) => _channel.Raise(current, Mathf.RoundToInt(_definition.MaxStamina));
    }
}
