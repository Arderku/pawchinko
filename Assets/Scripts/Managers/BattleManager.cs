using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns the round-based battle state machine. Each round, both sides drop a single ball
    /// simultaneously; the round only advances once the round has been scored (so energy
    /// updates land before the next round starts). Battle ends on BattleEndedEvent.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public const int PetsPerSide = 5;

        private enum State
        {
            WaitingForStart,
            WaitingForDrop,
            BallsInFlight,
            BattleOver
        }

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Placeholder Teams")]
        [SerializeField] private List<PlaceholderPet> playerTeam = new();
        [SerializeField] private List<PlaceholderPet> enemyTeam = new();

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private State state;
        [SerializeField] private int playerActiveIndex;
        [SerializeField] private int enemyActiveIndex;

        public int CurrentRound => currentRound;
        public int PlayerActiveIndex => playerActiveIndex;
        public int EnemyActiveIndex => enemyActiveIndex;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<DropRequestedEvent>(OnDropRequested);
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

            EnsureDefaultTeams();

            currentRound = 0;
            state = State.WaitingForStart;
            playerActiveIndex = 0;
            enemyActiveIndex = 0;

            Debug.Log("[BattleManager] Initialized");
        }

        /// <summary>
        /// Returns the currently active placeholder pet for the given side. Read-only convenience
        /// for the HUD; manager-to-manager state changes still go through events.
        /// </summary>
        public PlaceholderPet GetActivePet(Side side)
        {
            var team = side == Side.Player ? playerTeam : enemyTeam;
            int idx = side == Side.Player ? playerActiveIndex : enemyActiveIndex;
            if (team == null || team.Count == 0) return null;
            if (idx < 0 || idx >= team.Count) return null;
            return team[idx];
        }

        private void EnsureDefaultTeams()
        {
            if (playerTeam == null) playerTeam = new List<PlaceholderPet>();
            if (enemyTeam == null) enemyTeam = new List<PlaceholderPet>();
            if (playerTeam.Count < PetsPerSide)
            {
                playerTeam.Clear();
                for (int i = 0; i < PetsPerSide; i++) playerTeam.Add(new PlaceholderPet($"Pet {i + 1}", 1));
            }
            if (enemyTeam.Count < PetsPerSide)
            {
                enemyTeam.Clear();
                for (int i = 0; i < PetsPerSide; i++) enemyTeam.Add(new PlaceholderPet($"Pet {i + 1}", 1));
            }
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            if (state != State.WaitingForStart && state != State.BattleOver)
            {
                Debug.LogWarning("[BattleManager] BattleStartedEvent ignored - already in state " + state);
                return;
            }

            currentRound = 1;
            playerActiveIndex = 0;
            enemyActiveIndex = 0;
            state = State.WaitingForDrop;

            Debug.Log($"[BattleManager] Battle started - Round {currentRound}");
            eventSystem.Publish(new RoundStartedEvent(currentRound, playerActiveIndex, enemyActiveIndex));
        }

        private void OnDropRequested(DropRequestedEvent evt)
        {
            if (state != State.WaitingForDrop)
            {
                Debug.LogWarning($"[BattleManager] DropRequested ignored - state is {state}");
                return;
            }

            var ballManager = GameManager.Instance != null ? GameManager.Instance.BallManager : null;
            if (ballManager == null)
            {
                Debug.LogError("[BattleManager] BallManager unavailable, cannot spawn balls.");
                return;
            }

            state = State.BallsInFlight;

            ballManager.SpawnFor(Side.Player);
            ballManager.SpawnFor(Side.Enemy);

            Debug.Log($"[BattleManager] Round {currentRound} drop - both sides (active P={playerActiveIndex} E={enemyActiveIndex})");
        }

        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (state != State.BallsInFlight) return;

            // BattleEndedEvent (also published by EnergyManager from this same RoundScoredEvent) will
            // arrive after this handler if energy hits 0. We optimistically advance here; the
            // OnBattleEnded handler then pins the state to BattleOver and prevents further drops.
            currentRound++;
            playerActiveIndex = (playerActiveIndex + 1) % PetsPerSide;
            enemyActiveIndex = (enemyActiveIndex + 1) % PetsPerSide;
            state = State.WaitingForDrop;
            eventSystem.Publish(new RoundStartedEvent(currentRound, playerActiveIndex, enemyActiveIndex));
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            state = State.BattleOver;
            Debug.Log($"[BattleManager] Battle over - winner={evt.Winner}");
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            eventSystem.Unsubscribe<DropRequestedEvent>(OnDropRequested);
            eventSystem.Unsubscribe<RoundScoredEvent>(OnRoundScored);
            eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }
    }
}
