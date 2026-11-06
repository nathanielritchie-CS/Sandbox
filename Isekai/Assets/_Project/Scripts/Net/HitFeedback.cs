using System.Collections;
using Isekai.Core;
using UnityEngine;

namespace Isekai.Net
{
    // Presentation only, on every peer: a warm flash, a spark burst and a hit sound when this character's replicated health drops.
    [RequireComponent(typeof(NetworkHealth))]
    public class HitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private int _hitSparks = 14;
        [SerializeField] private int _deathSparks = 32;
        [SerializeField] private float _flashSeconds = 0.08f;
        [SerializeField] private float _flashBrightness = 2.5f;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _deathClips;  // layered on top of the hit

        private NetworkHealth _health;
        private MaterialPropertyBlock _flash;
        private int _last;

        private void Awake()
        {
            _health = GetComponent<NetworkHealth>();
            var color = Palette.Sunlight * _flashBrightness;
            color.a = 1f;
            _flash = new MaterialPropertyBlock();
            _flash.SetColor(BaseColorId, color);
        }

        private void OnEnable()
        {
            _last = _health.Current;
            _health.Changed += OnChanged;
        }

        private void OnDisable() => _health.Changed -= OnChanged;

        private void OnChanged(int current, int max)
        {
            if (current < _last)
            {
                _sparks.Emit(current > 0 ? _hitSparks : _deathSparks);
                Sfx.PlayRandom(_audio, _hitClips);
                if (current <= 0) Sfx.PlayRandom(_audio, _deathClips);
                StopAllCoroutines();
                StartCoroutine(Flash());
            }
            _last = current;
        }

        private IEnumerator Flash()
        {
            SetBlock(_flash);
            yield return new WaitForSeconds(_flashSeconds);
            SetBlock(null);
        }

        // Only the active model's renderers; inactive character variants are skipped.
        private void SetBlock(MaterialPropertyBlock block)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (!(r is ParticleSystemRenderer)) r.SetPropertyBlock(block);
        }
    }
}
