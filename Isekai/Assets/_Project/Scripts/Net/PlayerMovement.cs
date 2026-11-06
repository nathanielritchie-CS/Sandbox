using System.Collections.Generic;
using Isekai.Core.Events;
using Isekai.Gameplay.Movement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Isekai.Net
{
    [RequireComponent(typeof(CharacterController), typeof(NetworkHealth))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _sprint;
        [SerializeField] private TransformEventChannel _localPlayerChannel;
        [SerializeField] private TransformEventChannel _cameraChannel;
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _turnSpeed = 720f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _speedCheckInterval = 0.5f;
        [SerializeField] private float _speedTolerance = 1.5f;

        private CharacterController _controller;
        private NetworkHealth _health;
        private float _verticalVelocity;
        private Vector3 _lastValidPosition;
        private Vector3 _spawnPoint;
        private float _checkTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<NetworkHealth>();
        }

        public override void OnNetworkSpawn()
        {
            // shortcut: spawn points are a line by client id; replace with real spawn points later.
            _spawnPoint = new Vector3(OwnerClientId * 2f, 1f, 0f);
            _lastValidPosition = _spawnPoint;

            if (!IsOwner) return;
            Teleport(_spawnPoint);
            // The host's player spawns before the game scene (and its ground) has loaded, so it falls; put it back once loaded.
            NetworkManager.SceneManager.OnLoadEventCompleted += ReturnToSpawn;
            _move.action.Enable();
            _sprint.action.Enable();
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
            var speed = _sprint.action.IsPressed() ? _sprintSpeed : _walkSpeed;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + _gravity * Time.deltaTime;
            _controller.Move((direction * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _turnSpeed * Time.deltaTime);
        }

        private void CheckSpeed()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < _speedCheckInterval) return;

            if (MovementValidator.IsWithinSpeed(_lastValidPosition, transform.position, _checkTimer, _sprintSpeed * _speedTolerance))
                _lastValidPosition = transform.position;
            else
                SnapBackRpc(_lastValidPosition);
            _checkTimer = 0f;
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
