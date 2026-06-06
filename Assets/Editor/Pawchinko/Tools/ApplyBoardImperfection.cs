using System.Collections.Generic;
using Pawchinko;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pawchinko.Editor.Tools
{
    /// <summary>
    /// One-shot editor utility that makes the battle boards behave like real pachinko boards.
    ///
    /// Real plinko / pachinko boards prevent stuck balls AND prevent escape through:
    ///   * imperfect pin posts (slight position + radius variance)
    ///   * a solid frame around the play area (back glass, left/right rails, top frame)
    ///   * a catch-all under the buckets so any miss is still removed from play
    ///
    /// This menu applies all of the above to every board in the Battle scene, deterministically
    /// (peg jitter is seeded by row/col so the same scene reproduces the same layout).
    ///
    /// Together with the Ball Rigidbody's Z-position constraint (set on the prefab), the ball is
    /// physically incapable of leaving the playable plane: it can only move in X-Y, can only
    /// exit through a bucket trigger, and the bottom catcher despawns anything that somehow
    /// slips past every slot.
    /// </summary>
    public static class ApplyBoardImperfection
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";

        // --- Peg imperfection ---
        private const float PegPositionJitter = 0.008f;   // ±8 mm in X and Y
        private const float PegRadiusJitterPct = 0.025f;  // ±2.5%

        // --- Frame ---
        private const string LeftRailName = "Frame_LeftRail";
        private const string RightRailName = "Frame_RightRail";
        private const string TopRailName = "Frame_TopRail";
        private const string BottomCatcherName = "Frame_BottomCatcher";
        private const float RailThickness = 0.10f;       // 10 cm thick walls
        private const float RailMargin = 0.25f;          // 25 cm beyond outermost peg
        private const float BottomCatcherDrop = 1.5f;    // 1.5 m below lowest slot/peg

        // --- Legacy cleanup ---
        private static readonly string[] ObsoleteChildren = { "ZWall_Front", "ZWall_Back" };

        [MenuItem("Pawchinko/Apply Board Imperfection")]
        public static void Apply()
        {
            var scene = EnsureBattleSceneOpen();
            if (!scene.IsValid()) return;

            int pegCount = 0;
            int radiusCount = 0;
            int framesProcessed = 0;
            int obsoleteRemoved = 0;

            // 1) Peg jitter (deterministic per peg).
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var peg in root.GetComponentsInChildren<Peg>(true))
                {
                    int seed = unchecked(peg.Row * 73856093 ^ peg.Col * 19349663);
                    var rng = new System.Random(seed);
                    float dx = ((float)rng.NextDouble() * 2f - 1f) * PegPositionJitter;
                    float dy = ((float)rng.NextDouble() * 2f - 1f) * PegPositionJitter;
                    float dr = 1f + (((float)rng.NextDouble() * 2f - 1f) * PegRadiusJitterPct);

                    var t = peg.transform;
                    var lp = t.localPosition;
                    lp.x += dx;
                    lp.y += dy;
                    t.localPosition = lp;
                    pegCount++;

                    var sc = peg.GetComponent<SphereCollider>();
                    if (sc != null)
                    {
                        sc.radius *= dr;
                        EditorUtility.SetDirty(sc);
                        radiusCount++;
                    }
                    EditorUtility.SetDirty(peg);
                }
            }

            // 2) Discover boards. Each Peg.transform's "Board"-named ancestor counts as a board.
            var boards = new HashSet<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var peg in root.GetComponentsInChildren<Peg>(true))
                {
                    var boardRoot = FindBoardRoot(peg.transform);
                    if (boardRoot != null) boards.Add(boardRoot.gameObject);
                }
            }

            // 3) For each board: tear out obsolete Z walls, then build a proper frame sized to
            //    the actual peg + slot extents in that board.
            foreach (var board in boards)
            {
                obsoleteRemoved += RemoveObsoleteChildren(board);
                BuildFrame(board);
                framesProcessed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[ApplyBoardImperfection] Pegs jittered: {pegCount} ({radiusCount} radii). Frames built: {framesProcessed}. Obsolete children removed: {obsoleteRemoved}.");
        }

        /// <summary>
        /// Walks up the hierarchy from a peg until it finds an ancestor whose name contains
        /// "Board" (PlayerBoard / EnemyBoard convention).
        /// </summary>
        private static Transform FindBoardRoot(Transform pegTransform)
        {
            var t = pegTransform;
            while (t != null)
            {
                if (t.name.IndexOf("Board", System.StringComparison.OrdinalIgnoreCase) >= 0) return t;
                t = t.parent;
            }
            return pegTransform.parent != null ? pegTransform.parent.parent : null;
        }

        private static int RemoveObsoleteChildren(GameObject board)
        {
            int removed = 0;
            foreach (var name in ObsoleteChildren)
            {
                var child = board.transform.Find(name);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Computes the peg + slot extents (in the board's local space) and ensures rail walls
        /// hug them on the left / right / top, plus a wide trigger catcher below them.
        /// </summary>
        private static void BuildFrame(GameObject board)
        {
            var pegs = board.GetComponentsInChildren<Peg>(true);
            var slots = board.GetComponentsInChildren<Slot>(true);
            if (pegs.Length == 0)
            {
                Debug.LogWarning($"[ApplyBoardImperfection] Board '{board.name}' has no Pegs; skipping frame build.");
                return;
            }

            // Bounds in board-local space.
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var p in pegs)
            {
                var lp = board.transform.InverseTransformPoint(p.transform.position);
                if (lp.x < minX) minX = lp.x;
                if (lp.x > maxX) maxX = lp.x;
                if (lp.y < minY) minY = lp.y;
                if (lp.y > maxY) maxY = lp.y;
            }
            foreach (var s in slots)
            {
                var lp = board.transform.InverseTransformPoint(s.transform.position);
                if (lp.x < minX) minX = lp.x;
                if (lp.x > maxX) maxX = lp.x;
                if (lp.y < minY) minY = lp.y;
                if (lp.y > maxY) maxY = lp.y;
            }

            float frameHeight = (maxY - minY) + 2f * RailMargin;
            float frameCentreY = (maxY + minY) * 0.5f;
            float frameWidth = (maxX - minX) + 2f * RailMargin;
            float frameCentreX = (maxX + minX) * 0.5f;

            // Left rail: thin in X, tall in Y. Positioned just left of minPegX.
            EnsureSolidWall(board, LeftRailName,
                localPos: new Vector3(minX - RailMargin, frameCentreY, 0f),
                size: new Vector3(RailThickness, frameHeight, 1f));

            // Right rail
            EnsureSolidWall(board, RightRailName,
                localPos: new Vector3(maxX + RailMargin, frameCentreY, 0f),
                size: new Vector3(RailThickness, frameHeight, 1f));

            // Top rail: thin in Y, spans full width. Above the spawn area so any upward bounce
            // is contained.
            EnsureSolidWall(board, TopRailName,
                localPos: new Vector3(frameCentreX, maxY + RailMargin, 0f),
                size: new Vector3(frameWidth, RailThickness, 1f));

            // Bottom catcher: TRIGGER far below the slots. Any ball that escapes a slot
            // (e.g. wedge between buckets) ends up here and is despawned via the Slot
            // component, since we add a Slot to it with a sentinel index of -1.
            EnsureBottomCatcher(board,
                localPos: new Vector3(frameCentreX, minY - BottomCatcherDrop, 0f),
                size: new Vector3(frameWidth * 4f, 0.5f, 1f));
        }

        private static void EnsureSolidWall(GameObject board, string name, Vector3 localPos, Vector3 size)
        {
            var wall = GetOrCreateChild(board, name);
            wall.transform.localPosition = localPos;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one;

            var box = wall.GetComponent<BoxCollider>();
            if (box == null) box = wall.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
            box.isTrigger = false;

            EditorUtility.SetDirty(wall);
        }

        private static void EnsureBottomCatcher(GameObject board, Vector3 localPos, Vector3 size)
        {
            var catcher = GetOrCreateChild(board, BottomCatcherName);
            catcher.transform.localPosition = localPos;
            catcher.transform.localRotation = Quaternion.identity;
            catcher.transform.localScale = Vector3.one;

            var box = catcher.GetComponent<BoxCollider>();
            if (box == null) box = catcher.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
            box.isTrigger = true;

            // Slot with index -1 so it routes through the existing Ball.HandleSlotEntered
            // despawn path but is distinguishable from a real scoring bucket.
            var slot = catcher.GetComponent<Slot>();
            if (slot == null) slot = catcher.AddComponent<Slot>();
            slot.SetSlotIndex(-1);

            EditorUtility.SetDirty(catcher);
            EditorUtility.SetDirty(slot);
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static Scene EnsureBattleSceneOpen()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == BattleScenePath) return active;

            if (active.isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[ApplyBoardImperfection] User cancelled scene save; aborting.");
                    return new Scene();
                }
            }
            return EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        }
    }
}
