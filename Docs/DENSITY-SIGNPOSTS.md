# Density follow-up — signposts with directions

**Status:** implemented; focused standalone RED/GREEN, counter-checks and the
native keyboard examination scenario with direct gameplay-scale image review
are complete. The unfiltered native regression suite passed **15,655/15,655**
with zero failures/skips (392.82 seconds). Receipt:
`Verification/DensityPhase1/Continuation/native-editmode-final.json`.
This follows the shipped density tranches and addresses the static-signpost gap
in `DENSITY-PHASE-1.md` §3.
**Classification:** CoO-original use of existing map data, not a Qud parity port.

## Player-visible contract

Examining a generated surface signpost shows the four nearest actual named
Village POIs, excluding the current world cell. Bearings and map-cell distances
are calculated from the sign's current surface zone. The same deterministic
Manhattan-distance/Y/X ordering and known-empty-village exclusion used by
regional conversations remain authoritative. Signs give directions only:
no promises of available work, surviving services, safety, a walkable road, or
an undiscovered settlement. Examination does not write travel notes, discover
map cells, generate destination zones, or mutate quests.

Detached signs, signs in underground or invalid zones, and stale graphs with a
replaced cached zone retain their ordinary flavor without fabricated directions.
A moved sign reads its actual current graph; no active-zone global or saved
coordinate string supplies authority.

## Verification sweep before implementation

| Premise | Observed source | Bounded consequence |
|---|---|---|
| Existing `RegionalGuidance.Build` works for a sign | It requires living player and supported living speaker, conversation identity, hostility eligibility, and exact attached graph | Factor only read-only destination enumeration; preserve all conversation gates unchanged |
| A new part can just handle the same Examine event | Inherited `ExaminablePart` handles `InventoryAction/Examine` first and stops propagation | Add a runtime description part and a narrow explicit hook owned by the root integration pass |
| Signpost prose can stay untouched | Current prose says the arrows point at no names | Change only Signpost's prose and part wiring surgically; compare parsed blueprint objects before/after |
| Reading directions requires the active settlement singleton | Entity ownership supplies `SpatialZone`; `WorldLocationContext.For` supplies its map manager | Query that exact cached graph, including after save restoration; never trust a stale or unrelated active zone |
| Guidance is an authored route or service guarantee | Existing guidance measures map-cell offsets between named surface Village POIs | Retain coordinates/directions only and suppress quest IDs on signpost output |
| A new part requires manual factory registration | `EntityFactory` scans concrete Part subclasses in the core assembly | Use the normal `RegionalSignpostPart` registration convention and a fresh `.meta` |
| Existing saves acquire newly authored parts | Save loading reconstructs the saved part stream | Existing saved signs keep their old static text; new signs and new games get the part; no migration in this pre-release slice |

Sources read: `CLAUDE.md`; `QUD-DENSITY-GAP-ANALYSIS.md` §6 Phase 1;
`DENSITY-PHASE-1.md` §§3/10; `REGIONAL-GUIDANCE.md`;
`RegionalGuidance.cs`; `WorldLocationContext.cs`; `WorldMap.cs`;
`ExaminablePart.cs`; factory/part registration and the actual world Examine
dispatch; `Lore/MYSTERY-LEDGER.md`. Directions name existing settlements, not
under-text sites or answers to protected lore questions.

## Implementation

1. Add tests against real factory signposts and the actual Examine event before
   production changes; verify RED with unaffected-context controls in an isolated
   standalone runner while Unity is reserved, then run native regressions.
2. Factor the existing map destination loop into a shared read-only helper.
   Preserve regional-conversation eligibility and quest availability behavior.
3. Add `RegionalSignpostPart` with runtime `GetDirectionsText()`, using actual
   entity/zone ownership and returning no text when context is invalid.
4. Add the root-owned Examine hook, and one surgical Signpost blueprint edit.
   Directions are computed only when explicitly examined, not every frame.
5. Run focused signpost, regional-guidance, examine and save regressions;
   independently review purity, save behavior and lore/scope claims. Native
   visual validation must inspect text at gameplay scale, not only log content.

