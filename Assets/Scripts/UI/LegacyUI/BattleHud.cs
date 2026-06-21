using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pawchinko
{
        /// <summary>
        /// Battle HUD (Pickup-style layout): round counter top-center, both sides showing 5
        /// Pom cards split into Battle Zone (3) + Bench Zone (2), an ability picker beside the
        /// focused active card, two boards in the middle, score + total readouts at the bottom,
        /// and BATTLE / RETREAT main buttons with keyboard + gamepad support via the Input
        /// System (Confirm / Retreat / Swap / Ability / Navigate actions on the Battle map).
        ///
        /// Focus model: the player can navigate through 7 focusable items - the 5 player Pom
        /// cards (indices 0..4) plus the BATTLE button (index 5) and RETREAT button (index 6).
        /// Default focus is the BATTLE button so the player can immediately see what's selected
        /// and press Confirm to start dropping balls. Each focusable target has its own outline
        /// child that this script toggles in <see cref="RefreshFocus"/>.
        ///
        /// While the ability picker is open, navigation is captured by the picker: Up/Down
        /// cycle between NONE / ABILITY 1 / ABILITY 2. Confirm locks the highlighted option
        /// and returns focus to the originating Pom card. Retreat (Esc / B) cancels the
        /// picker without changing the selection and without ending the battle.
        ///
        /// Confirm semantics:
        /// - Picker open -> lock current ability selection and close.
        /// - Active card focused (0..2) -> open the picker for that Pom.
        /// - RETREAT focused (6) -> publish BattleEndedEvent (player loses).
        /// - Otherwise (bench focused / BATTLE focused) -> request drop.
        ///
        /// Retreat semantics:
        /// - Picker open -> cancel picker (no selection change, battle continues).
        /// - Otherwise -> publish BattleEndedEvent.
        /// </summary>
    public class BattleHud : MonoBehaviour
    {
        public const int NoFocus = -1;

        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Main Buttons")]
        [SerializeField] private Button battleButton;
        [SerializeField] private Button retreatButton;
        [SerializeField] private TMP_Text battleButtonLabel;
        [Tooltip("Focus outline child shown when the BATTLE button is the currently focused navigation target.")]
        [SerializeField] private GameObject battleButtonFocusOutline;
        [Tooltip("Focus outline child shown when the RETREAT button is the currently focused navigation target.")]
        [SerializeField] private GameObject retreatButtonFocusOutline;

        [Header("Top Bar")]
        [SerializeField] private TMP_Text roundCounterText;

        [Header("Pom Cards (length BattleManager.MaxRosterPoms = 5 per side)")]
        [Tooltip("Cards 0..2 are the Battle Zone, cards 3..4 are the Bench Zone. Same on both sides.")]
        [SerializeField] private List<BattlePomCardView> playerCards = new();
        [SerializeField] private List<BattlePomCardView> enemyCards = new();

        [Header("Portrait Stage (live 3D portraits)")]
        [Tooltip("Off-screen stage that renders the 5+5 Pom prefabs into the cards' RawImages.")]
        [SerializeField] private PomPortraitStage portraitStage;

        [Header("Ability Picker (player side only)")]
        [SerializeField] private AbilityPickerView playerAbilityPicker;

        [Header("Energy Bar (tug-of-war)")]
        [Tooltip("Tug-of-war bar at the bottom. Animates between player and enemy energy.")]
        [SerializeField] private TugOfWarBar energyBar;
        [Tooltip("Canvas parent under which score popups spawn (must live on the HUD canvas).")]
        [SerializeField] private RectTransform popupLayer;
        [Tooltip("Prefab/template used to spawn a +N flying popup when a ball lands. Cloned on demand.")]
        [SerializeField] private ScorePopup scorePopupTemplate;
        [Tooltip("Color used for player-side popups.")]
        [SerializeField] private Color playerPopupColor = new(0.25f, 0.55f, 0.95f);
        [Tooltip("Color used for enemy-side popups.")]
        [SerializeField] private Color enemyPopupColor = new(0.95f, 0.3f, 0.35f);
        [Tooltip("Camera used to project ball world positions into HUD canvas space. Defaults to Camera.main.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Canvas this HUD belongs to. Required to convert screen->local space for popups.")]
        [SerializeField] private Canvas hudCanvas;

        [Header("Winner Overlay")]
        [SerializeField] private GameObject winnerOverlay;
        [SerializeField] private TMP_Text winnerText;

        [Header("Control Hints")]
        [SerializeField] private TMP_Text controlHintText;

        [Header("Input Actions (Battle map)")]
        [SerializeField] private InputActionReference confirmAction;
        [SerializeField] private InputActionReference retreatAction;
        [SerializeField] private InputActionReference swapAction;
        [SerializeField] private InputActionReference abilityAction;
        [SerializeField] private InputActionReference navigateAction;

        // Focus indices layout (size = playerCards.Count + 2):
        //   0..2 = active Pom cards
        //   3..4 = bench Pom cards
        //   5    = BATTLE button (BattleButtonFocusIndex)
        //   6    = RETREAT button (RetreatButtonFocusIndex)
        // NoFocus (-1) is only used while clearing visuals; default focus on Initialize is BATTLE.
        private int _focusIndex = NoFocus;
        private int _abilitySelection = AbilityPickerView.NoneIndex; // 0..2
        private bool _pickerOpen;
        private float _navCooldown;
        private const float NavRepeatSeconds = 0.2f;

        // Periodic heartbeat that logs the live action state so we can spot regressions where
        // the Battle map gets disabled by other scenes. Set to 0 to silence the probe.
        private float _navProbeAccum;
        private const float NavProbeSeconds = 5f;

        private int BattleButtonFocusIndex => playerCards.Count;
        private int RetreatButtonFocusIndex => playerCards.Count + 1;
        private int FocusableCount => playerCards.Count + 2;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;

            ValidateSerializedRefs();
            ConfigureInputBackgroundBehavior();

            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);
            this.eventSystem.Subscribe<BattleStartedEvent>(OnBattleStarted);
            this.eventSystem.Subscribe<BallScoredEvent>(OnBallScored);
            this.eventSystem.Subscribe<EnergyChangedEvent>(OnEnergyChanged);
            this.eventSystem.Subscribe<BattleEndedEvent>(OnBattleEnded);

            WireButton(battleButton, OnBattlePressed);
            WireButton(retreatButton, OnRetreatPressed);

            WireAction(confirmAction, OnConfirmPressedCallback);
            WireAction(retreatAction, OnRetreatPressedCallback);
            WireAction(swapAction, OnSwapPressedCallback);
            WireAction(abilityAction, OnAbilityPressedCallback);
            if (navigateAction != null && navigateAction.action != null) navigateAction.action.Enable();

            // Make sure the entire Battle action map is enabled. Other systems on the same
            // asset (Player/Overworld controllers) call Disable() on their action maps when the
            // overworld pauses, and at least one Unity Input System path also tears down the
            // map when an asset is re-imported during Play mode. Enabling the map explicitly
            // here is the belt-and-braces fix; the individual Enable() calls above already
            // forked the actions out of the map, so this just keeps them aligned.
            EnableBattleActionMap();

            DiagnoseInputBinding("confirm", confirmAction);
            DiagnoseInputBinding("retreat", retreatAction);
            DiagnoseInputBinding("swap", swapAction);
            DiagnoseInputBinding("ability", abilityAction);
            DiagnoseInputBinding("navigate", navigateAction);

            ResetHudVisuals();
            _pickerOpen = false;
            _abilitySelection = AbilityPickerView.NoneIndex;
            if (playerAbilityPicker != null) playerAbilityPicker.Hide();
            ClearAllCards();
            RefreshButtonLabel();

            // Pre-populate Plan phase: BattleSceneRoot calls battleManager.StartBattle() right
            // after uiManager.Initialize, so by this point the rosters are built and the round
            // is set. Bind both sides immediately so cards are visible from frame 0.
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm != null)
            {
                RebindPlayerSide(bm);
                RebindEnemySide(bm);
                UpdateRoundText(bm.CurrentRound);
            }

            // Default selection is the BATTLE button so the player immediately sees what is
            // focused and can press Confirm to drop balls.
            _focusIndex = BattleButtonFocusIndex;
            RefreshFocus();

            Debug.Log("[BattleHud] Initialized");
        }

        private void Update()
        {
            EnsureBattleMapEnabled();
            HandleNavigateInput();
            ProbeInputHeartbeat();
        }

        private void EnsureBattleMapEnabled()
        {
            // Brute-force re-enable every frame. The probe revealed that nav.enabled was False
            // 1+ seconds after Initialize. Something in another scene (Overworld controllers,
            // OverworldManager pause, or the Input System reimport) is disabling the Battle
            // map after we enable it. Until that culprit is found, this keeps inputs alive.
            var map = ResolveBattleActionMap();
            if (map != null && !map.enabled) map.Enable();
        }

        private void ProbeInputHeartbeat()
        {
            if (NavProbeSeconds <= 0f) return;
            _navProbeAccum += Time.unscaledDeltaTime;
            if (_navProbeAccum < NavProbeSeconds) return;
            _navProbeAccum = 0f;

            string confirmState = DescribeActionState(confirmAction);
            string retreatState = DescribeActionState(retreatAction);
            string swapState = DescribeActionState(swapAction);
            string abilityState = DescribeActionState(abilityAction);
            string navState = DescribeActionState(navigateAction);

            Debug.Log(
                $"[BattleHud] Probe focus={_focusIndex} appFocused={Application.isFocused} " +
                $"confirm:[{confirmState}] retreat:[{retreatState}] swap:[{swapState}] ability:[{abilityState}] nav:[{navState}]");
        }

        private static string DescribeActionState(InputActionReference reference)
        {
            if (reference == null) return "ref=null";
            var action = reference.action;
            if (action == null) return "action=null";
            string map = action.actionMap != null ? action.actionMap.name : "<no-map>";
            string mapEnabled = action.actionMap != null ? action.actionMap.enabled.ToString() : "n/a";
            return $"{map}/{action.name} actEnabled={action.enabled} mapEnabled={mapEnabled} controls={action.controls.Count}";
        }

        // ---------- Input callback adapters ----------

        private void OnConfirmPressedCallback(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[BattleHud] Confirm action fired (control={ctx.control?.path}, focus={_focusIndex}, pickerOpen={_pickerOpen}).");
            OnConfirmPressed();
        }

        private void OnRetreatPressedCallback(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[BattleHud] Retreat action fired (control={ctx.control?.path}).");
            OnRetreatPressed();
        }

        private void OnSwapPressedCallback(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[BattleHud] Swap action fired (control={ctx.control?.path}, focus={_focusIndex}).");
            OnSwapPressed();
        }

        private void OnAbilityPressedCallback(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[BattleHud] Ability action fired (control={ctx.control?.path}, pickerOpen={_pickerOpen}).");
            OnAbilityPressed();
        }

        // ---------- High-level intents ----------

        /// <summary>
        /// Context-sensitive Confirm:
        ///   - Picker open -> lock current ability selection and close.
        ///   - Active card focused -> open the picker for that Pom.
        ///   - RETREAT button focused -> trigger retreat.
        ///   - Otherwise (bench focused / BATTLE focused / no focus) -> trigger a BATTLE drop.
        /// </summary>
        private void OnConfirmPressed()
        {
            if (_pickerOpen)
            {
                LockAbilitySelection();
                return;
            }
            if (IsActiveCardFocused())
            {
                OpenAbilityPickerForFocused();
                return;
            }
            if (_focusIndex == RetreatButtonFocusIndex)
            {
                OnRetreatPressed();
                return;
            }
            OnBattlePressed();
        }

        /// <summary>
        /// One press of BATTLE = one drop. No Start/Drop split; the auto-started Plan phase
        /// owns Round 1 setup, and every subsequent press progresses the rounds.
        /// </summary>
        private void OnBattlePressed()
        {
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null)
            {
                Debug.LogError("[BattleHud] BattleManager unavailable.");
                return;
            }
            if (bm.Phase != BattlePhase.WaitingForDrop)
            {
                Debug.Log($"[BattleHud] Drop ignored - phase is {bm.Phase}.");
                return;
            }
            bm.RequestDrop();
            RefreshButtonLabel();
        }

        private void OnRetreatPressed()
        {
            // Picker open -> Retreat acts as Cancel: close the picker without changing the
            // remembered selection and without ending the battle. The user still has their
            // active card focused so they can navigate to other cards or BATTLE/RETREAT.
            if (_pickerOpen)
            {
                CancelAbilityPicker();
                return;
            }

            if (eventSystem == null)
            {
                Debug.LogError("[BattleHud] EventSystem unavailable, cannot retreat.");
                return;
            }
            eventSystem.Publish(new BattleEndedEvent(Side.Enemy));
        }

        private void OnSwapPressed()
        {
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null) return;
            if (bm.Phase != BattlePhase.WaitingForDrop) return;
            if (!IsCardFocused()) return;

            // Swap focused active slot with the first bench slot. If focus is on bench,
            // swap that bench slot with the last active slot.
            if (_focusIndex < BattleManager.MaxActivePoms)
            {
                int active = Mathf.Clamp(_focusIndex, 0, BattleManager.MaxActivePoms - 1);
                int benchTarget = BattleManager.MaxActivePoms;
                if (bm.TrySwap(Side.Player, active, benchTarget))
                {
                    RebindPlayerSide(bm);
                }
            }
            else
            {
                int activeTarget = BattleManager.MaxActivePoms - 1;
                if (bm.TrySwap(Side.Player, _focusIndex, activeTarget))
                {
                    RebindPlayerSide(bm);
                }
            }
        }

        private void OnAbilityPressed()
        {
            // X / F only does anything while the ability picker is open. Picker open/close is
            // driven exclusively by Confirm per the interaction model.
            if (!_pickerOpen) return;
            _abilitySelection = (_abilitySelection + 1) % AbilityPickerView.OptionCount;
            if (playerAbilityPicker != null) playerAbilityPicker.Refresh(GetFocusedActivePom(), _abilitySelection);
            PreviewHighlightedAbility();
        }

        private void HandleNavigateInput()
        {
            if (navigateAction == null || navigateAction.action == null) return;
            Vector2 v = navigateAction.action.ReadValue<Vector2>();
            if (v.sqrMagnitude < 0.25f)
            {
                _navCooldown = 0f;
                return;
            }
            _navCooldown -= Time.unscaledDeltaTime;
            if (_navCooldown > 0f) return;
            _navCooldown = NavRepeatSeconds;

            if (_pickerOpen)
            {
                HandlePickerNavigate(v);
                return;
            }
            HandleCardAndButtonNavigate(v);
        }

        private void HandleCardAndButtonNavigate(Vector2 v)
        {
            int previous = _focusIndex;
            int max = FocusableCount - 1;
            if (_focusIndex == NoFocus)
            {
                if (v.y > 0.5f) _focusIndex = max;
                else if (v.y < -0.5f) _focusIndex = 0;
            }
            else
            {
                if (v.y > 0.5f) _focusIndex = Mathf.Max(0, _focusIndex - 1);
                else if (v.y < -0.5f) _focusIndex = Mathf.Min(max, _focusIndex + 1);
            }
            if (_focusIndex != previous)
            {
                Debug.Log($"[BattleHud] Navigate v=({v.x:F2},{v.y:F2}) focus {previous} -> {_focusIndex}.");
                RefreshFocus();
            }
        }

        private void HandlePickerNavigate(Vector2 v)
        {
            int previous = _abilitySelection;
            int max = AbilityPickerView.OptionCount - 1;
            if (v.y > 0.5f) _abilitySelection = Mathf.Max(0, _abilitySelection - 1);
            else if (v.y < -0.5f) _abilitySelection = Mathf.Min(max, _abilitySelection + 1);

            if (_abilitySelection != previous)
            {
                Debug.Log($"[BattleHud] Picker nav {previous} -> {_abilitySelection}.");
                if (playerAbilityPicker != null) playerAbilityPicker.Refresh(GetFocusedActivePom(), _abilitySelection);
                PreviewHighlightedAbility();
            }
        }

        // ---------- Ability picker control ----------

        private bool IsActiveCardFocused()
        {
            return _focusIndex >= 0 && _focusIndex < BattleManager.MaxActivePoms;
        }

        private bool IsCardFocused()
        {
            return _focusIndex >= 0 && _focusIndex < playerCards.Count;
        }

        private void OpenAbilityPickerForFocused()
        {
            if (playerAbilityPicker == null) return;
            var pom = GetFocusedActivePom();
            if (pom == null) return;
            _abilitySelection = AbilityPickerView.NoneIndex;
            _pickerOpen = true;
            playerAbilityPicker.Show(pom, _abilitySelection);
            PreviewHighlightedAbility();
        }

        private void LockAbilitySelection()
        {
            // Route the highlighted choice into the AbilityManager, which validates AP + type,
            // (re)builds the round modifiers and broadcasts the new state. NONE clears this Pom's
            // selection. Focus remains on the originating active card so the player keeps navigating.
            _pickerOpen = false;
            if (playerAbilityPicker != null) playerAbilityPicker.Hide();
            ClearAbilityPreview();

            int activeIndex = _focusIndex; // active card index (0..MaxActivePoms-1) while picker open
            var am = GameManager.Instance != null ? GameManager.Instance.AbilityManager : null;
            var pom = GetFocusedActivePom();
            if (am == null || pom == null)
            {
                Debug.LogWarning("[BattleHud] Cannot lock ability - AbilityManager or focused Pom unavailable.");
                return;
            }

            PomAbilityData ability = null;
            if (_abilitySelection == AbilityPickerView.Slot1Index) ability = LearnedAbilityAt(pom, 0);
            else if (_abilitySelection == AbilityPickerView.Slot2Index) ability = LearnedAbilityAt(pom, 1);

            bool ok = am.SelectAbility(activeIndex, ability);
            if (!ok && ability != null)
            {
                Debug.Log($"[BattleHud] Ability '{ability.DisplayName}' not locked (unaffordable or type-locked). Selection unchanged.");
            }
            else
            {
                Debug.Log($"[BattleHud] Ability locked: pom={activeIndex}, ability={(ability != null ? ability.DisplayName : "NONE")}, AP={pom.currentAP}/{pom.maxAP}");
            }
        }

        private static PomAbilityData LearnedAbilityAt(PomInstance pom, int learnedSlot)
        {
            if (pom == null || pom.learnedAbilities == null) return null;
            if (learnedSlot < 0 || learnedSlot >= pom.learnedAbilities.Length) return null;
            return pom.learnedAbilities[learnedSlot];
        }

        private void CancelAbilityPicker()
        {
            // Close the picker without committing the highlighted selection. _abilitySelection
            // is left untouched so the previously locked choice persists.
            _pickerOpen = false;
            if (playerAbilityPicker != null) playerAbilityPicker.Hide();
            ClearAbilityPreview();
            Debug.Log("[BattleHud] Ability picker cancelled (focus stays on card).");
        }

        /// <summary>
        /// Previews the currently highlighted ability's affected pegs on the board (type-colored
        /// wash) while the picker is open. NONE highlighted clears the preview.
        /// </summary>
        private void PreviewHighlightedAbility()
        {
            var pm = GameManager.Instance != null ? GameManager.Instance.PegManager : null;
            if (pm == null) return;

            var pom = GetFocusedActivePom();
            PomAbilityData ability = null;
            if (_abilitySelection == AbilityPickerView.Slot1Index) ability = LearnedAbilityAt(pom, 0);
            else if (_abilitySelection == AbilityPickerView.Slot2Index) ability = LearnedAbilityAt(pom, 1);

            pm.PreviewAbility(ability, pom, Side.Player);
        }

        /// <summary>Clears any planning peg preview (called when the picker closes).</summary>
        private void ClearAbilityPreview()
        {
            var pm = GameManager.Instance != null ? GameManager.Instance.PegManager : null;
            if (pm != null) pm.PreviewAbility(null, null, Side.Player);
        }

        private PomInstance GetFocusedActivePom()
        {
            if (!IsActiveCardFocused()) return null;
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null) return null;
            var active = bm.GetActivePoms(Side.Player);
            return active != null && _focusIndex < active.Count ? active[_focusIndex] : null;
        }

        // ---------- Event subscribers ----------

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            UpdateRoundText(evt.RoundNumber);
            RefreshButtonLabel();

            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null) return;
            RebindPlayerSide(bm);
            RebindEnemySide(bm);
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            // Wait one frame so EnergyManager.OnBattleStarted has run and PlayerMax/EnemyMax
            // are populated. Initialize is invoked in scene-root order; either order is safe
            // because we just read the values lazily here.
            var em = GameManager.Instance != null ? GameManager.Instance.EnergyManager : null;
            if (em != null && energyBar != null)
            {
                energyBar.Configure(em.PlayerMax, em.EnemyMax);
            }
        }

        private void OnBallScored(BallScoredEvent evt)
        {
            SpawnScorePopup(evt.Side, evt.Value, evt.WorldPos);
        }

        private void OnEnergyChanged(EnergyChangedEvent evt)
        {
            if (energyBar != null) energyBar.SetEnergies(evt.PlayerEnergy, evt.EnemyEnergy);
        }

        private void SpawnScorePopup(Side side, int value, Vector3 worldPos)
        {
            if (scorePopupTemplate == null || popupLayer == null) return;
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;

            Vector2 screenStart = cam.WorldToScreenPoint(worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screenStart,
                    hudCanvas != null && hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas?.worldCamera,
                    out Vector2 localStart))
            {
                return;
            }

            Vector2 localTarget = Vector2.zero;
            if (energyBar != null)
            {
                Vector3 barWorld = energyBar.GetSideAnchorWorld(side, hudCanvas != null ? hudCanvas.worldCamera : null);
                // Bar lives on the HUD canvas; its world point can be projected through the
                // canvas's world camera (or none for Overlay) directly into popupLayer space.
                Camera screenCam = hudCanvas != null && hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (hudCanvas != null ? hudCanvas.worldCamera : null);
                Vector2 barScreen = RectTransformUtility.WorldToScreenPoint(screenCam, barWorld);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, barScreen, screenCam, out localTarget);
            }

            var popup = Instantiate(scorePopupTemplate, popupLayer);
            popup.gameObject.SetActive(true);
            popup.Begin($"+{value}", side == Side.Player ? playerPopupColor : enemyPopupColor, localStart, localTarget);
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (winnerOverlay != null) winnerOverlay.SetActive(true);
            if (winnerText != null) winnerText.text = $"WINNER: {evt.Winner.ToString().ToUpper()}";
            RefreshButtonLabel();
        }

        // ---------- View helpers ----------

        private void ResetHudVisuals()
        {
            UpdateRoundText(0);
            if (winnerOverlay != null) winnerOverlay.SetActive(false);
            if (energyBar != null) energyBar.Configure(0, 0);
            if (controlHintText != null) controlHintText.text = "Y to swap\nX to ability";
        }

        private void RefreshButtonLabel()
        {
            if (battleButtonLabel == null) return;
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm != null && bm.Phase == BattlePhase.BallsInFlight)
            {
                battleButtonLabel.text = "...";
                return;
            }
            battleButtonLabel.text = "BATTLE";
        }

        private void RebindPlayerSide(BattleManager battleManager)
        {
            var roster = battleManager.GetRoster(Side.Player);
            BindCards(playerCards, roster);
            if (portraitStage != null) portraitStage.BindPlayerSide(roster);
            RefreshFocus();
        }

        private void RebindEnemySide(BattleManager battleManager)
        {
            var roster = battleManager.GetRoster(Side.Enemy);
            BindCards(enemyCards, roster);
            if (portraitStage != null) portraitStage.BindEnemySide(roster);
        }

        private static void BindCards(List<BattlePomCardView> cards, IReadOnlyList<PomInstance> roster)
        {
            if (cards == null) return;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;
                if (roster != null && i < roster.Count) card.Bind(roster[i]);
                else card.Clear();
            }
        }

        private void RefreshFocus()
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                var card = playerCards[i];
                if (card != null) card.SetFocused(i == _focusIndex);
            }
            if (battleButtonFocusOutline != null) battleButtonFocusOutline.SetActive(_focusIndex == BattleButtonFocusIndex);
            if (retreatButtonFocusOutline != null) retreatButtonFocusOutline.SetActive(_focusIndex == RetreatButtonFocusIndex);
            // Navigation never opens or closes the ability picker. The picker is controlled
            // solely by Confirm via OpenAbilityPickerForFocused / LockAbilitySelection.
        }

        private void ClearAllCards()
        {
            for (int i = 0; i < playerCards.Count; i++) playerCards[i]?.Clear();
            for (int i = 0; i < enemyCards.Count; i++) enemyCards[i]?.Clear();
            _focusIndex = NoFocus;
            if (battleButtonFocusOutline != null) battleButtonFocusOutline.SetActive(false);
            if (retreatButtonFocusOutline != null) retreatButtonFocusOutline.SetActive(false);
            RefreshFocus();
        }

        private void UpdateRoundText(int round)
        {
            if (roundCounterText == null) return;
            roundCounterText.text = round <= 0 ? "ROUND -" : $"ROUND {round}";
        }

        // ---------- Wiring helpers ----------

        private static void WireButton(Button button, System.Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => handler?.Invoke());
            button.interactable = true;
        }

        private static void WireAction(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= handler;
            reference.action.performed += handler;
            reference.action.Enable();
        }

        /// <summary>
        /// Resolves the Battle action map from any of the wired action references and enables
        /// the entire map. Returns the map (or null) so callers can inspect post-state.
        /// </summary>
        private InputActionMap EnableBattleActionMap()
        {
            var map = ResolveBattleActionMap();
            if (map == null)
            {
                Debug.LogError("[BattleHud] Could not resolve Battle action map from any reference.");
                return null;
            }
            if (!map.enabled) map.Enable();
            return map;
        }

        private InputActionMap ResolveBattleActionMap()
        {
            return confirmAction?.action?.actionMap
                ?? retreatAction?.action?.actionMap
                ?? swapAction?.action?.actionMap
                ?? abilityAction?.action?.actionMap
                ?? navigateAction?.action?.actionMap;
        }

        /// <summary>
        /// By default the Input System discards keyboard/gamepad input whenever the Game window
        /// loses focus (e.g. you click into the Inspector / Console). That presents as "keys
        /// don't work" even though the action map is enabled. Forcing IgnoreFocus + Run in
        /// Background keeps Battle navigation working while you're switching panels in-editor.
        /// </summary>
        private static void ConfigureInputBackgroundBehavior()
        {
            Application.runInBackground = true;
            if (InputSystem.settings != null)
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
        }

        private static void DiagnoseInputBinding(string label, InputActionReference reference)
        {
            if (reference == null)
            {
                Debug.LogError($"[BattleHud] {label} action reference is null - not wired in the inspector.");
                return;
            }
            var action = reference.action;
            if (action == null)
            {
                Debug.LogError($"[BattleHud] {label} action could not be resolved from reference '{reference.name}'.");
                return;
            }
            string map = action.actionMap != null ? action.actionMap.name : "<no-map>";
            Debug.Log($"[BattleHud] {label} -> {map}/{action.name} enabled={action.enabled} bindings={action.bindings.Count}");
        }

        private static void UnwireAction(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= handler;
            reference.action.Disable();
        }

        private void ValidateSerializedRefs()
        {
            if (battleButton == null) Debug.LogError("[BattleHud] battleButton not assigned!");
            if (retreatButton == null) Debug.LogError("[BattleHud] retreatButton not assigned!");
            if (roundCounterText == null) Debug.LogError("[BattleHud] roundCounterText not assigned!");
            if (playerCards == null || playerCards.Count != BattleManager.MaxRosterPoms)
            {
                Debug.LogError($"[BattleHud] playerCards must be exactly {BattleManager.MaxRosterPoms} entries (first {BattleManager.MaxActivePoms} active, last {BattleManager.MaxBenchPoms} bench).");
            }
            if (enemyCards == null || enemyCards.Count != BattleManager.MaxRosterPoms)
            {
                Debug.LogError($"[BattleHud] enemyCards must be exactly {BattleManager.MaxRosterPoms} entries.");
            }
            if (playerAbilityPicker == null) Debug.LogError("[BattleHud] playerAbilityPicker not assigned!");
            if (portraitStage == null) Debug.LogError("[BattleHud] portraitStage not assigned! Run Pawchinko/Build Battle HUD to rebuild.");
            if (energyBar == null) Debug.LogError("[BattleHud] energyBar not assigned! Run Pawchinko/Build Battle HUD to rebuild.");
            if (popupLayer == null) Debug.LogError("[BattleHud] popupLayer not assigned! Run Pawchinko/Build Battle HUD to rebuild.");
            if (scorePopupTemplate == null) Debug.LogError("[BattleHud] scorePopupTemplate not assigned! Run Pawchinko/Build Battle HUD to rebuild.");
            if (hudCanvas == null) Debug.LogError("[BattleHud] hudCanvas not assigned! Run Pawchinko/Build Battle HUD to rebuild.");
            if (battleButtonFocusOutline == null) Debug.LogError("[BattleHud] battleButtonFocusOutline not assigned!");
            if (retreatButtonFocusOutline == null) Debug.LogError("[BattleHud] retreatButtonFocusOutline not assigned!");
            if (confirmAction == null) Debug.LogError("[BattleHud] confirmAction not assigned!");
            if (retreatAction == null) Debug.LogError("[BattleHud] retreatAction not assigned!");
            if (swapAction == null) Debug.LogError("[BattleHud] swapAction not assigned!");
            if (abilityAction == null) Debug.LogError("[BattleHud] abilityAction not assigned!");
            if (navigateAction == null) Debug.LogError("[BattleHud] navigateAction not assigned!");
        }

        private void OnDestroy()
        {
            if (eventSystem != null)
            {
                eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
                eventSystem.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
                eventSystem.Unsubscribe<BallScoredEvent>(OnBallScored);
                eventSystem.Unsubscribe<EnergyChangedEvent>(OnEnergyChanged);
                eventSystem.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            }

            UnwireAction(confirmAction, OnConfirmPressedCallback);
            UnwireAction(retreatAction, OnRetreatPressedCallback);
            UnwireAction(swapAction, OnSwapPressedCallback);
            UnwireAction(abilityAction, OnAbilityPressedCallback);
            if (navigateAction != null && navigateAction.action != null) navigateAction.action.Disable();
        }
    }
}
