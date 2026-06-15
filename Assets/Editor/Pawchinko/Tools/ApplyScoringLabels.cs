using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pawchinko.Editor.Tools
{
    /// <summary>
    /// Stamps the visible "Nx" multiplier labels onto every bucket Slot in Battle.unity and
    /// installs a power label child on Ball.prefab. Idempotent: re-running cleans up and
    /// re-creates the labels, so you can safely re-run after changing scoring values.
    ///
    /// Bucket multipliers are read from the live <see cref="ScoringManager"/> in the scene
    /// (via SerializedObject so we don't depend on the field being public). Run this menu
    /// any time you change <see cref="BoardScoringConfig.slotValues"/> so the on-board
    /// readout stays in sync with the scoring source of truth.
    /// </summary>
    public static class ApplyScoringLabels
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";
        private const string BallPrefabPath = "Assets/VisualAssets/Prefabs/Battle/Ball.prefab";
        // Pre-built outline material avoids instancing a fresh material per label (which
        // the standard outlineWidth setter does, leaking materials into the prefab).
        private const string OutlineMaterialPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";

        private const string SlotLabelChildName = "MultiplierLabel";
        private const string BallLabelChildName = "PowerLabel";

        // Slot label rides on the bucket front face. The BucketVisual is parented to the
        // slot at localY=0.472 with localScale (0.55,0.496,0.7). Bigger font + larger scale
        // so the text reads at a glance from across the board (matches the reference
        // Pachinko renderer where multipliers fill most of the slot width).
        private static readonly Vector3 SlotLabelLocalPos = new(0f, 0.47f, -0.42f);
        private const float SlotLabelFontSize = 8f;
        private const float SlotLabelScale = 0.35f;

        // Ball label floats above the ball. We DON'T use a local Y offset any more: instead
        // CameraFacingBillboard pins the label at ball.position + Vector3.up * worldYOffset
        // each frame, which means the label never tilts with a spinning ball and always sits
        // a fixed distance above it. Local position is kept at origin so the billboard owns
        // both position and rotation.
        private static readonly Vector3 BallLabelLocalPos = Vector3.zero;
        private const float BallLabelFontSize = 8f;
        private const float BallLabelScale = 1.6f;
        private const float BallLabelWorldYOffset = 0.6f;

        [MenuItem("Pawchinko/Apply Scoring Labels")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ApplyScoringLabels] Stop Play mode before running this menu - scene/prefab edits can't be saved during play.");
                return;
            }

            int slotsStamped = ApplySlotLabels();
            bool ballOk = ApplyBallLabel();

            Debug.Log($"[ApplyScoringLabels] Slots stamped: {slotsStamped}. Ball label: {(ballOk ? "ok" : "FAILED")}.");
        }

        // ---------- Slot labels (scene) ----------

        private static int ApplySlotLabels()
        {
            var scene = EnsureBattleSceneOpen();
            if (!scene.IsValid()) return 0;

            int[] slotValues = ReadSlotValuesFromScene(scene);
            int stamped = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var slot in root.GetComponentsInChildren<Slot>(true))
                {
                    // Skip the bottom void catcher (slotIndex < 0) - it never scores so it
                    // shouldn't advertise a multiplier.
                    if (slot.SlotIndex < 0) continue;

                    int value = (slot.SlotIndex >= 0 && slot.SlotIndex < slotValues.Length)
                        ? slotValues[slot.SlotIndex]
                        : 1;

                    EnsureSlotLabel(slot, value);
                    stamped++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return stamped;
        }

        private static void EnsureSlotLabel(Slot slot, int multiplier)
        {
            var existing = slot.transform.Find(SlotLabelChildName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject(SlotLabelChildName);
            go.transform.SetParent(slot.transform, false);
            go.transform.localPosition = SlotLabelLocalPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(SlotLabelScale, SlotLabelScale, SlotLabelScale);

            var tmp = go.AddComponent<TextMeshPro>();
            ConfigureBucketTmp(tmp);

            var labelHolder = go.AddComponent<SlotMultiplierLabel>();
            var so = new SerializedObject(labelHolder);
            so.FindProperty("label").objectReferenceValue = tmp;
            so.FindProperty("multiplier").intValue = multiplier;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ApplyText runs in OnValidate / OnEnable, but be explicit so the saved scene is
            // correct without needing a domain reload.
            tmp.text = $"{multiplier}\u00D7";

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(slot);
        }

        private static void ConfigureBucketTmp(TextMeshPro tmp)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = SlotLabelFontSize;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyOutlineMaterial(tmp);
            tmp.sortingOrder = 10;
        }

        /// <summary>
        /// Reads the live BoardScoringConfig from the ScoringManager in the scene. Falls
        /// back to the BoardScoringConfig defaults if no ScoringManager is found, so a
        /// freshly-built scene still labels correctly.
        /// </summary>
        private static int[] ReadSlotValuesFromScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var scoring = root.GetComponentInChildren<ScoringManager>(true);
                if (scoring == null) continue;

                var so = new SerializedObject(scoring);
                var configProp = so.FindProperty("scoring");
                if (configProp == null) continue;
                var arrayProp = configProp.FindPropertyRelative("slotValues");
                if (arrayProp == null || !arrayProp.isArray) continue;

                var values = new int[arrayProp.arraySize];
                for (int i = 0; i < arrayProp.arraySize; i++)
                    values[i] = arrayProp.GetArrayElementAtIndex(i).intValue;
                return values;
            }

            return new BoardScoringConfig().slotValues;
        }

        private static Scene EnsureBattleSceneOpen()
        {
            var open = SceneManager.GetSceneByPath(BattleScenePath);
            if (open.IsValid() && open.isLoaded) return open;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;
            return EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        }

        // ---------- Ball label (prefab) ----------

        private static bool ApplyBallLabel()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(BallPrefabPath);
            var root = scope.prefabContentsRoot;
            if (root == null)
            {
                Debug.LogError($"[ApplyScoringLabels] Could not load Ball prefab at {BallPrefabPath}");
                return false;
            }

            var ball = root.GetComponent<Ball>();
            if (ball == null)
            {
                Debug.LogError($"[ApplyScoringLabels] Ball prefab root is missing the Ball component.");
                return false;
            }

            var existing = root.transform.Find(BallLabelChildName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject(BallLabelChildName);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = BallLabelLocalPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(BallLabelScale, BallLabelScale, BallLabelScale);

            var tmp = go.AddComponent<TextMeshPro>();
            ConfigureBallTmp(tmp);

            // Billboard owns both rotation (face camera) and position (world-up offset above
            // the ball root). Following the ball in WORLD space keeps the label level even
            // when the ball spins violently from peg impacts.
            var billboard = go.AddComponent<CameraFacingBillboard>();
            var billboardSo = new SerializedObject(billboard);
            billboardSo.FindProperty("followTarget").objectReferenceValue = root.transform;
            billboardSo.FindProperty("worldYOffset").floatValue = BallLabelWorldYOffset;
            billboardSo.ApplyModifiedPropertiesWithoutUndo();

            var labelHolder = go.AddComponent<BallPowerLabel>();
            var so = new SerializedObject(labelHolder);
            so.FindProperty("label").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire the label into the Ball's serialized field so Init can drive it.
            var ballSo = new SerializedObject(ball);
            var powerLabelProp = ballSo.FindProperty("powerLabel");
            if (powerLabelProp != null)
            {
                powerLabelProp.objectReferenceValue = labelHolder;
                ballSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogError("[ApplyScoringLabels] Ball.powerLabel serialized field not found - did you forget to reimport scripts?");
                return false;
            }

            return true;
        }

        private static void ConfigureBallTmp(TextMeshPro tmp)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = BallLabelFontSize;
            tmp.color = Color.yellow;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyOutlineMaterial(tmp);
            tmp.sortingOrder = 20;
            tmp.text = "1\u00D7";
            // BallPowerLabel.SetPower will toggle visibility at Init based on the source
            // Pom's Power stat; default to visible so authoring previews still draw.
            tmp.gameObject.SetActive(true);
        }

        /// <summary>
        /// Assigns the shared LiberationSans SDF Outline material so the label has an
        /// outline without instancing a new material (the per-instance outlineWidth setter
        /// in TMP_Text instantiates renderer.material and leaks it into the prefab).
        /// </summary>
        private static void ApplyOutlineMaterial(TextMeshPro tmp)
        {
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (outlineMat == null)
            {
                Debug.LogWarning($"[ApplyScoringLabels] Outline material missing at {OutlineMaterialPath} - labels will render without outline.");
                return;
            }
            tmp.fontSharedMaterial = outlineMat;
        }
    }
}