The shared map query retains the conversation eligibility checks before it is
called. `BuildSignpost` instead obtains the sign's actual `SpatialZone`, requires
the manager's cached zone to be that same object, and asks for destinations with
work suppressed. `RegionalSignpostPart.GetDirectionsText()` formats those leads
only when the existing Examine description is requested:

```csharp
var leads = RegionalGuidance.BuildSignpost(ParentEntity);
if (leads.Count == 0) return string.Empty;
// "Carved directions:" followed by the existing coordinate/bearing format.
```

No destination list or coordinates are saved in the part. There is no new
command, acceptance/rejection path, turn cost, generation call or RNG draw:
invalid context simply contributes no extra description to ordinary Examine.
`ExaminablePart.BuildExamineLine()` owns presentation composition and appends the
directions before its other supported details.

| Scope choice | Reason |
|---|---|
| Reuse the existing regional destination query | Preserve town selection, map axes, tie ordering and known-empty filtering rather than introduce another travel contract |
| Suppress quest IDs for signs | A fixed object cannot promise available work or a living contact |
| No separate popup or journal mutation in this slice | Use the existing world Examine path; inventory UI improvements belong to the companion item-examine slice |
| Existing saved signs stay static | Their saved part stream is authoritative; no pre-release migration was requested |

## Tests and counter-checks

- Real factory signpost emits real nearest destinations; ordinary fixtures do
  not. Names, coordinates, direction axes, distance ordering and ties are pinned
  against map data without introducing new destinations.
- Surface/live context succeeds; detached, underground, malformed and replaced
  graphs suppress directions. Moving an existing sign switches its origin.
- Removing a destination suppresses it on next examine; current-place and
  non-village POIs never become outgoing directions. Known empty cached villages
  retain the existing exclusion, while an ungenerated village stays eligible.
- Cache count, discovery, player properties, and narrative state remain
  unchanged. Sign output contains no quest/work suggestion even when a nearby
  town has available work; conversations retain their existing behavior.
- Newly saved signs recover the same runtime description after normal graph
  reattachment. Old signs without the new part remain static rather than
  silently acquiring a migration.

## Evidence, review and implementation log

- Sweep and plan were written before Assets changes. The existing actual
  factory/event path produced the old flavor string on RED: 42 cases total,
  33 passed, 9 failed. All nine failures were the intended missing directions;
  eight other signpost controls and 25 existing Examine tests passed. Receipt:
  [signpost-red.json](Verification/DensityFollowup/signpost-red.json).
- After the implementation and root-owned Examine hook, the focused run passed
  76/76. Expanded save controls passed **91/91**: 17 signpost regression cases,
  20 dedicated signpost adversarial cases, 25 existing Examine cases, 14 density
  content cases, one whole-world graph roundtrip and 14 simple-part roundtrips.
  Receipt: [signpost-green.json](Verification/DensityFollowup/signpost-green.json).
  Runs used a private runner copy; shared selection files were not modified.
- The 20 adversarial cases cover malformed prefixed IDs, negative and upper-bound
  coordinates, all four map corners, non-surface addresses, legacy surface IDs,
  separate worlds with the same zone address, repeated destination names, cached
  graph replacement/restoration and independent sign instances. These were added
  after implementation as invariant probes; they are not represented as 20 new
  RED-to-GREEN bug discoveries.
- Parsed blueprint comparison found exactly one changed blueprint, `Signpost`.
  The edit preserves local JSON formatting and changes only its flavor and new
  part wiring. Receipt:
  [signpost-blueprint-diff.json](Verification/DensityFollowup/signpost-blueprint-diff.json).
- **Q1/Q2 — symmetry and consistency:** conversation eligibility, supported
  speaker identities, hostility rules, available-work checks and travel-note
  behavior remain unchanged. Only the existing map enumeration was extracted;
  signs share its destination semantics but never its actor/work requirements.
  The new part follows factory naming and normal saved-part reconstruction.
- **Q3 — counter-check completeness:** valid surface context is paired with
  detached, stale, unbound, underground and malformed context; moved signs use
  actual ownership despite a stale event parameter. Empty villages, non-villages,
  blank names and the current cell are excluded against eligible town controls.
  New-part save restoration is paired with detached restoration and old signs
  lacking the part. Available conversation work is paired with sign suppression.
