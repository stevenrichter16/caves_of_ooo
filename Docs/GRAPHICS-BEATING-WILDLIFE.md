# Beating wildlife — three shared actor bodies

Status: bounded source/Editor integration verified2026-10-28; source publication pending. Three unchanged approved16×16 bodies now resolve on the flat16×24 grid; this is not reauthored16×24 art or full-area polish. Read existing `Docs/GRAPHICS-BEATING-MATERIALS.md` and `Docs/Verification/Live16x24/beating-independent-review.md:88–123`; preserve the accepted material kit and separate later composition polish.

## Exact identities and corrections

All three inherit `Creature` (`Assets/Resources/Content/Blueprints/Objects.json:2700`): layer 10, solid physics, Body/Brain/Inventory, DamageFlash, and CreatureCorpse death handling. Positive current owned HP is required; full original HP, peaceful AI or unchanged faction are not body requirements.

| Owner/source | Authored identity | Correction / essential behavior |
|---|---|---|
| Scorpion, `Objects.json:17602` | `x`, `&W`; HP10; Flesh/Organic,Chitinous; combustion .2, brittleness .3; thermal flame500/capacity1.2 | Distinct from GlassScorpion. ScorpionSting is 1d3 piercing with Poisoned specification; corpse harvest VenomGland chance75%, not guaranteed. |
| BrittleHound, `Objects.json:19601` | `b`, `&W`; HP20; Glass/Crystal/Brittle, brittleness .6 | Inherits Creature combustion .15 and thermal flame500/capacity2: “glass” does not imply thermally inert. BrittleFangs is 1d6 with Bleeding specification. |
| GlassblownDrifter, `Objects.json:16856` | `@`, `&R`; HP45; AV3/DV3; Brain sight12; Conversation and Trader | Inherits **Flesh/Organic**, not Glass. Talkable trader with `GlassblownDrifter_1`, `DrifterStock`, initial purse80. Preserve mutable stock, purse, conversation, equipment, relationships and AI; do not treat it as a Merchant alias. |

Natural weapon source: `Assets/Scripts/Gameplay/Anatomy/NaturalWeaponFactory.cs:46,111`. Generation: `Assets/Scripts/Data/Tables/PopulationTable.cs:471,491–510` supplies Scorpion/Hound in Beating tiers; `Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs:378–395` spawns Drifter at GlassblownObelisk, alongside a real Pillar.

## Existing art and routing

Exact immutable candidates, all 16×16, viewed directly:

- `Assets/Resources/Sprites/Environment/scorpion.png` — SHA256 `32650e55a2a1284984fb11129b243242f4f613551e175272f0535a55c3205091`.
- `.../brittle_hound.png` — `6b277f949cf55edad26579ebfc732d93af95f285650b6dfaabe94ad60d94fea8`.
- `.../glassblown_drifter.png` — `2dbfbc75816a83e7bda5243cc87ef11834684624cadeea2428d70c8b3ed5bcd0`.

Use cached centered FullRect/16PPU sprites without resampling if actual trial approves silhouettes/contrast. These are established legacy assets, not newly accepted flat-runtime coverage. Exact editable authorship for these rasters was not located in the bounded audit. Existing `ArtSource/SpreadVisitorCreatures/Export/models/spread-visitor-{scorpion,brittle-hound}.fbx` and Spread humanoid routes are separate 3D sources, not demonstrated PNG provenance; preserve them and all PNG/meta inputs.

`EnvironmentSpriteRenderer.cs:150,160,165` already names the three legacy texture mappings. No current `FlatPixel*Catalog` species match was found. Shared residents only covers exact Villager/Merchant. Add authoritative known-role refusal before weaker local opening/incidental branches (`FlatPixelGreyboxCatalog.cs:1351–1376`), so malformed/custom species cannot regain art through glyph `@`/`b`/`x`. Keep legacy presentation reversible.

## Disclosed baseline and minimal gates

