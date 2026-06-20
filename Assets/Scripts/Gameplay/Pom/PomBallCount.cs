using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Ball-count math: how many balls a Pom contributes per drop at a given level.
    ///
    /// The balls-per-level curve is a property of the <see cref="BallGrowthStyle"/>, NOT of the
    /// individual Pom. Every Pom only picks a style; the actual min / max / step numbers for each
    /// style are defined once here, so <b>all Poms that share a style share the exact same
    /// balls-per-level table</b> (two Power Spikes Poms both give the same count at level 5). The
    /// table is fixed in code, so it is identical on every machine and run.
    ///
    /// Every result is clamped to <see cref="MaxBallsCap"/> (25) - the game-wide rule that no Pom
    /// drops more than 25 balls at any level - over the global level range 1..<see cref="MaxPomLevel"/>.
    ///
    /// All styles change on the same 5-level cadence (<see cref="StepLevelInterval"/>): the count is
    /// flat within each bracket (Lv 1-5, 6-10, ... 46-50) and only changes at a bracket boundary.
    /// The styles differ in the SHAPE of that 10-step climb, not in how often they change.
    /// </summary>
    public static class PomBallCount
    {
        /// <summary>Hard ceiling on balls a single Pom contributes at any level. Game-wide rule.</summary>
        public const int MaxBallsCap = 25;

        /// <summary>Global max creature level the growth curve is defined over (design: 1..50).</summary>
        public const int MaxPomLevel = 50;

        /// <summary>Stepped styles (Power Spikes) change the ball count once every this many levels.</summary>
        public const int StepLevelInterval = 5;

        private const float SteadyPawsExponent = 0.6f;       // < 1 = front-loaded: climbs early, then levels off
        private const float LateBloomerExponent = 2.0f;      // > 1 = back-loaded: slow early, blooms late
        private const float LuckyChaosBandHalfWidth = 0.3f;  // fraction of (max-min) the count can swing

        /// <summary>Min balls (level 1) and max balls (max level) for a style.</summary>
        public readonly struct StyleCurve
        {
            public readonly int Min;
            public readonly int Max;

            public StyleCurve(int min, int max)
            {
                Min = min;
                Max = max;
            }
        }

        // ====================================================================================
        //  EDIT BALLS-PER-TYPE HERE. This is the single source of truth for every style's
        //  balls-per-level numbers, shared by all Poms with that style. Min = balls at level 1,
        //  Max = balls at the top bracket. EVERY style changes only on the 5-level grid
        //  (Lv 1-5, 6-10, ... 46-50) - see Evaluate. All values stay within the 25-ball cap.
        //
        //  Balance intent: every style ENDS at the same destination - the 25-ball cap at level 50 -
        //  so none is strictly best at max level. They differ only in the JOURNEY (the Min and the
        //  shape of the climb):
        //    - Steady Paws  : starts HIGHEST, front-loaded climb that levels off (reliable early).
        //    - Growing Rush : even climb (the dependable baseline).
        //    - Power Spikes : long flats then BIG jumps (uneven; see PowerSpikeShape).
        //    - Late Bloomer : starts LOWEST, weak for most of the game, biggest late surge to the cap.
        //    - Lucky Chaos  : bounces around the line, then settles onto the cap at the top band.
        // ====================================================================================
        /// <summary>The single shared balls-per-level curve for each style. Edit these to retune.
        /// Max is the level-50 (top bracket) count; all styles cap out at <see cref="MaxBallsCap"/>.</summary>
        public static StyleCurve GetCurve(BallGrowthStyle style)
        {
            switch (style)
            {
                case BallGrowthStyle.SteadyPaws: return new StyleCurve(4, MaxBallsCap);   // front-loaded, levels off
                case BallGrowthStyle.GrowingRush: return new StyleCurve(2, MaxBallsCap);  // even climb
                case BallGrowthStyle.PowerSpikes: return new StyleCurve(2, MaxBallsCap);  // long flats then big jumps
                case BallGrowthStyle.LateBloomer: return new StyleCurve(1, MaxBallsCap);  // slow then big finish
                case BallGrowthStyle.LuckyChaos: return new StyleCurve(2, MaxBallsCap);   // bounce, settles on cap at top
                default: return new StyleCurve(1, 1);
            }
        }

        // Power Spikes shape: cumulative progress (min->max) per 5-level bracket. Long-ish flats
        // punctuated by big jumps (brackets 2->3 and 5->6) so it reads as real "spikes" rather than
        // a smooth climb. One value per bracket; must be non-decreasing and end at 1.
        private static readonly float[] PowerSpikeShape =
            { 0f, 0.10f, 0.15f, 0.45f, 0.50f, 0.55f, 0.85f, 0.90f, 0.95f, 1f };

        /// <summary>Balls this Pom contributes at <paramref name="level"/> (uses its growth style).</summary>
        public static int GetBallCountForLevel(PomData data, int level)
        {
            return data == null ? 0 : GetBallCountForLevel(data.BallGrowthStyle, level);
        }

        /// <summary>Balls any Pom of the given <paramref name="style"/> contributes at <paramref name="level"/>.</summary>
        public static int GetBallCountForLevel(BallGrowthStyle style, int level)
        {
            return Evaluate(style, level);
        }

        /// <summary>Ball count the runtime instance contributes at its current level.</summary>
        public static int GetCurrentBallCount(PomInstance instance)
        {
            return instance != null ? GetBallCountForLevel(instance.data, instance.level) : 0;
        }

        /// <summary>
        /// Core evaluator: maps (style, level) -> balls using the shared style curve. The ball
        /// count is quantized to the 5-level grid (StepLevelInterval): it is flat within each
        /// bracket (Lv 1-5, 6-10, ... 46-50) and only changes at a bracket boundary, for EVERY
        /// style. The styles differ in the SHAPE of that 10-step climb. Clamps level into
        /// [1, <see cref="MaxPomLevel"/>] and the result into [0, cap].
        /// </summary>
        public static int Evaluate(BallGrowthStyle style, int level)
        {
            StyleCurve curve = GetCurve(style);
            int min = Mathf.Clamp(curve.Min, 0, MaxBallsCap);
            int max = Mathf.Clamp(Mathf.Max(curve.Max, min), min, MaxBallsCap);
            int lvl = Mathf.Clamp(level, 1, MaxPomLevel);

            // Which 5-level bracket the level falls in, and its position 0..1 across all brackets.
            int interval = Mathf.Max(1, StepLevelInterval);
            int bracket = (lvl - 1) / interval;               // 0..topBracket (e.g. 0..9)
            int topBracket = (MaxPomLevel - 1) / interval;
            float bt = topBracket > 0 ? bracket / (float)topBracket : 0f;

            float value;
            switch (style)
            {
                case BallGrowthStyle.SteadyPaws:
                    value = Mathf.Lerp(min, max, Mathf.Pow(bt, SteadyPawsExponent));
                    break;
                case BallGrowthStyle.GrowingRush:
                    value = Mathf.Lerp(min, max, bt);
                    break;
                case BallGrowthStyle.LateBloomer:
                    value = Mathf.Lerp(min, max, Mathf.Pow(bt, LateBloomerExponent));
                    break;
                case BallGrowthStyle.PowerSpikes:
                    value = Mathf.Lerp(min, max, PowerSpikeShape[Mathf.Clamp(bracket, 0, PowerSpikeShape.Length - 1)]);
                    break;
                case BallGrowthStyle.LuckyChaos:
                    value = EvaluateLuckyChaos(min, max, bt, bracket);
                    break;
                default:
                    value = min;
                    break;
            }

            return Mathf.Clamp(Mathf.RoundToInt(value), 0, MaxBallsCap);
        }

        // A band centred on the linear value, sliding up with the bracket, sampled by deterministic
        // noise keyed to the BRACKET ONLY - so the bounce changes only every 5 levels, is identical
        // for every Lucky Chaos Pom, and never drifts across machines/runs. The band tapers to zero
        // as bt -> 1, so the top bracket lands exactly on the cap (same level-50 destination as the
        // other styles) while still bouncing earlier on.
        private static float EvaluateLuckyChaos(int min, int max, float bt, int bracket)
        {
            float center = Mathf.Lerp(min, max, bt);
            float half = (max - min) * LuckyChaosBandHalfWidth * (1f - bt);
            float u = Hash01(bracket);
            return Mathf.Clamp(Mathf.Lerp(center - half, center + half, u), min, max);
        }

        // Stable, deterministic, well-distributed value in [0,1) from an integer key (a bracket
        // index here), using an integer finalizer with multiple mix rounds. Same input -> same
        // output on every machine and run, so Lucky Chaos counts never drift, and the spread stays
        // roughly uniform (so the bounce sits symmetrically around the line, not biased low/high).
        private static float Hash01(int key)
        {
            unchecked
            {
                uint h = (uint)key + 0x9E3779B9u;
                h ^= h >> 16;
                h *= 0x21F0AAADu;
                h ^= h >> 15;
                h *= 0x735A2D97u;
                h ^= h >> 15;
                return (h >> 8) / (float)(1u << 24); // top 24 bits -> [0,1)
            }
        }
    }
}
