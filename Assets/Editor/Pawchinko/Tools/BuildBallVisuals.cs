using System.Collections.Generic;
using Pawchinko;
using UnityEditor;
using UnityEngine;

namespace PawchinkoEditor
{
    /// <summary>
    /// One-shot, idempotent editor tool that builds the per-<see cref="PomType"/> ball assets and
    /// wires them into a single shared <see cref="BallLibrary"/>:
    /// <list type="bullet">
    ///   <item><description>A URP/Lit <b>visual material</b> per type (distinct colour).</description></item>
    ///   <item><description>A <b>PhysicsMaterial</b> per type (per-type bounciness / friction - e.g. Calm is far less bouncy than Chaos).</description></item>
    ///   <item><description>A <b>Prefab Variant</b> of the base <c>Ball.prefab</c> per type, with the type material on the ball mesh and the type PhysicsMaterial on the SphereCollider. Being a variant, every type ball inherits future edits to the base ball (Rigidbody, script, label).</description></item>
    ///   <item><description>The <c>BallLibrary.asset</c> mapping each type to its variant, with a fallback.</description></item>
    /// </list>
    /// Player and enemy share these balls - the look/feel is driven by ball type, not side.
    ///
    /// Re-running overwrites assets in place (paths are stable), so prefab GUIDs and every
    /// reference to them stay valid. Tune the colours / bounciness in <see cref="Configs"/>.
    /// </summary>
    public static class BuildBallVisuals
    {
        private const string BaseBallPrefabPath = "Assets/VisualAssets/Prefabs/Battle/Ball.prefab";
        private const string MaterialsFolder = "Assets/VisualAssets/Materials/Ball/Types";
        private const string PhysicsFolder = "Assets/VisualAssets/Physics";
        private const string VariantsFolder = "Assets/VisualAssets/Prefabs/Battle/BallTypes";
        private const string LibraryFolder = "Assets/Data/Ball";
        private const string LibraryPath = LibraryFolder + "/BallLibrary.asset";

        private struct TypeConfig
        {
            public PomType type;
            public Color color;
            public float bounciness;
            public float dynamicFriction;
            public float staticFriction;
        }

        // Per-type look + feel. These are sensible starting values - tweak freely.
        private static readonly TypeConfig[] Configs =
        {
            new TypeConfig { type = PomType.Chaos,  color = new Color(0.85f, 0.15f, 0.85f), bounciness = 0.55f, dynamicFriction = 0.10f, staticFriction = 0.10f }, // erratic, bouncy
            new TypeConfig { type = PomType.Calm,   color = new Color(0.30f, 0.70f, 0.95f), bounciness = 0.10f, dynamicFriction = 0.30f, staticFriction = 0.30f }, // dead, sticky
            new TypeConfig { type = PomType.Greedy, color = new Color(1.00f, 0.82f, 0.10f), bounciness = 0.30f, dynamicFriction = 0.18f, staticFriction = 0.18f }, // grounded gold
            new TypeConfig { type = PomType.Trick,  color = new Color(0.30f, 0.85f, 0.40f), bounciness = 0.45f, dynamicFriction = 0.08f, staticFriction = 0.08f }, // slippery
            new TypeConfig { type = PomType.Lucky,  color = new Color(1.00f, 0.45f, 0.70f), bounciness = 0.40f, dynamicFriction = 0.15f, staticFriction = 0.15f },
            new TypeConfig { type = PomType.Wild,   color = new Color(1.00f, 0.50f, 0.10f), bounciness = 0.50f, dynamicFriction = 0.12f, staticFriction = 0.12f },
        };

        [MenuItem("Pawchinko/Build Ball Visuals (per type)")]
        public static void Build()
        {
            var baseBall = AssetDatabase.LoadAssetAtPath<GameObject>(BaseBallPrefabPath);
            if (baseBall == null)
            {
                Debug.LogError($"[BuildBallVisuals] Base ball prefab not found at {BaseBallPrefabPath}.");
                return;
            }

            EnsureFolder("Assets/VisualAssets/Materials", "Ball");
            EnsureFolder("Assets/VisualAssets/Materials/Ball", "Types");
            EnsureFolder("Assets/VisualAssets/Prefabs/Battle", "BallTypes");
            EnsureFolder("Assets/Data", "Ball");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogError("[BuildBallVisuals] URP/Lit shader not found. Is URP installed?");
                return;
            }

            var variantsByType = new Dictionary<PomType, Ball>();

            foreach (var cfg in Configs)
            {
                string name = cfg.type.ToString();

                Material mat = BuildMaterial(cfg, litShader, $"{MaterialsFolder}/Ball_{name}_Mat.mat");
                PhysicsMaterial phys = BuildPhysicsMaterial(cfg, $"{PhysicsFolder}/Ball_{name}_PhysMat.asset");
                Ball variant = BuildVariant(baseBall, cfg.type, mat, phys, $"{VariantsFolder}/Ball_{name}.prefab");

                if (variant != null) variantsByType[cfg.type] = variant;
            }

            BuildLibrary(variantsByType);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildBallVisuals] Built {Configs.Length} ball types + BallLibrary at {LibraryPath}. Wire this library onto each BallSpawner.ballLibrary.");
        }

        private static Material BuildMaterial(TypeConfig cfg, Shader shader, string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", cfg.color);
            mat.SetColor("_Color", cfg.color);
            mat.SetFloat("_Smoothness", 0.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static PhysicsMaterial BuildPhysicsMaterial(TypeConfig cfg, string path)
        {
            var phys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (phys == null)
            {
                phys = new PhysicsMaterial();
                AssetDatabase.CreateAsset(phys, path);
            }
            phys.name = $"Ball_{cfg.type}_PhysMat";
            phys.bounciness = cfg.bounciness;
            phys.dynamicFriction = cfg.dynamicFriction;
            phys.staticFriction = cfg.staticFriction;
            phys.frictionCombine = PhysicsMaterialCombine.Average;
            phys.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(phys);
            return phys;
        }

        private static Ball BuildVariant(GameObject baseBall, PomType type, Material mat, PhysicsMaterial phys, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseBall);
            if (instance == null)
            {
                Debug.LogError($"[BuildBallVisuals] Failed to instantiate base ball for {type}.");
                return null;
            }

            try
            {
                // Root MeshRenderer is the ball sphere; the child PowerLabel renderer is left alone.
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.sharedMaterial = mat;

                var collider = instance.GetComponent<SphereCollider>();
                if (collider != null) collider.sharedMaterial = phys;

                var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError($"[BuildBallVisuals] Failed to save variant for {type} at {path}.");
                    return null;
                }
                return saved.GetComponent<Ball>();
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void BuildLibrary(Dictionary<PomType, Ball> variantsByType)
        {
            var lib = AssetDatabase.LoadAssetAtPath<BallLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<BallLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var so = new SerializedObject(lib);
            var entries = so.FindProperty("entries");
            entries.ClearArray();

            int index = 0;
            Ball fallback = null;
            foreach (var cfg in Configs)
            {
                if (!variantsByType.TryGetValue(cfg.type, out var ball) || ball == null) continue;
                entries.InsertArrayElementAtIndex(index);
                var element = entries.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("type").enumValueIndex = (int)cfg.type;
                element.FindPropertyRelative("prefab").objectReferenceValue = ball;
                fallback ??= ball;
                index++;
            }

            so.FindProperty("fallbackPrefab").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lib);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
