using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Accumulates per-side ball scores during a round and publishes RoundScoredEvent once both
    /// sides have settled. Slot values come from the placeholder BoardScoringConfig.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Placeholder Scoring")]
        [SerializeField] private BoardScoringConfig scoring = new();

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentRound;
        [SerializeField] private int playerRoundScore;
        [SerializeField] private int enemyRoundScore;
        [SerializeField] private bool playerSettled;
        [SerializeField] private bool enemySettled;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);
            this.eventSystem.Subscribe<BallSettledEvent>(OnBallSettled);

            currentRound = 0;
            ResetRoundAccumulators();

            Debug.Log("[ScoringManager] Initialized");
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            currentRound = evt.RoundNumber;
            ResetRoundAccumulators();
        }

        private void OnBallSettled(BallSettledEvent evt)
        {
            int value = LookupSlotValue(evt.SlotIndex);
            if (evt.Side == Side.Player)
            {
                playerRoundScore += value;
                playerSettled = true;
            }
            else
            {
                enemyRoundScore += value;
                enemySettled = true;
            }

            Debug.Log($"[ScoringManager] {evt.Side} slot={evt.SlotIndex} value={value} (round={currentRound})");

            if (playerSettled && enemySettled)
            {
                eventSystem.Publish(new RoundScoredEvent(currentRound, playerRoundScore, enemyRoundScore));
            }
        }

        private int LookupSlotValue(int slotIndex)
        {
            if (scoring == null || scoring.slotValues == null) return 0;
            if (slotIndex < 0 || slotIndex >= scoring.slotValues.Length) return 0;
            return scoring.slotValues[slotIndex];
        }

        private void ResetRoundAccumulators()
        {
            playerRoundScore = 0;
            enemyRoundScore = 0;
            playerSettled = false;
            enemySettled = false;
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            eventSystem.Unsubscribe<BallSettledEvent>(OnBallSettled);
        }
    }
}
