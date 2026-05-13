namespace Pawchinko
{
    /// <summary>
    /// Broad category an ability falls into. Drives icon / UI grouping; the actual effect math
    /// is described by PomAbilityEffect + PomAbilityFormula. Placeholder set - extend as new
    /// abilities are designed.
    /// </summary>
    public enum PomAbilityKind
    {
        SelfBuff = 0,
        EnemyDebuff = 1,
        PegBuff = 2,
        PegDebuff = 3,
        BucketModifier = 4,
        BallModifier = 5,
        BoardScramble = 6
    }
}
