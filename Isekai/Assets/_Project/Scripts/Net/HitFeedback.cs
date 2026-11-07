using System.Collections;
using System.Collections.Generic;
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
        private Color _flashColor;
        private MaterialPropertyBlock _block; // created in Awake: Unity forbids it in field initializers
        private readonly List<(Renderer renderer, int slot, bool had, Color color)> _saved = new();
        private int _last;

        private void Awake()
        {
            _health = GetComponent<NetworkHealth>();
            _block = new MaterialPropertyBlock();
            _flashColor = Palette.Sunlight * _flashBrightness;
            _flashColor.a = 1f;
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
                EndFlash(); // a hit during a flash: restore first, or the flash colour would be saved as the original
                StartCoroutine(Flash());
            }
            _last = current;
        }

        private IEnumerator Flash()
        {
            StartFlash();
            yield return new WaitForSeconds(_flashSeconds);
            EndFlash();
        }

        // Per material slot, so a player's chosen colours (also per-slot blocks) come back afterwards.
        // Only the active model's renderers; inactive ones are skipped.
        private void StartFlash()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                for (var i = 0; i < r.sharedMaterials.Length; i++)
                {
                    r.GetPropertyBlock(_block, i);
                    var had = _block.HasColor(BaseColorId);
                    _saved.Add((r, i, had, had ? _block.GetColor(BaseColorId) : default));
                    _block.SetColor(BaseColorId, _flashColor);
                    r.SetPropertyBlock(_block, i);
                }
            }
        }

        private void EndFlash()
        {
            foreach (var (r, slot, had, color) in _saved)
            {
                if (r == null) continue;
                if (had)
                {
                    r.GetPropertyBlock(_block, slot);
                    _block.SetColor(BaseColorId, color);
                    r.SetPropertyBlock(_block, slot);
                }
                else
                {
                    r.SetPropertyBlock(null, slot);
                }
            }
            _saved.Clear();
        }
    }
}
