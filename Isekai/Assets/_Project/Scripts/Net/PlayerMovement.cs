using System.Collections.Generic;
using Isekai.Core.Events;
using Isekai.Data.Movement;
using Isekai.Gameplay.Movement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Isekai.Net
{
    [RequireComponent(typeof(CharacterController), typeof(NetworkHealth), typeof(PlayerStamina))]
    public class PlayerMovement : NetworkBehaviour
    {
        private const float SprintThreshold = 1.2f; // moving faster than walk speed × this counts as sprinting

        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _sprint;
        [SerializeField] private InputActionReference _roll;
        [SerializeField] private InputActionReference _jump;
        [SerializeField] private TransformEventChannel _localPlayerChannel;
        [SerializeField] private TransformEventChannel _cameraChannel;
        [SerializeField] private MobilityDefinition _mobility;
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _turnSpeed = 720f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpHeight = 1f;
        [SerializeField] private float _speedCheckInterval = 0.5f;
        [SerializeField] private float _speedTolerance = 1.5f;
        [SerializeField] private float _busySpeedFactor = 0.3f; // while swinging or charging, so the feet don't skate

        private CharacterController _controller;
        private NetworkHealth _health;
        private float _verticalVelocity;
        private Vector3 _lastValidPosition;
        private Vector3 _spawnPoint;
        private float _checkTimer;
        private PlayerStamina _stamina;
        private CharacterAnimator _animator; // optional
        private PlayerLockOn _lockOn;        // optional
        private Vector3 _rollDirection;
        private float _rollTimeLeft;
        private bool _exhausted;
        private float _serverRollEnd = float.MinValue;
        private bool _windowHadStamina = true;
        private Vector3 _lastServerPosition;
        private float _windowPath; // ground covered this window, turns included

        public bool ServerIsRolling => Time.time < _serverRollEnd;
        public bool IsRolling => _rollTimeLeft > 0f; // owner's own view
        public bool IsGrounded => _controller.isGrounded; // owner's own view

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<NetworkHealth>();
            _stamina = GetComponent<PlayerStamina>();
            _animator = GetComponent<CharacterAnimator>();
            _lockOn = GetComponent<PlayerLockOn>();
        }

        public override void OnNetworkSpawn()
        {
            // shortcut: spawn points are a line by client id; replace with real spawn points later.
            _spawnPoint = new Vector3(OwnerClientId * 2f, 1f, 0f);
            _lastValidPosition = _spawnPoint;
            _lastServerPosition = transform.position;

            if (!IsOwner) return;
            Teleport(_spawnPoint);
            // The host's player spawns before the game scene (and its ground) has loaded, so it falls; put it back once loaded.
            NetworkManager.SceneManager.OnLoadEventCompleted += ReturnToSpawn;
            _move.action.Enable();
            _sprint.action.Enable();
            _roll.action.Enable();
            _jump.action.Enable();
            _localPlayerChannel.Raise(transform);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (NetworkManager.SceneManager != null) NetworkManager.SceneManager.OnLoadEventCompleted -= ReturnToSpawn;
            _localPlayerChannel.Raise(null);
        }

        private void ReturnToSpawn(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut) => Teleport(_spawnPoint);

        private void Update()
        {
            if (IsOwner && _health.IsAlive) Move();
            if (IsServer) CheckSpeed();
        }

        private void Move()
        {
            var camera = _cameraChannel.Current;
            var direction = MoveDirection.FromInput(_move.action.ReadValue<Vector2>(), camera != null ? camera.eulerAngles.y : 0f);
            if (_roll.action.WasPressedThisFrame()) TryRoll(direction);

            var rolling = _rollTimeLeft > 0f;
            if (rolling) _rollTimeLeft -= Time.deltaTime;
            var busy = _animator != null && _animator.IsBusy;
            var velocity = rolling ? _rollDirection * (_mobility.RollDistance / _mobility.RollSeconds) : direction * (WalkOrSprintSpeed() * (busy ? _busySpeedFactor : 1f));
            var jump = _jump.action.WasPressedThisFrame() && _controller.isGrounded && !rolling && !busy;
            var startVelocity = jump ? Mathf.Sqrt(-2f * _gravity * _jumpHeight) : _controller.isGrounded ? -1f : _verticalVelocity;
            _verticalVelocity = _controller.isGrounded && !jump ? -1f : startVelocity + _gravity * Time.deltaTime;
            if (jump)
            {
                if (_animator != null) _animator.PlayJumpLocal();
                JumpRpc();
            }
            // The frame's average vertical speed: the jump keeps its height even through a long frame.
            _controller.Move((velocity + Vector3.up * ((startVelocity + _verticalVelocity) * 0.5f)) * Time.deltaTime);

            // Locked on: always face the target; moving sideways or back then plays the strafe animations.
            var facing = direction;
            var target = _lockOn != null ? _lockOn.Target : null;
            if (target != null)
            {
                facing = target.transform.position - transform.position;
                facing.y = 0f;
            }
            if (!rolling && facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), _turnSpeed * Time.deltaTime);
        }

        // After running dry, sprinting waits until some stamina is back.
        private float WalkOrSprintSpeed()
        {
            if (_stamina.Current <= 0) _exhausted = true;
            else if (_stamina.Current >= _mobility.SprintRestart) _exhausted = false;
            return _sprint.action.IsPressed() && !_exhausted ? _sprintSpeed : _walkSpeed;
        }

        // Owner: roll at once if the replicated stamina allows; the server decides whether it counts (cost, invulnerability).
        private void TryRoll(Vector3 direction)
        {
            if (_rollTimeLeft > 0f || _stamina.Current < _mobility.RollCost) return;
            _rollDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(_rollDirection);
            _rollTimeLeft = _mobility.RollSeconds;
            if (_animator != null) _animator.PlayRollLocal();
            RollRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RollRpc()
        {
            if (!_health.IsAlive || ServerIsRolling || !_stamina.ServerTrySpend(_mobility.RollCost)) return;
            _serverRollEnd = Time.time + _mobility.RollSeconds;
            _health.ServerSetInvulnerable(_mobility.InvulnerableSeconds);
            if (_animator != null) _animator.ServerPlayRoll();
        }

        // Animation only: the jump itself is owner movement, which the server checks like any other.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void JumpRpc()
        {
            if (_health.IsAlive && _animator != null) _animator.ServerPlayJump();
        }

        private void CheckSpeed()
        {
            var step = transform.position - _lastServerPosition;
            step.y = 0f;
            _windowPath += step.magnitude;
            _lastServerPosition = transform.position;
            _checkTimer += Time.deltaTime;
            if (_checkTimer < _speedCheckInterval) return;

            // A roll is a short burst: a window that had one may move at sprint pace, and isn't billed as sprinting.
            var rolled = Time.time - _serverRollEnd < _checkTimer;
            var maxSpeed = (_windowHadStamina || rolled ? _sprintSpeed : _walkSpeed) * _speedTolerance;
            if (MovementValidator.IsWithinSpeed(_lastValidPosition, transform.position, _checkTimer, maxSpeed))
            {
                if (!rolled && _windowPath / _checkTimer > _walkSpeed * SprintThreshold)
                    _stamina.ServerDrain(_mobility.SprintDrainPerSecond * _checkTimer);
                _lastValidPosition = transform.position;
            }
            else
                SnapBackRpc(_lastValidPosition);
            _checkTimer = 0f;
            _windowPath = 0f;
            _windowHadStamina = _stamina.Current > 0;
        }

        // Server: put the owner back at their spawn point (after death).
        public void ServerRespawn()
        {
            _lastValidPosition = _spawnPoint;
            SnapBackRpc(_spawnPoint);
        }

        // Server-only: otherwise any client could teleport another player by calling this on their object.
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void SnapBackRpc(Vector3 position) => Teleport(position);

        private void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _verticalVelocity = 0f;
            _controller.enabled = true;
        }
    }
}
