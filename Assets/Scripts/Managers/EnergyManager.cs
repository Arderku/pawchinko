using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns team-summed energy. Seeds on BattleStartedEvent from the sum of every Pom's
    /// BaseEnergy across the full roster (active + bench), per PAWCHINKO_DESIGN_GUIDE
    /// Section 7. PlayerMax / EnemyMax are immutable for the battle and define the tug-of-war
    /// bar's two pool extents.
    ///
    /// Tug-of-war scoring is PER ROUND, not per ball: balls accumulate score during the
    /// round (ScoringManager owns the running totals + popups), and at round end the round's
    /// net diff is applied to ONLY the losing side. Example: player scored 50, enemy scored
    /// 20 -> enemy loses 30 energy, player loses nothing. A tie does nothing. Battle ends
    /// the instant either pool reaches 0.
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int playerEnergy;
        [SerializeField] private int enemyEnergy;
        [SerializeField] private int playerMax;
        [SerializeField] private int enemyMax;
        [SerializeField] private bool battleActive;

        public int PlayerEnergy => playerEnergy;
        public int EnemyEnergy => enemyEnergy;
        public int PlayerMax => playerMax;
        public int EnemyMax => enemyMax;
        public bool BattleActive => battleActive;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);

            playerEnergy = 0;
            enemyEnergy = 0;
            playerMax = 0;
            enemyMax = 0;
            battleActive = false;

            Debug.Log("[EnergyManager] Initialized");
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            playerMax = LookupStartingEnergy(Side.Player);
            enemyMax = LookupStartingEnergy(Side.Enemy);
            playerEnergy = playerMax;
            enemyEnergy = enemyMax;
            battleActive = true;

            Debug.Log($"[EnergyManager] Battle started - seed P={playerEnergy}/{playerMax} E={enemyEnergy}/{enemyMax}");
            eventSystem.Publish(new EnergyChangedEvent(playerEnergy, enemyEnergy));
        }

        private int LookupStartingEnergy(Side side)
        {
            var battleManager = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (battleManager == null)
            {
                Debug.LogError("[EnergyManager] BattleManager unavailable, cannot seed energy.");
                return 0;
            }
            var roster = battleManager.GetRoster(side);
            if (roster == null || roster.Count == 0)
            {
                Debug.LogError($"[EnergyManager] {side} roster is empty, energy will be 0.");
                return 0;
            }
            int sum = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                var pom = roster[i];
                if (pom != null && pom.data != null) sum += pom.data.BaseEnergy;
            }
            return sum;
        }

        /// <summary>
        /// Per-round tug-of-war: at end of round, only the LOSING side takes damage equal to
        /// the absolute score diff. Ties do nothing. Energy is clamped at 0 so we don't
        /// broadcast negatives.
        /// </summary>
        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (!battleActive) return;

            // Abilities can scale the energy a side collects from this round's score (e.g. +20%
            // to your own, or -% to the opponent's) before the tug-of-war diff is applied.
            var am = GameManager.Instance != null ? GameManager.Instance.AbilityManager : null;
            float playerPct = am != null ? am.GetModifiers(Side.Player).EnergyPercent : 0f;
            float enemyPct = am != null ? am.GetModifiers(Side.Enemy).EnergyPercent : 0f;

            int playerScore = Mathf.Max(0, Mathf.RoundToInt(evt.PlayerScore * (1f + playerPct)));
            int enemyScore = Mathf.Max(0, Mathf.RoundToInt(evt.EnemyScore * (1f + enemyPct)));

            int diff = playerScore - enemyScore;
            if (diff > 0)
            {
                enemyEnergy = Mathf.Max(0, enemyEnergy - diff);
            }
            else if (diff < 0)
            {
                playerEnergy = Mathf.Max(0, playerEnergy + diff); // diff is negative
            }

            Debug.Log($"[EnergyManager] Round {evt.RoundNumber} scored {evt.PlayerScore}|{evt.EnemyScore} (energy {playerScore}|{enemyScore}) diff={diff} -> P={playerEnergy}/{playerMax} E={enemyEnergy}/{enemyMax}");
            eventSystem.Publish(new EnergyChangedEvent(playerEnergy, enemyEnergy));

            if (playerEnergy <= 0 || enemyEnergy <= 0)
            {
                battleActive = false;
                Side winner = playerEnergy > enemyEnergy ? Side.Player : Side.Enemy;
                Debug.Log($"[EnergyManager] Battle ended - winner={winner} (P={playerEnergy}/{playerMax} E={enemyEnergy}/{enemyMax})");
                eventSystem.Publish(new BattleEndedEvent(winner));
            }
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            eventSystem.Unsubscribe<RoundScoredEvent>(OnRoundScored);
        }
    }
}
