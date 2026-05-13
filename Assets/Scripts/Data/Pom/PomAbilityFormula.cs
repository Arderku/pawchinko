using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Single home for "Pom stats x ability raw values -> final number" math. Raw effect values
    /// authored on PomAbilityDefinition are tuned independently from Pom stats; this helper is
    /// where stats amplify (or scale) the final in-battle outcome without ever silently
    /// replacing the ability's authored numbers (PAWCHINKO_DESIGN_GUIDE Section 13).
    ///
    /// Methods currently return the raw effect value; per-stat scaling lands when battle wiring
    /// needs concrete tuning (TBD per design guide).
    /// </summary>
    public static class PomAbilityFormula
    {
        /// <summary>
        /// Final whole-ball bonus an ability adds to the round's ball total. Example: an ability
        /// authored as "+1 ball" on a high-Power Pom may resolve to +2; today this is a no-op
        /// passthrough so call sites can wire up without waiting on tuning.
        /// </summary>
        public static int ComputeBonusBalls(PomStats stats, PomAbilityEffect effect)
        {
            if (effect == null) return 0;
            // TODO: factor in stats.power - heavier hitters should net more extra balls.
            return Mathf.RoundToInt(effect.value);
        }

        /// <summary>
        /// Final multiplier an ability applies to bucket / ball score. Example: a 2x multiplier
        /// on a high-Power Pom may resolve to 2.4x. Today returns the raw value.
        /// </summary>
        public static float ComputeMultiplier(PomStats stats, PomAbilityEffect effect)
        {
            if (effect == null) return 1f;
            // TODO: factor in stats.power - power should boost score-scaling abilities.
            return effect.value;
        }

        /// <summary>
        /// Final 0..1 chance an ability procs. Example: an authored 50% chance on a high-Luck
        /// Pom may resolve to 65%. Today returns the raw chance (clamped 0..1).
        /// </summary>
        public static float ComputeProcChance(PomStats stats, PomAbilityEffect effect)
        {
            if (effect == null) return 0f;
            // TODO: factor in stats.luck - luck biases proc rates upward.
            return Mathf.Clamp01(effect.chance);
        }
    }
}
