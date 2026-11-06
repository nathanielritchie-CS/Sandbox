using UnityEngine;

namespace Isekai.Gameplay.Combat
{
    public class XpReward : MonoBehaviour
    {
        [SerializeField] private int _amount = 50;

        public int Amount => _amount;
    }
}
