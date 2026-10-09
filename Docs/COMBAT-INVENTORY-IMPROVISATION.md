# Combat inventory improvisation

Status: implemented, reviewed and verified, 8 October 2026. Follow-up to the [current item audit](ITEM-UTILITY-AUDIT-2026-10-08.md). The user wants ordinary finds to suggest intuitive ways to handle danger, change a fight or escape it. This phase develops existing obtainable items rather than adding a new damage tier.

## Player experience and design rules

The intended thought is: “I have this thing; its physical properties could help here.” A successful addition must change a real decision under pressure, be discoverable from its name/examine text and action menu, and obey the same local rules for friends and enemies. A resource spent on combat is no longer available for its civilian use. Effects need visible state, bounded lifetime, costs, refusal without payment, and save/load support.

This is an original design guided by the user's qualitative reference to Caves of Qud. No source parity or implementation comparison is claimed.

## Verification sweep and corrections

| Candidate or assumption | Current implementation | Design consequence |
|---|---|---|
| Frog oil already behaves like oil | It is a harvested, traded item with no oil action. Existing ground oil can cause a random slip and interacts with heat. | Add a finite grease-film use, reusing the existing terrain rules. Do not create an unlimited refillable pool. |
| Silver sand can simply erase an oil hazard | Sand is an existing repair/request material. Erasing oil would also silently erase its other hazards. | Add temporary grit for traction; retain oil, cold, fire and pool contact. |
| A smoke screen alone lets an engaged enemy lose the player | Smoke blocks initial AI acquisition, but KillGoal keeps following the hidden target's live position. Engaged ranged selection also lacks a shared sight gate. | A working screen requires last-seen pursuit and sight-gated targeting, with reacquisition and adjacency counter-checks. |
| Veilpuff is already a smoke item | The obtainable Cave crop throws cryo-mist gas, which freezes but does not obscure sight. | Preserve its cold drawback and add explicitly opaque mist with matching presentation. |
| Steam is cover | Current tile steam does not block sight. | Do not advertise a water-to-steam escape trick in this phase. |
| A timber door wedge would help against ordinary pursuit | Most hostile blueprints cannot open doors at all. Only selected runtime inhabitants currently receive that capability. | Defer wedging until humanoid door behavior is deliberately expanded; otherwise it would mostly duplicate closing the door. |
| An inventory action automatically spends a turn | InventoryUI has an explicit time-cost allowlist. | Cover native action dispatch and time cost, not merely service success. |

Key sources: [liquid vessel validation](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs), [AI pursuit](../Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs), [AI targeting](../Assets/Scripts/Gameplay/AI/AIHelpers.cs), [gas grenade impact](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs), [inventory UI](../Assets/Scripts/Presentation/UI/InventoryUI.cs). Exact implementation/evidence references will be recorded with the completed work.

## First coherent set

| Item | Proposed combat use | Cost and limitation | Example decision |
|---|---|---|---|
| Frog oil | Pour grease here or on an adjacent passable tile; eight-turn oil film. | One item and one action. Existing chance-based slipping, not guaranteed paralysis. The ground film can catch fire; it does not apply a heat-vulnerability coating to creatures. | Lay a risky patch in a pursuer's route or prepare a position before igniting it. |
| Silver sand | Scatter twelve-turn grit here or adjacent for firm footing on oil/ice. | One item and one action. Suppresses slips only; enemies can use the same footing. Does not cleanse dangerous liquids or extinguish fire. | Cross a slick escape route, or make a safe position while leaving the surrounding ground hazardous. |
| Veilpuff bladder | Existing thrown cold mist also makes a brief opaque screen. | Consumed on throwing; retained cold effects threaten allies too. Bodies/projectiles can cross. Cover must be between observer and target, and adjacent opponents remain dangerous. | Block distant targeting, change direction behind the screen, and let pursuers search where they last saw you. |

Existing sleep, confusion and stun gas crops remain valid alternatives. This phase does not invent duplicate payloads for them. Meat diversion, noise lures, darkness-based stealth and door wedges remain separate follow-ups because their present AI prerequisites are materially different.

## Implementation plan

