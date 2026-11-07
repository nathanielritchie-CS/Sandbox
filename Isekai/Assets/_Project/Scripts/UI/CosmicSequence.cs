using System.Collections;
using Isekai.Core;
using Isekai.Core.Events;
using Isekai.Gameplay.Appearance;
using Isekai.Gameplay.Players;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Isekai.UI
{
    // The cosmic space: the voice explains, the soul takes shape, the player names it; then the new self is saved.
    public class CosmicSequence : MonoBehaviour
    {
        [SerializeField] private GameObject _narration;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private Button _advanceClick;   // full-screen and invisible while the voice speaks
        [SerializeField] private GameObject _creator;
        [SerializeField] private CharacterCreator _colours;
        [SerializeField] private AstralForm _astral;
        [SerializeField] private GameObject _namePanel;
        [SerializeField] private TMP_InputField _nameField;
        [SerializeField] private Button _toName;
        [SerializeField] private Button _back;
        [SerializeField] private Button _begin;
        [SerializeField] private GameObject _confirm;
        [SerializeField] private TMP_Text _confirmText;
        [SerializeField] private Button _confirmYes;
        [SerializeField] private Button _confirmNo;
        [SerializeField] private CanvasGroup _whiteout;
        [SerializeField] private TextEventChannel _created;
        [SerializeField] private float _charactersPerSecond = 40f;
        [SerializeField, TextArea] private string[] _intro;
        [SerializeField, TextArea] private string[] _outro; // {name} becomes the chosen name
        [SerializeField] private InputAction _advanceKey = new("Advance", InputActionType.Button, "<Keyboard>/space");

        private bool _advance;
        private string _chosenName;

        private void Awake()
        {
            _advanceClick.onClick.AddListener(Advance);
            _toName.onClick.AddListener(() => Show(naming: true));
            _back.onClick.AddListener(() => Show(creating: true));
            _begin.onClick.AddListener(AskToBegin);
            _confirmYes.onClick.AddListener(() => StartCoroutine(Finish()));
            _confirmNo.onClick.AddListener(() => _confirm.SetActive(false));
        }

        private void OnEnable()
        {
            _advanceKey.performed += OnAdvanceKey;
            _advanceKey.Enable();
        }

        private void OnDisable()
        {
            _advanceKey.performed -= OnAdvanceKey;
            _advanceKey.Disable();
        }

        private IEnumerator Start()
        {
            _whiteout.alpha = 0f;
            yield return Say(_intro);
            _astral.Materialize();
            _colours.ShowOnPreview();
            Show();
            yield return new WaitForSeconds(0.6f);
            Show(creating: true);
        }

        public void Advance() => _advance = true;

        private void OnAdvanceKey(InputAction.CallbackContext context) => Advance();

        private void Show(bool speaking = false, bool creating = false, bool naming = false)
        {
            _narration.SetActive(speaking);
            _advanceClick.gameObject.SetActive(speaking);
            _creator.SetActive(creating);
            _namePanel.SetActive(naming);
            _confirm.SetActive(false);
            if (naming) _nameField.ActivateInputField();
        }

        private void AskToBegin()
        {
            _chosenName = PlayerName.Clean(_nameField.text);
            _confirmText.text = "Your form and the name " + _chosenName + " will be yours forever.\nBegin your new life?";
            _confirm.SetActive(true);
        }

        private IEnumerator Finish()
        {
            CharacterSave.Create(_chosenName, _colours.Catalog.Ids(), _colours.Colors).Save();
            var outro = new string[_outro.Length];
            for (var i = 0; i < outro.Length; i++) outro[i] = _outro[i].Replace("{name}", _chosenName);
            yield return Say(outro);
            yield return Ease.Tween(1.2f, t => _whiteout.alpha = t, Ease.OutCubic);
            _created.Raise(_chosenName);
        }

        // Typewriter lines; a click or Space finishes a line, the next one moves on.
        private IEnumerator Say(string[] lines)
        {
            Show(speaking: true);
            foreach (var line in lines)
            {
                _advance = false;
                for (var shown = 0f; shown < line.Length && !_advance; shown += Time.unscaledDeltaTime * _charactersPerSecond)
                {
                    _line.text = line.Substring(0, (int)shown);
                    yield return null;
                }
                _line.text = line;
                _advance = false;
                yield return null;
                while (!_advance) yield return null;
            }
        }
    }
}
