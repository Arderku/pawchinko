using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Pawchinko;

namespace PawchinkoEditor
{
    /// <summary>
    /// Rebuilds the Battle scene's UGUI HUD to match the pickup-style mockup: top round
    /// counter, per-side rosters split into Battle Zone (3 cards) + Bench Zone (2 cards), an
    /// ability picker beside the focused player active card, score + total readouts at
    /// bottom centre, BATTLE / RETREAT main buttons at bottom left, and Input System
    /// references wired to the Battle action map. Idempotent: clicking the menu again
    /// destroys the previous HUD GO and rebuilds it cleanly.
    /// </summary>
    public static class BuildBattleHud
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";
        private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string CanvasName = "Canvas";
        private const string HudRootName = "BattleHud";

        [MenuItem("Pawchinko/Build Battle HUD")]
        public static void Build()
        {
            var scene = EnsureBattleSceneOpen();
            if (!scene.IsValid())
            {
                Debug.LogError("[BuildBattleHud] Battle scene not loadable.");
                return;
            }

            var canvas = FindCanvas(scene);
            if (canvas == null)
            {
                Debug.LogError("[BuildBattleHud] Canvas root not found in Battle scene.");
                return;
            }

            DestroyExistingHud(canvas);

            var hudGo = new GameObject(HudRootName, typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            StretchToParent(hudGo.GetComponent<RectTransform>());

            var hud = hudGo.AddComponent<BattleHud>();

            BuildTopBar(hudGo, out var roundCounterText);
            BuildPlayerRoster(hudGo, out var playerCards, out var playerCardPortraits, out var playerAbilityPicker);
            BuildEnemyRoster(hudGo, out var enemyCards, out var enemyCardPortraits);
            BuildBottomBar(
                hudGo,
                out var battleButton, out var battleButtonLabel, out var battleButtonFocusOutline,
                out var retreatButton, out var retreatButtonFocusOutline,
                out var controlHintText,
                out var roundScoreText, out var roundTotalText,
                out var playerEnergyText, out var enemyEnergyText);
            BuildWinnerOverlay(hudGo, out var winnerOverlay, out var winnerText);

            if (!BuildPomPortraitStage(scene, playerCardPortraits, enemyCardPortraits, out var portraitStage))
            {
                Debug.LogError("[BuildBattleHud] Portrait stage build failed; aborting before scene save.");
                return;
            }

            var actions = LoadBattleActions();
            WireBattleHud(
                hud,
                battleButton, retreatButton, battleButtonLabel,
                battleButtonFocusOutline, retreatButtonFocusOutline,
                roundCounterText,
                playerCards, enemyCards,
                playerAbilityPicker,
                portraitStage,
                playerEnergyText, enemyEnergyText,
                roundScoreText, roundTotalText,
                winnerOverlay, winnerText,
                controlHintText,
                actions.confirm, actions.retreat, actions.swap, actions.ability, actions.navigate);

            WireUIManager(scene, hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildBattleHud] Rebuilt Battle HUD and saved scene.");
        }

        // ---------- Scene + Canvas helpers ----------

        private static Scene EnsureBattleSceneOpen()
        {
            var scene = SceneManager.GetSceneByPath(BattleScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!File.Exists(BattleScenePath))
                {
                    Debug.LogError($"[BuildBattleHud] Battle scene not found at {BattleScenePath}.");
                    return default;
                }
                scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            }
            return scene;
        }