1. Write RED tests against production inventory/action, movement, AI targeting and content routes. Record the failing run before implementing behavior.
2. Implement bounded grease/grit use on the existing obtainable supplies, with exact ownership, position, quantity, transaction and time checks. Integrate saved tile residue, native rendering and inspection. Reject redundant/stale/invalid actions without consuming items or time.
3. Implement opaque veil mist and finite last-seen pursuit. Refresh knowledge only when visible; approach the remembered location when hidden; stop searching after a bounded period. Keep ordinary reacquisition and faction hostility. Gate ranged target selection on sight, without making mist physically stop projectiles.
4. Verify useful outcomes as well as effects: grit actually prevents a forced slip; oil can still cause slip without grit; obscured enemies follow remembered positions and do not fire at hidden live coordinates; visible/adjacent counter-cases retain combat.
5. Run targeted native regression suites, save/load and adversarial checks. Add a reproducible live scenario for the item decisions and inspect presentation. Fix discovered issues, update this living document and the item catalog's follow-up note.

## Acceptance and counter-checks

- Real inventory actions spend exactly one unit and one turn on success. Failure, stale selection, insufficient material, invalid ownership, nested rollback and duplicate commands do not spend either.
- Oil uses the existing slip/heat rules. Grit prevents only slip and applies equally to the player, NPCs and enemies. Existing hazards remain. Expiry restores ordinary footing behavior.
- Existing saved supplies can access the new loose-material actions where possible; tile state round-trips through existing serialization.
- Mist blocks shared sight and targeting while remaining traversable. Targets moving out of sight do not leak coordinates into pursuit. Visible targets update knowledge; reacquisition resumes combat; adjacent opponents remain threatening.
- Target death, zone departure, unreachable remembered cells, finite search, multiple observers, special AI goals and save reconstruction cannot create permanent stuck turns or retain magical tracking.
- Examine/menu text explains what the physical item does and its meaningful risk. Visible ground/mist state matches mechanics.

## Implementation and review log

The three utilities, native inventory timing, grit/veil presentation, and bounded pursuit have been implemented after native RED. Native verification and final review are complete.

### In-phase review

- 🟡 Resolved: opaque cover alone left engaged enemies following hidden live coordinates. KillGoal now stores observed body contact and spends at most six actor actions searching; ranged aiming needs actual sight.
- 🟡 Resolved after dedicated native RED: older serialized veilpuff Parts had none of the new cloud fields. An exact blueprint/payload fallback supplies normal four-turn cover without altering other grenades or explicit custom configurations.
- 🟡 Resolved after native RED: lifecycle cleanup dereferenced a missing Brain, wounded hidden pursuers bypassed retreat, followers repeatedly joined unseen fights, and large-body visibility incorrectly licensed aiming at a different hidden limb. New tests cover each matching counter-case.
- 🟡 Resolved after existing regression RED: clearing goals during a committed melee windup must preserve the paid attack intent. Its fixed ray still resolves; mist does not retroactively erase an already committed attack.
- 🟡 Resolved after native RED: a grenade hitting a targetable solid door detonated inside its solid cell and produced no optical screen. Solid-object impacts now use the approach-side traversable cell, matching ordinary wall impacts; nonsolid creatures retain centered payloads.
- 🟡 Resolved: grit expiry over remaining oil needed a tile-change notification; otherwise displayed safe footing could outlive its actual effect.
- 🔵 Scope: six search **actor actions** and four optical **tile turns** are different clocks. The four-turn veil is a stationary burst; harmful cold gas disperses separately.
- ⚪ Deferred: timber wedges duplicate closing doors against most current enemies. Noise lures and darkness-based stealth need separate AI design. NPCs react to surfaces and cover, but do not autonomously choose the new inventory actions.

### How the items are found

- Silver sand: village merchants carry one in their finite initial repair stock. Sill at world (10,10), west of the preferred glade start, is one reachable source. This does not promise repeat restocking.
- Frog oil: harvest Reedfrog corpses in the Sodden (1–2 units, 80% chance) or MawToad corpses (2–4, 90%). These are live population routes around Sumphold; a particular corpse need not yield oil.
- Veilpuff bladder: harvest ripe cultivated veilpuff in ordinary shallow caves. The cave crop plan selects columns and rotates species across depths 1–5; candidate placements need connected ground and an incoming staircase. No guaranteed early source or merchant stock is claimed.

