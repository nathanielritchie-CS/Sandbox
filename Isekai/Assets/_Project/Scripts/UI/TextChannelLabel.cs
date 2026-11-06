using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    public class TextChannelLabel : MonoBehaviour
    {
        [SerializeField] private TextEventChannel _channel;
        [SerializeField] private UnityEngine.UI.Text _text;

        private void OnEnable()
        {
            _channel.Raised += Show;
            Show(_channel.Current);
        }

        private void OnDisable() => _channel.Raised -= Show;

        private void Show(string value) => _text.text = value ?? "";
    }
}
