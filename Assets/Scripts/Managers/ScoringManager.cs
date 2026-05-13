using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Accumulates per-side ball scores during a round and publishes RoundScoredEvent once both
    /// sides have settled every ball they spawned. Expected counts arrive via DropRequestedEvent;
    /// per-ball value is the placeholder slot value scaled by the active Pom's Power stat
    /// (PAWCHINKO_DESIGN_GUIDE Section 14: Score = BucketValue * Power * Modifiers - modifier
    /// math lands when abilities resolve).
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
        [SerializeField] private int playerExpected;
        [SerializeField] private int enemyExpected;
        [SerializeField] private int playerLanded;
        [SerializeField] private int enemyLanded;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);
            this.eventSystem.Subscribe<DropRequestedEvent>(OnDropRequested);
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

        private void OnDropRequested(DropRequestedEvent evt)
        {
            playerExpected = evt.PlayerBallCount;
            enemyExpected = evt.EnemyBallCount;
            playerLanded = 0;
            enemyLanded = 0;
        }

        private void OnBallSettled(BallSettledEvent evt)
        {
            int slotValue = LookupSlotValue(evt.SlotIndex);
            int scored = ScaleByPomPower(evt.SourcePom, slotValue);

            if (evt.Side == Side.Player)
            {
                playerRoundScore += scored;
                playerLanded++;
            }
            else
            {
                enemyRoundScore += scored;
                enemyLanded++;
            }

            string pomName = evt.SourcePom != null && evt.SourcePom.Definition != null ? evt.SourcePom.Definition.DisplayName : "(null)";
            Debug.Log($"[ScoringManager] {evt.Side} {pomName} slot={evt.SlotIndex} slotValue={slotValue} scored={scored} (round={currentRound} {playerLanded}/{playerExpected} {enemyLanded}/{enemyExpected})");

            if (playerExpected > 0 && enemyExpected > 0
                && playerLanded >= playerExpected
                && enemyLanded >= enemyExpected)
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

        private static int ScaleByPomPower(Pom pom, int slotValue)
        {
            if (pom == null || pom.Definition == null || pom.Definition.BaseStats == null) return slotValue;
            float power = pom.Definition.BaseStats.power;
            if (power <= 0f) return slotValue;
            return Mathf.RoundToInt(slotValue * power);
        }

        private void ResetRoundAccumulators()
        {
            playerRoundScore = 0;
            enemyRoundScore = 0;
            playerExpected = 0;
            enemyExpected = 0;
            playerLanded = 0;
            enemyLanded = 0;
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            eventSystem.Unsubscribe<DropRequestedEvent>(OnDropRequested);
            eventSystem.Unsubscribe<BallSettledEvent>(OnBallSettled);
        }
    }
}
