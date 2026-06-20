using System;
using System.Linq;

namespace Pawchinko
{
    /// <summary>
    /// Ability-learning operations for runtime Pom instances. Enforces the design rule that a
    /// Pom may only learn abilities matching one of its types (primary or, when set,
    /// secondary) AND present in the species' learnable pool. Section 13 of the design guide.
    /// </summary>
    public static class PomAbilityLearning
    {
        /// <summary>
        /// True when the ability is in the species' learnable pool AND the species satisfies the
        /// ability's required type (an ability whose <see cref="PomAbilityData.RequiredType"/> is
        /// "any" can be learned by every Pom; otherwise the species' primary or secondary type
        /// must match).
        /// </summary>
        public static bool CanLearn(PomData data, PomAbilityData ability)
        {
            if (data == null || ability == null) return false;
            var pool = data.LearnableAbilities;
            if (pool == null || !pool.Contains(ability)) return false;

            var required = ability.RequiredType;
            if (required.any) return true;
            if (required.Matches(data.PrimaryType)) return true;
            if (data.HasSecondaryType && required.Matches(data.SecondaryType)) return true;
            return false;
        }

        public static bool CanLearn(PomInstance instance, PomAbilityData ability)
        {
            return instance != null && CanLearn(instance.data, ability);
        }

        /// <summary>
        /// Assigns the ability to a learned-ability slot (0 or 1). Throws on invalid inputs so
        /// authoring tools surface the mistake instead of silently dropping the ability.
        /// </summary>
        public static void Learn(PomInstance instance, PomAbilityData ability, int slot)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (ability == null) throw new ArgumentNullException(nameof(ability));
            if (slot < 0 || slot >= PomInstance.LearnedAbilitySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), $"Pom learned-ability slot must be 0..{PomInstance.LearnedAbilitySlotCount - 1}");
            }
            if (!CanLearn(instance, ability))
            {
                string owner = instance.data != null ? instance.data.DisplayName : "<null>";
                throw new ArgumentException($"Pom '{owner}' cannot learn ability '{ability.DisplayName}' (required type {(ability.RequiredType.any ? "Any" : ability.RequiredType.type.ToString())}).", nameof(ability));
            }

            if (instance.learnedAbilities == null || instance.learnedAbilities.Length != PomInstance.LearnedAbilitySlotCount)
            {
                instance.learnedAbilities = new PomAbilityData[PomInstance.LearnedAbilitySlotCount];
            }
            instance.learnedAbilities[slot] = ability;
        }

        /// <summary>Clears a learned-ability slot.</summary>
        public static void Forget(PomInstance instance, int slot)
        {
            if (instance == null || instance.learnedAbilities == null) return;
            if (slot < 0 || slot >= PomInstance.LearnedAbilitySlotCount) return;
            instance.learnedAbilities[slot] = null;
        }
    }
}
