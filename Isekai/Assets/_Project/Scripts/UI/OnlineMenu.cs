using Isekai.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Isekai.UI
{
    public class OnlineMenu : MonoBehaviour
    {
        [SerializeField] private SessionRequestChannel _requests;
        [SerializeField] private TMPro.TMP_InputField _code;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() => _requests.Raise(true, ""));
            _joinButton.onClick.AddListener(() => _requests.Raise(false, _code.text));
        }
    }
}
