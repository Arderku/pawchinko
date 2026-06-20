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

## Current state (as of 2026-04-28)

- Unity 6000.4.0f1, URP 17.4, ugui 2.0 (TMP bundled), Input System 1.19.
- Working scene flow: [Assets/Scenes/Boot.unity](../Scenes/Boot.unity) loads [Assets/Scenes/Overworld.unity](../Scenes/Overworld.unity), then `SceneFlowManager` additively loads/unloads [Assets/Scenes/Battle.unity](../Scenes/Battle.unity) on encounter/battle-end events. [Assets/Scenes/SampleScene.unity](../Scenes/SampleScene.unity) remains as legacy/reference battle content.
- Layers defined in `ProjectSettings/TagManager.asset`: `Ball=8`, `Peg=9`, `Wall=10`, `Slot=11`, `Player=12`, `PomPortrait=13`. Collision matrix configured per [PHYSICS_DROP_GUIDE.md](Desgin/PHYSICS_DROP_GUIDE.md) Section 3.
- Folder layout matches [AI_AGENT_CODE_GUIDE.md](Desgin/AI_AGENT_CODE_GUIDE.md) Section 2.

### Scripts

```
Assets/Scripts/
  Pawchinko.asmdef
  Core/
    EventSystem.cs   - generic pub/sub, singleton
    Events.cs        - EncounterTriggeredEvent, OverworldPausedEvent,
                       OverworldResumedEvent, BattleStartedEvent, RoundStartedEvent,
                       DropRequestedEvent, BallSettledEvent, RoundScoredEvent,
                       EnergyChangedEvent, BattleEndedEvent
    Side.cs          - enum { Player, Enemy }
  Managers/
    GameManager.cs    - Boot singleton, owns persistent systems + scene registrations
    SceneFlowManager.cs - sole owner of scene load/unload calls
    OverworldManager.cs - scene root for player/camera pause and resume
    BattleSceneRoot.cs - battle scene composition root, owns init order
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
  Gameplay/Overworld/
    OverworldPlayerController.cs - top-down CharacterController movement + input pause
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

### Scene composition (Boot / Overworld / Battle)

```
Boot.unity
  GameEventSystem  (Pawchinko.EventSystem, persistent)
  GameManager      (Pawchinko.GameManager + SceneFlowManager, persistent)

Overworld.unity
  Directional Light, Global Volume
  Main Camera + CinemachineCamera
  EventSystem      (UnityEngine.EventSystems - UI input)
  Environment      (Game Corner model, BoxWooden, disabled Plane)
  Player           (CharacterController + OverworldPlayerController)
  Managers         (OverworldManager)

Battle.unity
  Directional Light, Global Volume, Main Camera
  EventSystem      (UnityEngine.EventSystems - battle UI input)
  Managers         (BattleSceneRoot, BattleManager, BoardManager, BallManager,
                    ScoringManager, EnergyManager, UIManager)
  Boards           (PlayerBoard + EnemyBoard, spawners, pegs, slots)
  Canvas/BattleHud (temp Start/Exit/Drop controls, roster, energy, score, winner)
