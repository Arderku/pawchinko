using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Applies per-round peg <b>hide</b> effects (Section 13). Peg power-on-hit is handled by the
    /// ball itself in <see cref="Ball.OnCollisionEnter"/>; this manager only toggles peg visibility:
    /// at drop time it hides the pegs an ability flagged this round (so balls pass through them) and
    /// at the start of the next round it restores every peg.
    ///
    /// Boards are discovered through <see cref="BattleBoard"/> roots, so no scene wiring is needed.
    /// If the board pegs have not been built yet (the <c>Build Board Pegs</c> tool was never run),
    /// no boards are found and the manager is a harmless no-op.
    /// </summary>
    public class PegManager : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;

        private BattleBoard[] _boards;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<DropRequestedEvent>(OnDropRequested);
            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);

            RefreshBoards();
            Debug.Log($"[PegManager] Initialized ({(_boards != null ? _boards.Length : 0)} board(s))");
        }

        private void RefreshBoards()
        {
            _boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Include);
        }

        private void OnDropRequested(DropRequestedEvent evt)
        {
            ApplyHides();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            RestoreAll();
        }

        /// <summary>Hides the pegs each side's resolved modifiers flagged for this round.</summary>
        private void ApplyHides()
        {
            if (_boards == null || _boards.Length == 0) RefreshBoards();
            if (_boards == null || _boards.Length == 0) return;

            var am = GameManager.Instance != null ? GameManager.Instance.AbilityManager : null;

            for (int b = 0; b < _boards.Length; b++)
            {
                var board = _boards[b];
                if (board == null) continue;

                var mods = am != null ? am.GetModifiers(board.Side) : RoundModifiers.Empty;
                var pegs = board.Pegs;
                for (int p = 0; p < pegs.Count; p++)
                {
                    var peg = pegs[p];
                    if (peg != null) peg.SetHidden(mods.IsPegHidden(peg.PegIndex));
                }
            }
        }

        /// <summary>Re-shows every peg on every board (called at round start, before the next plan window).</summary>
        private void RestoreAll()
        {
            if (_boards == null || _boards.Length == 0) RefreshBoards();
            if (_boards == null) return;

            for (int b = 0; b < _boards.Length; b++)
            {
                var board = _boards[b];
                if (board == null) continue;

                var pegs = board.Pegs;
                for (int p = 0; p < pegs.Count; p++)
                {
                    var peg = pegs[p];
                    if (peg != null && peg.IsHidden) peg.SetHidden(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<DropRequestedEvent>(OnDropRequested);
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }
    }
}
