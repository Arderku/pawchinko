using UnityEngine;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    /// <summary>
    /// Owns the Boot scene's UIDocument. Constructs always-on Boot UI views
    /// (currently just the version badge).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BootUIManager : MonoBehaviour
    {
        private UIDocument _document;
        private GameVersionView _gameVersion;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            VisualElement root = _document.rootVisualElement;
            if (root == null) { Debug.LogError("[BootUIManager] rootVisualElement is null"); return; }

            VisualElement gameVersionRoot = root.Q<VisualElement>("GameVersionUI");
            if (gameVersionRoot == null) { Debug.LogError("[BootUIManager] GameVersionUI instance not found"); return; }

            _gameVersion = new GameVersionView(gameVersionRoot);
            _gameVersion.Show();

            Debug.Log("[BootUIManager] Initialized");
        }

        private void OnDisable()
        {
            _gameVersion?.Dispose();
            _gameVersion = null;
        }
    }
}
