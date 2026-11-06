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

        public int Damage => _damage;
        public float Range => _range;
        public float ArcDegrees => _arcDegrees;
        public float Cooldown => _cooldown;
    }
}
