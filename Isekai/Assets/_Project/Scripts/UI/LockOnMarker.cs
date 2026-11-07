using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    // A dot on the locked-on target's body, on this player's screen only.
    public class LockOnMarker : MonoBehaviour
    {
        [SerializeField] private TransformEventChannel _targetChannel;
        [SerializeField] private TransformEventChannel _cameraChannel;
        [SerializeField] private RectTransform _marker;

        private Camera _camera;

        private void LateUpdate()
        {
            var target = _targetChannel.Current;
            var view = _cameraChannel.Current;
            var show = target != null && view != null;
            if (show)
            {
                if (_camera == null || _camera.transform != view) _camera = view.GetComponent<Camera>();
                var point = target.TryGetComponent(out Collider body) ? body.bounds.center : target.position;
                var screen = _camera.WorldToScreenPoint(point);
                show = screen.z > 0f;
                if (show) _marker.position = screen;
            }
            if (_marker.gameObject.activeSelf != show) _marker.gameObject.SetActive(show);
        }
    }
}
