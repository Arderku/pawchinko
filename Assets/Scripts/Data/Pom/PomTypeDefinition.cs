using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Authored-once metadata for a single Pom type (PAWCHINKO_DESIGN_GUIDE Section 10). Holds
    /// the type's identity, gameplay style, default behavior knobs, and the soft loop hints
    /// (good against / weak against). These are NOT damage multipliers - they only describe
    /// gameplay flow; counterplay still happens through abilities.
    /// </summary>
    [CreateAssetMenu(menuName = "Pawchinko/Pom/Type Definition", fileName = "PomType_New")]
    public class PomTypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private PomType type;
        [SerializeField] private string theme;
        [SerializeField, TextArea] private string playstyle;

        [Header("Loop (informational - NOT a damage multiplier)")]
        [SerializeField] private List<PomType> strengthsAgainst = new();
        [SerializeField] private List<PomType> weaknessAgainst = new();

        [Header("Defaults")]
        [SerializeField] private PomTypeBehavior defaultBehavior = new();
        [SerializeField] private PomVisualIdentity defaultVisuals = new();

        public PomType Type => type;
        public string Theme => theme;
        public string Playstyle => playstyle;
        public IReadOnlyList<PomType> StrengthsAgainst => strengthsAgainst;
        public IReadOnlyList<PomType> WeaknessAgainst => weaknessAgainst;
        public PomTypeBehavior DefaultBehavior => defaultBehavior;
        public PomVisualIdentity DefaultVisuals => defaultVisuals;
    }
}
