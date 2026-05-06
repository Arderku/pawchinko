using UnityEngine;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    /// <summary>
    /// Always-on top-left version badge. Reads GameManager.GameVersion once.
    /// </summary>
    public class GameVersionView : UIView
    {
        private Label _versionLabel;

        public GameVersionView(VisualElement root) : base(root) { }

        protected override void SetVisualElements()
        {
            _versionLabel = root.Q<Label>("gameVersionText");
            if (_versionLabel == null)
            {
                Debug.LogError("[GameVersionView] gameVersionText label not found");
                return;
            }
            _versionLabel.text = $"v{GameManager.GameVersion}";
        }

        public override void Show()
        {
            base.Show();
            if (_versionLabel != null) _versionLabel.text = $"v{GameManager.GameVersion}";
        }
    }
}
