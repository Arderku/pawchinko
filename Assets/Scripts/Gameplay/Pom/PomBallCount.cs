namespace Pawchinko
{
    /// <summary>
    /// Ball-count math: how many balls a Pom contributes per drop given its level. Reads from
    /// the level-band scale authored on <see cref="PomData"/>.
    /// </summary>
    public static class PomBallCount
    {
        /// <summary>
        /// Returns the ball count this <see cref="PomData"/> contributes at the given level,
        /// from the first matching band in <see cref="PomData.BallCountLevelBands"/>; falls
        /// back to <see cref="PomData.BaseBallCount"/> when no band covers the level.
        /// </summary>
        public static int GetBallCountForLevel(PomData data, int level)
        {
            if (data == null) return 0;
            var bands = data.BallCountLevelBands;
            if (bands != null)
            {
                for (int i = 0; i < bands.Count; i++)
                {
                    var band = bands[i];
                    if (band != null && level >= band.minLevel && level <= band.maxLevel) return band.count;
                }
            }
            return data.BaseBallCount;
        }

        /// <summary>Ball count the runtime instance contributes at its current level.</summary>
        public static int GetCurrentBallCount(PomInstance instance)
        {
            return instance != null ? GetBallCountForLevel(instance.data, instance.level) : 0;
        }
    }
}
