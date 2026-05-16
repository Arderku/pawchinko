using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns team-summed energy. Seeds on BattleStartedEvent from the sum of every Pom's
    /// BaseEnergy across the full roster (active + bench), per PAWCHINKO_DESIGN_GUIDE
    /// Section 7. Applies the per-round score diff and ends the battle on energy &lt;= 0.
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int playerEnergy;
        [SerializeField] private int enemyEnergy;
        [SerializeField] private bool battleActive;

        public int PlayerEnergy => playerEnergy;
        public int EnemyEnergy => enemyEnergy;
        public bool BattleActive => battleActive;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);

            playerEnergy = 0;
            enemyEnergy = 0;
            battleActive = false;

            Debug.Log("[EnergyManager] Initialized");
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            playerEnergy = LookupStartingEnergy(Side.Player);
            enemyEnergy = LookupStartingEnergy(Side.Enemy);
            battleActive = true;

            Debug.Log($"[EnergyManager] Battle started - seed P={playerEnergy} E={enemyEnergy}");
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

        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (!battleActive) return;

            int diff = evt.PlayerScore - evt.EnemyScore;
            playerEnergy += diff;
            enemyEnergy -= diff;

            Debug.Log($"[EnergyManager] Round {evt.RoundNumber} scored {evt.PlayerScore}|{evt.EnemyScore} diff={diff} -> P={playerEnergy} E={enemyEnergy}");
            eventSystem.Publish(new EnergyChangedEvent(playerEnergy, enemyEnergy));

            if (playerEnergy <= 0 || enemyEnergy <= 0)
            {
                battleActive = false;
                Side winner = playerEnergy > enemyEnergy ? Side.Player : Side.Enemy;
                Debug.Log($"[EnergyManager] Battle ended - winner={winner}");
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
