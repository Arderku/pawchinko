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
    /// How a Pom's balls-per-drop grows from level 1 to the game's max level. A Pom only *picks*
    /// a style - it authors no numbers. The actual balls-per-level curve for each style is defined
    /// once, globally, in <see cref="PomBallCount"/>, so <b>every Pom that shares a style shares the
    /// exact same balls-per-level table</b> (e.g. all Power Spikes Poms get the same count at level
    /// 5). The programmer name is the maths; the player-facing name (in the comment) is what the UI
    /// will eventually show. NOTE: every style only changes its ball count on the 5-level grid
    /// (Lv 1-5, 6-10, ... 46-50) and every style ends at the same destination - the 25-ball cap at
    /// level 50. They differ only in the starting count and the SHAPE of the climb (i.e. WHEN you get
    /// your balls), not in how many you get at the top.
    /// </summary>
    public enum BallGrowthStyle
    {
        /// <summary>"Steady Paws". Gentle front-loaded climb - starts highest, rises early, then eases
        /// into the cap. Banks the most balls over a run; the reliable pick.</summary>
        SteadyPaws = 0,

        /// <summary>Tiered / Step - "Power Spikes". Long flats punctuated by big jumps (uneven steps).</summary>
        PowerSpikes = 1,

        /// <summary>Linear - "Growing Rush". Even, predictable growth from min to the cap.</summary>
        GrowingRush = 2,

        /// <summary>Curve - "Late Bloomer". Starts lowest, weak most of the game, then a hard late surge
        /// to the cap. Fewest balls over a run (patience tax), same level-50 ceiling.</summary>
        LateBloomer = 3,

        /// <summary>Random Range - "Lucky Chaos". Bounces within a level-scaled band, then settles onto the
        /// cap at the top band; deterministic per 5-level bracket (not per Pom) so every Lucky Chaos Pom
        /// shows the same count at the same level.</summary>
        LuckyChaos = 4
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
        [Tooltip("Action Points this Pom has available for abilities. Refills to this max at the start of every round.")]
        [Min(0)]
        [SerializeField] private int baseAP = 3;
        [SerializeField] private PomBaseStats baseStats = new();

        [Header("Ball Growth (level -> balls per drop)")]
        [Tooltip("Only the GROWTH STYLE is picked here. The actual balls-per-level numbers are defined once per style in PomBallCount and are shared by every Pom with the same style. See the 'Ball Growth Preview' below the default fields.")]
        [SerializeField] private BallGrowthStyle ballGrowthStyle = BallGrowthStyle.GrowingRush;

        [Header("Abilities (learnable pool - type must match primary OR secondary)")]
        [SerializeField] private List<PomAbilityData> learnableAbilities = new();

        [Header("Visuals")]
        [Tooltip("3D prefab spawned on the PomPortraitStage for card portraits and (later) the in-battle creature stage. Should be the species visual root with an Animator (controller optional).")]
        [SerializeField] private GameObject portraitPrefab;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public PomRarity Rarity => rarity;

        public PomType PrimaryType => primaryType;
        public bool HasSecondaryType => hasSecondaryType;
        public PomType SecondaryType => secondaryType;

        public int MaxLevel => maxLevel;
        public int BaseEnergy => baseEnergy;
        public int BaseAP => baseAP;
        public PomBaseStats BaseStats => baseStats;

        public BallGrowthStyle BallGrowthStyle => ballGrowthStyle;

        public IReadOnlyList<PomAbilityData> LearnableAbilities => learnableAbilities;

        public GameObject PortraitPrefab => portraitPrefab;
    }

    /// <summary>
    /// Runtime instance of a Pom: a reference to an immutable <see cref="PomData"/> asset plus
    /// the mutable per-instance state (level, experience, current Action Points, the two learned
    /// ability slots). Pure data; team rosters hold these. All operations live in helper classes
    /// (<see cref="PomFactory"/>, <see cref="PomBallCount"/>, <see cref="PomAbilityLearning"/>).
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomInstance
    {
        public const int LearnedAbilitySlotCount = 2;

        public PomData data;
        public int level = 1;
        public int experience;

        /// <summary>Action Point pool for abilities. <see cref="currentAP"/> refills to
        /// <see cref="maxAP"/> at the start of every round (AbilityManager).</summary>
        public int maxAP;
        public int currentAP;

        public PomAbilityData[] learnedAbilities = new PomAbilityData[LearnedAbilitySlotCount];
    }
}
