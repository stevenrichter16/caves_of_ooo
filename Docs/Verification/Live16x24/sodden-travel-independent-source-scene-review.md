# Sodden source/import final review — 2026-10-27

Scope: independent source verification, imported PNG bytes/import settings,
preserved inputs, a bounded dry-overlay/mire-mapping code read and living-doc
accuracy. No Unity actions, Git actions, Assets edits, rendering or simulation
execution were performed. The source verifier was called in memory without its
receipt-writing entry point. Final actual captures for all three districts were
subsequently reviewed below. Restart/reload and hardware performance remain
outside this review's observed evidence.

## Result

No source/import blocker found. The imported
`Assets/Resources/Sprites/Live16x24/sodden-travel.png` is byte-identical to the
frozen source: 2,833 bytes,448×24,28 frames, SHA256
`45df6770bc5ff407c4dd2d719918855affd5aa7931197222b7e9e65a08839a1b`.

- Current source verification passes. All17 files named by the frozen handoff
  still match their sizes/hashes; no handoff update is needed.
- All231 source-preservation entries and all39 entries in the parent's
  `/tmp/coo-sodden-import-before.json` retain their original hashes. These are
  the exact bounded baselines independently rechecked; this is not a fresh
  whole-repository Git comparison or a replacement for the parent's separately
  recorded38-foreign-tracked-file preservation check.
- All20 accepted non-mire frames remain identical in semantic cells and decoded
  native pixels. The13 preserved pass2 snapshot files also match.
- Import metadata was checked by named section/key, permitting Unity-added
  defaults: exact GUID/spriteID; sRGB; no mipmaps/NPOT scaling; Point/Clamp on all
  axes; sprite texture/single-sprite settings; FullRect/no extrusion; centered
  pivot;16PPU; alpha usage/transparency; sufficient texture size; no authored
  importer subrects; uncompressed/non-crunched settings for every listed
  platform. Metadata SHA256:
  `447bcc5b53eab78f529cdae7042dddba1bf67a54a2c52b72fb789093e636c91d`.
- The source README correctly limits its repeated-render proof to decoded
  native pixels and source cells, records nonidentical PNG encodings and keeps
  the actual433-cell carpet defect as the reason for this revision.

## Bounded runtime cross-check

In `FlatPixelSoddenTravelCatalog`, only exact Reeds, DeadTree and Duckboard
owners enter `NeutralDryOverlay`. `ForegroundFrame` still requires supported
owned parts, expected glyph/tags/material, applicable intact/thermal state and
absence of competing gameplay meaning. Explicit cultivation/liquid/source/
trigger vetoes precede the generic Terrain exception. `TryResolveGround`
rejects nonempty live tile state before examining occupants. Unknown terrain,
custom visual variants and missing lower Grass/StoneFloor remain conservative.

`MireFrameAt` maps filled to16/22/23/24 and wet film to17/25/26/27. Its coordinate
hash selects base for12 of16 low-bit values and distributes the remainder over
three variants. It neither animates nor consumes simulation RNG. No concrete
defect found in this bounded code read; native execution remains parent-owned.

## Living-doc corrections/limits to carry into final delivery

`Docs/GRAPHICS-SODDEN-TRAVEL.md` accurately describes the28-frame source,
preserved non-mire art, actual first-integration defects and current pending
scene acceptance. Keep those limits until the revised actual captures exist.

The current `sodden-travel-native-final.xml` (05:40:08–05:40:38UTC) has307
cases: **304 Passed,0 Failed,3 Inconclusive**. Each inconclusive case is
`WarmRegionalAuthorityLookupDoesNotAllocateAndStillReadsCurrentMap` for one of
the three district addresses. The reason is: “Current-thread allocation
counter did not detect the positive control.” Do not call this307 passes or
use it to establish allocation-free runtime behavior. The current living doc's
304-pass wording is accurate; add the three inconclusive outcomes when
recording that final suite unless a later receipt supersedes it.

The first-source contact reviews are not actual-scene acceptance. Room/object-
family coverage and physical Deck performance remain separate open work.

## Matched actual crossing review

Inspected all four original1280×800 Game captures directly, without crops,
resizing or image edits:

- `sodden-after-01-crossing-entry.png` against
  `sodden-final-01-crossing-entry.png`.
- `sodden-after-03-east-board.png` against
  `sodden-final-03-east-board.png`.

Their literal submitted-paint JSON establishes the following. Each pair has
exactly matching `(x,y,ownerID,blueprint)` rows, the same selected-cell totals,
seed65/zone16.7.0, identical player position/tick and zero unmatched receipts.
The images establish the actual1280×800 output; the JSON's larger Screen
dimensions describe the Editor tool context.

| View | Matching state | Actual mire selection | Corrected lower ground |
| --- | --- | --- | --- |
|Entry|20,12, tick1370;1047 on-screen cells|433 filled pools:323 frame16,31 frame22,41 frame23,38 frame24;74.6% base|19 boards and12 reeds now retain authored Grass beneath them; all previously had glyph backing.|
|East board|60,3, tick1860;1091 on-screen cells|363 filled pools:276 frame16,36 frame22,29 frame23,22 frame24;76.0% base|47 boards,17 reeds and1 dead tree now retain authored Grass; all previously had glyph backing.|

