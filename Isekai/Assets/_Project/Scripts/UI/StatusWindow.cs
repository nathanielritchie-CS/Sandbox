using System;
using Isekai.Core;
using Isekai.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Isekai.UI
{
    // The Otherworlder's status window: a holographic panel toggled with C at any time. Shows name, level, HP/stamina/EXP
    // and the stats; + asks to spend a stat point (the server decides). Class and title are fixed text until classes exist.
    public class StatusWindow : MonoBehaviour
    {
        [Serializable]
        private class Meter
        {
            public RectTransform Fill;
            public TMP_Text Value;
        }

        [Serializable]
        private class StatRow
        {
            public TMP_Text Name;
            public TMP_Text Value;
            public Button Plus;
        }

        [SerializeField] private StatusEventChannel _status;
        [SerializeField] private HealthEventChannel _health;
        [SerializeField] private HealthEventChannel _stamina;
        [SerializeField] private TextEventChannel _spendPoint;
        [SerializeField] private InputGate _gate;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private TMP_Text _points;
        [SerializeField] private Meter _hp;
        [SerializeField] private Meter _sta;
        [SerializeField] private Meter _exp;
        [SerializeField] private StatRow[] _rows;
        [SerializeField] private AudioSource _openSound; // optional
        [SerializeField] private float _openSeconds = 0.22f;
        [SerializeField] private float _closeSeconds = 0.12f;
        [SerializeField] private InputAction _toggle = new("ToggleStatus", InputActionType.Button, "<Keyboard>/c");

        private PlayerStatus _current;
        private Coroutine _anim;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                var index = i;
                _rows[i].Plus.onClick.AddListener(() =>
                {
                    if (_current != null && index < _current.Stats.Length) _spendPoint.Raise(_current.Stats[index].Name);
                });
            }
        }

        private void OnEnable()
        {
            _toggle.performed += OnToggle;
            _toggle.Enable();
            _status.Raised += Show;
            _health.Raised += ShowHealth;
            _stamina.Raised += ShowStamina;
            Show(_status.Current);
            ShowHealth(_health.Current, _health.Max);
            ShowStamina(_stamina.Current, _stamina.Max);
            _panel.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _toggle.performed -= OnToggle;
            _toggle.Disable();
            _status.Raised -= Show;
            _health.Raised -= ShowHealth;
            _stamina.Raised -= ShowStamina;
            if (IsOpen) _gate.SetWindow(false);
            IsOpen = false;
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            if (!_gate.MenuOpen) SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen) return;
            IsOpen = open;
            _gate.SetWindow(open);
            if (_anim != null) StopCoroutine(_anim);
            if (open)
            {
                _panel.gameObject.SetActive(true);
                if (_openSound != null) _openSound.Play();
                // Hologram switching on: unfolds vertically and flickers before it settles.
                _anim = StartCoroutine(Ease.Tween(_openSeconds, t =>
                {
                    _panel.localScale = new Vector3(1f, Mathf.Lerp(0.04f, 1f, t), 1f);
                    _fade.alpha = t >= 0.6f ? 1f : (Mathf.Repeat(t * 9f, 1f) > 0.35f ? t + 0.3f : 0.25f);
                }, Ease.OutCubic));
            }
            else
            {
                _anim = StartCoroutine(Ease.Tween(_closeSeconds, t =>
                {
                    _panel.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.04f, t), 1f);
                    _fade.alpha = 1f - t;
                    if (t >= 1f) _panel.gameObject.SetActive(false);
                }, Ease.OutCubic));
            }
        }

        private void Show(PlayerStatus status)
        {
            _current = status;
            if (status == null)
            {
                _name.text = "-";
                return;
            }
            _name.text = status.Name;
            _level.text = "LV. " + status.Level;
            _points.text = "POINTS  " + status.UnspentPoints;
            SetMeter(_exp, status.XpIntoLevel, status.XpToNext);
            for (var i = 0; i < _rows.Length; i++)
            {
                var has = i < status.Stats.Length;
                _rows[i].Name.text = has ? status.Stats[i].Name.ToUpperInvariant() : "";
                _rows[i].Value.text = has ? status.Stats[i].Value.ToString() : "";
                _rows[i].Plus.gameObject.SetActive(has && status.UnspentPoints > 0);
            }
        }

        private void ShowHealth(int current, int max) => SetMeter(_hp, current, max);

        private void ShowStamina(int current, int max) => SetMeter(_sta, current, max);

        private static void SetMeter(Meter meter, int current, int max)
        {
            meter.Fill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)current / max) : 0f, 1f);
            meter.Value.text = max > 0 ? current + " / " + max : "-";
        }
    }
}
