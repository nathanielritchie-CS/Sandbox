using Isekai.Core.Events;
using Unity.Cinemachine;
using UnityEngine;

namespace Isekai.Gameplay.Cameras
{
    // Kicks the follow camera (through Cinemachine Impulse) whenever the local player loses health.
    public class DamageShake : MonoBehaviour
    {
        [SerializeField] private HealthEventChannel _health;
        [SerializeField] private CinemachineImpulseSource _impulse;
        [SerializeField] private float _force = 0.25f;

        private int _last;

        private void OnEnable()
        {
            _last = _health.Current;
            _health.Raised += OnHealth;
        }

        private void OnDisable() => _health.Raised -= OnHealth;

        private void OnHealth(int current, int max)
        {
            if (max > 0 && current < _last) _impulse.GenerateImpulseWithForce(_force);
            _last = current;
        }
    }
}
