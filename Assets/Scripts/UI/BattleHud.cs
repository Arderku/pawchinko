using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// Battle HUD: temp dev controls (Start / Exit / Drop), round counter, placeholder roster
    /// strips, active-pet card readouts, energy + score readouts, and a winner overlay.
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Dev Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private TMP_Text roundCounterText;

        [Header("Roster")]
        [SerializeField] private List<RectTransform> playerRosterRows = new();
        [SerializeField] private List<RectTransform> enemyRosterRows = new();
        [SerializeField] private RectTransform playerActiveIndicator;
        [SerializeField] private RectTransform enemyActiveIndicator;

        [Header("Active Cards")]
        [SerializeField] private TMP_Text playerActiveTitleText;
        [SerializeField] private TMP_Text playerActiveSubText;
        [SerializeField] private TMP_Text enemyActiveTitleText;
        [SerializeField] private TMP_Text enemyActiveSubText;

        [Header("Energy / Score / Winner")]
        [SerializeField] private TMP_Text playerEnergyText;
        [SerializeField] private TMP_Text enemyEnergyText;
        [SerializeField] private TMP_Text roundScoreText;
        [SerializeField] private GameObject winnerOverlay;
        [SerializeField] private TMP_Text winnerText;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            if (startButton == null) Debug.LogError("[BattleHud] startButton not assigned!");
            if (exitButton == null) Debug.LogError("[BattleHud] exitButton not assigned!");
            if (dropButton == null) Debug.LogError("[BattleHud] dropButton not assigned!");
            if (roundCounterText == null) Debug.LogError("[BattleHud] roundCounterText not assigned!");
            if (playerRosterRows == null || playerRosterRows.Count == 0) Debug.LogError("[BattleHud] playerRosterRows not assigned!");
            if (enemyRosterRows == null || enemyRosterRows.Count == 0) Debug.LogError("[BattleHud] enemyRosterRows not assigned!");
            if (playerActiveIndicator == null) Debug.LogError("[BattleHud] playerActiveIndicator not assigned!");
            if (enemyActiveIndicator == null) Debug.LogError("[BattleHud] enemyActiveIndicator not assigned!");
            if (playerActiveTitleText == null) Debug.LogError("[BattleHud] playerActiveTitleText not assigned!");
            if (playerActiveSubText == null) Debug.LogError("[BattleHud] playerActiveSubText not assigned!");
            if (enemyActiveTitleText == null) Debug.LogError("[BattleHud] enemyActiveTitleText not assigned!");
            if (enemyActiveSubText == null) Debug.LogError("[BattleHud] enemyActiveSubText not assigned!");
            if (playerEnergyText == null) Debug.LogError("[BattleHud] playerEnergyText not assigned!");
            if (enemyEnergyText == null) Debug.LogError("[BattleHud] enemyEnergyText not assigned!");
            if (roundScoreText == null) Debug.LogError("[BattleHud] roundScoreText not assigned!");
            if (winnerOverlay == null) Debug.LogError("[BattleHud] winnerOverlay not assigned!");
            if (winnerText == null) Debug.LogError("[BattleHud] winnerText not assigned!");

            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);
            this.eventSystem.Subscribe<RoundScoredEvent>(OnRoundScored);
            this.eventSystem.Subscribe<EnergyChangedEvent>(OnEnergyChanged);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

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
            HideActiveIndicators();
            if (winnerOverlay != null) winnerOverlay.SetActive(false);
            if (playerEnergyText != null) playerEnergyText.text = "ENERGY: --";
            if (enemyEnergyText != null) enemyEnergyText.text = "ENERGY: --";
            if (roundScoreText != null) roundScoreText.text = "0 | 0";

            Debug.Log("[BattleHud] Initialized");
        }

        private void OnStartClicked()
        {
            if (winnerOverlay != null) winnerOverlay.SetActive(false);
            if (roundScoreText != null) roundScoreText.text = "0 | 0";
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

            UpdateActiveIndicator(playerActiveIndicator, playerRosterRows, evt.PlayerActivePetIndex);
            UpdateActiveIndicator(enemyActiveIndicator, enemyRosterRows, evt.EnemyActivePetIndex);

            var battleManager = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (battleManager != null)
            {
                var playerPet = battleManager.GetActivePet(Side.Player);
                var enemyPet = battleManager.GetActivePet(Side.Enemy);
                UpdateActiveCard(playerActiveTitleText, playerActiveSubText, playerPet);
                UpdateActiveCard(enemyActiveTitleText, enemyActiveSubText, enemyPet);
            }
        }

        private void OnRoundScored(RoundScoredEvent evt)
        {
            if (roundScoreText != null) roundScoreText.text = $"{evt.PlayerScore} | {evt.EnemyScore}";
        }

        private void OnEnergyChanged(EnergyChangedEvent evt)
        {
            if (playerEnergyText != null) playerEnergyText.text = $"ENERGY: {evt.PlayerEnergy}";
            if (enemyEnergyText != null) enemyEnergyText.text = $"ENERGY: {evt.EnemyEnergy}";
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (winnerOverlay != null) winnerOverlay.SetActive(true);
            if (winnerText != null) winnerText.text = $"WINNER: {evt.Winner.ToString().ToUpper()}";
            if (dropButton != null) dropButton.interactable = false;
            if (startButton != null) startButton.interactable = true;
        }

        private void UpdateActiveIndicator(RectTransform indicator, List<RectTransform> rows, int activeIndex)
        {
            if (indicator == null || rows == null || rows.Count == 0) return;
            if (activeIndex < 0 || activeIndex >= rows.Count) return;
            var row = rows[activeIndex];
            if (row == null) return;

            indicator.gameObject.SetActive(true);
            var indicatorPos = indicator.anchoredPosition;
            indicatorPos.y = row.anchoredPosition.y - row.sizeDelta.y * 0.5f;
            indicator.anchoredPosition = indicatorPos;
        }

        private void UpdateActiveCard(TMP_Text titleText, TMP_Text subText, PlaceholderPet pet)
        {
            if (pet == null) return;
            if (titleText != null) titleText.text = $"Active: {pet.petName} Lv.{pet.level}";
            if (subText != null) subText.text = "Ball x1";
        }

        private void HideActiveIndicators()
        {
            if (playerActiveIndicator != null) playerActiveIndicator.gameObject.SetActive(false);
            if (enemyActiveIndicator != null) enemyActiveIndicator.gameObject.SetActive(false);
        }

        private void UpdateRoundText(int round)
        {
            if (roundCounterText == null) return;
            roundCounterText.text = round <= 0 ? "Round -" : $"Round {round}";
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            eventSystem.Unsubscribe<RoundScoredEvent>(OnRoundScored);
            eventSystem.Unsubscribe<EnergyChangedEvent>(OnEnergyChanged);
            eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }
    }
}
