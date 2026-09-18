# Normal-session tile persistence

2026-09-09. Minimum implementation passed **31/31 EditMode tests** after a genuine initial **17 failures / 14 controls** RED. The dedicated adversarial sweep then produced **39 passing controls / 1 genuine load-hook ordering failure**, zero C# errors. The narrow post-hook recapture correction is implemented; the combined persistence suites now pass **71/71**, with zero C# errors in the parent-run 302-case focused batch. Root owns all Unity runs, native verification and commits.

## Problem and scope

`GrovelandsFormationBuilder.cs:220–222` writes permanent water directly to native TileState in TendrilFen. Water-only cells have no pool entity. The existing raw zone graph in `SaveGraphSerializer.SaveZone/LoadZone/SaveCell/LoadCell` writes ambience, version, cell flags and entity references, and therefore loses this water plus transient tile layers on session reload.

The extension preserves exact current native TileState: deliberately erased water stays absent; coating/residue IDs, order and duration survive; Heat/Cold/Charge and cloud ID/duration survive. It includes every cached zone at every depth, even explicitly empty zones. It does not regenerate terrain or water from seed, reconstruct old lost water, alter player locations or add pool entities. Rendering must follow the loaded native state.

This is CoO-original persistence plumbing, **not Qud parity**. The generic self-describing v8 save-field overhaul remains separate. Raw standalone `SaveGraphSerializer.SaveOverworldZoneManager` and entity graph encodings remain unchanged. Only full `GameSessionState` adds the section.

## Source verification and corrections

| Source | Verified fact and implementation consequence |
|---|---|
| `SaveSystem.cs:59–80,167–177` | BinaryWriter/Reader encode ints; existing section checks hash names. One shared internal hash helper prevents drift. Existing string encoding remains untouched. |
| `GameSessionState.Save`, `SaveSystem.cs:372–374` | Queue bodies finish before the new optional section, followed by the unchanged final session check. |
| `GameSessionState.Load`, `SaveSystem.cs:408–429` | Validate the entire optional section and final footer before applying candidate tile data, publishing globals, clearing FX or dispatching hooks. |
| `ZoneTileState.cs:416–447` | Existing JSON import silently accepts/skips malformed data. The extension uses a strict bounded binary parser; its only reuse of `LoadFromString(null)` is an already-validated, callback-suppressed clear. |
| `Zone.ProjectPool`, `Zone.cs:494–499` | AddEntity may stamp permanent coatings. Native legacy-scene repair can therefore overwrite a deliberately empty saved tile even without seed regeneration. |
| `SaveReader.RunLoadHooks` | OnAfterLoad and FinalizeLoad may deliberately mutate loaded tiles. Reapplying the original pre-hook snapshot would erase legitimate hook work; the dedicated paired regression reproduced this before the post-hook recapture correction. |
| Existing `SaveSystemAdversarialTests` trailing-garbage/concatenated-stream cases | Parsing stops at the session End check. The extension must not inspect EOF, seek or consume a following session. |
| GZip load path | Uses a nonseekable GZipStream; the selected marker discriminator needs no stream positioning or EOF probe. |

## Version and wire contract

The main header remains **format 7**. The optional section has its own **version 1**:

```
existing format-7 session through EntityBodies.End
TileState.Begin check
sectionVersion : int = 1
zoneCount : int
  zoneID : extensionString
  tileCount : int
    packedCell : int = y * 80 + x
    coatingCount : int
      ID : extensionString; turns : int
    residueCount : int
      ID : extensionString; turns : int
    heat : int; cold : int; charge : int
    cloudID : extensionString; cloudTurns : int
TileState.End check
GameSession.End check
```

An `extensionString` is an int UTF-8 byte count followed by exactly those bytes. Only the new section uses this encoding. The writer sorts zone IDs ordinally and cell keys numerically; coating and residue order is preserved.

The reader consumes one marker at the old footer. `GameSession.End` means an exact legacy session with no tile snapshot. `TileState.Begin` requires a complete valid version-1 section, TileState.End and GameSession.End. Other markers, unknown section versions and truncated markers are errors. No EOF probe or seeking is involved, and permitted post-End bytes remain unread.

**Compatibility is one-way:** this reader accepts exact old format-7 sessions. Old executables are not promised to read newly extended saves; their old footer check rejects the section. This intentionally does not claim universal cross-executable compatibility despite the retained header number. Section checks are name-derived sentinels, **not payload checksums**; arbitrary valid-value bit changes cannot be detected.

### Validation limits

| Field | Accepted contract |
|---|---|
| Cached zones | 0–4096; exact decoded cache count; every known cache key once; nonnull Zone with matching ZoneID. Null manager requires zero records. Custom IDs need not follow Overworld syntax. |
| Tiles per zone | 0–2000; unique packed coordinates 0–1999. Empty zones are explicit. Empty written tile records are rejected. |
| Layers per family | 0–256; nonempty distinct IDs within each coating/residue family, positive duration through int.MaxValue. Same ID in different families is valid. |
| Identifier bytes | 0–1024 strict UTF-8; empty allowed only for the absent cloud. Unknown nonempty IDs remain valid and are never registry-filtered. |
| Heat/Cold/Charge | 0 through ZoneTileState.MaxEnergy (currently 2), with no clamping. |
| Cloud | Empty ID requires duration 0; nonempty ID requires positive duration. |

