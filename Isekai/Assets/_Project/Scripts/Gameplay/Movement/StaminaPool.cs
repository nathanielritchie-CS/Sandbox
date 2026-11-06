using UnityEngine;

namespace Isekai.Gameplay.Movement
{
    // Stamina that spending empties and that refills once nothing has been spent for a moment.
    public class StaminaPool
    {
        private readonly float _regenPerSecond;
        private readonly float _regenDelay;
        private float _sinceSpent;

        public StaminaPool(float max, float regenPerSecond, float regenDelay)
        {
            Max = max;
            Current = max;
            _regenPerSecond = regenPerSecond;
            _regenDelay = regenDelay;
            _sinceSpent = regenDelay;
        }

        public float Max { get; }
        public float Current { get; private set; }

        // All or nothing (a roll).
        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;
            Current -= amount;
            _sinceSpent = 0f;
            return true;
        }

        // As much as there is (sprinting).
        public void Drain(float amount)
        {
            Current = Mathf.Max(0f, Current - amount);
            _sinceSpent = 0f;
        }

        public void Tick(float deltaTime)
        {
            _sinceSpent += deltaTime;
            if (_sinceSpent >= _regenDelay) Current = Mathf.Min(Max, Current + _regenPerSecond * deltaTime);
        }
    }
}
