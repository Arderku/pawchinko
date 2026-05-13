using System;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Generic bag of raw tunable values an ability uses when it resolves. Kept as a flat
    /// record (not a polymorphic hierarchy) so existing JSON payloads round-trip cleanly and
    /// new effect kinds can land without churn. PomAbilityFormula is the single place that
    /// translates these raw numbers into a final in-battle outcome using Pom stats.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomAbilityEffect
    {
        public string type = string.Empty;
        public string operation = string.Empty;
        public float value;
        public float positiveValue;
        public float negativeValue;
        public int targets;
        public int pegCount;
        public float chance;
    }
}
