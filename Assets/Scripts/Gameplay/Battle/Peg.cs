using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// A board peg with a stable <see cref="PegIndex"/> so abilities can target specific pegs
    /// (Section 13, peg effects). The index is assigned once by the <c>Build Board Pegs</c> editor
    /// tool (sorted by board position) and is shared by the player and enemy boards because the
    /// enemy board is a prefab variant of the player board - the per-side identity lives on the
    /// board root (<see cref="BattleBoard"/>), not here.
    ///
    /// All physics behaviour comes from the MeshCollider + PhysicsMaterial on the same GameObject;
    /// this component only adds identity and a per-round hide toggle.
    /// </summary>
    public class Peg : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Stable index used by ability peg-targeting. Assigned by the Build Board Pegs tool.")]
        [SerializeField] private int pegIndex = -1;

        [Header("Layout (optional, legacy)")]
        [SerializeField] private int row = -1;
        [SerializeField] private int col = -1;

        public int PegIndex => pegIndex;
        public int Row => row;
        public int Col => col;
        public bool IsHidden { get; private set; }

        private Renderer _renderer;
        private Collider _collider;
        private bool _cached;

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            if (_cached) return;
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _cached = true;
        }

        /// <summary>Sets the stable peg index. Called once by the board-build editor tool.</summary>
        public void SetPegIndex(int index) => pegIndex = index;

        /// <summary>Sets the peg's grid coordinates (legacy; optional).</summary>
        public void SetCoords(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        /// <summary>
        /// Hides/shows the peg for the round: toggles its renderer + collider so a hidden peg is
        /// invisible AND lets balls pass through it. Restored at the start of the next round.
        /// </summary>
        public void SetHidden(bool hidden)
        {
            CacheComponents();
            IsHidden = hidden;
            if (_renderer != null) _renderer.enabled = !hidden;
            if (_collider != null) _collider.enabled = !hidden;
        }
    }
}
