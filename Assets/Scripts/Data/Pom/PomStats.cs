using System;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Base stat block for a Pom (PAWCHINKO_DESIGN_GUIDE Section 9). These are the only stats
    /// the design guide defines; ability output is computed from these via PomAbilityFormula
    /// (raw ability numbers are never replaced silently).
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomStats
    {
        public float power;
        public float weight;
        public float luck;
        public float control;

        public PomStats() { }

        public PomStats(float power, float weight, float luck, float control)
        {
            this.power = power;
            this.weight = weight;
            this.luck = luck;
            this.control = control;
        }
    }
}
