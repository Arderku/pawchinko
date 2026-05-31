using UnityEditor;
using UnityEngine;
using Pawchinko;

namespace PawchinkoEditor
{
    /// <summary>
    /// One-shot editor tool that builds the species-level Pom visual prefabs from their FBX
    /// + material assets and wires the resulting prefab into the matching <see cref="PomData"/>
    /// asset's <c>portraitPrefab</c> field.
    ///
    /// Convention: visual prefabs live under <c>Assets/VisualAssets/Prefabs/Poms/</c> and are
    /// named after the species' <c>PomData.Id</c> (e.g. <c>Pom_001.prefab</c>). Each prefab is a
    /// <b>Prefab Variant</b> of the species' FBX — the variant root <i>is</i> the FBX root, with
    /// these overrides baked in:
    /// <list type="bullet">
    ///   <item><description><see cref="Animator"/> added to the root so dropping in an AnimatorController later is asset-only.</description></item>
    ///   <item><description>The species material applied to every renderer.</description></item>
    ///   <item><description>The whole subtree forced onto the <see cref="PomPortraitSlot.PortraitLayerName"/> layer so portrait cameras can isolate it.</description></item>
    /// </list>
    /// No empty wrapper GameObject above the FBX — the variant is the FBX, period. Idempotent:
    /// rerunning overwrites the existing prefab in place, which preserves the GUID and keeps
    /// every PomData reference stable.
    /// </summary>
    public static class BuildPomVisuals
    {
        private const string Pom1FbxPath = "Assets/VisualAssets/Models/Poms/Pom_1/FBX_Pom_01.fbx";
        private const string Pom1MaterialPath = "Assets/VisualAssets/Models/Poms/Pom_1/M_Paw_Base_01.mat";
        private const string Pom1PrefabPath = "Assets/VisualAssets/Prefabs/Poms/Pom_001.prefab";
        private const string GlitchPugDataPath = "Assets/Data/Pom/Creatures/Pom_GlitchPug.asset";

        [MenuItem("Pawchinko/Build Pom Visuals (Pom_1 -> GlitchPug)")]
        public static void Build()
        {
            var prefab = BuildPom1Prefab();
            if (prefab == null) return;

            AssignToGlitchPug(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildPomVisuals] Built '{Pom1PrefabPath}' as a Variant of '{Pom1FbxPath}' and assigned it to Pom_GlitchPug.portraitPrefab.");
        }

        private static GameObject BuildPom1Prefab()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Pom1FbxPath);
            if (fbx == null)
            {
                Debug.LogError($"[BuildPomVisuals] FBX not found at {Pom1FbxPath}.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(Pom1MaterialPath);
            if (material == null)
            {
                Debug.LogWarning($"[BuildPomVisuals] Material not found at {Pom1MaterialPath}; the FBX's default materials will be used.");
            }

            int portraitLayer = LayerMask.NameToLayer(PomPortraitSlot.PortraitLayerName);
            if (portraitLayer < 0)
            {
                Debug.LogError($"[BuildPomVisuals] Layer '{PomPortraitSlot.PortraitLayerName}' is missing. Add it via Project Settings > Tags and Layers first.");
                return null;
            }

            // Instantiate the FBX as a prefab instance, mutate it, then save as a Prefab Variant
            // connected to the FBX. The variant root IS the FBX root - no wrapper.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (instance == null)
            {
                Debug.LogError($"[BuildPomVisuals] Failed to instantiate FBX at {Pom1FbxPath}.");
                return null;
            }

            try
            {
                if (instance.GetComponent<Animator>() == null) instance.AddComponent<Animator>();
                if (material != null) ApplyMaterialToAllRenderers(instance, material);
                SetLayerRecursively(instance, portraitLayer);

                var variant = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, Pom1PrefabPath, InteractionMode.AutomatedAction, out bool success);
                if (!success || variant == null)
                {
                    Debug.LogError($"[BuildPomVisuals] Failed to save prefab variant at {Pom1PrefabPath}.");
                    return null;
                }
                return variant;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void ApplyMaterialToAllRenderers(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                renderer.sharedMaterials = mats;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            var t = root.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        private static void AssignToGlitchPug(GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<PomData>(GlitchPugDataPath);
            if (data == null)
            {
                Debug.LogError($"[BuildPomVisuals] PomData not found at {GlitchPugDataPath}.");
                return;
            }

            var so = new SerializedObject(data);
            var prop = so.FindProperty("portraitPrefab");
            if (prop == null)
            {
                Debug.LogError("[BuildPomVisuals] portraitPrefab field not found on PomData - did the script recompile?");
                return;
            }
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
    }
}