- **🔵 Review correction — purity evidence:** Unity JSON does not capture the
  multidimensional map arrays or all private narrative state. The purity test
  therefore compares visited cells, POI identities and player properties directly,
  and compares actual narrative/storylet save bytes. This tightened the tests;
  it was not a newly discovered gameplay defect.
- **Q4 — doc/implementation:** no Qud parity, route safety, settlement discovery,
  guaranteed living services, quest availability or save migration is claimed.
  Cold-eye source review found no remaining production correctness findings.
- **🧪 Standalone limit:** these receipts prove factory wiring, actual Examine
  events, data-derived text, context guards and serialization under the standalone
  runner. Native UI rendering and readable text at gameplay scale are supported
  separately by the bounded scenario below. The existing `RegionalGuidanceTests` and
  `RegionalGuidanceAdversarialTests` need native UI/save fixtures and are queued
  with the root's native integration pass. No balance or performance result is
  claimed by these focused tests or the native UI scenario.

## Files changed

- `Assets/Scripts/Gameplay/Conversations/RegionalGuidance.cs`
- New `Assets/Scripts/Gameplay/Entities/RegionalSignpostPart.cs` + `.meta`
- Root-owned `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs` hook
- Surgical Signpost-only edit in `Assets/Resources/Content/Blueprints/Objects.json`
- New `Assets/Tests/EditMode/Gameplay/Storylets/DensitySignpostTests.cs` + `.meta`
- New `Assets/Tests/EditMode/Gameplay/Storylets/DensitySignpostAdversarialTests.cs` + `.meta`
- `Docs/Verification/DensityFollowup/signpost-{red,green,blueprint-diff}.json`
- New `Assets/Scripts/Scenarios/Custom/DensityExamineNativePlayer.cs` + `.meta`
- New `Assets/Editor/Scenarios/DensityExamineNativeBatch.cs` + `.meta`
- `Docs/Verification/DensityFollowup/native-examine-first*.json`, raw log excerpt
  and `NativeExamine/9c5fa9a75a35496d9b85716e6f5b7b28/` report and screenshots
- This document; the root integration owns parent status and native evidence
  references updated with the same implementation commit

## Native examination evidence plan and sweep

The next verification slice adds a finite developer scenario and isolated
launcher, separate from the lair/capture launcher. It supplies player-scale UI
evidence for this signpost change and the companion item-examine change.

| Verification premise | Sweep result and implementation boundary |
|---|---|
| Existing test events prove the native menu route | They prove data/composition only. The scenario must queue actual Input System keys through the boot menu, look/world action menu, inventory and announcement state machine |
| Finding the content naturally is required for a UI capture | This is an explicitly staged UI fixture in its own disposable new game: one real factory Signpost next to the player, and WarlordCleaver with Serrated tier 2 and IronshodBoots added to inventory. The tonic is the real designed starter stack. It makes no acquisition, encounter or loot-frequency claim |
| Adding a new tonic gives the scenario a separate inspectable object | `NewGameLoadout` already grants two HealingTonics and `InventoryPart.AddObject` merges a matching stack, consuming the incoming identity. Inspect the existing starter stack and assert its original unit count remains unchanged |
| Opening an item directly is the ordinary input path | `InventoryUI` starts on equipment. Use I, Tab, bounded row navigation, Enter, then the visible Examine action; reflection observes menu rows and state but never selects or executes |
| World sign text must open a modal | Existing world Examine writes the sidebar log. Use L and the world action menu, then ordinary = / - log scrolling to capture the full directions; no invented popup behavior |
| A long weapon necessarily overflows the announcement | `AnnouncementUI` wraps at 52 characters in a centered 45-row overlay. Capture the actual WarlordCleaver plus enhancement text; root separately checks overflow before changing shared UI |
| The previous capture helper is safe to copy | Reuse `DensityNativeScreenshot`; renderer owns its orientation/backend fixes. Do not duplicate or edit the capture implementation |
| A temporary scenario can use the player's current save | `NativeSaveIsolation` owns a tokenized temporary root and marker through Play teardown, restores prior root/preferences, and rejects concurrent owners. Reuse it; snapshot/restore scenes and bootstrap seed, restore the cloned input settings and owned keyboard, and unregister runtime saving on shutdown |

