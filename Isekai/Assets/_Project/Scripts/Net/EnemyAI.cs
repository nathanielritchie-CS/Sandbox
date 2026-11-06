using System.Collections.Generic;
using Isekai.Data.Enemies;
using Isekai.Data.Stats;
using Isekai.Gameplay.Combat;
using Isekai.Gameplay.Targeting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Isekai.Net
{
    // Server-only brain: chase the nearest living player in range, hit them when close enough.
    [RequireComponent(typeof(NetworkHealth), typeof(NavMeshAgent))]
    public class EnemyAI : NetworkBehaviour
    {
        private const float RepathSeconds = 0.2f;

        [SerializeField] private EnemyDefinition _definition;

        private readonly List<PlayerCandidate> _candidates = new();
        private NavMeshAgent _agent;
        private NetworkHealth _health;
        private Vector3 _home;
        private float _nextAttackTime;
        private float _nextRepathTime;
        private bool _wasAlive = true;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<NetworkHealth>();
            // Only the server steers; clients follow the server-authoritative NetworkTransform.
            _agent.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _home = transform.position;
            _agent.enabled = true;
            _agent.speed = _definition.MoveSpeed;
            _agent.stoppingDistance = _definition.AttackRange * 0.8f;
        }

        private void Update()
        {
            if (!IsServer || !_agent.enabled || !_agent.isOnNavMesh) return;

            if (!_health.IsAlive)
            {
                if (_agent.hasPath) _agent.ResetPath();
                _wasAlive = false;
                return;
            }
            if (!_wasAlive)
            {
                // Just came back (the dummy behaviour restores health): start again from home.
                _agent.Warp(_home);
                _wasAlive = true;
            }

            var target = FindTarget();
            if (target == null)
            {
                if (_agent.hasPath) _agent.ResetPath();
                return;
            }

            var targetPosition = target.transform.position;
            if (Time.time >= _nextRepathTime && NavMesh.SamplePosition(targetPosition, out var hit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _nextRepathTime = Time.time + RepathSeconds;
            }

            var offset = targetPosition - transform.position;
            offset.y = 0f;
            if (offset.magnitude > _definition.AttackRange || Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + _definition.AttackCooldown;
            var defense = target.TryGetComponent(out PlayerStats stats) ? stats.Get(StatType.Defense) : 0;
            target.GetComponent<NetworkHealth>().ServerTakeDamage(Damage.Mitigate(_definition.AttackDamage, defense));
        }

        private NetworkObject FindTarget()
        {
            _candidates.Clear();
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                var player = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (player != null && player.TryGetComponent(out NetworkHealth health) && health.IsAlive)
                    _candidates.Add(new PlayerCandidate(clientId, player.transform.position));
            }
            var targetId = NearestPlayer.Pick(transform.position, _candidates, _definition.AggroRange);
            return targetId.HasValue ? NetworkManager.SpawnManager.GetPlayerNetworkObject(targetId.Value) : null;
        }
    }
}
