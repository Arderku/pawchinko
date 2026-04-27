using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Root composition object. Owns the EventSystem reference and initializes every sub-manager
    /// in a known order. In MVP we live in SampleScene; later this moves to Boot.unity.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        [Header("Event System")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Managers")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private BallManager ballManager;
        [SerializeField] private ScoringManager scoringManager;
        [SerializeField] private EnergyManager energyManager;
        [SerializeField] private UIManager uiManager;

        public EventSystem EventSystem => eventSystem;
        public BattleManager BattleManager => battleManager;
        public BoardManager BoardManager => boardManager;
        public BallManager BallManager => ballManager;
        public ScoringManager ScoringManager => scoringManager;
        public EnergyManager EnergyManager => energyManager;
        public UIManager UIManager => uiManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

#if !UNITY_EDITOR
            Debug.unityLogger.logEnabled = false;
#endif

            if (eventSystem == null)
            {
                eventSystem = EventSystem.Instance;
            }

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            // Order matters: subscribers (Scoring, Energy) must be alive before publishers.
            // BattleManager.StartBattle() (called directly by the HUD's Start button) publishes
            // BattleStartedEvent then RoundStartedEvent synchronously - EnergyManager must already
            // be subscribed. UIManager is initialized last so the HUD subscribes to its gameplay
            // events before BattleManager's first publish on the first OnStartClicked.
            if (boardManager != null) boardManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] BoardManager not assigned in Inspector!");

            if (ballManager != null) ballManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] BallManager not assigned in Inspector!");

            if (scoringManager != null) scoringManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] ScoringManager not assigned in Inspector!");

            if (energyManager != null) energyManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] EnergyManager not assigned in Inspector!");

            if (battleManager != null) battleManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] BattleManager not assigned in Inspector!");

            if (uiManager != null) uiManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] UIManager not assigned in Inspector!");

            Debug.Log("[GameManager] All managers initialized");
        }
    }
}
