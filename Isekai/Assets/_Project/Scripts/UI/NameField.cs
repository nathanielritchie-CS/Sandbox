using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    // Title screen: the name you go by in the other world. Remembered between sessions; your character sends it on spawn.
    public class NameField : MonoBehaviour
    {
        private const string Key = "player.name";

        [SerializeField] private TMPro.TMP_InputField _field;
        [SerializeField] private TextEventChannel _channel;

        private void Awake()
        {
            _field.SetTextWithoutNotify(PlayerPrefs.GetString(Key, ""));
            _channel.Raise(_field.text);
            _field.onValueChanged.AddListener(OnChanged);
        }

        private void OnDisable() => PlayerPrefs.Save();

        private void OnChanged(string text)
        {
            PlayerPrefs.SetString(Key, text);
            _channel.Raise(text);
        }
    }
}
