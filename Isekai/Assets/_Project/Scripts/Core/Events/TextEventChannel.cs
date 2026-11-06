using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Text Event Channel")]
    public class TextEventChannel : ScriptableObject
    {
        public event Action<string> Raised;

        // Kept so a label that appears after the event (e.g. in the next scene) still shows the latest text.
        [NonSerialized] private string _current;
        public string Current => _current;

        public void Raise(string value)
        {
            _current = value;
            Raised?.Invoke(value);
        }
    }
}
