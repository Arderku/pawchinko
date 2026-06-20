using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Creates runtime <see cref="PomInstance"/> records bound to a <see cref="PomData"/>
    /// asset. Single responsibility: instantiation; the data classes themselves stay free of
    /// constructor / validation behaviour.
    /// </summary>
    public static class PomFactory
    {
        /// <summary>
        /// Creates a runtime <see cref="PomInstance"/> bound to a <see cref="PomData"/> asset.
        /// Clamps the starting level to a minimum of 1, seeds the Action Point pool from the
        /// species <see cref="PomData.BaseAP"/>, and auto-fills the learned-ability slots from
        /// the species learnable pool (until a real progression/learning flow exists, this is
        /// what makes the in-battle ability picker show usable abilities).
        /// </summary>
        public static PomInstance CreatePomInstance(PomData data, int level = 1)
        {
            int ap = data != null ? data.BaseAP : 0;
            var instance = new PomInstance
            {
                data = data,
                level = Mathf.Max(1, level),
                experience = 0,
                maxAP = ap,
                currentAP = ap,
                learnedAbilities = new PomAbilityData[PomInstance.LearnedAbilitySlotCount]
            };

            AutoFillLearnedAbilities(instance);
            return instance;
        }

        /// <summary>
        /// Fills empty learned-ability slots from the species learnable pool (in order), skipping
        /// abilities the Pom cannot learn. Temporary stand-in for a real learning/progression flow.
        /// </summary>
        private static void AutoFillLearnedAbilities(PomInstance instance)
        {
            if (instance == null || instance.data == null) return;
            var pool = instance.data.LearnableAbilities;
            if (pool == null) return;

            int slot = 0;
            for (int i = 0; i < pool.Count && slot < PomInstance.LearnedAbilitySlotCount; i++)
            {
                var ability = pool[i];
                if (ability == null) continue;
                if (!PomAbilityLearning.CanLearn(instance, ability)) continue;
                instance.learnedAbilities[slot++] = ability;
            }
        }
    }
}
