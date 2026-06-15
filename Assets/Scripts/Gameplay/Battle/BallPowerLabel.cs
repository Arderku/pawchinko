using TMPro;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// 3D label that floats on a ball and shows the source Pom's Power as a multiplier
    /// ("2×", "1.5×", ...). <see cref="Ball.Init"/> calls <see cref="SetPower"/> once when
    /// the ball spawns; after that the value is fixed for the lifetime of the ball.
    ///
    /// When the source Pom's power is at or below <see cref="hideBelow"/> the label hides
    /// itself, so the common 1× case doesn't clutter the board with noise.
    /// </summary>
    public class BallPowerLabel : MonoBehaviour
    {
        [Tooltip("3D TextMeshPro that renders the multiplier glyph.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Format string. {0} is replaced by the power value (one decimal of precision).")]
        [SerializeField] private string format = "{0:0.##}×";
        [Tooltip("Hide the label if power < this threshold. Default 1.0001 hides plain 1× balls.")]
        [SerializeField] private float hideBelow = 1.0001f;

        /// <summary>
        /// Sets the power readout. Power &lt; <see cref="hideBelow"/> hides the label.
        /// Idempotent and safe to call from <see cref="Ball.Init"/>.
        /// </summary>
        public void SetPower(float power)
        {
            if (label == null) return;
            if (power < hideBelow)
            {
                label.gameObject.SetActive(false);
                return;
            }
            label.gameObject.SetActive(true);
            label.text = string.Format(format, power);
        }
    }
}
