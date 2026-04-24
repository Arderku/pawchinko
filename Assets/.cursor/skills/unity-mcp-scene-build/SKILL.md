---
name: unity-mcp-scene-build
description: Idempotency patterns for Unity_RunCommand CommandScripts that build scenes, folders, or assets. Use when writing a CommandScript that creates/rebuilds GameObjects under scene roots, creates folders under Assets/, calls AssetDatabase.CreateAsset, or otherwise mutates the project so the script can be safely re-run.
---

# Unity MCP Scene Build - Idempotency Patterns

Design every mutating CommandScript so it can be safely run twice. Re-running should converge on the same final state, never duplicate or error out.

## 1. Cleanup-then-build for scene roots

Destroy the named root before re-creating it. Safe even on first run because the find returns null.

```csharp
var roots = scene.GetRootGameObjects();
foreach (var go in roots)
{
    if (go == null) continue;
    if (go.name == "Managers" || go.name == "Boards" || go.name == "Canvas")
    {
        UnityEngine.Object.DestroyImmediate(go);
    }
}
// ...now build them fresh.
```

## 2. EnsureFolder helper

`AssetDatabase.CreateAsset` does not auto-create parent folders. Build the path one segment at a time.

```csharp
private static void EnsureFolder(string parent, string name)
{
    string full = parent + "/" + name;
    if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, name);
}

EnsureFolder("Assets", "VisualAssets");
EnsureFolder("Assets/VisualAssets", "Materials");
EnsureFolder("Assets/VisualAssets/Materials", "Board");
```

## 3. LoadOrCreate asset helper

Look up the asset first; only create when missing. Returns the same instance on a re-run.

```csharp
private static T LoadOrCreate<T>(string path, System.Func<T> factory) where T : UnityEngine.Object
{
    var existing = AssetDatabase.LoadAssetAtPath<T>(path);
    if (existing != null) return existing;
    var asset = factory();
    AssetDatabase.CreateAsset(asset, path);
    return asset;
}
```

## 4. Always end with save + refresh

For scene mutations:

```csharp
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

For asset-only mutations omit the scene lines but keep `SaveAssets` + `Refresh`.

## Where to look next

- [Docs~/UNITY_MCP_HELPER.md - Idempotency patterns](../../../Docs~/UNITY_MCP_HELPER.md#idempotency-patterns) for the canonical version.
- [Issue 04](../../../Docs~/UNITY_MCP_HELPER.md#04---layermasknametolayer-returns--1-right-after-writing-layers) explains why layer-writing scripts must `SaveAssets`+`Refresh` then run a second pass before reading layer indices.
- [Issue 13](../../../Docs~/UNITY_MCP_HELPER.md#13---asset-folders-must-exist-before-createasset) for the folder gotcha.
