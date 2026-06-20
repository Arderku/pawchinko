using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// What a single <see cref="AbilityEffect"/> does when an ability resolves at the start of a
    /// round (before balls drop). One ability can stack several effects. All effects last exactly
    /// one round.
    /// </summary>
    public enum AbilityEffectKind
    {
        /// <summary>Scales/sets the power of dropped balls (optionally only one ball type).</summary>
        BallPower = 0,

        /// <summary>Increases or decreases how many balls the side drops this round.</summary>
        BallCount = 1,

        /// <summary>Boosts/lowers a bucket's value, or restricts which ball type scores in it.</summary>
        BucketModifier = 2,

        /// <summary>Forces or biases which spawn slots (top zones) balls drop from.</summary>
        SpawnSlotBias = 3,

        /// <summary>Scales the energy the side collects from this round's score (percent).</summary>
        EnergyPercent = 4,

        /// <summary>Targets specific pegs: grow/shrink ball power when a ball hits them, or hide them.</summary>
        PegEffect = 5
    }

    /// <summary>What a <see cref="AbilityEffectKind.PegEffect"/> does to its targeted pegs.</summary>
    public enum PegAction
    {
        /// <summary>When a ball hits a targeted peg, modify the ball's power (mode/amount, per-hit chance, type-filtered).</summary>
        PowerOnHit = 0,

        /// <summary>Hide the targeted pegs for the round (renderer + collider off, so balls pass through).</summary>
        Hide = 1
    }

    /// <summary>How an effect's <c>amount</c> combines with the value it modifies.</summary>
    public enum AbilityValueMode
    {
        /// <summary>value * amount (e.g. 1.5 = +50%, 0.5 = halve).</summary>
        Multiply = 0,

        /// <summary>value + amount (e.g. +2, -1).</summary>
        Add = 1,

        /// <summary>value = amount (overwrite).</summary>
        Set = 2
    }

    /// <summary>
    /// Optional Pom-type gate reused for two things: the ability's "required type" (which Poms may
    /// use it) and per-effect ball-type filters (which balls an effect touches). <see cref="any"/>
    /// true means "no type restriction" - this is how "None / any" is expressed without adding a
    /// None entry to <see cref="PomType"/> (which would break the 6-type ball pipeline).
    /// </summary>
    [Preserve]
    [Serializable]
    public struct PomTypeFilter
    {
        [Tooltip("When true the filter matches every Pom type (no restriction). When false only 'type' matches.")]
        public bool any;
        [Tooltip("The single type this filter matches when 'any' is false.")]
        public PomType type;

        /// <summary>True when the filter places no restriction or the candidate equals the chosen type.</summary>
        public bool Matches(PomType candidate)
        {
            return any || candidate == type;
        }

        /// <summary>A filter that matches every type.</summary>
        public static PomTypeFilter Any => new PomTypeFilter { any = true };
    }

    /// <summary>
    /// One round-scoped effect on an ability. The fields used depend on <see cref="kind"/>; the
    /// custom property drawer hides the irrelevant ones. Authored on <see cref="PomAbilityData"/>.
    /// </summary>
    [Preserve]
    [Serializable]
    public class AbilityEffect
    {
        [Tooltip("What this effect does. Determines which other fields are used.")]
        public AbilityEffectKind kind = AbilityEffectKind.BallPower;

        [Tooltip("How 'amount' combines with the value (Multiply/Add/Set). Energy always uses the amount as a +/- percent.")]
        public AbilityValueMode mode = AbilityValueMode.Multiply;

        [Tooltip("The effect magnitude. Power/Bucket: multiplier or +/-. Ball Count: +/- (Add) or factor (Multiply). Energy: +/- percent (0.2 = +20%).")]
        public float amount = 1f;

        [Range(0f, 1f)]
        [Tooltip("Chance the effect applies. Ball Power / Spawn Slot roll PER BALL; Bucket / Ball Count / Energy roll ONCE when the round resolves.")]
        public float chance = 1f;

        [Tooltip("Ball-type filter. 'any' = affects all ball types; otherwise only the chosen type.")]
        public PomTypeFilter typeFilter = PomTypeFilter.Any;

        [Tooltip("Bucket Modifier / Spawn Slot Bias / Peg Effect: which indices to target (buckets 0..6, spawn zones 0..5, peg indices from the layout).")]
        public int[] targetIndices = Array.Empty<int>();

        [Tooltip("Bucket Modifier only: when true, balls that DON'T match the type filter score 0 in the targeted buckets ('only this type scores here').")]
        public bool typeExclusive;

        [Tooltip("Spawn Slot Bias only: when true, ALL balls spawn from the target zones; when false, each ball has 'chance' to be biased to them.")]
        public bool forceSpawn;

        [Tooltip("Peg Effect only: PowerOnHit modifies a ball's power when it hits a targeted peg; Hide removes the targeted pegs for the round.")]
        public PegAction pegAction = PegAction.PowerOnHit;
    }

    /// <summary>Tiny shared helper so every consumer combines an ability amount the same way.</summary>
    public static class AbilityMath
    {
        public static float Apply(float value, AbilityValueMode mode, float amount)
        {
            switch (mode)
            {
                case AbilityValueMode.Multiply: return value * amount;
                case AbilityValueMode.Add: return value + amount;
                case AbilityValueMode.Set: return amount;
                default: return value;
            }
        }
    }
}
