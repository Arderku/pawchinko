using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Holds references to the per-side BallSpawners so other managers can request a drop
    /// without knowing the scene hierarchy.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Spawners")]
        [SerializeField] private BallSpawner playerSpawner;
        [SerializeField] private BallSpawner enemySpawner;

        public BallSpawner PlayerSpawner => playerSpawner;
        public BallSpawner EnemySpawner => enemySpawner;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            if (playerSpawner == null) Debug.LogError("[BoardManager] playerSpawner not assigned!");
            if (enemySpawner == null) Debug.LogError("[BoardManager] enemySpawner not assigned!");

            Debug.Log("[BoardManager] Initialized");
        }

        /// <summary>
        /// Returns the spawner for the given side, or null if not configured.
        /// </summary>
        public BallSpawner GetSpawner(Side side)
        {
            return side == Side.Player ? playerSpawner : enemySpawner;
        }
    }
}
