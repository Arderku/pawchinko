using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pawchinko
{
    /// <summary>
    /// Owns the round-based battle state machine. Each side enters battle with a flexible
    /// roster of 1..<see cref="MaxRosterPoms"/> Poms split into a Battle Zone (the first
    /// <see cref="MaxActivePoms"/> entries spawn balls + may use abilities) and a Bench Zone
    /// (the remaining up to <see cref="MaxBenchPoms"/> entries sit out, ready to swap in).
    /// Each round, both sides drop the sum of their active Poms' ball-count contributions
    /// simultaneously; the round only advances once it has been scored. Battle ends on
    /// BattleEndedEvent.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public const int MaxActivePoms = 3;
        public const int MaxBenchPoms = 2;
        public const int MaxRosterPoms = MaxActivePoms + MaxBenchPoms;

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Rosters (1..5 PomData per side)")]
        [Tooltip("1..5 Pom species the player brings to battle. First MaxActivePoms (3) are the Battle Zone; remainder is the Bench Zone (up to MaxBenchPoms / 2).")]
        [FormerlySerializedAs("playerPomDefinitions")]
        [SerializeField] private List<PomData> playerPoms = new();
        [Tooltip("1..5 Pom species the enemy brings to battle. Same Battle/Bench split as the player.")]
        [FormerlySerializedAs("enemyPomDefinitions")]
        [SerializeField] private List<PomData> enemyPoms = new();
        [SerializeField] private int playerStartingLevel = 1;
        [SerializeField] private int enemyStartingLevel = 1;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private BattlePhase phase;
        [SerializeField] private List<PomInstance> playerRoster = new();
        [SerializeField] private List<PomInstance> enemyRoster = new();

        public int CurrentRound => currentRound;
        public BattlePhase Phase => phase;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

            EnsureRosters();

            currentRound = 0;
            phase = BattlePhase.WaitingForStart;

            Debug.Log($"[BattleManager] Initialized (P roster={playerRoster.Count}, E roster={enemyRoster.Count})");
        }

        /// <summary>Full roster (Battle Zone + Bench Zone) for the given side.</summary>
        public IReadOnlyList<PomInstance> GetRoster(Side side)
        {
            return side == Side.Player ? playerRoster : enemyRoster;
        }

        /// <summary>
        /// Battle-Zone Poms for the given side - the first <see cref="MaxActivePoms"/> entries
        /// of the roster (or fewer if the roster is shorter).
        /// </summary>
        public IReadOnlyList<PomInstance> GetActivePoms(Side side)
        {
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            int active = Mathf.Min(MaxActivePoms, roster.Count);
            var result = new List<PomInstance>(active);
            for (int i = 0; i < active; i++) result.Add(roster[i]);
            return result;
        }

        /// <summary>
        /// Bench-Zone Poms for the given side - roster entries past
        /// <see cref="MaxActivePoms"/>, up to <see cref="MaxBenchPoms"/> total.
        /// </summary>
        public IReadOnlyList<PomInstance> GetBenchPoms(Side side)
        {
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            int benchStart = MaxActivePoms;
            if (roster.Count <= benchStart) return System.Array.Empty<PomInstance>();
            int benchCount = Mathf.Min(MaxBenchPoms, roster.Count - benchStart);
            var result = new List<PomInstance>(benchCount);
            for (int i = 0; i < benchCount; i++) result.Add(roster[benchStart + i]);
            return result;
        }

        /// <summary>
        /// Primary active Pom (roster[0]) or null if the roster is empty. Convenience for HUD.
        /// </summary>
        public PomInstance GetActivePom(Side side)
        {
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            return roster.Count > 0 ? roster[0] : null;
        }

        /// <summary>
        /// Swaps two roster entries on the given side. Designed for the HUD's "Y to swap" -
        /// pass an active-zone index (0..MaxActivePoms-1) and a bench-zone index
        /// (MaxActivePoms..MaxRosterPoms-1). Allowed only between rounds (Phase ==
        /// WaitingForDrop). Returns true on success.
        /// </summary>
        public bool TrySwap(Side side, int rosterIndexA, int rosterIndexB)
        {
            if (phase != BattlePhase.WaitingForDrop)
            {
                Debug.LogWarning($"[BattleManager] TrySwap ignored - phase is {phase}, must be WaitingForDrop.");
                return false;
            }
            var roster = side == Side.Player ? playerRoster : enemyRoster;
            if (roster == null) return false;
            if (rosterIndexA < 0 || rosterIndexA >= roster.Count) return false;
            if (rosterIndexB < 0 || rosterIndexB >= roster.Count) return false;
            if (rosterIndexA == rosterIndexB) return false;

            (roster[rosterIndexA], roster[rosterIndexB]) = (roster[rosterIndexB], roster[rosterIndexA]);
            Debug.Log($"[BattleManager] Swapped {side} roster slots {rosterIndexA} <-> {rosterIndexB}");
            return true;
        }

        private void EnsureRosters()
        {
            playerRoster = BuildRoster(Side.Player, playerPoms, playerStartingLevel);
            enemyRoster = BuildRoster(Side.Enemy, enemyPoms, enemyStartingLevel);
        }

        private List<PomInstance> BuildRoster(Side side, List<PomData> pomData, int startingLevel)
        {
            var roster = new List<PomInstance>();
            if (pomData == null || pomData.Count == 0)
            {
                Debug.LogError($"[BattleManager] {side} roster is empty - assign 1..{MaxRosterPoms} PomData in the Inspector!");
                return roster;
            }
            if (pomData.Count > MaxRosterPoms)
            {
                Debug.LogWarning($"[BattleManager] {side} roster has {pomData.Count} entries; only the first {MaxRosterPoms} will be used (3 Battle + 2 Bench).");
            }
            int take = Mathf.Min(pomData.Count, MaxRosterPoms);
            for (int i = 0; i < take; i++)
            {
                var data = pomData[i];
                if (data == null)
                {
                    Debug.LogError($"[BattleManager] {side} roster slot {i} is null - fix the Inspector list.");
                    continue;
                }
                roster.Add(PomFactory.CreatePomInstance(data, startingLevel));
            }
            return roster;
        }

        /// <summary>
        /// Begins a new battle. Called by UI (Battle button). Publishes BattleStartedEvent
        /// first so EnergyManager seeds energy before round 1.
        /// </summary>
        public void StartBattle()
        {
            if (phase != BattlePhase.WaitingForStart && phase != BattlePhase.BattleOver)
            {
                Debug.LogWarning("[BattleManager] StartBattle ignored - already in phase " + phase);
                return;
            }

            EnsureRosters();

            currentRound = 1;
            phase = BattlePhase.WaitingForDrop;

            Debug.Log($"[BattleManager] Battle started - Round {currentRound}");
            eventSystem.Publish(new BattleStartedEvent());
            eventSystem.Publish(new RoundStartedEvent(currentRound));
        }

        /// <summary>
        /// Triggers the simultaneous drop for the current round. Each active Pom on each side
        /// spawns its current-level ball count; per-ball source-Pom info travels with the ball
        /// so ScoringManager can apply per-Pom Power.
        /// </summary>
        public void RequestDrop()
        {
            if (phase != BattlePhase.WaitingForDrop)
            {
                Debug.LogWarning($"[BattleManager] RequestDrop ignored - phase is {phase}");
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
                Debug.LogError($"[BattleManager] Drop aborted - non-positive ball total (P={playerBalls} E={enemyBalls}). Check PomData ball-count scale / roster setup.");
                return;
            }

            phase = BattlePhase.BallsInFlight;

            eventSystem.Publish(new DropRequestedEvent(playerBalls, enemyBalls));

            SpawnSide(ballManager, Side.Player, playerActive);
            SpawnSide(ballManager, Side.Enemy, enemyActive);

            Debug.Log($"[BattleManager] Round {currentRound} drop - P={playerBalls} balls ({playerActive.Count} active Poms), E={enemyBalls} balls ({enemyActive.Count} active Poms)");
        }

        private static int CountBalls(IReadOnlyList<PomInstance> activePoms)
        {
            int total = 0;
            for (int i = 0; i < activePoms.Count; i++) total += PomBallCount.GetCurrentBallCount(activePoms[i]);
            return total;
        }

        private static void SpawnSide(BallManager ballManager, Side side, IReadOnlyList<PomInstance> activePoms)
        {
            for (int i = 0; i < activePoms.Count; i++)
            {
                var pom = activePoms[i];
                int count = PomBallCount.GetCurrentBallCount(pom);
                for (int b = 0; b < count; b++) ballManager.SpawnFor(side, pom);
            }
        }

        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (phase != BattlePhase.BallsInFlight) return;

            currentRound++;
            phase = BattlePhase.WaitingForDrop;
            eventSystem.Publish(new RoundStartedEvent(currentRound));
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            phase = BattlePhase.BattleOver;
            Debug.Log($"[BattleManager] Battle over - winner={evt.Winner}");
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundScoredEvent>(OnRoundScored);
            eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }
    }

    /// <summary>Public phase enum exposed by BattleManager so UI can drive button states.</summary>
    public enum BattlePhase
    {
        WaitingForStart = 0,
        WaitingForDrop = 1,
        BallsInFlight = 2,
        BattleOver = 3
    }
}
