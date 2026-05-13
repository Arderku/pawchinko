using System;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// One inclusive level band in a Pom's ball-count scale. A Pom with level inside
    /// [minLevel, maxLevel] contributes <see cref="count"/> balls per drop.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomBallCountBand
    {
        public int minLevel;
        public int maxLevel;
        public int count;

        public PomBallCountBand() { }

        public PomBallCountBand(int minLevel, int maxLevel, int count)
        {
            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
            this.count = count;
        }

        /// <summary>
        /// Returns true if the given level falls inside this band (inclusive on both ends).
        /// </summary>
        public bool Contains(int level)
        {
            return level >= minLevel && level <= maxLevel;
        }
    }
}
