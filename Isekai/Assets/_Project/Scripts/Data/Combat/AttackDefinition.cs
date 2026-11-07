using UnityEngine;

namespace Isekai.Data.Combat
{
    [CreateAssetMenu(menuName = "Isekai/Combat/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _range = 2f;
        [SerializeField] private float _arcDegrees = 90f;
        [SerializeField] private float _cooldown = 0.5f;
        [Tooltip("Seconds from the swing starting to the weapon connecting; damage lands then.")]
        [SerializeField] private float _impactDelay = 0.3f;
        [SerializeField] private float _staminaCost;

        [Header("Charged attacks only")]
        [Tooltip("Damage at full charge; Damage is dealt at the minimum (ready) charge.")]
        [SerializeField] private int _maxDamage;
        [Tooltip("Seconds of charging before a release counts.")]
        [SerializeField] private float _chargeReadySeconds;
        [Tooltip("Seconds of charging for full power.")]
        [SerializeField] private float _chargeFullSeconds;

        public int Damage => _damage;
        public float Range => _range;
        public float ArcDegrees => _arcDegrees;
        public float Cooldown => _cooldown;
        public float ImpactDelay => _impactDelay;
        public float StaminaCost => _staminaCost;
        public int MaxDamage => _maxDamage;
        public float ChargeReadySeconds => _chargeReadySeconds;
        public float ChargeFullSeconds => _chargeFullSeconds;
    }
}
