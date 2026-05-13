using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Runtime instance of a Pom: a reference to an immutable PomDefinition asset plus the
    /// mutable per-Pom state (level, exp, the two learned ability slots). This is the thing
    /// team rosters hold and the thing future save data serializes; the SO it points to stays
    /// authoritative for static fields.
    /// </summary>
    [Preserve]
    [Serializable]
    public class Pom
    {
        public const int LearnedAbilitySlotCount = 2;

        [SerializeField] private PomDefinition definition;
        [SerializeField] private int level = 1;
        [SerializeField] private int exp;
        [SerializeField] private PomAbilityDefinition[] learnedAbilities = new PomAbilityDefinition[LearnedAbilitySlotCount];

        public Pom() { }

        public Pom(PomDefinition definition, int level = 1)
        {
            this.definition = definition;
            this.level = Mathf.Max(1, level);
            this.exp = 0;
            this.learnedAbilities = new PomAbilityDefinition[LearnedAbilitySlotCount];
        }

        public PomDefinition Definition => definition;
        public int Level => level;
        public int Exp => exp;
        public PomAbilityDefinition[] LearnedAbilities => learnedAbilities;

        /// <summary>
        /// Convenience accessor: how many balls this Pom contributes at its current level,
        /// resolved from the definition's PomBallProfile bands.
        /// </summary>
        public int CurrentBallCount => definition != null ? definition.BallProfile.GetBallCountForLevel(level) : 0;

        /// <summary>
        /// Returns true if the ability matches this Pom's type AND is part of the species'
        /// learnable pool. A Pom cannot learn abilities of another type (hard rule, see
        /// PAWCHINKO_DESIGN_GUIDE Section 13).
        /// </summary>
        public bool CanLearn(PomAbilityDefinition ability)
        {
            if (definition == null) return false;
            return definition.CanLearn(ability);
        }

        /// <summary>
        /// Assigns the ability to a learned-ability slot (0 or 1). The third learn replaces an
        /// existing slot - callers decide which slot to overwrite. Throws on invalid inputs so
        /// authoring tools surface the mistake instead of silently dropping the ability.
        /// </summary>
        public void Learn(PomAbilityDefinition ability, int slot)
        {
            if (ability == null) throw new ArgumentNullException(nameof(ability));
            if (slot < 0 || slot >= LearnedAbilitySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), $"Pom learned-ability slot must be 0..{LearnedAbilitySlotCount - 1}");
            }
            if (!CanLearn(ability))
            {
                throw new ArgumentException($"Pom of type {(definition != null ? definition.Type.ToString() : "<null>")} cannot learn ability '{ability.DisplayName}' (type {ability.Type}).", nameof(ability));
            }

            if (learnedAbilities == null || learnedAbilities.Length != LearnedAbilitySlotCount)
            {
                learnedAbilities = new PomAbilityDefinition[LearnedAbilitySlotCount];
            }
            learnedAbilities[slot] = ability;
        }

        /// <summary>
        /// Clears a learned-ability slot.
        /// </summary>
        public void Forget(int slot)
        {
            if (slot < 0 || slot >= LearnedAbilitySlotCount) return;
            if (learnedAbilities == null) return;
            learnedAbilities[slot] = null;
        }
    }
}
