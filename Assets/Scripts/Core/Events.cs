namespace Pawchinko
{
    /// <summary>
    /// Published by overworld content when gameplay should transition into a battle instance.
    /// Payload stays empty until encounter data is designed.
    /// </summary>
    public class EncounterTriggeredEvent
    {
    }

    /// <summary>
    /// Published by SceneFlowManager before the additive battle scene loads.
    /// </summary>
    public class OverworldPausedEvent
    {
    }

    /// <summary>
    /// Published by SceneFlowManager after the battle scene unloads.
    /// </summary>
    public class OverworldResumedEvent
    {
    }

    /// <summary>
    /// Published by BattleManager when a battle begins. Cross-system gameplay broadcast
    /// (EnergyManager seeds team energy, future systems may also react). Not a UI input.
    /// </summary>
    public class BattleStartedEvent
    {
    }

    /// <summary>
    /// Published when a new round begins. Both sides drop simultaneously per round. Active-pet
    /// indices were removed alongside the 1v1 vertical slice and will be reintroduced when the
    /// 6-total / 3-active roster lands.
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
    /// Published by BattleManager when a drop is initiated for the current round (after the
    /// state guard passes, before balls spawn). Carries the expected ball count per side so
    /// ScoringManager knows how many BallSettledEvents to wait for before declaring the round
    /// scored. Cross-system gameplay broadcast; not a UI input.
    /// </summary>
    public class DropRequestedEvent
    {
        public int PlayerBallCount { get; }
        public int EnemyBallCount { get; }

        public DropRequestedEvent(int playerBallCount, int enemyBallCount)
        {
            PlayerBallCount = playerBallCount;
            EnemyBallCount = enemyBallCount;
        }
    }

    /// <summary>
    /// Published when a ball physically settles in a slot trigger. SourcePom is the active Pom
    /// instance that spawned this ball - scoring uses it to apply per-Pom Power (and later
    /// stat-driven modifiers). May be null only if the ball was spawned without a Pom (debug
    /// paths).
    /// </summary>
    public class BallSettledEvent
    {
        public int BallId { get; }
        public Side Side { get; }
        public int SlotIndex { get; }
        public PomInstance SourcePom { get; }

        public BallSettledEvent(int ballId, Side side, int slotIndex, PomInstance sourcePom)
        {
            BallId = ballId;
            Side = side;
            SlotIndex = slotIndex;
            SourcePom = sourcePom;
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