Actual `beating-final-03-brine-lens.png/.json` shows Drifter35219 at46,13 and Scorpion35226 at58,20, zone17,13, tick2750, player40,12. Actual final05 CaravanRoad shows Hound39380 at30,11 and Scorpion39379 at15,1, zone19,13, tick2870. All records explicitly have current paint receipts and glyph fallback; these are historical disclosed coordinates, not guaranteed current NPC positions.

Healthy earned choices: `Fixtures/packaged-beating-brine65` turn3210/HP38 in17,13, or `Fixtures/harvest-products-road65` turn3540/HP38 in19,13. Reacquire through ordinary controls/readers; normal eastward world travel connects17,13→19,13. Do not navigate from undisclosed save contents.

Next: close reader baselines, immutable-art critique, finite RED owner/resource/disclosure/corpse/effect counters, then actual world/readers/travel and Drifter Chat/Trade. Retain one Render dispatch and damage/status cues, genuine actor selection/covering and reader authorization; no new attack animation or spawn/density changes. Surface fullness, remaining species and area polish stay open.

## Root corrections and current baseline — 2026-10-28
Root read all four raw blueprint contracts including inherited Creature, NaturalWeaponFactory sting/fangs, actual population tables and obelisk stamp. Body lookup must not pin material/temperature: Drifter is Flesh; Hound inherits thermal behavior. Preserve poison/bleed mechanics and75%optional Scorpion harvest; no guarantee invented. Directly viewed all three existing square rasters: small distinct silhouettes suitable for bounded immutable reuse; no fresh source pixels or animations needed.
Reacquired actual packaged-beating-brine65 via isolated normal Continue: Overworld.17.13.0,turn3210,player40,12HP38, Drifter35219at46,13 and Scorpion35226at59,21 currently disclosed. Baseline world and exact readers captured. Before03 is Look at59,20 after a CUA timeout, not a reader; before04 completes the exact Scorpion reader. No game state changed by Look.49 protected paths still preserved; source residents5de432821 verified.

CaravanRoad baseline: normal Continue harvest-products-road65 reacquires player59,9HP38, Hound39380at58,9 and Scorpion39378at59,10; actual world/reader05/06. Historical preflight coordinates30,11 are obsolete at this checkpoint; no hidden-owner search used. Preserved both isolated baseline roots before Finish. Actual hourly report02:28UTC covered resident publication5de432821 and next three surface bodies; next03:28UTC.

Semantic RED725f88f30ab3445298b165174ee3f630:20/20 failed on absent adapter or actual glyph fallback, exported before production. Then installed three-body catalog and four world/reader seams; unchanged16×16source pixels, no blueprint/AI/material/economy/combat changes. Current owner, neutral visual, singlecell/HP/uniqueownedparts guards preserve distinct species and real Drifterservices. No new animation or generation.

## Verified integration and actual play — 2026-10-28 02:46UTC

**Implementation:** `FlatPixelBeatingActorsCatalog` caches independent immutable Scorpion/Hound/Drifter views, with exact neutral identity, live positive HP, single-cell owned parts and Drifter-only service guards. Four `FlatPixelGreyboxCatalog` world/reader routes make refusal authoritative; optional resources, selected-owner Render cues and disposal are preserved. No content, combat, spawning, floor, stock or AI changes. Original PNGs/metas and alternate presentation remain intact. Qud mechanics are reused unchanged; no new parity claim or external-source port.

**Native:** genuine20/20 semantic RED job725f88f30ab3445298b165174ee3f630 preceded implementation. One production compile correction renamed a colliding local `actorTile` to `beatingBodyTile` in the modern reader; no semantic change. Final job1eb85bb470b24f66bee5f802abfd498f passed139/139 distinct tests:20actors+24sharedresidents+95Beatingmaterials,16.568s. Positive/counter cases cover exact body/reader identity, ownership and mutation refusal, missing resources, stale/disclosed/covered subjects, oneRender Wet cue, real damage/death/corpse and Drifter chat/purchase without restocking. Native mechanics are not claims of actual poison/bleed/corpse observation.

