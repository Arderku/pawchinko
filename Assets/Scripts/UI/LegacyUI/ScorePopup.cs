using TMPro;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Animated "+N" score popup. Spawned by <see cref="BattleHud"/> when a BallScoredEvent
    /// fires; tweens from the ball's world position (projected onto the HUD canvas) toward
    /// the tug-of-war bar, briefly scaling up at the start for impact and fading out as it
    /// arrives. Auto-destroys when the trip is complete. Stateless after construction - the
    /// owning HUD calls <see cref="Begin"/> once and forgets it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Header("Timing (seconds)")]
        [SerializeField] private float popDuration = 0.18f;
        [SerializeField] private float flyDuration = 0.55f;

        [Header("Visual")]
        [SerializeField] private float popStartScale = 0.5f;
        [SerializeField] private float popPeakScale = 1.35f;
        [SerializeField] private float endScale = 0.9f;
        [SerializeField] private float arcHeight = 90f;

        private RectTransform _rt;
        private CanvasGroup _cg;
        private Vector2 _start;
        private Vector2 _target;
        private float _elapsed;
        private bool _flying;
        private float _totalDuration;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cg = GetComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
        }

        public void Begin(string text, Color color, Vector2 anchoredStart, Vector2 anchoredTarget)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_cg == null) _cg = GetComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }
            _start = anchoredStart;
            _target = anchoredTarget;
            _rt.anchoredPosition = _start;
            _rt.localScale = Vector3.one * popStartScale;
            _cg.alpha = 1f;
            _elapsed = 0f;
            _flying = false;
            _totalDuration = popDuration + flyDuration;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _totalDuration)
            {
                Destroy(gameObject);
                return;
            }

            if (_elapsed < popDuration)
            {
                float t = _elapsed / popDuration;
                float s = Mathf.Lerp(popStartScale, popPeakScale, t);
                _rt.localScale = Vector3.one * s;
                _cg.alpha = 1f;
                return;
            }

            // Fly phase: ease-in-cubic for the position so the label accelerates into the bar.
            if (!_flying)
            {
                _flying = true;
                _rt.localScale = Vector3.one * popPeakScale;
            }
            float ft = (_elapsed - popDuration) / flyDuration;
            float ease = ft * ft * ft;
            Vector2 pos = Vector2.Lerp(_start, _target, ease);
            // Add a small arc so the popup curves into the bar instead of cutting straight down.
            float arc = Mathf.Sin(ft * Mathf.PI) * arcHeight;
            pos.y += arc;
            _rt.anchoredPosition = pos;
            _rt.localScale = Vector3.one * Mathf.Lerp(popPeakScale, endScale, ft);
            _cg.alpha = 1f - ft * ft;
        }
    }
}
