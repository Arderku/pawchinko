using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Maps each <see cref="PomType"/> to the ball prefab spawned for that type. One shared asset
    /// is referenced by every <see cref="BallSpawner"/> (both sides) - player and enemy balls are
    /// visually identical, so the type, not the side, decides the look and feel of a ball. Each
    /// per-type prefab carries its own mesh material and its own PhysicsMaterial, so e.g. Calm
    /// balls can be made less bouncy than Chaos balls purely through their prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Ball Library", fileName = "BallLibrary")]
    public class BallLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public PomType type;
            public Ball prefab;
        }

        [Tooltip("One entry per PomType. The prefab is the per-type ball variant (own material + PhysicsMaterial).")]
        [SerializeField] private List<Entry> entries = new();

        [Tooltip("Used when a type has no entry (or its prefab is missing) so a drop never silently fails.")]
        [SerializeField] private Ball fallbackPrefab;

        /// <summary>
        /// Returns the ball prefab for <paramref name="type"/>, or <see cref="fallbackPrefab"/>
        /// when the type is unmapped. May return null only if neither is configured.
        /// </summary>
        public Ball GetPrefab(PomType type)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.type == type && entry.prefab != null) return entry.prefab;
            }
            return fallbackPrefab;
        }
    }
}
