using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Session Request Channel")]
    public class SessionRequestChannel : ScriptableObject
    {
        // (isHost, joinCode); joinCode is ignored when hosting.
        public event Action<bool, string> Raised;

        public void Raise(bool isHost, string joinCode) => Raised?.Invoke(isHost, joinCode);
    }
}
