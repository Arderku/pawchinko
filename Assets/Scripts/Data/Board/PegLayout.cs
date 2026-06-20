using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Captured board peg layout used by the ability inspector's visual peg picker. The
    /// <c>Build Board Pegs</c> tool fills <see cref="positions"/> with each peg's normalized
    /// board position (x,y in 0..1, y up) indexed by <see cref="Peg.PegIndex"/>, so designers can
    /// click pegs on a board-shaped grid instead of typing raw indices.
    ///
    /// This is purely editor authoring data; nothing reads it at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Board/Peg Layout", fileName = "PegLayout")]
    public class PegLayout : ScriptableObject
    {
        [Tooltip("Number of pegs on a board.")]
        public int count;

        [Tooltip("Normalized board position per peg index (x right, y up, both 0..1).")]
        public Vector2[] positions = System.Array.Empty<Vector2>();

        /// <summary>Position for a peg index, or (0,0) when out of range.</summary>
        public Vector2 PositionOf(int pegIndex)
        {
            if (positions == null || pegIndex < 0 || pegIndex >= positions.Length) return Vector2.zero;
            return positions[pegIndex];
        }
    }
}
