using UnityEngine;
using UnityEngine.EventSystems;

namespace Isekai.UI
{
    // Drag on the preview to turn your new self; scroll to come closer or step back.
    public class PreviewTurntable : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [SerializeField] private Transform _model;
        [SerializeField] private Transform _camera;
        [SerializeField] private float _degreesPerPixel = 0.35f;
        [SerializeField] private float _zoomStep = 0.35f;
        [SerializeField] private float _nearest = 1.6f;
        [SerializeField] private float _farthest = 4.6f;

        public void OnDrag(PointerEventData eventData) => _model.Rotate(0f, -eventData.delta.x * _degreesPerPixel, 0f, Space.World);

        public void OnScroll(PointerEventData eventData)
        {
            if (Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;
            var forward = _camera.forward;
            var depth = Vector3.Dot(_model.position - _camera.position, forward);
            var target = Mathf.Clamp(depth - Mathf.Sign(eventData.scrollDelta.y) * _zoomStep, _nearest, _farthest);
            _camera.position += forward * (depth - target);
        }
    }
}
