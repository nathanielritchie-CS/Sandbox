using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Gameplay.Appearance
{
    // The soul before it has a body: every material swapped for a faint glow until Materialize.
    public class AstralForm : MonoBehaviour
    {
        [SerializeField] private Material _astral;
        [SerializeField] private ParticleSystem _burst; // optional

        private readonly List<(Renderer renderer, Material[] materials)> _bodies = new();

        public bool IsAstral { get; private set; }

        private void Awake()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var original = r.sharedMaterials;
                _bodies.Add((r, original));
                var glow = new Material[original.Length];
                for (var i = 0; i < glow.Length; i++) glow[i] = _astral;
                r.sharedMaterials = glow;
            }
            IsAstral = true;
        }

        public void Materialize()
        {
            foreach (var (r, materials) in _bodies) r.sharedMaterials = materials;
            if (_burst != null) _burst.Play();
            IsAstral = false;
        }
    }
}
