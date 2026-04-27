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

## Current state (as of 2026-04-24)

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
    Events.cs        - BattleStartedEvent, RoundStartedEvent (active-pet indices),
                       DropRequestedEvent, BallSettledEvent, RoundScoredEvent,
                       EnergyChangedEvent, BattleEndedEvent
    Side.cs          - enum { Player, Enemy }
  Managers/
    GameManager.cs    - singleton, owns sub-managers, runs Initialize chain
    BattleManager.cs  - round state machine, simultaneous drop, active-pet rotation, BattleOver
    BoardManager.cs   - holds per-side BallSpawner refs
    BallManager.cs    - assigns ball IDs, routes Settled callbacks to events
    ScoringManager.cs - per-round score accumulator, publishes RoundScoredEvent
    EnergyManager.cs  - team-summed energy, applies round diff, publishes BattleEndedEvent on <=0
  UI/
    UIManager.cs     - owns BattleHud
    BattleHud.cs     - Start/Exit/Drop, round counter, roster + active card, energy/score/winner
  Gameplay/Battle/
    Ball.cs          - Rigidbody/SphereCollider component, Settled event
    Peg.cs           - row/col data marker
    Slot.cs          - trigger collider, forwards entries to Ball
    BallSpawner.cs   - per-board spawner, jitter + torque, optional material override
  Data/
    BoardLayout.cs        - plain data (peg counts, slot count, spacings)
    PlaceholderPet.cs     - [Serializable] {petName, level} stand-in for Paw data
    BoardScoringConfig.cs - [Serializable] {slotValues=[1,3,5,3,1]} placeholder bucket values
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
  ScoringManager   (Pawchinko.ScoringManager - per-round score accumulator)
  EnergyManager    (Pawchinko.EnergyManager - team-summed energy)
  UIManager
Main Camera        (0, 0, -19.75), FOV 25, sky-blue clear color
Directional Light  (existing)
Global Volume      (existing URP volume)
Boards
  PlayerBoard      (-3.5, 0, 0)  blue tint
    Backboard, Pegs/ (5x5 staggered), Walls/ (Left/Right/Floor),
    Slots/ (Slot_0..Slot_4 trigger, slotIndex 0..4), BallSpawner + SpawnPoint, BallContainer
  EnemyBoard       (+3.5, 0, 0)  red tint, mirrored, EnemyBall_Mat override
Canvas (Screen Space - Overlay, 1920x1080 ScaleWithScreenSize)
  BattleHud
    RoundCounterBar  (decorative bar behind RoundCounterText)
    RoundCounterText (TMP, top-center, anchor (0.5,1) y=-40)
    TempDevHeader    (TMP, center, "Temp Dev Buttons", italic 24pt, y=+170)
    StartButton      (center, yellow,        y=+90, 280x80)
    ExitButton       (center, red,           y=  0, 280x80, stops Play / Application.Quit)
    DropButton       (center, green,         y=-90, 280x80, interactable toggles per round)
    PlaceholderMarker(TMP italic 18pt top-right "PLACEHOLDER UI" alpha 0.45)
    PlayerEnergyText (TMP 36pt bold top-left  "ENERGY: --")
    EnemyEnergyText  (TMP 36pt bold top-right "ENERGY: --")
    PlayerRoster     (mid-left panel 220x540)
      Header           (TMP "PLAYER")
      PlayerRow_0..4   (Image + Label TMP "Pet N Lv.--")
      PlayerActiveIndicator (Image, hidden until Part 2)
    EnemyRoster      (mid-right, mirrored)
      Header           (TMP "ENEMY")
      EnemyRow_0..4    (Image + Label TMP "Pet N Lv.--")
      EnemyActiveIndicator (Image, hidden until Part 2)
    PlayerActiveCard (bottom-left 340x140)
      Title/Sub/Ability TMPs ("Active: Pet --" / "Ball x--" / "Ability: --")
    EnemyActiveCard  (bottom-right, mirrored)
    RoundScoreText   (bottom-center above DROP, "0 | 0")
    BucketLabelsPlayer (5x BucketValuePlayer_N "--")
    BucketLabelsEnemy  (5x BucketValueEnemy_N "--")
    WinnerOverlay    (full-canvas dim, SetActive false; child WinnerText 96pt)
