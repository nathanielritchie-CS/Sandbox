using System.Collections.Generic;
using Isekai.Data.Combat;
using Isekai.Data.Stats;
using Isekai.Gameplay.Combat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.Net
{
    [RequireComponent(typeof(PlayerStats), typeof(NetworkHealth))]
    public class PlayerAttack : NetworkBehaviour
    {
        [SerializeField] private InputActionReference _attack;
        [SerializeField] private AttackDefinition _definition;

        private readonly HashSet<NetworkHealth> _hit = new();
        private PlayerStats _stats;
        private NetworkHealth _health;
        private float _nextAttackTime;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<NetworkHealth>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) _attack.action.Enable();
        }

        private void Update()
        {
            if (IsOwner && _health.IsAlive && _attack.action.WasPressedThisFrame()) AttackRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void AttackRpc()
        {
            if (!_health.IsAlive || Time.time < _nextAttackTime) return;
            _nextAttackTime = Time.time + _definition.Cooldown;

            var damage = _definition.Damage + _stats.Get(StatType.Attack);
            _hit.Clear();
            foreach (var collider in Physics.OverlapSphere(transform.position, _definition.Range))
            {
                var health = collider.GetComponentInParent<NetworkHealth>();
                // No friendly fire: players (yourself included) are never targets. Teams/factions come later.
                if (health == null || health.TryGetComponent<PlayerStats>(out _) || !_hit.Add(health)) continue;
                if (!MeleeReach.IsInReach(transform.position, transform.forward, health.transform.position, _definition.Range, _definition.ArcDegrees)) continue;
                if (health.ServerTakeDamage(damage) && health.TryGetComponent(out XpReward reward))
                    _stats.ServerAddXp(reward.Amount);
            }
        }
    }
}
