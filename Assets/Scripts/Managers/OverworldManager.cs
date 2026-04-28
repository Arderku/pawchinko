using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pawchinko
{
    /// <summary>
    /// Scene-scoped root for Overworld.unity. Hides the entire overworld scene while a battle
    /// is loaded so Battle renders alone, then restores it on resume.
    /// </summary>
    public class OverworldManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        private GameObject _selfRoot;
        private bool _initialized;

        private void Awake()
        {
            _selfRoot = transform.root.gameObject;

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[OverworldManager] GameManager unavailable. Start from Boot.unity for full scene flow.");
                return;
            }

            GameManager.Instance.RegisterOverworldManager(this);
        }

        /// <summary>
        /// Initializes overworld subscriptions using the Boot-owned event system.
        /// </summary>
        public void Initialize(EventSystem eventSystem)
        {
            if (_initialized) return;

            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<OverworldPausedEvent>(OnOverworldPaused);
            this.eventSystem.Subscribe<OverworldResumedEvent>(OnOverworldResumed);

            SetOverworldActive(true);
            _initialized = true;
            Debug.Log("[OverworldManager] Initialized");
        }

        private void OnOverworldPaused(OverworldPausedEvent evt)
        {
            SetOverworldActive(false);
        }

        private void OnOverworldResumed(OverworldResumedEvent evt)
        {
            SetOverworldActive(true);
        }

        private void SetOverworldActive(bool active)
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid()) return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root == _selfRoot) continue;
                root.SetActive(active);
            }
        }

        private void OnDestroy()
        {
            if (eventSystem != null)
            {
                eventSystem.Unsubscribe<OverworldPausedEvent>(OnOverworldPaused);
                eventSystem.Unsubscribe<OverworldResumedEvent>(OnOverworldResumed);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.DeregisterOverworldManager(this);
            }
        }
    }
}
