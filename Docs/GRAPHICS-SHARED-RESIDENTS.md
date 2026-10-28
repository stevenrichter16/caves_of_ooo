# Shared settlement residents: finite preflight

Priority: broad settlement/interior readability, not further cave coverage. Scope exact neutral **Villager, Merchant, Farmer** bodies; no new animation or service mechanics. Preparation only; baseline readers, RED and actual acceptance remain pending root.

## Source-correction table

| Premise | Verified correction |
|---|---|
| All three are interchangeable `@` actors | `Assets/Resources/Content/Blueprints/Objects.json:11884,13178,24562`: Villager `@/&g`, Merchant `@/&W`, Farmer `@/&Y`, all layer10, solid Creature, empty default VisualID/VisualVariant. Farmer inherits Villager; Merchant independently inherits Creature. Require exact blueprint plus neutral visual contract, not glyph alone. |
| Passive resident means friendly | Villager/Merchant Brain defaults Staying=true, Wanders/WandersRandomly=false. Farmer adds self-preservation and well visitation. Actual faction/hostility, injury and goals remain live; never gate body selection on friendliness or initial AI state. |
| Every Villager is an ordinary human visual | `VillagePopulationBuilder.cs:953–963` creates blueprint Villager named **Ellun** for the BMO quest, then changes glyph to`b` and color`&c` without VisualID. Empty VisualID alone is insufficient. Conversely generated names such as Perret are legitimate; do not require default DisplayName. |
| Neutral body implies fixed conversation/purse/stock | Villager has Conversation Villager_1/Trader VillagerStock35; Merchant Merchant_1/MerchantStock150; Farmer Farmer_1/FarmerStock45. Villager also authors IntProps Drams100, Merchant500. `Economy/TraderPart.cs:55–92` only installs the configured purse when current drams<=0 and stocks an empty shelf; `VillagePopulationBuilder.cs:810–824` adds repair stock. Never pin current purse, contents or exact conversation ID: normal quests/trading/restock mutate them. |
| Living and corpse can share actor art | Creature inheritance (`Objects.json:2700`) supplies Body/Brain/Inventory, Flesh+Organic/Thermal, DamageFlash and Corpse→CreatureCorpse. All three start20HP. `Entities/CorpsePart.cs:11–15,199` records SourceBlueprint on a **different corpse owner** after death/inventory drops. Admit owned positive live HP; never resurrect a body using corpse SourceBlueprint. |

The semantic guard should require unique owned Render/Physics/Body/Brain/HP and the real Conversation/Trader/Inventory components, neutral visual identity, current one-cell placement/custody/disclosure. Normal HP changes, equipment, names, conversation variants, stock, effects and hostility do not redraw the body. Preserve the existing one-dispatched-Render damage/status cue, ordinary Chat/Trade refusal/actions and actor foreground selection. Queries must not call ObjectCreated, restock, dialogue, idle offers or AI decisions.

## Existing art and precedence

- Villager: `Assets/Resources/Sprites/Live16x24/opening-cast.png`,64×24, frame0 rect(0,0,16,24); SHA256 `35a2de34d9bf6d6dfe66c7dd47439f7ac03ec6b7de9e09f026d6cb623e24d73a`. Editable `ArtSource/Live16x24/cast/opening-cast.blend`, `source-cells.json`; builder `Tools/Live16x24/build_opening_cast.py`.
- Farmer: `Assets/Resources/Sprites/Live16x24/farmer.png`,16×24; SHA256 `4981b3cb861d30a3c8db4b426237f2a751dd48eca584034dac43d78acb63bfc0`. Editable `ArtSource/Live16x24/incidental-actors/incidental-actors.blend`, `source-cells.json`; builder `Tools/Live16x24/build_incidental_actors.py`.
- Merchant: existing `Assets/Resources/Sprites/Environment/merchant.png`,16×16; SHA256 `d79c98d33a03b156c400e9f7120c99f1a133a67468c3dfee609e77af53ea6f2e`. Existing raster is available for direct immutable reuse; this bounded audit has **not identified an exact editable authoring source**. Preserve merchant_sheet/merchant_cast and metadata; do not slice their different layouts as16×24 by assumption.

Use centered FullRect16PPU cached views, independent optional resources/disposal. Local Villager currently uses `FlatPixelOpeningArtCatalog.cs:168–184,643–652`; Farmer uses `FlatPixelIncidentalActorCatalog.cs:39–66` behind Greybox's local gate. Neither is among the four global motion identities (`FlatPixelActorMotionCatalog.cs:61–75`). Preserve approved local pixels and explicit injections. New strict known-role refusals must not fall through to the weaker local CastFrameOf route, which presently checks only Creature+blueprint. Do not widen motion or legacy3D policy.

## Earned actual starting points

Best Sill start: `Fixtures/sill-envelope-after65`,turn1070HP38,player67,18; matched-reload census explicitly discloses neutral Villager7584 at62,12. Earlier same-graph north-door/river census discloses Merchant7510 at73,12; reconfirm current visibility before approach. Ellun7600 at72,11 in the interior census is the **excluded reskin**, not neutral-positive evidence. Healthy alternative: chest99,turn540HP40 at43,12, but its receipt contains no NPC coordinates.

Beating alternative: `Fixtures/beating-baseline-dune65`,turn2930HP38. `beating-final-06-dune-belt.json` discloses wayfaring Merchant at44,16,player40,12,blank visual strings; `WorldTravellers.cs:70–80` changes name/receipt, not species. No exact disclosed Farmer position was established here.

Capture current body/readers and normal conversation/trade first. Then compact paired native guards/resource/Ellun/corpse/effect tests, actual movement and save/restart. No hidden-save navigation, all-village acceptance or area-polish advancement follows.

