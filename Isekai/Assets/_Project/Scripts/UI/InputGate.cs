using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.UI
{
    // Owns the cursor and which player input is on, so windows don't fight over it.
    // Esc menu open: all player input off. A window open (status): you can still move, but the mouse belongs to the
    // window (attack, lock-on and camera look off). Neither: cursor locked and hidden, everything on.
    public class InputGate : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;

        private InputActionMap _player;
        private InputAction[] _mouse;
        private bool _menu;
        private bool _window;

        public bool MenuOpen => _menu;

        private void OnEnable() => Apply();

        private void OnDisable()
        {
            Player().Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetMenu(bool open)
        {
            _menu = open;
            Apply();
        }

        public void SetWindow(bool open)
        {
            _window = open;
            Apply();
        }

        // Lazy: other components call in from their own OnEnable, possibly before this one's Awake.
        private InputActionMap Player()
        {
            if (_player != null) return _player;
            _player = _actions.FindActionMap("Player", true);
            _mouse = new[] { _player.FindAction("Attack", true), _player.FindAction("LockOn", true), _player.FindAction("Look", true) };
            return _player;
        }

        private void Apply()
        {
            var player = Player();
            var free = _menu || _window;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
            if (_menu)
            {
                player.Disable();
                return;
            }
            player.Enable();
            foreach (var action in _mouse)
            {
                if (_window) action.Disable();
                else action.Enable();
            }
        }
    }
}
