using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns the round-based battle state machine. Each side enters battle with a flexible
    /// roster of Poms (1..N). At any moment up to <see cref="MaxActivePoms"/> Poms are active
    /// (spawning balls, using abilities); the remainder sit on the bench recovering AP. Each
    /// round, both sides drop the sum of their active Poms' ball-count contributions
    /// simultaneously; the round only advances once it has been scored. Battle ends on
    /// BattleEndedEvent.
    ///
    /// Pre-Planning-Phase scope: the active set is currently the first <c>min(MaxActivePoms,
    /// roster.Count)</c> entries; swapping comes with the Planning Phase UI. Players may bring
    /// fewer than <see cref="MaxActivePoms"/> total Poms - 1, 2, or more all work
    /// (PAWCHINKO_DESIGN_GUIDE Section 5/6).
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public const int MaxActivePoms = 3;

        private enum State
        {
            WaitingForStart,
            WaitingForDrop,
            BallsInFlight,
            BattleOver
        }

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Rosters")]
        [Tooltip("1..N Pom species the player brings to battle. The first MaxActivePoms entries are active each round; the rest sit on the bench.")]
        [SerializeField] private List<PomDefinition> playerPomDefinitions = new();
        [Tooltip("1..N Pom species the enemy brings to battle. Same active/bench split as the player.")]
        [SerializeField] private List<PomDefinition> enemyPomDefinitions = new();
        [SerializeField] private int playerStartingLevel = 1;
        [SerializeField] private int enemyStartingLevel = 1;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private State state;
        [SerializeField] private List<Pom> playerRoster = new();
        [SerializeField] private List<Pom> enemyRoster = new();

        public int CurrentRound => currentRound;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

            EnsureRosters();

            currentRound = 0;
            state = State.WaitingForStart;

            Debug.Log($"[BattleManager] Initialized (P roster={playerRoster.Count}, E roster={enemyRoster.Count})");
        }

        /// <summary>
        /// Returns the full roster (active + bench) for the given side.
        /// </summary>
        public IReadOnlyList<Pom> GetRoster(Side side)
        {
            return side == Side.Player ? playerRoster : enemyRoster;
        }

        /// <summary>
        /// Returns the active Poms for the given side - the first
        /// <see cref="MaxActivePoms"/> entries of the roster, or fewer if the roster is shorter.
        /// </summary>
        public IReadOnlyList<Pom> GetActivePoms(Side side)
        {
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            int active = Mathf.Min(MaxActivePoms, roster.Count);
            // Allocating a small list each call is fine for the current battle cadence; if this
            // shows up in a hot path later, replace with a pooled buffer.
            var result = new List<Pom>(active);
            for (int i = 0; i < active; i++) result.Add(roster[i]);
            return result;
        }

        /// <summary>
        /// Returns the primary active Pom for the given side (first slot of the active set), or
        /// null if the roster is empty. Convenience for the HUD's single-card display while the
        /// Planning Phase UI is still TBD.
        /// </summary>
        public Pom GetActivePom(Side side)
        {
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            return roster.Count > 0 ? roster[0] : null;
        }

        private void EnsureRosters()
        {
            playerRoster = BuildRoster(Side.Player, playerPomDefinitions, playerStartingLevel);
            enemyRoster = BuildRoster(Side.Enemy, enemyPomDefinitions, enemyStartingLevel);
        }

        private List<Pom> BuildRoster(Side side, List<PomDefinition> definitions, int startingLevel)
        {
            var roster = new List<Pom>();
            if (definitions == null || definitions.Count == 0)
            {
                Debug.LogError($"[BattleManager] {side} roster is empty - assign at least one PomDefinition in the Inspector!");
                return roster;
            }
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null)
                {
                    Debug.LogError($"[BattleManager] {side} roster slot {i} is null - fix the Inspector list.");
                    continue;
                }
                roster.Add(new Pom(def, startingLevel));
            }
            return roster;
        }

        /// <summary>
        /// Begins a new battle. Called directly by UI (BattleHud) - not via the bus.
        /// Publishes BattleStartedEvent first so EnergyManager seeds energy before round 1.
        /// </summary>
        public void StartBattle()
        {
            if (state != State.WaitingForStart && state != State.BattleOver)
            {
                Debug.LogWarning("[BattleManager] StartBattle ignored - already in state " + state);
                return;
            }

            // Re-seed Pom runtime instances so a restart after BattleOver resets level/exp/abilities.
            EnsureRosters();

            currentRound = 1;
            state = State.WaitingForDrop;

            Debug.Log($"[BattleManager] Battle started - Round {currentRound}");
            eventSystem.Publish(new BattleStartedEvent());
            eventSystem.Publish(new RoundStartedEvent(currentRound));
        }

        /// <summary>
        /// Triggers the simultaneous drop for the current round. Called directly by UI. Each
        /// active Pom on each side spawns its current-level ball count; per-ball source-Pom
        /// info travels with the ball so ScoringManager can apply per-Pom Power.
        /// </summary>
        public void RequestDrop()
        {
            if (state != State.WaitingForDrop)
            {
                Debug.LogWarning($"[BattleManager] RequestDrop ignored - state is {state}");
                return;
            }

            var ballManager = GameManager.Instance != null ? GameManager.Instance.BallManager : null;
            if (ballManager == null)
            {
                Debug.LogError("[BattleManager] BallManager unavailable, cannot spawn balls.");
                return;
            }

            var playerActive = GetActivePoms(Side.Player);
            var enemyActive = GetActivePoms(Side.Enemy);

            int playerBalls = CountBalls(playerActive);
            int enemyBalls = CountBalls(enemyActive);

            if (playerBalls <= 0 || enemyBalls <= 0)
            {
                Debug.LogError($"[BattleManager] Drop aborted - non-positive ball total (P={playerBalls} E={enemyBalls}). Check PomDefinition.ballProfile / roster setup.");
                return;
            }

            state = State.BallsInFlight;

            // Publish FIRST so ScoringManager knows expected counts before any BallSettledEvent fires.
            eventSystem.Publish(new DropRequestedEvent(playerBalls, enemyBalls));

            SpawnSide(ballManager, Side.Player, playerActive);
            SpawnSide(ballManager, Side.Enemy, enemyActive);

            Debug.Log($"[BattleManager] Round {currentRound} drop - P={playerBalls} balls ({playerActive.Count} active Poms), E={enemyBalls} balls ({enemyActive.Count} active Poms)");
        }

        private static int CountBalls(IReadOnlyList<Pom> activePoms)
        {
            int total = 0;
            for (int i = 0; i < activePoms.Count; i++) total += activePoms[i].CurrentBallCount;
            return total;
        }

        private static void SpawnSide(BallManager ballManager, Side side, IReadOnlyList<Pom> activePoms)
        {
            for (int i = 0; i < activePoms.Count; i++)
            {
                var pom = activePoms[i];
                int count = pom.CurrentBallCount;
                for (int b = 0; b < count; b++) ballManager.SpawnFor(side, pom);
            }
        }

        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (state != State.BallsInFlight) return;

            // BattleEndedEvent (also published by EnergyManager from this same RoundScoredEvent) will
            // arrive after this handler if energy hits 0. We optimistically advance here; the
            // OnBattleEnded handler then pins the state to BattleOver and prevents further drops.
            currentRound++;
            state = State.WaitingForDrop;
            eventSystem.Publish(new RoundStartedEvent(currentRound));
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            state = State.BattleOver;
            Debug.Log($"[BattleManager] Battle over - winner={evt.Winner}");
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundScoredEvent>(OnRoundScored);
            eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }
    }
}
