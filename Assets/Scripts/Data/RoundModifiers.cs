using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Per-side, per-round bag of resolved ability effects, built by <see cref="AbilityManager"/>
    /// when abilities are locked in and read by the gameplay systems for that one round:
    /// <list type="bullet">
    /// <item>BattleManager - ball count (<see cref="ApplyBallCount"/>)</item>
    /// <item>BallSpawner - spawn-zone bias (<see cref="RollAllowedZones"/>) + per-ball power (<see cref="ApplyBallPower"/>)</item>
    /// <item>ScoringManager - bucket value rules (<see cref="ApplyBucket"/>)</item>
    /// <item>EnergyManager - energy collected (<see cref="EnergyPercent"/>)</item>
    /// </list>
    /// Per-round chances (bucket / ball count / energy) are rolled once when the modifiers are
    /// built, so anything stored here is already "active". Per-ball chances (ball power / spawn
    /// bias) are stored and rolled at apply time, once per ball.
    /// </summary>
    [Preserve]
    public class RoundModifiers
    {
        public struct BallPowerEntry
        {
            public PomTypeFilter filter;
            public AbilityValueMode mode;
            public float amount;
            public float chance; // rolled per ball
        }

        public struct BucketEntry
        {
            public int slot;
            public PomTypeFilter filter;
            public bool typeExclusive;
            public AbilityValueMode mode;
            public float amount;
        }

        public struct SpawnBiasEntry
        {
            public int[] zones;
            public bool force;
            public float chance; // rolled per ball when not forced
        }

        public struct PegPowerEntry
        {
            public int[] pegs;
            public PomTypeFilter filter;
            public AbilityValueMode mode;
            public float amount;
            public float chance; // rolled per peg hit
        }

        public readonly List<BallPowerEntry> BallPower = new();
        public readonly List<BucketEntry> Buckets = new();
        public readonly List<SpawnBiasEntry> SpawnBias = new();
        public readonly List<PegPowerEntry> PegPower = new();
        public readonly HashSet<int> HiddenPegs = new();
        public int BallCountAdd;
        public float BallCountMult = 1f;
        public float EnergyPercent;

        /// <summary>True when nothing was applied this round (lets consumers skip work).</summary>
        public bool IsEmpty =>
            BallPower.Count == 0 && Buckets.Count == 0 && SpawnBias.Count == 0
            && PegPower.Count == 0 && HiddenPegs.Count == 0
            && BallCountAdd == 0 && Mathf.Approximately(BallCountMult, 1f)
            && Mathf.Approximately(EnergyPercent, 0f);

        public void Clear()
        {
            BallPower.Clear();
            Buckets.Clear();
            SpawnBias.Clear();
            PegPower.Clear();
            HiddenPegs.Clear();
            BallCountAdd = 0;
            BallCountMult = 1f;
            EnergyPercent = 0f;
        }

        /// <summary>
        /// Final power for one ball: starts from <paramref name="basePower"/> and applies every
        /// matching ball-power entry, rolling each entry's per-ball chance. Never returns &lt; 0.
        /// </summary>
        public float ApplyBallPower(float basePower, PomType ballType)
        {
            float power = basePower;
            for (int i = 0; i < BallPower.Count; i++)
            {
                var e = BallPower[i];
                if (!e.filter.Matches(ballType)) continue;
                if (e.chance < 1f && Random.value > e.chance) continue;
                power = AbilityMath.Apply(power, e.mode, e.amount);
            }
            return Mathf.Max(0f, power);
        }

        /// <summary>Final ball count for the side after the (already-rolled) count modifiers.</summary>
        public int ApplyBallCount(int baseCount)
        {
            float scaled = baseCount * BallCountMult + BallCountAdd;
            return Mathf.Max(0, Mathf.RoundToInt(scaled));
        }

        /// <summary>
        /// Bucket value for a ball of <paramref name="ballType"/> landing in <paramref name="slot"/>.
        /// Applies every bucket entry targeting that slot. A type-exclusive entry zeroes the value
        /// for non-matching ball types ("only this type scores here"). Never returns &lt; 0.
        /// </summary>
        public int ApplyBucket(int slotValue, int slot, PomType ballType)
        {
            int value = slotValue;
            for (int i = 0; i < Buckets.Count; i++)
            {
                var b = Buckets[i];
                if (b.slot != slot) continue;
                bool matches = b.filter.Matches(ballType);
                if (b.typeExclusive && !matches) return 0;
                if (!matches) continue;
                value = Mathf.RoundToInt(AbilityMath.Apply(value, b.mode, b.amount));
            }
            return Mathf.Max(0, value);
        }

        /// <summary>
        /// Spawn zones one ball is allowed to use, or null for "no restriction (all zones)". Forced
        /// biases win and are unioned; otherwise the first chance-based bias that rolls true applies.
        /// </summary>
        public int[] RollAllowedZones()
        {
            if (SpawnBias.Count == 0) return null;

            List<int> forced = null;
            for (int i = 0; i < SpawnBias.Count; i++)
            {
                var s = SpawnBias[i];
                if (s.zones == null || s.zones.Length == 0 || !s.force) continue;
                (forced ??= new List<int>()).AddRange(s.zones);
            }
            if (forced != null && forced.Count > 0) return forced.ToArray();

            for (int i = 0; i < SpawnBias.Count; i++)
            {
                var s = SpawnBias[i];
                if (s.zones == null || s.zones.Length == 0) continue;
                if (s.chance >= 1f || Random.value <= s.chance) return (int[])s.zones.Clone();
            }
            return null;
        }

        /// <summary>
        /// New ball power after hitting peg <paramref name="pegIndex"/>: applies every peg-power
        /// entry that targets the peg and matches the ball type, rolling each entry's per-hit
        /// chance. Returns <paramref name="power"/> unchanged when no entry applies. Never &lt; 0.
        /// </summary>
        public float ApplyPegHit(float power, int pegIndex, PomType ballType)
        {
            if (PegPower.Count == 0) return power;
            float result = power;
            for (int i = 0; i < PegPower.Count; i++)
            {
                var e = PegPower[i];
                if (!Contains(e.pegs, pegIndex)) continue;
                if (!e.filter.Matches(ballType)) continue;
                if (e.chance < 1f && Random.value > e.chance) continue;
                result = AbilityMath.Apply(result, e.mode, e.amount);
            }
            return Mathf.Max(0f, result);
        }

        /// <summary>True when the peg is hidden this round (collider + renderer disabled).</summary>
        public bool IsPegHidden(int pegIndex)
        {
            return HiddenPegs.Count > 0 && HiddenPegs.Contains(pegIndex);
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

        private static readonly RoundModifiers SharedEmpty = new();

        /// <summary>A shared, always-empty instance so callers never have to null-check.</summary>
        public static RoundModifiers Empty => SharedEmpty;
    }
}
