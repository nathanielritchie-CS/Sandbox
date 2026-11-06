using System.Collections;
using Isekai.Core;
using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    // The local player's level-up moment: an accent flash and a sting, then a "LEVEL n" banner that pops in and out.
    // Timing follows the game-feel milestone recipe; input stays live (nothing here blocks raycasts).
    public class LevelUpMoment : MonoBehaviour
    {
        [SerializeField] private StatusEventChannel _status;
        [SerializeField] private UnityEngine.UI.Image _flash;
        [SerializeField] private RectTransform _banner;
        [SerializeField] private TMPro.TMP_Text _label;
        [SerializeField] private AudioSource _sting;

        private int _lastLevel;

        private void OnEnable()
        {
            _lastLevel = _status.Current?.Level ?? 0;
            _status.Raised += OnStatus;
        }

        private void OnDisable() => _status.Raised -= OnStatus;

        private void OnStatus(PlayerStatus status)
        {
            var level = status?.Level ?? 0;
            if (_lastLevel > 0 && level > _lastLevel)
            {
                StopAllCoroutines();
                StartCoroutine(Play(level));
            }
            _lastLevel = level;
        }

        private IEnumerator Play(int level)
        {
            _label.text = "LEVEL " + level;
            _sting.Play();
            _flash.gameObject.SetActive(true);
            StartCoroutine(Ease.Tween(0.1f, t => SetFlashAlpha(0.6f * (1f - t)), Ease.OutCubic));
            yield return new WaitForSecondsRealtime(0.05f);
            _banner.gameObject.SetActive(true);
            yield return Ease.Tween(0.35f, t => _banner.localScale = Vector3.one * t, Ease.OutBack);
            yield return new WaitForSecondsRealtime(1.1f);
            yield return Ease.Tween(0.2f, t => _banner.localScale = Vector3.one * (1f - t), Ease.OutCubic);
            _banner.gameObject.SetActive(false);
            _flash.gameObject.SetActive(false);
        }

        private void SetFlashAlpha(float alpha)
        {
            var c = _flash.color;
            c.a = alpha;
            _flash.color = c;
        }
    }
}
