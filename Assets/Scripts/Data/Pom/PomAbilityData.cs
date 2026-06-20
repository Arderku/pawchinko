using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>Which board(s) an ability's effects apply to when it resolves.</summary>
    public enum PomAbilityBoardTarget
    {
        /// <summary>The caster's own board (the player's side).</summary>
        Self = 0,
        /// <summary>The opposing board.</summary>
        Enemy = 1,
        /// <summary>Both boards.</summary>
        Both = 2
    }

    /// <summary>
    /// One ability asset (Section 13). An ability declares who may use it (<see cref="requiredType"/>,
    /// where "any" means no restriction), what it costs (<see cref="apCost"/>), which board(s) it
    /// hits (<see cref="boardTarget"/>), and a list of <see cref="AbilityEffect"/>s that resolve at
    /// the start of a round before balls drop and last exactly that one round. The numbers are
    /// authored per asset (unlike the shared ball-growth table) so each ability can be tuned on its
    /// own. The effect math lives in <see cref="AbilityManager"/> + <see cref="RoundModifiers"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Ability Data", fileName = "PomAbility_New")]
    public class PomAbilityData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        [Header("Usage")]
        [Tooltip("Which Poms may learn/use this ability. Set 'any' = true so ANY Pom can use it; otherwise the Pom's primary OR secondary type must match.")]
        [SerializeField] private PomTypeFilter requiredType = PomTypeFilter.Any;
        [Tooltip("Action Points this ability costs to cast. The Pom must have at least this much current AP.")]
        [Min(0)]
        [SerializeField] private int apCost = 1;
        [Tooltip("Which board(s) the effects apply to. Self = the caster's board, Enemy = the opponent's, Both = both.")]
        [SerializeField] private PomAbilityBoardTarget boardTarget = PomAbilityBoardTarget.Self;

        [Header("Effects (resolved at round start, last one round)")]
        [SerializeField] private List<AbilityEffect> effects = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;

        public PomTypeFilter RequiredType => requiredType;
        public int ApCost => apCost;
        public PomAbilityBoardTarget BoardTarget => boardTarget;

        public IReadOnlyList<AbilityEffect> Effects => effects;
    }
}
