using UnityEngine;

namespace Isekai.Data.Movement
{
    [CreateAssetMenu(menuName = "Isekai/Movement/Mobility Definition")]
    public class MobilityDefinition : ScriptableObject
    {
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _sprintDrainPerSecond = 20f;
        [SerializeField] private float _sprintRestart = 20f;   // after running dry, sprinting waits until stamina is back to this
        [SerializeField] private float _regenPerSecond = 35f;
        [SerializeField] private float _regenDelay = 0.8f;
        [SerializeField] private float _rollCost = 30f;
        [SerializeField] private float _rollDistance = 4.5f;
        [SerializeField] private float _rollSeconds = 0.45f;
        [SerializeField] private float _invulnerableSeconds = 0.3f;

        public float MaxStamina => _maxStamina;
        public float SprintDrainPerSecond => _sprintDrainPerSecond;
        public float SprintRestart => _sprintRestart;
        public float RegenPerSecond => _regenPerSecond;
        public float RegenDelay => _regenDelay;
        public float RollCost => _rollCost;
        public float RollDistance => _rollDistance;
        public float RollSeconds => _rollSeconds;
        public float InvulnerableSeconds => _invulnerableSeconds;
    }
}
