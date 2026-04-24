---
name: unity-mcp-preflight
description: Run the Unity MCP pre-flight checklist exactly once. Manual only. Verifies the user-unity-mcp bridge is responsive and prints the 7-item Editor-state checklist for the user to confirm before a session of MCP work.
disable-model-invocation: true
argument-hint: (no args)
---

# /unity-mcp-preflight - One-Shot Bridge + Checklist

Run this checklist **once** when the user invokes `/unity-mcp-preflight`. After step 3 below, stop. Do not re-run on subsequent turns; if the user wants another preflight they will type the slash command again.

## Step 1 - Bridge ping (one Unity_RunCommand call)

Call `Unity_RunCommand` exactly once with this minimal CommandScript:

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("preflight-ok");
    }
}
```

Interpret the response:

- **"preflight-ok" appears in the response logs**: bridge is healthy. Continue to step 2.
- **`{"success": false, "error": "Unity not detected (no fresh discovery files found)"}`**: see [Issue 05](../../../Docs~/UNITY_MCP_HELPER.md#05---unity-not-detected-no-fresh-discovery-files-found). Wait ~8 seconds, retry once. If it still fails, report "Unity bridge unreachable; ask the user to confirm Unity Editor is open and focused on this project" and **stop** (skip step 2 and 3).
- **Any other error**: report the raw error verbatim and **stop**.

## Step 2 - Print the 7-item Editor-state checklist

Print the following list verbatim so the user can eyeball the Editor before the next mutating call. Do not run any tools to verify these - they are user-confirmation items:

1. Unity Editor open and focused on the Pawchinko project.
2. MCP bridge connected (just confirmed in step 1).
3. Active scene is the one you intend to mutate.
4. Editor in **Edit** mode, not Play mode (layer changes, scene save, prefab creation all require Edit mode).
5. Console docked so the user can see live log output (you cannot always rely on `Unity_GetConsoleLogs` - see [Issue 06](../../../Docs~/UNITY_MCP_HELPER.md#06---unity_getconsolelogs-returns-empty-in-play-mode)).
6. Save unsaved work before letting big mutating CommandScripts run.
7. TextMeshPro Essentials: if a "Import TMP Essentials?" dialog appears, the user must click it - the agent cannot dismiss modals.

(Canonical version: [Docs~/UNITY_MCP_HELPER.md - Pre-flight checklist](../../../Docs~/UNITY_MCP_HELPER.md#pre-flight-checklist).)

## Step 3 - Stop

After steps 1 and 2 complete, **stop**. Do not proactively re-run the bridge ping, re-print the checklist, or treat preflight as an active mode on subsequent turns. The user will type `/unity-mcp-preflight` again if they want another run.