### Verification receipts so far

Raw native Unity receipts live in `Docs/Verification/CombatInventoryImprovisation/`. Initial RED (`red-native.json`) exercises missing gameplay/presentation. Compatibility RED isolates legacy Parts and null lifecycle handling. Review RED caught retreat; the initial door case was subsequently corrected because VillageDoor defaults open. That tool job reported an initialization timeout after recording test progress, so it is **not** a completed suite receipt. `pursuit-regression-red-native.json` is a completed 63/63 native run proving follower, large-body aiming and committed-melee regressions before their fixes. `door-discovery-red-native.json` completed 15/15: the corrected closed-door case failed at the missing paid-screen assertion, and four discovery/help cases failed before their fixes; all ten counter-cases passed. Final results are recorded below.

### Integration summary

```text
Carried FrogOil / SilverSand
  → inventory menu binds origin, target, item identity and quantity
  → existing transaction consumes one unit and writes one finite surface layer
  → successful native UI confirmation hands off one normal action
  → movement, AI navigation, inspection and rendered ground read the same layer

Thrown VeilpuffBladder
  → existing gas-grenade impact creates harmful cryo-mist
  → eligible open cells also receive four-turn optical veil
  → shared sight drives field of view, enemy acquisition and target selection
  → engaged pursuers retain only the last observed contact, with a finite search
```

Old saves need no item-Part migration for the two loose materials. Standard saved veilpuff payloads receive the exact legacy optical fallback and dynamic carried-item guidance. Existing old planted crops can retain old saved descriptions. NPCs benefit from or suffer the same surfaces and sight rules; autonomous NPC choice to spread supplies is not included.

The optical screen is **not invisibility**: adjacent endpoints remain visible, physical attacks/projectiles pass through, and a committed strike retains its original ray. The player must put the screen between themselves and a distant threat and use terrain/movement to remain out of view after the screen expires.

### Regression review corrections

- The first broad native run completed **1,497 tests: 1,495 passed, two failed**, with full raw XML in `regression-native.xml`. Both failures were old pathfinding fixtures that explicitly bypassed sight by injecting a KillGoal and expected it to follow a hidden target's live coordinates. The navigation algorithms are unchanged. These tests, plus an L-wall case passing via incidental wandering, were converted to direct known-waypoint navigation tests; learned/unknown hidden pursuit has separate paired coverage.
- An adversarial test asset had an invalid 33-character GUID. The import check found it; the metadata now has a fresh valid 32-hex GUID. Those 29 cases were absent from the first broad run and are included in the final run. Earlier results are not inflated to count them.
- The editor connector can label a job as an initialization timeout before native Unity begins it. Native `TestResults.xml` is retained as the authoritative completed-suite receipt; bridge status alone is not treated as a passing run.
- The existing Glowmaw one-shot proximity ambush remains possible within two tiles, even through cover; after dropping it uses ordinary Brain behavior. This matches the item's warning that nearby enemies remain dangerous. Specialized Furrowstalker hunting already uses its own bounded sight-based prey memory. Retreat behavior is not a stealth simulation.

### PlayMode review, first pass

The first isolated native demonstration (`Native/9b13a1a30a5c4134b23ae376e4c227f7/report.json`) passed both real inventory uses: one item and exactly one normal action each, rendered grease/grit, preserved underlying oil, real veil consumption/cold payload, remembered position and unchanged live campaign context. Two **harness assumptions** failed and were corrected without changing gameplay:

1. It inspected the opaque screen's hidden center instead of the visible near edge. The renderer correctly relinquished the hidden cells; the screenshot showed the near edge.
2. The probe stepped its observer into real cryo gas before checking six AI actions. That froze the actor, so blocked TakeTurn events did not spend search actions. The corrected bounded-search probe checks mist occlusion first, then prevents entry into harmful gas with its controlled wall boundary.

This first pass proves that the cold drawback is active even in the demonstration; it does not count as a fully passing native audit.

## Files changed

