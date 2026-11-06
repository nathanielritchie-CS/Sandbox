using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Health Event Channel")]
    public class HealthEventChannel : ScriptableObject
    {
        // (current, max). Max 0 = no local player.
        public event Action<int, int> Raised;

        // Kept so a HUD that appears after the event (e.g. in the next scene) still shows the latest value.
        [NonSerialized] private int _current;
        [NonSerialized] private int _max;
        public int Current => _current;
        public int Max => _max;

        public void Raise(int current, int max)
        {
            _current = current;
            _max = max;
            Raised?.Invoke(current, max);
        }
    }
}
