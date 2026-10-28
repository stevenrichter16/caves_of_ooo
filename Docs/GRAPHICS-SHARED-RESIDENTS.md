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
