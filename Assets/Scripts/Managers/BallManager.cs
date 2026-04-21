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

        private int _nextBallId;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            _nextBallId = 0;
            Debug.Log("[BallManager] Initialized");
        }

        /// <summary>
        /// Spawns a single ball on the given side via the BoardManager's spawner reference.
        /// </summary>
        public Ball SpawnFor(Side side)
        {
            var boardManager = GameManager.Instance != null ? GameManager.Instance.BoardManager : null;
            if (boardManager == null)
            {
                Debug.LogError("[BallManager] BoardManager unavailable.");
                return null;
            }

            var spawner = boardManager.GetSpawner(side);
            if (spawner == null)
            {
                Debug.LogError($"[BallManager] No spawner for side {side}.");
                return null;
            }

            int id = _nextBallId++;
            Ball ball = spawner.Spawn(id, side);
            if (ball != null) ball.Settled += OnBallSettled;
            return ball;
        }

        private void OnBallSettled(Ball ball, Slot slot)
        {
            ball.Settled -= OnBallSettled;
            if (eventSystem == null) return;
            eventSystem.Publish(new BallSettledEvent(ball.Id, ball.Side, slot.SlotIndex));
        }

        private void OnDestroy()
        {
        }
    }
}
