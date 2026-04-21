# Pawchinko Dev Log

> Pillar: *"Strategy you choose + controlled randomness you watch."* Living progress log for AI agents and humans working on this project.

## Read this first (every agent, every session)

1. Read the three design docs before changing anything substantive:
   - [Docs~/Desgin/PAWCHINKO_DESIGN_GUIDE.md](Desgin/PAWCHINKO_DESIGN_GUIDE.md) - what the game is and isn't.
   - [Docs~/Desgin/AI_AGENT_CODE_GUIDE.md](Desgin/AI_AGENT_CODE_GUIDE.md) - folder layout, naming, manager pattern, event bus.
   - [Docs~/Desgin/PHYSICS_DROP_GUIDE.md](Desgin/PHYSICS_DROP_GUIDE.md) - ball/peg/slot rules, collision matrix, modifier extension hook.
2. All gameplay code lives in `Assets/Scripts/` under `namespace Pawchinko`. Compiled into the `Pawchinko` assembly via [Assets/Scripts/Pawchinko.asmdef](../Scripts/Pawchinko.asmdef).
3. Managers communicate **only** through `Pawchinko.EventSystem` (the `[GameEventSystem]` GameObject). Never call another manager directly inside a hot path; use events.
4. Never call `SceneManager.LoadSceneAsync` outside `SceneFlowManager` (does not exist yet - see Next Milestones).
5. Never directly assign `Rigidbody.velocity` or `transform.position` on balls. All ball physics goes through `AddForce` / `AddTorque` only.

## Current state (as of 2026-04-21)

- Unity 6000.4.0f1, URP 17.4, ugui 2.0 (TMP bundled), Input System 1.19.
- Single working scene: [Assets/Scenes/SampleScene.unity](../Scenes/SampleScene.unity). The Boot/Overworld/Battle scene split from the design guide is **not** yet in place; everything lives in SampleScene.
- Layers defined in `ProjectSettings/TagManager.asset`: `Ball=8`, `Peg=9`, `Wall=10`, `Slot=11`. Collision matrix configured per [PHYSICS_DROP_GUIDE.md](Desgin/PHYSICS_DROP_GUIDE.md) Section 3.
- Folder layout matches [AI_AGENT_CODE_GUIDE.md](Desgin/AI_AGENT_CODE_GUIDE.md) Section 2.

### Scripts

```
Assets/Scripts/
  Pawchinko.asmdef
  Core/
    EventSystem.cs   - generic pub/sub, singleton
    Events.cs        - BattleStartedEvent, RoundStartedEvent, DropRequestedEvent,
                       BallSettledEvent, TurnEndedEvent
    Side.cs          - enum { Player, Enemy }
  Managers/
    GameManager.cs   - singleton, owns sub-managers, runs Initialize chain
    BattleManager.cs - turn/round state machine
    BoardManager.cs  - holds per-side BallSpawner refs
    BallManager.cs   - assigns ball IDs, routes Settled callbacks to events
  UI/
    UIManager.cs     - owns BattleHud
    BattleHud.cs     - START + DROP buttons, round counter
  Gameplay/Battle/
    Ball.cs          - Rigidbody/SphereCollider component, Settled event
    Peg.cs           - row/col data marker
    Slot.cs          - trigger collider, forwards entries to Ball
    BallSpawner.cs   - per-board spawner, jitter + torque, optional material override
  Data/
    BoardLayout.cs   - plain data (peg counts, slot count, spacings)
```

### Assets

```
Assets/VisualAssets/
  Materials/
    Board/   PlayerBoard_Mat, EnemyBoard_Mat, Peg_Mat, Wall_Mat, Slot_Mat
    Ball/    PlayerBall_Mat, EnemyBall_Mat
  Physics/   Ball_PhysMat.asset, Peg_PhysMat.asset (PhysicsMaterial - Unity 6 type)
  Prefabs/Battle/
    Ball.prefab      - Rigidbody + SphereCollider + Ball.cs, layer=Ball,
                       linearDamping=0.05, angularDamping=0.2, ContinuousDynamic,
                       maxAngularVelocity=50, PhysicsMaterial assigned
```

### Scene composition (SampleScene)

