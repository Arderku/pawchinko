using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns the turn-based battle state machine. Player drops first, then enemy; one full
    /// player+enemy cycle is a "round". MVP loops indefinitely.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        private enum State
        {
            WaitingForStart,
            WaitingForDrop,
            BallInFlight
        }

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private Side activeSide;
        [SerializeField] private State state;

        public int CurrentRound => currentRound;
        public Side ActiveSide => activeSide;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<DropRequestedEvent>(OnDropRequested);
            this.eventSystem.Subscribe<BallSettledEvent>(OnBallSettled);

            currentRound = 0;
            activeSide = Side.Player;
            state = State.WaitingForStart;

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
            activeSide = Side.Player;
            state = State.WaitingForDrop;

            Debug.Log($"[BattleManager] Battle started - Round {currentRound}, {activeSide} to drop");
            eventSystem.Publish(new RoundStartedEvent(currentRound, activeSide));
        }

        private void OnDropRequested(DropRequestedEvent evt)
        {
            if (state != State.WaitingForDrop)
            {
                Debug.LogWarning($"[BattleManager] DropRequested({evt.Side}) ignored - state is {state}");
                return;
            }
            if (evt.Side != activeSide)
            {
                Debug.LogWarning($"[BattleManager] DropRequested({evt.Side}) ignored - active side is {activeSide}");
                return;
            }

            var ballManager = GameManager.Instance != null ? GameManager.Instance.BallManager : null;
            if (ballManager == null)
            {
                Debug.LogError("[BattleManager] BallManager unavailable, cannot spawn ball.");
                return;
            }

            state = State.BallInFlight;
            ballManager.SpawnFor(activeSide);
            Debug.Log($"[BattleManager] Ball dropped for {activeSide}");
        }

        private void OnBallSettled(BallSettledEvent evt)
        {
            if (state != State.BallInFlight)
            {
                Debug.LogWarning($"[BattleManager] BallSettled({evt.Side}) received in unexpected state {state}");
                return;
            }

            Debug.Log($"[BattleManager] Ball settled - side={evt.Side}, slot={evt.SlotIndex}");
            eventSystem.Publish(new TurnEndedEvent(evt.Side));

            if (evt.Side == Side.Player)
            {
                activeSide = Side.Enemy;
                state = State.WaitingForDrop;
                eventSystem.Publish(new RoundStartedEvent(currentRound, activeSide));
            }
            else
            {
                currentRound++;
                activeSide = Side.Player;
                state = State.WaitingForDrop;
                eventSystem.Publish(new RoundStartedEvent(currentRound, activeSide));
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
