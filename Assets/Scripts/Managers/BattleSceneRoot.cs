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
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private PegManager pegManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private UIManager uiManager;

        private bool _initialized;

        public BoardManager BoardManager => boardManager;
        public BallManager BallManager => ballManager;
        public ScoringManager ScoringManager => scoringManager;
        public EnergyManager EnergyManager => energyManager;
        public AbilityManager AbilityManager => abilityManager;
        public PegManager PegManager => pegManager;
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

            // AbilityManager has no scene dependencies; create one on demand so the battle scene
            // doesn't need a manual wiring step. Must be initialized before StartBattle so it
            // catches Round 1's RoundStartedEvent (AP refill + clear).
            if (abilityManager == null) abilityManager = gameObject.AddComponent<AbilityManager>();
            abilityManager.Initialize(eventSystem);

            // PegManager applies peg hide/restore around drops; same on-demand creation as above.
            // Initialized before StartBattle so it catches Round 1's RoundStartedEvent (restore).
            if (pegManager == null) pegManager = gameObject.AddComponent<PegManager>();
            pegManager.Initialize(eventSystem);

            if (battleManager != null) battleManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] BattleManager not assigned!");

            if (uiManager != null) uiManager.Initialize(eventSystem);
            else Debug.LogError("[BattleSceneRoot] UIManager not assigned!");

            // Auto-enter the Plan phase for Round 1 so the HUD shows rosters + energy from
            // frame 0. Subscribers (Energy/Scoring/HUD) are now all wired, so the events fire
            // safely. BattleManager.StartBattle guards re-entry on its own.
            if (battleManager != null) battleManager.StartBattle();

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
