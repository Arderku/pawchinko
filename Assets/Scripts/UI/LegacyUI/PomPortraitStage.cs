using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Off-screen stage owning the per-card <see cref="PomPortraitSlot"/>s (5 player + 5 enemy)
    /// that render live 3D Pom portraits for the Battle HUD. Pure view: the
    /// <see cref="BattleHud"/> calls <see cref="BindPlayerSide"/> / <see cref="BindEnemySide"/>
    /// whenever a roster is (re)bound and the stage maps each roster index to the matching slot.
    /// Roster indices 0..2 are the Battle Zone, 3..4 are the Bench Zone (same as the card list).
    /// </summary>
    public class PomPortraitStage : MonoBehaviour
    {
        [Header("Slots (length BattleManager.MaxRosterPoms = 5 per side)")]
        [SerializeField] private List<PomPortraitSlot> playerSlots = new();
        [SerializeField] private List<PomPortraitSlot> enemySlots = new();

        /// <summary>Binds the player roster to the player-side portrait slots.</summary>
        public void BindPlayerSide(IReadOnlyList<PomInstance> roster) => BindSide(playerSlots, roster);

        /// <summary>Binds the enemy roster to the enemy-side portrait slots.</summary>
        public void BindEnemySide(IReadOnlyList<PomInstance> roster) => BindSide(enemySlots, roster);

        /// <summary>Clears all portraits on both sides.</summary>
        public void ClearAll()
        {
            ClearSide(playerSlots);
            ClearSide(enemySlots);
        }

        private static void BindSide(List<PomPortraitSlot> slots, IReadOnlyList<PomInstance> roster)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (roster != null && i < roster.Count) slot.SetPom(roster[i]);
                else slot.Clear();
            }
        }

        private static void ClearSide(List<PomPortraitSlot> slots)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++) slots[i]?.Clear();
        }
    }
}
