---
name: unity-mcp-troubleshoot
description: Diagnose Unity MCP failures by symptom. Use when Unity_RunCommand, Unity_Camera_Capture, or Unity_GetConsoleLogs returns an error or empty output - including NullReferenceException, Inconsistent Accessibility, "Unity not detected", "User interactions are not supported", "Failed to render scene preview", "namespace but is used like a type", or empty console logs in Play mode.
---

# Unity MCP Troubleshoot - Symptom Lookup

When an MCP tool fails or returns unexpected output, find the symptom in the table below and apply the matching fix from `Docs~/UNITY_MCP_HELPER.md`. Each row links to the canonical entry with full code snippets.

| # | Symptom (substring of the error or behavior) | Cause (one line) | Fix |
|---|---|---|---|
| 01 | `CreateAsset() should not be used to create a file of type 'physicsMaterial'` | Unity 6 deprecated the `.physicsMaterial` extension | Save as `.asset` ([Issue 01](../../../Docs~/UNITY_MCP_HELPER.md#01---physicsmaterial-file-extension)) |
| 02 | `error CS0118: 'Image' is a namespace but is used like a type` (or any short type name) | MCP wrapper namespace shadows the type | Alias at top of script, e.g. `using UIImage = UnityEngine.UI.Image;` ([Issue 02](../../../Docs~/UNITY_MCP_HELPER.md#02---image-type-clashes-with-mcp-wrapper-namespace)) |
| 03 | `UNEXPECTED_ERROR: User interactions are not supported for MCP tool calls` | Unity wants to open a confirmation dialog (typically `AssetDatabase.DeleteAsset` on a loaded/locked asset) | Delete the file + `.meta` via filesystem tools, then call `AssetDatabase.Refresh()` ([Issue 03](../../../Docs~/UNITY_MCP_HELPER.md#03---assetdatabasedeleteasset-may-fail-with-user-interactions-are-not-supported)) |
| 04 | `LayerMask.NameToLayer` returns `-1` right after writing layers | Layer changes need a refresh round-trip before runtime sees them | Split into two `Unity_RunCommand` calls; end pass 1 with `SaveAssets`+`Refresh`, query layers in pass 2 ([Issue 04](../../../Docs~/UNITY_MCP_HELPER.md#04---layermasknametolayer-returns--1-right-after-writing-layers)) |
| 05 | `{"success": false, "error": "Unity not detected (no fresh discovery files found)"}` | Bridge paused while Unity recompiles after a `.cs`/asmdef change | Wait 5-10s and retry; on second failure ask the user to confirm Editor focus ([Issue 05](../../../Docs~/UNITY_MCP_HELPER.md#05---unity-not-detected-no-fresh-discovery-files-found)) |
| 06 | `Unity_GetConsoleLogs` returns `{"logs": [], "totalCount": 0}` in Play mode | Tool limitation - does not surface runtime `Debug.Log` | Inspect state via a follow-up CommandScript using `result.Log`, or ask the user to read the docked Console ([Issue 06](../../../Docs~/UNITY_MCP_HELPER.md#06---unity_getconsolelogs-returns-empty-in-play-mode)) |
| 07 | `Unity_Camera_Capture` with `cameraInstanceID`: `Failed to render scene preview` | Flaky for non-Scene-View cameras | Retry once -> capture Scene View (no `cameraInstanceID`) -> use `Unity_SceneView_CaptureMultiAngleSceneView` with `focusObjectIds` ([Issue 07](../../../Docs~/UNITY_MCP_HELPER.md#07---unity_camera_capture-with-camerainstanceid-may-fail)) |
| 08 | `UNEXPECTED_ERROR: Object reference not set to an instance of an object` from a heavy reflection script (esp. in Play mode) | MCP response layer fails on complex reflection results | Split the script; prefer typed `GetComponent<T>()` over reflection; log primitives, not object refs ([Issue 08](../../../Docs~/UNITY_MCP_HELPER.md#08---mcp-wrapper-throws-object-reference-not-set-on-heavy-reflection-scripts)) |
| 09 | `NullReferenceException` or `Inconsistent Accessibility` running a CommandScript | Class is renamed, made `public`, or wrapped in an extra namespace | Use exactly `internal class CommandScript : IRunCommand`; never add a namespace ([Issue 09](../../../Docs~/UNITY_MCP_HELPER.md#09---class-name--accessibility-rules)) |
| 10 | Expected log lines never appear in the agent response | Used `Debug.Log` instead of `result.Log` | Switch to `result.Log/LogWarning/LogError` ([Issue 10](../../../Docs~/UNITY_MCP_HELPER.md#10---use-resultlog-not-debuglog-in-commandscripts)) |
| 11 | `error CS0117: 'Rigidbody' does not contain a definition for 'drag'` (or `angularDrag`, `PhysicMaterial`, `PhysicMaterialCombine`) | Unity 6 physics API renames | Use `linearDamping`/`angularDamping`/`PhysicsMaterial`/`PhysicsMaterialCombine` ([Issue 11](../../../Docs~/UNITY_MCP_HELPER.md#11---unity-6-physics-api-renames)) - see also the `unity6-api-migration` skill |
| 12 | Confusion between `UnityEngine.EventSystems.EventSystem` and `Pawchinko.EventSystem` | Same short name, two responsibilities | Name the project bus GameObject `GameEventSystem`; fully qualify `using` directives when both are referenced ([Issue 12](../../../Docs~/UNITY_MCP_HELPER.md#12---two-eventsystem-types-coexist)) |
| 13 | Silent failure or odd error from `AssetDatabase.CreateAsset` with a path whose folder doesn't exist | `CreateAsset` does not auto-create folders | Use the `EnsureFolder` helper before `CreateAsset` ([Issue 13](../../../Docs~/UNITY_MCP_HELPER.md#13---asset-folders-must-exist-before-createasset)) |
| 14 | `[SerializeField] private` Inspector ref stays null after CommandScript "assigns" it; or `FindProperty` returns null | `SerializedObject` walks fields by storage name (lowercase field), not the PascalCase property | Use `so.FindProperty("eventSystem")` for `[SerializeField] private EventSystem eventSystem;` ([Issue 14](../../../Docs~/UNITY_MCP_HELPER.md#14---wiring-serializefield-private-refs-from-a-commandscript)) - see also the `unity-serialized-field-wire` skill |

## Safety rule

If the listed fix does not match the actual symptom (different cause, fix doesn't resolve the error, or the symptom is genuinely new):

- Do **not** improvise repeated retries or guess at a different fix.
- Report what you observed, what you tried, and what's still broken.
- If the user confirms it is genuinely a new gotcha worth recording, suggest they invoke `/unity-mcp-log-issue <short title>` to append a new numbered entry per the [Append-an-entry template](../../../Docs~/UNITY_MCP_HELPER.md#append-an-entry-template). Do not append it yourself.