        private static GameObject FindCanvas(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == CanvasName && root.GetComponent<Canvas>() != null) return root;
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas.gameObject;
            }
            return null;
        }

        private static void DestroyExistingHud(GameObject canvas)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (child.name == HudRootName) Object.DestroyImmediate(child.gameObject);
            }
        }

        // ---------- Top bar ----------

        private static void BuildTopBar(GameObject parent, out TMP_Text roundCounterText)
        {
            var bar = CreateUiObject("TopBar", parent.transform);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(400f, 50f);
            rt.anchoredPosition = new Vector2(0f, -16f);

            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0.92f, 0.92f, 0.94f, 1f);

            roundCounterText = CreateText("Round", bar.transform, "ROUND 1", 32f);
            StretchToParent(roundCounterText.rectTransform);
            roundCounterText.alignment = TextAlignmentOptions.Center;
            roundCounterText.color = Color.black;
        }

        // ---------- Player + Enemy rosters ----------

        private const float CardWidth = 220f;
        private const float CardHeight = 76f;
        private const float CardGap = 8f;
        private const float SectionGap = 14f;
        private const float RosterTopOffset = -90f;
        private const float RosterSideMargin = 20f;

        private static void BuildPlayerRoster(GameObject parent, out System.Collections.Generic.List<BattlePomCardView> cards, out System.Collections.Generic.List<RawImage> portraitImages, out AbilityPickerView abilityPicker)
        {
            BuildRoster(parent, "PlayerRoster", isPlayer: true, out cards, out portraitImages);

            // Ability picker is positioned to the RIGHT of the player's roster column.
            var pickerGo = new GameObject("PlayerAbilityPicker", typeof(RectTransform));
            pickerGo.transform.SetParent(parent.transform, false);
            var prt = pickerGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.sizeDelta = new Vector2(180f, CardHeight);
            prt.anchoredPosition = new Vector2(RosterSideMargin + CardWidth + 8f, RosterTopOffset);

            abilityPicker = BuildAbilityPicker(pickerGo);
        }

        private static void BuildEnemyRoster(GameObject parent, out System.Collections.Generic.List<BattlePomCardView> cards, out System.Collections.Generic.List<RawImage> portraitImages)
        {
            BuildRoster(parent, "EnemyRoster", isPlayer: false, out cards, out portraitImages);
        }

        private static void BuildRoster(GameObject parent, string name, bool isPlayer, out System.Collections.Generic.List<BattlePomCardView> cards, out System.Collections.Generic.List<RawImage> portraitImages)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(isPlayer ? 0f : 1f, 1f);
            rt.anchorMax = new Vector2(isPlayer ? 0f : 1f, 1f);
            rt.pivot = new Vector2(isPlayer ? 0f : 1f, 1f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight * 5 + CardGap * 4 + SectionGap);
            rt.anchoredPosition = new Vector2(isPlayer ? RosterSideMargin : -RosterSideMargin, RosterTopOffset);

            cards = new System.Collections.Generic.List<BattlePomCardView>(BattleManager.MaxRosterPoms);
            portraitImages = new System.Collections.Generic.List<RawImage>(BattleManager.MaxRosterPoms);

            CreateSectionLabel("BattleZoneLabel", root.transform, "BATTLE ZONE", 0f);

            float y = -20f;
            for (int i = 0; i < BattleManager.MaxActivePoms; i++)
            {
                cards.Add(BuildPomCard($"{name}_Card_{i}", root.transform, y, isActiveSlot: true, out var portrait));
                portraitImages.Add(portrait);
                y -= (CardHeight + CardGap);
            }
            y -= (SectionGap - CardGap);
            CreateSectionLabel("BenchZoneLabel", root.transform, "BENCH ZONE", y);
            y -= 20f;
            for (int i = BattleManager.MaxActivePoms; i < BattleManager.MaxRosterPoms; i++)
            {
                cards.Add(BuildPomCard($"{name}_Card_{i}", root.transform, y, isActiveSlot: false, out var portrait));
                portraitImages.Add(portrait);
                y -= (CardHeight + CardGap);
            }
        }

        private static void CreateSectionLabel(string goName, Transform parent, string text, float y)
        {
            var label = CreateText(goName, parent, text, 14f);
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 18f);
            rt.anchoredPosition = new Vector2(0f, y);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.4f, 0.4f, 0.45f);
            label.fontStyle = FontStyles.Bold;
        }

        private static BattlePomCardView BuildPomCard(string goName, Transform parent, float y, bool isActiveSlot, out RawImage portraitImage)
        {
            var card = new GameObject(goName, typeof(RectTransform));
            card.transform.SetParent(parent, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, CardHeight);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = card.AddComponent<Image>();
            bg.color = isActiveSlot ? new Color(0.97f, 0.97f, 0.98f) : new Color(0.92f, 0.92f, 0.93f);

            // Live 3D portrait: RawImage textured at build time by the matching PomPortraitSlot.
            var portrait = new GameObject("Portrait", typeof(RectTransform));
            portrait.transform.SetParent(card.transform, false);
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.sizeDelta = new Vector2(58f, 58f);
            prt.anchoredPosition = new Vector2(8f, 0f);
            portraitImage = portrait.AddComponent<RawImage>();
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = false;
            portraitImage.enabled = false;

            // Name text
            var nameText = CreateText("Name", card.transform, "POM_NAME", 14f);
            SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(74f, -6f), new Vector2(-90f, 18f));
            nameText.color = Color.black;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.fontStyle = FontStyles.Bold;

            // Level + Type row
            var levelText = CreateText("Level", card.transform, "LV 10", 11f);
            SetAnchored(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(74f, -26f), new Vector2(-12f, 16f));
            levelText.color = new Color(0.25f, 0.25f, 0.3f);
            levelText.alignment = TextAlignmentOptions.Left;

            var typeText = CreateText("Type", card.transform, "TYPE", 11f);
            SetAnchored(typeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -26f), new Vector2(-12f, 16f));
            typeText.color = new Color(0.25f, 0.25f, 0.3f);
            typeText.alignment = TextAlignmentOptions.Left;

            // Info row
            var infoText = CreateText("Info", card.transform, "BALLS x1", 11f);
            SetAnchored(infoText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(74f, 8f), new Vector2(-90f, 16f));
            infoText.color = new Color(0.35f, 0.35f, 0.4f);
            infoText.alignment = TextAlignmentOptions.Left;

            var focus = BuildFocusOutline(card.transform, thickness: 4f);

            var view = card.AddComponent<BattlePomCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("typeText").objectReferenceValue = typeText;
            so.FindProperty("infoText").objectReferenceValue = infoText;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImage;
            so.FindProperty("focusOutline").objectReferenceValue = focus;
            so.FindProperty("emptyState").objectReferenceValue = null;
            so.FindProperty("filledState").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        // ---------- Focus outline builder ----------

        private static readonly Color FocusOutlineColor = new Color(0.45f, 0.25f, 0.65f, 1f);

        /// <summary>
        /// Creates a 4-edge strip outline as a child of the given RectTransform. The outline
        /// GameObject is returned with active=false so callers can toggle it on focus.
        /// </summary>
        private static GameObject BuildFocusOutline(Transform parent, float thickness)
        {
            var outline = new GameObject("FocusOutline", typeof(RectTransform));
            outline.transform.SetParent(parent, false);
            var ort = outline.GetComponent<RectTransform>();
            StretchToParent(ort);

            BuildEdgeStrip(outline.transform, "EdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
            BuildEdgeStrip(outline.transform, "EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
            BuildEdgeStrip(outline.transform, "EdgeLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
            BuildEdgeStrip(outline.transform, "EdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));

            outline.SetActive(false);
            return outline;
        }

        private static void BuildEdgeStrip(Transform parent, string goName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = FocusOutlineColor;
            img.raycastTarget = false;
        }

        // ---------- Ability picker ----------

        private static AbilityPickerView BuildAbilityPicker(GameObject root)
        {
            var view = root.AddComponent<AbilityPickerView>();

            float optionHeight = 22f;
            float gap = 4f;
            float panelHeight = optionHeight * AbilityPickerView.OptionCount + gap * (AbilityPickerView.OptionCount - 1) + 12f;

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, panelHeight);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.96f, 0.96f, 0.97f);

            float y = -6f;
            var (noneLabel, noneHighlight) = BuildAbilityOption(root.transform, "OptionNone", "NONE", y, optionHeight);
            y -= (optionHeight + gap);
            var (slot1Label, slot1Highlight) = BuildAbilityOption(root.transform, "OptionSlot1", "ABILITY 1", y, optionHeight);
            y -= (optionHeight + gap);
            var (slot2Label, slot2Highlight) = BuildAbilityOption(root.transform, "OptionSlot2", "ABILITY 2", y, optionHeight);

            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("noneLabel").objectReferenceValue = noneLabel;
            so.FindProperty("slot1Label").objectReferenceValue = slot1Label;
            so.FindProperty("slot2Label").objectReferenceValue = slot2Label;
            so.FindProperty("noneHighlight").objectReferenceValue = noneHighlight;
            so.FindProperty("slot1Highlight").objectReferenceValue = slot1Highlight;
            so.FindProperty("slot2Highlight").objectReferenceValue = slot2Highlight;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
        }

        private static (TMP_Text label, GameObject highlight) BuildAbilityOption(Transform parent, string goName, string text, float y, float height)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-12f, height);
            rt.anchoredPosition = new Vector2(0f, y);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.86f, 0.86f, 0.88f);

            var highlight = new GameObject("Highlight", typeof(RectTransform));
            highlight.transform.SetParent(go.transform, false);
            var hrt = highlight.GetComponent<RectTransform>();
            StretchToParent(hrt);
            var hImg = highlight.AddComponent<Image>();
            hImg.color = new Color(0.45f, 0.25f, 0.65f, 0.35f);
            hImg.raycastTarget = false;
            highlight.SetActive(false);

            var label = CreateText("Label", go.transform, text, 12f);
            StretchToParent(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
            return (label, highlight);
        }

        // ---------- Bottom bar ----------

        private static void BuildBottomBar(GameObject parent,
            out Button battleButton, out TMP_Text battleButtonLabel, out GameObject battleButtonFocusOutline,
            out Button retreatButton, out GameObject retreatButtonFocusOutline,
            out TMP_Text controlHintText,
            out TMP_Text roundScoreText, out TMP_Text roundTotalText,
            out TMP_Text playerEnergyText, out TMP_Text enemyEnergyText)
        {
            // Battle + Retreat buttons (bottom-left stack)
            var actionStack = new GameObject("ActionStack", typeof(RectTransform));
            actionStack.transform.SetParent(parent.transform, false);
            var arRt = actionStack.GetComponent<RectTransform>();
            arRt.anchorMin = new Vector2(0f, 0f);
            arRt.anchorMax = new Vector2(0f, 0f);
            arRt.pivot = new Vector2(0f, 0f);
            arRt.sizeDelta = new Vector2(180f, 220f);
            arRt.anchoredPosition = new Vector2(RosterSideMargin, 40f);

            battleButton = BuildButton(actionStack.transform, "BattleButton", "BATTLE", new Vector2(0f, 120f), out battleButtonLabel, out battleButtonFocusOutline);
            retreatButton = BuildButton(actionStack.transform, "RetreatButton", "RETREAT", new Vector2(0f, 50f), out _, out retreatButtonFocusOutline);

            controlHintText = CreateText("ControlHint", actionStack.transform, "Y to swap\nX to ability", 11f);
            var hintRt = controlHintText.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(0f, 32f);
            hintRt.anchoredPosition = new Vector2(0f, 6f);
            controlHintText.alignment = TextAlignmentOptions.Center;
            controlHintText.color = new Color(0.3f, 0.3f, 0.35f);

            // Score readout bottom center
            var scoreStack = new GameObject("ScoreStack", typeof(RectTransform));
            scoreStack.transform.SetParent(parent.transform, false);
            var ssRt = scoreStack.GetComponent<RectTransform>();
            ssRt.anchorMin = new Vector2(0.5f, 0f);
            ssRt.anchorMax = new Vector2(0.5f, 0f);
            ssRt.pivot = new Vector2(0.5f, 0f);
            ssRt.sizeDelta = new Vector2(500f, 150f);
            ssRt.anchoredPosition = new Vector2(0f, 30f);

            var scoreBg = scoreStack.AddComponent<Image>();
            scoreBg.color = new Color(0.85f, 0.85f, 0.88f);

            roundScoreText = CreateText("RoundScore", scoreStack.transform, "0   0", 56f);
            var rsRt = roundScoreText.rectTransform;
            rsRt.anchorMin = new Vector2(0f, 1f);
            rsRt.anchorMax = new Vector2(1f, 1f);
            rsRt.pivot = new Vector2(0.5f, 1f);
            rsRt.sizeDelta = new Vector2(0f, 70f);
            rsRt.anchoredPosition = new Vector2(0f, -10f);
            roundScoreText.alignment = TextAlignmentOptions.Center;
            roundScoreText.color = Color.black;

            roundTotalText = CreateText("RoundTotal", scoreStack.transform, "0", 56f);
            var rtRt = roundTotalText.rectTransform;
            rtRt.anchorMin = new Vector2(0f, 0f);
            rtRt.anchorMax = new Vector2(1f, 0f);
            rtRt.pivot = new Vector2(0.5f, 0f);
            rtRt.sizeDelta = new Vector2(0f, 70f);
            rtRt.anchoredPosition = new Vector2(0f, 6f);
            roundTotalText.alignment = TextAlignmentOptions.Center;
            roundTotalText.color = Color.black;

            // Energy texts (small, under each side's roster)
            playerEnergyText = CreateText("PlayerEnergy", parent.transform, "ENERGY --", 14f);
            var peRt = playerEnergyText.rectTransform;
            peRt.anchorMin = new Vector2(0f, 1f);
            peRt.anchorMax = new Vector2(0f, 1f);
            peRt.pivot = new Vector2(0f, 1f);
            peRt.sizeDelta = new Vector2(CardWidth, 20f);
            peRt.anchoredPosition = new Vector2(RosterSideMargin, RosterTopOffset - (CardHeight + CardGap) * BattleManager.MaxRosterPoms - SectionGap - 30f);
            playerEnergyText.alignment = TextAlignmentOptions.Center;
            playerEnergyText.color = Color.black;

            enemyEnergyText = CreateText("EnemyEnergy", parent.transform, "ENERGY --", 14f);
            var eeRt = enemyEnergyText.rectTransform;
            eeRt.anchorMin = new Vector2(1f, 1f);
            eeRt.anchorMax = new Vector2(1f, 1f);
            eeRt.pivot = new Vector2(1f, 1f);
            eeRt.sizeDelta = new Vector2(CardWidth, 20f);
            eeRt.anchoredPosition = new Vector2(-RosterSideMargin, RosterTopOffset - (CardHeight + CardGap) * BattleManager.MaxRosterPoms - SectionGap - 30f);
            enemyEnergyText.alignment = TextAlignmentOptions.Center;
            enemyEnergyText.color = Color.black;
        }

        private static Button BuildButton(Transform parent, string goName, string label, Vector2 anchoredPos, out TMP_Text labelText, out GameObject focusOutline)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 60f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.85f, 0.85f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            labelText = CreateText("Label", go.transform, label, 22f);
            StretchToParent(labelText.rectTransform);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.black;
            labelText.fontStyle = FontStyles.Bold;

            focusOutline = BuildFocusOutline(go.transform, thickness: 4f);

            return btn;
        }

        // ---------- Winner overlay ----------

        private static void BuildWinnerOverlay(GameObject parent, out GameObject overlay, out TMP_Text text)
        {
            overlay = new GameObject("WinnerOverlay", typeof(RectTransform));
            overlay.transform.SetParent(parent.transform, false);
            StretchToParent(overlay.GetComponent<RectTransform>());

            var bg = overlay.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            text = CreateText("WinnerText", overlay.transform, "WINNER", 80f);
            var trt = text.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(800f, 120f);
            trt.anchoredPosition = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;

            overlay.SetActive(false);
        }

        // ---------- Portrait stage (off-screen 3D portraits) ----------

        private const string PortraitStageName = "PomPortraitStage";
        private const float PortraitSlotSpacing = 10f;
        private const float PortraitEnemyYOffset = 50f;
        private const float PortraitStageWorldY = -10000f;
        private const float PortraitCameraDistance = 2f;
        private const float PortraitCameraOrthoSize = 0.6f;

        // Spawn anchor transform applied to every slot. Tweak these to reframe all portraits at
        // once. The anchor sits at the centre of the portrait camera's view; the Pom prefab is
        // spawned as a child of the anchor with identity local TRS, so the anchor *is* the
        // framing knob. Rotation is split per side so player + enemy Poms face the right way
        // (each towards their opponent's board).
        private static readonly Vector3 PortraitAnchorLocalPosition = new Vector3(-0.01f, -0.6f, 0f);
        private static readonly Vector3 PortraitAnchorPlayerEuler = new Vector3(0f, 241.79f, 0f);
        private static readonly Vector3 PortraitAnchorEnemyEuler = new Vector3(0f, -67.62f, 0f);
        private static readonly Vector3 PortraitAnchorLocalScale = new Vector3(2f, 2f, 2f);

        /// <summary>
        /// Builds the off-screen <see cref="PomPortraitStage"/> for the Battle scene. One
        /// <see cref="Camera"/> + <see cref="RenderTexture"/> per card slot (5 player + 5
        /// enemy = 10). Cameras isolate the PomPortrait layer via culling mask so the main
        /// + UI cameras must exclude PomPortrait themselves. Returns false if the required
        /// PomPortrait layer is missing.
        /// </summary>
        private static bool BuildPomPortraitStage(Scene scene, System.Collections.Generic.List<RawImage> playerImages, System.Collections.Generic.List<RawImage> enemyImages, out PomPortraitStage stage)
        {
            stage = null;
            int portraitLayer = LayerMask.NameToLayer(PomPortraitSlot.PortraitLayerName);
            if (portraitLayer < 0)
            {
                Debug.LogError($"[BuildBattleHud] Layer '{PomPortraitSlot.PortraitLayerName}' missing. Add it via Project Settings > Tags and Layers before running this menu.");
                return false;
            }

            DestroyExistingPortraitStage(scene);
            ExcludePortraitLayerFromOtherCameras(scene, portraitLayer);

            var stageGo = new GameObject(PortraitStageName);
            SceneManager.MoveGameObjectToScene(stageGo, scene);
            stageGo.transform.position = new Vector3(0f, PortraitStageWorldY, 0f);

            stage = stageGo.AddComponent<PomPortraitStage>();

            var playerSlots = new System.Collections.Generic.List<PomPortraitSlot>(BattleManager.MaxRosterPoms);
            var enemySlots = new System.Collections.Generic.List<PomPortraitSlot>(BattleManager.MaxRosterPoms);

            for (int i = 0; i < BattleManager.MaxRosterPoms; i++)
            {
                playerSlots.Add(BuildPortraitSlot(stageGo.transform, $"PlayerSlot_{i}", new Vector3(i * PortraitSlotSpacing, 0f, 0f), portraitLayer, PortraitAnchorPlayerEuler, i < playerImages.Count ? playerImages[i] : null));
                enemySlots.Add(BuildPortraitSlot(stageGo.transform, $"EnemySlot_{i}", new Vector3(i * PortraitSlotSpacing, PortraitEnemyYOffset, 0f), portraitLayer, PortraitAnchorEnemyEuler, i < enemyImages.Count ? enemyImages[i] : null));
            }

            var so = new SerializedObject(stage);
            SetListReferences(so.FindProperty("playerSlots"), playerSlots.ConvertAll(s => (Object)s));
            SetListReferences(so.FindProperty("enemySlots"), enemySlots.ConvertAll(s => (Object)s));
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void DestroyExistingPortraitStage(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == PortraitStageName) Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Removes the PomPortrait layer from every other camera's culling mask in the scene
        /// so only the portrait cameras can see the portrait Poms.
        /// </summary>
        private static void ExcludePortraitLayerFromOtherCameras(Scene scene, int portraitLayer)
        {
            int mask = 1 << portraitLayer;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    if ((cam.cullingMask & mask) == 0) continue;
                    cam.cullingMask &= ~mask;
                    EditorUtility.SetDirty(cam);
                }
            }
        }

        private static PomPortraitSlot BuildPortraitSlot(Transform stageRoot, string goName, Vector3 localPosition, int portraitLayer, Vector3 anchorEuler, RawImage targetImage)
        {
            var slotGo = new GameObject(goName);
            slotGo.transform.SetParent(stageRoot, false);
            slotGo.transform.localPosition = localPosition;
            slotGo.layer = portraitLayer;

            var camGo = new GameObject("PortraitCamera");
            camGo.transform.SetParent(slotGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -PortraitCameraDistance);
            camGo.transform.localRotation = Quaternion.identity;
            camGo.layer = portraitLayer;

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = PortraitCameraOrthoSize;
            cam.cullingMask = 1 << portraitLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.95f, 0.97f, 0f);
            cam.depth = -10f;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 10f;

            var anchorGo = new GameObject("SpawnAnchor");
            anchorGo.transform.SetParent(slotGo.transform, false);
            anchorGo.transform.localPosition = PortraitAnchorLocalPosition;
            anchorGo.transform.localRotation = Quaternion.Euler(anchorEuler);
            anchorGo.transform.localScale = PortraitAnchorLocalScale;
            anchorGo.layer = portraitLayer;

            var slot = slotGo.AddComponent<PomPortraitSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("portraitCamera").objectReferenceValue = cam;
            so.FindProperty("spawnAnchor").objectReferenceValue = anchorGo.transform;
            so.FindProperty("targetImage").objectReferenceValue = targetImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        // ---------- Input action references ----------

        private struct BattleInputRefs
        {
            public InputActionReference confirm;
            public InputActionReference retreat;
            public InputActionReference swap;
            public InputActionReference ability;
            public InputActionReference navigate;
        }

        private static BattleInputRefs LoadBattleActions()
        {
            var refs = new BattleInputRefs();
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
            if (asset == null)
            {
                Debug.LogError($"[BuildBattleHud] InputActionAsset not found at {InputActionsAssetPath}.");
                return refs;
            }

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsAssetPath);
            foreach (var sub in subAssets)
            {
                if (sub is InputActionReference reference && reference.action != null)
                {
                    if (reference.action.actionMap == null || reference.action.actionMap.name != "Battle") continue;
                    switch (reference.action.name)
                    {
                        case "Confirm": refs.confirm = reference; break;
                        case "Retreat": refs.retreat = reference; break;
                        case "Swap": refs.swap = reference; break;
                        case "Ability": refs.ability = reference; break;
                        case "Navigate": refs.navigate = reference; break;
                    }
                }
            }

            if (refs.confirm == null) Debug.LogError("[BuildBattleHud] Confirm InputActionReference not found - reimport the .inputactions asset.");
            if (refs.retreat == null) Debug.LogError("[BuildBattleHud] Retreat InputActionReference not found.");
            if (refs.swap == null) Debug.LogError("[BuildBattleHud] Swap InputActionReference not found.");
            if (refs.ability == null) Debug.LogError("[BuildBattleHud] Ability InputActionReference not found.");
            if (refs.navigate == null) Debug.LogError("[BuildBattleHud] Navigate InputActionReference not found.");
            return refs;
        }

        // ---------- BattleHud serialized-field wiring ----------

        private static void WireBattleHud(
            BattleHud hud,
            Button battleButton, Button retreatButton, TMP_Text battleButtonLabel,
            GameObject battleButtonFocusOutline, GameObject retreatButtonFocusOutline,
            TMP_Text roundCounterText,
            System.Collections.Generic.List<BattlePomCardView> playerCards,
            System.Collections.Generic.List<BattlePomCardView> enemyCards,
            AbilityPickerView playerAbilityPicker,
            PomPortraitStage portraitStage,
            TMP_Text playerEnergyText, TMP_Text enemyEnergyText,
            TMP_Text roundScoreText, TMP_Text roundTotalText,
            GameObject winnerOverlay, TMP_Text winnerText,
            TMP_Text controlHintText,
            InputActionReference confirm, InputActionReference retreat, InputActionReference swap, InputActionReference ability, InputActionReference navigate)
        {
            var so = new SerializedObject(hud);
            so.FindProperty("battleButton").objectReferenceValue = battleButton;
            so.FindProperty("retreatButton").objectReferenceValue = retreatButton;
            so.FindProperty("battleButtonLabel").objectReferenceValue = battleButtonLabel;
            so.FindProperty("battleButtonFocusOutline").objectReferenceValue = battleButtonFocusOutline;
            so.FindProperty("retreatButtonFocusOutline").objectReferenceValue = retreatButtonFocusOutline;
            so.FindProperty("roundCounterText").objectReferenceValue = roundCounterText;

            SetListReferences(so.FindProperty("playerCards"), playerCards.ConvertAll(c => (Object)c));
            SetListReferences(so.FindProperty("enemyCards"), enemyCards.ConvertAll(c => (Object)c));

            so.FindProperty("playerAbilityPicker").objectReferenceValue = playerAbilityPicker;
            so.FindProperty("portraitStage").objectReferenceValue = portraitStage;
            so.FindProperty("playerEnergyText").objectReferenceValue = playerEnergyText;
            so.FindProperty("enemyEnergyText").objectReferenceValue = enemyEnergyText;
            so.FindProperty("roundScoreText").objectReferenceValue = roundScoreText;
            so.FindProperty("roundTotalText").objectReferenceValue = roundTotalText;
            so.FindProperty("winnerOverlay").objectReferenceValue = winnerOverlay;
            so.FindProperty("winnerText").objectReferenceValue = winnerText;
            so.FindProperty("controlHintText").objectReferenceValue = controlHintText;

            so.FindProperty("confirmAction").objectReferenceValue = confirm;
            so.FindProperty("retreatAction").objectReferenceValue = retreat;
            so.FindProperty("swapAction").objectReferenceValue = swap;
            so.FindProperty("abilityAction").objectReferenceValue = ability;
            so.FindProperty("navigateAction").objectReferenceValue = navigate;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetListReferences(SerializedProperty listProp, System.Collections.Generic.List<Object> items)
        {
            listProp.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        // ---------- UIManager wiring ----------

        private static void WireUIManager(Scene scene, BattleHud hud)
        {
            UIManager uiManager = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                uiManager = root.GetComponentInChildren<UIManager>(true);
                if (uiManager != null) break;
            }
            if (uiManager == null)
            {
                Debug.LogWarning("[BuildBattleHud] UIManager not found in scene - BattleHud will not auto-initialize.");
                return;
            }

            var so = new SerializedObject(uiManager);
            so.FindProperty("battleHud").objectReferenceValue = hud;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Misc UI helpers ----------

        private static GameObject CreateUiObject(string goName, Transform parent)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TMP_Text CreateText(string goName, Transform parent, string text, float fontSize)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
