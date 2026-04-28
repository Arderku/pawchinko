using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Persistent Boot composition object. Owns always-alive systems and accepts registration
    /// from scene-scoped managers as Overworld and Battle scenes load.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        [Header("Event System")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Persistent Managers")]
        [SerializeField] private SceneFlowManager sceneFlowManager;

        [Header("Scene Managers (read-only at runtime)")]
        [SerializeField] private OverworldManager overworldManager;
        [SerializeField] private BattleSceneRoot battleSceneRoot;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private BallManager ballManager;
        [SerializeField] private ScoringManager scoringManager;
        [SerializeField] private EnergyManager energyManager;
        [SerializeField] private UIManager uiManager;

        public EventSystem EventSystem => eventSystem;
        public SceneFlowManager SceneFlowManager => sceneFlowManager;
        public OverworldManager OverworldManager => overworldManager;
        public BattleSceneRoot BattleSceneRoot => battleSceneRoot;
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
            DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR
            Debug.unityLogger.logEnabled = false;
#endif

            if (eventSystem == null)
            {
                eventSystem = EventSystem.Instance;
            }

            InitializePersistentManagers();
        }

        /// <summary>
        /// Registers the currently loaded overworld scene manager.
        /// </summary>
        public void RegisterOverworldManager(OverworldManager manager)
        {
            overworldManager = manager;
            if (overworldManager != null)
            {
                overworldManager.Initialize(eventSystem);
                Debug.Log("[GameManager] OverworldManager registered");
            }
        }

        /// <summary>
        /// Clears the overworld manager reference when the scene unloads.
        /// </summary>
        public void DeregisterOverworldManager(OverworldManager manager)
        {
            if (overworldManager != manager) return;
            overworldManager = null;
            Debug.Log("[GameManager] OverworldManager deregistered");
        }

        /// <summary>
        /// Registers and initializes the loaded battle scene root.
        /// </summary>
        public void RegisterBattleScene(BattleSceneRoot root)
        {
            battleSceneRoot = root;
            if (battleSceneRoot == null) return;

            boardManager = root.BoardManager;
            ballManager = root.BallManager;
            scoringManager = root.ScoringManager;
            energyManager = root.EnergyManager;
            battleManager = root.BattleManager;
            uiManager = root.UIManager;

            battleSceneRoot.Initialize(eventSystem);
            Debug.Log("[GameManager] Battle scene registered");
        }

        /// <summary>
        /// Clears battle references when the additive battle scene unloads.
        /// </summary>
        public void DeregisterBattleScene(BattleSceneRoot root)
        {
            if (battleSceneRoot != root) return;

            battleSceneRoot = null;
            battleManager = null;
            boardManager = null;
            ballManager = null;
            scoringManager = null;
            energyManager = null;
            uiManager = null;

            Debug.Log("[GameManager] Battle scene deregistered");
        }

        private void InitializePersistentManagers()
        {
            if (sceneFlowManager != null) sceneFlowManager.Initialize(eventSystem);
            else Debug.LogError("[GameManager] SceneFlowManager not assigned in Inspector!");

            Debug.Log("[GameManager] Persistent managers initialized");
        }
    }
}
