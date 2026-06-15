using TMPro;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// 3D label that displays a bucket's score multiplier ("1×", "3×", "5×"...). The text
    /// is recomputed whenever <see cref="multiplier"/> is set in code or changed in the
    /// Inspector (via <see cref="OnValidate"/>), so authors get live editor preview.
    ///
    /// Pure view: owns no scoring logic. The actual scoring multiplier comes from
    /// <see cref="BoardScoringConfig"/> on <see cref="ScoringManager"/>; the editor menu
    /// <c>Pawchinko/Apply Scoring Labels</c> copies those values onto each slot's label.
    /// Re-run the menu if you change the scoring config.
    /// </summary>
    [ExecuteAlways]
    public class SlotMultiplierLabel : MonoBehaviour
    {
        [Tooltip("3D TextMeshPro that renders the multiplier glyph.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Bucket score multiplier (set from BoardScoringConfig at build time).")]
        [SerializeField, Min(0)] private int multiplier;
        [Tooltip("Format string. {0} is replaced by the multiplier value.")]
        [SerializeField] private string format = "{0}×";

        public int Multiplier
        {
            get => multiplier;
            set
            {
                multiplier = Mathf.Max(0, value);
                ApplyText();
            }
        }

        private void OnEnable() => ApplyText();

#if UNITY_EDITOR
        private void OnValidate() => ApplyText();
#endif

        private void ApplyText()
        {
            if (label == null) return;
            label.text = string.Format(format, multiplier);
        }
    }
}
