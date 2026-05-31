using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// One Pom card in the Battle HUD (Battle Zone or Bench Zone). Displays name, level, type,
    /// and a short info line, plus a focus outline used by the keyboard / gamepad navigator.
    /// Pure view: never mutates Pom data and never knows about input.
    ///
    /// The portrait is a UGUI <see cref="RawImage"/> whose texture is the
    /// <see cref="RenderTexture"/> owned by the matching <see cref="PomPortraitSlot"/> on the
    /// <see cref="PomPortraitStage"/>. This view only toggles the image's visibility; the
    /// stage instantiates / destroys the actual 3D model.
    /// </summary>
    public class BattlePomCardView : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text infoText;

        [Header("Portrait")]
        [Tooltip("RawImage that displays this card's live 3D portrait. Texture is wired at build time to the PomPortraitSlot's RenderTexture.")]
        [SerializeField] private RawImage portraitImage;

        [Header("State Decorators")]
        [SerializeField] private GameObject focusOutline;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;

        public bool IsEmpty { get; private set; } = true;

        /// <summary>Fills the card with the given Pom instance. Pass null to clear.</summary>
        public void Bind(PomInstance instance)
        {
            if (instance == null || instance.data == null)
            {
                Clear();
                return;
            }

            IsEmpty = false;
            if (emptyState != null) emptyState.SetActive(false);
            if (filledState != null) filledState.SetActive(true);
            if (portraitImage != null) portraitImage.enabled = true;

            if (nameText != null) nameText.text = instance.data.DisplayName;
            if (levelText != null) levelText.text = $"LV {instance.level}";
            if (typeText != null)
            {
                typeText.text = instance.data.HasSecondaryType
                    ? $"{instance.data.PrimaryType}/{instance.data.SecondaryType}"
                    : instance.data.PrimaryType.ToString();
            }
            if (infoText != null)
            {
                int balls = PomBallCount.GetCurrentBallCount(instance);
                infoText.text = $"BALLS x{balls}";
            }
        }

        /// <summary>Marks the card empty (no Pom in this slot).</summary>
        public void Clear()
        {
            IsEmpty = true;
            if (emptyState != null) emptyState.SetActive(true);
            if (filledState != null) filledState.SetActive(false);
            if (portraitImage != null) portraitImage.enabled = false;
            if (nameText != null) nameText.text = "--";
            if (levelText != null) levelText.text = string.Empty;
            if (typeText != null) typeText.text = string.Empty;
            if (infoText != null) infoText.text = string.Empty;
        }

        /// <summary>Toggles the focus outline (purple border in the mockup).</summary>
        public void SetFocused(bool focused)
        {
            if (focusOutline != null) focusOutline.SetActive(focused);
        }
    }
}
