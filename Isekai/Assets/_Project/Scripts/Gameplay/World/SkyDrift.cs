using UnityEngine;

namespace Isekai.Gameplay.World
{
    // Turns the sky slowly. Works on a copy of the material, so the asset itself is never changed while playing.
    public class SkyDrift : MonoBehaviour
    {
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");

        [SerializeField] private Material _sky;
        [SerializeField] private float _degreesPerSecond = 0.8f;

        private Material _copy;

        private void Start()
        {
            _copy = new Material(_sky);
            RenderSettings.skybox = _copy;
        }

        private void Update() => _copy.SetFloat(RotationId, Mathf.Repeat(Time.time * _degreesPerSecond, 360f));

        private void OnDestroy() => Destroy(_copy);
    }
}
