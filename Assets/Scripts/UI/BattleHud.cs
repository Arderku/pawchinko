using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// Battle HUD: temp dev controls (Start / Exit / Drop) stacked in the middle, plus the
    /// round counter. Drop is disabled while balls are in flight and re-enabled when the next
    /// round starts (driven by RoundStartedEvent).
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("UI")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private TMP_Text roundCounterText;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            if (startButton == null) Debug.LogError("[BattleHud] startButton not assigned!");
            if (exitButton == null) Debug.LogError("[BattleHud] exitButton not assigned!");
            if (dropButton == null) Debug.LogError("[BattleHud] dropButton not assigned!");
            if (roundCounterText == null) Debug.LogError("[BattleHud] roundCounterText not assigned!");

            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClicked);
                startButton.interactable = true;
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
                exitButton.interactable = true;
            }
            if (dropButton != null)
            {
                dropButton.onClick.RemoveAllListeners();
                dropButton.onClick.AddListener(OnDropClicked);
                dropButton.interactable = false;
            }

            UpdateRoundText(0);

            Debug.Log("[BattleHud] Initialized");
        }

        private void OnStartClicked()
        {
            if (startButton != null) startButton.interactable = false;
            eventSystem.Publish(new BattleStartedEvent());
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDropClicked()
        {
            if (dropButton != null) dropButton.interactable = false;
            eventSystem.Publish(new DropRequestedEvent());
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            UpdateRoundText(evt.RoundNumber);
            if (dropButton != null) dropButton.interactable = true;
        }

        private void UpdateRoundText(int round)
        {
            if (roundCounterText == null) return;
            roundCounterText.text = round <= 0 ? "Round -" : $"Round {round}";
        }

        private void OnDestroy()
        {
            if (eventSystem != null)
            {
                eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            }
        }
    }
}
