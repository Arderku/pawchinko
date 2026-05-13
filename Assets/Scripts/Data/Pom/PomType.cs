namespace Pawchinko
{
    /// <summary>
    /// Identity tag for a Pom. Types describe gameplay personality and board behavior; they are
    /// NOT a rock-paper-scissors damage table (PAWCHINKO_DESIGN_GUIDE Section 10). Counterplay
    /// lives in abilities, not in passive type matchups.
    /// </summary>
    public enum PomType
    {
        Chaos = 0,
        Calm = 1,
        Greedy = 2,
        Trick = 3,
        Lucky = 4,
        Wild = 5
    }
}
