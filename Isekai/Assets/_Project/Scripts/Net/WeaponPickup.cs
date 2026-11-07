using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Isekai.Net
{
    // A weapon lying in the world. The server hands it out and brings it back after a while, so a partner can take one too.
    public class WeaponPickup : NetworkBehaviour
    {
        [SerializeField] private string _displayName = "Sword";
        [SerializeField] private GameObject _visual;
        [SerializeField] private float _returnSeconds = 20f;

        private readonly NetworkVariable<bool> _available = new(true);

        public string DisplayName => _displayName;
        public bool IsAvailable => _available.Value;

        public override void OnNetworkSpawn()
        {
            _available.OnValueChanged += OnAvailableChanged;
            _visual.SetActive(_available.Value);
        }

        public override void OnNetworkDespawn() => _available.OnValueChanged -= OnAvailableChanged;

        private void OnAvailableChanged(bool previous, bool current) => _visual.SetActive(current);

        public void ServerTake()
        {
            _available.Value = false;
            StartCoroutine(ReturnLater());
        }

        private IEnumerator ReturnLater()
        {
            yield return new WaitForSeconds(_returnSeconds);
            _available.Value = true;
        }
    }
}
