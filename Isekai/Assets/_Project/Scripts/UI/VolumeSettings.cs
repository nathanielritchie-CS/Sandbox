using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Isekai.UI
{
    // Volume sliders drive exposed mixer parameters and are remembered in PlayerPrefs.
    // Without sliders (title screen) it only applies the saved values.
    public class VolumeSettings : MonoBehaviour
    {
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private Slider _master;   // optional
        [SerializeField] private Slider _effects;  // optional
        [SerializeField] private Slider _ambience; // optional

        // Start, not Awake: AudioMixer.SetFloat called in Awake doesn't stick.
        private void Start()
        {
            Bind("MasterVol", 0.5f, _master);
            Bind("SFXVol", 1f, _effects);
            Bind("AmbientVol", 1f, _ambience);
        }

        private void OnDisable() => PlayerPrefs.Save();

        private void Bind(string parameter, float fallback, Slider slider)
        {
            var key = "volume." + parameter;
            var value = PlayerPrefs.GetFloat(key, fallback);
            Apply(parameter, value);
            if (slider == null) return;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v =>
            {
                Apply(parameter, v);
                PlayerPrefs.SetFloat(key, v);
            });
        }

        // Slider 0..1 to decibels: 1 = the mix as designed (0 dB), 0 = silent (-80 dB).
        private void Apply(string parameter, float value) => _mixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}
