# Standalone web migration

The browser edition runs the existing C# simulation, independently of Unity. Continue moving the native player-facing workflows into the phone interface. The user explicitly permits breaking earlier web-save compatibility; do not spend this phase on historical save conversion. New checkpoints must still save and restore correctly.

## Current phase: equipment, abilities and accurate aiming

Status: implemented and verified in release `0e3ec926d6c4379d` (8 November 2026).

Goal: make native combat preparation usable on phones: inspect every ability even during cooldown, manage ten assignments without consuming time, choose an actual equipment slot and review replacements, and aim with the native targeting geometry. Preserve portrait/landscape controls, free cancellation and fresh checkpoint restoration.

Milestones: (1) native details and slot management with explicit-empty assignment restoration; (2) body-slot choice and displacement review through the native planner/command; (3) mode-correct targeting, Vault landing/neutral review, separate rite readings and ground/body throwing; (4) real WASM touch checks, independent source review and publication.

| Source verified before implementation | Port decision / correction |
| --- | --- |
| `ActivatedAbilitiesPart.AssignAbilityToSlot` | Ten zero-based slots; occupied assignment displaces the old binding, never swaps. Unbound abilities remain usable. |
| `SaveSystem.LoadActivatedAbilitiesPart` | Current unconditional legacy migration rebinds intentionally empty saved slots. Only migrate absent bindings; retain explicit saved emptiness. |
| `AbilityDetailsBuilder.BuildForAbility` | Native details already imported. Replace its keyboard-shortcut line because browser digits remain movement controls. `IsUsable` checks cooldown only. |
| `EquipPlanner.Build`, `EquipCommand`, `EquipmentComparisonService` | Planner provides validity, exact claims and displacement pairs. Empty displacement lists do not imply validity. Resolve attached limb IDs strictly; missing IDs must not become automatic placement. Confirm revalidates current body/item/plan and executes the native transaction. |
| `InputHandler` targeting, `VaultTargeting.TryPlan` | Four modes. DirectionLine sends a unit heading and no TargetCell; SelfCentered sends neither target nor heading. Adjacent requires a non-self neighbor. Cursor sends the exact cell. Vault needs landing review and explicit neutral-attack acceptance with target identity recheck. |
| `RitePreviewBuilder`, `ThrowPreview` | Native read-only known-target previews exist; import the rite builder. Avoid FX-producing generic beam helpers. Direction guides are not promised damage areas. Ground throws already exist in the bridge; expose that choice. |

Readiness: 🟢 simulation, equipment planner, ability data and original rendering builders exist; 🟢 browser workflow/projection implemented; ⚪ no new combat balance, authored content, historical save conversion or Unity runtime work required. This ports Caves of Ooo semantics; no new Qud parity claim.

Review protocol: issue single-use server previews; reject stale view, changed anatomy, item ownership, source or Vault target before committing. Cancel and all preview/detail queries spend no time. Assignment and equipment changes are free but checkpoint-dirty. Pending UI choices are never serialized and clear on new/load/transition or unrelated commands. Native inventory callback transaction behavior is retained; this phase does not rewrite upstream callback atomicity.

Performance and diagnostics: build detailed DTOs only for open panels, never per animation frame. Reuse native command diagnostics; emit bounded browser-action Routed/Rejected records for new commands. Draw targeting only when input/state changes. No new update loop or universal trajectory simulation.

Verification plan: RED→GREEN native contracts for free reads/cancel, occupied slot replacement, empty-slot restoration, exact equipment choices and stale rejection, all targeting modes and neutral Vault; real Chromium/WebKit touch workflows in portrait and short landscape, then existing storage, travel and landscape regressions. Record source review and evidence below before publication.

### Implementation and review record

Implemented in three new bridge partials (`AbilityControls`, `Equipment`, `Targeting`) and the existing action/snapshot/lifecycle adapter. The HTML panels expose native details, slot assignments, body-part plans, single-use confirmations, rite readings and explicit throw intent. The canvas draws a bounded heading guide. `import-core.py` now imports the native rite reader and reproduces the one-line explicit-empty slot-load correction; reimport on 8 November reproduced all 955 imported C# files and provenance exactly.

