using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Off-screen stage that draws all live 3D Pom portraits for the Battle HUD into a single
    /// shared atlas <see cref="RenderTexture"/> using exactly one <see cref="Camera"/>. Each
    /// <see cref="PomPortraitSlot"/> is a cell of the atlas; the slot's <see cref="RawImage"/>
    /// is wired to the same atlas with a per-cell <see cref="RawImage.uvRect"/> slice.
    ///
    /// Why this instead of one camera per card: a Battle HUD with 5 player + 5 enemy slots
    /// is otherwise 10 cameras and 10 RenderTextures, each doing its own culling, RT bind,
    /// clear and pipeline setup every frame. Routing through one camera cuts CPU pipeline
    /// setup ~10× and lets the GPU batch portrait draw calls by material.
    ///
    /// Per-slot framing is preserved by parking the shared camera at the slot's
    /// <see cref="PomPortraitSlot.CameraAnchor"/> pose just before each render, so the
    /// pixels in each atlas cell are identical to what a private per-slot camera would have
    /// produced. The camera renders 10 times per <see cref="LateUpdate"/>, but the cost of
    /// "set rect + set transform + Render()" is dominated by GPU work, not pipeline setup.
    ///
    /// The atlas grid is fixed at 5 columns × 2 rows: player row 0..4 on the top half, enemy
    /// row 0..4 on the bottom half. Roster indices 0..2 are the Battle Zone, 3..4 the Bench
    /// Zone (same as the card list).
    /// </summary>
    public class PomPortraitStage : MonoBehaviour
    {
        [Header("Shared rendering")]
        [Tooltip("Single camera that renders every portrait. Its transform is reposed per-slot by this stage; do not parent anything to it.")]
        [SerializeField] private Camera sharedCamera;
        [Tooltip("Pixel resolution per atlas cell. 192x192 ≈ 2× the displayed card slot size, good headroom for retina-style screens.")]
        [SerializeField] private Vector2Int cellResolution = new Vector2Int(192, 192);

        [Header("Slots (length BattleManager.MaxRosterPoms = 5 per side)")]
        [SerializeField] private List<PomPortraitSlot> playerSlots = new();
        [SerializeField] private List<PomPortraitSlot> enemySlots = new();

        private RenderTexture _atlasRT;
        private Rect[] _playerCellRects;
        private Rect[] _enemyCellRects;
        private int _cols;
        private const int Rows = 2;
        private const int PlayerRow = 1;   // top row of atlas
        private const int EnemyRow = 0;    // bottom row of atlas

        public Texture AtlasTexture => _atlasRT;

        private void Awake()
        {
            _cols = Mathf.Max(playerSlots?.Count ?? 0, enemySlots?.Count ?? 0);
            if (_cols <= 0)
            {
                Debug.LogError("[PomPortraitStage] No slots configured; portraits will not render.");
                return;
            }

            CreateAtlas();
            ConfigureSharedCamera();
            BuildCellRects();
            ConfigureSlots(playerSlots, _playerCellRects);
            ConfigureSlots(enemySlots, _enemyCellRects);
        }

        private void CreateAtlas()
        {
            if (cellResolution.x <= 0 || cellResolution.y <= 0)
            {
                cellResolution = new Vector2Int(192, 192);
            }
            int w = cellResolution.x * _cols;
            int h = cellResolution.y * Rows;
            _atlasRT = new RenderTexture(w, h, 24)
            {
                name = "RT_PomPortraitAtlas",
                antiAliasing = 1,
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _atlasRT.Create();
        }

        private void ConfigureSharedCamera()
        {
            if (sharedCamera == null)
            {
                Debug.LogError("[PomPortraitStage] sharedCamera is not assigned.");
                return;
            }
            sharedCamera.targetTexture = _atlasRT;
            // We drive Render() manually from LateUpdate so the pipeline doesn't render this
            // camera again during its normal pass.
            sharedCamera.enabled = false;
        }

        private void BuildCellRects()
        {
            _playerCellRects = new Rect[_cols];
            _enemyCellRects = new Rect[_cols];
            float w = 1f / _cols;
            float h = 1f / Rows;
            for (int i = 0; i < _cols; i++)
            {
                _playerCellRects[i] = new Rect(i * w, PlayerRow * h, w, h);
                _enemyCellRects[i] = new Rect(i * w, EnemyRow * h, w, h);
            }
        }

        private void ConfigureSlots(List<PomPortraitSlot> slots, Rect[] cellRects)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count && i < cellRects.Length; i++)
            {
                slots[i]?.ConfigureFromStage(_atlasRT, cellRects[i]);
            }
        }

        private void LateUpdate()
        {
            if (sharedCamera == null || _atlasRT == null) return;
            RenderSide(playerSlots, _playerCellRects);
            RenderSide(enemySlots, _enemyCellRects);
        }

        /// <summary>
        /// Renders one row of the atlas. Re-poses the shared camera to each slot's
        /// <see cref="PomPortraitSlot.CameraAnchor"/> and writes into the matching cell rect.
        /// All cells are rendered every frame so disabled / cleared slots also get their
        /// previous-frame pixels cleared by the camera's solid-color clear.
        /// </summary>
        private void RenderSide(List<PomPortraitSlot> slots, Rect[] cellRects)
        {
            if (slots == null || cellRects == null) return;
            int count = Mathf.Min(slots.Count, cellRects.Length);
            for (int i = 0; i < count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.CameraAnchor == null) continue;

                sharedCamera.rect = cellRects[i];
                sharedCamera.transform.SetPositionAndRotation(
                    slot.CameraAnchor.position,
                    slot.CameraAnchor.rotation);
                sharedCamera.Render();
            }
        }

        /// <summary>Binds the player roster to the player-side portrait slots.</summary>
        public void BindPlayerSide(IReadOnlyList<PomInstance> roster) => BindSide(playerSlots, roster);

        /// <summary>Binds the enemy roster to the enemy-side portrait slots.</summary>
        public void BindEnemySide(IReadOnlyList<PomInstance> roster) => BindSide(enemySlots, roster);

        /// <summary>Clears all portraits on both sides.</summary>
        public void ClearAll()
        {
            ClearSide(playerSlots);
            ClearSide(enemySlots);
        }

        private static void BindSide(List<PomPortraitSlot> slots, IReadOnlyList<PomInstance> roster)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (roster != null && i < roster.Count) slot.SetPom(roster[i]);
                else slot.Clear();
            }
        }

        private static void ClearSide(List<PomPortraitSlot> slots)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++) slots[i]?.Clear();
        }

        private void OnDestroy()
        {
            if (_atlasRT != null)
            {
                if (sharedCamera != null && sharedCamera.targetTexture == _atlasRT)
                    sharedCamera.targetTexture = null;
                _atlasRT.Release();
                Destroy(_atlasRT);
                _atlasRT = null;
            }
        }
    }
}
