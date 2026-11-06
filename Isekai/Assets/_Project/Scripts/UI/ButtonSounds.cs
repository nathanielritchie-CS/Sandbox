using Isekai.Core;
using UnityEngine;

namespace Isekai.UI
{
    // Plays a click for every button under this object.
    public class ButtonSounds : MonoBehaviour
    {
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip[] _clicks;

        private void Awake()
        {
            foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
                button.onClick.AddListener(() => Sfx.PlayRandom(_audio, _clicks));
        }
    }
}
