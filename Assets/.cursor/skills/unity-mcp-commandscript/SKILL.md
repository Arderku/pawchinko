---
name: unity-mcp-commandscript
description: Rules and template for writing a Unity_RunCommand CommandScript in this Unity 6 project. Use whenever about to call Unity_RunCommand, write `internal class CommandScript : IRunCommand`, or mutate scenes/assets/settings via the user-unity-mcp bridge.
---

# Unity MCP CommandScript - Golden Template

Every `Unity_RunCommand` call must use this exact shape. Most failures come from breaking one of the rules below.

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        try
        {
            // 1. Your logic here.
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // 2. Register changes for Undo/Redo + tracking.
            result.RegisterObjectCreation(cube);

            // 3. Log via result, NEVER Debug.Log (Debug.Log is invisible to the agent).
            result.Log("Created {0}", cube);
        }
        catch (System.Exception ex)
        {
            result.LogError("Unhandled: " + ex);
        }
    }
}
```

## The 8 hard rules

1. Class **must** be named exactly `CommandScript`. Other names crash the bridge with `NullReferenceException`.
2. **Must** be `internal`. `public` causes "Inconsistent Accessibility" compile errors because the MCP wrapper namespace is internal.
3. Use `result.Log(format, args)`, `result.LogWarning(...)`, `result.LogError(...)` for output. `Debug.Log` does NOT flow through the response.
4. Use `result.RegisterObjectCreation(obj)` after creating GameObjects/Assets.
5. Use `result.RegisterObjectModification(obj)` **before** mutating serialized properties on existing objects.
6. Use `result.DestroyObject(obj)` instead of `Object.DestroyImmediate` when destroying tracked content.
7. End mutating scripts with `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`. For scene mutations, also `EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);`.
8. The MCP wraps your script inside `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor` - watch for type-name clashes (e.g. `UnityEngine.UI.Image` collides; alias as `using UIImage = UnityEngine.UI.Image;`).

## Where to look next

- For the full per-rule rationale and the canonical version of this template, see [Docs~/UNITY_MCP_HELPER.md - Golden CommandScript template](../../../Docs~/UNITY_MCP_HELPER.md#golden-commandscript-template).
- If a CommandScript fails despite following these rules, consult the troubleshoot skill or [Docs~/UNITY_MCP_HELPER.md - Known issues + fixes](../../../Docs~/UNITY_MCP_HELPER.md#known-issues--fixes).
- For idempotent scene-build patterns (cleanup-then-build, EnsureFolder, LoadOrCreate, save+refresh footer) see the `unity-mcp-scene-build` skill.
