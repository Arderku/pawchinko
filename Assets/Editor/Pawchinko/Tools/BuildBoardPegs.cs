using System.Collections.Generic;
using System.IO;
using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace Pawchinko.Editor.Tools
{
    /// <summary>
    /// One-shot editor utility that gives the board pegs stable identity for the ability peg system
    /// (Section 13, Phase 2):
    ///
    ///   * Adds a <see cref="Peg"/> component to every <c>FBX_Pin_*</c> on the player board and
    ///     assigns a stable <see cref="Peg.PegIndex"/> ordered by board position (top-to-bottom,
    ///     then left-to-right). Indices are shared with the enemy board because it is a prefab
    ///     variant of the player board.
    ///   * Adds a <see cref="BattleBoard"/> root marker set to Player on the base prefab, and
    ///     overrides the side to Enemy on the variant - the per-side identity the shared pegs lack.
    ///   * Writes a <see cref="PegLayout"/> asset capturing each peg's normalized board position so
    ///     the ability inspector can show a clickable, board-shaped peg picker.
    ///
    /// Safe to re-run: existing components are reused, indices + layout are recomputed.
    /// </summary>
    public static class BuildBoardPegs
    {
        private const string PlayerBoardPath = "Assets/VisualAssets/Prefabs/Board/PlayerBoard.prefab";
        private const string EnemyBoardPath = "Assets/VisualAssets/Prefabs/Board/EneamyBoard.prefab";
        private const string BoardDataFolder = "Assets/Data/Board";
        private const string PegLayoutPath = BoardDataFolder + "/PegLayout.asset";

        private const string PinNamePrefix = "FBX_Pin";

        [MenuItem("Pawchinko/Build Board Pegs")]
        public static void Build()
        {
            var playerRoot = PrefabUtility.LoadPrefabContents(PlayerBoardPath);
            if (playerRoot == null)
            {
                Debug.LogError($"[BuildBoardPegs] Could not load player board at {PlayerBoardPath}.");
                return;
            }

            try
            {
                var pins = CollectPins(playerRoot.transform);
                if (pins.Count == 0)
                {
                    Debug.LogError($"[BuildBoardPegs] No '{PinNamePrefix}_*' pegs found under the player board.");
                    return;
                }

                // The pin GameObjects all sit near the board origin; the visible peg geometry lives
                // in each pin's MESH, so the real board position is the renderer/collider bounds
                // center (world-space within the loaded prefab), not transform.position.
                var center = new List<Vector3>(pins.Count);
                foreach (var t in pins) center.Add(PinCenter(t));

                // The board is a plane: one axis barely varies (depth) - drop it. Of the two
                // in-plane axes, prefer world-Y as "vertical" (the board stands up in Y here);
                // otherwise the larger-spread axis is vertical. The other is horizontal.
                ChooseAxes(center, out int hAxis, out int vAxis);

                var order = new List<int>(pins.Count);
                for (int i = 0; i < pins.Count; i++) order.Add(i);
                order.Sort((a, b) =>
                {
                    int byV = center[b][vAxis].CompareTo(center[a][vAxis]); // top (higher) first
                    if (byV != 0) return byV;
                    return center[a][hAxis].CompareTo(center[b][hAxis]); // then left to right
                });

                // Bounds for normalization (in the chosen plane).
                float minH = float.MaxValue, maxH = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
                foreach (var p in center)
                {
                    if (p[hAxis] < minH) minH = p[hAxis];
                    if (p[hAxis] > maxH) maxH = p[hAxis];
                    if (p[vAxis] < minV) minV = p[vAxis];
                    if (p[vAxis] > maxV) maxV = p[vAxis];
                }
                float rangeH = Mathf.Max(1e-4f, maxH - minH);
                float rangeV = Mathf.Max(1e-4f, maxV - minV);

                var positions = new Vector2[pins.Count];
                for (int index = 0; index < order.Count; index++)
                {
                    int pinIdx = order[index];
                    var peg = pins[pinIdx].GetComponent<Peg>();
                    if (peg == null) peg = pins[pinIdx].gameObject.AddComponent<Peg>();
                    peg.SetPegIndex(index);

                    var p = center[pinIdx];
                    positions[index] = new Vector2((p[hAxis] - minH) / rangeH, (p[vAxis] - minV) / rangeV);
                }

                var board = playerRoot.GetComponent<BattleBoard>();
                if (board == null) board = playerRoot.AddComponent<BattleBoard>();
                board.SetSide(Side.Player);

                PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerBoardPath);
                Debug.Log($"[BuildBoardPegs] Player board: indexed {pins.Count} pegs (horizontal axis = {AxisName(hAxis)}, vertical axis = {AxisName(vAxis)}).");

                WritePegLayout(pins.Count, positions);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }

            // Refresh so the variant load sees the new base components, then override the side.
            AssetDatabase.SaveAssets();

            var enemyRoot = PrefabUtility.LoadPrefabContents(EnemyBoardPath);
            if (enemyRoot != null)
            {
                try
                {
                    var board = enemyRoot.GetComponent<BattleBoard>();
                    if (board == null) board = enemyRoot.AddComponent<BattleBoard>();
                    board.SetSide(Side.Enemy);
                    PrefabUtility.SaveAsPrefabAsset(enemyRoot, EnemyBoardPath);
                    Debug.Log("[BuildBoardPegs] Enemy board: side override set to Enemy (pegs inherited from player board).");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(enemyRoot);
                }
            }
            else
            {
                Debug.LogWarning($"[BuildBoardPegs] Enemy board not found at {EnemyBoardPath}; only the player board was processed.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildBoardPegs] Done. Scene board instances inherit the pegs automatically.");
        }

        private static List<Transform> CollectPins(Transform root)
        {
            var pins = new List<Transform>();
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name.StartsWith(PinNamePrefix)) pins.Add(t);
            }
            return pins;
        }

        // World-space center of a pin's visible geometry (the transform sits at the board origin).
        private static Vector3 PinCenter(Transform t)
        {
            var r = t.GetComponent<Renderer>();
            if (r != null) return r.bounds.center;
            var c = t.GetComponent<Collider>();
            if (c != null) return c.bounds.center;
            return t.position;
        }

        // Picks the board-plane axes: the least-varying axis is depth and is dropped; of the other
        // two, world-Y is preferred as vertical (boards stand up in Y), else the larger spread wins.
        private static void ChooseAxes(List<Vector3> pts, out int hAxis, out int vAxis)
        {
            float[] s = { Spread(pts, 0), Spread(pts, 1), Spread(pts, 2) };
            int depth = 0;
            for (int i = 1; i < 3; i++) if (s[i] < s[depth]) depth = i;

            int a = (depth + 1) % 3;
            int b = (depth + 2) % 3;
            if (a == 1 || b == 1)
            {
                vAxis = 1;                  // world Y is in-plane: use it as vertical
                hAxis = (a == 1) ? b : a;
            }
            else
            {
                vAxis = s[a] >= s[b] ? a : b;
                hAxis = (vAxis == a) ? b : a;
            }
        }

        private static string AxisName(int axis) => axis == 0 ? "X" : (axis == 1 ? "Y" : "Z");

        private static float Spread(List<Vector3> values, int axis)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in values)
            {
                float c = v[axis];
                if (c < min) min = c;
                if (c > max) max = c;
            }
            return max - min;
        }

        private static void WritePegLayout(int count, Vector2[] positions)
        {
            if (!Directory.Exists(BoardDataFolder)) Directory.CreateDirectory(BoardDataFolder);

            var layout = AssetDatabase.LoadAssetAtPath<PegLayout>(PegLayoutPath);
            bool isNew = false;
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<PegLayout>();
                AssetDatabase.CreateAsset(layout, PegLayoutPath);
                isNew = true;
            }

            var so = new SerializedObject(layout);
            so.FindProperty("count").intValue = count;
            var arr = so.FindProperty("positions");
            arr.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).vector2Value = positions[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);

            Debug.Log($"[BuildBoardPegs] {(isNew ? "Created" : "Updated")} peg layout ({count} pegs) at {PegLayoutPath}.");
        }
    }
}
