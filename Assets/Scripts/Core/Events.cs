using UnityEngine;

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
    /// paths). BallType is the ball's rolled type (for type-gated bucket rules) and Power is the
    /// ball's already-resolved per-ball power (base Pom power x any ability modifiers), so scoring
    /// does not recompute it. ContactPoint is the slot's world position - used by UI systems to
    /// start a score popup animation from where the ball landed.
    /// </summary>
    public class BallSettledEvent
    {
        public int BallId { get; }
        public Side Side { get; }
        public int SlotIndex { get; }
        public PomInstance SourcePom { get; }
        public PomType BallType { get; }
        public float Power { get; }
        public Vector3 ContactPoint { get; }

        public BallSettledEvent(int ballId, Side side, int slotIndex, PomInstance sourcePom, PomType ballType, float power, Vector3 contactPoint)
        {
            BallId = ballId;
            Side = side;
            SlotIndex = slotIndex;
            SourcePom = sourcePom;
            BallType = ballType;
            Power = power;
            ContactPoint = contactPoint;
        }
    }

    /// <summary>
    /// Published by AbilityManager whenever the locked ability selection is (re)resolved into the
    /// per-side <see cref="RoundModifiers"/> for the current round. Cross-system broadcast (UI/FX
    /// may react). The gameplay consumers pull the modifiers from AbilityManager when they act, so
    /// they do not have to cache from this event - it exists for display + future systems.
    /// </summary>
    public class AbilitiesResolvedEvent
    {
        public RoundModifiers PlayerModifiers { get; }
        public RoundModifiers EnemyModifiers { get; }

        public AbilitiesResolvedEvent(RoundModifiers playerModifiers, RoundModifiers enemyModifiers)
        {
            PlayerModifiers = playerModifiers;
            EnemyModifiers = enemyModifiers;
        }
    }

    /// <summary>
    /// Published by AbilityManager when a Pom commits an ability for the round (or clears it, with
    /// <see cref="Ability"/> null). Carries the AP state so the HUD can show cost / remaining AP.
    /// </summary>
    public class AbilityCastEvent
    {
        public Side Side { get; }
        public int RosterIndex { get; }
        public PomAbilityData Ability { get; }
        public int CurrentAP { get; }
        public int MaxAP { get; }

        public AbilityCastEvent(Side side, int rosterIndex, PomAbilityData ability, int currentAP, int maxAP)
        {
            Side = side;
            RosterIndex = rosterIndex;
            Ability = ability;
            CurrentAP = currentAP;
            MaxAP = maxAP;
        }
    }

    /// <summary>
    /// Published by ScoringManager every time a ball lands in a scoring slot and produces a
    /// positive value. Carries enough info for the UI to spawn a flying "+N" popup that
    /// travels from the ball world position to the tug-of-war bar, and for EnergyManager to
    /// chip away at the OPPOSING side's energy in real time (per-ball, not per-round).
    /// </summary>
    public class BallScoredEvent
    {
        public Side Side { get; }
        public int Value { get; }
        public Vector3 WorldPos { get; }

        public BallScoredEvent(Side side, int value, Vector3 worldPos)
        {
            Side = side;
            Value = value;
            WorldPos = worldPos;
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
