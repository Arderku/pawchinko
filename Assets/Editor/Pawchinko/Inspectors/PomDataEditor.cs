using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace PawchinkoEditor
{
    /// <summary>
    /// Custom inspector for <see cref="PomData"/>. Adds a read-only "Ball Growth Preview" under the
    /// default fields that shows how many balls this Pom drops at each level for the selected
    /// <see cref="BallGrowthStyle"/>. The table is computed from <see cref="PomBallCount"/> - the
    /// single shared source of truth - so it reflects the exact counts used in battle, and makes it
    /// obvious that the curve is identical for every Pom that picks the same style.
    /// </summary>
    [CustomEditor(typeof(PomData))]
    public class PomDataEditor : Editor
    {
        // One sample per 5-level bracket (bracket start levels). The count is flat across each
        // bracket, so a single sample per bracket fully describes the curve.
        private static readonly int[] BracketStartLevels = { 1, 6, 11, 16, 21, 26, 31, 36, 41, 46 };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var data = (PomData)target;
            BallGrowthStyle style = data.BallGrowthStyle;
            var curve = PomBallCount.GetCurve(style);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ball Growth Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"'{PlayerFacingName(style)}' style. Balls per drop, shown per 5-level band (1..{PomBallCount.MaxPomLevel}).\n" +
                $"Every style changes ONLY every 5 levels - the count is flat inside each band.\n" +
                $"This curve is SHARED by every Pom with this style - it is not unique to this Pom. " +
                $"Range {curve.Min} -> {curve.Max} balls (game-wide cap {PomBallCount.MaxBallsCap}).",
                MessageType.Info);

            DrawTable(style);
        }

        private void DrawTable(BallGrowthStyle style)
        {
            float maxForBar = Mathf.Max(1, PomBallCount.GetCurve(style).Max);

            foreach (int startLevel in BracketStartLevels)
            {
                int endLevel = Mathf.Min(startLevel + 4, PomBallCount.MaxPomLevel);
                int balls = PomBallCount.GetBallCountForLevel(style, startLevel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Lv {startLevel}-{endLevel}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{balls}", GUILayout.Width(28));

                // Simple bar so the growth shape is readable at a glance.
                Rect r = GUILayoutUtility.GetRect(10, 14, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(r, new Color(0.18f, 0.18f, 0.18f));
                float fill = Mathf.Clamp01(balls / maxForBar);
                var filled = new Rect(r.x, r.y, r.width * fill, r.height);
                EditorGUI.DrawRect(filled, new Color(0.30f, 0.70f, 0.95f));

                EditorGUILayout.EndHorizontal();
            }
        }

        // Programmer enum -> player-facing label (the names locked in for a future UI).
        private static string PlayerFacingName(BallGrowthStyle style)
        {
            switch (style)
            {
                case BallGrowthStyle.SteadyPaws: return "Steady Paws";
                case BallGrowthStyle.PowerSpikes: return "Power Spikes";
                case BallGrowthStyle.GrowingRush: return "Growing Rush";
                case BallGrowthStyle.LateBloomer: return "Late Bloomer";
                case BallGrowthStyle.LuckyChaos: return "Lucky Chaos";
                default: return style.ToString();
            }
        }
    }
}
