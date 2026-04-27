using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns team-summed energy. Seeds on BattleStartedEvent (placeholderEnergyPerPet * petsPerSide),
    /// applies the per-round score diff (PAWCHINKO_DESIGN_GUIDE Section 7), and ends the battle
    /// on energy &lt;= 0. All values are explicitly placeholder until creature data exists.
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Placeholder Tuning")]
        [SerializeField] private int placeholderEnergyPerPet = 10;
        [SerializeField] private int petsPerSide = 5;

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
            int starting = placeholderEnergyPerPet * petsPerSide;
            playerEnergy = starting;
            enemyEnergy = starting;
            battleActive = true;

            Debug.Log($"[EnergyManager] Battle started - seeding energy {starting} per side");
            eventSystem.Publish(new EnergyChangedEvent(playerEnergy, enemyEnergy));
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