**Actual CaravanRoad:** before05/06→after01–05, earned seed65 Overworld.19.13.0. Player59,9HP38 sees Hound39380at58,9 and Scorpion39378at59,10; another Scorpion39379at10,5. Exact readers match bodies and material identities. One ordinary left bump misses Hound; its fangs hit player for5, HP33 and Confused. Actual F5fixture135 is turn3550; fresh isolated EditorContinue135 restores59,9HP33, Hound58,9, Scorpions59,10 and11,5. Three ownerIDs here are two species. No hit, poison, bleed or death was forced or inferred.

**Actual BrineLens:** before01/02→after06–13, earned seed65 Overworld.17.13.0. Drifter35219 starts46,13HP45, stock4/purse100; Scorpion35226at59,21. Exact Examine and normal approach follow the wandering drifter to45,8; player45,9HP38. Dialogue uses the same body. Trade09 shows4items/purse100 and player50. After10 is only the confirmation prompt, not a completed purchase. Confirmed buy11 transfers one ember-fruit for18: stock3/purse118/player32. F5fixture136 is turn3290. Fresh isolated EditorContinue136 restores position/HP, Drifter45,8stock3/purse118, Scorpion60,18 and world artwork. These two existing checkpoints are independently loaded; no new cross-zone traversal was performed.

**Comparison:** world rectangle[0,40,940,640],564000pixels. Road before05→after01 differs1721pixels; Brine before01→after06 differs1084pixels. Both actual save/restart pairs (after04→05,12→13) match all564000pixels exactly. Fullscreen UI/log equality is not claimed. Existing approved scene reinterpretation/material references remain the design basis; no new imagegen or pixel authorship was needed for this reuse-only slice.

**Profile:** actual Road Editor69.488s,24199samples, renderer mean0.1074ms/p99.2807ms, drawcalls mean39.30/max42. Includes readers, one combat action, UI/tool/idle time; no controlled speedup, sustained FPS, package or physicalDeck claim. Console retained two memoryless depth load/store warnings; no clean-log claim.

### Self-review and scope bounds
- 🟡 No unresolved implementation blocker after independent source/actual review. Actual purchase and both reloads are root-observed.
- 🔵 These three reused16square sprites replace glyphs; they do not establish uniform16×24 art quality. Drifter is a small rounded red silhouette; later design/polish may improve it without misrepresenting Flesh as Glass.
- 🧪 No new packaged, Linux, physicalDeck, cross-zone, poison/bleed or corpse play observation. Current packages remain source7e2ed7a30; batch these surface changes with residents before rebuilding.
- ⚪ Broad empty sand patches, mixed glyph Sack/Well/tent-service owners and the narrow dialogue faction-label clipping remain visible. Coverage is separate from all55+5open polish scopes. No fake clutter/loot/services were added.

### Preservation and reproduction
Launch the existing isolated `LiveFlatPixelGreyboxTools` checkpoint workflow with `Fixtures/harvest-products-road65` or `packaged-beating-brine65`; use ordinary Continue, Look→Enter→Examine and normal movement/interaction. Reproduce reloads from `beating-actors-road135` and `beating-actors-drifter136` without editing saves. Full baseline/after/restart roots were copied and SHA256-verified before Finish under `/private/tmp/coo-beating-*-preserved-*`. After final Finish: playingfalse, rootnull, sceneclean.49 protected foreign paths unchanged. Three owned cave meta whitespace changes backed up/restored; no foreign staging, cache deletion or storage reclamation.

Files: new actor catalog+meta and20-case tests+meta; Greyboxfourroutes; this record, completion/worldcoverage/CLAUDE and bothJSON ledgers; `beating-actors-*` actual/test/profile/review evidence and fixtures135/136. No art pixels changed. Next finite candidate review compares shared surface props (Sack/Well/tent identities) for impact and reusable art; package batch and missing actors/props remain open. Last actual hourly report02:28UTC,next03:28UTC; this milestone is not a duplicate hourly report.
