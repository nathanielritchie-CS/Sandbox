using System.Collections;
using Isekai.Core;
using UnityEngine;

namespace Isekai.Net
{
    [RequireComponent(typeof(NetworkHealth))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private Color _healthyColor = Color.white;

        private NetworkHealth _health;

        private void Awake() => _health = GetComponent<NetworkHealth>();
        private void OnEnable() => _health.Changed += Show;
        private void OnDisable() => _health.Changed -= Show;

        private void Show(int current, int max)
        {
            var alive = current > 0;
            _renderer.enabled = alive;
            _collider.enabled = alive;
            _renderer.material.color = Color.Lerp(Palette.Danger, _healthyColor, (float)current / max);

            if (!alive && _health.IsServer) StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(_respawnDelay);
            _health.ServerRestore();
        }
    }
}
