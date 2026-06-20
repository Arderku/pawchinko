using System.Collections.Generic;
using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace PawchinkoEditor
{
    /// <summary>
    /// Property drawer for <see cref="AbilityEffect"/>. Shows only the fields that matter for the
    /// selected <see cref="AbilityEffectKind"/> so designers aren't presented with bucket indices
    /// on a ball-power effect, a type filter on an energy effect, and so on.
    ///
    /// For <see cref="AbilityEffectKind.PegEffect"/> the raw <c>targetIndices</c> array is replaced
    /// by a clickable, board-shaped peg picker driven by the generated <see cref="PegLayout"/>
    /// asset (run "Pawchinko/Build Board Pegs" to create it). Without that asset it falls back to
    /// the plain index array.
    /// </summary>
    [CustomPropertyDrawer(typeof(AbilityEffect))]
    public class AbilityEffectDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const string PegLayoutPath = "Assets/Data/Board/PegLayout.asset";

        private const float PegBoxHeight = 150f;
        private const float PegCellW = 22f;
        private const float PegCellH = 16f;
        private const float PegPad = 5f;

        // Scalar fields visible per kind (peg targeting is drawn separately).
        private static IEnumerable<string> VisibleFields(AbilityEffectKind kind)
        {
            yield return "kind";
            switch (kind)
            {
                case AbilityEffectKind.BallPower:
                    yield return "typeFilter";
                    yield return "mode";
                    yield return "amount";
                    yield return "chance";
                    break;
                case AbilityEffectKind.BallCount:
                    yield return "mode";
                    yield return "amount";
                    yield return "chance";
                    break;
                case AbilityEffectKind.BucketModifier:
                    yield return "targetIndices";
                    yield return "typeFilter";
                    yield return "typeExclusive";
                    yield return "mode";
                    yield return "amount";
                    yield return "chance";
                    break;
                case AbilityEffectKind.SpawnSlotBias:
                    yield return "targetIndices";
                    yield return "forceSpawn";
                    yield return "chance";
                    break;
                case AbilityEffectKind.EnergyPercent:
                    yield return "amount";
                    yield return "chance";
                    break;
                // PegEffect is resolved separately (see ResolvedFields) because its visible
                // fields depend on the live pegAction value and it owns a custom picker.
            }
        }

        // Power/type/chance fields shown for PegEffect depending on its action.
        private static IEnumerable<string> PegScalarFields(PegAction action)
        {
            if (action == PegAction.PowerOnHit)
            {
                yield return "typeFilter";
                yield return "mode";
                yield return "amount";
            }
            yield return "chance"; // per-hit (power) or per-peg (hide)
        }

        private static AbilityEffectKind GetKind(SerializedProperty property)
        {
            var kindProp = property.FindPropertyRelative("kind");
            return kindProp != null ? (AbilityEffectKind)kindProp.enumValueIndex : AbilityEffectKind.BallPower;
        }

        private static PegAction GetPegAction(SerializedProperty property)
        {
            var p = property.FindPropertyRelative("pegAction");
            return p != null ? (PegAction)p.enumValueIndex : PegAction.PowerOnHit;
        }

        private static IEnumerable<string> ResolvedFields(SerializedProperty property)
        {
            var kind = GetKind(property);
            if (kind != AbilityEffectKind.PegEffect)
            {
                foreach (var f in VisibleFields(kind)) yield return f;
                yield break;
            }

            yield return "kind";
            yield return "pegAction";
            foreach (var f in PegScalarFields(GetPegAction(property))) yield return f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            foreach (var fieldName in ResolvedFields(property))
            {
                var prop = property.FindPropertyRelative(fieldName);
                if (prop == null) continue;
                float h = EditorGUI.GetPropertyHeight(prop, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), prop, true);
                y += h + Spacing;
            }

            if (GetKind(property) == AbilityEffectKind.PegEffect)
            {
                y = DrawPegPicker(new Rect(position.x, y, position.width, 0f), property);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float total = 0f;
            foreach (var fieldName in ResolvedFields(property))
            {
                var prop = property.FindPropertyRelative(fieldName);
                if (prop == null) continue;
                total += EditorGUI.GetPropertyHeight(prop, true) + Spacing;
            }

            if (GetKind(property) == AbilityEffectKind.PegEffect)
            {
                total += PegPickerHeight(property);
            }

            return total;
        }

        // ---- Peg picker --------------------------------------------------------------------

        private static float PegPickerHeight(SerializedProperty property)
        {
            var layout = LoadLayout();
            float line = EditorGUIUtility.singleLineHeight;
            if (layout == null || layout.count <= 0)
            {
                // Fallback: header + hint + raw array field.
                var arr = property.FindPropertyRelative("targetIndices");
                float arrH = arr != null ? EditorGUI.GetPropertyHeight(arr, true) : line;
                return (line + Spacing) * 2f + arrH + Spacing;
            }
            // Header line + toolbar line + board box + count line.
            return (line + Spacing) * 2f + PegBoxHeight + Spacing + line + Spacing;
        }

        private float DrawPegPicker(Rect rect, SerializedProperty property)
        {
            var indices = property.FindPropertyRelative("targetIndices");
            float y = rect.y;
            float line = EditorGUIUtility.singleLineHeight;
            var layout = LoadLayout();

            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, line), "Target Pegs", EditorStyles.boldLabel);
            y += line + Spacing;

            if (layout == null || layout.count <= 0)
            {
                EditorGUI.LabelField(new Rect(rect.x, y, rect.width, line), "Run 'Pawchinko/Build Board Pegs' for a visual picker.", EditorStyles.miniLabel);
                y += line + Spacing;
                if (indices != null)
                {
                    float arrH = EditorGUI.GetPropertyHeight(indices, true);
                    EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, arrH), indices, true);
                    y += arrH + Spacing;
                }
                return y;
            }

            var selected = ReadSelected(indices);

            // Toolbar: select all / clear.
            float btnW = 70f;
            if (GUI.Button(new Rect(rect.x, y, btnW, line), "Select All"))
            {
                selected.Clear();
                for (int i = 0; i < layout.count; i++) selected.Add(i);
                WriteSelected(indices, selected);
            }
            if (GUI.Button(new Rect(rect.x + btnW + 4f, y, btnW, line), "Clear"))
            {
                selected.Clear();
                WriteSelected(indices, selected);
            }
            y += line + Spacing;

            var box = new Rect(rect.x, y, rect.width, PegBoxHeight);
            GUI.Box(box, GUIContent.none);

            float innerW = Mathf.Max(1f, box.width - PegPad * 2f - PegCellW);
            float innerH = Mathf.Max(1f, box.height - PegPad * 2f - PegCellH);

            for (int i = 0; i < layout.count; i++)
            {
                Vector2 n = layout.PositionOf(i);
                float px = box.x + PegPad + n.x * innerW;
                float py = box.y + PegPad + (1f - n.y) * innerH; // normalized y is up; screen y is down
                var cell = new Rect(px, py, PegCellW, PegCellH);

                bool was = selected.Contains(i);
                bool now = GUI.Toggle(cell, was, i.ToString(), EditorStyles.miniButton);
                if (now != was)
                {
                    if (now) selected.Add(i); else selected.Remove(i);
                    WriteSelected(indices, selected);
                }
            }
            y += PegBoxHeight + Spacing;

            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, line), $"{selected.Count} peg(s) selected", EditorStyles.miniLabel);
            y += line + Spacing;

            return y;
        }

        private static HashSet<int> ReadSelected(SerializedProperty indices)
        {
            var set = new HashSet<int>();
            if (indices == null) return set;
            for (int i = 0; i < indices.arraySize; i++)
            {
                set.Add(indices.GetArrayElementAtIndex(i).intValue);
            }
            return set;
        }

        private static void WriteSelected(SerializedProperty indices, HashSet<int> set)
        {
            if (indices == null) return;
            var sorted = new List<int>(set);
            sorted.Sort();
            indices.arraySize = sorted.Count;
            for (int i = 0; i < sorted.Count; i++) indices.GetArrayElementAtIndex(i).intValue = sorted[i];
        }

        private static PegLayout LoadLayout()
        {
            return AssetDatabase.LoadAssetAtPath<PegLayout>(PegLayoutPath);
        }
    }
}
