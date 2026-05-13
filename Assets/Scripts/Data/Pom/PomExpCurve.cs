namespace Pawchinko
{
    /// <summary>
    /// Selects how much EXP a Pom needs to reach the next level. Concrete curve numbers are
    /// TBD; this enum is the authoring seam designers pick from.
    /// </summary>
    public enum PomExpCurve
    {
        Slow = 0,
        Medium = 1,
        Fast = 2
    }
}
