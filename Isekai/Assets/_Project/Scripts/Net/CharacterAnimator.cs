using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // Presentation only: drives the character's Animator from replicated movement and health, plus server attack events.
    [RequireComponent(typeof(NetworkHealth))]
    public class CharacterAnimator : NetworkBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private const float TeleportSpeed = 15f; // faster than anything can move: a teleport (respawn), not a run

        // One per look: players get one by client id, anything else uses the first.
        [SerializeField] private Animator[] _variants;
        [SerializeField] private float _speedDampSeconds = 0.1f;
        [SerializeField] private float _hideAfterDeathSeconds;  // 0 = stay visible (players)
        [SerializeField] private Collider _disableWhenDead;     // optional

        private NetworkHealth _health;
        private Animator _animator;
        private Vector3 _lastPosition;
        private int _lastHealth;
        private float _deadSeconds;

        private void Awake() => _health = GetComponent<NetworkHealth>();

        public override void OnNetworkSpawn()
        {
            var chosen = (int)(OwnerClientId % (ulong)_variants.Length);
            for (var i = 0; i < _variants.Length; i++) _variants[i].gameObject.SetActive(i == chosen);
            _animator = _variants[chosen];
            _lastPosition = transform.position;
            _lastHealth = _health.Current;
            _health.Changed += OnHealthChanged;
        }

        public override void OnNetworkDespawn() => _health.Changed -= OnHealthChanged;

        private void Update()
        {
            if (_animator == null) return;
            var dead = !_health.IsAlive;
            _deadSeconds = dead ? _deadSeconds + Time.deltaTime : 0f;
            if (_disableWhenDead != null) _disableWhenDead.enabled = !dead;
            if (_hideAfterDeathSeconds > 0f) _animator.gameObject.SetActive(!dead || _deadSeconds < _hideAfterDeathSeconds);
            if (!_animator.isActiveAndEnabled) return;

            var delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            delta.y = 0f;
            var speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
            if (speed > TeleportSpeed) speed = 0f;
            _animator.SetFloat(SpeedId, speed, _speedDampSeconds, Time.deltaTime);
            _animator.SetBool(DeadId, dead);
        }

        // Server: show this character's attack on every peer.
        public void ServerPlayAttack()
        {
            if (IsServer) AttackRpc();
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void AttackRpc()
        {
            if (_animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger(AttackId);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current > 0 && current < _lastHealth && _animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger(HitId);
            _lastHealth = current;
        }
    }
}
