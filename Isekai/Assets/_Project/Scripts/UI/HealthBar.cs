using Isekai.Core;
using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    // A local player meter (HP, stamina): a bar and an optional line of HP text. Hidden while there is no local player.
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private HealthEventChannel _channel;
        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMPro.TMP_Text _label;
        [SerializeField] private float _fillSeconds = 0.3f;

        private Coroutine _tween;

        private void OnEnable()
        {
            _channel.Raised += Show;
            Show(_channel.Current, _channel.Max);
        }

        private void OnDisable() => _channel.Raised -= Show;

        private void Show(int current, int max)
        {
            _panel.SetActive(max > 0);
            if (max <= 0) return;
            if (_label != null) _label.text = current > 0 ? "HP " + current + " / " + max : "You died. Respawning...";
            var from = _fill.anchorMax.x;
            var to = Mathf.Clamp01((float)current / max);
            if (_tween != null) StopCoroutine(_tween);
            _tween = StartCoroutine(Ease.Tween(_fillSeconds, t => _fill.anchorMax = new Vector2(Mathf.Lerp(from, to, t), 1f), Ease.OutCubic));
        }
    }
}
