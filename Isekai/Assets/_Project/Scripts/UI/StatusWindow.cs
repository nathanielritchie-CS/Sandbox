using System.Text;
using Isekai.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.UI
{
    public class StatusWindow : MonoBehaviour
    {
        [SerializeField] private StatusEventChannel _status;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMPro.TMP_Text _text;
        [SerializeField] private InputAction _toggle = new("ToggleStatus", InputActionType.Button, "<Keyboard>/c");

        private void OnEnable()
        {
            _toggle.performed += OnToggle;
            _toggle.Enable();
            _status.Raised += Show;
            Show(_status.Current);
        }

        private void OnDisable()
        {
            _toggle.performed -= OnToggle;
            _toggle.Disable();
            _status.Raised -= Show;
        }

        private void OnToggle(InputAction.CallbackContext context) => _panel.SetActive(!_panel.activeSelf);

        private void Show(PlayerStatus status)
        {
            if (status == null)
            {
                _text.text = "No character";
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Level ").Append(status.Level).Append('\n');
            sb.Append("XP ").Append(status.XpIntoLevel).Append(" / ").Append(status.XpToNext).Append("\n\n");
            foreach (var line in status.Stats)
                sb.Append(line.Name).Append(": ").Append(line.Value).Append('\n');
            _text.text = sb.ToString();
        }
    }
}
