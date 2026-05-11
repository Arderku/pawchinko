using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns the battle HUD and any future UI sub-managers. Initialized by GameManager.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("UI Sub-Managers")]
        [SerializeField] private BattleHud battleHud;

        public BattleHud BattleHud => battleHud;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            if (battleHud != null) battleHud.Initialize(eventSystem);
            else Debug.LogError("[UIManager] battleHud not assigned!");

            Debug.Log("[UIManager] Initialized");
        }
    }
}
