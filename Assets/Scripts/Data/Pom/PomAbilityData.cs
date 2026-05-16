using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Broad category an ability falls into. Drives icon / UI grouping; concrete effect math
    /// lives in the resolver that consumes it (TBD).
    /// </summary>
    public enum PomAbilityCategory
    {
        SelfBuff = 0,
        EnemyDebuff = 1,
        PegBuff = 2,
        PegDebuff = 3,
        BucketModifier = 4,
        BallModifier = 5,
        BoardScramble = 6
    }

    /// <summary>Which board(s) the ability applies to when resolved.</summary>
    public enum PomAbilityBoardTarget
    {
        Self = 0,
        Enemy = 1,
        Both = 2
    }

    /// <summary>
    /// One ability asset (Section 13). The <see cref="type"/> field gates learnability: a Pom
    /// can only learn an ability whose type matches one of its own types (primary or
    /// secondary). Every ability lasts exactly one round - duration is not authored.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Ability Data", fileName = "PomAbility_New")]
    public class PomAbilityData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private PomType type;
        [SerializeField] private PomAbilityCategory category;

        [Header("Resolution")]
        [SerializeField] private PomAbilityBoardTarget boardTarget;
        [SerializeField] private float effectValue;
        [SerializeField, Range(0f, 1f)] private float procChance = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public PomType Type => type;
        public PomAbilityCategory Category => category;
        public PomAbilityBoardTarget BoardTarget => boardTarget;
        public float EffectValue => effectValue;
        public float ProcChance => procChance;
    }
}
