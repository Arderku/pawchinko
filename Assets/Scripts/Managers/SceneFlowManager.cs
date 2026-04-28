using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pawchinko
{
    /// <summary>
    /// Owns all scene loading and unloading for the Boot / Overworld / Battle flow.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string overworldSceneName = "Overworld";
        [SerializeField] private string battleSceneName = "Battle";

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        private bool _overworldLoadRequested;
        private bool _battleLoaded;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

            LoadOverworldIfNeeded();
            Debug.Log("[SceneFlowManager] Initialized");
        }

        private void LoadOverworldIfNeeded()
        {
            if (_overworldLoadRequested) return;
            if (SceneManager.GetSceneByName(overworldSceneName).isLoaded) return;

            _overworldLoadRequested = true;
            var op = SceneManager.LoadSceneAsync(overworldSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneFlowManager] Failed to load {overworldSceneName}.");
                return;
            }

            op.completed += _ =>
            {
                var overworldScene = SceneManager.GetSceneByName(overworldSceneName);
                if (overworldScene.IsValid())
                {
                    SceneManager.SetActiveScene(overworldScene);
                }
                Debug.Log($"[SceneFlowManager] Loaded {overworldSceneName}");
            };
        }

        private void OnEncounterTriggered(EncounterTriggeredEvent evt)
        {
            if (_battleLoaded)
            {
                Debug.LogWarning("[SceneFlowManager] Encounter ignored because Battle is already loaded.");
                return;
            }

            _battleLoaded = true;
            eventSystem.Publish(new OverworldPausedEvent());

            var op = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                _battleLoaded = false;
                eventSystem.Publish(new OverworldResumedEvent());
                Debug.LogError($"[SceneFlowManager] Failed to load {battleSceneName}.");
                return;
            }

            op.completed += _ =>
            {
                var battleScene = SceneManager.GetSceneByName(battleSceneName);
                if (battleScene.IsValid())
                {
                    SceneManager.SetActiveScene(battleScene);
                }
                Debug.Log($"[SceneFlowManager] Loaded {battleSceneName}");
            };
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (!_battleLoaded) return;

            var battleScene = SceneManager.GetSceneByName(battleSceneName);
            if (!battleScene.isLoaded)
            {
                _battleLoaded = false;
                eventSystem.Publish(new OverworldResumedEvent());
                return;
            }

            var op = SceneManager.UnloadSceneAsync(battleScene);
            if (op == null)
            {
                Debug.LogError($"[SceneFlowManager] Failed to unload {battleSceneName}.");
                return;
            }

            op.completed += _ =>
            {
                _battleLoaded = false;
                var overworldScene = SceneManager.GetSceneByName(overworldSceneName);
                if (overworldScene.IsValid())
                {
                    SceneManager.SetActiveScene(overworldScene);
                }
                eventSystem.Publish(new OverworldResumedEvent());
                Debug.Log($"[SceneFlowManager] Unloaded {battleSceneName}");
            };
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }
    }
}
