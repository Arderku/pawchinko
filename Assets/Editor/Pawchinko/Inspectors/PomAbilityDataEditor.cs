using System.Text;
using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace PawchinkoEditor
{
    /// <summary>
    /// Custom inspector for <see cref="PomAbilityData"/>. Draws the default fields (the effects list
    /// uses <see cref="AbilityEffectDrawer"/> so only the relevant fields per kind are shown) and
    /// adds a plain-language summary: who can use it, AP cost, board target, and what each effect
    /// does, so authoring stays readable without decoding raw numbers.
    /// </summary>
    [CustomEditor(typeof(PomAbilityData))]
    public class PomAbilityDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var ability = (PomAbilityData)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ability Summary", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(BuildSummary(ability), MessageType.Info);
        }

        private static string BuildSummary(PomAbilityData a)
        {
            var sb = new StringBuilder();
            string usable = a.RequiredType.any ? "any Pom" : $"{a.RequiredType.type} Poms";
            sb.AppendLine($"Usable by: {usable}");
            sb.AppendLine($"AP cost: {a.ApCost}");
            sb.AppendLine($"Applies to: {a.BoardTarget} board");
            sb.AppendLine();

            var effects = a.Effects;
            if (effects == null || effects.Count == 0)
            {
                sb.Append("No effects authored yet.");
                return sb.ToString();
            }

            sb.AppendLine($"Effects ({effects.Count}):");
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e == null) continue;
                sb.AppendLine($"  \u2022 {Describe(e)}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string Describe(AbilityEffect e)
        {
            string balls = e.typeFilter.any ? "all balls" : $"{e.typeFilter.type} balls";
            string chance = e.chance >= 1f ? "always" : $"{Mathf.RoundToInt(e.chance * 100f)}% chance";

            switch (e.kind)
            {
                case AbilityEffectKind.BallPower:
                    return $"Ball power {Mode(e)} for {balls} ({chance}, per ball).";
                case AbilityEffectKind.BallCount:
                    return $"Ball count {Mode(e)} for the side ({chance}).";
                case AbilityEffectKind.BucketModifier:
                    string excl = e.typeExclusive ? $"; only {balls} score here" : "";
                    return $"Buckets [{Join(e.targetIndices)}] value {Mode(e)} for {balls}{excl} ({chance} per bucket).";
                case AbilityEffectKind.SpawnSlotBias:
                    return e.forceSpawn
                        ? $"Force every ball to spawn from zones [{Join(e.targetIndices)}]."
                        : $"Bias balls toward spawn zones [{Join(e.targetIndices)}] ({chance}, per ball).";
                case AbilityEffectKind.EnergyPercent:
                    return $"Energy collected this round {(e.amount >= 0f ? "+" : "")}{Mathf.RoundToInt(e.amount * 100f)}% ({chance}).";
                case AbilityEffectKind.PegEffect:
                    return e.pegAction == PegAction.Hide
                        ? $"Hide pegs [{Join(e.targetIndices)}] for the round ({chance} per peg)."
                        : $"Pegs [{Join(e.targetIndices)}] change ball power {Mode(e)} for {balls} on hit ({chance} per hit).";
                default:
                    return e.kind.ToString();
            }
        }

        private static string Mode(AbilityEffect e)
        {
            switch (e.mode)
            {
                case AbilityValueMode.Multiply: return $"x{e.amount:0.##}";
                case AbilityValueMode.Add: return $"{(e.amount >= 0f ? "+" : "")}{e.amount:0.##}";
                case AbilityValueMode.Set: return $"set to {e.amount:0.##}";
                default: return e.amount.ToString("0.##");
            }
        }

        private static string Join(int[] arr)
        {
            return arr == null || arr.Length == 0 ? "-" : string.Join(", ", arr);
        }
    }
}
