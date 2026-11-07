using System.Collections;
using Isekai.Core;
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
        private static readonly int RollId = Animator.StringToHash("Roll");
        private static readonly int ComboId = Animator.StringToHash("Combo");
        private static readonly int HeavyId = Animator.StringToHash("Heavy");
        private static readonly int ChargingId = Animator.StringToHash("Charging");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private const float TeleportSpeed = 15f; // faster than anything can move: a teleport (respawn), not a run
        private const string AttackTag = "Attack"; // swing, heavy and charge states

        // One per look: players get one by client id, anything else uses the first.
        [SerializeField] private Animator[] _variants;
        [SerializeField] private float _speedDampSeconds = 0.1f;
        [SerializeField] private float _hideAfterDeathSeconds;  // 0 = stay visible (players)
        [SerializeField] private Collider _disableWhenDead;     // optional
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip[] _attackClips;
        [SerializeField] private float _attackVolume = 0.5f;    // under the hit sound, which is the payoff
        [SerializeField] private AudioClip[] _rollClips;
        [SerializeField] private float _hitStopSeconds = 0.06f;
        [SerializeField] private ParticleSystem _chargeSparks; // optional: players only
        [SerializeField] private int _readySparks = 10;
        [SerializeField] private int _fullSparks = 24;
        [SerializeField] private float _groundRay = 1.15f; // root to feet (1 m on the player capsule) plus a little

        private NetworkHealth _health;
        private Animator _animator;
        private bool _directional; // the controller blends locomotion by direction (MoveX/MoveZ, m/s in the character's frame)
        private bool _jumps;       // the controller has Jump/Grounded
        private RuntimeAnimatorController _ownMoves;
        private RuntimeAnimatorController _moves; // null = the model's own controller
        private Vector3 _lastPosition;
        private int _lastHealth;
        private float _deadSeconds;

        private void Awake() => _health = GetComponent<NetworkHealth>();

        // A swing, heavy or charge is playing (or about to): the owner moves slower meanwhile.
        public bool IsBusy => _animator != null && _animator.isActiveAndEnabled &&
            (_animator.GetCurrentAnimatorStateInfo(0).IsTag(AttackTag) || _animator.GetNextAnimatorStateInfo(0).IsTag(AttackTag));

        public override void OnNetworkSpawn()
        {
            var chosen = (int)(OwnerClientId % (ulong)_variants.Length);
            for (var i = 0; i < _variants.Length; i++) _variants[i].gameObject.SetActive(i == chosen);
            _animator = _variants[chosen];
            _ownMoves = _animator.runtimeAnimatorController;
            if (_moves != null) _animator.runtimeAnimatorController = _moves;
            _directional = _jumps = false;
            foreach (var parameter in _animator.parameters)
            {
                _directional |= parameter.nameHash == MoveXId;
                _jumps |= parameter.nameHash == GroundedId;
            }
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
            var velocity = Time.deltaTime > 0f ? delta / Time.deltaTime : Vector3.zero;
            if (velocity.magnitude > TeleportSpeed) velocity = Vector3.zero;
            _animator.SetFloat(SpeedId, velocity.magnitude, _speedDampSeconds, Time.deltaTime);
            if (_directional)
            {
                var local = transform.InverseTransformDirection(velocity);
                _animator.SetFloat(MoveXId, local.x, _speedDampSeconds, Time.deltaTime);
                _animator.SetFloat(MoveZId, local.z, _speedDampSeconds, Time.deltaTime);
            }
            _animator.SetBool(DeadId, dead);
            // Every peer probes the ground under every character, so remote players land in step with their movement.
            if (_jumps) _animator.SetBool(GroundedId, Physics.Raycast(transform.position, Vector3.down, _groundRay, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
        }

        // Another moveset with the same parameters and states (e.g. the sword's); null = the model's own. Works before or after spawn.
        public void UseMoves(RuntimeAnimatorController moves)
        {
            _moves = moves;
            if (_animator != null) _animator.runtimeAnimatorController = moves != null ? moves : _ownMoves;
        }

        // Owner: show its own jump at once; the server shows it to everyone else.
        public void PlayJumpLocal() => PlayJump();

        public void ServerPlayJump()
        {
            if (IsServer) JumpRpc();
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void JumpRpc() => PlayJump();

        private void PlayJump()
        {
            if (_jumps && _animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger(JumpId);
        }

        // Server: show this character's attack on every peer. comboStep 1..n picks a player's combo animation; 0 = the only attack.
        public void ServerPlayAttack(int comboStep = 0)
        {
            if (IsServer) AttackRpc(comboStep);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void AttackRpc(int comboStep)
        {
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                if (comboStep > 0) _animator.SetInteger(ComboId, comboStep);
                _animator.SetTrigger(AttackId);
            }
            Sfx.PlayRandom(_audio, _attackClips, _attackVolume);
        }

        public void ServerPlayHeavyAttack()
        {
            if (IsServer) HeavyAttackRpc();
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void HeavyAttackRpc()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetBool(ChargingId, false); // the release; also covers a charge-stop that hasn't arrived yet
                _animator.SetTrigger(HeavyId);
            }
            Sfx.PlayRandom(_audio, _attackClips, _attackVolume);
        }

        // Owner: show its own charge stance at once; the server shows it to everyone else.
        public void SetChargingLocal(bool charging) => SetCharging(charging);

        public void ServerSetCharging(bool charging)
        {
            if (IsServer) ChargingRpc(charging);
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void ChargingRpc(bool charging) => SetCharging(charging);

        private void SetCharging(bool charging)
        {
            if (_animator != null && _animator.isActiveAndEnabled) _animator.SetBool(ChargingId, charging);
        }

        // Owner: a spark burst when the charge is ready, a bigger one at full power.
        public void PlayChargeCue(bool full)
        {
            if (_chargeSparks == null) return;
            _chargeSparks.Emit(new ParticleSystem.EmitParams { startColor = Palette.Accent }, full ? _fullSparks : _readySparks);
        }

        // Owner: freeze the character's animation for a moment when its hit lands.
        public void PlayHitStop()
        {
            if (_animator != null && _animator.isActiveAndEnabled) StartCoroutine(HitStop());
        }

        private IEnumerator HitStop()
        {
            _animator.speed = 0f;
            yield return new WaitForSecondsRealtime(_hitStopSeconds);
            _animator.speed = 1f;
        }

        // Owner: show its own roll at once; the server shows it to everyone else.
        public void PlayRollLocal() => PlayRoll();

        public void ServerPlayRoll()
        {
            if (IsServer) RollRpc();
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void RollRpc() => PlayRoll();

        private void PlayRoll()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetBool(ChargingId, false); // a roll cancels a charge
                _animator.SetTrigger(RollId);
            }
            Sfx.PlayRandom(_audio, _rollClips, _attackVolume);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current > 0 && current < _lastHealth && _animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger(HitId);
            _lastHealth = current;
        }
    }
}
