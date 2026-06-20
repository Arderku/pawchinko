using TMPro;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Small flyout that lists the focused active Pom's ability slots (NONE / ABILITY 1 /
    /// ABILITY 2) and highlights which one is currently selected. The HUD owns selection
    /// state; this view just renders.
    /// </summary>
    public class AbilityPickerView : MonoBehaviour
    {
        public const int NoneIndex = 0;
        public const int Slot1Index = 1;
        public const int Slot2Index = 2;
        public const int OptionCount = 3;

        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("AP Readout (optional)")]
        [Tooltip("Optional label that shows the focused Pom's current/max Action Points. Safe to leave unassigned.")]
        [SerializeField] private TMP_Text apLabel;

        [Header("Option Labels (length 3: None, Slot1, Slot2)")]
        [SerializeField] private TMP_Text noneLabel;
        [SerializeField] private TMP_Text slot1Label;
        [SerializeField] private TMP_Text slot2Label;

        [Header("Option Highlights (length 3: None, Slot1, Slot2)")]
        [SerializeField] private GameObject noneHighlight;
        [SerializeField] private GameObject slot1Highlight;
        [SerializeField] private GameObject slot2Highlight;

        /// <summary>Shows the picker and refreshes labels from the focused Pom.</summary>
        public void Show(PomInstance focused, int selectedIndex)
        {
            if (root != null) root.SetActive(true);
            Refresh(focused, selectedIndex);
        }

        /// <summary>Hides the picker.</summary>
        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>Refreshes labels + highlight without toggling visibility.</summary>
        public void Refresh(PomInstance focused, int selectedIndex)
        {
            int currentAP = focused != null ? focused.currentAP : 0;
            int maxAP = focused != null ? focused.maxAP : 0;

            if (apLabel != null) apLabel.text = $"AP {currentAP}/{maxAP}";
            if (noneLabel != null) noneLabel.text = "NONE";

            if (slot1Label != null) slot1Label.text = ResolveLabel(focused, 0, currentAP);
            if (slot2Label != null) slot2Label.text = ResolveLabel(focused, 1, currentAP);

            int clamped = Mathf.Clamp(selectedIndex, 0, OptionCount - 1);
            if (noneHighlight != null) noneHighlight.SetActive(clamped == NoneIndex);
            if (slot1Highlight != null) slot1Highlight.SetActive(clamped == Slot1Index);
            if (slot2Highlight != null) slot2Highlight.SetActive(clamped == Slot2Index);
        }

        private static string ResolveLabel(PomInstance focused, int learnedSlot, int currentAP)
        {
            if (focused == null || focused.learnedAbilities == null) return "(empty)";
            if (learnedSlot < 0 || learnedSlot >= focused.learnedAbilities.Length) return "(empty)";
            var ability = focused.learnedAbilities[learnedSlot];
            if (ability == null) return "(empty)";

            string label = $"{ability.DisplayName}  ({ability.ApCost} AP)";
            // Flag picks the Pom cannot currently afford; the HUD also rejects locking them.
            if (ability.ApCost > currentAP) label += "  - LOW AP";
            return label;
        }
    }
}
