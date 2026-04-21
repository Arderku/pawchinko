using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns the round-based battle state machine. Each round, both sides drop a single ball
    /// simultaneously; the next round only starts once both balls have settled.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        private enum State
        {
            WaitingForStart,
            WaitingForDrop,
            BallsInFlight
        }

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private State state;
        [SerializeField] private bool playerSettled;
        [SerializeField] private bool enemySettled;

        public int CurrentRound => currentRound;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<DropRequestedEvent>(OnDropRequested);
            this.eventSystem.Subscribe<BallSettledEvent>(OnBallSettled);

            currentRound = 0;
            state = State.WaitingForStart;
            playerSettled = false;
            enemySettled = false;

            Debug.Log("[BattleManager] Initialized");
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            if (state != State.WaitingForStart)
            {
                Debug.LogWarning("[BattleManager] BattleStartedEvent ignored - already in state " + state);
                return;
            }

            currentRound = 1;
            state = State.WaitingForDrop;

            Debug.Log($"[BattleManager] Battle started - Round {currentRound}");
            eventSystem.Publish(new RoundStartedEvent(currentRound));
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
            playerSettled = false;
            enemySettled = false;

            ballManager.SpawnFor(Side.Player);
            ballManager.SpawnFor(Side.Enemy);

            Debug.Log($"[BattleManager] Round {currentRound} drop - both sides");
        }

        private void OnBallSettled(BallSettledEvent evt)
        {
            if (state != State.BallsInFlight)
            {
                Debug.LogWarning($"[BattleManager] BallSettled({evt.Side}) received in unexpected state {state}");
                return;
            }

            if (evt.Side == Side.Player) playerSettled = true;
            else enemySettled = true;

            Debug.Log($"[BattleManager] Ball settled - side={evt.Side}, slot={evt.SlotIndex}");

            if (playerSettled && enemySettled)
            {
                currentRound++;
                state = State.WaitingForDrop;
                eventSystem.Publish(new RoundStartedEvent(currentRound));
            }
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            eventSystem.Unsubscribe<DropRequestedEvent>(OnDropRequested);
            eventSystem.Unsubscribe<BallSettledEvent>(OnBallSettled);
        }
    }
}
