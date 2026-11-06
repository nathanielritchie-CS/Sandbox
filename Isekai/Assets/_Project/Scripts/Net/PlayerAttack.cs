using System.Collections;
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
        [SerializeField] private InputActionReference _heavyAttack;
        [SerializeField] private AttackDefinition[] _combo; // the light-attack chain, in order
        [SerializeField] private AttackDefinition _heavy;
        [SerializeField] private float _comboWindow = 1f;   // a light attack this soon after the last one continues the chain

        private PlayerStats _stats;
        private NetworkHealth _health;
        private CharacterAnimator _presentation; // optional
        private PlayerMovement _movement;
        private PlayerStamina _stamina;
        private ComboChain _chain;
        private float _nextAttackTime;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<NetworkHealth>();
            _presentation = GetComponent<CharacterAnimator>();
            _movement = GetComponent<PlayerMovement>();
            _stamina = GetComponent<PlayerStamina>();
            _chain = new ComboChain(_combo.Length, _comboWindow);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _attack.action.Enable();
            _heavyAttack.action.Enable();
        }

        private void Update()
        {
            if (!IsOwner || !_health.IsAlive) return;
            if (_attack.action.WasPressedThisFrame()) AttackRpc();
            if (_heavyAttack.action.WasPressedThisFrame()) HeavyAttackRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void AttackRpc()
        {
            if (!CanAttack()) return;
            var step = _chain.Next(Time.time);
            Strike(_combo[step - 1], step);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void HeavyAttackRpc()
        {
            if (!CanAttack() || !_stamina.ServerTrySpend(_heavy.StaminaCost)) return;
            _chain.Reset();
            Strike(_heavy, 0);
        }

        private bool CanAttack() => _health.IsAlive && Time.time >= _nextAttackTime && !_movement.ServerIsRolling;

        // Server: targets are picked when the swing starts; the damage lands when the weapon connects. Combo step 0 = heavy.
        private void Strike(AttackDefinition attack, int comboStep)
        {
            _nextAttackTime = Time.time + attack.Cooldown;
            if (_presentation != null)
            {
                if (comboStep > 0) _presentation.ServerPlayAttack(comboStep);
                else _presentation.ServerPlayHeavyAttack();
            }

            var targets = new HashSet<NetworkHealth>();
            foreach (var collider in Physics.OverlapSphere(transform.position, attack.Range))
            {
                var health = collider.GetComponentInParent<NetworkHealth>();
                // No friendly fire: players (yourself included) are never targets. Teams/factions come later.
                if (health == null || health.TryGetComponent<PlayerStats>(out _) || targets.Contains(health)) continue;
                if (MeleeReach.IsInReach(transform.position, transform.forward, health.transform.position, attack.Range, attack.ArcDegrees))
                    targets.Add(health);
            }
            if (targets.Count > 0) StartCoroutine(ServerStrike(targets, attack.Damage + _stats.Get(StatType.Attack), attack.ImpactDelay));
        }

        private IEnumerator ServerStrike(HashSet<NetworkHealth> targets, int damage, float impactDelay)
        {
            yield return new WaitForSeconds(impactDelay);
            if (!_health.IsAlive) yield break;
            var landed = false;
            foreach (var target in targets)
            {
                if (target == null) continue;
                var before = target.Current;
                if (target.ServerTakeDamage(damage) && target.TryGetComponent(out XpReward reward))
                    _stats.ServerAddXp(reward.Amount);
                landed |= target.Current < before;
            }
            if (landed) HitConfirmRpc();
        }

        // Server → the attacker's owner: the hit landed (hit-stop on that player's screen only).
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void HitConfirmRpc()
        {
            if (_presentation != null) _presentation.PlayHitStop();
        }
    }
}
