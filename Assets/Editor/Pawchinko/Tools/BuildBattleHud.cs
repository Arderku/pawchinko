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
                out var controlHintText);
            BuildEnergyBar(hudGo, out var energyBar);
            BuildPopupLayer(hudGo, out var popupLayer, out var scorePopupTemplate);
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
                energyBar, popupLayer, scorePopupTemplate, canvas.GetComponent<Canvas>(),
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

        private const float CardWidth = 300f;
        private const float CardHeight = 110f;
        private const float CardGap = 10f;
        private const float SectionGap = 16f;
        private const float RosterTopOffset = -90f;
        private const float RosterSideMargin = 24f;

        // Portrait sub-layout inside a card.
        private const float CardPortraitSize = 90f;
        private const float CardPortraitMargin = 10f;
        private const float CardTextInset = CardPortraitMargin + CardPortraitSize + 10f; // text edge inset on the portrait side

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
                cards.Add(BuildPomCard($"{name}_Card_{i}", root.transform, y, isActiveSlot: true, mirrored: !isPlayer, out var portrait));
                portraitImages.Add(portrait);
                y -= (CardHeight + CardGap);
            }
            y -= (SectionGap - CardGap);
            CreateSectionLabel("BenchZoneLabel", root.transform, "BENCH ZONE", y);
            y -= 20f;
            for (int i = BattleManager.MaxActivePoms; i < BattleManager.MaxRosterPoms; i++)
            {
                cards.Add(BuildPomCard($"{name}_Card_{i}", root.transform, y, isActiveSlot: false, mirrored: !isPlayer, out var portrait));
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

        private static BattlePomCardView BuildPomCard(string goName, Transform parent, float y, bool isActiveSlot, bool mirrored, out RawImage portraitImage)
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

            // Mirrored layout (enemy side): portrait sits on the RIGHT, text block on the LEFT.
            // Non-mirrored (player side): portrait on the LEFT, text block on the RIGHT.
            float portraitAnchorX = mirrored ? 1f : 0f;
            float portraitX = mirrored ? -CardPortraitMargin : CardPortraitMargin;
            var textAlign = mirrored ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;

            // Text block inset from each card edge.
            float textLeftInset = mirrored ? 10f : CardTextInset;
            float textRightInset = mirrored ? CardTextInset : 10f;
            float textBlockMidX = (textLeftInset - textRightInset) * 0.5f; // anchored at card-center

            // Live 3D portrait: RawImage textured at build time by the matching PomPortraitSlot.
            var portrait = new GameObject("Portrait", typeof(RectTransform));
            portrait.transform.SetParent(card.transform, false);
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(portraitAnchorX, 0.5f);
            prt.anchorMax = new Vector2(portraitAnchorX, 0.5f);
            prt.pivot = new Vector2(portraitAnchorX, 0.5f);
            prt.sizeDelta = new Vector2(CardPortraitSize, CardPortraitSize);
            prt.anchoredPosition = new Vector2(portraitX, 0f);
            portraitImage = portrait.AddComponent<RawImage>();
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = false;
            portraitImage.enabled = false;

            // Name text (top row, full width of the text block).
            var nameText = CreateText("Name", card.transform, "POM_NAME", 18f);
            SetAnchored(nameText.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(textBlockMidX, -8f), new Vector2(-(textLeftInset + textRightInset), 22f));
            nameText.color = Color.black;
            nameText.alignment = textAlign;
            nameText.fontStyle = FontStyles.Bold;

            // Level + Type row: each is anchored to one edge of the text block.
            //   Player (non-mirrored): Level pinned to text-block LEFT edge (next to portrait),
            //                          Type  pinned to text-block RIGHT edge (card edge).
            //   Enemy  (mirrored):     Level pinned to text-block RIGHT edge (next to portrait),
            //                          Type  pinned to text-block LEFT edge (card edge).
            // Text-block width is (CardWidth - textLeftInset - textRightInset) = 180 in both layouts.
            // Each half therefore gets up to 90px; use 85 to leave a small visual gap in the middle.
            const float HalfRowWidth = 85f;

            var levelText = CreateText("Level", card.transform, "LV 10", 13f);
            if (mirrored)
            {
                SetAnchored(levelText.rectTransform,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-textRightInset, -34f), new Vector2(HalfRowWidth, 18f));
            }
            else
            {
                SetAnchored(levelText.rectTransform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(textLeftInset, -34f), new Vector2(HalfRowWidth, 18f));
            }
            levelText.color = new Color(0.25f, 0.25f, 0.3f);
            levelText.alignment = textAlign;

            var typeText = CreateText("Type", card.transform, "TYPE", 13f);
            if (mirrored)
            {
                SetAnchored(typeText.rectTransform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(textLeftInset, -34f), new Vector2(HalfRowWidth, 18f));
            }
            else
            {
                SetAnchored(typeText.rectTransform,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-textRightInset, -34f), new Vector2(HalfRowWidth, 18f));
            }
            typeText.color = new Color(0.25f, 0.25f, 0.3f);
            typeText.alignment = textAlign;

            // Info row (bottom of the text block, full width).
            var infoText = CreateText("Info", card.transform, "BALLS x1", 13f);
            SetAnchored(infoText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(textBlockMidX, 10f), new Vector2(-(textLeftInset + textRightInset), 18f));
            infoText.color = new Color(0.35f, 0.35f, 0.4f);
            infoText.alignment = textAlign;

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
            out TMP_Text controlHintText)
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
        }

        private const float EnergyBarWidth = 820f;
        private const float EnergyBarHeight = 38f;

        private static void BuildEnergyBar(GameObject parent, out TugOfWarBar energyBar)
        {
            // Container sits at bottom-center, just above the very bottom of the screen.
            var container = new GameObject("EnergyBar", typeof(RectTransform));
            container.transform.SetParent(parent.transform, false);
            var crt = container.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.sizeDelta = new Vector2(EnergyBarWidth, EnergyBarHeight + 36f);
            crt.anchoredPosition = new Vector2(0f, 26f);

            // Track (the actual bar). Background frame + the two color fills + center marker.
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(container.transform, false);
            var trt = track.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0f);
            trt.anchorMax = new Vector2(0.5f, 0f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.sizeDelta = new Vector2(EnergyBarWidth, EnergyBarHeight);
            trt.anchoredPosition = new Vector2(0f, 0f);
            track.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.85f);

            var playerFill = new GameObject("PlayerFill", typeof(RectTransform), typeof(Image));
            playerFill.transform.SetParent(track.transform, false);
            var pfImg = playerFill.GetComponent<Image>();
            pfImg.color = new Color(0.25f, 0.55f, 0.95f);
            var pfRt = playerFill.GetComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0f, 0f);
            pfRt.anchorMax = new Vector2(0f, 1f);
            pfRt.pivot = new Vector2(0f, 0.5f);
            pfRt.sizeDelta = new Vector2(EnergyBarWidth * 0.5f, 0f);
            pfRt.anchoredPosition = new Vector2(0f, 0f);

            var enemyFill = new GameObject("EnemyFill", typeof(RectTransform), typeof(Image));
            enemyFill.transform.SetParent(track.transform, false);
            var efImg = enemyFill.GetComponent<Image>();
            efImg.color = new Color(0.95f, 0.3f, 0.35f);
            var efRt = enemyFill.GetComponent<RectTransform>();
            efRt.anchorMin = new Vector2(1f, 0f);
            efRt.anchorMax = new Vector2(1f, 1f);
            efRt.pivot = new Vector2(1f, 0.5f);
            efRt.sizeDelta = new Vector2(EnergyBarWidth * 0.5f, 0f);
            efRt.anchoredPosition = new Vector2(0f, 0f);

            var marker = new GameObject("Marker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(track.transform, false);
            marker.GetComponent<Image>().color = new Color(1f, 0.95f, 0.4f);
            var mrt = marker.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0f, 0.5f);
            mrt.anchorMax = new Vector2(0f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(6f, EnergyBarHeight + 14f);
            mrt.anchoredPosition = new Vector2(EnergyBarWidth * 0.5f, 0f);

            // Numeric labels above the track on each side
            var playerLabel = CreateText("PlayerLabel", container.transform, "0", 22f);
            playerLabel.color = new Color(0.85f, 0.92f, 1f);
            playerLabel.fontStyle = FontStyles.Bold;
            playerLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var plRt = playerLabel.rectTransform;
            plRt.anchorMin = new Vector2(0f, 0f);
            plRt.anchorMax = new Vector2(0f, 0f);
            plRt.pivot = new Vector2(0f, 0f);
            plRt.sizeDelta = new Vector2(180f, 28f);
            plRt.anchoredPosition = new Vector2(8f, EnergyBarHeight + 4f);

            var enemyLabel = CreateText("EnemyLabel", container.transform, "0", 22f);
            enemyLabel.color = new Color(1f, 0.88f, 0.88f);
            enemyLabel.fontStyle = FontStyles.Bold;
            enemyLabel.alignment = TextAlignmentOptions.MidlineRight;
            var elRt = enemyLabel.rectTransform;
            elRt.anchorMin = new Vector2(1f, 0f);
            elRt.anchorMax = new Vector2(1f, 0f);
            elRt.pivot = new Vector2(1f, 0f);
            elRt.sizeDelta = new Vector2(180f, 28f);
            elRt.anchoredPosition = new Vector2(-8f, EnergyBarHeight + 4f);

            // Component wiring
            energyBar = container.AddComponent<TugOfWarBar>();
            var so = new SerializedObject(energyBar);
            so.FindProperty("track").objectReferenceValue = trt;
            so.FindProperty("playerFill").objectReferenceValue = pfImg;
            so.FindProperty("enemyFill").objectReferenceValue = efImg;
            so.FindProperty("marker").objectReferenceValue = mrt;
            so.FindProperty("playerLabel").objectReferenceValue = playerLabel;
            so.FindProperty("enemyLabel").objectReferenceValue = enemyLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPopupLayer(GameObject parent, out RectTransform popupLayer, out ScorePopup template)
        {
            // Full-canvas-sized layer that is the LAST sibling so popups render on top of the
            // cards/bar but below the winner overlay (winner overlay is added after).
            var go = new GameObject("PopupLayer", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            popupLayer = go.GetComponent<RectTransform>();
            StretchToParent(popupLayer);
            // Don't intercept clicks - popups are decorative.
            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var templateGo = new GameObject("ScorePopupTemplate", typeof(RectTransform), typeof(CanvasGroup));
            templateGo.transform.SetParent(popupLayer, false);
            var trt = templateGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(140f, 60f);

            var label = CreateText("Label", templateGo.transform, "+0", 42f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            template = templateGo.AddComponent<ScorePopup>();
            var so = new SerializedObject(template);
            var labelProp = so.FindProperty("label");
            if (labelProp != null) labelProp.objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Template stays disabled - we Instantiate() and SetActive(true) per popup.
            templateGo.SetActive(false);
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
        // Perspective FoV. 35° at 2-unit distance ≈ 1.26 units tall view, matching the previous
        // orthographic framing while giving a proper 3D look.
        private const float PortraitCameraFieldOfView = 35f;

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
        /// shared <see cref="Camera"/> + a single atlas <see cref="RenderTexture"/> serves
        /// every card slot (5 player + 5 enemy = 10 cells of one atlas). Per-slot framing
        /// is preserved by giving each slot its own CameraAnchor that the stage teleports
        /// the shared camera to before rendering that cell. The shared camera isolates the
        /// PomPortrait layer via culling mask, so the main + UI cameras must exclude that
        /// layer themselves. Returns false if the PomPortrait layer is missing.
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

            // Single shared camera, child of the stage. The stage repositions it per-render.
            var sharedCam = BuildSharedPortraitCamera(stageGo.transform, portraitLayer);

            var playerSlots = new System.Collections.Generic.List<PomPortraitSlot>(BattleManager.MaxRosterPoms);
            var enemySlots = new System.Collections.Generic.List<PomPortraitSlot>(BattleManager.MaxRosterPoms);

            for (int i = 0; i < BattleManager.MaxRosterPoms; i++)
            {
                playerSlots.Add(BuildPortraitSlot(stageGo.transform, $"PlayerSlot_{i}", new Vector3(i * PortraitSlotSpacing, 0f, 0f), portraitLayer, PortraitAnchorPlayerEuler, i < playerImages.Count ? playerImages[i] : null));
                enemySlots.Add(BuildPortraitSlot(stageGo.transform, $"EnemySlot_{i}", new Vector3(i * PortraitSlotSpacing, PortraitEnemyYOffset, 0f), portraitLayer, PortraitAnchorEnemyEuler, i < enemyImages.Count ? enemyImages[i] : null));
            }

            var so = new SerializedObject(stage);
            so.FindProperty("sharedCamera").objectReferenceValue = sharedCam;
            SetListReferences(so.FindProperty("playerSlots"), playerSlots.ConvertAll(s => (Object)s));
            SetListReferences(so.FindProperty("enemySlots"), enemySlots.ConvertAll(s => (Object)s));
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>
        /// Builds the single shared portrait camera, child of the stage. Initial pose is
        /// irrelevant - the stage repositions it per-render. Disabled by default so the
        /// pipeline does not render it during the normal camera pass; the stage drives
        /// Camera.Render() manually from LateUpdate.
        /// </summary>
        private static Camera BuildSharedPortraitCamera(Transform stageRoot, int portraitLayer)
        {
            var camGo = new GameObject("SharedPortraitCamera");
            camGo.transform.SetParent(stageRoot, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            camGo.layer = portraitLayer;

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = PortraitCameraFieldOfView;
            cam.cullingMask = 1 << portraitLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.95f, 0.97f, 0f);
            cam.depth = -10f;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 10f;
            cam.enabled = false;
            return cam;
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

        /// <summary>
        /// Builds one portrait slot. A slot owns two empty Transforms:
        /// * <c>SpawnAnchor</c> - where the Pom prefab is instantiated; pose this to frame
        ///   the Pom inside its portrait.
        /// * <c>CameraAnchor</c> - the pose the shared camera teleports to before rendering
        ///   this slot. Placed at <c>(0, 0, -PortraitCameraDistance)</c> in the slot's local
        ///   space so the camera looks down its -Z toward the SpawnAnchor, matching the
        ///   previous per-slot-camera framing exactly.
        /// </summary>
        private static PomPortraitSlot BuildPortraitSlot(Transform stageRoot, string goName, Vector3 localPosition, int portraitLayer, Vector3 anchorEuler, RawImage targetImage)
        {
            var slotGo = new GameObject(goName);
            slotGo.transform.SetParent(stageRoot, false);
            slotGo.transform.localPosition = localPosition;
            slotGo.layer = portraitLayer;

            var anchorGo = new GameObject("SpawnAnchor");
            anchorGo.transform.SetParent(slotGo.transform, false);
            anchorGo.transform.localPosition = PortraitAnchorLocalPosition;
            anchorGo.transform.localRotation = Quaternion.Euler(anchorEuler);
            anchorGo.transform.localScale = PortraitAnchorLocalScale;
            anchorGo.layer = portraitLayer;

            var camAnchorGo = new GameObject("CameraAnchor");
            camAnchorGo.transform.SetParent(slotGo.transform, false);
            camAnchorGo.transform.localPosition = new Vector3(0f, 0f, -PortraitCameraDistance);
            camAnchorGo.transform.localRotation = Quaternion.identity;
            camAnchorGo.layer = portraitLayer;

            var slot = slotGo.AddComponent<PomPortraitSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("spawnAnchor").objectReferenceValue = anchorGo.transform;
            so.FindProperty("cameraAnchor").objectReferenceValue = camAnchorGo.transform;
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
            TugOfWarBar energyBar, RectTransform popupLayer, ScorePopup scorePopupTemplate, Canvas hudCanvas,
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
            so.FindProperty("energyBar").objectReferenceValue = energyBar;
            so.FindProperty("popupLayer").objectReferenceValue = popupLayer;
            so.FindProperty("scorePopupTemplate").objectReferenceValue = scorePopupTemplate;
            so.FindProperty("hudCanvas").objectReferenceValue = hudCanvas;
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
