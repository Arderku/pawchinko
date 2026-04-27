namespace Pawchinko
{
    /// <summary>
    /// Published when the player presses START to begin the battle.
    /// </summary>
    public class BattleStartedEvent
    {
    }

    /// <summary>
    /// Published when a new round begins. Both sides drop simultaneously per round; per-side
    /// active-pet indices (0..4) drive the roster active-indicator + active-card text.
    /// </summary>
    public class RoundStartedEvent
    {
        public int RoundNumber { get; }
        public int PlayerActivePetIndex { get; }
        public int EnemyActivePetIndex { get; }

        public RoundStartedEvent(int roundNumber, int playerActivePetIndex, int enemyActivePetIndex)
        {
            RoundNumber = roundNumber;
            PlayerActivePetIndex = playerActivePetIndex;
            EnemyActivePetIndex = enemyActivePetIndex;
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

    /// <summary>
    /// Published once both sides have settled their ball(s) for the current round and the
    /// per-side scores have been tallied. Drives the energy update + round advance.
    /// </summary>
    public class RoundScoredEvent
    {
        public int RoundNumber { get; }
        public int PlayerScore { get; }
        public int EnemyScore { get; }

        public RoundScoredEvent(int roundNumber, int playerScore, int enemyScore)
        {
            RoundNumber = roundNumber;
            PlayerScore = playerScore;
            EnemyScore = enemyScore;
        }
    }

    /// <summary>
    /// Published whenever team-summed energy changes (battle start seed + every round delta).
    /// </summary>
    public class EnergyChangedEvent
    {
        public int PlayerEnergy { get; }
        public int EnemyEnergy { get; }

        public EnergyChangedEvent(int playerEnergy, int enemyEnergy)
        {
            PlayerEnergy = playerEnergy;
            EnemyEnergy = enemyEnergy;
        }
    }

    /// <summary>
    /// Published when one side's energy reaches 0 or below. Carries the winning side.
    /// </summary>
    public class BattleEndedEvent
    {
        public Side Winner { get; }

        public BattleEndedEvent(Side winner)
        {
            Winner = winner;
        }
    }
}
