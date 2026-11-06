using Isekai.Core.Events;
using Unity.Cinemachine;
using UnityEngine;

namespace Isekai.Gameplay.Cameras
{
    public class CameraTargetBinder : MonoBehaviour
    {
        [SerializeField] private TransformEventChannel _localPlayerChannel;
        [SerializeField] private TransformEventChannel _cameraChannel;
        [SerializeField] private CinemachineCamera _followCamera;
        [SerializeField] private Transform _mainCamera;

        private void OnEnable()
        {
            _cameraChannel.Raise(_mainCamera);
            _localPlayerChannel.Raised += Bind;
            Bind(_localPlayerChannel.Current);
        }

        private void OnDisable()
        {
            _localPlayerChannel.Raised -= Bind;
            _cameraChannel.Raise(null);
        }

        private void Bind(Transform target) => _followCamera.Follow = target;
    }
}
