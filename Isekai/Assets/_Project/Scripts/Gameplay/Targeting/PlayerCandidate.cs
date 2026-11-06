using UnityEngine;

namespace Isekai.Gameplay.Targeting
{
    public readonly struct PlayerCandidate
    {
        public readonly ulong Id;
        public readonly Vector3 Position;

        public PlayerCandidate(ulong id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }
}
