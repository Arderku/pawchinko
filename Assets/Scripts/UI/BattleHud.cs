using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// Battle HUD: Start button, Drop buttons (one per side, mutually exclusive), round counter.
    /// Listens to RoundStartedEvent to toggle which Drop button is active.
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("UI")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button dropPlayerButton;
        [SerializeField] private Button dropEnemyButton;
        [SerializeField] private TMP_Text roundCounterText;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            if (startButton == null) Debug.LogError("[BattleHud] startButton not assigned!");
            if (dropPlayerButton == null) Debug.LogError("[BattleHud] dropPlayerButton not assigned!");
            if (dropEnemyButton == null) Debug.LogError("[BattleHud] dropEnemyButton not assigned!");
            if (roundCounterText == null) Debug.LogError("[BattleHud] roundCounterText not assigned!");

            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClicked);
            }
            if (dropPlayerButton != null)
            {
                dropPlayerButton.onClick.RemoveAllListeners();
                dropPlayerButton.onClick.AddListener(OnDropPlayerClicked);
            }
            if (dropEnemyButton != null)
            {
                dropEnemyButton.onClick.RemoveAllListeners();
                dropEnemyButton.onClick.AddListener(OnDropEnemyClicked);
            }

            ShowOnly(startButton);
            UpdateRoundText(0);

            Debug.Log("[BattleHud] Initialized");
        }

        private void OnStartClicked()
        {
            HideAllButtons();
            eventSystem.Publish(new BattleStartedEvent());
        }

        private void OnDropPlayerClicked()
        {
            HideAllButtons();
            eventSystem.Publish(new DropRequestedEvent(Side.Player));
        }

        private void OnDropEnemyClicked()
        {
            HideAllButtons();
            eventSystem.Publish(new DropRequestedEvent(Side.Enemy));
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            UpdateRoundText(evt.RoundNumber);
            ShowOnly(evt.ActiveSide == Side.Player ? dropPlayerButton : dropEnemyButton);
        }

        private void UpdateRoundText(int round)
        {
            if (roundCounterText == null) return;
            roundCounterText.text = round <= 0 ? "Round -" : $"Round {round}";
        }

        private void ShowOnly(Button toShow)
        {
            if (startButton != null) startButton.gameObject.SetActive(startButton == toShow);
            if (dropPlayerButton != null) dropPlayerButton.gameObject.SetActive(dropPlayerButton == toShow);
            if (dropEnemyButton != null) dropEnemyButton.gameObject.SetActive(dropEnemyButton == toShow);
        }

        private void HideAllButtons()
        {
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (dropPlayerButton != null) dropPlayerButton.gameObject.SetActive(false);
            if (dropEnemyButton != null) dropEnemyButton.gameObject.SetActive(false);
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
