using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Picks the <see cref="PomType"/> of a ball a Pom is about to spawn. A single-type Pom
    /// always spawns its primary type; a dual-type Pom rolls a fresh 50/50 between primary and
    /// secondary for every ball, so a Chaos/Calm Pom drops a mix of Chaos and Calm balls.
    ///
    /// Kept as a tiny stateless helper (mirrors <see cref="PomBallCount"/>) so the roll has one
    /// home and the spawner stays focused on instantiation.
    /// </summary>
    public static class PomBallType
    {
        /// <summary>Rolls the spawned ball type for the given species data.</summary>
        public static PomType Roll(PomData data)
        {
            if (data == null) return default;
            if (data.HasSecondaryType && Random.value < 0.5f) return data.SecondaryType;
            return data.PrimaryType;
        }

        /// <summary>Rolls the spawned ball type for a runtime Pom instance.</summary>
        public static PomType Roll(PomInstance instance)
        {
            return instance != null ? Roll(instance.data) : default;
        }
    }
}
