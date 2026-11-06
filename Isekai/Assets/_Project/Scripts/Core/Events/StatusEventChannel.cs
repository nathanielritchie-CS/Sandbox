using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Status Event Channel")]
    public class StatusEventChannel : ScriptableObject
    {
        public event Action<PlayerStatus> Raised;

        // Kept so a window that opens after the event still gets the latest status. Null = no local player.
        [NonSerialized] private PlayerStatus _current;
        public PlayerStatus Current => _current;

        public void Raise(PlayerStatus value)
        {
            _current = value;
            Raised?.Invoke(value);
        }
    }
}
