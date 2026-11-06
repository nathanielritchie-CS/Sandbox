using System;
using Isekai.Core.Events;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai.Net
{
    // Lives on the persistent NetworkManager object so the session outlives the Bootstrap scene.
    public class SessionController : MonoBehaviour
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private SessionRequestChannel _requests;
        [SerializeField] private TextEventChannel _status;
        [SerializeField] private string _gameSceneName = "TestLevel";
        [SerializeField] private int _maxPlayers = 4;

        private ISession _session;
        private bool _busy;

        private void OnEnable() => _requests.Raised += OnRequest;
        private void OnDisable() => _requests.Raised -= OnRequest;

        private async void OnRequest(bool isHost, string joinCode)
        {
            if (_busy) return;
            if (!isHost && string.IsNullOrWhiteSpace(joinCode))
            {
                _status.Raise("Enter a join code.");
                return;
            }

            _busy = true;
            try
            {
                _status.Raise(isHost ? "Creating game..." : "Joining game...");
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (isHost)
                {
                    // The session also starts Netcode as host; then the level loads exactly like the direct flow.
                    var options = new SessionOptions { MaxPlayers = _maxPlayers, IsPrivate = true }.WithRelayNetwork();
                    _session = await MultiplayerService.Instance.CreateSessionAsync(options);
                    _status.Raise("Join code: " + _session.Code);
                    _networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
                }
                else
                {
                    var code = joinCode.Trim().ToUpperInvariant();
                    _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                    _status.Raise("Join code: " + code);
                }
            }
            catch (Exception e)
            {
                _status.Raise("Failed: " + e.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private void OnDestroy()
        {
            if (_session == null) return;
            // Fire and forget: the app may be quitting, and the service expires abandoned sessions anyway.
            _ = _session is IHostSession host ? host.DeleteAsync() : _session.LeaveAsync();
        }
    }
}
