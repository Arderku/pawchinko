using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// Tug-of-war energy bar. The bar is a fixed-width track; the divider between the player
    /// fill (left, blue) and enemy fill (right, red) slides toward whichever side has more
    /// remaining energy. Internally we keep "displayed" energies that smoothly chase the
    /// target values set via <see cref="SetEnergies"/>, so the bar visibly pushes one way or
    /// the other when scoring lands.
    ///
    /// PlayerEnergy / EnemyEnergy come from <see cref="EnergyManager"/>. Max values are also
    /// pushed in here so the bar knows the total span (PlayerMax + EnemyMax) - the divider
    /// starts at PlayerMax / Total and slides as energies deplete.
    /// </summary>
    [ExecuteAlways]
    public class TugOfWarBar : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("RectTransform that owns the full bar width. Player + Enemy fills are sized as fractions of this rect.")]
        [SerializeField] private RectTransform track;
        [Tooltip("Image whose width represents the player's remaining share (left side, anchored to left edge).")]
        [SerializeField] private Image playerFill;
        [Tooltip("Image whose width represents the enemy's remaining share (right side, anchored to right edge).")]
        [SerializeField] private Image enemyFill;
        [Tooltip("Optional decorative marker that parks where the two fills meet (the 'rope').")]
        [SerializeField] private RectTransform marker;
        [Tooltip("Optional numeric label rendered on the player side.")]
        [SerializeField] private TMP_Text playerLabel;
        [Tooltip("Optional numeric label rendered on the enemy side.")]
        [SerializeField] private TMP_Text enemyLabel;

        [Header("Animation")]
        [Tooltip("Energy units per second that the displayed value chases the real value.")]
        [SerializeField] private float chaseSpeed = 80f;

        private float _targetPlayer;
        private float _targetEnemy;
        private float _displayPlayer;
        private float _displayEnemy;

        /// <summary>
        /// Sets the maximums for each side (pool sizes). Called once at battle start; the
        /// bar uses these to seed the initial energies and to label the player/enemy pools.
        /// </summary>
        public void Configure(int playerMax, int enemyMax)
        {
            _targetPlayer = Mathf.Max(0, playerMax);
            _targetEnemy = Mathf.Max(0, enemyMax);
            _displayPlayer = _targetPlayer;
            _displayEnemy = _targetEnemy;
            ApplyVisuals();
        }

        /// <summary>
        /// Updates the bar's TARGET energies. The displayed bar smoothly chases the target,
        /// so subscribers don't need to re-tween manually.
        /// </summary>
        public void SetEnergies(int playerEnergy, int enemyEnergy)
        {
            _targetPlayer = Mathf.Max(0, playerEnergy);
            _targetEnemy = Mathf.Max(0, enemyEnergy);
        }

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0f;
            _displayPlayer = Mathf.MoveTowards(_displayPlayer, _targetPlayer, chaseSpeed * dt);
            _displayEnemy = Mathf.MoveTowards(_displayEnemy, _targetEnemy, chaseSpeed * dt);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (track == null) return;
            float w = track.rect.width;

            // Tug-of-war divider position: the rope marker lands where the two pools meet,
            // which is the player's share of the *remaining* energy. The bar is always 100%
            // painted - we never show a gap; only the divider slides. When player wins a
            // round, enemy energy drops, divider slides right (player has more rope). Game
            // ends when divider hits an extreme (one side at 0).
            float remaining = _displayPlayer + _displayEnemy;
            float pFrac = remaining > 0.0001f ? _displayPlayer / remaining : 0.5f;
            float eFrac = 1f - pFrac;

            // Anchors are authored in the BuildBattleHud editor pass (player anchored bottom-
            // left vertical-stretch, enemy anchored bottom-right vertical-stretch, marker on
            // (0,0.5)). We only touch width/X so the vertical stretch is preserved.
            if (playerFill != null)
            {
                var rt = playerFill.rectTransform;
                Vector2 sz = rt.sizeDelta;
                sz.x = w * pFrac;
                rt.sizeDelta = sz;
            }
            if (enemyFill != null)
            {
                var rt = enemyFill.rectTransform;
                Vector2 sz = rt.sizeDelta;
                sz.x = w * eFrac;
                rt.sizeDelta = sz;
            }
            if (marker != null)
            {
                Vector2 p = marker.anchoredPosition;
                p.x = w * pFrac;
                marker.anchoredPosition = p;
            }
            if (playerLabel != null) playerLabel.text = Mathf.RoundToInt(_displayPlayer).ToString();
            if (enemyLabel != null) enemyLabel.text = Mathf.RoundToInt(_displayEnemy).ToString();
        }

        /// <summary>
        /// World-space anchor used by score popups as their fly-to destination. Returns the
        /// center of the track in screen space (popups then convert per-camera). Player popups
        /// should aim at the LEFT half, enemy popups at the RIGHT half - see <see cref="GetSideAnchorWorld"/>.
        /// </summary>
        public Vector3 GetSideAnchorWorld(Side side, Camera uiCamera)
        {
            if (track == null) return transform.position;
            Vector3[] corners = new Vector3[4];
            track.GetWorldCorners(corners);
            // corners: 0=BL, 1=TL, 2=TR, 3=BR
            Vector3 leftMid = (corners[0] + corners[1]) * 0.5f;
            Vector3 rightMid = (corners[2] + corners[3]) * 0.5f;
            return side == Side.Player ? Vector3.Lerp(leftMid, rightMid, 0.25f) : Vector3.Lerp(leftMid, rightMid, 0.75f);
        }
    }
}
