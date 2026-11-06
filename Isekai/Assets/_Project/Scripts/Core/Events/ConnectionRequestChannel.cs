using System;
using UnityEngine;

namespace Isekai.Core.Events
{
    [CreateAssetMenu(menuName = "Isekai/Events/Connection Request Channel")]
    public class ConnectionRequestChannel : ScriptableObject
    {
        public event Action<bool, string, ushort> Raised;

        public void Raise(bool isHost, string address, ushort port) => Raised?.Invoke(isHost, address, port);
    }
}
