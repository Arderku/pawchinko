using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Marks a board root and records which <see cref="Side"/> it belongs to. The enemy board is a
    /// prefab variant of the player board, so the shared <see cref="Peg"/> components cannot carry a
    /// side - this root marker provides it. The <c>Build Board Pegs</c> tool adds this to the player
    /// board (Player) and overrides the side to Enemy on the variant.
    ///
    /// Exposes the board's pegs (cached) so <see cref="PegManager"/> can apply per-round peg effects.
    /// </summary>
    public class BattleBoard : MonoBehaviour
    {
        [SerializeField] private Side side;

        public Side Side => side;

        private Peg[] _pegs;

        /// <summary>Pegs under this board, including ones currently hidden (disabled renderer/collider, not inactive GameObjects).</summary>
        public IReadOnlyList<Peg> Pegs
        {
            get
            {
                if (_pegs == null) _pegs = GetComponentsInChildren<Peg>(true);
                return _pegs;
            }
        }

        /// <summary>Editor-only setter used by the board-build tool.</summary>
        public void SetSide(Side value) => side = value;

        /// <summary>Forces the peg cache to rebuild (after layout changes).</summary>
        public void RefreshPegs() => _pegs = null;
    }
}
