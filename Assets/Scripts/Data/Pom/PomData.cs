using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Pom gameplay identity. Drives ball visuals, the learnable ability pool, and high-level
    /// design personality. NOT a damage-multiplier table (PAWCHINKO_DESIGN_GUIDE Section 10).
    /// </summary>
    public enum PomType
    {
        Chaos = 0,
        Calm = 1,
        Greedy = 2,
        Trick = 3,
        Lucky = 4,
        Wild = 5
    }

    /// <summary>
    /// Rarity tier for a Pom species. Placeholder set; drop rates TBD (Section 16).
    /// </summary>
    public enum PomRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// Base stat block (Section 9). Pure data; ability resolvers amplify their output using
    /// these stats without ever silently overwriting an ability's authored numbers.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomBaseStats
    {
        public float power;
        public float weight;
        public float luck;
        public float control;
    }

    /// <summary>
    /// One inclusive level band in a Pom's ball-count scale. Levels in [minLevel..maxLevel]
    /// contribute <see cref="count"/> balls per drop.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomBallCountLevelBand
    {
        public int minLevel;
        public int maxLevel;
        public int count;
    }

    /// <summary>
    /// Static per-species data for a Pom (Section 8). Pure data: a Pom has a primary type and
    /// optionally a secondary type (dual type); the ball it spawns inherits its primary type
    /// for visuals (Section 11). All operations on this data live in dedicated helper classes
    /// (<see cref="PomFactory"/>, <see cref="PomBallCount"/>, <see cref="PomAbilityLearning"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Pom Data", fileName = "Pom_New")]
    public class PomData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private PomRarity rarity;

        [Header("Types (1 or 2 - dual type optional)")]
        [SerializeField] private PomType primaryType;
        [SerializeField] private bool hasSecondaryType;
        [SerializeField] private PomType secondaryType;

        [Header("Battle Tuning")]
        [SerializeField] private int maxLevel = 50;
        [SerializeField] private int baseEnergy = 10;
        [SerializeField] private PomBaseStats baseStats = new();

        [Header("Ball Count Scale (level -> balls per drop)")]
        [SerializeField] private int baseBallCount = 1;
        [SerializeField] private List<PomBallCountLevelBand> ballCountLevelBands = new();

        [Header("Abilities (learnable pool - type must match primary OR secondary)")]
        [SerializeField] private List<PomAbilityData> learnableAbilities = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public PomRarity Rarity => rarity;

        public PomType PrimaryType => primaryType;
        public bool HasSecondaryType => hasSecondaryType;
        public PomType SecondaryType => secondaryType;

        public int MaxLevel => maxLevel;
        public int BaseEnergy => baseEnergy;
        public PomBaseStats BaseStats => baseStats;

        public int BaseBallCount => baseBallCount;
        public IReadOnlyList<PomBallCountLevelBand> BallCountLevelBands => ballCountLevelBands;

        public IReadOnlyList<PomAbilityData> LearnableAbilities => learnableAbilities;
    }

    /// <summary>
    /// Runtime instance of a Pom: a reference to an immutable <see cref="PomData"/> asset plus
    /// the mutable per-instance state (level, experience, the two learned ability slots). Pure
    /// data; team rosters hold these. All operations live in helper classes (<see
    /// cref="PomFactory"/>, <see cref="PomBallCount"/>, <see cref="PomAbilityLearning"/>).
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomInstance
    {
        public const int LearnedAbilitySlotCount = 2;

        public PomData data;
        public int level = 1;
        public int experience;
        public PomAbilityData[] learnedAbilities = new PomAbilityData[LearnedAbilitySlotCount];
    }
}
