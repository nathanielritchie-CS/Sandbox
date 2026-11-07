using System.Collections.Generic;
using Isekai.Core.Events;
using Isekai.Gameplay.Targeting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.Net
{
    // Owner only, never sent to the server: right click locks onto the living enemy nearest the middle of the screen.
    // The camera and the HUD marker read the target channel; movement and attacks read Target.
    [RequireComponent(typeof(NetworkHealth))]
    public class PlayerLockOn : NetworkBehaviour
    {
        [SerializeField] private InputActionReference _lockOn;
        [SerializeField] private TransformEventChannel _cameraChannel;
        [SerializeField] private TransformEventChannel _targetChannel;
        [SerializeField] private float _range = 15f;
        [SerializeField] private float _maxAngle = 45f; // off the camera's centre line
        [SerializeField] private float _releaseRange = 20f;

        private readonly List<NetworkHealth> _found = new();
        private readonly List<Vector3> _points = new();
        private NetworkHealth _health;

        public NetworkHealth Target { get; private set; }

        private void Awake() => _health = GetComponent<NetworkHealth>();

        public override void OnNetworkSpawn()
        {
            if (IsOwner) _lockOn.action.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner) SetTarget(null);
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (_lockOn.action.WasPressedThisFrame()) Toggle();
            if (Target != null && (!_health.IsAlive || !Target.IsAlive || HorizontalDistance(Target.transform.position) > _releaseRange))
                SetTarget(null);
        }

        private void Toggle()
        {
            if (Target != null)
            {
                SetTarget(null);
                return;
            }
            var view = _cameraChannel.Current;
            if (view == null || !_health.IsAlive) return;
            _found.Clear();
            _points.Clear();
            foreach (var collider in Physics.OverlapSphere(transform.position, _range))
            {
                var health = collider.GetComponentInParent<NetworkHealth>();
                // Enemies and the training dummy: anything with health that isn't a player.
                if (health == null || !health.IsAlive || health.TryGetComponent<PlayerStats>(out _) || _found.Contains(health)) continue;
                _found.Add(health);
                _points.Add(collider.bounds.center);
            }
            var pick = LockOnPicker.Pick(view.position, view.forward, transform.position, _points, _range, _maxAngle);
            if (pick >= 0) SetTarget(_found[pick]);
        }

        private float HorizontalDistance(Vector3 point)
        {
            var offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void SetTarget(NetworkHealth target)
        {
            Target = target;
            _targetChannel.Raise(target != null ? target.transform : null);
        }
    }
}
