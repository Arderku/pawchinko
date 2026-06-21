using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Applies per-round peg <b>hide</b> effects (Section 13) and the peg <b>color</b> feedback for
    /// abilities. Peg power-on-hit is handled by the ball itself in <see cref="Ball.OnCollisionEnter"/>;
    /// this manager toggles peg visibility and tints:
    /// <list type="bullet">
    ///   <item>During planning it <b>previews</b> the highlighted ability's target pegs in that
    ///   ability's type color (a flat wash) on the board(s) it would hit.</item>
    ///   <item>At drop time it hides the pegs an ability flagged and re-tints the locked abilities'
    ///   target pegs in the <b>applied</b> (full-bright, glowing) state so they visibly change again.</item>
    ///   <item>At the start of the next round it restores + un-tints every peg.</item>
    /// </list>
    ///
    /// Boards are discovered through <see cref="BattleBoard"/> roots, so no scene wiring is needed.
    /// If the board pegs have not been built yet (the <c>Build Board Pegs</c> tool was never run),
    /// no boards are found and the manager is a harmless no-op.
    /// </summary>
    public class PegManager : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;

        private BattleBoard[] _boards;
        private readonly List<Peg> _tintedPegs = new();

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
            ApplyActiveTints();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            RestoreAll();
        }

        /// <summary>
        /// Planning preview: clears any prior tint, then washes the highlighted ability's target
        /// pegs in its type color (flat, no glow) on the board(s) it would hit, so the player can
        /// see what it affects before locking it. <paramref name="ability"/> null just clears.
        /// </summary>
        public void PreviewAbility(PomAbilityData ability, PomInstance caster, Side casterSide)
        {
            ClearTints();
            if (ability == null) return;
            TintAbilityPegs(ability, caster, casterSide, active: false);
        }

        /// <summary>
        /// Re-tints the locked player abilities' target pegs in their type color in the full-bright
        /// "applied" state as the round's abilities resolve at drop time.
        /// </summary>
        private void ApplyActiveTints()
        {
            ClearTints();

            var gm = GameManager.Instance;
            var am = gm != null ? gm.AbilityManager : null;
            var bm = gm != null ? gm.BattleManager : null;
            if (am == null || bm == null) return;

            var active = bm.GetActivePoms(Side.Player);
            if (active == null) return;

            int count = Mathf.Min(active.Count, BattleManager.MaxActivePoms);
            for (int i = 0; i < count; i++)
            {
                var ability = am.GetSelection(i);
                if (ability != null) TintAbilityPegs(ability, active[i], Side.Player, active: true);
            }
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

        /// <summary>Re-shows + un-tints every peg on every board (called at round start, before the next plan window).</summary>
        private void RestoreAll()
        {
            ClearTints();

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

        /// <summary>
        /// Tints every peg an ability targets (any peg effect) in its type color, on the board(s)
        /// its <see cref="PomAbilityBoardTarget"/> routes to relative to the caster's side.
        /// </summary>
        private void TintAbilityPegs(PomAbilityData ability, PomInstance caster, Side casterSide, bool active)
        {
            var effects = ability.Effects;
            if (effects == null) return;

            if (_boards == null || _boards.Length == 0) RefreshBoards();
            if (_boards == null) return;

            Color color = PomTypeColors.ForAbility(ability, caster);

            for (int e = 0; e < effects.Count; e++)
            {
                var eff = effects[e];
                if (eff == null || eff.kind != AbilityEffectKind.PegEffect || eff.targetIndices == null) continue;

                for (int b = 0; b < _boards.Length; b++)
                {
                    var board = _boards[b];
                    if (board == null) continue;
                    if (!HitsBoard(ability.BoardTarget, casterSide, board.Side)) continue;
                    TintPegsOnBoard(board, eff.targetIndices, color, active);
                }
            }
        }

        private void TintPegsOnBoard(BattleBoard board, int[] pegIndices, Color color, bool active)
        {
            var pegs = board.Pegs;
            for (int p = 0; p < pegs.Count; p++)
            {
                var peg = pegs[p];
                if (peg == null || !Contains(pegIndices, peg.PegIndex)) continue;
                peg.SetTint(true, color, active);
                if (!_tintedPegs.Contains(peg)) _tintedPegs.Add(peg);
            }
        }

        private void ClearTints()
        {
            for (int i = 0; i < _tintedPegs.Count; i++)
            {
                if (_tintedPegs[i] != null) _tintedPegs[i].SetTint(false, Color.clear, false);
            }
            _tintedPegs.Clear();
        }

        private static bool HitsBoard(PomAbilityBoardTarget target, Side casterSide, Side boardSide)
        {
            switch (target)
            {
                case PomAbilityBoardTarget.Self: return boardSide == casterSide;
                case PomAbilityBoardTarget.Enemy: return boardSide != casterSide;
                case PomAbilityBoardTarget.Both: return true;
                default: return false;
            }
        }

        private static bool Contains(int[] values, int value)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value) return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<DropRequestedEvent>(OnDropRequested);
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }
    }
}
