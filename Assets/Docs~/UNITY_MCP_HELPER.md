# Unity MCP Helper

> Generic, project-agnostic field guide for AI agents using the `user-unity-mcp` bridge in this Pawchinko Unity project. Living document - **append a new entry every time you hit (and fix) a fresh issue**.

## Read this first

- **What this is**: a running registry of Unity MCP gotchas + their fixes. Read before starting any non-trivial MCP work.
- **When to read**: at the top of every agent session that will use `Unity_RunCommand`, `Unity_Camera_Capture`, or related MCP tools.
- **When to append**: anytime you spend more than ~5 minutes debugging an MCP-specific issue. Add an entry under [Known issues + fixes](#known-issues--fixes) using the [Append-an-entry template](#append-an-entry-template) so the next agent doesn't waste the same time.

`Docs~/` is Unity-ignored per [Docs~/Desgin/AI_AGENT_CODE_GUIDE.md](Desgin/AI_AGENT_CODE_GUIDE.md) §3, so editing this file does not touch the asset database.

---

## Golden CommandScript template

Every `Unity_RunCommand` call must use this exact shape. Most failures come from breaking one of these rules.

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

**Hard rules:**

1. Class **must** be named exactly `CommandScript`. Other names crash the bridge with `NullReferenceException`.
2. **Must** be `internal`. `public` causes "Inconsistent Accessibility" compile errors because the MCP wrapper namespace is internal.
3. Use `result.Log(format, args)` for output. `Debug.Log` does not flow through the response.
4. Use `result.RegisterObjectCreation(obj)` after creating GameObjects/Assets.
5. Use `result.RegisterObjectModification(obj)` **before** mutating serialized properties on existing objects.
6. Use `result.DestroyObject(obj)` instead of `Object.DestroyImmediate` when destroying tracked content.
7. End mutating scripts with `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`. For scene mutations, also `EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);`.
8. The MCP wraps your script inside `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor` - watch for type-name clashes (see [Issue 02](#02---image-type-clashes-with-mcp-wrapper-namespace)).

---

## Tool quick reference

| Tool | Purpose | When to use |
|---|---|---|
| `Unity_RunCommand` | Compile + execute a `CommandScript` in the Editor. The hammer for everything. | Any scene/asset/setting mutation, layer config, prefab build, hierarchy creation, Inspector wiring, runtime inspection. |
| `Unity_GetConsoleLogs` | Reads Editor Console entries (`logTypes: "Error,Warning,Log"`). | Verify clean compile after a `.cs` change; surface stale errors. **Does not surface in-game `Debug.Log` during Play mode** ([Issue 06](#06---unity_getconsolelogs-returns-empty-in-play-mode)). |
| `Unity_Camera_Capture` | Renders an image. With no args -> Scene View. With `cameraInstanceID` -> that camera. | Verify a camera frames the scene correctly, or grab the Game view as a screenshot. Flaky for arbitrary cameras ([Issue 07](#07---unity_camera_capture-with-camerainstanceid-may-fail)). |
| `Unity_SceneView_CaptureMultiAngleSceneView` | 2x2 grid: Iso / Front / Top / Right. Optional `focusObjectIds`. | Best 3D scene structure verification. Use after building hierarchies to visually confirm layout. |
| `Unity_SceneView_Capture2DScene` | Orthographic top-down capture of a world rect. | 2D scenes / tilemaps only. Not relevant to Pawchinko's 3D battle scene. |
| `Unity_AssetGeneration_GenerateAsset`, `Unity_AssetGeneration_GetModels` | AI-generated visual assets (textures, models). | When you need placeholder art assets generated rather than authored manually. |

---

## Known issues + fixes

Numbered in discovery order so cross-references stay stable. Add new entries to the bottom.

### 01 - PhysicsMaterial file extension

- **Symptom**: `CreateAsset() should not be used to create a file of type 'physicsMaterial' - consider using AssetDatabase.ImportAsset() to import an existing 'physicsMaterial' file instead or change the file type to '*.asset'.`
- **Cause**: Unity 6 deprecated the `.physicsMaterial` extension for `AssetDatabase.CreateAsset`. The asset is still `PhysicsMaterial` (note the `s` - see [Issue 11](#11---unity-6-physics-api-renames)).
- **Fix**: save the asset with `.asset` extension.

```csharp
var pm = new PhysicsMaterial("Ball_PhysMat") { bounciness = 0.35f, dynamicFriction = 0.15f };
AssetDatabase.CreateAsset(pm, "Assets/VisualAssets/Physics/Ball_PhysMat.asset");
```

### 02 - `Image` type clashes with MCP wrapper namespace

- **Symptom**: `error CS0118: 'Image' is a namespace but is used like a type`.
- **Cause**: the MCP wraps your `CommandScript` inside `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`, in which `Image` resolves to that subsystem's namespace, not `UnityEngine.UI.Image`. This can hit any short type name.
- **Fix**: alias UI types at the top of the script.

```csharp
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;
// ...
go.AddComponent<UIImage>();
```

- **Notes**: same risk applies to other short-named types - if you see "namespace but used like a type" for `Foo`, alias it.

### 03 - `AssetDatabase.DeleteAsset` may fail with "User interactions are not supported"

- **Symptom**: `UNEXPECTED_ERROR: User interactions are not supported for MCP tool calls. Tools requiring user interaction cannot be called via MCP.`
- **Cause**: Unity prompts a confirmation dialog when the asset is loaded/locked, and MCP cannot answer dialogs.
- **Fix**: delete the file (and its `.meta`) via filesystem tools first, then call `AssetDatabase.Refresh()` from a CommandScript.

```csharp
// In a follow-up CommandScript after the file deletion:
AssetDatabase.Refresh();
```

### 04 - `LayerMask.NameToLayer` returns -1 right after writing layers

- **Symptom**: a CommandScript that writes layers to `TagManager.asset` and then immediately reads them with `LayerMask.NameToLayer` gets `-1`.
- **Cause**: layer changes need a refresh/round-trip before the runtime API sees them.
- **Fix**: split layer creation and layer-dependent work into **separate** `Unity_RunCommand` calls. End the layer-write script with `AssetDatabase.SaveAssets() + AssetDatabase.Refresh()`. Re-query layer indices in the next call.

```csharp
// Pass 1: write layers, save, refresh.
tagManager.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();

// Pass 2 (separate CommandScript): now safe to query.
int ballLayer = LayerMask.NameToLayer("Ball");
```

### 05 - "Unity not detected (no fresh discovery files found)"

- **Symptom**: `Unity_RunCommand` returns `{"success": false, "error": "Unity not detected (no fresh discovery files found)"}` immediately.
- **Cause**: the previous batch (usually a `.cs` write or asmdef change) triggered a domain reload, and the bridge is paused while Unity recompiles.
- **Fix**: wait ~5-10 seconds, then retry. Use `AwaitShell` with `block_until_ms: 8000` (no `task_id`) for clean polling. If it still fails after a second retry, ask the user to confirm the Editor is still focused on this project.

### 06 - `Unity_GetConsoleLogs` returns empty in Play mode

- **Symptom**: `Unity_GetConsoleLogs` returns `{"logs": [], "totalCount": 0}` even though you can see logs in the Unity Console window.
- **Cause**: tool limitation - it does not surface runtime `Debug.Log` output from in-game scripts while in Play mode.
- **Fix**: verify state via a follow-up `Unity_RunCommand` that inspects GameObject / component state directly using `result.Log`.

```csharp
var bm = GameObject.Find("BattleManager");
result.Log("BattleManager alive: {0}", bm != null);
result.Log("Active: {0}", bm != null && bm.activeInHierarchy);
```

- **Notes**: also consider asking the user to read the docked Console window if state inspection is insufficient.

### 07 - `Unity_Camera_Capture` with `cameraInstanceID` may fail

- **Symptom**: `Error executing tool: Failed to render scene preview.`
- **Cause**: flaky for non-Scene-View cameras. Causes vary (no render target, camera disabled, render pipeline ordering).
- **Fix**: in order of preference:
  1. Retry once.
  2. Call `Unity_Camera_Capture` with **no** `cameraInstanceID` to capture the Scene View.
  3. Call `Unity_SceneView_CaptureMultiAngleSceneView` with `focusObjectIds: [<obj instance ids>]` for a 4-angle view that frames the requested objects automatically.
  4. As a last resort, manually compute viewport coords with `Camera.WorldToViewportPoint` to assert framing.

### 08 - MCP wrapper throws "Object reference not set..." on heavy reflection scripts

- **Symptom**: `UNEXPECTED_ERROR: Object reference not set to an instance of an object` from a CommandScript that uses reflection extensively (especially during Play mode), even though the script logically should not NRE.
- **Cause**: the MCP response-handling layer occasionally fails on complex reflection results in Play mode (formatting, reference traversal).
- **Fix**:
  1. Split the script into smaller, more focused CommandScripts.
  2. Prefer typed access (`go.GetComponent<T>()`) over `Type.GetType + GetField + GetValue` chains where possible.
  3. Inspect state via plain primitives (bools, strings, ints) instead of complex object references in the log calls.

### 09 - Class name + accessibility rules

- **Symptom**: `NullReferenceException` or `Inconsistent Accessibility` errors when running a CommandScript.
- **Cause**: the MCP looks for an exact `internal class CommandScript` declaration.
- **Fix**: keep this exact line every time, no exceptions:

```csharp
internal class CommandScript : IRunCommand
```

- **Notes**: never rename the class. Never make it `public`. Never add a namespace yourself - the MCP wraps one around your code.

### 10 - Use `result.Log`, not `Debug.Log`, in CommandScripts

- **Symptom**: messages you expected in the agent's response don't show up.
- **Cause**: `Debug.Log` writes to the Editor Console but does not flow back through the `Unity_RunCommand` response.
- **Fix**: use `result.Log("format {0}", arg)`, `result.LogWarning(...)`, `result.LogError(...)`. Reserve `Debug.Log` for log lines that should be visible to the developer in the Editor Console (rare from MCP scripts).

### 11 - Unity 6 physics API renames

- **Symptom**: `error CS0117: 'Rigidbody' does not contain a definition for 'drag'` (or `angularDrag`, `PhysicMaterial`, `PhysicMaterialCombine`).
- **Cause**: Unity 6 renamed several physics APIs.
- **Fix**: use the new names.

| Old (Unity 5/2022) | New (Unity 6) |
|---|---|
| `Rigidbody.drag` | `Rigidbody.linearDamping` |
| `Rigidbody.angularDrag` | `Rigidbody.angularDamping` |
| `PhysicMaterial` | `PhysicsMaterial` |
| `PhysicMaterialCombine` | `PhysicsMaterialCombine` |

### 12 - Two `EventSystem` types coexist

- **Symptom**: confusion / accidental misuse between `UnityEngine.EventSystems.EventSystem` (Unity UI input) and a project-defined `Pawchinko.EventSystem` (the project event bus).
- **Cause**: same short name, two different responsibilities, both commonly present in scenes.
- **Fix**: name the project bus GameObject `GameEventSystem` to disambiguate in the Hierarchy / Inspector. Keep both - they serve different purposes (UI input vs project events). When writing `using` directives, fully-qualify either side if both are used.

### 13 - Asset folders must exist before `CreateAsset`

- **Symptom**: silent failure or confusing error when calling `AssetDatabase.CreateAsset` with a path whose parent folder doesn't exist.
- **Cause**: `CreateAsset` does not auto-create folders.
- **Fix**: ensure folders exist first.

```csharp
private static void EnsureFolder(string parent, string name)
{
    string full = parent + "/" + name;
    if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, name);
}

EnsureFolder("Assets", "VisualAssets");
EnsureFolder("Assets/VisualAssets", "Materials");
EnsureFolder("Assets/VisualAssets/Materials", "Board");
// then CreateAsset...
```

### 14 - Wiring `[SerializeField] private` refs from a CommandScript

- **Symptom**: Inspector references stay null after a CommandScript "assigns" them; or `FindProperty` returns null.
- **Cause**: `SerializedObject` walks fields by their **storage** name (the lowercase `[SerializeField]` field), not the public `PascalCase` property.
- **Fix**:

```csharp
var so = new SerializedObject(targetComponent);
var prop = so.FindProperty("eventSystem"); // matches: [SerializeField] private EventSystem eventSystem;
if (prop == null) { result.LogError("field not found"); return; }
prop.objectReferenceValue = eventSystemComponent;
so.ApplyModifiedPropertiesWithoutUndo();
```

- **Notes**: never reference the `PascalCase` property name (`EventSystem`) for `FindProperty` - it won't resolve. Only the field-storage name works.

---

## Pre-flight checklist

Run these mental checks before kicking off a session of MCP work:

1. **Unity Editor open and focused** on the Pawchinko project. The bridge only works while Unity is the foreground app for this project.
2. **MCP bridge connected.** Verify with a tiny CommandScript that just calls `result.Log("ok")`. If you get "Unity not detected" twice in a row, ask the user to confirm.
3. **Active scene is the one you intend to mutate.** Most scene-build CommandScripts open the scene explicitly, but having it open avoids surprise prompts about saving an unrelated scene.
4. **Editor in Edit mode, not Play mode.** Layer changes, scene save, prefab creation all require Edit mode. Exit Play mode before mutating.
5. **Console docked** so the user can see live log output (you cannot always rely on `Unity_GetConsoleLogs` - see [Issue 06](#06---unity_getconsolelogs-returns-empty-in-play-mode)).
6. **Save unsaved work** before letting big mutating CommandScripts run, since scene-build passes overwrite scene contents.
7. **TextMeshPro Essentials**: Unity 6 + ugui 2.0 auto-imports TMP on first use, but if a "Import TMP Essentials?" dialog appears, the user must click it - the agent cannot dismiss modals.

---

## Idempotency patterns

Design every mutating CommandScript so you can safely run it twice. Three reusable patterns:

### Cleanup-then-build for scene roots

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

### Folder helper

```csharp
private static void EnsureFolder(string parent, string name)
{
    string full = parent + "/" + name;
    if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, name);
}
```

### Asset helper (load-or-create)

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

### Always end with save + refresh

```csharp
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

---

## Append-an-entry template

Copy this block, fill it in, and add it under [Known issues + fixes](#known-issues--fixes) with the next sequential number.

```markdown
### NN - <short title>

- **Symptom**: <what failed; copy the exact error message if helpful>
- **Cause**: <root cause>
- **Fix**: <minimal repro of the workaround>
- **Notes**: <project-specific gotcha, optional>
```

Code snippets that illustrate the fix are highly welcome - the next agent can copy-paste them.
