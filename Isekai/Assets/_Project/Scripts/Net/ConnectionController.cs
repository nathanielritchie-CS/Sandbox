using Isekai.Core.Events;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai.Net
{
    public class ConnectionController : MonoBehaviour
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _transport;
        [SerializeField] private ConnectionRequestChannel _requests;
        [SerializeField] private string _gameSceneName = "TestLevel";

        private void OnEnable() => _requests.Raised += Connect;
        private void OnDisable() => _requests.Raised -= Connect;

        private void Connect(bool isHost, string address, ushort port)
        {
            if (isHost)
            {
                _transport.SetConnectionData(address, port, "0.0.0.0");
                if (_networkManager.StartHost())
                    _networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
            }
            else
            {
                _transport.SetConnectionData(address, port);
                _networkManager.StartClient();
            }
        }
    }
}
