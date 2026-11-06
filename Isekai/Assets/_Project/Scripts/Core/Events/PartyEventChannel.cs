using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Party Event Channel")]
    public class PartyEventChannel : ScriptableObject
    {
        public event Action<PartyStatus> Raised;

        // Kept so a panel that opens after the event still gets the latest state. Null = nothing to show.
        [NonSerialized] private PartyStatus _current;
        public PartyStatus Current => _current;

        public void Raise(PartyStatus value)
        {
            _current = value;
            Raised?.Invoke(value);
        }
    }
}
