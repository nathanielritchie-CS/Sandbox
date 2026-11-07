using Isekai.Core.Events;
using Unity.Cinemachine;
using UnityEngine;

namespace Isekai.Gameplay.Cameras
{
    // While locked on, a dedicated camera takes over: it follows a pivot that sits on the local player and faces the target
    // (so the camera stays behind the player, over the shoulder) and aims at a point between the two, so the enemy is near
    // the middle of the screen and the character stays in view. Mouse look is off meanwhile.
    public class LockOnCamera : MonoBehaviour
    {
        [SerializeField] private TransformEventChannel _localPlayerChannel;
        [SerializeField] private TransformEventChannel _targetChannel;
        [SerializeField] private CinemachineCamera _lockCamera; // Follow = _pivot, LookAt = _focus
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _focus;
        [SerializeField] private CinemachineOrbitalFollow _orbit; // the normal camera, kept facing the same way for the blend back
        [SerializeField] private CinemachineInputAxisController _look;
        [SerializeField] private float _chestHeight = 1.3f;
        [SerializeField, Range(0f, 1f)] private float _towardTarget = 0.6f; // where the camera aims, from your chest to the target

        private void OnDisable()
        {
            _look.enabled = true;
            _lockCamera.enabled = false;
        }

        private void LateUpdate()
        {
            var player = _localPlayerChannel.Current;
            var target = _targetChannel.Current;
            var locked = player != null && target != null;
            if (_look.enabled == locked) _look.enabled = !locked;
            if (_lockCamera.enabled != locked) _lockCamera.enabled = locked;
            if (!locked) return;

            var chest = player.position + Vector3.up * _chestHeight;
            var targetPoint = target.TryGetComponent(out Collider body) ? body.bounds.center : target.position;
            var toTarget = targetPoint - chest;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.25f) // right on top of it: keep the last facing
            {
                var facing = Quaternion.LookRotation(toTarget);
                _pivot.SetPositionAndRotation(player.position, facing);
                _orbit.HorizontalAxis.Value = Mathf.Repeat(facing.eulerAngles.y + 180f, 360f) - 180f; // the axis wraps at ±180
            }
            else
            {
                _pivot.position = player.position;
            }
            _focus.position = Vector3.Lerp(chest, targetPoint, _towardTarget);
        }
    }
}
