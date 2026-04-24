---
name: unity-serialized-field-wire
description: Wire `[SerializeField] private` Inspector references from a Unity_RunCommand CommandScript using SerializedObject.FindProperty. Use whenever assigning a private serialized field on a component, or when FindProperty returns null and the field is known to exist.
---

# Wire [SerializeField] private refs from a CommandScript

`SerializedObject` walks fields by their **storage name** (the lowercase `[SerializeField]` field), NOT the public PascalCase property. This trips up agents because the public-facing name is the obvious thing to type.

## The rule

For a field declared like:

```csharp
[SerializeField] private EventSystem eventSystem;
public EventSystem EventSystem => eventSystem;
```

Use the field name `"eventSystem"`, not the property name `"EventSystem"`:

```csharp
var so = new SerializedObject(targetComponent);
var prop = so.FindProperty("eventSystem"); // matches the storage name
if (prop == null) { result.LogError("field not found"); return; }
prop.objectReferenceValue = eventSystemComponent;
so.ApplyModifiedPropertiesWithoutUndo();
```

## Common mistakes that produce a null `prop`

- Passing the PascalCase property name (`"EventSystem"`).
- Passing the FormerlySerializedAs name when the field has been renamed (use the current storage name).
- Using `prop.objectReferenceInstanceIDValue` when the assignment target is a component and you have the component reference - use `objectReferenceValue` instead.
- Forgetting `so.ApplyModifiedPropertiesWithoutUndo()` (or `ApplyModifiedProperties()` if you do want Undo) - without it the assignment is lost on the next domain reload.

## Where to look next

- [Docs~/UNITY_MCP_HELPER.md - Issue 14](../../../Docs~/UNITY_MCP_HELPER.md#14---wiring-serializefield-private-refs-from-a-commandscript) for the canonical entry.
- The 2026-04-21 dev log entry "Simultaneous drop + temp dev HUD" in [Docs~/DEV_LOG.md](../../../Docs~/DEV_LOG.md) shows a real Pawchinko case wiring `BattleHud.startButton`, `exitButton`, `dropButton`.
