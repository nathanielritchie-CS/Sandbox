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
        private const float ChargeGraceSeconds = 0.05f; // network jitter between the charge request and the release

        [SerializeField] private InputActionReference _attack;
        [SerializeField] private AttackDefinition[] _combo; // the light-attack chain, in order
        [SerializeField] private AttackDefinition _heavy;   // charged: keep holding attack after a swing
        [SerializeField] private AttackDefinition[] _swordCombo; // the same chain with the sword in hand
        [SerializeField] private AttackDefinition _swordHeavy;
        [SerializeField] private float _comboWindow = 1f;   // a light attack this soon after the last one continues the chain
        [SerializeField] private float _chargeAfter = 0.45f; // holding attack this long after the press starts a charge

        private PlayerStats _stats;
        private NetworkHealth _health;
        private CharacterAnimator _presentation; // optional
        private PlayerMovement _movement;
        private PlayerStamina _stamina;
        private PlayerLockOn _lockOn; // optional
        private ComboChain _chain;
        private PlayerWeapon _weapon; // optional

        private bool Armed => _weapon != null && _weapon.IsArmed;
        private AttackDefinition[] Combo => Armed ? _swordCombo : _combo;
        private AttackDefinition Heavy => Armed ? _swordHeavy : _heavy;
        private float _nextAttackTime;
        private float _serverChargeStart = -1f;

        // Owner-side input state.
        private bool _holding;
        private float _pressTime;
        private bool _charging;
        private float _chargeStart;
        private int _cuesShown;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<NetworkHealth>();
            _presentation = GetComponent<CharacterAnimator>();
            _movement = GetComponent<PlayerMovement>();
            _stamina = GetComponent<PlayerStamina>();
            _lockOn = GetComponent<PlayerLockOn>();
            _weapon = GetComponent<PlayerWeapon>();
            _chain = new ComboChain(_combo.Length, _comboWindow);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) _attack.action.Enable();
        }

        private void Update()
        {
            if (IsServer && _serverChargeStart >= 0f && (!_health.IsAlive || _movement.ServerIsRolling)) ServerStopCharge();
            if (!IsOwner) return;
            if (!_health.IsAlive || !_attack.action.enabled) // dead, or the menu switched the input off
            {
                _holding = false;
                CancelCharge();
                return;
            }
            if (_attack.action.WasPressedThisFrame()) Press();
            if (_attack.action.WasReleasedThisFrame()) Release();
            TickCharge();
        }

        // Owner: a click swings at once; holding on afterwards charges the heavy blow.
        private void Press()
        {
            if (!_movement.IsGrounded) return; // no swings in mid-air
            _holding = true;
            _pressTime = Time.time;
            FaceTarget();
            AttackRpc(LockedTarget());
        }

        private void Release()
        {
            _holding = false;
            if (!_charging) return;
            StopCharge();
            FaceTarget();
            ReleaseChargeRpc(LockedTarget());
        }

        // Owner: locked on, the swing turns to the target at once (the server aims it there too).
        private void FaceTarget()
        {
            if (_lockOn == null || _lockOn.Target == null) return;
            var toTarget = _lockOn.Target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(toTarget);
        }

        private NetworkObjectReference LockedTarget() =>
            _lockOn != null && _lockOn.Target != null ? new NetworkObjectReference(_lockOn.Target.NetworkObject) : default;

        private void TickCharge()
        {
            if (_charging)
            {
                if (_movement.IsRolling)
                {
                    _holding = false;
                    CancelCharge();
                    return;
                }
                var held = Time.time - _chargeStart;
                if (_cuesShown == 0 && held >= Heavy.ChargeReadySeconds) ShowCue(false);
                if (_cuesShown == 1 && held >= Heavy.ChargeFullSeconds) ShowCue(true);
                return;
            }
            if (!_holding || Time.time - _pressTime < _chargeAfter || _stamina.Current < Heavy.StaminaCost || _movement.IsRolling) return;
            _charging = true;
            _chargeStart = Time.time;
            _cuesShown = 0;
            if (_presentation != null) _presentation.SetChargingLocal(true);
            ChargeRpc();
        }

        private void ShowCue(bool full)
        {
            _cuesShown++;
            if (_presentation != null) _presentation.PlayChargeCue(full);
        }

        private void StopCharge()
        {
            if (!_charging) return;
            _charging = false;
            if (_presentation != null) _presentation.SetChargingLocal(false);
        }

        private void CancelCharge()
        {
            if (!_charging) return;
            StopCharge();
            CancelChargeRpc();
        }

        // target: the owner's lock-on target, or empty.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void AttackRpc(NetworkObjectReference target)
        {
            if (!CanAttack()) return;
            var step = _chain.Next(Time.time);
            var attack = Combo[step - 1];
            Strike(attack, step, attack.Damage, target);
        }

        // Server: the charge is timed by the server's own clock, from this request to the release.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ChargeRpc()
        {
            if (_serverChargeStart >= 0f || !_health.IsAlive || _movement.ServerIsRolling || _stamina.Current < Heavy.StaminaCost) return;
            _serverChargeStart = Time.time;
            if (_presentation != null) _presentation.ServerSetCharging(true);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ReleaseChargeRpc(NetworkObjectReference target)
        {
            if (_serverChargeStart < 0f) return;
            var charged = Time.time - _serverChargeStart + ChargeGraceSeconds;
            ServerStopCharge();
            var damage = ChargeLevel.Damage(charged, Heavy.ChargeReadySeconds, Heavy.ChargeFullSeconds, Heavy.Damage, Heavy.MaxDamage);
            if (damage <= 0 || !CanAttack() || !_stamina.ServerTrySpend(Heavy.StaminaCost)) return;
            _chain.Reset();
            Strike(Heavy, 0, damage, target);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void CancelChargeRpc()
        {
            if (_serverChargeStart >= 0f) ServerStopCharge();
        }

        private void ServerStopCharge()
        {
            _serverChargeStart = -1f;
            if (_presentation != null) _presentation.ServerSetCharging(false);
        }

        private bool CanAttack() => _health.IsAlive && Time.time >= _nextAttackTime && !_movement.ServerIsRolling;

        // Server: targets are picked when the swing starts; the damage lands when the weapon connects. Combo step 0 = heavy.
        private void Strike(AttackDefinition attack, int comboStep, int baseDamage, NetworkObjectReference lockTarget)
        {
            _nextAttackTime = Time.time + attack.Cooldown;
            if (_presentation != null)
            {
                if (comboStep > 0) _presentation.ServerPlayAttack(comboStep);
                else _presentation.ServerPlayHeavyAttack();
            }

            var forward = Aim(lockTarget, attack.Range);
            var targets = new HashSet<NetworkHealth>();
            foreach (var collider in Physics.OverlapSphere(transform.position, attack.Range))
            {
                var health = collider.GetComponentInParent<NetworkHealth>();
                // No friendly fire: players (yourself included) are never targets. Teams/factions come later.
                if (health == null || health.TryGetComponent<PlayerStats>(out _) || targets.Contains(health)) continue;
                if (MeleeReach.IsInReach(transform.position, forward, health.transform.position, attack.Range, attack.ArcDegrees))
                    targets.Add(health);
            }
            if (targets.Count > 0) StartCoroutine(ServerStrike(targets, baseDamage + _stats.Get(StatType.Attack), attack.ImpactDelay));
        }

        // Server: swing toward the lock-on target if it's a living enemy within this attack's range, else where the player faces.
        // Range is still enforced, so naming a far target gains nothing (facing was never trusted: rotation is owner-controlled).
        private Vector3 Aim(NetworkObjectReference lockTarget, float range)
        {
            if (lockTarget.TryGet(out var target) && target.TryGetComponent(out NetworkHealth health) && health.IsAlive && !target.TryGetComponent<PlayerStats>(out _))
            {
                var toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f && toTarget.magnitude <= range) return toTarget.normalized;
            }
            return transform.forward;
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
