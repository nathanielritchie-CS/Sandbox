using Isekai.Core.Events;
using Isekai.Gameplay.Movement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.Net
{
    [RequireComponent(typeof(CharacterController))]
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
        private float _verticalVelocity;
        private Vector3 _lastValidPosition;
        private float _checkTimer;

        private void Awake() => _controller = GetComponent<CharacterController>();

        public override void OnNetworkSpawn()
        {
            // shortcut: spawn points are a line by client id; replace with real spawn points later.
            var spawn = new Vector3(OwnerClientId * 2f, 1f, 0f);
            _lastValidPosition = spawn;

            if (!IsOwner) return;
            Teleport(spawn);
            _move.action.Enable();
            _sprint.action.Enable();
            _localPlayerChannel.Raise(transform);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner) _localPlayerChannel.Raise(null);
        }

        private void Update()
        {
            if (IsOwner) Move();
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

        [Rpc(SendTo.Owner)]
        private void SnapBackRpc(Vector3 position) => Teleport(position);

        private void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }
    }
}