```

## Implemented systems

| System | Status | Files | Design ref |
|---|---|---|---|
| Event bus (pub/sub) | MVP | `Core/EventSystem.cs`, `Core/Events.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 9 |
| Manager bootstrap | MVP | `Managers/GameManager.cs`, `Managers/BattleSceneRoot.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 7 |
| Boot/Overworld/Battle scene split | MVP | `Scenes/{Boot,Overworld,Battle}.unity`, `Managers/SceneFlowManager.cs` | [AI_AGENT_CODE_GUIDE](Desgin/AI_AGENT_CODE_GUIDE.md) Section 8 |
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
| Overworld | MVP shell | `Scenes/Overworld.unity`, `Managers/OverworldManager.cs`, `Gameplay/Overworld/OverworldPlayerController.cs` | [PAWCHINKO_DESIGN_GUIDE](Desgin/PAWCHINKO_DESIGN_GUIDE.md) Section 4 |

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
4. New gameplay code: place under the correct scene folder (`Gameplay/Overworld/` or `Gameplay/Battle/`) - **never** import overworld code from battle code or vice versa.
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
2. **First encounter trigger** - add a minimal `EncounterZone` in `Gameplay/Overworld/` that publishes `EncounterTriggeredEvent` and supplies whatever payload the design settles on.
3. **First ability + `IBallModifier` implementation** - any of the worked examples in [PHYSICS_DROP_GUIDE](Desgin/PHYSICS_DROP_GUIDE.md) Sections 10-13 makes a good first one. Wire ability selection into the round flow before the drop.
4. **Per-creature ball-count contribution** - replace the hardcoded "1 ball per side per round" with the sum of each side's active-pet ball contribution. Update `BattleHud.UpdateActiveCard.Sub` to display the real ball count. Depends on milestone 1.
5. **3D creature stage** - replace the 2D roster strips with 5x 3D creature meshes per side along the outer board edges per [PAWCHINKO_DESIGN_GUIDE Section 6](Desgin/PAWCHINKO_DESIGN_GUIDE.md). Roster strip drops back to a thin name/level overlay.

Completed since last review:
- ~~Scoring + Energy (MVP)~~ - done 2026-04-24 with placeholder values.
- ~~5-creature roster strip (UI)~~ - done 2026-04-24 as placeholder strips with active-pet rotation.

## Change log

(Reverse chronological. One entry per agent session.)

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - All growth styles converge to the 25 cap at level 50

Per user ("at level 50 each of them just end up at 25 balls"): every style now shares the same destination - the `MaxBallsCap` (25) at level 50 - and differs only in the journey (Min + climb shape). Set every `GetCurve` Max to `MaxBallsCap`. For Lucky Chaos, tapered the bounce band by `(1 - bt)` so it converges exactly onto the cap at the top bracket while still bouncing earlier.

- Verified via MCP - all five hit 25 at L50. Per-band (Lv 1-5 .. 46-50): Steady 4,10,13,15,17,19,20,22,24,25; Growing Rush 2,5,7,10,12,15,17,20,22,25; Power Spikes 2,4,5,12,14,15,22,23,24,25; Late Bloomer 1,1,2,4,6,8,12,16,20,25; Lucky Chaos 2,3,2,10,12,17,17,21,23,25. Run-totals L1-50 now express the tradeoff: Steady 845 (front-loaded, banks most), Power Spikes 730, Growing Rush 675, Lucky Chaos 660, Late Bloomer 475 (patience tax, same ceiling).
- Files: `PomBallCount.cs` (Max=cap for all, Lucky Chaos band taper, balance comment), `PomData.cs` (enum docs), `PAWCHINKO_DESIGN_GUIDE.md` (§8).

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - All growth styles now step on the 5-level grid

Per user ("we do different each 5 levels"): the whole roster should progress in readable 5-level chunks, not just Power Spikes. `Evaluate` now quantizes **every** style to the 5-level grid - the count is flat within each band (Lv 1-5, 6-10, ... 46-50) and only changes at a band boundary. The styles now differ purely in the **shape** of the 10-step climb, not in how often they change.

- **`PomBallCount.Evaluate`** computes a single `bracket = (level-1)/StepLevelInterval` (0..9) and `bt = bracket/topBracket`, then feeds `bt` (not per-level `t`) into each style's shape: Steady `Pow(bt,0.6)`, Growing Rush linear `bt`, Late Bloomer `Pow(bt,2.0)`, Power Spikes via new `PowerSpikeShape[]`, Lucky Chaos band keyed to the **bracket**. Removed `EvaluateSteps` (folded into the unified bracket math).
- **Power Spikes is genuinely spiky now** (so it stays distinct from linear once both step every 5 levels): new `PowerSpikeShape` cumulative array `{0,.10,.15,.45,.50,.55,.85,.90,.95,1}` = long flats with big jumps at brackets 2->3 and 5->6. Bumped its curve to 2->18. `Hash01` param renamed `level`->`key` (now a bracket index).
- **Verified via MCP** that all five styles change only at L6/11/16/21/26/31/36/41/46. Per-band counts (Lv 1-5 .. 46-50): Steady 4,4,6,8,9,10,11,12,12,13; Growing Rush 2,4,5,7,9,10,12,14,15,17; Power Spikes 2,4,4,9,10,11,16,16,17,18; Late Bloomer 1,1,2,3,5,7,10,14,18,22; Lucky Chaos 2,2,2,7,9,13,11,17,17,17. Sums L1-50: Steady 475, Growing Rush 475, Power Spikes 535, Lucky Chaos 485, Late Bloomer 415.
- **`PomDataEditor`** preview now lists one row per 5-level band ("Lv 1-5" ... "Lv 46-50") and states every style changes only every 5 levels. Updated enum docs (`PomData`) and design guide §8 accordingly.

Files: `PomBallCount.cs`, `PomData.cs` (enum docs), `PomDataEditor.cs` (banded preview), `PAWCHINKO_DESIGN_GUIDE.md` (§8).

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - Ball Growth balance pass + Steady Paws now progresses

Per user: Steady Paws felt wrong as a flat line (always 6, no progress), and the styles needed balancing. All edits are in `PomBallCount` (still the single shared source of truth).

- **Steady Paws is no longer flat.** It's now a gentle, front-loaded climb (4 -> 13) using `SteadyPawsExponent = 0.6` (concave: rises fast early, then levels off). Reads as "reliable, strong early". Updated the enum/doc/guide wording (was "same count at every level").
- **Rebalanced all five so L50 endpoints + run-totals sit in a tighter band**, differing in journey not destination. New curves in `GetCurve`: Steady 4->13, Growing Rush 2->17 (linear), Power Spikes 2->17 (steps every 5 levels), Late Bloomer 1->22 (back-loaded, exponent 2.0, biggest finish), Lucky Chaos 2->17 (bounce). Sum of balls L1..50: Steady 480, Power Spikes 475, Growing Rush 475, Lucky Chaos 492, Late Bloomer 402 (intentionally lowest total but highest ceiling = patience payoff). Previously the spread was 265..512.
- **Fixed the Lucky Chaos hash bias.** The old FNV+single-shift finalizer skewed low, so Lucky Chaos sat *under* its own line at high levels (L50=12 vs max 16). Replaced `Hash01` with a multi-round integer finalizer; the bounce is now symmetric around the linear line (e.g. L5=6 above, mid around the line, occasional dips), still fully deterministic per level.
- **`StyleCurve` and stepping unchanged** otherwise (Power Spikes still uses `StepLevelInterval = 5`).

Files changed:
- `Assets/Scripts/Gameplay/Pom/PomBallCount.cs` (curves, Steady concave, stronger hash, balance comment)
- `Assets/Scripts/Data/Pom/PomData.cs` (SteadyPaws enum doc)
- `Assets/Docs~/Desgin/PAWCHINKO_DESIGN_GUIDE.md` (§8 Steady Paws wording)
- `Assets/Docs~/DEV_LOG.md` (this entry)

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - Power Spikes steps every 5 levels + clearer edit point

Small tuning follow-up. (1) Where to edit per-style ball numbers is now an explicit, commented block: `PomBallCount.GetCurve(style)` (Min = balls at L1, Max = balls at L50). (2) The stepped **Power Spikes** style now jumps once **every 5 levels** instead of ~12: added `PomBallCount.StepLevelInterval = 5`; `EvaluateSteps` brackets by `(level-1)/interval`, so brackets are 1-5, 6-10, ... 46-50. Dropped the now-unused per-style `steps` from `StyleCurve` (Min/Max only). Verified: Power Spikes = 1,1,1,1,1 / 3 / 4 / 6 / 8 / 9 / 11 / 13 / 14 / 16 across the ten 5-level brackets. The continuous styles (Linear/Curve/Lucky) are unchanged and still grow smoothly by design.

- `Assets/Scripts/Gameplay/Pom/PomBallCount.cs` (StepLevelInterval, 5-level bracket math, slimmer StyleCurve, labeled edit block)
- `Assets/Docs~/Desgin/PAWCHINKO_DESIGN_GUIDE.md` (§8 Power Spikes note)
- `Assets/Docs~/DEV_LOG.md` (this entry)

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - Ball Growth curves are shared per style (not per Pom) + inspector preview

Follow-up to the same-day Ball Growth entry below. Per user: the balls-per-level data should belong to the **style**, not the Pom - every Pom with the same style must give the **identical** count at a given level (two Power Spikes Poms both give the same balls at level 5). A Pom now only *picks a style*; it authors no numbers. Also added an inspector preview so the level->balls table is visible.

- **`PomData`**: removed the per-Pom `PomBallGrowth` class (min/max/steps). Replaced with a single `BallGrowthStyle ballGrowthStyle` field + `BallGrowthStyle` property. The enum is unchanged.
- **`PomBallCount`**: the per-style curve (min/max/steps) now lives here as the **single shared source of truth** via `GetCurve(BallGrowthStyle)`; constants `MaxBallsCap = 25` and `MaxPomLevel = 50` moved here. `Evaluate` is now `(style, level)` and reads the global level range, so the table is keyed only on style+level. **Lucky Chaos is now seeded by level only (not species id)** so every Lucky Chaos Pom shares one table. Public API kept (`GetBallCountForLevel(PomData,int)`, `GetCurrentBallCount`) plus new `GetBallCountForLevel(BallGrowthStyle,int)` for the preview - `BattleManager` / `BattlePomCardView` untouched.
- **`PomDataEditor`** (`Editor/Pawchinko/Inspectors/`, new): custom inspector that draws a read-only "Ball Growth Preview" - a level->balls table (Lv 1,5,...,50) with bars - under the default fields, computed from `PomBallCount` so it matches battle exactly. States that the curve is shared by all Poms of that style.
- **`BuildTestPomRoster`**: seeds now carry only a `BallGrowthStyle`; dropped the per-seed min/max/steps and the `BallGrowthData` struct. Re-ran it to migrate the 5 creature assets to the single `ballGrowthStyle` field.
- **Docs**: `PAWCHINKO_DESIGN_GUIDE.md` §8 updated to state the Pom picks only a style and the curve is shared + defined in `PomBallCount.GetCurve`.

Current shared curves (`PomBallCount.GetCurve`, tweak here to retune game-wide; all <= 25 at L50): Steady Paws 6 flat; Power Spikes 1->16 (4 tiers); Growing Rush 2->16 linear; Late Bloomer 1->20 (t^2.5); Lucky Chaos 2->18 bounded. Verified via MCP: every authored Pom's count equals its style's count (pom==style true), highest L50 = 20.

Files changed:
- `Assets/Scripts/Data/Pom/PomData.cs` (style-only field)
- `Assets/Scripts/Gameplay/Pom/PomBallCount.cs` (shared per-style curves)
- `Assets/Editor/Pawchinko/Inspectors/PomDataEditor.cs` (new preview)
- `Assets/Editor/Pawchinko/Tools/BuildTestPomRoster.cs` (style-only seeds)
- `Assets/Data/Pom/Creatures/Pom_*.asset` (migrated to ballGrowthStyle)
- `Assets/Docs~/Desgin/PAWCHINKO_DESIGN_GUIDE.md` (§8)
- `Assets/Docs~/DEV_LOG.md` (this entry)

### 2026-06-20 - Cursor agent (Claude Opus 4.8) - Ball Growth styles (replaces level-band ball counts)

Replaced the old "inclusive level band -> ball count" authoring with a single **Ball Growth** profile per species: a `BallGrowthStyle` plus a min (level 1) and max (max level) count. The style shapes the curve between them. Player-facing names are locked in for a future UI (none yet). Game-wide rule enforced in code: **no Pom drops more than 25 balls at any level**.

Styles (programmer name / player-facing name):
- **Fixed / "Steady Paws"** - constant (uses min).
- **Tiered-Step / "Power Spikes"** - flat, then jumps in N tiers (`steps`).
- **Linear / "Growing Rush"** - even min->max.
- **Curve / "Late Bloomer"** - slow early, ramps near max (t^2.5).
- **Random Range / "Lucky Chaos"** - bounces within a level-scaled band, but **deterministic per (species, level)** via a stable FNV-1a hash of the species id - never re-rolled at runtime, so a given Pom always shows the same count at the same level. (User's "not random but data is same".)

- **`PomData`** (`Data/Pom/`): removed `baseBallCount` + `ballCountLevelBands` (+ the `PomBallCountLevelBand` class). Added `BallGrowthStyle` enum + `[Serializable] PomBallGrowth` (style, minBalls, maxBalls, steps; const `MaxBallsCap = 25`) and a `ballGrowth` field / `BallGrowth` property. Old serialized fields drop off existing assets on next save.
- **`PomBallCount`** (`Gameplay/Pom/`): rewritten from a band lookup into a style evaluator. Same public API (`GetBallCountForLevel`, `GetCurrentBallCount`) so `BattleManager` + `BattlePomCardView` call sites are untouched; added `Evaluate(growth, idSeed, level, maxLevel)`. Every result clamped to [0, 25].
- **`BuildTestPomRoster`** (editor tool): each test species now authors a distinct style so all five are represented - Zen Sloth=Steady Paws(4), Coin Hoarder=Growing Rush(2->14), Mirage Fox=Power Spikes(1->12), Clover Cat=Lucky Chaos(2->16), Glitch Pug=Late Bloomer(1->20). Dropped the `DefaultBands()` helper; added `WriteBallGrowth`.
- **Assets** (re-authored + verified via Unity MCP, no scene changes): the 5 creature assets in `Assets/Data/Pom/Creatures/`. Verified counts at L1/10/25/40/50; highest at L50 across the roster = 20 (<= 25 cap). Each style behaves as intended (constant / linear / flat-then-spike / late ramp / bounded bounce).
- **Docs**: `PAWCHINKO_DESIGN_GUIDE.md` §8 (Ball Profile -> "Ball growth" with the five styles + 25 cap) and §11 (replaced the "TBD ball-count scaling formula" line).

Files added/changed:
- `Assets/Scripts/Data/Pom/PomData.cs` (growth enum + config, dropped band fields)
- `Assets/Scripts/Gameplay/Pom/PomBallCount.cs` (style evaluator)
- `Assets/Editor/Pawchinko/Tools/BuildTestPomRoster.cs` (per-style seed data)
- `Assets/Data/Pom/Creatures/Pom_{ZenSloth,CoinHoarder,MirageFox,CloverCat,GlitchPug}.asset` (re-authored growth)
- `Assets/Docs~/Desgin/PAWCHINKO_DESIGN_GUIDE.md` (§8 + §11)
- `Assets/Docs~/DEV_LOG.md` (this entry)

Note: only `minBalls`, `maxBalls`, `steps` (+ style) are authored data; the curve exponent (2.5) and Lucky Chaos band width (35%) are tuning constants in `PomBallCount`. Re-run `Pawchinko/Build Test Pom Roster (5v5)` to reproduce the test data from scratch.

### 2026-06-18 - Cursor agent (Claude Opus 4.8) - Per-type ball prefabs (type from Pom, own visuals + PhysicsMaterial)

Balls now inherit a real <see cref="PomType"/> from the Pom that drops them and look/feel different per type. A single-type Pom only spawns its type; a dual-type Pom rolls a fresh 50/50 between its primary and secondary for every ball. Player and enemy share the exact same balls - the type, not the side, drives the visual + physics, so the old per-side material override is gone.

- **`PomBallType`** (`Gameplay/Pom/`, new): tiny stateless helper (mirrors `PomBallCount`). `Roll(PomData/PomInstance)` -> primary type for single-type Poms, 50/50 primary/secondary for dual-type.
- **`BallLibrary`** (`Data/`, new ScriptableObject): maps each `PomType` -> a ball prefab, plus a fallback. One shared asset (`Assets/Data/Ball/BallLibrary.asset`) referenced by both spawners.
- **`Ball.Init`**: now takes the resolved `PomType` explicitly (was always `sourcePom.PrimaryType`), so the ball's `Type` matches the per-type prefab it was instantiated from.
- **`BallSpawner`**: dropped the single `ballPrefab` + per-side `ballMaterialOverride`; added a `ballLibrary` ref. Rolls the type at `Enqueue` (fixed when queued) and picks the prefab via `BallLibrary.GetPrefab(type)` at spawn. Logs + skips cleanly if a type has no prefab and no fallback.
- **Assets** (built by the new `Pawchinko/Build Ball Visuals (per type)` menu tool, `Editor/Pawchinko/Tools/BuildBallVisuals.cs`, idempotent): for each of the 6 `PomType`s - a URP/Lit colour material (`VisualAssets/Materials/Ball/Types/`), a `PhysicsMaterial` with per-type bounciness/friction (`VisualAssets/Physics/Ball_<Type>_PhysMat.asset`), and a **Prefab Variant** of the base `Ball.prefab` (`VisualAssets/Prefabs/Battle/BallTypes/Ball_<Type>.prefab`) with the colour on the ball mesh and the PhysicsMaterial on the SphereCollider. Variants inherit all future base-ball edits (Rigidbody/script/label). Tuning lives in `BuildBallVisuals.Configs` - e.g. **Calm** bounciness `0.10` (dead) vs **Chaos** `0.55` (erratic).
- **Scene wiring** (via Unity MCP): set `ballLibrary` on `Boards/PlayerBoard/BallSpawner` and `Boards/EneamyBoard/BallSpawner` in `Battle.unity`; pruned the now-dead `ballMaterialOverride` instance override.

Default per-type look/feel (all tweakable in `BuildBallVisuals.Configs`, then re-run the menu item): Chaos = magenta / very bouncy; Calm = blue / dead + sticky; Greedy = gold / grounded; Trick = green / slippery; Lucky = pink; Wild = orange.

Files added/changed:
- `Assets/Scripts/Gameplay/Pom/PomBallType.cs` (new)
- `Assets/Scripts/Data/BallLibrary.cs` (new)
- `Assets/Scripts/Gameplay/Battle/Ball.cs` (Init takes explicit type)
- `Assets/Scripts/Gameplay/Battle/BallSpawner.cs` (library + type roll, dropped material override)
- `Assets/Editor/Pawchinko/Tools/BuildBallVisuals.cs` (new build tool)
- `Assets/VisualAssets/Materials/Ball/Types/Ball_<Type>_Mat.mat` x6 (new)
- `Assets/VisualAssets/Physics/Ball_<Type>_PhysMat.asset` x6 (new)
- `Assets/VisualAssets/Prefabs/Battle/BallTypes/Ball_<Type>.prefab` x6 (new)
- `Assets/Data/Ball/BallLibrary.asset` (new)
- `Assets/Scenes/Battle.unity` (both spawners wired to the library)
- `Assets/Docs~/DEV_LOG.md` (this entry)

Manual check (needs Play mode): drop balls and confirm each ball's colour/bounce matches its type. Dual-type 50/50 won't be visible until a Pom is authored with `hasSecondaryType = true` - the `BuildTestPomRoster` species are all single-type today.

### 2026-06-18 - Cursor agent (Claude Opus 4.8) - Balls are plain physics bodies (tilted board + Wall containment)

The board now has a physical back panel and an invisible front glass on the `Wall` layer, and the board itself is tilted. That makes both the old code-level Z-position lock and the scripted corrective forces wrong: a world-axis `FreezePositionZ` can't follow a tilted plane (balls dropped in a straight vertical line and clipped pegs), and the anti-stuck watchdog's `transform.position` writes shoved balls straight through the new colliders. Reverted the ball to a plain Unity physics body contained by real geometry.

- **`Ball.cs`**: removed `lateralGravity`, the position-based stall watchdog (`FixedUpdate`), `ApplyAntiStuckNudge` (impulse + torque + hard `transform.position` dislodge), and the `Body.sleepThreshold = 0` override. Kept only `Init`, the settle path (`HandleSlotEntered`), and the `Settled` event. The ball applies **zero** scripted forces now — gravity + collisions only.
- **`Ball.prefab`**: cleared the Rigidbody constraint mask (`m_Constraints: 8 -> 0`, i.e. dropped `FreezePositionZ`) so the ball can travel down the tilted plane; the back panel + front glass (`Wall` layer) keep it in the play plane instead. Removed the now-dead serialized fields (`lateralGravity`, `stuckDistanceThreshold`, `stuckTimeBeforeNudge`, `nudgeImpulse`, `maxNudgeImpulse`, `hardDislodgeAfterNudges`, `hardDislodgeOffset`).
- Supersedes the 2026-04-19 "Ball Z-axis lock" entry below, which added `FreezePositionZ` *because no walls existed yet*. They exist now, so the lock is gone.
- **Not touched**: `BallSpawner` still applies a one-shot spawn torque + small X/Z jitter for organic variety (standard plinko feel per [PHYSICS_DROP_GUIDE §6](Desgin/PHYSICS_DROP_GUIDE.md)), and `ApplyBoardImperfection` peg jitter stays as the natural anti-stuck mechanism.

Files changed:
- `Assets/Scripts/Gameplay/Battle/Ball.cs` (stripped to a plain physics ball)
- `Assets/VisualAssets/Prefabs/Battle/Ball.prefab` (constraints cleared, dead fields removed)
- `Assets/Docs~/DEV_LOG.md` (this entry)

### 2026-05-31 - Cursor agent (Claude Opus 4.7) - Live 3D Pom portraits in Battle cards

Battle HUD cards now render the species' 3D Pom prefab live via render-to-texture instead of a flat placeholder colour, satisfying the design rule that portraits are real 3D meshes, not 2D art ([PAWCHINKO_DESIGN_GUIDE §6](Desgin/PAWCHINKO_DESIGN_GUIDE.md#6-battle-scene-composition-critical)). The same `PomData.portraitPrefab` will feed the in-world Creature Stage later, so this work shrinks that future task.

- **Data**: added `portraitPrefab` (`GameObject`) + `PortraitPrefab` property to `PomData`. `PomInstance`, `PomFactory`, and `BattleManager` are unchanged — they only deal in data.
- **Asset pipeline**: new layer `PomPortrait` (slot 13) in `TagManager`; new folder `VisualAssets/Prefabs/Poms/`; built `Pom_1.prefab` from `VisualAssets/Models/Poms/Pom_1/FBX_Pom_01.fbx` with an `Animator` (no controller — adding clips later is asset-only) and material `M_Paw_Base_01.mat`; whole subtree forced onto the `PomPortrait` layer; prefab assigned to `Pom_GlitchPug.asset.portraitPrefab`. New menu item `Pawchinko/Build Pom Visuals (Pom_1 -> GlitchPug)` reproduces the prefab idempotently.
- **Portrait subsystem**: new `Assets/Scripts/UI/LegacyUI/PomPortraitSlot.cs` owns one `Camera` + `RenderTexture` + spawn anchor per card and pipes the RT into a `RawImage`; new `Assets/Scripts/UI/LegacyUI/PomPortraitStage.cs` holds 5 player + 5 enemy slots and exposes `BindPlayerSide` / `BindEnemySide` / `ClearAll`. Both are pure view — never touch `PomData` and never read input.
- **Card view**: `BattlePomCardView` gained a `RawImage portraitImage` field. `Bind` enables it, `Clear` disables it — the texture itself is wired at HUD-build time.
- **HUD wiring**: `BattleHud` gained a `PomPortraitStage portraitStage` ref (with `ValidateSerializedRefs` check) and now calls `portraitStage.BindPlayerSide` / `BindEnemySide` alongside `BindCards` in `RebindPlayerSide` / `RebindEnemySide`.
- **Builder**: `Pawchinko/Build Battle HUD` now (a) builds card portraits as `RawImage` instead of `Image`, (b) builds the off-screen `PomPortraitStage` (positioned at `y=-10000` so it cannot bleed into the world) with 10 slot cameras whose culling mask is `PomPortrait` only, (c) removes the `PomPortrait` layer bit from every other camera's culling mask in the Battle scene, (d) wires the per-slot RTs into the card `RawImage`s and the stage ref into `BattleHud`. Idempotent; aborts cleanly if the `PomPortrait` layer is missing.
- **Docs**: updated [PAWCHINKO_DESIGN_GUIDE §6 + §17](Desgin/PAWCHINKO_DESIGN_GUIDE.md) to make explicit that card portraits are live 3D renders sharing the species prefab with the Creature Stage; updated [AI_AGENT_CODE_GUIDE §12](Desgin/AI_AGENT_CODE_GUIDE.md#12-visualassets-conventions) with the new `VisualAssets/Prefabs/Poms/` convention and the rule that `PomData.portraitPrefab` is the single source of truth for a species' visual.

What's *not* in this entry (deliberate): the big in-world Creature Stage (next task — reuses the same prefab), additional species beyond `Pom_GlitchPug`, an `AnimatorController` for `Pom_1` (drops in later as asset-only change), and the root-cause hunt for the `EnsureBattleMapEnabled` brute-force in `BattleHud` (orthogonal to portraits).

### 2026-05-11 - Cursor agent - UI Toolkit (Boot) + UI agent skills

- **Boot UI / game version**: basic UI Toolkit panel for the version string, recommended folder layout (`Uxml/Components`, `Uss/Components`, `PanelSettings`), `PanelSettings` on the Boot `UIDocument`, `GameManager.GameVersion` (`0.0.1`), `UIView` + scene `UIDocument` manager (`Scripts/UI/`), version label wired from code.
- **Agent docs**: three skills in `Assets/.cursor/skills/` — `unity-ui-view-pattern`, `unity-ui-uss-pitfalls`, `unity-ui-uxml-structure` (mirror [UI-Notes](UI-Notes/README.md)); [UNITY_MCP_HELPER.md](UNITY_MCP_HELPER.md) Issue 15 — move/rename assets with `AssetDatabase.MoveAsset` so GUIDs/refs stay valid; troubleshoot skill table row + README “AI agent helpers” cross-links.

### 2026-04-28 - Cursor agent (GPT-5.5) - Boot / Overworld / Battle scene split

Implemented the scene architecture from [AI_AGENT_CODE_GUIDE Section 8](Desgin/AI_AGENT_CODE_GUIDE.md#8-scene-architecture) and promoted the checked-in Overworld player controller out of `Scripts/Temp`.

- **Scene flow**: added `EncounterTriggeredEvent`, `OverworldPausedEvent`, `OverworldResumedEvent`, and `SceneFlowManager`. `SceneFlowManager` is the only script that calls `SceneManager.LoadSceneAsync` / `UnloadSceneAsync`.
- **Boot**: added `Assets/Scenes/Boot.unity` with persistent `GameManager`, `GameEventSystem`, and `SceneFlowManager`; updated build settings to `Boot`, `Overworld`, `Battle`.
- **Overworld**: wired `OverworldManager` into `Assets/Scenes/Overworld.unity`; replaced `SimpleTopDownController` with `OverworldPlayerController` under `Gameplay/Overworld/`; removed the old `Scripts/Temp` script asset.
- **Battle**: created `Assets/Scenes/Battle.unity` from the existing battle scene content, removed Boot-only `GameManager` / `GameEventSystem`, and added `BattleSceneRoot` to preserve manager initialization order.
- **Docs**: updated this log and the scene-scoped manager description in `AI_AGENT_CODE_GUIDE.md`.

Verified via Unity MCP: `Boot.unity`, `Overworld.unity`, and `Battle.unity` each have the expected root boundaries, zero missing scripts, and the expected manager/component counts. Compile check succeeded after script import.

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