## Root scope and verification — 2026-10-28
Delivery evidencef1acb8760378adbc206854e591845728ff2e8ea8 is verified on main. Root read Creature/Villager/Merchant/Farmer blueprint contracts, existing actor guards and opening CastFrameOf, Ellun placement and actual Trader ObjectCreated stocking. Inspected approved opening-cast and merchant rasters. First slice is Villager+Merchant only; Farmer retains its existing route pending a disclosed baseline. Do not require initial names, stock, purse, AI state or conversation IDs; preserve real services and effects. The exact neutral glyph/color guard excludes Ellun even without VisualID. Generated sprites and animation are unnecessary for approved immutable reuse. The original Merchant editable authoring source remains unidentified; preserve existing raster and alternatives.

Baseline132: actual seed65 Sill Overworld.10.10.0, turn1210 HP38, player65,12, neutral Perret Lowbank Villager7584 at66,12; captured glyph world and examine, real dialogue/trade via keyboard. Earlier waypoint merchant7510 at73,12 was actually disclosed; moving service actors blocked the first approach, preserved without changing AI. Ellun remains b. Fixture132 is an untouched F5 pair. Entire isolation copied/hash-verified to /private/tmp/coo-residents-before-preserved-2e131e05 before Finish. Capture03 is Look,05 is action menu (filenames are historical intent);04 is actual examine,06 dialogue,07 trade. Approved sprite reuse avoids unnecessary regenerated replacements/concepts.

Test-draft correction: EntityFactory is in CavesOfOoo.Data; added missing using before semantic RED. The first two jobs discovered zero tests during compilation and are not passes or RED evidence.

Semantic RED job5591bf24bfa24aedaf23fcbd136a60f2 discovered24 cases:23 failed (missing adapter/old glyph), one existing injected-cast control passed. Then installed two-body catalog and four authoritative Greybox routes, preserving existing cast injections, separate resource failures and single Render cue. No blueprints, AI, services, art pixels or geometry changed.

GREEN33d34f46829245f69440543c108a9d08:24/24. Related initial25a3827e230849f7a93edc9d05250569:129/130; old opening Villager test demanded ignoring an explicit terrain VisualID. Corrected that single expectation to the new neutral-body contract: reskin uses neither cast nor masonry, clearing VisualID restores original frame. Other three local cast expectations unchanged. This is an intentional identity-policy change, not a gameplay change.
Independent cold review found no P1/P2 in catalog/four routes/resources/cues/disposal; comment position corrected.

## Bounded result — 2026-10-28 02:22UTC

**Integrated and actually observed:** exact neutral Villager and Merchant bodies in Sill, reused unchanged approved rasters; world, exact Examine and dialogue portraits share identity. Farmer and other specialist/reskinned residents remain unchanged. No new animation, blueprint, layout, economy or save-format change.

- Final native job `610dc7daa4154147ae916ba396f930b3`:154/154, including24 new cases plus130 relevant opening/cast/incidental/Grovelands cases. The separate24-case GREEN is included, not added to154. RED23fail/1existingpass and initial related129/130 retained.
- Actual seed65 F5 fixture132: turn1210,HP38,player65,12; neutral Villager7584 at66,12. Matched reader04→after02 replaces the glyph with approved16×24 Villager. Normal dialogue/trade after03/04 work; the entire1280×800 trade frame equals before07 pixel-for-pixel. Ellun remains its distinct b glyph.
- Normal walking through open VillageDoor7473 reaches the shop. Merchant7510 at73,12 uses unchanged16×16 source centered at16PPU, with genuine Chair6809 and Sitting status. Exact subject selection requires Everything here→merchant; after06 is the real reader,08 dialogue,09 trade. Queries/inspection leave14stock entries and500drams intact; no actual purchase claimed (native test covers purchase).
- F5 fixtures133 turn1310 at72,11 and134 turn1320 at72,12 retainHP38. Fresh isolated Editor Continue134 restores72,12 and the same merchant/stock/purse/nativebody. After11 is the clean restart world capture. After05 is a pile notice,07 an ordinary world view,10 a Pause-overlay save frame; their intent-based filenames do not establish the named reader/dialogue/cleanworld gates. No identical restart-pixel claim.
- Profile `shared-residents-editor-profile.json`:81.571s,32,153samples; renderer mean0.0812ms,p99.2266ms,drawcalls mean36.55/max43. Includes tool/UI/idle, no controlled speedup, sustained FPS or physical Deck claim. Two existing memoryless-depth console notices; no clean-log claim.
- Isolated before/after/restart branches fully copied and hash-verified under `/private/tmp/coo-residents-{before,after,restart}-preserved-*` before Finish. Root verifies Playfalse, SaveRootOverridenull,sceneclean.49 foreign dirty files unchanged; no drive/cache deletion.

**Self-review:** no remaining P1/P2 identified by independent source review. Coverage is bounded to two roles, not every settlement. Whole-scene captures retain sparse grass, dark repeated surfaces and specialist glyphs; no area-polish promotion. Merchant16×16 body is smaller/rounder than Villager16×24; source-preserving reuse is intentional and may need the later cohesive cast-polish pass. Exact editable Merchant raster provenance remains unidentified. Existing Mac/Linux packages remain source7e2ed7a30 and do not yet include residents; batch the next surface-family source before rebuilding.

**Next:** Beating Scorpion/BrittleHound/GlassblownDrifter preflight in `GRAPHICS-BEATING-WILDLIFE.md`. Missing surface creatures and talkable drifter are higher-impact next coverage than parked cave liquids. Full world coverage, harvest observations, Farmer/specialist residents and55+5 area-polish scopes remain open.
