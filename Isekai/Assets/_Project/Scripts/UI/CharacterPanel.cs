using Isekai.Core.Events;
using Isekai.Gameplay.Appearance;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Isekai.UI
{
    // Title screen: your Otherworlder. Without one you create it in the cosmic space before you can play.
    public class CharacterPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _create;
        [SerializeField] private TMP_Text _createLabel;
        [SerializeField] private GameObject _confirm;
        [SerializeField] private TMP_Text _confirmText;
        [SerializeField] private Button _confirmYes;
        [SerializeField] private Button _confirmNo;
        [SerializeField] private Selectable[] _needsCharacter;   // host/join controls
        [SerializeField] private GameObject[] _hideInCosmicSpace; // the title screen's own layout
        [SerializeField] private TextEventChannel _name;          // the saved name, sent by your character on spawn
        [SerializeField] private TextEventChannel _created;       // raised by the cosmic space once the new self is saved
        [SerializeField] private string _cosmicScene = "CosmicSpace";

        private CharacterSave _save;

        private void Awake()
        {
            _create.onClick.AddListener(OnCreate);
            _confirmYes.onClick.AddListener(() =>
            {
                CharacterSave.Delete();
                _confirm.SetActive(false);
                EnterCosmicSpace();
            });
            _confirmNo.onClick.AddListener(() => _confirm.SetActive(false));
            _confirm.SetActive(false);
            Refresh();
        }

        private void OnEnable() => _created.Raised += OnCreated;

        private void OnDisable() => _created.Raised -= OnCreated;

        private void OnCreate()
        {
            if (_save == null)
            {
                EnterCosmicSpace();
                return;
            }
            _confirmText.text = "Start over? " + _save.Name + " will be gone for good.";
            _confirm.SetActive(true);
        }

        // Additive, so the persistent NetworkManager in this scene isn't loaded twice on the way back.
        private void EnterCosmicSpace()
        {
            foreach (var go in _hideInCosmicSpace) go.SetActive(false);
            var load = SceneManager.LoadSceneAsync(_cosmicScene, LoadSceneMode.Additive);
            load.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(_cosmicScene)); // its sky and light
        }

        private void OnCreated(string newName)
        {
            SceneManager.UnloadSceneAsync(_cosmicScene);
            foreach (var go in _hideInCosmicSpace) go.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            _save = CharacterSave.Load();
            var has = _save != null;
            _label.text = has ? "Your Otherworlder:  " + _save.Name : "You have no Otherworlder yet.";
            _createLabel.text = has ? "Start over" : "Create your Otherworlder";
            _name.Raise(has ? _save.Name : "");
            foreach (var control in _needsCharacter) control.interactable = has;
        }
    }
}
