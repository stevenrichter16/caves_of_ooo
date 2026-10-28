# Beating wildlife — finite source preflight

Status: planning only, after shared residents. No new art, tests, gameplay inspection or coverage acceptance. Read existing `Docs/GRAPHICS-BEATING-MATERIALS.md` and `Docs/Verification/Live16x24/beating-independent-review.md:88–123`; preserve the accepted material kit and separate later composition polish.

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
