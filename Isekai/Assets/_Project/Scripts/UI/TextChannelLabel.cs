using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    public class TextChannelLabel : MonoBehaviour
    {
        [SerializeField] private TextEventChannel _channel;
        [SerializeField] private TMPro.TMP_Text _text;
        [SerializeField] private GameObject _background; // optional: hidden while there is no text

        private void OnEnable()
        {
            _channel.Raised += Show;
            Show(_channel.Current);
        }

        private void OnDisable() => _channel.Raised -= Show;

        private void Show(string value)
        {
            _text.text = value ?? "";
            if (_background != null) _background.SetActive(!string.IsNullOrEmpty(value));
        }
    }
}
