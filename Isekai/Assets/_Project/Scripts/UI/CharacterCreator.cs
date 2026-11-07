using Isekai.Data.Appearance;
using Isekai.Gameplay.Appearance;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Isekai.UI
{
    // The creator's colours: pick a part, then a swatch or any colour (hue, saturation, brightness).
    // The preview model shows every change at once.
    public class CharacterCreator : MonoBehaviour
    {
        [SerializeField] private AppearanceApplier _preview;
        [SerializeField] private Button[] _slots;      // one per catalog colour slot, in catalog order
        [SerializeField] private Image[] _chips;       // each slot's current colour
        [SerializeField] private TMP_Text _editing;    // the part being coloured
        [SerializeField] private Button[] _swatches;   // filled from that part's palette
        [SerializeField] private Slider _hue;
        [SerializeField] private Slider _saturation;
        [SerializeField] private Slider _brightness;
        [SerializeField] private Button _randomize;
        [SerializeField] private Button _reset;
        [SerializeField] private Color _selectedTint = new(0.31f, 0.89f, 1f, 0.35f);
        [SerializeField] private Color _idleTint = new(0.31f, 0.89f, 1f, 0.06f);

        private Color32[] _colors;
        private int _selected;

        public AppearanceCatalog Catalog => _preview.Catalog;
        public Color32[] Colors => _colors ??= Catalog.Defaults(); // usable before the panel is first shown (Awake hasn't run)

        private void Awake()
        {
            _colors ??= Catalog.Defaults();
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = i;
                _slots[i].onClick.AddListener(() => Select(slot));
            }
            for (var i = 0; i < _swatches.Length; i++)
            {
                var cell = i;
                _swatches[i].onClick.AddListener(() => SetColor(Catalog.Colors[_selected].Palette[cell], true));
            }
            _hue.onValueChanged.AddListener(_ => PickHsv());
            _saturation.onValueChanged.AddListener(_ => PickHsv());
            _brightness.onValueChanged.AddListener(_ => PickHsv());
            _randomize.onClick.AddListener(Randomize);
            _reset.onClick.AddListener(() => SetAll(Catalog.Defaults()));
            RefreshChips();
            Select(0);
        }

        public void ShowOnPreview() => _preview.Apply(Colors);

        public void Select(int slot)
        {
            _selected = slot;
            var part = Catalog.Colors[slot];
            _editing.text = part.Label.ToUpperInvariant();
            for (var i = 0; i < _swatches.Length; i++)
            {
                var has = part.Palette != null && i < part.Palette.Length;
                _swatches[i].gameObject.SetActive(has);
                if (has) _swatches[i].image.color = part.Palette[i];
            }
            for (var i = 0; i < _slots.Length; i++) _slots[i].image.color = i == slot ? _selectedTint : _idleTint;
            SyncSliders();
        }

        private void PickHsv() => SetColor(Color.HSVToRGB(_hue.value, _saturation.value, _brightness.value), false);

        private void SetColor(Color color, bool syncSliders)
        {
            _colors[_selected] = color;
            _chips[_selected].color = color;
            if (syncSliders) SyncSliders();
            ShowOnPreview();
        }

        private void SyncSliders()
        {
            Color.RGBToHSV(_colors[_selected], out var h, out var s, out var v);
            _hue.SetValueWithoutNotify(h);
            _saturation.SetValueWithoutNotify(s);
            _brightness.SetValueWithoutNotify(v);
        }

        // A random swatch per part: palettes keep the result tasteful.
        private void Randomize()
        {
            var colors = new Color32[_colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var part = Catalog.Colors[i];
                colors[i] = part.Palette != null && part.Palette.Length > 0 ? part.Palette[Random.Range(0, part.Palette.Length)] : part.Default;
            }
            SetAll(colors);
        }

        private void SetAll(Color32[] colors)
        {
            _colors = colors;
            RefreshChips();
            SyncSliders();
            ShowOnPreview();
        }

        private void RefreshChips()
        {
            for (var i = 0; i < _chips.Length && i < _colors.Length; i++) _chips[i].color = _colors[i];
        }
    }
}
