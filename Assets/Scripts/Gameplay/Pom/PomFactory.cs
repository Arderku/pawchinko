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
        /// Clamps the starting level to a minimum of 1 and allocates the learned-ability slot
        /// array.
        /// </summary>
        public static PomInstance CreatePomInstance(PomData data, int level = 1)
        {
            return new PomInstance
            {
                data = data,
                level = Mathf.Max(1, level),
                experience = 0,
                learnedAbilities = new PomAbilityData[PomInstance.LearnedAbilitySlotCount]
            };
        }
    }
}