```
Managers (root)
  GameEventSystem  (Pawchinko.EventSystem)
  GameManager
  BattleManager
  BoardManager
  BallManager
  UIManager
Main Camera        (0, 0, -14), FOV 60, sky-blue clear color
Directional Light  (existing)
Global Volume      (existing URP volume)
Boards
  PlayerBoard      (-3.5, 0, 0)  blue tint
    Backboard, Pegs/ (5x5 staggered), Walls/ (Left/Right/Floor),
    Slots/ (Slot_0..Slot_3 trigger), BallSpawner + SpawnPoint, BallContainer
  EnemyBoard       (+3.5, 0, 0)  red tint, mirrored, EnemyBall_Mat override
Canvas (Screen Space - Overlay, 1920x1080 ScaleWithScreenSize)
  BattleHud
    RoundCounterText (TMP, top-center)
    StartButton      (center, yellow)
    DropPlayerButton (bottom-left, blue, hidden)
    DropEnemyButton  (bottom-right, red, hidden)
EventSystem        (UnityEngine.EventSystems - existing UI input)
```

## Implemented systems

| System | Status | Files | Design ref |
|---|---|---|---|
| Event bus (pub/sub) | MVP | `Core/EventSystem.cs`, `Core/Events.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 9 |
| Manager bootstrap | MVP | `Managers/GameManager.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 7 |
| Turn-based battle flow (1 ball/side, alternating, looping) | MVP | `Managers/BattleManager.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 5 (subset) |
| Physics drop (Rigidbody ball, peg field, wall, slot trigger) | MVP | `Gameplay/Battle/*.cs`, `Ball.prefab`, layer matrix | [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Sections 2-7 |
| Board procedural geometry (in-editor build) | MVP | Not codified yet - currently baked by MCP scene-build pass | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 12 |
| HUD (Start + per-side Drop + round counter) | MVP | `UI/UIManager.cs`, `UI/BattleHud.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 17 (subset) |
| Modifier hook (`IBallModifier`) | NOT STARTED | - | [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Section 9 |
| Scoring | NOT STARTED | - | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 14 |
| Energy | NOT STARTED | - | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 7 |
| Creatures, Stats, Ball Profiles | NOT STARTED | - | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Sections 8-11 |
| Abilities | NOT STARTED | - | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 13 |
| Overworld | NOT STARTED | - | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 4 |
| Boot/Overworld/Battle scene split | NOT STARTED | - | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8 |
| `SceneFlowManager` | NOT STARTED | - | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8 |

## Known gaps and TBDs

(Mirrors the TBDs in the design docs - do **not** invent values, ask the user.)

- Ball-count scaling formula per creature/level - [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 8.
- AP economy (per-round budget, refresh, deploy cost) - Section 8.
- Ability selection scope (active creature only vs team pool) - Section 13.
- Star/tier semantics on abilities - Section 13.
- Canonical board layouts (peg arrangement, bucket count, bucket values) - Section 12.
- Encounter rate formula and per-zone tables - Section 4.
- Reward contents (currency, items, creature drops) and rates - Section 16.
- Trainer roster data - Section 4.
- Audio direction - Section 18.
- Pause/resume snapshot granularity for the overworld - Section 4.
- `EncounterTriggeredEvent` payload - [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8.

## How to extend (for future agents)

1. Read the three design docs first. Do **not** invent values for any TBD without asking the user.
2. New scripts: follow the manager pattern in [AI_AGENT_CODE_GUIDE.md](Desgin/AI_AGENT_CODE_GUIDE.md) Section 7. Subscribe in `Initialize`, unsubscribe in `OnDestroy`.
3. New events: add to `Core/Events.cs`, name ends in `Event`, past tense for "happened".
4. New gameplay code: place under `Gameplay/Battle/` (battle scope) - **never** import overworld code from battle code or vice versa.
5. New assets: place under `VisualAssets/<Category>/<Family>/`. Don't drop files into the wrong category.
6. When adding scene content via Unity MCP `Unity_RunCommand`:
   - Class must be `internal class CommandScript : IRunCommand`.
   - Use `result.Log` (not `Debug.Log`) so messages flow back to the agent.
   - Wrap mutations in `result.RegisterObjectCreation` / `RegisterObjectModification`.
   - Always end with `EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);` if the scene changed.
   - `PhysicsMaterial` files **must** use `.asset` extension (`.physicsMaterial` triggers a CreateAsset error in Unity 6).
   - `UnityEngine.UI.Image` clashes with the `Unity.AI.Assistant.Agent.Dynamic.Extension.Editor` namespace the MCP wraps your script in - use `using UIImage = UnityEngine.UI.Image;` alias.
   - `LayerMask.NameToLayer` returns -1 right after a layer change - end the layer pass with `AssetDatabase.Refresh()` and re-query in the next pass.
7. After substantive script changes, call `Unity_RunCommand` with a no-op `CommandScript` to force a recompile, then `Unity_GetConsoleLogs(logTypes:"Error,Warning")` to verify zero errors. Note: in Play mode, `Unity_GetConsoleLogs` may return empty results - this is a tool limitation, not a bug in your code; verify by inspecting GameObject state instead.

## Next milestones (priority order)

1. **Scoring + Energy (MVP)** - per-ball score on slot trigger, per-round summing, energy delta application, battle end on energy<=0. Adds: `ScoringManager`, `EnergyManager`, events (`BallScoredEvent`, `RoundScoredEvent`, `EnergyChangedEvent`, `BattleEndedEvent`), HUD energy bars and per-side score readout. See [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Sections 7 + 14.
2. **Creatures (data only)** - `Paw` `[Serializable]` data class, ScriptableObject `PawDefinition` for static data, team data on `BattleManager` so per-side energy/ball counts come from creatures, not constants. See [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 8 (will need user input on TBDs).
3. **5-creature roster strip (UI)** - left/right team strips with per-row name + level, active-creature arrow indicator. NO per-row HP bars (hard rule). See [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 17.
4. **Boot/Overworld/Battle scene split** - introduce `Boot.unity`, `Overworld.unity`, move battle assets into `Battle.unity`, wire `SceneFlowManager`. See [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8.
5. **First ability + `IBallModifier` implementation** - any of the worked examples in [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Sections 10-13 makes a good first one.

## Change log

(Reverse chronological. One entry per agent session.)

### 2026-04-21 - Cursor agent (Claude Opus 4.7) - Basic Battle Scene MVP

Stood up the minimal turn-based plinko battle in `SampleScene` per the [Basic Battle Scene MVP plan](../../.cursor/plans/basic_battle_scene_mvp_e67a81b6.plan.md). Specifically:

- Wrote 13 scripts under `Scripts/{Core,Managers,UI,Gameplay/Battle,Data}` + `Pawchinko.asmdef`. Removed the legacy single-file `Scripts/GameManager.cs` stub.
- Configured layers (`Ball/Peg/Wall/Slot` at indices 8-11) and collision matrix via Unity MCP.
- Created URP/Lit materials (board, ball, peg, wall, slot) and `PhysicsMaterial` assets via Unity MCP.
- Built `Ball.prefab` (Rigidbody + SphereCollider + `Ball.cs`, Unity 6 physics tuning).
- Built the entire SampleScene hierarchy (managers, two boards, pegs, walls, slots, spawners, canvas, buttons) and wired every Inspector reference via `SerializedObject` in one idempotent MCP `CommandScript`.
- Verified visually with `Unity_SceneView_CaptureMultiAngleSceneView`: both boards visible, blue/red tinting correct, 5x5 staggered pegs, 4 yellow slots per side, walls correct.
- Verified Main Camera frames both boards (PlayerBoard viewport x=0.37, EnemyBoard viewport x=0.63).
- Smoke test: entered/exited Play mode cleanly, all manager GameObjects persisted, no editor crashes.

Files added/changed:
- `Assets/Scripts/Pawchinko.asmdef` (new)
- `Assets/Scripts/Core/{EventSystem,Events,Side}.cs` (new)
- `Assets/Scripts/Managers/{GameManager,BattleManager,BoardManager,BallManager}.cs` (new; GameManager replaces deleted stub)
- `Assets/Scripts/UI/{UIManager,BattleHud}.cs` (new)
- `Assets/Scripts/Gameplay/Battle/{Ball,Peg,Slot,BallSpawner}.cs` (new)
- `Assets/Scripts/Data/BoardLayout.cs` (new)
- `Assets/VisualAssets/Materials/...` (new, 7 materials)
- `Assets/VisualAssets/Physics/{Ball,Peg}_PhysMat.asset` (new)
- `Assets/VisualAssets/Prefabs/Battle/Ball.prefab` (new)
- `Assets/Scenes/SampleScene.unity` (rebuilt - managers, boards, canvas)
- `ProjectSettings/TagManager.asset` (4 layers added)
- `ProjectSettings/DynamicsManager.asset` (collision matrix updated)
- `Assets/Docs~/DEV_LOG.md` (new - this file)

Follow-ups for the next agent:
- The MVP loops indefinitely; no win condition. Adding scoring + energy (next milestone) gives a battle end.
- Slot triggers fire on first ball entry only (`Ball._hasSettled` flag). Future scoring code must subscribe to `BallSettledEvent`, NOT poll slots.
- `BallSpawner.ballMaterialOverride` lets enemy balls render in their own color via `sharedMaterial` swap; if you ever need per-ball runtime material variation, switch to `material` (which clones).
- The `BattleHud` button positions are anchored to bottom-left and bottom-right corners; resizing the game window keeps them in place.
