using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// One reusable ability asset. The <see cref="type"/> field gates learnability - a Pom can
    /// only learn an ability whose type matches its own (see Pom.CanLearn). Raw effect values
    /// are authored here; final in-battle output is computed through PomAbilityFormula so the
    /// owning Pom's stats can scale the result without overwriting the ability's numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Ability Definition", fileName = "PomAbility_New")]
    public class PomAbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private PomType type;
        [SerializeField] private PomAbilityKind kind;
        [SerializeField, TextArea] private string description;

        [Header("Resolution")]
        [SerializeField] private PomAbilityTarget target;
        [SerializeField] private PomAbilityDuration duration;
        [SerializeField] private PomAbilityEffect effect = new();

        public string Id => id;
        public string DisplayName => displayName;
        public PomType Type => type;
        public PomAbilityKind Kind => kind;
        public string Description => description;
        public PomAbilityTarget Target => target;
        public PomAbilityDuration Duration => duration;
        public PomAbilityEffect Effect => effect;
    }
}