**Named-defect verdict: pass for these two actual crossing views.** The bright
paired mounds are gone. The pool is one continuous, subdued olive field with
small irregular wet marks, matching the native28-frame contact's intent.
The narrow wood route and reed/tree silhouettes no longer sit inside isolated
black rectangular gaps. Their foreground identities and the cut-peat banks
remain recognizable and unchanged. The dark southern remembered patches
already appear in the22-frame capture and are existing fog/fallback behavior,
not a newly erased dry-ground surface.

**Fullness verdict: still open.** These are more coherent travel scenes, but
large ordinary Grass areas still show conspicuously regular bright stippling.
The actual crossing retains fallback identities for Bandfrog and the route
notice at entry, and the passage guard/post at the east route. Their existing
functions are the next concrete sprite opportunities; these screenshots do
not justify inventing extra scenery or claim a complete Sodden biome. They
also exercise only positive-stock filled mire: no actual depleted-film view,
hazard-state transition or broad biome-wide art coverage is established here.

The appropriate bounded acceptance is the repaired carpet/backing defects on
these unchanged earned crossing views. Service-owner coverage and actual
fullness remain separate outstanding gates.

## Final actual shelter and peat works

Also inspected the full original Game captures `sodden-final-06-shelter-entry`,
`08-shelter-room-matched`, `09-peat-works-entry`, `10-peat-cut-route`,
`11-peat-reader` and `12-saved-works`. Literal paint receipts exist for06/08/09/10;
reader11 and saved-view12 are screenshot evidence, not new paint-count samples.

| Actual view | Disclosed native environment/ground selected counts | Remaining disclosed glyph owners |
| --- | --- | --- |
|Shelter15,7;40,12;tick1900;937 cells|37 Duckboard,281 MirePool,8 Reeds,33 StoneWall;572 Grass and2 StoneFloor selected as ground|1 route notice;2 Sumpsieve crops, respectively stage2 and stage0.|
|Shelter15,7;34,8;tick2000;184 cells|6 Duckboard,39 StoneWall;66 Grass and70 StoneFloor selected as ground|1 unrepaired dressing bench and1 PeatCutter actor, displayed as Sella, dressing keeper.|
|Works17,7;40,12;tick2060;706 cells|37 Duckboard,108 MirePool,59 PeatBank,13 Reeds,49 StoneWall;396 Grass and41 StoneFloor selected as ground|1 wayfaring Merchant and1 unharvested SoddenWorksSalvage, displayed as collapsed drying frame.|
|Works17,7;39,16;tick2110;653 cells|36 Duckboard,115 MirePool,60 PeatBank,14 Reeds,32 StoneWall;359 Grass and35 StoneFloor selected as ground|1 wayfaring Merchant; the salvage is no longer in this disclosed sample, not proven removed.|

Every listed receipt has zero unmatched rows. Each also contains the one
authored player. Ground counts above describe selected ground owners, not the
larger number of lower floor submissions under actors, walls or other overlays.
The samples overlap and are not additive coverage totals or a whole-zone
denominator.

All observed mire still uses **filled** frames16/22/23/24. The shelter sample
contains206/19/22/34 respectively; the works entry69/18/13/8, and cut route
70/17/17/11. Small bounded layouts need not individually reproduce the hash's
nominal75% weight. No film frame17/25/26/27 occurs in these captures, so no
ordinary depletion/remaining-film gameplay claim is supported.

The shelter exterior reads as a quiet wet settlement edge with connected wooden
approaches; the interior now has a coherent stone enclosure and visible floor.
It nevertheless feels largely empty because its only two service identities
are still a line glyph and a ring glyph. The works has distinguishable strata,
wood travel surfaces, masonry and reed clusters; continuous cut banks read as
solid material rather than isolated posts. No new material blocker is visible
in these bounded final scenes. Broad grass stippling and fallback owners remain
conspicuous across the district.

The peat reader visibly shows the matching native cutbank portrait, Material
Peat and Structure10/10. The saved works view shows the player at39,16 with38HP
and the recent autosave log. It does not by itself prove a subsequent restart
or save reload; that is a separate parent-owned check.

## Plain continuation requirements

Accept the current static-material and dry-backing corrections for the observed
three district scenes. Do not declare their fullness, all Sodden coverage or
Steam Deck performance complete.

1. Give existing service identities their own readable native forms: the
   dressing bench and keeper, route notice, passage guard/post, crop stages,
   collapsed drying frame and merchant. Keep each visual tied to its actual
   usable state. Bandfrog is an additional disclosed enemy-art gap.
2. Review the same rooms and routes with those real objects visible before
   adding new furnishings. The keeper's room is the clearest current example:
   first render its bench and occupant, then decide if its70 selected floor
   cells still feel underused.
3. Reduce the conspicuous regular grass marks only through a named scene-scale
   revision with preserved inputs; do not replace approved art speculatively.
4. Carry the verified material rules to ordinary Sodden formations after their
   actual owners, hazards, routes and POI exclusions are checked. Plain water
   and still-hazardous drained mire must remain different facts.

These needs extend the existing chunk functions. The review does not recommend
extra decorative clutter, new animation, invented interactions or terrain
changes merely to fill the image.