Counts and byte lengths are checked before their arrays are allocated. The writer validates and deep-copies layer payload before emitting the section; malformed public TileState is rejected rather than silently repaired. The loader parses into detached records and validates all framing before candidate state is applied. Normal save/load result and failure diagnostics remain owned by the existing SaveGameService; this adds no per-cell diagnostic flood.

## Candidate state, hooks and native scene repair

The implemented ordering is:

1. Parse all graph data, the complete optional tile section and both footer checks.
2. Apply validated tile data to the decoded candidate zones, with runtime render delegates suppressed during reconstruction and restored in finally.
3. Publish the existing turn/settlement/message/reputation state and execute existing OnAfterLoad/FinalizeLoad hooks. These hooks observe restored native tiles.
4. For an extended session, capture tile state **after** those hooks. Preserve legitimate hook additions/removals across native scene/index repair.
5. Rebuild loaded world indexes and existing scene migrations, then reapply that post-hook snapshot so AddEntity pool projections do not refill erased water.

Equal tile data is left untouched during Apply. The extension does not serialize delegates or advance durations. Legacy files without a section bypass snapshot application/recapture entirely and retain existing migration semantics. An old lost fen is not healed from seed.

The first minimum incorrectly reused the original snapshot at step 5. The mutation-enabled test actually failed with water duration 53 instead of 0, while its unchanged-hook control passed. The correction captures after all OnAfterLoad and FinalizeLoad calls, before rebuild; the paired test also asserts the hook-created liquid and cloud survive. The corrected combined persistence suites passed **71/71**.

Parser/framing failures leave existing live globals and pooled FX requests intact, as checked by the existing isolation fixture. General custom constructors, deserializers, load-hook exceptions and bootstrap apply failures retain their earlier contract and are outside this parser atomicity guarantee.

## TDD, counterchecks and independent review

- Initial 31-case suite: **17 RED failures / 14 passing controls**, zero C# errors; minimum implementation **31/31 GREEN**, parent-run. Raw RED: `Docs/Verification/GameSystemAudit/R3D-tile-save-red.xml.gz`.
- Dedicated 40-case suite: 31 malformed-section cases plus valid independent wire, UTF-8 and cross-family boundary, hook mutation on/off, actual Morrowfast migration with/without extension, writer invalid state, deterministic ordering and maximum-layer controls. **39 PASS / 1 genuine RED** against the minimum; post-hook correction implemented; dedicated rerun **40/40 GREEN** (combined persistence **71/71 GREEN**).
- Independent source review of bounds/read/apply found no additional must-fix beyond the known hook-result recapture; source-only, no Unity run claim.

The initial suite covers actual TendrilFen generation over an explicit open fixture; erased water, liquid pool erasure, all tile families/order/energy/cloud values, active/inactive/underground cache entries, corners, empty zones/null manager, unknown IDs, no save-time aging, resumed Tick, repeat roundtrip, hook visibility and graph aliases. It also checks actual compressed QuickSave/QuickLoad, nonseek input, legacy footer-only sessions, permitted trailing data, malformed framing before global publication and unchanged standalone zone graph scope.

The dedicated malformed cases preserve the live turn manager, settlement, message/reputation state, save root/preferences, pooled ASCII/spell request identities and payloads, and assert zero load-hook calls. A separately assembled valid wire control prevents a malformed fixture prefix from making every refusal test vacuous.

### In-phase self-review

- 🟡 Original-snapshot reapplication erased legitimate hook mutations in the actual paired RED. Fixed with post-hook recapture for extended sessions only; **71/71 combined GREEN**.
- ⚪ Retain header 7 with a versioned optional section and explicit one-way compatibility; older executables reject new saves.
- ⚪ No old-save water reconstruction and no changes to standalone/raw graph serialization.
- 🧪 Actual native complete-world save/reload evidence pending parent verification. EditMode tests do not establish rendered water quality or gameplay input feel.

## Performance and native acceptance

Work occurs only during save/load. The writer visits cached zones and their sparse written keys; it does not add per-frame/per-turn loops or render callbacks. Load rebuild copies are bounded by saved state and use no seed regeneration. No performance improvement is claimed.

Native acceptance should use seed 729490642 and the actual south TendrilFen world-generation pipeline. In a private save scope, record the complete native water mask, explicitly fixture-erase one cell, F5, mutate current state, F6 and require the exact saved mask plus saved erasure. Include all cached center/ring zones, persistent pool entities, empty cells and player location/aliases. This verifies persistence; it does not establish water aesthetics or recover data lost by older saves.

## Owned files

- `Assets/Scripts/Gameplay/Save/SaveSystem.cs`: surgical shared-marker/byte helpers and GameSession optional-section ordering. Preserve all concurrent FX/scene-migration hunks.
- `Assets/Scripts/Gameplay/Save/SessionTileStateSerializer.cs` + metadata: strict bounded optional section and detached snapshots.
- `Assets/Tests/EditMode/Gameplay/Save/GameAuditTileSessionSaveTests.cs` + metadata: parent-adopted initial 31 tests.
- `Assets/Tests/EditMode/Gameplay/Save/GameAuditTileSessionSaveAdversarialTests.cs` + metadata: 40 dedicated cases.
- `Docs/SPAWN-RING-TILE-PERSISTENCE.md`: schema, compatibility, scope, corrections, review and actual evidence.
