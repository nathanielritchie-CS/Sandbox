using System.Collections;
using Isekai.Core;
using Isekai.Core.Events;
using TMPro;
using UnityEngine;

namespace Isekai.Net
{
    // Presentation only, on every peer: the amount pops up above this character and floats away when its replicated health drops.
    [RequireComponent(typeof(NetworkHealth))]
    public class DamageNumbers : MonoBehaviour
    {
        private const int PoolSize = 4;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Material _material;      // the font's shadowed preset, for contrast over the meadow
        [SerializeField] private TransformEventChannel _camera;
        [SerializeField] private bool _isPlayer;          // players' numbers in Danger red, everyone else's in warm white
        [SerializeField] private float _height = 2.2f;
        [SerializeField] private float _rise = 1f;
        [SerializeField] private float _seconds = 0.8f;

        private readonly TextMeshPro[] _pool = new TextMeshPro[PoolSize];
        private readonly Coroutine[] _running = new Coroutine[PoolSize];
        private NetworkHealth _health;
        private int _next;
        private int _last;

        private void Awake()
        {
            _health = GetComponent<NetworkHealth>();
            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("DamageNumber");
                go.transform.SetParent(transform, false);
                var text = go.AddComponent<TextMeshPro>();
                text.font = _font;
                text.fontSharedMaterial = _material;
                text.fontSize = 5f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.color = _isPlayer ? Palette.Danger : Palette.Text;
                go.SetActive(false);
                _pool[i] = text;
            }
        }

        private void OnEnable()
        {
            _last = _health.Current;
            _health.Changed += OnChanged;
        }

        private void OnDisable() => _health.Changed -= OnChanged;

        private void OnChanged(int current, int max)
        {
            if (current < _last) Show(_last - current);
            _last = current;
        }

        private void Show(int amount)
        {
            var i = _next;
            _next = (_next + 1) % PoolSize;
            if (_running[i] != null) StopCoroutine(_running[i]);
            _running[i] = StartCoroutine(Float(_pool[i], amount));
        }

        private IEnumerator Float(TextMeshPro text, int amount)
        {
            text.text = amount.ToString();
            text.gameObject.SetActive(true);
            var start = new Vector3(Random.Range(-0.3f, 0.3f), _height, 0f);
            for (var t = 0f; t < 1f; t += Time.deltaTime / _seconds)
            {
                text.transform.localPosition = start + Vector3.up * (_rise * Ease.OutCubic(t));
                text.transform.localScale = Vector3.one * Ease.OutBack(Mathf.Min(1f, t * 5f));
                text.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                if (_camera.Current != null) text.transform.rotation = _camera.Current.rotation;
                yield return null;
            }
            text.gameObject.SetActive(false);
        }
    }
}
