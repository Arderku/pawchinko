using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Scene-scoped composition root for Battle.unity. Owns battle manager initialization order.
    /// </summary>
    public class BattleSceneRoot : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private BallManager ballManager;
        [SerializeField] private ScoringManager scoringManager;
        [SerializeField] private EnergyManager energyManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private UIManager uiManager;

        private bool _initialized;

        public BoardManager BoardManager => boardManager;
        public BallManager BallManager => ballManager;
        public ScoringManager ScoringManager => scoringManager;
        public EnergyManager EnergyManager => energyManager;
        public BattleManager BattleManager => battleManager;
        public UIManager UIManager => uiManager;

        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[BattleSceneRoot] GameManager unavailable. Start from Boot.unity.");
                return;
            }

            GameManager.Instance.RegisterBattleScene(this);
        }

        /// <summary>
        /// Initializes battle managers in the order required by synchronous gameplay events.
        /// </summary>
        public void Initialize(EventSystem eventSystem)
        {
            if (_initialized) return;

            // Order matters: scoring and energy must subscribe before battle starts.
            if (boardManager != null) boardManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] BoardManager not assigned!");

            if (ballManager != null) ballManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] BallManager not assigned!");

            if (scoringManager != null) scoringManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] ScoringManager not assigned!");

            if (energyManager != null) energyManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] EnergyManager not assigned!");

            if (battleManager != null) battleManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] BattleManager not assigned!");

            if (uiManager != null) uiManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] UIManager not assigned!");

            _initialized = true;
            Debug.Log("[BattleSceneRoot] Initialized");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DeregisterBattleScene(this);
            }
        }
    }
}
