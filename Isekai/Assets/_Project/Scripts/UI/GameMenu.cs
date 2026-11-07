using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.UI
{
    // Esc opens the menu (volume, controls): the cursor is freed and the player's input switched off through the gate.
    // The world keeps running; co-op can't pause.
    public class GameMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private InputGate _gate;
        [SerializeField] private InputAction _toggle = new("ToggleMenu", InputActionType.Button, "<Keyboard>/escape");

        private void OnEnable()
        {
            _toggle.performed += OnToggle;
            _toggle.Enable();
            Show(false);
        }

        private void OnDisable()
        {
            _toggle.performed -= OnToggle;
            _toggle.Disable();
        }

        private void OnToggle(InputAction.CallbackContext context) => Show(!_panel.activeSelf);

        private void Show(bool open)
        {
            _panel.SetActive(open);
            _gate.SetMenu(open);
        }
    }
}