| In-phase finding | Resolution / scope |
| --- | --- |
| 🟡 Directional clicks previously silently changed off-axis selections into diagonal shots. | The UI selects a named eight-way heading; the bridge requires a unit direction and omits TargetCell. Exact cell modes remain separate. |
| 🟡 A neutral Vault could attack directly without acceptance. | Preview the native plan, require explicit acceptance, and recheck target, landing, actor/source, ability and revision before executing. |
| 🟡 Explicitly empty assignments were rebound on load. | Preserve the saved ten-slot array; legacy backfill only when no slots were serialized. The all-empty roundtrip has an independent regression. |
| 🟡 Review found directional shots out of the map could consume Jet Blast cooldown/time. | Match the native neighboring-cell bounds guard, in bridge and UI. All four edges reject without an event or turn. |
| 🟡 UI review found old aim surviving a menu/new-game context, and free equipment/throw selection counted as dirty. | Reset target, guide and pointer when context changes. Classify actual selection as free and final assignment/equipment as dirty. |
| 🟡 Root review found closing nested slot/throw-style dialogs left a native panel open. | Close that panel too; browser regression proves subsequent gameplay is released without time passing. |
| 🔵 Presentation extension | Readable HTML review sheets and ten labeled slots. Numeric keys retain browser movement, so native keyboard-shortcut text is omitted. No general damage-area promise is made. |
| ⚪ Native inventory transaction boundaries | Execute the native EquipCommand after browser request-time revalidation. Extensible callback atomicity inside the upstream command is unchanged; no stronger guarantee is claimed. |
| 🧪 Limits | Physical iPhone lifecycle/performance and every late-game ability interaction remain unqualified. No Unity editor/player validation or historical web-save conversion is required for this standalone release. |

Final native evidence on 8 November: **54/54 contracts pass**. An isolated rerun of the exact pre-phase `9f14432` source with these same tests gives **39 pass / 15 fail**: all original 37 pass, and two rejection-only cases already pass; the other fifteen expose the absent/incorrect workflows. The recovered baseline and final logs/JSON are retained in `Verification/evidence/actions-*`. Storage/coordinator contracts pass **30/30**. Actual final-build regression suites pass **50 gameplay checks**, **63 landscape checks**, and **49 world-travel checks** in Chromium/WebKit. The new action-menu suite passes **48 checks across four Chromium/WebKit portrait/landscape cases**: ten slots, replacement/removal, explicit equipment, review/cancel, nested dialog close, direction changes, keyboard interruption, body/ground throwing, cooldown inspection and exact fresh-page restoration. All suites report zero browser errors. No physical iPhone claim follows from emulated viewports.

The initial directional test expected Ember Spit to succeed into empty ground; native source correctly rejects `no_target`. The final fixture uses the same creature behind an indestructible wall and then with that wall removed, checking free rejection versus real damage/cooldown and one turn. This was a test-precondition correction, not a combat behavior change.

## Previous release: world travel and cartography

Status: implemented and verified in release `1c2547bf70c6e172`.

Goal: leave the opening area using the native world map, identify the region and disclosed landmarks, descend into a selected area, and return with the same character and persistent world state. Preserve the published portrait/landscape controls and single-action touch behavior.

Scope:

1. Expose actual world-map entry/descent through a travel command and native stair fallback. Existing transition, turn and checkpoint paths remain authoritative.
2. Export explicit map mode, logical world coordinates, biome, local depth, disclosed map-cell labels and genuine visitation. Surface/cave names use authored places and biome context.
3. Render the embedded 20×20 map using approved native regional atlas backgrounds and disclosed symbols. Keep input coordinates in native zone space. Frame the map independently from local exploration, preserving local zoom when returning.
4. Make World map and Enter area reachable from the phone controls, desktop menu and keyboard. Reading maps/landmarks remains free; moving/travelling advances native time.
5. Verify real core contracts, actual WASM touch gameplay, map offset targeting, travel checkpoints and landscape regressions; publish to the existing private Site and update the existing GitHub branch.

## Source verification before implementation

