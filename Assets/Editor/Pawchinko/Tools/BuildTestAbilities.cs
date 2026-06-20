using System;
using System.IO;
using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace Pawchinko.Editor.Tools
{
    /// <summary>
    /// One-shot editor utility that authors a small set of sample abilities covering every
    /// <see cref="AbilityEffectKind"/> (including both peg actions), migrates the two pre-existing
    /// CHAOS ability assets to the new effect-list schema, and assigns a pair of abilities to each
    /// test-roster Pom so the in-battle ability picker has real content end to end. Run it AFTER
    /// "Pawchinko/Build Test Pom Roster (5v5)" so the Pom assets exist. The peg abilities reference
    /// peg indices, so also run "Pawchinko/Build Board Pegs" for them to resolve to real pegs.
    /// </summary>
    public static class BuildTestAbilities
    {
        private const string AbilitiesFolder = "Assets/Data/Pom/Abilities";
        private const string CreaturesFolder = "Assets/Data/Pom/Creatures";

        private const string Chaos001Path = AbilitiesFolder + "/PomAbility_CHAOS_001.asset";
        private const string Chaos002Path = AbilitiesFolder + "/PomAbility_CHAOS_002.asset";
        private const string PowerSurgePath = AbilitiesFolder + "/PomAbility_PowerSurge.asset";
        private const string ExtraDropPath = AbilitiesFolder + "/PomAbility_ExtraDrop.asset";
        private const string CenterFunnelPath = AbilitiesFolder + "/PomAbility_CenterFunnel.asset";
        private const string EnergyHarvestPath = AbilitiesFolder + "/PomAbility_EnergyHarvest.asset";
        private const string ChargedPegsPath = AbilitiesFolder + "/PomAbility_ChargedPegs.asset";
        private const string PhantomPegsPath = AbilitiesFolder + "/PomAbility_PhantomPegs.asset";

        [MenuItem("Pawchinko/Build Test Abilities")]
        public static void Build()
        {
            if (!Directory.Exists(AbilitiesFolder)) Directory.CreateDirectory(AbilitiesFolder);

            // Glitch Field (CHAOS_001): doubles the center bucket's value this round (self).
            var glitchField = Author(Chaos001Path, "CHAOS_001", "Glitch Field",
                "Doubles the center bucket's value for your board this round.",
                apCost: 1, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.BucketModifier, AbilityValueMode.Multiply, 2f, 1f,
                        any: true, PomType.Chaos, new[] { 3 }, typeExclusive: false, forceSpawn: false);
                });

            // Static Charge (CHAOS_002): halves every enemy ball's power this round (enemy debuff).
            var staticCharge = Author(Chaos002Path, "CHAOS_002", "Static Charge",
                "Weakens every enemy ball's power by half this round.",
                apCost: 2, PomAbilityBoardTarget.Enemy, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.BallPower, AbilityValueMode.Multiply, 0.5f, 1f,
                        any: true, PomType.Chaos, Array.Empty<int>(), typeExclusive: false, forceSpawn: false);
                });

            // Power Surge: +50% power to all of your balls this round.
            var powerSurge = Author(PowerSurgePath, "ABIL_POWER_SURGE", "Power Surge",
                "Boosts all of your balls' power by 50% this round.",
                apCost: 2, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.BallPower, AbilityValueMode.Multiply, 1.5f, 1f,
                        any: true, PomType.Chaos, Array.Empty<int>(), typeExclusive: false, forceSpawn: false);
                });

            // Extra Drop: +3 balls this round.
            var extraDrop = Author(ExtraDropPath, "ABIL_EXTRA_DROP", "Extra Drop",
                "Drops 3 extra balls this round.",
                apCost: 2, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.BallCount, AbilityValueMode.Add, 3f, 1f,
                        any: true, PomType.Chaos, Array.Empty<int>(), typeExclusive: false, forceSpawn: false);
                });

            // Center Funnel: force all balls to spawn from the two center zones (of 6: zones 2 & 3).
            var centerFunnel = Author(CenterFunnelPath, "ABIL_CENTER_FUNNEL", "Center Funnel",
                "Forces your balls to spawn from the center zones this round.",
                apCost: 1, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.SpawnSlotBias, AbilityValueMode.Multiply, 1f, 1f,
                        any: true, PomType.Chaos, new[] { 2, 3 }, typeExclusive: false, forceSpawn: true);
                });

            // Energy Harvest: +25% energy from this round's score.
            var energyHarvest = Author(EnergyHarvestPath, "ABIL_ENERGY_HARVEST", "Energy Harvest",
                "Increases the energy gained from this round's score by 25%.",
                apCost: 2, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.EnergyPercent, AbilityValueMode.Add, 0.25f, 1f,
                        any: true, PomType.Chaos, Array.Empty<int>(), typeExclusive: false, forceSpawn: false);
                });

            // Charged Pegs: the first ten pegs grow a ball's power x1.25 on hit (50% chance per hit).
            var chargedPegs = Author(ChargedPegsPath, "ABIL_CHARGED_PEGS", "Charged Pegs",
                "Charges your top pegs: each hit has a 50% chance to grow the ball's power by 25% this round.",
                apCost: 2, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.PegEffect, AbilityValueMode.Multiply, 1.25f, 0.5f,
                        any: true, PomType.Chaos, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                        typeExclusive: false, forceSpawn: false, pegAction: PegAction.PowerOnHit);
                });

            // Phantom Pegs: hide five pegs this round so your balls fall straight through them.
            var phantomPegs = Author(PhantomPegsPath, "ABIL_PHANTOM_PEGS", "Phantom Pegs",
                "Removes five of your pegs for the round, opening a straighter drop lane.",
                apCost: 2, PomAbilityBoardTarget.Self, effects =>
                {
                    var e = AddEffect(effects);
                    SetEffect(e, AbilityEffectKind.PegEffect, AbilityValueMode.Multiply, 1f, 1f,
                        any: true, PomType.Chaos, new[] { 10, 11, 12, 13, 14 },
                        typeExclusive: false, forceSpawn: false, pegAction: PegAction.Hide);
                });

            AssetDatabase.SaveAssets();

            // Assign two learnable abilities to each test-roster Pom (requiredType = any, so every
            // species can learn them). PomFactory auto-fills the in-battle picker from this pool.
            AssignAbilities(CreaturesFolder + "/Pom_GlitchPug.asset", glitchField, staticCharge);
            AssignAbilities(CreaturesFolder + "/Pom_ZenSloth.asset", powerSurge, extraDrop);
            AssignAbilities(CreaturesFolder + "/Pom_CoinHoarder.asset", centerFunnel, energyHarvest);
            AssignAbilities(CreaturesFolder + "/Pom_MirageFox.asset", chargedPegs, phantomPegs);
            AssignAbilities(CreaturesFolder + "/Pom_CloverCat.asset", extraDrop, energyHarvest);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildTestAbilities] Authored 8 sample abilities and assigned them across the test roster.");
        }

        private static PomAbilityData Author(string path, string id, string displayName, string description,
            int apCost, PomAbilityBoardTarget boardTarget, Action<SerializedProperty> writeEffects)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PomAbilityData>(path);
            bool isNew = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PomAbilityData>();
                AssetDatabase.CreateAsset(asset, path);
                isNew = true;
            }

            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;

            var req = so.FindProperty("requiredType");
            req.FindPropertyRelative("any").boolValue = true;
            req.FindPropertyRelative("type").enumValueIndex = 0;

            so.FindProperty("apCost").intValue = apCost;
            so.FindProperty("boardTarget").enumValueIndex = (int)boardTarget;

            var effects = so.FindProperty("effects");
            effects.arraySize = 0;
            writeEffects(effects);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[BuildTestAbilities] {(isNew ? "Created" : "Updated")} '{displayName}' at {path}");
            return asset;
        }

        private static SerializedProperty AddEffect(SerializedProperty effects)
        {
            int i = effects.arraySize;
            effects.arraySize = i + 1;
            return effects.GetArrayElementAtIndex(i);
        }

        private static void SetEffect(SerializedProperty element, AbilityEffectKind kind, AbilityValueMode mode,
            float amount, float chance, bool any, PomType type, int[] targetIndices, bool typeExclusive, bool forceSpawn,
            PegAction pegAction = PegAction.PowerOnHit)
        {
            element.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            element.FindPropertyRelative("mode").enumValueIndex = (int)mode;
            element.FindPropertyRelative("amount").floatValue = amount;
            element.FindPropertyRelative("chance").floatValue = chance;

            var tf = element.FindPropertyRelative("typeFilter");
            tf.FindPropertyRelative("any").boolValue = any;
            tf.FindPropertyRelative("type").enumValueIndex = (int)type;

            var idx = element.FindPropertyRelative("targetIndices");
            int count = targetIndices != null ? targetIndices.Length : 0;
            idx.arraySize = count;
            for (int i = 0; i < count; i++) idx.GetArrayElementAtIndex(i).intValue = targetIndices[i];

            element.FindPropertyRelative("typeExclusive").boolValue = typeExclusive;
            element.FindPropertyRelative("forceSpawn").boolValue = forceSpawn;
            element.FindPropertyRelative("pegAction").enumValueIndex = (int)pegAction;
        }

        private static void AssignAbilities(string pomPath, params PomAbilityData[] abilities)
        {
            var pom = AssetDatabase.LoadAssetAtPath<PomData>(pomPath);
            if (pom == null)
            {
                Debug.LogWarning($"[BuildTestAbilities] Pom not found at {pomPath}; run 'Pawchinko/Build Test Pom Roster (5v5)' first.");
                return;
            }

            var so = new SerializedObject(pom);
            var list = so.FindProperty("learnableAbilities");
            list.arraySize = abilities.Length;
            for (int i = 0; i < abilities.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pom);

            Debug.Log($"[BuildTestAbilities] Assigned {abilities.Length} abilities to {pom.name}");
        }
    }
}
