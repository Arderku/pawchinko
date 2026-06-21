using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Per-<see cref="PomType"/> colors. These mirror the ball palette authored in
    /// <c>BuildBallVisuals</c> so type-based FX (e.g. the ability peg preview) read in the same
    /// color as that type's balls. Kept tiny and dependency-free so both UI and gameplay can use it.
    /// </summary>
    public static class PomTypeColors
    {
        public static readonly Color Chaos = new(0.85f, 0.15f, 0.85f);  // glitchy purple
        public static readonly Color Calm = new(0.30f, 0.70f, 0.95f);   // soft blue
        public static readonly Color Greedy = new(1.00f, 0.82f, 0.10f); // treasure gold
        public static readonly Color Trick = new(0.30f, 0.85f, 0.40f);  // carnival green
        public static readonly Color Lucky = new(1.00f, 0.45f, 0.70f);  // charm pink
        public static readonly Color Wild = new(1.00f, 0.50f, 0.10f);   // primal orange

        /// <summary>The canonical color for a Pom/ball type.</summary>
        public static Color For(PomType type)
        {
            switch (type)
            {
                case PomType.Chaos: return Chaos;
                case PomType.Calm: return Calm;
                case PomType.Greedy: return Greedy;
                case PomType.Trick: return Trick;
                case PomType.Lucky: return Lucky;
                case PomType.Wild: return Wild;
                default: return Color.white;
            }
        }

        /// <summary>
        /// Color that represents an ability: its required type's color when it is type-locked,
        /// otherwise the casting Pom's primary type color (white if neither is known).
        /// </summary>
        public static Color ForAbility(PomAbilityData ability, PomInstance caster)
        {
            if (ability != null && !ability.RequiredType.any) return For(ability.RequiredType.type);
            if (caster != null && caster.data != null) return For(caster.data.PrimaryType);
            return Color.white;
        }
    }
}
