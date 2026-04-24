---
name: unity6-api-migration
description: Unity 6 physics and asset API renames. Use when about to write Rigidbody.drag, Rigidbody.angularDrag, PhysicMaterial, PhysicMaterialCombine, or save a .physicsMaterial asset file - any of which now break with CS0117 or CreateAsset errors.
---

# Unity 6 API Migration - Renames You Will Hit

This is Unity 6 (6000.4.0f1, URP 17.4). Several common APIs were renamed; the old names produce `CS0117` at compile time or asset-creation errors at runtime.

## Physics renames

| Old (Unity 5/2022) | New (Unity 6) |
|---|---|
| `Rigidbody.drag` | `Rigidbody.linearDamping` |
| `Rigidbody.angularDrag` | `Rigidbody.angularDamping` |
| `PhysicMaterial` (no `s`) | `PhysicsMaterial` |
| `PhysicMaterialCombine` | `PhysicsMaterialCombine` |

## Asset extension rename

`PhysicsMaterial` assets must be saved with the `.asset` extension, not `.physicsMaterial`. The old extension triggers:

> CreateAsset() should not be used to create a file of type 'physicsMaterial' - consider using AssetDatabase.ImportAsset() ... or change the file type to '*.asset'.

```csharp
var pm = new PhysicsMaterial("Ball_PhysMat") { bounciness = 0.35f, dynamicFriction = 0.15f };
AssetDatabase.CreateAsset(pm, "Assets/VisualAssets/Physics/Ball_PhysMat.asset");
```

## Where to look next

- [Docs~/UNITY_MCP_HELPER.md - Issue 11](../../../Docs~/UNITY_MCP_HELPER.md#11---unity-6-physics-api-renames) for the canonical entry on physics renames.
- [Docs~/UNITY_MCP_HELPER.md - Issue 01](../../../Docs~/UNITY_MCP_HELPER.md#01---physicsmaterial-file-extension) for the asset-extension rule.
- This skill is intentionally Unity-6-scoped (`unity6-` prefix). When the project upgrades to a future major version, add a sibling `unity7-api-migration` skill rather than mutating this one.
