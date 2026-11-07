using System.Collections.Generic;
using Isekai.Data.Appearance;
using UnityEngine;

namespace Isekai.Gameplay.Appearance
{
    // Shows a look on a character model: each catalog colour on every renderer slot that uses that slot's material.
    public class AppearanceApplier : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private AppearanceCatalog _catalog;

        private MaterialPropertyBlock _block;

        public AppearanceCatalog Catalog => _catalog;

        public void Apply(IReadOnlyList<Color32> colors)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var materials = r.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var slot = _catalog.SlotOf(materials[i]);
                    if (slot < 0 || slot >= colors.Count) continue;
                    r.GetPropertyBlock(_block, i);
                    _block.SetColor(BaseColorId, colors[slot]);
                    r.SetPropertyBlock(_block, i);
                }
            }
        }
    }
}
