namespace Pawchinko
{
    /// <summary>
    /// How long an ability effect persists once resolved. Instant fires once during the round;
    /// Round lasts until end-of-round cleanup; Battle persists until BattleEndedEvent.
    /// </summary>
    public enum PomAbilityDuration
    {
        Instant = 0,
        Round = 1,
        Battle = 2
    }
}
