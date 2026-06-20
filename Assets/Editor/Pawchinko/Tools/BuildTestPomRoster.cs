using System.Collections.Generic;
using System.IO;
using Pawchinko;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pawchinko.Editor.Tools
{
    /// <summary>
    /// One-shot editor utility that seeds 4 placeholder Pom species (POM_002..POM_005), one per
    /// remaining <see cref="PomType"/>, and fills the Battle scene's <see cref="BattleManager"/>
    /// rosters with a full 5-Pom team on each side so the battle UI can be exercised end-to-end.
    /// All new species reuse <c>Pom_001.prefab</c> for their 3D portrait until per-species art
    /// lands.
    /// </summary>
    public static class BuildTestPomRoster
    {
        private const string CreaturesFolder = "Assets/Data/Pom/Creatures";
        private const string GlitchPugAssetPath = CreaturesFolder + "/Pom_GlitchPug.asset";
        private const string PortraitPrefabPath = "Assets/VisualAssets/Prefabs/Poms/Pom_001.prefab";
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";

        private struct PomSeed
        {
            public string id;
            public string displayName;
            public string description;
            public PomType primaryType;
            public PomRarity rarity;
            public int baseEnergy;
            public PomBaseStatsData baseStats;
            public BallGrowthStyle ballGrowthStyle;
        }

        private struct PomBaseStatsData
        {
            public float power, weight, luck, control;
        }

        private static readonly PomSeed[] SeedSpecies =
        {
            new PomSeed
            {
                id = "POM_002",
                displayName = "Zen Sloth",
                description = "A serene drifter that smooths peg paths into tranquil arcs.",
                primaryType = PomType.Calm,
                rarity = PomRarity.Common,
                baseEnergy = 20,
                baseStats = new PomBaseStatsData { power = 1.6f, weight = 4.0f, luck = 3f, control = 4f },
                ballGrowthStyle = BallGrowthStyle.SteadyPaws,
            },
            new PomSeed
            {
                id = "POM_003",
                displayName = "Coin Hoarder",
                description = "A pocket-stuffed pack rat that converts board luck into shiny score.",
                primaryType = PomType.Greedy,
                rarity = PomRarity.Common,
                baseEnergy = 28,
                baseStats = new PomBaseStatsData { power = 2.4f, weight = 2.5f, luck = 4f, control = 2f },
                ballGrowthStyle = BallGrowthStyle.GrowingRush,
            },
            new PomSeed
            {
                id = "POM_004",
                displayName = "Mirage Fox",
                description = "A slippery trickster that bends ball trajectories where they should not bend.",
                primaryType = PomType.Trick,
                rarity = PomRarity.Uncommon,
                baseEnergy = 24,
                baseStats = new PomBaseStatsData { power = 1.9f, weight = 2.0f, luck = 6f, control = 3f },
                ballGrowthStyle = BallGrowthStyle.PowerSpikes,
            },
            new PomSeed
            {
                id = "POM_005",
                displayName = "Clover Cat",
                description = "Pure feline serendipity. Bounces in your favour just often enough to feel earned.",
                primaryType = PomType.Lucky,
                rarity = PomRarity.Uncommon,
                baseEnergy = 22,
                baseStats = new PomBaseStatsData { power = 1.8f, weight = 2.2f, luck = 8f, control = 2.5f },
                ballGrowthStyle = BallGrowthStyle.LuckyChaos,
            },
        };

        // Growth style assigned to Glitch Pug (the pre-existing base species) so all five styles
        // are represented across the test roster.
        private const BallGrowthStyle GlitchPugGrowthStyle = BallGrowthStyle.LateBloomer;

        [MenuItem("Pawchinko/Build Test Pom Roster (5v5)")]
        public static void Build()
        {
            var portraitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortraitPrefabPath);
            if (portraitPrefab == null)
            {
                EditorUtility.DisplayDialog("Build Test Pom Roster",
                    $"Could not load portrait prefab at:\n{PortraitPrefabPath}\n\nRun \"Pawchinko/Build Pom Visuals\" first.",
                    "OK");
                return;
            }

            if (!Directory.Exists(CreaturesFolder))
            {
                Directory.CreateDirectory(CreaturesFolder);
            }

            var seededPoms = new List<PomData>(SeedSpecies.Length);
            foreach (var seed in SeedSpecies)
            {
                seededPoms.Add(CreateOrUpdatePom(seed, portraitPrefab));
            }
            AssetDatabase.SaveAssets();

            var glitchPug = AssetDatabase.LoadAssetAtPath<PomData>(GlitchPugAssetPath);
            if (glitchPug == null)
            {
                EditorUtility.DisplayDialog("Build Test Pom Roster",
                    $"Could not load Glitch Pug at:\n{GlitchPugAssetPath}",
                    "OK");
                return;
            }

            // Give the pre-existing base species a growth style too, so the roster covers all five.
            var pugSo = new SerializedObject(glitchPug);
            WriteBallGrowth(pugSo, GlitchPugGrowthStyle);
            pugSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glitchPug);
            AssetDatabase.SaveAssets();

            var fullRoster = new List<PomData> { glitchPug };
            fullRoster.AddRange(seededPoms);

            ApplyToBattleScene(fullRoster);

            AssetDatabase.Refresh();
            Debug.Log($"[BuildTestPomRoster] Seeded {seededPoms.Count} test Poms and filled Battle scene with 5v5 rosters.");
        }

        private static PomData CreateOrUpdatePom(PomSeed seed, GameObject portraitPrefab)
        {
            var path = $"{CreaturesFolder}/Pom_{seed.displayName.Replace(" ", string.Empty)}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PomData>(path);
            bool isNew = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PomData>();
                AssetDatabase.CreateAsset(asset, path);
                isNew = true;
            }

            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = seed.id;
            so.FindProperty("displayName").stringValue = seed.displayName;
            so.FindProperty("description").stringValue = seed.description;
            so.FindProperty("rarity").enumValueIndex = (int)seed.rarity;
            so.FindProperty("primaryType").enumValueIndex = (int)seed.primaryType;
            so.FindProperty("hasSecondaryType").boolValue = false;
            so.FindProperty("secondaryType").enumValueIndex = 0;
            so.FindProperty("maxLevel").intValue = 50;
            so.FindProperty("baseEnergy").intValue = seed.baseEnergy;

            var statsProp = so.FindProperty("baseStats");
            statsProp.FindPropertyRelative("power").floatValue = seed.baseStats.power;
            statsProp.FindPropertyRelative("weight").floatValue = seed.baseStats.weight;
            statsProp.FindPropertyRelative("luck").floatValue = seed.baseStats.luck;
            statsProp.FindPropertyRelative("control").floatValue = seed.baseStats.control;

            WriteBallGrowth(so, seed.ballGrowthStyle);

            so.FindProperty("learnableAbilities").arraySize = 0;
            so.FindProperty("portraitPrefab").objectReferenceValue = portraitPrefab;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log($"[BuildTestPomRoster] {(isNew ? "Created" : "Updated")} {seed.displayName} at {path}");
            return asset;
        }

        private static void WriteBallGrowth(SerializedObject so, BallGrowthStyle style)
        {
            so.FindProperty("ballGrowthStyle").enumValueIndex = (int)style;
        }

        private static void ApplyToBattleScene(List<PomData> roster)
        {
            // Avoid clobbering unsaved scene work in progress.
            var openScene = SceneManager.GetActiveScene();
            bool reopened = false;
            Scene battleScene;
            if (openScene.path == BattleScenePath)
            {
                battleScene = openScene;
            }
            else
            {
                if (openScene.isDirty)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.LogWarning("[BuildTestPomRoster] User cancelled scene save; aborting roster apply.");
                        return;
                    }
                }
                battleScene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
                reopened = true;
            }

            BattleManager battleManager = null;
            foreach (var root in battleScene.GetRootGameObjects())
            {
                battleManager = root.GetComponentInChildren<BattleManager>(includeInactive: true);
                if (battleManager != null) break;
            }

            if (battleManager == null)
            {
                Debug.LogError($"[BuildTestPomRoster] No BattleManager found in {BattleScenePath}.");
                return;
            }

            var so = new SerializedObject(battleManager);
            ApplyRoster(so.FindProperty("playerPoms"), roster);
            ApplyRoster(so.FindProperty("enemyPoms"), roster);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(battleManager);
            EditorSceneManager.MarkSceneDirty(battleScene);
            EditorSceneManager.SaveScene(battleScene);

            if (reopened)
            {
                Debug.Log($"[BuildTestPomRoster] Updated and saved {BattleScenePath}.");
            }
        }

        private static void ApplyRoster(SerializedProperty listProp, List<PomData> roster)
        {
            listProp.arraySize = roster.Count;
            for (int i = 0; i < roster.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = roster[i];
            }
        }
    }
}
