namespace Pawchinko
{
    /// <summary>
    /// Published when the player presses START to begin the battle.
    /// </summary>
    public class BattleStartedEvent
    {
    }

    /// <summary>
    /// Published when a new round/turn begins. Identifies the side that should drop next.
    /// </summary>
    public class RoundStartedEvent
    {
        public int RoundNumber { get; }
        public Side ActiveSide { get; }

        public RoundStartedEvent(int roundNumber, Side activeSide)
        {
            RoundNumber = roundNumber;
            ActiveSide = activeSide;
        }
    }

    /// <summary>
    /// Published by the UI when the player clicks a DROP button.
    /// </summary>
    public class DropRequestedEvent
    {
        public Side Side { get; }

        public DropRequestedEvent(Side side)
        {
            Side = side;
        }
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

    /// <summary>
    /// Published once a side's drop has fully resolved (ball settled), signalling the turn flip.
    /// </summary>
    public class TurnEndedEvent
    {
        public Side JustEnded { get; }

        public TurnEndedEvent(Side justEnded)
        {
            JustEnded = justEnded;
        }
    }
}
