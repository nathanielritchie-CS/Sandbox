using Isekai.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Isekai.UI
{
    public class ConnectionMenu : MonoBehaviour
    {
        [SerializeField] private ConnectionRequestChannel _requests;
        [SerializeField] private InputField _address;
        [SerializeField] private InputField _port;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() => Request(true));
            _joinButton.onClick.AddListener(() => Request(false));
        }

        private void Request(bool isHost)
        {
            if (!ushort.TryParse(_port.text, out var port))
            {
                Debug.LogWarning($"Invalid port: '{_port.text}'");
                return;
            }
            _hostButton.interactable = false;
            _joinButton.interactable = false;
            _requests.Raise(isHost, _address.text, port);
        }
    }
}
