using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// Battle HUD: temp dev controls (Start / Exit / Drop), round counter, roster strips,
    /// active-Pom card readouts, energy + score readouts, and a winner overlay. Roster row
    /// label slots are optional - wire them per-row in the scene to surface Pom name + level;
    /// leave them empty for the placeholder pre-roster layout.
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
        [Tooltip("Optional - per-row labels for the player roster. If wired, populated with each Pom's display name + level.")]
        [SerializeField] private List<TMP_Text> playerRosterRowLabels = new();
        [Tooltip("Optional - per-row labels for the enemy roster. If wired, populated with each Pom's display name + level.")]
        [SerializeField] private List<TMP_Text> enemyRosterRowLabels = new();
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

            // TODO: Temporary code-based onClick wiring (unblocks playtest). Replaced when UI Toolkit
            // panels land and buttons are wired via UI Toolkit clicked events / UnityEvents in the Inspector.
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

            var battleManager = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (battleManager == null)
            {
                Debug.LogError("[BattleHud] BattleManager unavailable, cannot start battle.");
                return;
            }
            battleManager.StartBattle();
        }

        private void OnExitClicked()
        {
            // Player flees the battle: end with Enemy as winner so SceneFlowManager unloads
            // Battle and resumes Overworld via the standard BattleEndedEvent flow.
            if (eventSystem == null)
            {
                Debug.LogError("[BattleHud] EventSystem unavailable, cannot exit battle.");
                return;
            }
            if (exitButton != null) exitButton.interactable = false;
            eventSystem.Publish(new BattleEndedEvent(Side.Enemy));
        }

        private void OnDropClicked()
        {
            if (dropButton != null) dropButton.interactable = false;

            var battleManager = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (battleManager == null)
            {
                Debug.LogError("[BattleHud] BattleManager unavailable, cannot request drop.");
                return;
            }
            battleManager.RequestDrop();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            UpdateRoundText(evt.RoundNumber);
            if (dropButton != null) dropButton.interactable = true;

            // The active indicator pins to row 0 (the primary active Pom). The Planning Phase UI
            // will eventually let the player choose which active row gets the primary indicator.
            UpdateActiveIndicator(playerActiveIndicator, playerRosterRows, 0);
            UpdateActiveIndicator(enemyActiveIndicator, enemyRosterRows, 0);

            var battleManager = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (battleManager != null)
            {
                var playerRoster = battleManager.GetRoster(Side.Player);
                var enemyRoster = battleManager.GetRoster(Side.Enemy);
                PopulateRosterLabels(playerRosterRowLabels, playerRoster);
                PopulateRosterLabels(enemyRosterRowLabels, enemyRoster);

                var playerActive = battleManager.GetActivePoms(Side.Player);
                var enemyActive = battleManager.GetActivePoms(Side.Enemy);
                UpdateActiveCard(playerActiveTitleText, playerActiveSubText, playerActive);
                UpdateActiveCard(enemyActiveTitleText, enemyActiveSubText, enemyActive);
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

        private void UpdateActiveCard(TMP_Text titleText, TMP_Text subText, IReadOnlyList<Pom> activePoms)
        {
            if (activePoms == null || activePoms.Count == 0) return;
            var primary = activePoms[0];
            if (primary == null || primary.Definition == null) return;

            int totalBalls = 0;
            for (int i = 0; i < activePoms.Count; i++)
            {
                var p = activePoms[i];
                if (p != null) totalBalls += p.CurrentBallCount;
            }

            if (titleText != null)
            {
                titleText.text = activePoms.Count > 1
                    ? $"Active: {primary.Definition.DisplayName} Lv.{primary.Level} (+{activePoms.Count - 1})"
                    : $"Active: {primary.Definition.DisplayName} Lv.{primary.Level}";
            }
            if (subText != null) subText.text = $"{primary.Definition.Type} | Ball x{totalBalls}";
        }

        private static void PopulateRosterLabels(List<TMP_Text> labels, IReadOnlyList<Pom> roster)
        {
            if (labels == null || labels.Count == 0) return;
            for (int i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null) continue;
                if (roster != null && i < roster.Count && roster[i] != null && roster[i].Definition != null)
                {
                    label.text = $"{roster[i].Definition.DisplayName} Lv.{roster[i].Level}";
                }
                else
                {
                    label.text = "--";
                }
            }
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