EventSystem        (UnityEngine.EventSystems - existing UI input)
```

## Implemented systems

| System | Status | Files | Design ref |
|---|---|---|---|
| Event bus (pub/sub) | MVP | `Core/EventSystem.cs`, `Core/Events.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 9 |
| Manager bootstrap | MVP | `Managers/GameManager.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 7 |
| Round-based battle flow (1 ball/side, simultaneous drop, looping) | MVP | `Managers/BattleManager.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 5 (subset) |
| Physics drop (Rigidbody ball, peg field, wall, slot trigger) | MVP | `Gameplay/Battle/*.cs`, `Ball.prefab`, layer matrix | [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Sections 2-7 |
| Board procedural geometry (in-editor build) | MVP | Not codified yet - currently baked by MCP scene-build pass | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 12 |
| HUD (Start/Exit/Drop, round counter, roster, active card, energy, score, winner) | MVP | `UI/UIManager.cs`, `UI/BattleHud.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 17 (placeholder values) |
| Active-pet round-robin (5 placeholder pets/side, indicator + active card) | MVP | `Managers/BattleManager.cs`, `Data/PlaceholderPet.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 5 (subset) |
| Scoring (per-round accumulation, slot value lookup) | MVP (placeholder values) | `Managers/ScoringManager.cs`, `Data/BoardScoringConfig.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 14 |
| Energy (team-summed, win on <=0, placeholder seed) | MVP (placeholder values) | `Managers/EnergyManager.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 7 |
| Modifier hook (`IBallModifier`) | NOT STARTED | - | [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Section 9 |
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

1. **Creatures (data only)** - `Paw` `[Serializable]` data class, ScriptableObject `PawDefinition` for static data. Replace `PlaceholderPet` + `EnergyManager.placeholderEnergyPerPet` + `BoardScoringConfig.slotValues` placeholders with real per-creature `Energy Value` + canonical bucket layouts. See [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Sections 8 + 12 (will need user input on TBDs).
2. **Boot/Overworld/Battle scene split** - introduce `Boot.unity`, `Overworld.unity`, move battle assets into `Battle.unity`, wire `SceneFlowManager`. See [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8.
3. **First ability + `IBallModifier` implementation** - any of the worked examples in [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Sections 10-13 makes a good first one. Wire ability selection into the round flow before the drop.
4. **Per-creature ball-count contribution** - replace the hardcoded "1 ball per side per round" with the sum of each side's active-pet ball contribution. Update `BattleHud.UpdateActiveCard.Sub` to display the real ball count. Depends on milestone 1.
5. **3D creature stage** - replace the 2D roster strips with 5x 3D creature meshes per side along the outer board edges per [PAWCHINKO_DESIGN_GUIDE Section 6](Desgin/PAWCHINKO_DESIGN_GUIDE.md). Roster strip drops back to a thin name/level overlay.

Completed since last review:
- ~~Scoring + Energy (MVP)~~ - done 2026-04-24 with placeholder values.
- ~~5-creature roster strip (UI)~~ - done 2026-04-24 as placeholder strips with active-pet rotation.

## Change log

(Reverse chronological. One entry per agent session.)

### 2026-04-24 - Cursor agent (Claude Opus 4.7) - Bucket visuals + BattleManager.OnBallSettled cleanup

Small follow-up after the Part 3 playtest. User wanted the bucket positions to read visually so it's obvious where balls land; also fixes a noisy (but harmless) warning surfaced by playtesting.

- **Bucket visuals**: added a `BucketVisual` child cube under each `Slot_0..4` on both boards. **No collider** (the auto-added `BoxCollider` on the primitive is destroyed before parenting), so balls pass straight through and physics resolution is untouched per [PHYSICS_DROP_GUIDE Section 7](Desgin/PHYSICS_DROP_GUIDE.md). Local position `(0, -0.15, 0)` and local scale `(0.55, 0.6, 0.7)` - sits inside the slot trigger volume, resting on the `WallFloor` between the side walls, just under the bucket value number that floats in the HUD above. (First pass placed them at `(0, -0.7, 0)` which hung below the floor outside the board; corrected on user feedback.)
- **Color tiers** by slot value: low (1) = cool blue, mid (3) = green, high (5) = vibrant orange. New URP/Lit materials under `Assets/VisualAssets/Materials/Bucket/`:
  - `Bucket_Low_Mat.mat`  - `(0.18, 0.42, 0.85)`
  - `Bucket_Mid_Mat.mat`  - `(0.20, 0.75, 0.35)`
  - `Bucket_High_Mat.mat` - `(1.00, 0.55, 0.10)`
- **`BattleManager` cleanup**: dropped the `BallSettledEvent` subscription + `OnBallSettled` handler. The handler was only logging since Part 3 moved round-advance onto `RoundScoredEvent`. It also produced a confusing "BallSettled received in unexpected state" warning whenever the second ball of a round triggered the synchronous `BallSettledEvent -> ScoringManager -> RoundScoredEvent -> BattleManager` cascade (state had already advanced past `BallsInFlight` by the time `BattleManager` saw the original `BallSettledEvent`). `ScoringManager` already owns the per-ball settle work; `BattleManager` doesn't need a duplicate subscriber. Per [Keep it lean](Desgin/AI_AGENT_CODE_GUIDE.md#keep-it-lean).

Files added/changed:
- `Assets/VisualAssets/Materials/Bucket/{Bucket_Low_Mat,Bucket_Mid_Mat,Bucket_High_Mat}.mat` (new)
- `Assets/Scenes/SampleScene.unity` (10x BucketVisual cubes, 5 per board)
- `Assets/Scripts/Managers/BattleManager.cs` (dropped OnBallSettled subscription + handler)
- `Assets/Docs~/DEV_LOG.md` (this entry)

Verified: 10 visuals total, all with renderer + tier material, **0 colliders** (re-checked via MCP). Multi-angle scene capture shows blue/green/orange/green/blue gradient at the bottom of both boards. Console clean post-recompile.

### 2026-04-24 - Cursor agent (Claude Opus 4.7) - Part 3: scoring + energy + winner

Final slice of the [battle-ui-rounds-energy plan](../../.cursor/plans/battle-ui-rounds-energy_ec5b89f6.plan.md). Closes the loop: balls now score, energy now ticks, and one side eventually wins. All values are placeholder per [PAWCHINKO_DESIGN_GUIDE Sections 7 + 12 + 14](Desgin/PAWCHINKO_DESIGN_GUIDE.md).

- **Events** (`Core/Events.cs`): added `RoundScoredEvent(round, playerScore, enemyScore)`, `EnergyChangedEvent(playerEnergy, enemyEnergy)`, `BattleEndedEvent(Side winner)`.
- **Data** (`Scripts/Data/BoardScoringConfig.cs`, new): `[Serializable] [Preserve]` plain class with `int[] slotValues = {1,3,5,3,1}` - placeholder bucket values until canonical board layouts exist (Section 12 TBD).
- **`ScoringManager`** (new): subscribes to `RoundStartedEvent` (resets accumulators) + `BallSettledEvent` (looks up slot value, accumulates per-side score, publishes `RoundScoredEvent` once both sides have settled).
- **`EnergyManager`** (new): subscribes to `BattleStartedEvent` (seeds 5 pets * 10 energy = 50 per side, publishes `EnergyChangedEvent`) + `RoundScoredEvent` (applies `playerEnergy += diff; enemyEnergy -= diff` per [Section 7](Desgin/PAWCHINKO_DESIGN_GUIDE.md), publishes `EnergyChangedEvent`, then publishes `BattleEndedEvent` if either side <= 0). All numbers exposed as `[SerializeField]` placeholders so they're tweakable without code changes.
- **`BattleManager`**: round advance moved out of `OnBallSettled` and into `OnRoundScored` so `EnergyManager` updates land before the HUD is re-armed for the next drop. New `BattleOver` state pinned by `OnBattleEnded` blocks further drops; pressing Start again resets to round 1. Removed unused `playerSettled` / `enemySettled` flags (Keep it lean - only kept while they served a real purpose).
- **`BattleHud`**: new `[Header("Energy / Score / Winner")]` block (5 fields). Subscribes to `RoundScoredEvent` (updates `RoundScoreText`), `EnergyChangedEvent` (updates both energy texts), `BattleEndedEvent` (activates `WinnerOverlay`, sets `WinnerText` to "WINNER: PLAYER/ENEMY", disables Drop, re-enables Start). Start click hides the overlay so a fresh battle can begin.
- **`GameManager`**: added `[SerializeField]` for `scoringManager` + `energyManager` plus public getters. `InitializeManagers` order is now `Board -> Ball -> Scoring -> Energy -> Battle -> UI` so subscribers exist before publishers fire (and `EnergyManager` subscribes to `RoundScoredEvent` before `BattleManager` does, guaranteeing the energy delta + `BattleEndedEvent` land before `BattleManager` advances the round).
- **Scene** (mutated via two Unity MCP `CommandScript` passes):
  - Slot rebuild: each board's `Slots/` rebuilt to 5 trigger-only `Slot_0..4` (size 0.6x1x1, local X evenly spaced -1.4..+1.4, layer `Slot`, `slotIndex` 0..4 wired via `SerializedObject`). Old 4-slot layout destroyed.
  - Bucket value labels (`BucketValuePlayer_N` / `BucketValueEnemy_N` from Part 1) re-anchored via `Camera.WorldToViewportPoint` of each new slot, hovering ~10% viewport-y above. Text set to "1", "3", "5", "3", "1".
  - Added `Managers/ScoringManager` + `Managers/EnergyManager` GameObjects with the new components. Wired `eventSystem` on each (using the **`Pawchinko.EventSystem`** on `GameEventSystem` per [UNITY_MCP_HELPER Issue 12](UNITY_MCP_HELPER.md#12---two-eventsystem-types-coexist), aliased to `PEventSystem` to disambiguate). Wired `GameManager.scoringManager` / `energyManager` and `BattleHud.{playerEnergyText, enemyEnergyText, roundScoreText, winnerOverlay, winnerText}` via `SerializedObject.FindProperty` per [Issue 14](UNITY_MCP_HELPER.md#14---wiring-serializefield-private-refs-from-a-commandscript).

Verified: every new SerializedField reports `assigned=true`. Both boards have exactly 5 trigger-only slots on the `Slot` layer with `slotIndex` 0..4. Zero console errors / warnings post-recompile.

Files added/changed:
- `Assets/Scripts/Core/Events.cs` (3 new events)
- `Assets/Scripts/Data/BoardScoringConfig.cs` (new)
- `Assets/Scripts/Managers/ScoringManager.cs` (new)
- `Assets/Scripts/Managers/EnergyManager.cs` (new)
- `Assets/Scripts/Managers/BattleManager.cs` (round advance gated on RoundScoredEvent)
- `Assets/Scripts/Managers/GameManager.cs` (owns ScoringManager + EnergyManager, init order)
- `Assets/Scripts/UI/BattleHud.cs` (energy/score/winner)
- `Assets/Scenes/SampleScene.unity` (5 slots/side, 2 new manager GOs, HUD wiring)
- `Assets/Docs~/DEV_LOG.md` (this entry, Scripts/Implemented/Scene composition/Next milestones updates)

Manual playtest TODO (cannot be automated per [Issue 06](UNITY_MCP_HELPER.md#06---unity_getconsolelogs-returns-empty-in-play-mode)):
1. Press Start - both energy texts should read "ENERGY: 50", indicators move to row 0 on both rosters, active cards read "Active: Pet 1 Lv.1 / Ball x1".
2. Mash Drop - per round, RoundScoreText updates to e.g. "5 | 3", energy texts decrement / increment by the diff, indicators advance through rows 0..4..0, bucket labels along the bottom show 1/3/5/3/1.
3. Eventually one side hits 0 - WinnerOverlay appears with "WINNER: PLAYER" or "WINNER: ENEMY", Drop disables, Start re-enables.
4. Press Start again - overlay hides, fresh round 1 begins.

Follow-ups for the next agent:
- All numbers (energy per pet, slot values, balls per round) are placeholder. Replace once `Paw` / `PawDefinition` ScriptableObject creature data lands ([Section 8](Desgin/PAWCHINKO_DESIGN_GUIDE.md)).
- `BattleHud.UpdateActiveCard` still hardcodes "Ball x1" - derive from creature ball-count contribution once available.
- Bucket labels are anchored to viewport coords from a one-shot capture; if the camera moves, they will drift. A future pass can add a `UiBucketLabel` component that re-runs `WorldToViewportPoint` in `LateUpdate` against a target slot.
- Boot/Overworld/Battle scene split is still pending - all systems live in `SampleScene` ([AI_AGENT_CODE_GUIDE Section 8](Desgin/AI_AGENT_CODE_GUIDE.md)).

### 2026-04-24 - Cursor agent (Claude Opus 4.7) - Part 2: round-robin active pet + roster wiring

Second slice of the [battle-ui-rounds-energy plan](../../.cursor/plans/battle-ui-rounds-energy_ec5b89f6.plan.md). Adds placeholder team data + a per-side round-robin active-pet rotation. The drop loop still runs forever (no scoring/energy yet) - only the active indicator and active card text now move per round.

- **Data** (`Scripts/Data/PlaceholderPet.cs`, new): `[Serializable] [Preserve]` plain class with `petName` + `level`. Stand-in until real `Paw` / `PawDefinition` ScriptableObject data exists ([PAWCHINKO_DESIGN_GUIDE Section 8](Desgin/PAWCHINKO_DESIGN_GUIDE.md)).
- **Events** (`Core/Events.cs`): `RoundStartedEvent` now carries `PlayerActivePetIndex` + `EnemyActivePetIndex` (0..4). Per [Keep it lean](Desgin/AI_AGENT_CODE_GUIDE.md#keep-it-lean), no overload preserving the old single-arg constructor.
- **`BattleManager`**: added `playerTeam` / `enemyTeam` (5 placeholder pets each), `playerActiveIndex` / `enemyActiveIndex` runtime state, and `GetActivePet(Side)` read-only convenience for the HUD. Indices reset to 0 on `BattleStartedEvent`, increment `(idx + 1) % 5` after both sides settle, and are republished in the next `RoundStartedEvent`. `EnsureDefaultTeams()` keeps the script runnable even without Inspector wiring.
- **`BattleHud`**: new `[Header("Roster")]` block (5x `playerRosterRows`, 5x `enemyRosterRows`, two `*ActiveIndicator` RectTransforms) and `[Header("Active Cards")]` block (4 TMP texts). `OnRoundStarted` repositions the indicator to the active row's vertical center, activates it, and reads `BattleManager.GetActivePet(side)` to update the active-card title (`"Active: <name> Lv.<level>"`) and sub (`"Ball x1"`).
- **Scene**: one Unity MCP wiring pass populated `BattleManager.playerTeam` / `enemyTeam` (5 placeholder pets named `"Pet 1".."Pet 5"`, level 1) and wired all new `BattleHud` SerializedField references via `SerializedObject.FindProperty` per [UNITY_MCP_HELPER Issue 14](UNITY_MCP_HELPER.md#14---wiring-serializefield-private-refs-from-a-commandscript).

Verified: every new SerializedField reports `assigned=true` / arrays sized 5 with all 5 elements filled. Zero console errors / warnings after recompile.

Files added/changed:
- `Assets/Scripts/Data/PlaceholderPet.cs` (new)
- `Assets/Scripts/Core/Events.cs` (RoundStartedEvent extended)
- `Assets/Scripts/Managers/BattleManager.cs` (teams + rotation + GetActivePet)
- `Assets/Scripts/UI/BattleHud.cs` (roster + active-card wiring)
- `Assets/Scenes/SampleScene.unity` (BattleManager team data + BattleHud refs)
- `Assets/Docs~/DEV_LOG.md` (this entry, Scripts table update below)

Follow-ups for the next agent (Part 3):
- Round still advances on raw `BallSettledEvent`. Part 3 introduces `ScoringManager` + `EnergyManager` and gates round advance on the new `RoundScoredEvent` so energy updates land before the next round starts.
- `ActiveCard.Sub` is hardcoded to "Ball x1". Once creatures own ball-count contributions, this should derive from the active pet.
- The bottom-right `EnemyActiveCard.Ability` text is currently inert - abilities are out of scope per [PAWCHINKO_DESIGN_GUIDE Section 13](Desgin/PAWCHINKO_DESIGN_GUIDE.md) (TBD selection scope).

### 2026-04-24 - Cursor agent (Claude Opus 4.7) - Part 1: placeholder battle HUD blocking

First slice of the [battle-ui-rounds-energy plan](../../.cursor/plans/battle-ui-rounds-energy_ec5b89f6.plan.md). Pure UI blocking pass under `Canvas/BattleHud` - no script changes, no behavior change. Every label is a clearly-marked placeholder (`"--"`, `"Pet 1 Lv.--"`, `"PLACEHOLDER UI"` watermark) so no illustrative value gets confused for canonical per [PAWCHINKO_DESIGN_GUIDE.md Section 6](Desgin/PAWCHINKO_DESIGN_GUIDE.md).

Added under `Canvas/BattleHud` in one idempotent Unity MCP CommandScript pass (cleanup-then-build of the new children only; existing Start/Exit/Drop/RoundCounter/TempDevHeader untouched):

- `PlaceholderMarker` (top-right watermark, italic 18pt, alpha 0.45)
- `RoundCounterBar` (decorative bar behind existing RoundCounterText)
- `PlayerEnergyText` / `EnemyEnergyText` (top corners, 36pt bold, "ENERGY: --")
- `PlayerRoster` / `EnemyRoster` panels with `Header`, 5 rows (`PlayerRow_0..4` / `EnemyRow_0..4`) each containing a tinted background `Image` + `Label` TMP "Pet N Lv.--", and a hidden yellow `*ActiveIndicator` square
- `PlayerActiveCard` / `EnemyActiveCard` (bottom corners, 340x140, with `Title` "Active: Pet --", `Sub` "Ball x--", `Ability` "Ability: --")
- `RoundScoreText` (bottom-center above DROP, "0 | 0")
- `BucketLabelsPlayer` / `BucketLabelsEnemy` containers, each with 5 `BucketValuePlayer_N` / `BucketValueEnemy_N` "--" labels positioned approximately by viewport coords (will be re-anchored to the real 5-slot world positions in Part 3)
- `WinnerOverlay` full-canvas dim panel + `WinnerText` 96pt - `SetActive(false)` initially

Used `using UIImage = UnityEngine.UI.Image;` alias per [UNITY_MCP_HELPER Issue 02](UNITY_MCP_HELPER.md#02---image-type-clashes-with-mcp-wrapper-namespace) to avoid the `Image` namespace clash inside the MCP wrapper namespace.

Files added/changed:
- `Assets/Scenes/SampleScene.unity` (UI hierarchy only)
- `Assets/Docs~/DEV_LOG.md` (this entry, scene composition update below)

Follow-ups for the next agent (Part 2):
- Roster rows + active indicators are inert. Part 2 wires `BattleHud` to read `RoundStartedEvent`'s new active-pet indices, repositions the indicators, and updates the active-card text per round.
- Bucket labels still show "--" - Part 3 sets them to {1,3,5,3,1} after the slot rebuild.
- Energy / score / winner texts are blank - Part 3 hooks them to `EnergyManager` / `ScoringManager`.

### 2026-04-21 - Cursor agent (Claude Opus 4.7) - Simultaneous drop + temp dev HUD

Replaced the alternating-turn flow with a simultaneous both-sides drop, and reshaped the HUD to a single centered "Temp Dev Buttons" stack. Per the [Keep it lean](Desgin/AI_AGENT_CODE_GUIDE.md#keep-it-lean) rule, deleted obsolete event surface rather than leaving compat shims.

- **Events** (`Core/Events.cs`):
  - `DropRequestedEvent` is now sideless (drops both sides at once).
  - `RoundStartedEvent` no longer carries `ActiveSide` - one round = one simultaneous drop.
  - `TurnEndedEvent` deleted (no per-side turn flip anymore).
- **`BattleManager`**: state machine reduced to `WaitingForStart -> WaitingForDrop -> BallsInFlight -> WaitingForDrop`. Tracks `playerSettled` / `enemySettled` bools; round only increments once both balls have settled, then re-publishes `RoundStartedEvent` to re-arm the Drop button.
- **`BattleHud`**: replaced the two side-specific buttons with a single centered Drop button. Added Exit (stops Play in editor / `Application.Quit` in builds) and a "Temp Dev Buttons" italic header. Drop uses `interactable = false/true` (not SetActive) so the stack layout doesn't shift between drops.
- **Scene** (`SampleScene.unity`, mutated via Unity MCP):
  - Destroyed `DropPlayerButton` and `DropEnemyButton`.
  - Repositioned `StartButton` to `(0, +90)`, size 280x80.
  - Created `TempDevHeader` (TMP, italic 24pt) at `(0, +170)`, `ExitButton` (red) at `(0, 0)`, `DropButton` (green) at `(0, -90)`.
  - Re-wired `BattleHud.startButton` / `exitButton` / `dropButton` SerializedFields via `SerializedObject.FindProperty(<storage name>)` per [Issue 14](UNITY_MCP_HELPER.md#14---wiring-serializefield-private-refs-from-a-commandscript).

Files added/changed:
- `Assets/Scripts/Core/Events.cs` (TurnEndedEvent removed; DropRequestedEvent / RoundStartedEvent simplified)
- `Assets/Scripts/Managers/BattleManager.cs` (round state machine + simultaneous drop)
- `Assets/Scripts/UI/BattleHud.cs` (Start/Exit/Drop with interactable toggle)
- `Assets/Scenes/SampleScene.unity` (HUD rebuild)
- `Assets/Docs~/DEV_LOG.md` (this entry, scene composition, scripts table, implemented systems row)

Follow-ups for the next agent:
- The HUD is explicitly labelled "Temp Dev Buttons". Replace this with the real per-side roster strips ([Next milestones](#next-milestones-priority-order) #3) before vertical-slice playtesting; the dev stack is purely for triggering the loop in-editor.
- `Exit` is wired to `EditorApplication.isPlaying = false` in editor and `Application.Quit()` in builds. There is no save/confirm flow yet - safe to use because there is no persistent state to lose.
- `BattleManager` ignores `BallSettledEvent` if state isn't `BallsInFlight`; this is correct now but will need revisiting once Slot triggers fire for multiple balls per round (e.g. abilities that spawn extra balls).

### 2026-04-21 - Cursor agent (Claude Opus 4.7) - Camera reframe + Z-axis ball lock

Small tuning pass driven by user feedback on the MVP drop:

- **Main Camera reframe** (manually adjusted by user, recorded here): position `(0, 0, -14) -> (0, 0, -19.75)`, FOV `60 -> 25`. Tighter framing on both boards with less perspective distortion. Updated [Scene composition](#scene-composition-samplescene) accordingly.
- **Ball Z-axis lock**: balls were drifting along Z on first peg impact and falling out of the board container. Added `RigidbodyConstraints.FreezePositionZ` on `Assets/VisualAssets/Prefabs/Battle/Ball.prefab` (`m_Constraints: 0 -> 8`) so balls only translate on X/Y. No invisible walls were added, per user direction. Rotation is left fully unconstrained for now; if peg collisions look off (ball wobbling around X/Y axes), a follow-up agent can additionally freeze `RotationX | RotationY` (constraint mask `8 | 16 | 32 = 56`) for a strict 2D feel.

Files added/changed:
- `Assets/VisualAssets/Prefabs/Battle/Ball.prefab` (Rigidbody constraints)
- `Assets/Docs~/DEV_LOG.md` (this entry + Main Camera line)

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
