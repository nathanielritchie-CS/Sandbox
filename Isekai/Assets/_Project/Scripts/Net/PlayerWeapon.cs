using Isekai.Core.Events;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.Net
{
    // The sword pickup: E next to an available pickup asks the server, which arms this player.
    // Everyone then sees the sword in the hand and the sword moves; PlayerAttack switches to the sword's attacks.
    public class PlayerWeapon : NetworkBehaviour
    {
        private const float ServerReachSlack = 0.75f; // the owner checked reach on its own view; allow for movement in flight

        [SerializeField] private InputActionReference _interact;
        [SerializeField] private float _reach = 2.5f;
        [SerializeField] private GameObject _heldSword;                    // under the right hand bone, hidden while unarmed
        [SerializeField] private RuntimeAnimatorController _swordMoves;    // override of the hero controller
        [SerializeField] private TextEventChannel _prompt;                 // "E  Pick up Sword" while one is in reach

        private readonly NetworkVariable<bool> _armed = new();
        private CharacterAnimator _presentation;
        private NetworkHealth _health;
        private WeaponPickup _inReach;

        public bool IsArmed => _armed.Value;

        private void Awake()
        {
            _presentation = GetComponent<CharacterAnimator>();
            _health = GetComponent<NetworkHealth>();
        }

        public override void OnNetworkSpawn()
        {
            _armed.OnValueChanged += OnArmedChanged;
            Show(_armed.Value);
            if (!IsOwner) return;
            _interact.action.performed += OnInteract;
            _interact.action.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _armed.OnValueChanged -= OnArmedChanged;
            if (!IsOwner) return;
            _interact.action.performed -= OnInteract;
            _prompt.Raise(null);
        }

        private void Update()
        {
            if (!IsOwner) return;
            var nearest = !_armed.Value && _health.IsAlive ? Nearest() : null;
            if (nearest == _inReach) return;
            _inReach = nearest;
            _prompt.Raise(nearest != null ? "E  Pick up " + nearest.DisplayName : null);
        }

        private WeaponPickup Nearest()
        {
            WeaponPickup best = null;
            var bestDistance = float.MaxValue;
            foreach (var hit in Physics.OverlapSphere(transform.position, _reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                if (!hit.TryGetComponent(out WeaponPickup pickup) || !pickup.IsAvailable) continue;
                var distance = (pickup.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = pickup;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (_inReach != null) PickUpRpc(_inReach.NetworkObject);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void PickUpRpc(NetworkObjectReference target)
        {
            if (_armed.Value || !_health.IsAlive || !target.TryGet(out var targetObject) || !targetObject.TryGetComponent(out WeaponPickup pickup)) return;
            if (!pickup.IsAvailable || Vector3.Distance(pickup.transform.position, transform.position) > _reach + ServerReachSlack) return;
            pickup.ServerTake();
            _armed.Value = true;
        }

        private void OnArmedChanged(bool previous, bool current) => Show(current);

        private void Show(bool armed)
        {
            _heldSword.SetActive(armed);
            _presentation.UseMoves(armed ? _swordMoves : null);
        }
    }
}
