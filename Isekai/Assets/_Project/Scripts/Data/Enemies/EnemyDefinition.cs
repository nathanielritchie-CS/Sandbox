using UnityEngine;

namespace Isekai.Data.Enemies
{
    [CreateAssetMenu(menuName = "Isekai/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private float _aggroRange = 8f;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private int _attackDamage = 8;
        [SerializeField] private float _attackCooldown = 1.2f;
        [Tooltip("Seconds from the attack starting to it connecting; damage lands then.")]
        [SerializeField] private float _attackImpactDelay = 0.25f;

        public float AggroRange => _aggroRange;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public int AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackImpactDelay => _attackImpactDelay;
    }
}
