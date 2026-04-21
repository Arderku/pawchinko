namespace Pawchinko
{
    /// <summary>
    /// Published when the player presses START to begin the battle.
    /// </summary>
    public class BattleStartedEvent
    {
    }

    /// <summary>
    /// Published when a new round begins. Both sides drop simultaneously per round, so there is
    /// no "active side" - the round number is enough to drive UI / scoring.
    /// </summary>
    public class RoundStartedEvent
    {
        public int RoundNumber { get; }

        public RoundStartedEvent(int roundNumber)
        {
            RoundNumber = roundNumber;
        }
    }

    /// <summary>
    /// Published by the UI when the player clicks DROP. Triggers a simultaneous drop on both sides.
    /// </summary>
    public class DropRequestedEvent
    {
    }

    /// <summary>
    /// Published when a ball physically settles in a slot trigger.
    /// </summary>
    public class BallSettledEvent
    {
        public int BallId { get; }
        public Side Side { get; }
        public int SlotIndex { get; }

        public BallSettledEvent(int ballId, Side side, int slotIndex)
        {
            BallId = ballId;
            Side = side;
            SlotIndex = slotIndex;
        }
    }
}
