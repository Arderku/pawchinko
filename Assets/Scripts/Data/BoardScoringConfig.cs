using System;

namespace Pawchinko
{
    /// <summary>
    /// Placeholder per-slot score values, indexed left-to-right (0..N-1). Will be replaced once
    /// canonical board layouts exist (PAWCHINKO_DESIGN_GUIDE Section 12 - currently TBD).
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public class BoardScoringConfig
    {
        public int[] slotValues = new[] { 1, 3, 5, 3, 1 };
    }
}
