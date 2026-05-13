using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Static ball-spawning data for a Pom species: which ball type spawns, how it spawns,
    /// and how many balls drop per round at each level band. Lives on PomDefinition; runtime
    /// Pom instances look up their current ball count via GetBallCountForLevel.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomBallProfile
    {
        public string ballType = string.Empty;
        public string spawnPattern = string.Empty;
        public int baseBallCount = 1;
        public List<PomBallCountBand> ballCountScale = new();

        /// <summary>
        /// Returns the ball count for the given level, picked from the first matching band.
        /// Falls back to <see cref="baseBallCount"/> if no band covers the level or the scale
        /// is empty.
        /// </summary>
        public int GetBallCountForLevel(int level)
        {
            if (ballCountScale == null || ballCountScale.Count == 0) return baseBallCount;
            for (int i = 0; i < ballCountScale.Count; i++)
            {
                var band = ballCountScale[i];
                if (band != null && band.Contains(level)) return band.count;
            }
            return baseBallCount;
        }
    }
}
