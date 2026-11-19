# MACHINE V1 DOSSIER: Anchored capstan

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Authority: root `Lore/` only; `Docs/Lore/` is not used. All C#/JSON below is a sketch for an implementing engineer, not compiled code.

Evidence tags: **[V]** I read the cited line this session. **[R]** taken from `mw/*.md` research or `ARCHITECTURE-SPEC.md` and not re-read. **[I]** inference or design. **[U]** unverified; the named RED test or first step settles it.

Inputs read: `design/ARCHITECTURE-SPEC.md` (whole), `Docs/MACHINE-ROSTER-VISION.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md` (capstan rows and :160 correction), `Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md` (:95-107), `Docs/MACHINE-RIG-RELOCATION.md`, `Docs/SPREAD-CONNECTED-EXPLORATION-DESIGN.md` (heavy frame), `Docs/QUEST-FREE-HAULING-IMPLEMENTATION.md`, `Docs/DRAG-AND-HAUL.md`, `mw/lore.md` (M6, matrices, landmines, names), `mw/arch.md`, `mw/home.md`, `mw/presentation.md`, settlement dossiers (capstan rows), and the code named in section 1.

---

## 0. Verdict

1. **Tier: `wave3-needs-new-system`.** The capstan needs a pull motion, an anchor/terminus rule, a multi-round effort contract and a protected-subject marker that do not exist in code or in the architecture spec. It also lacks a qualified purpose (item 2). It depends on wave-2 contracts (S3 declared time, S4 station, S5 reader, S7 solid placement) but is not buildable from them alone.
2. **Purpose is NOT qualified today.** Every one of the pitch's three jobs (recover a machine onto transport, open a passage, recover something from a pit) lacks a shipped situation. Of the shipped haulable loads, the only ones above a starting character's hauling cap are the 150-weight MillStone, the 136-weight ConnectedHeavyFrame and (by a grip rule, not weight) the SmithAnvil. None has a consumer worth moving it for, all three gates erode with ordinary levelling (section 2), and the repo already forbids raising weights, impairing actors or editing starters to manufacture demand (`MACHINE-SYSTEMS-DESIGN.md:160`, `EXECUTION-PLAN.md:103`). I agree with that standing finding and add two new facts to it (the levelling arithmetic and the fact that the heavy frame's Strength gate is a deliberate build-identity feature).
3. **Do not use the capstan to satisfy "a machine in every town".** Every settlement dossier rates it "poor", or at best "plausible by theme, conditional, parked" (Ginmere, Olderdeep, Morrowfast), and none finds a heavy load or obstruction (section 6). A placed post with a verb and nothing to pull is a fake service. The spec's census is by place, so no town needs the capstan for coverage.
4. **V1 below is a conditional definition.** It is the smallest version that would be honest *if* gate G0 (a qualifying heavy objective) exists. It must not be built, placed or counted until G0 is met. The first deliverable is a decision record (K0), not code.
5. **What the spec lacks for this machine** (section 4): `ActionTime.Rounds(n)` (the capstan is the second consumer the spec says it waits for), a pull-motion service, a `ProtectedSubject` marker (a preserved body is haulable today and only a name list could exclude it), and a place for the machine kind in the census as "purpose-gated" rather than "pending".
6. **Most honest first objective (G0-R):** a heavy *reclamation* find that is worth more intact at a workshop than dismantled where it lies (the pitch's headline, and the active reclamation program). That needs machine 1's yield-differs-by-station work first, so the capstan should sit **after** machine 1 and S19 in any sequence.

---

## 1. Verified ground truth and premise corrections

### 1.1 What already ships toward the capstan (all [V] unless marked)

| # | Fact | Evidence |
|---|---|---|
| F1 | Hauling is complete and local: `DragPart`/`DraggedPart` symmetric grip, adjacent grab (`GrabReach = 1`), the load follows into the cell the hauler vacated, a blocked follow breaks the grip ("slips"), Speed penalty `weight * 4 / 10` clamped to a floor of 20. | `DragSystem.cs:122, 233, 237, 250, 373-392, 399` |
| F2 | The haul cap is `Strength x 8`; carry and haul share one ladder; `IsRooted` = "no `HandlingPart` and not takeable"; living things are refused; `MinLiftStrength` is a separate grip gate (`NotStrongEnough`). | `DragRules.cs:76, 82, 106, 122-144` |
| F3 | A load's weight already includes cargo: `HandlingService.GetWeight` returns the shell, or `ContainerLoadPart.Weight(shell)` (bounded traversal, overflow to `int.MaxValue` = unhaulable). Only one blueprint opts in today (`SoddenWorksLocker`, shell 40). | `HandlingService.cs:48-55`; `SecondExplorationObjects.cs:72-96`; `SecondExplorationSites.cs:106-111` |
| F4 | Every shipped haulable and weight (`HandlingPart.Weight`): MillStone 150, ConnectedHeavyFrame 136, SmithAnvil 120 (+ `MinLiftStrength 18`), AbandonedFieldWorkRig 120, StoneCoffer 110, SaltCuredBody 90, GleanersTimberPallet 90, HaulBarrel 75, FallenBeam 60. (A scripted scan of all 767 `Objects.json` blueprints for `Handling` parts finds exactly these nine with weight above 2; the rest are 2 or less, i.e. ordinary carryable items.) | `Objects.json:34221, 34301, 34381, 34461, 34541, 34094, 59327`; ConnectedHeavyFrame at :59195 region |
| F5 | The seven starting builds have Strength 16, 20, 16, 16, 18, 20, 18 (duelist, breaker, stormcaller, bombardier, warden, hurler, classic), so haul caps are 128, 160, 128, 128, 144, 160, 144. | `StartingBuilds.json:10, 34, 57, 80, 104, 127, 148`; `DragRules.cs:76` |
| F6 | Strength grows. Attribute points arrive at levels 3, 9, 15 (spent one at a time) and every attribute +1 at 6, 12, 18. | `AttributeAdvancement.cs:25, 27` |
| F7 | Heavy loads are placed in wilderness by `HaulablePropBuilder`: 350/1000 chance per zone, then one pick from the biome pool (Spread: HaulBarrel, FallenBeam, MillStone; Stump: MillStone, StoneCoffer; Beating/Desert: SaltCuredBody, StoneCoffer; Overwrit: SmithAnvil; Ruins: StoneCoffer, SmithAnvil, FallenBeam). It deliberately never plugs a one-cell gap, with a comment that the player "may not be strong enough" to remove it. | `HaulablePropBuilder.cs:30, 45-58, 94-97`; wilderness-only is [R] (`mw/settlement-gantry.md`, `OverworldZoneManager.cs:966-968`) |
| F8 | Optional hauling shortcuts exist: `SpreadExplorationHauling` (a hedge wall with a gap closed by a FallenBeam or HaulBarrel, detour 14-60 steps) and the `HeavyFrame` satellite family (a 12-cell stone wall with the 136-weight frame in the aperture, a cache of 2 FireClay and a movement manual behind it, detour always open). Census: 9 realized heavy-frame sites in 68 seeds. | `SpreadExplorationHauling.cs:13-20, 40-60`; `ConnectedSpreadSatellite.cs:45, 124`; `SPREAD-CONNECTED-EXPLORATION-DESIGN.md:185-189, 507` |
| F9 | The heavy frame's Strength gate is intentional design: "Breaker (Strength 20) and Classic (18) meet the existing drag limit; Strength 16 builds do not. All can use the detour." | `SPREAD-CONNECTED-EXPLORATION-DESIGN.md:187, 200, 338` |
| F10 | One bespoke multi-round effort path exists: `ExecuteRigCrossing` pays two ordinary rounds in the source zone **before** an atomic relocation, inside `ActionResolutionGuard`, then commits and stamps `Noise.Mark(Actor)`. It is rig-only (`FieldWorkRigRules.Payload`), plain ground only, cardinal border only. | `InputHandler.cs:1146-1182` (loop at :1165); `RigRelocation.cs:12, 39, 125` |
| F11 | The shipped installed-state-on-an-anchor precedent: `RopeShortcutPart` (a `SecondExplorationServicePart`) is added to a `RopeAnchor` at the Overworld.2.7.1/2.7.2 landing; verb `RigRopeShortcut` spends 2 `KnotflaxCord` once, in one transaction with a two-owner rollback, and installs real stair connections. `RopeAnchor` the blueprint is scenery only (Render, Physics non-solid, Examinable). | `SecondExplorationRoutes.cs:8-70` (spend at :37); `SecondExplorationSites.cs:98-105`; `Objects.json:37475` area |
| F12 | The shipped instance-attached service pattern: `SecondExplorationServicePart` supplies `Verbs`, `Current`, `Choices`, `Apply`, claim, report and reject-with-diag; `SecondExplorationActions.Special(item)` is the existing "not ours to touch" predicate (QuestItem, Unique, Essential, NoTrade, Owned, NoTake, QuestID/OwnerID properties). | `SecondExplorationActions.cs:27, 106-132` |
| F13 | `MovementSystem.ForceMoveTo` runs AfterMove and cell-entry events but not BeforeMove, is documented for "knockbacks, pulls", and `LiquidSlipSystem` explicitly ignores non-creatures ("items and props have no feet to lose"). So a pulled load does not slip: terrain effects on a pull must be authored by the pull rule itself. | `MovementSystem.cs:215-245`; `LiquidSlipSystem.cs:60` |
| F14 | `Noise.Mark(actor)` stamps the actor; only `HuntsBySoundPart` creatures (`PaleStalker` is the only blueprint with it) perceive noise, and "nothing that stood still". Moving, striking and casting mark noise; a stationary operator marks none unless the machine does. | `Noise.cs:6-30`; `MovementSystem.cs:116, 232`; blueprint scan |
| F15 | `drag` is a default-on diag category. | `Diag.cs:119-120` |
| F16 | Nothing named capstan or windlass exists in `Assets/Scripts`, `Assets/Resources` or `Assets/Tests` (grep returns no hit), so there is no name collision and nothing "already built toward it" beyond F1-F14. | grep this session |

### 1.2 Premise corrections (the research and docs contain these errors)

| # | Premise | Verdict |
|---|---|---|
| C1 | `Docs/CHUNK-ACTIVITY.md:151` lists the Fallen-cart waysite's overturned cart as "(haulable)". | **False at HEAD.** `OverturnedCart` has Physics (Solid) and Destructible (HP 12) only, no `HandlingPart`, so `DragRules.IsRooted` returns true and the verdict is `Rooted` (`Objects.json:41429` area; `Waysites.cs:186-189` adds nothing). The cart is therefore not a capstan target and not hauling content. |
| C2 | "Strength 16 builds cannot move the frame" is presented as a lasting gate. | **Transient.** A Strength-16 build reaches Strength 17 (cap 136) with its first attribute point at level 3 or the all-attribute bonus at level 6 (F6). The millstone (150) needs Strength 19: a Strength-18 start reaches it at level 6 with no point spent on Strength; a Strength-16 start at level 9 if it spends points on Strength. Pacing to levels 3/6/9 is [U] (not measured here). Any weight-gated purpose shrinks as the character grows. |
| C3 | `MACHINE-SYSTEMS-DESIGN.md:160`/`EXECUTION-PLAN.md:105` call the MillStone "placed clear of corridors". | **Narrower.** The builder only avoids plugging a one-cell gap (`HasOpenNeighbourOnBothSides`, `HaulablePropBuilder.cs:97`). Same conclusion (no forced obstruction); different mechanism. |
| C4 | Spec C4: "`ActionTime` rounds arrive with a second consumer". | The capstan *is* that consumer (F10 is the first). But F10 pays **before** commit and is outside the menu path; the world menu pays **after** a successful dispatch. The rounds contract must choose one semantic (section 3.5). |
| C5 | `mw/lore.md` M6 lists "Concord caravans and loading courts" as owners of a windlass. | The Concord *logistics identity* is canon (`Lore/Factions/04_SaccharineConcord.md:99`, "The Concord's power IS its logistics"; `08_MaterialCulture.md:143-146` "portable, weighed, priced"). No canon file names a windlass, capstan, winch, hoist or pulley (grep of `Lore/` this session: only two unrelated "haul" hits). Every name below is a draft, not a canon term. |

---

## 2. Purpose qualification (principle 1 of `SYSTEMS-DESIGN-PRINCIPLES.md`)

### 2.1 Who is blocked by weight on each shipped load, and for how long

Cap = Strength x 8. "Level" = earliest level a build can haul it by the arithmetic of F6 (assumes points spent on Strength; [U] for XP pacing).

| Load (weight) | Blocked starts | Gate disappears | Payoff for moving it |
|---|---|---|---|
| FallenBeam 60, HaulBarrel 75, SaltCuredBody 90, pallet 90, coffer 110, rig 120 | none (every start hauls them) | n/a | already ordinary hauling |
| SmithAnvil 120, `MinLiftStrength 18` (grip, not weight) | the three Strength-16 builds | level 6 (all +1) or level 3 with a point | none (communal anvil is not salvage, `EXECUTION-PLAN.md:105`); the Overwrit/Ruins wild anvil has no consumer |
| ConnectedHeavyFrame 136 | the three Strength-16 builds | level 3 (point) or 6 | shortcut to a 2-FireClay cache; the detour is always open and 14+ steps (F8); the gate is intended (F9) |
| MillStone 150 | all but the two Strength-20 builds | level 6 (Strength 18 starts), level 9 (Strength 16, points on Strength) | **none**: "Off its spindle it is just a very heavy circle" (`Objects.json:34221` area); no spindle exists |

A windlass that lets a Strength-16 build open the frame saves, once per crossing, a detour of 14 to 60 steps and costs a post, rigging and 2 rounds per cell. That is not a purpose; it also erodes the Breaker's deliberate niche (F9) without giving the owner anything the detour and a level-up do not.

### 2.2 The pitch's three jobs against shipped situations

| Pitch job | Shipped situation | Verdict |
|---|---|---|
| "Get it onto transport" (recover machinery) | No transport object (cart is rooted scenery, C1); no heavy recoverable machine exists (rig 120 is below every cap; the only reclamation finds are small items) | **absent** |
| "Open a passage" | Gates (`SpreadFieldGate`, wicket) are not loads; the only blocked passages are the optional hedge/frame shortcuts with an open detour; generation refuses to plug corridors (F7) | **absent** (and compulsory obstruction is forbidden by principle 7) |
| "Recover something from a pit" | No pit, chasm, shaft or sunk-item object exists (blueprint scan this session); sinkhole ledges are stairs; `RopeAnchor` is scenery with one optional rope shortcut | **absent** |
| Heavier-than-everyone loads | Heaviest shipped is 150; a Strength-20 build hauls 160, and Strength reaches at least 40 in test fixtures (`DragRulesTests.cs:28`) | **absent**; authoring a heavier load only to defeat hauling is "enlarging loads to manufacture demand" (`MACHINE-SYSTEMS-DESIGN.md:160`) |

### 2.3 What a legitimate purpose has to be (gate G0)

A purpose is qualified only if all of the following hold. These are tests the owner can apply to any proposed objective:

1. The load is **valuable for a reason other than being heavy** (finite recoverable worth, a route, a designed staging place).
2. Moving it is **not obtainable by ordinary hauling at the character level that can reach the site**, for a reason other than a weight number chosen to exclude builds. Valid reasons: **range** (the load is beyond reach of a hauler's adjacent grip and the route is one a hauler should not or cannot walk, for example across a hazard), a **destination that is a fixed fixture** (a bench, a slip, a loading court), or a weight derived from the object's real composition and cargo (F3), not from a stat gate.
3. At least two other approaches stay viable (leave it, another route, dismantle or break where the load supports it, level up, help where supported).
4. The terminus is a **designed place** (see 3.3: a windlass only pulls toward itself, so what it defines is a "bring it here" spot).
5. No compulsion: declining costs nothing and no progression lock sits behind it.
6. Canon-safe (section 3.11).

**Two candidate shapes that could meet G0** (design proposals for the owner; neither exists):

- **G0-R (recommended): heavy reclamation find.** A finite wreck or assembly whose *intact* worth is realised only at a workshop (the field work rig or the Cinderhold forge), versus a cold on-site dismantle that yields less (the existing `GleanersTimberPallet` "dismantle for timber (2)" is the shipped shape of the on-site option). The windlass post stands at the workshop and brings the find in. This is the pitch's headline and the roadmap's own ordering (`MACHINE-SYSTEMS-DESIGN.md:116`: "Introduce a capstan through an actual heavy recovery objective"). It needs machine 1's "what survives differs by where you work it" first, which is not shipped (`mw/settlement-cinderhold.md`, M3a/b/c accept only pack-and-offcut processing).
- **G0-G (geometry): a stranded load.** A valuable haulable resting where walking the route costs real exposure (an acid or tar seep from the liquid hazard system), so a straight line from a post across the hazard is the only low-exposure way in, with "wade in and take the exposure" and "leave it" as alternatives. This is the "pit" job realised with shipped liquid rules, but it is authored content and risks reading as a tool puzzle; use only with the owner's explicit approval and a real alternative.

If neither is wanted, the honest outcome is that the capstan stays parked and the roster is eleven machines for this wave.

---

## 3. V1 (conditional on G0): the windlass post

### 3.1 What it is

A **fixed, bedded, solid, non-takeable post with a hand-turned drum** (one Part, `CapstanPart`, on one blueprint) that pulls **one eligible load at a time, in a straight line, toward itself, one cell per use**. It stores nothing, burns nothing, wears nothing, and has no timer. The operator is always the player in V1.

V1 deliberately omits: a portable kit, anchoring to arbitrary scenery, bent routes, pushing away, vertical pulls, doors/gates as loads, other zones, NPC operators, fees, and pooled help. Each omission is recorded as an unsupported-state limit in the reader (3.6) and in section 3.14.

### 3.2 Canon-safe names (all drafts; none passed the blind voice gate: R10, L17)

| Register | Name |
|---|---|
| Surface villages and general | **windlass post** (player-facing default) |
| Concord | caravan windlass (the "weighed, standardised" idiom suits a rated post) |
| Existing shipped words to stay close to | "ditchkeepers' hoist frame", "hoist well", "landing post", "rope anchor" (`Objects.json`) |
| Tent-Right (V2 only, portable) | pole-and-line, offered as a gift, never priced (R7, L3) |
| Morrowfast (local non-canon proposal, not canon) | drag-post; draft line from `lore.md` M6: "Anchor first. Then we talk about the weight." |
| Catacomb / Choir / Bower | none: forbidden register (3.11) |

Internal class and blueprint names (permanent once saved, spec section 3): `CapstanPart` in namespace `CavesOfOoo.Core`, blueprint `WindlassPost`. No collision (F16). "Capstan" stays the internal and roadmap word because the user's vision uses it; the player-facing word is "windlass post".

### 3.3 Verbs, exactly

One new verb on the post, offered once per eligible load in view:

- **`HaulInLine|<loadId>`**, label drafted as "haul in the <load> (N cells, R rounds a cell)". Executes **one cell** of pull per use: the load moves one cell along the ray toward the post, and R rounds are paid. The player repeats the verb to continue. Progress lives in the load's position, not in a job object.

No second verb in V1. Existing verbs are untouched: Haul and LetGo (`HandlingPart.cs:62-82`), Take, Throw, Examine. There is no "release" verb because nothing remains attached after a step.

**Why one cell per use** [I]: it needs no long-running action, no job state to save, no mid-job reload case and no interruption protocol beyond "the player stops"; every use is a committed, coherent world state (the rig path was written for exactly that property: "it never leaves an unpaid transported pair to serialize", `InputHandler.cs:1145`). A "keep hauling until it arrives" loop is V2 and needs the interruption rules in 3.14.

Where it appears: the world action menu of the post when the operator is within 1 cell of the post (same reach as every shipped station: `StationQuery` reach 1, `GrabReach = 1`). The verb is on the post, not the load, because the operator stands at the post.

### 3.4 Rules (pure; mirror `DragRules`)

Let `own = DragRules.MaxDragWeight(operator)` (Strength x 8, F2), `W = HandlingService.GetWeight(load)` (shell plus cargo, F3), `post.MaxRoundsPerCell` (draft 3), `post.LineReach` (draft 8).

- **Rounds per cell** `R = max(1, ceil(W / own))`. Refuse if `own == 0` or `R > MaxRoundsPerCell`. This is work conservation: more force needed than you have means proportionally more rounds for the same cell. It makes weight, Strength and cargo all matter (the roadmap's first evidence: "existing cargo weight affect pulling").
- **Eligible load** (entity level, new `LineHaulRules`, beside `DragRules`): has a `HandlingPart` (opt-in, F2), non-living, not `IsRooted`, not carry-able ("take it instead", mirrors `CarryInstead`), no `SpatialFootprintPart` (V1 limit, as `SpreadExplorationHauling.EligibleLoad` and `RigRelocation`), not already `DraggedPart`, passes `SecondExplorationActions.Special` inverted (not quest, unique, essential, notrade, owned, notake, no quest or owner property), not under a gathering claim (`LocalGatheringClaims.WarningFor`, [U] exact semantics, conservative refusal), **and not a protected subject** (3.11; needs the missing marker).
- **`MinLiftStrength` is honoured** in V1 (the anvil still refuses a Strength-16 operator with the existing message). Rationale [I]: "nowhere to hold it" is a grip fact the line does not obviously solve, and relaxing it would change what the communal anvil means to every build. Owner decision O4.
- **Route**: the load must lie exactly on one of the post's 8 rays (cardinal or diagonal), within `LineReach`; every cell between load and post must satisfy `!cell.BlocksMovement(load)` (the same predicate the follow rule uses, `DragSystem.cs:392`) and contain no creature and **no hazard** (liquid pool, trap, fire; V1 limit "ordinary open ground only", recorded as an implementation limit not a design rule, per `MACHINE-SYSTEMS-DESIGN.md:35` "Narrow pilot admission ... record it as an implementation limit, not the permanent design"). The **terminus** is the cell adjacent to the post on the ray; if the operator or anything else stands there, the pull is refused ("someone is in the way"). If the load is already adjacent there is nothing to pull.
- **Terminus must not seal the post or a site route** [I]: a windlass only pulls *toward* itself, so a weak operator cannot move the load away again (3.3 consequence). The pull is refused if the terminus would remove the operator's only reachable standing cell or break connectivity the site was authored to preserve (uses `Geometry.PreservesRoutes`, [R] spec C3 step 5; [U] whether it can be called outside generation: K1 RED).
- **Posts that are not ready** offer nothing: destroyed or Gone, being hauled (a post has no `HandlingPart`, so cannot be), or (later) faulted via `StationReadiness`.

### 3.5 Cost, time and what the preview shows

- **Cost**: `R` scheduler rounds per cell, **paid after the committed step** (the world-menu semantic; spec C4 says rounds arrive with a second consumer, and this is it). Refusals before commit cost nothing. No fee, no fuel, no cord, no wear. A consumable would be a toll (`MACHINE-RIG-RELOCATION.md:9`: "no fuel, cord, repair or maintenance toll"); the only finite inputs are **time** (rounds) and **position** (cells of load displacement).
- **Noise**: the operator is stamped `Noise.Mark` every round (F14). This is a real, verified consequence: eyeless sound hunters (`PaleStalker`) hear the operator; a silent stationary player would otherwise be invisible to them.
- **Preview** (the row label is the quote, as `RigRopeShortcut` and `LayCordSnare` do): load name, cells to the post, rounds per cell, and one honest clause when applicable: "you could haul this yourself" (when `DragRules.CanDrag == Ok`), "heavier than you can haul unaided" (R > 1), "the line will not bear it (needs R rounds a cell; the post manages M)" as a **disabled row** with its reason (spec S3 `AddDisabled`), "the way is blocked N cells along", "someone is in the way". Refusal text names a reason class and a cell distance only, never an unseen owner or hazard name (RigRelocation precedent: no hidden-detail disclosure). Only loads in visible, explored cells are listed.
- **Reader** (`x`/F1 via spec S5 `IDescribeState`, draft text, voice gate pending): State "A post bedded in the ground, with a drum and a long line. It reaches 8 cells." Uses "Hauls one load at a time, in a straight line, toward the post. It bears up to 3 times what you could haul yourself." Limits "It pulls toward itself and never away. It will not move anything alive, owned or set in a floor, nor anything across wet or fouled ground. Turning the drum is loud."

### 3.6 Blocked, failed, interrupted, saved

| State | Behaviour | Diag (category `drag`, default-on) |
|---|---|---|
| Refused before commit | nothing changes, no turn, log line, menu stays | `PullRefused { reason, post, load }` with a stable reason code |
| `ForceMoveTo` fails after checks pass | load stays, no rounds paid, log "The line goes slack." | `PullJammed { post, load, cell }` |
| Operator hurt, killed, action-blocked, or leaves the post between uses | the next use re-validates from the world; nothing is half-done because each step is atomic | none extra |
| Load destroyed or removed between menu open and select | stale command (bound to load id and cell, as `LayCordSnare|zone|ox|oy|x|y|count` binds) refuses | `PullRefused { reason: "stale-selection" }` |
| Post destroyed | offers nothing | none |
| Success | load moved one cell; R rounds paid; noise stamped each round | `Pulled { post, load, from, to, rounds, weight, operatorCapacity }` |
| Save/load at any time | the post's config fields and the load's position save with their entities; no attachment, job or progress is ever saved | none |
| Old save | an old save has no post; a fresh zone gets it by authored placement only (3.13) | none |

Saved surface: `CapstanPart { int LineReach; int MaxRoundsPerCell; }` only. No zone id, coordinates, operator id or load reference (spec S1 hygiene lint, hook B1; keeps it relocatable and base-ready). `FormatVersion` stays 8.

### 3.7 Alternatives the player keeps (including leaving it alone)

For any load a post could serve, all of the following stay available and are tested to still work:

1. **Leave it.** The load is optional scenery; declining costs nothing (generation never plugs a corridor, F7).
2. **Take the open route.** The optional shortcuts always keep their detour (F8).
3. **Haul it yourself** if `DragRules.CanDrag == Ok` (free, no post, with the existing Speed penalty). The levelling arithmetic (C2) means this becomes true for every shipped load.
4. **Dismantle or break it where the load allows**: pallet "dismantle for timber (2)", barrel HP 8, hedge HP 10. (Frame and millstone offer no yield; the frame's own text says so.)
5. **Vault a two-cell gap** with the movement manual behind the frame (`SPREAD-CONNECTED-EXPLORATION-DESIGN.md:187`).
6. **Help from others: not supported.** `DragRules` reads one actor's Strength; companions do not add to it. Stated honestly in the reader's limits and in the owner's open questions; do not imply it.

The windlass is the only one of these that works independent of the operator's Strength and from a distance, but it needs the post, rounds and noise. That is the real trade; no option is dominated on every axis **except** that for the existing frame and millstone, level-up and the detour dominate it (section 2): another reason V1 needs a G0 objective.

### 3.8 Consequence outside its own menu (verified mechanisms)

- **Route and AI**: a pulled load is `Solid`, so it opens or closes a gap for every creature's pathing, not just the player's (`BlocksMovement` is the shared predicate, F1). Moving the frame out of the aperture opens the same lane to hostile creatures; parking a load can close one. K1/K3 tests assert both directions with a real creature path.
- **Noise and exposure**: the operator is stationary for `R` rounds a cell with the world's scheduler running (monsters act) and is audible to eyeless hunters (F14).
- **Finite worth**: where G0-R applies, bringing the find to the bench changes what can be recovered from it (competing finite outcomes: on-site dismantle versus workshop recovery). No such yield difference exists yet.
- **Claims**: loads under a gathering claim are refused, so the machine cannot launder a claimed reserve across a site.

### 3.9 Canon constraints and landmines (cite `mw/lore.md` ids)

- **R1-R3 / L1**: never pull anything in `AuthoredWildernessZoneIDs` (`OverworldZoneManager.cs:771`, [R]): tenth fire, woven doll, Felling-Site (six Firsts), Overwrit, Sealed Library barriers. Use the existing exclusion list.
- **L4 Choir**: "Do not dig; everything here is somebody" (`Lore/Codex/11_ChoirGroveSign.md`). A bedded post is a dug anchor: **never in Grovelands groves, tendril fens, composting grounds or the Deepest Cathedral**; the post's placement validator rejects those zones (data list, R8).
- **L5 Pale Curation**: a preserved body must not be a machine-moved load. `SaltCuredBody` (weight 90, `HandlingPart`, haulable by every build, `Objects.json:34541`) is the one shipped case; nothing in the blueprint marks it protected, so exclusion today would be a name match. The spec must define a `ProtectedSubject` marker (4.4). Marrowstye's intake expects bodies hauled *by hand* across bays (`mw/settlement-marrowstye.md`); that is a Curation act and V1 does not automate it. `PreFellingBody` is already rooted (no `HandlingPart`).
- **L7 Bower**: moving an installation is vandalism; Posy has no capstan.
- **L8 Catacomb**: no plaque, niche, ancestral bone, Memory-Marble/Tepuibone/Mute-Stone wall or Founding Plume as load or anchor; catacomb villages exempt for installed machines by outsiders (Quiet's Door, Olderdeep, Lampwell, Spivenor, Cathedral in the spec's exemption data). `RopeAnchor`s at Olderdeep/Ginmere/Stillleaf/Cathedral descents are scenery and are **not** capstan anchors in V1 (a V2 anchor predicate must exclude them unless Recension/Curation review says otherwise; Olderdeep touches Mystery #2).
- **L3 / R7 Tent-Right**: never price a pole-and-line; never anchor to or meter `GuestClothPole`, the guest locker or the well (sacred fixtures, `lore.md` section 2). Any Tent-Right form is a gift.
- **R5 / L10**: the machine stores no work. Do not add a wound spring, banked tension or any charge to V1; stored work is spec S18 and canon-gated.
- **L9 / L16**: no "alloy", "motor", "winch engine" or power language; the device is a hand-turned simple machine.
- **R9 closure ledger**: V1 creates no standing arrangement (no rigged line, no commission), so no spoken-end verb is needed. A V2 rigged line or a base installation **does** create an undertaken act and needs the designed verb (spec hook 2).
- **R10 / L17**: every authored string (names, preview text, reader lines, any NPC mention) is a draft until it passes the blind voice gate. No shipped machine string has a recorded pass.

### 3.10 Operators

The player is the only operator in V1. NPCs do not turn the drum (spec S20, `AIStationKeeperPart`, is unshipped). Shipped roles that can explain or sell: **Tinker** (stock includes iron spikes and components, `mw/settlement-slip.md`), **PeatCutter**, **Warden**. Missing roles named by canon: **Caravan-Master** (Concord windlass), boat-builder. Local and non-canon: Morrowfast **Orrit Coilstitch** (rope mender) and the Stillcord rescue-line theme. Any NPC line is a `Mention` (spec S24), voice-gated.

### 3.11 Placement (and the census stance)

- **Not a town installation and not a census instrument.** The spec's `InstallationRegistry` is place-keyed; the capstan must not be added as a "machine for place X" to raise coverage. Record it as a *machine kind with zero placements, gated on G0*, in the dossier and the owner's list, not as `Pending` rows (which the census wants to ratchet to zero).
- **Where it would go if G0 exists**: wherever the G0 objective is authored, as an authored fixture of that site, fresh-only, not by the generic town builder. Thematically apt places (each rated "poor" today for lack of a load): Sumphold's LoadingFinger/HullTrestles aprons (`BoatFrame` is a pinned non-takeable decor owner), Tally's loading courts (Concord caravan windlass), Gantry's caravan yard, a reclamation workshop yard (Peat Works rig, Cinderhold forge), Morrowfast's rope shop (local, non-canon).
- **Spec fit**: the post is a **solid blueprint**, so its placement is the first real consumer of S7's solid-blueprint branch (`SolidTestBlueprintKeepsAStandingCellAndRoutes`). It carries `InstallationMarkerPart { RetainZone = false }` because it is stateless; but note a pulled load's new position is world state in a cached zone, and in a *relapse-capable village* regeneration would reset it, so a G0 site must be in a non-relapsing zone or the marker must retain (`ZoneRetained`).
- Old worlds: unvisited zones get it on first visit; visited zones never do (stated limit).

### 3.12 Pitch behaviours deferred, with reasons

| Pitch behaviour | Version | Why deferred |
|---|---|---|
| "Attach a cable to heavy machinery, wreckage, gates, or other supported objects" at any sound anchor | V2 | needs a property-grounded anchor predicate (solid, rooted, sound, not protected) and the protected-subject marker; the shipped precedent for rigging is cord-spending at an anchor (`RopeShortcutPart`, F11). Anchor eligibility must be property-based, not a marked pairing (`MACHINE-SYSTEMS-DESIGN.md:33`) |
| "Portable versions would handle smaller loads"; installed vs loose | V2 | `InstallablePart`/`IRebindable` (spec S19) and a finite carried kit placed by the `CordSnareActions` pattern (paid adjacent placement of a finite object); also the home/base bridge |
| "maneuver the load" (turning, pushing away, snatch blocks) | V2/V3 | V1 pulls straight toward itself and cannot undo; maneuvering needs a second anchor or block, bent routes and a re-anchor verb |
| "Open a passage" (gates, doors, jammed barriers) | V3 | gates are not loads; needs a weighted/jammable passage object and a canon list of passages that must stay closed |
| "Recover something from a pit" | V3 | no pit objects exist; needs a vertical-level concept and a destination |
| "Get it onto transport" | V3 | no transport object (cart is rooted, C1); needs a cart/sled load model and cross-zone cargo (only the rig crosses, F10) |
| Keep-hauling loop ("haul in until it arrives") | V2 | needs `Rounds` semantics, damage and hostile-in-view interruption (model on `RestSystem`, [U]) and a declared interruption contract |
| Ground effects (sticky or slippery) on the route | V2 | `Slippery` is consumed only for creatures (`LiquidSlipSystem.cs:60, 141`, `TerrainNavigationWeight.cs:43`) and `Sticky` only appears in readouts (`LiquidDefinition.cs:89`, `InventoryDecisionDetails.cs:162`); no hauling code reads either (`DragSystem.cs` has no ground modifier; `DRAG-AND-HAUL.md` section 5 only proposes it). V1 refuses hazard cells instead |
| Pooled help (companions add Strength) | V3 | `DragRules` reads one actor; party support needs its own design |
| NPC operators, terms and fees | V3 | spec S20 and S10 (gated on critique) |
| Cross-zone pull | V3 | `Zone.TryTransferHaulingPairTo` is reachable only through `RigRelocation`; generalising it is a save-integrity decision |

---

## 4. What the machine needs that the architecture spec lacks

| # | Need | Gap in spec | Proposal |
|---|---|---|---|
| N1 | Multi-round declared effort | `ActionTime { Unspecified, Free, OneAction }`; rounds "arrive with a second consumer" (C4) | Extend S3 with `Rounds(n)`, n in 1..4, **pay-after** semantics equal to n sequential `EndTurnAndProcess` calls, stopping early if the actor dies or is action-blocked; leave the rig's pay-before pre-commit path (F10) alone; a parity test pins both |
| N2 | Pull motion | no motion service | New `LineHaulRules` (pure) and `LineHaulSystem` (one-step execution) beside `DragRules`/`DragSystem`, using `ForceMoveTo` (F13) and the follow rule's `BlocksMovement(load)` predicate. Do **not** change `DragRules`/`HandlingPart` (ordinary convenience untouchable, R7) |
| N3 | Protected-subject marker | C10 forbids vocabulary but defines no marker; `lore.md` section 10 asks for `ProtectedSubject` | A data-driven tag or Part honoured by every machine adapter; first consumer here (SaltCuredBody) and every processing machine. Without it, V1 would carry a name list |
| N4 | Disabled rows with reasons | exists as S3 | needed for "will not bear it", "blocked N cells along" |
| N5 | Reader lines | exists as S5 | needed for State, Uses, Limits |
| N6 | Station-style receipt | S4 | optional for V1 (kind `"windlass"`, reach 1); legacy-equivalent path is the `SecondExplorationServicePart` shape (F12). Prefer S4 so access terms apply later |
| N7 | Solid-blueprint placement | S7 solid branch has only a test blueprint | first real consumer; rule: standing cell plus `PreservesRoutes` |
| N8 | Census class | place-keyed census; no machine-kind class | do not extend; keep capstan out of the census (3.11) |
| N9 | Installable and loose | S19 | for V2 only |
| N10 | Protected zones list for fixed anchors | exemptions are place-keyed | add machine-kind-keyed forbidden-zone data (Choir groves, fens, composting grounds, catacomb zones, `AuthoredWildernessZoneIDs`) to the post's placement validator (R8) |

---

## 5. Slice plan (RED-first; sized to the 5-minute isolated-clone cadence)

Nothing here is authorised by this dossier. The standing hold in `CLAUDE.md` ("No new runtime changes", with payload, equipped-input and save policies behind independent critique) applies. Slice letters are K to avoid clashing with the spec's S-numbers.

| Slice | Delivers | Needs | Cycles |
|---|---|---|---|
| **K0** | **Purpose decision record, no code.** Actual-play baseline for an ordinary Strength-16 and a Strength-20 start against a real heavy-frame site and a MillStone, recording steps saved versus detour, rounds a windlass would cost, and the level at which ordinary hauling takes over; the G0 choice (R or G) by the owner. Output `Docs/MACHINE-CAPSTAN-PURPOSE.md`. If no G0 is chosen, the capstan stays parked and stops here | owner | 0 (play) |
| **K1** | Pure `LineHaulRules`: legality, rounds, route and terminus. No world change | K0 yes; S1 | 1 |
| **K2** | `ActionTime.Rounds(n)` and the disabled-row copy needed by 3.5 | S3 | 1-2 (shared with S3) |
| **K3** | `CapstanPart`, blueprint `WindlassPost`, verb `HaulInLine`, one-cell step, noise, diag, reader | K1, K2, S4, S5 | 2-3 |
| **K4** | Authored placement at the G0 site (fresh-only), forbidden-zone validator, cached-save control | K3, S7 solid branch | 1-2 + play |
| **K5** | Actual-play acceptance: fresh world, cached control, F5, fresh Continue; Strength-16 and Strength-20 builds; each alternative in 3.7; the noise and route consequences | K4 | play |

### RED-first tests (names; each positive pairs with a counter-check; adversarial file where two or more taxonomy surfaces apply)

**K1 `LineHaulRulesTests` (pure; fixtures like `DragRulesTests.cs`):**
`RoundsPerCell_IsCeilOfWeightOverOwnCapacity` (Strength 16: 60, 75, 120, 128 give 1; 129, 136, 150 give 2); `ExactlyAtOwnCapacity_IsOneRound` with counter `OneOverCapacity_IsTwoRounds`; `RefusedWhenRoundsExceedPostRating` with the boundary counter at `MaxRoundsPerCell`; `ActorWithoutStrength_Refused`; `UsesContainerLoadWeight_NotShellOnly` (add and remove a cargo item and watch R change); `ShippedLoadsAtStarterStrengths_Table` (pins MillStone 150, frame 136, anvil 120/18 at Strength 16, 18, 20 against the blueprint data so any weight edit fails loudly); `HonoursMinLiftStrength_AnvilRefusedAt16`; `RefusesLiving`, `RefusesRooted`, `RefusesCarryableItem`, `RefusesFootprintLoad`, `RefusesLoadAlreadyDragged`, `RefusesWhenOperatorIsHauling`, `RefusesSpecialOwnedQuestUnique` (one case per tag and property), `RefusesProtectedSubject` (test-assembly Part standing in for N3), `RefusesClaimedReserve`; route: `LoadMustLieOnARay`, `BeyondLineReachRefused` (8 passes, 9 fails), `BlockedCellRefusedAndNamesOnlyDistance`, `CreatureInRouteRefused`, `HazardCellRefused` (liquid, trap, fire each), `AlreadyAdjacentNothingToPull`, `TerminusIsAdjacentCellAlongRay`, `OperatorOnTerminusRefused`, `TerminusRefusedWhenItSealsPostApproach`; disclosure: `RefusalTextNamesNoUnseenOwner`, `OnlyVisibleExploredLoadsListed`; golden: `DragRulesAndHaulRowsUnchanged_ForEveryShippedLoadAtEveryStarterStrength` (green before any code, proves ordinary hauling is untouched).

**K3 `LineHaulStepTests` and `LineHaulAdversarialTests`:**
`RowOnPost_PerEligibleVisibleLoad_WithCellsAndRounds`; `RowAbsentWhenOperatorNotWithinOne` (counter); `StepMovesLoadExactlyOneCellTowardPost_ChargesRoundsPerCell` (Strength 16 on the 136 frame pays 2, Strength 20 pays 1); `RefusalBeforeCommitChargesNothing`; `ForceMoveFailureLeavesLoadAndChargesNothing` (callback injection); `MarksOperatorNoiseEachRound` with counter `StationaryOperatorWithoutVerbMarksNone`; `PaleStalkerHuntsOperatorByNoise` (integration with `HuntsBySoundPart`); `PulledLoadOpensRouteForCreaturePath` and `ParkedLoadClosesRouteForCreaturePath` (real creature path, both directions); `InterruptionBetweenStepsLeavesLoadWhereItIsAndPostStateless`; `CapstanPartRoundTripsPublicFields`, `SaveWithoutPartLoadsAndIsNotAPost`, `PostRelocatedZoneSurvivesSessionRoundTrip`; `DiagPinned` (`Pulled`, `PullRefused` per reason code, `PullJammed`); adversarial: stale command after the load moved; duplicate selection; load destroyed or removed mid-flow; actor zone change; two posts and one load (deterministic by id); a load between two posts; null actor, zone, load; `int.MaxValue` cargo overflow refused, never wrapped; negative or zero Strength; `LineReach` of 0 or negative and `MaxRoundsPerCell` of 0 clamp or refuse; post Gone offers nothing.

**K4 `CapstanPostPlacementTests`:** fresh-only (cached zone untouched); deterministic by `StableIndex`, no rng use; standing cell and route preservation; forbidden-zone data rejected (Choir grove, fen, composting, catacomb, authored scene); `SecondExplorationPacketsUntouched`; ratchet `CapstanKindHasZeroPlacementsUntilG0Met`; `NotListedInCoverageCensus`.

**K5 actual play (not native):** build-pair walk-through with the Strength-16 and Strength-20 starts against the G0 site; the same site with each alternative; F5 and fresh Continue with the load in its new cell; cached-save control showing no post in an older save; evidence limits stated (CUA-only, Linux/Deck unexecuted, no packaged performance claim).

---

## 6. Settlement and faction fit (summary of the dossiers)

All 25 `mw/settlement-*.md` dossiers rate the capstan **poor**, except three rated "plausible by theme, conditional, parked" for the same missing reason (no heavy load): **Ginmere** (three `RopeAnchor`s, shelf terraces and the two-cord rope landing, `SecondExplorationSites.cs:98-105`; stairs and the rope shortcut already connect the levels), **Olderdeep** (three `RopeAnchor` pins; Mystery #2 chamber, so any machine there is fenced) and **Morrowfast** (Stillcord rescue-line fellowship; local non-canon proposal). Typical "poor" reasons: Sumphold (BoatFrame fixed, no heavy load), Tally (`Crate` has no `HandlingPart`), Tine (BoatFrame pinned decor, weir not an entity), Wellmeet (tents portable; haul builder wilderness-only), Slip (no obstruction, text-only ferryman), Last Counter (crates small), Gantry (no haulable generated), Marrowstye (yard built for hauling, but 90/110-weight cargo is hauled by every start), Posy (L7), Cinderhold (anvil communal, no pump rod or ore cart entities), Quillhold, Stillleaf and Cathedral (examine-only anchors). The shipped rope-anchor idiom exists only at sinkhole descents (Olderdeep, Ginmere, Stillleaf, Cathedral: `*CompositionPlan.cs` and `SinkholeDescentBuilder.cs:58`), which are catacomb, Choir or sealed-library zones where V1's placement rule forbids a bedded post (3.9, L4/L8); Morrowfast is only their staging village. Faction matrix (`lore.md` section 5): Concord O, Tent-Right O, surface villagers O, Stillcord O, Bower X, Choir X, catacomb T/X by place, Curation T, Recension T. Canon *permits* the idiom in several places; none supplies a load.

---

## 7. Home/base bridge (so the shared contracts do not block it)

- A base is where a windlass earns its keep (bring a heavy stocked container or a find to the bench). `CapstanPart` stores no zone id or coordinates, so it is relocatable and survives zone regeneration (spec hook 4).
- V2 portable form: a finite carried kit placed by the `CordSnareActions` pattern, with `InstallablePart { Mode = Portable }` and an `InstallationMarkerPart { Origin = "Player", RetainZone = true }`; the base zone is then retained by the single C2 predicate (spec hook 1). Anchoring is a property predicate, not a marked target.
- Ownership: the pull verb honours `StationAccess` once S9/S10 land; "Owner" access fails closed until the home design registers it.
- Closure ledger: a base windlass with a rigged line is an undertaken act; the home design must give it a spoken-end verb (R9).
- Heavy stocked containers (`ContainerLoadPart`, F3) are the one *player-driven* heavy load that needs no authored objective; base storage is the likeliest honest long-run consumer, but it depends on the home system and per-container weight rules.

---

## 8. Open questions for the owner (this dossier does not decide them)

1. **G0**: R (heavy reclamation find, needs machine 1 depth), G (stranded load across a hazard), or leave the capstan parked.
2. Is a weight gate acceptable as a purpose at all, given it dissolves with levelling (C2)? If not, only range, geometry and designed destinations qualify.
3. Should the post be Rating-bound (draft `MaxRoundsPerCell = 3`)? No shipped load triggers the refusal (3 x 128 = 384 > 150), so the refusal path is covered only by fixtures; accept that, or set a lower rating.
4. `MinLiftStrength` honoured (default) or relaxed for a line? Relaxing changes what the anvil means to every build.
5. May the windlass pull `SaltCuredBody`-class loads once a `ProtectedSubject` marker exists, or is it a permanent refusal (Curation owns the decision)?
6. One cell per use (default) or a keep-hauling loop (needs interruption rules)?
7. Is "windlass post" the player-facing name; who owns the blind gate for every string (L17)?
8. Companion help: should party members ever add to a haul? Not supported today; stated honestly.

---

## 9. Verification record

**Read this session [V]:** `ARCHITECTURE-SPEC.md` (all); `DragRules.cs`; `DragSystem.cs`; `HandlingPart.cs`; `HandlingService.cs` (weight and lift); `RigRelocation.cs`; `FieldWorkRigRules.cs`; `InputHandler.cs:1120-1200` (`ExecuteRigCrossing`, `EndTurnAndProcessChecked` head); `SpreadExplorationHauling.cs`; `ConnectedSpreadSatellite.cs` (head, specs, goods); `HaulablePropBuilder.cs`; `SecondExplorationRoutes.cs` (`RopeShortcutPart`); `SecondExplorationObjects.cs` (`ContainerLoadPart`); `SecondExplorationActions.cs` (helpers and base class); `SecondExplorationSites.cs:85-125`; `MovementSystem.cs:215-245`; `LiquidSlipSystem.cs:56-80`; `Noise.cs`; `AttributeAdvancement.cs`; `StartingBuild.cs`; `StartingBuilds.json`; `CordSnareActions.cs` (head); `Diag.cs:119`; `Objects.json` blueprints (HaulBarrel, MillStone, SmithAnvil, StoneCoffer, FallenBeam, SaltCuredBody, ConnectedHeavyFrame, GleanersTimberPallet, OverturnedCart, RopeAnchor, BoatHookPost, KnotflaxCord, RepairCordBundle, SpreadFieldGate, RepairRopeWell, Tree, Pillar, Hedge, PreFellingBody, Rubble, HollowLog, BoatFrame); a scripted scan of all 767 blueprints for `Handling` weights and for pit/shaft/hole/transport names; `Docs/MACHINE-ROSTER-VISION.md`, `MACHINE-SYSTEMS-DESIGN.md` (capstan rows, :37, :116, :160), `EXECUTION-PLAN.md:95-107`, `MACHINE-RIG-RELOCATION.md`, `SPREAD-CONNECTED-EXPLORATION-DESIGN.md:185-189, 507-520`, `QUEST-FREE-HAULING-IMPLEMENTATION.md` (head), `DRAG-AND-HAUL.md` (head), `HAULING-LIFECYCLE-PLAN.md` (head); `Lore/` greps for windlass, capstan, haul; `Lore/History/08_MaterialCulture.md:139-150`; `mw/lore.md` sections 0-11; settlement dossier capstan rows.

**Not read [U], each settled by the named test or step:** exact `WorldInteractionSystem` menu-gather path and `MenuShortcutMap` for disabled rows (S3 RED); `LocalGatheringClaims.WarningFor` semantics for haul (K1 `RefusesClaimedReserve`); whether `Geometry.PreservesRoutes` is callable outside generation (K1 terminus test); `RestSystem` interrupt API for the V2 loop; `OverworldZoneManager.cs:966-968` wilderness-only claim for the haul builder ([R]); levelling pace to levels 3, 6 and 9 (K0 measures it); `LiquidDefinition` `Sticky`/`Slippery` consumers (V2); whether `Cell.BlocksMovement(load)` ignores the operator in the terminus (K1 `OperatorOnTerminusRefused`).

No test, build, Unity run or git write was performed.
