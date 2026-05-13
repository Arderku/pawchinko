using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Static per-species data for a Pom (PAWCHINKO_DESIGN_GUIDE Section 8). Runtime Pom
    /// instances hold a reference to one of these plus their own mutable level / exp /
    /// learned-ability slots.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Definition", fileName = "Pom_New")]
    public class PomDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string species;
        [SerializeField, TextArea] private string description;
        [SerializeField] private PomRarity rarity;
        [SerializeField] private PomType type;

        [Header("Progression")]
        [SerializeField] private int levelMax = 50;
        [SerializeField] private PomExpCurve expCurve = PomExpCurve.Medium;

        [Header("Battle Tuning")]
        [SerializeField] private int baseEnergy = 10;
        [SerializeField] private int apCost = 1;
        [SerializeField] private PomStats baseStats = new();
        [SerializeField] private PomBallProfile ballProfile = new();

        [Header("Type Overrides (optional)")]
        [Tooltip("Optional per-species override of the default behavior block on PomTypeDefinition.")]
        [SerializeField] private PomTypeBehavior typeBehaviorOverride;

        [Header("Visual / Tags")]
        [SerializeField] private PomVisualIdentity visualIdentity = new();
        [SerializeField] private List<string> tags = new();

        [Header("Abilities (learnable pool - type must match)")]
        [SerializeField] private List<PomAbilityDefinition> learnableAbilities = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Species => species;
        public string Description => description;
        public PomRarity Rarity => rarity;
        public PomType Type => type;
        public int LevelMax => levelMax;
        public PomExpCurve ExpCurve => expCurve;
        public int BaseEnergy => baseEnergy;
        public int ApCost => apCost;
        public PomStats BaseStats => baseStats;
        public PomBallProfile BallProfile => ballProfile;
        public PomTypeBehavior TypeBehaviorOverride => typeBehaviorOverride;
        public PomVisualIdentity VisualIdentity => visualIdentity;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<PomAbilityDefinition> LearnableAbilities => learnableAbilities;

        /// <summary>
        /// Returns true when the given ability is in this species' learnable pool AND its
        /// type matches this species' type. The runtime Pom uses this on Learn().
        /// </summary>
        public bool CanLearn(PomAbilityDefinition ability)
        {
            if (ability == null) return false;
            if (ability.Type != type) return false;
            if (learnableAbilities == null) return false;
            return learnableAbilities.Contains(ability);
        }
    }
}