- `CombatUtilityActions`, inventory action collection/dispatch and `InventoryUI`: paid carried-item grease/grit verbs.
- `LiquidSlipSystem`, `TerrainNavigationWeight`: shared footing and risk behavior.
- `KillGoal`, `AIHelpers`, `CombatTacticsPart`, `FollowLeaderGoal`: bounded observed pursuit and actual-contact sight for aiming/assistance.
- `GasGrenadePart`, `ThrowItemCommand`, `LineTargeting`: optical payload, approach-side solid impact and gas/projectile distinction.
- `ZoneTileState`, `TileStateCatalog`, `CellStatusReadout`, `SpreadTransientSource`, `SpreadTransientVolumes`, `ZoneRenderer`: saved finite layers, expiry invalidation and native/fallback presentation.
- `Objects.json`: only FrogOil, VeilpuffBladder and VeilpuffCrop; `BiomeCrops.json`: only Veilpuff use guidance. Both edited surgically.
- `MaterialUseDescription`: existing supply and legacy saved-item guidance.
- Eight `CombatInventory*Tests` fixtures, corrected `CombatPathfindingTests`, `CombatInventoryNativePlayer` and `CombatInventoryNativeBatch`; all new C# files have unique valid `.meta` GUIDs.
- This living document and raw verification receipts; historical item-audit/catalog follow-up notices.

## Completed native PlayMode evidence

The revised isolated run **`798e2d9a7a2b4f448490ab7c99216bbb` completed all 13 required checks, zero failures, zero unexpected errors**. Its [raw report](Verification/CombatInventoryImprovisation/Native/798e2d9a7a2b4f448490ab7c99216bbb/report.json) includes six captures and exact observations. The launcher returned Unity to the original clean SampleScene; player saves and camera/input preferences were isolated or restored.

**Can verify (script-observable):** actual keyboard inventory selection for grease and grit, one unit consumed and one normal +10-tick action each, native submitted ground meshes, grit retaining underlying oil, real thrown veil payload, visible near-edge geometry with concealed center, six finite remembered-position search actions in the separately controlled probe, and preservation of the live player context around that probe.

**Cannot verify (visual / feel):** a normal novice discovering all three items, long-term balance, fun or optimal escape tactics. The thrown-veil command and detached AI probe are direct production calls, not a native-keyboard combat playthrough. The search probe holds the observer behind a controlled barrier and does not advance gas/cloud time. Gameplay geometry is real; the flat supplied test floor is not a newly authored biome.

**Visual inspection:** the silver-sand popup is readable and offers directional actions alongside Examine/Drop. The closer capture shows pale gritty ground beside the 3D player and the blue voxel mist edge on the other side; occluded center cells are not rendered. The close fixture framing crops parts of the surrounding HUD, so it is evidence of local effect presentation rather than a polished whole-screen composition. No new user camera preference is stored.

Reproduce with **Caves Of Ooo → Scenarios → UI → Combat Inventory Improvisation Native Audit** while Unity is outside Play and scene edits are saved. The launcher refuses dirty/untitled scenes and uses a temporary save root. It stops automatically; reports go under the verification directory with a new run ID.

## Final verification

**Native Unity EditMode: 1,526 / 1,526 passed, zero failures or skips**, including **142 new combat-inventory cases** and 1,384 surrounding regression cases. Three existing navigation fixtures were corrected as documented above. [Raw native XML](Verification/CombatInventoryImprovisation/final-native.xml) records the complete run (476.85 seconds). The standalone runner was not used for this pass.

**Native PlayMode: 13 / 13 required checks passed**, zero unexpected errors, six captures; see the explicit observable/visual/feel bounds above. The controlled native demonstration does not establish that every ordinary escape situation is balanced or that an NPC independently chooses these inventory actions.

**Content and change checks:** only FrogOil, VeilpuffBladder and VeilpuffCrop differ in parsed Objects.json; only Veilpuff guidance changes in BiomeCrops.json. All 11 new script metadata files have distinct valid 32-hex GUIDs. `git diff --check` is clean. [Source-validation receipt](Verification/CombatInventoryImprovisation/source-validation.json) records source hashes and test scope. No BitLocker/tinkering admission changes.

No unresolved high or medium finding remains within this feature's scope. Remaining tuning requires natural encounter playtesting, especially the four-turn screen and short grease/grit lifetimes; no blanket escape guarantee is claimed.
