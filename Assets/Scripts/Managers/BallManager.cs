using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns ball lifecycle: assigns IDs, asks the right spawner to instantiate, and reroutes
    /// settle callbacks from Ball into the EventSystem.
    /// </summary>
    public class BallManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private BoardManager boardManager;

        private int _nextBallId;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            if (boardManager == null && GameManager.Instance != null)
            {
                boardManager = GameManager.Instance.BoardManager;
            }

            if (boardManager == null)
            {
                Debug.LogError("[BallManager] BoardManager not assigned!");
            }
            else
            {
                // BallSpawner is now async (queued + waved); subscribe once so we can hook
                // the per-ball Settled event the moment each ball is actually instantiated.
                if (boardManager.PlayerSpawner != null) boardManager.PlayerSpawner.BallSpawned += OnBallSpawned;
                if (boardManager.EnemySpawner != null) boardManager.EnemySpawner.BallSpawned += OnBallSpawned;
            }

            _nextBallId = 0;
            Debug.Log("[BallManager] Initialized");
        }

        /// <summary>
        /// Queues a single ball drop on the given side via the BoardManager's spawner. The
        /// spawner releases queued balls across N invisible spawn zones at the top of the
        /// board so they don't overlap. The source Pom rides along with each ball so scoring
        /// can apply its stats via BallSettledEvent.
        /// </summary>
        public void SpawnFor(Side side, PomInstance sourcePom)
        {
            if (boardManager == null)
            {
                Debug.LogError("[BallManager] BoardManager unavailable.");
                return;
            }

            var spawner = boardManager.GetSpawner(side);
            if (spawner == null)
            {
                Debug.LogError($"[BallManager] No spawner for side {side}.");
                return;
            }

            int id = _nextBallId++;
            spawner.Enqueue(id, side, sourcePom);
        }

        private void OnBallSpawned(Ball ball)
        {
            if (ball == null) return;
            ball.Settled += OnBallSettled;
        }

        private void OnBallSettled(Ball ball, Slot slot)
        {
            ball.Settled -= OnBallSettled;
            if (eventSystem == null) return;
            eventSystem.Publish(new BallSettledEvent(ball.Id, ball.Side, slot.SlotIndex, ball.SourcePom));
        }

        private void OnDestroy()
        {
            if (boardManager != null)
            {
                if (boardManager.PlayerSpawner != null) boardManager.PlayerSpawner.BallSpawned -= OnBallSpawned;
                if (boardManager.EnemySpawner != null) boardManager.EnemySpawner.BallSpawned -= OnBallSpawned;
            }
        }
    }
}