| Reference | Verified behavior / correction |
| --- | --- |
| `WorldMapTraversal.TryWorldMapVertical` | Surface ascent and world-map descent; underground ascent requires stairs. Direct `Ascend` is more permissive, so use the guarded entry point. |
| Native `InputHandler.TryUseStairs` | Try ordinary stairs first, then world-map fallback; transition and end the player turn once. No hostile guard applies to this fallback. |
| `WorldMapTravelCostPart` | Successful map movement adds 10 clock ticks through `AfterMove`, in addition to the ordinary turn. The bridge must not add them again. |
| `WorldMap` | Logical 20×20 grid starts at native zone offset **(30,3)**, not (30,2). |
| `WorldMapZoneBuilder` | Live map-cell render names/glyphs hide undiscovered sinkholes. Cached zones are not proof of visitation. |
| `FlatPixelWorldMapCatalog` | Reuses six existing 16×24 atlases, darkened per biome, with native foreground symbols. Copy original assets, do not redraw them. |
| Existing `BrowserGame.EnterZone` | Already applies the native arrival hooks and actor registration; reuse it. Existing JS saves automatically on zone transitions. |

Readiness: simulation and authored map/art are present; browser routing, projection and presentation are missing. This is a port of existing Caves of Ooo behavior, with an HTML/canvas presentation; it makes no Qud parity claim.

## Performance and observability

Only project the 400 meaningful map cells while in world-map mode; avoid serializing its surrounding 1,600 border cells. Resolve the lower map-cell owner while scanning occupants, so the player sprite cannot hide its destination label. Keep images cached and redraw on snapshots, resize, zoom and pan; do not add an animation loop. Native worldmap traversal/movement emits its existing diagnostic records. Bridge refusals return a visible error and do not advance time.

## Verification and review record

- Native tests: original 27 pass; ten new contracts first failed RED against the old bridge, then all 37 pass. The first GREEN attempt exposed border-wall attacks consuming a turn. A logical map-bounds guard fixes all four borders. Rejections, hidden sinkhole names/color, read-only inspection/journey and fresh map restoration pass.
- Browser tests: final 49 real-WASM travel checks pass in Chromium/WebKit at both phone orientations, including all 400 cell-center hit tests and 568×320 landscape. An additional overlapping WebKit run confirms short-window aim/cancel preserves free time and cell access. Existing 50 browser regressions, 63 landscape checks and 30 storage/coordinator checks pass. Receipts are in `Verification/evidence/world-*`.
- Independent bridge review: no remaining actionable travel/disclosure/persistence defects found. Native stair auto-walk and Helmwood passage handling remain broader migration gaps.
- Unity editor/player validation is intentionally excluded: the user requested a standalone website game.

### In-phase review and divergences

- 🟡 Fixed: the native world-map zone is surrounded by ordinary Wall objects. The browser now rejects moves beyond the logical 20×20 map before the generic structure-attack path. This is a deliberate boundary correction, not a new travel rule or extra time charge.
- 🟡 Fixed from actual browser screenshot review: the portrait zoom toolbar could cover northeastern cells despite the whole map fitting geometrically. Map framing now reserves the toolbar's actual occupied band. A full 400-cell hit-test assertion was added after this visual finding; it was not a pre-implementation RED test.
- 🔵 Presentation extension: fitted overview, separate local/map zoom, HTML place guide and visited corner marks. Native authored textures, disclosed symbols, traversal and discovery semantics remain authoritative.
- 🧪 Physical iPhone performance/lifecycle and every regional gameplay interaction remain outside emulated browser evidence. No historical web-save conversion was added or required.

The five new image files are unmodified original blobs from `Assets/Resources/Sprites/Live16x24/` at source revision `e5a782a2e8d91354798b917e3aceed36b4c5f42f`; the existing Sill atlas supplies the sixth region. Their combined additional PNG payload is under 15 KB. Local regional terrain coverage is still separate work.

## Remaining migration gaps, source-audited

- Hauling feedback: the core can grab/release, but propagation can be mistaken for failure; show current load and speed penalty.
- Crafting-station `CraftToggle`/`CraftNoop` rows need browser routing; batch forge/brew presentation is incomplete.
- Status/hazard/lighting projection and regional local-terrain art coverage. Do not paint every region as Sill or invent hazard-free ground.
- Original campfire/harvested-resource and some cellar asset lookup states need corrections.

Existing single crafting, throwing, ordinary equipment and skills are already in the web edition. No native firearm system was found; inventing one is outside migration scope.
