using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Transform Event Channel")]
    public class TransformEventChannel : ScriptableObject
    {
        public event Action<Transform> Raised;

        // Kept so listeners that subscribe after the event (e.g. in a later-loaded scene) still get it.
        [NonSerialized] private Transform _current;
        public Transform Current => _current;

        public void Raise(Transform value)
        {
            _current = value;
            Raised?.Invoke(value);
        }
    }
}