The driver will assert one visible Examine per staged item, expected live
description text, return to the same inventory row, no item consumption/HP/turn
change, and sign context purity. It will emit per-case diagnostics and a durable
report plus screenshots. A launcher watchdog bounds the run and requests normal
Play shutdown on failure. Native callback errors and backend console messages
remain separate evidence. Screenshots require direct visual inspection before
any readability claim. No game events will be fired through ad hoc editor code.

Sources read for this extension: `DensityPhase1BenchPlayer`,
`DensityPhase1BenchBatch`, `NativeSaveIsolation`, `ChunkGameplayNativeAudit`'s
existing material-examine path, `InventoryUI`, `InputHandler`,
`WorldActionMenuUI`, `AnnouncementUI`, `DensityNativeScreenshot`, and
`EnhancementSerrated`. This plan/sweep precedes the new scenario source files.

Prepared `DensityExamineNativePlayer.cs` and `DensityExamineNativeBatch.cs` with
fresh `.meta` files. The separate menu entry is
`Caves Of Ooo/Scenarios/World/Density Examine Native Audit`. The driver expects
16 native assertions, records full observed sign/item descriptions and captures
the actual menus, item modals and overlapping sidebar log portions. It preserves
the starter tonic stack and verifies its unit count, removes only its staged
equipment/sign on shutdown, and rejects developer-mode bootstrap. The launcher
reuses the owned-save isolation and scene restoration pattern; its watchdog is
three minutes.

## Native examination result and direct image review

Run `9c5fa9a75a35496d9b85716e6f5b7b28` passed **16/16** native assertions on its
first execution, in 7.15 seconds, with zero unexpected application-log callback
errors. The disposable new game began in `Overworld.2.6.0`. The recorded sign
description gave Morrowfast (3,6), Cinderhold (6,6), Posy (5,9) and Gantry (7,8),
with their actual offsets from (2,6). No destination was generated, discovered
or added to travel notes by examination.

All **seven original 1920×1080 screenshots** were opened directly and reviewed.
The sign's four directions are readable in the sidebar; this run needed only
the latest-log capture, although the harness supports ordinary log scrolling
with overlapping captures. Each item menu contains exactly one Examine action.
The WarlordCleaver's full authored prose, weapon values, damaging-hit attempt
and Serrated enhancement fit in its modal. IronshodBoots show AV +2, DV +0,
equipped Speed -5 and Feet. The real two-unit HealingTonic starter stack shows
its existing 4d6+4 healing and drink/throw behavior. Each modal has a readable
dismiss footer and returns to the same inventory row. Examination preserved
turn, HP, effects, inventory identities and the original tonic stack count.

The verified native editor file descriptor pointed to `Editor-prev.log`, not
the path advertised by the editor API. A retained 64,271-byte raw log excerpt
contains zero native backend messages, exception lines or compiler errors;
this is separate from the callback count. Pre/post snapshots compare equal
for clean scene setup, save root, requested seed, background setting, complete
Input System settings and keyboard IDs, with preferences preserved. The owned
temporary save root was removed and isolation was inactive after Play teardown.

Evidence: [native result and image review](Verification/DensityFollowup/native-examine-first.json),
[restoration receipt](Verification/DensityFollowup/native-examine-first-postflight.json),
[full descriptions and checks](Verification/DensityFollowup/NativeExamine/9c5fa9a75a35496d9b85716e6f5b7b28/report.json),
[signpost screenshot](Verification/DensityFollowup/NativeExamine/9c5fa9a75a35496d9b85716e6f5b7b28/signpost-log-latest.png),
and [weapon screenshot](Verification/DensityFollowup/NativeExamine/9c5fa9a75a35496d9b85716e6f5b7b28/weapon-description.png).

**Bounds:** this is one staged native UI fixture at one resolution, using an
isolated game and real factory content. It does not prove natural acquisition,
placement frequency, balance, every possible dynamic description length or
every display resolution. The ordinary saved game was not loaded or modified.
