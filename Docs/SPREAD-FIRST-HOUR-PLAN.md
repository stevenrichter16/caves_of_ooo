# Spread first-hour content and readability plan

Status: **admitted implementation verified; explicit deferrals retained**, 27 September 2026, baseline `0417e145`.
Execution follows [the implementation prompt](SPREAD-FIRST-HOUR-IMPLEMENTATION-PROMPT.md).
Current evidence and scope corrections are recorded in §11 below. The previous
20,068/20,068 Unity EditMode result is the inherited integration baseline,
not evidence that these proposed additions work. Qud reference: none; this is
original CoO content using the existing game systems and approved voxel style.

## 1. Recommendation and completion contract

Make the new Spread start lead into a small, understandable expedition:
**notice a lead → prepare at a real service → choose an encounter → recover a
useful item → inspect and use or sell it → revisit a changed place**.

First repair misleading guidance and unreadable information. Then add the
approved encounter variants in stages, sharing one small source/persistence
contract; early E1 sourcing and E2/E3 admission remain evidence-gated.
Use existing quests, services, weapons and tactical actions wherever
they already satisfy the experience. Do not rebuild an onboarding campaign.

The player should be able to decline the encounter, retreat, lose, return later,
or spend time doing existing village work. No main-story or hospitality gate
depends on killing the new enemies. A meaningful reward is a visible choice
between usable equipment, preparation or sale value, not necessarily a numerical
upgrade. Do not promise a reward has survived if another actor has taken it.

The first-hour claim is a playtest target, not a hard timer. Success requires a
normal fresh character and in-world information, with no invisible direction
knowledge, granted equipment, forced injury, teleport or desired-loot reroll.
Measure how long discovery actually takes before assigning a pacing claim.

### Scope boundaries

- Keep the new start and approved glade composition; preserve fallback starts,
  explicit scenarios and Continue at the saved location.
- Reuse the existing Sill services, canonical village stories, regional notes,
  quest markers, trading, equipment, harvesting and save graph.
- Keep BitLocker dev-only; neutral GlassScorpion; plain Shambler without spores;
  silent Gin Frogs; the Sill omen/ending Urqu rule; natural Choir loot; friendly
  service NPCs; and protected unique actors/artifacts.
- Follow `Lore/MYSTERY-LEDGER.md` and culture voice cards. New early encounters
  concern ordinary material life, not answers to protected mysteries. No new
  infection/disease vector, divine appearance or deep-lore revelation.
- No general loot-rate increase, extra stat progression, T4 item, global rare
  enemy framework, offscreen caravan simulation, biome expansion, combat rewrite,
  fire-unit normalization or general UI redesign.
- New content is for fresh generation. Preserve serialized old entities and
  saved locations; do not silently restock, rebuild or repopulate old saves.
  Any narrowly required save support needs explicit compatibility tests.
- The stalled campaign automation and dog-fetching demonstration remain deferred.

## 2. Current-state audit and corrected premises

Classification: **working** means a shipped implementation with the stated
evidence; **confirmed mismatch** means current sources disagree; **extension**
means intentional new content; **unverified** requires a bounded observation.
Source anchors are relative to this repository and refer to the inspected commit.

| Surface | Classification and evidence | Planning consequence |
|---|---|---|
| Fresh start | Working. `ReferenceGladePlan.cs:11` names `Overworld.11.10.0`; `FreshGameStart` selects valid current Spread alternatives rather than forcing an invalid glade. Ordinary N/Continue acceptance is recorded in `DENSITY-SPREAD-START.md`. | Base leads on the actual current map/start, not an unconditional coordinate. |
| Starting content | Working. `ReferenceGladePlan.cs:50–54` already places supplies, quartz, signpost, enemies, Warden, Villager and pet. | No new tutorial chest, duplicate residents or empty-world premise. |
| Nearby settlement | Working source. `WorldMapAuthoring.cs:212` places Sill at 10,10; `VillagePopulationBuilder.cs:121–162` attempts Merchant, Quartermaster, Scribe and Innkeeper. | Verify their actual placed living owners and offered services; do not invent a shop or promise fixed stock. |
| Existing starter expedition | Working, but earlier route. `CHUNK-GAMEPLAY-IMPLEMENTATION.md` records the old 2.6 → Morrowfast 3.6 expedition and 28 live checks. | Its existence is not proof of discovery from 11.10. Preserve it and its identities rather than clone it beside the new spawn. |
| Regional directions | Working. `RegionalSignpostPart.cs:15–27` reads current surface directions through RegionalGuidance. It does not record notes or promise services. | Preserve truthful map resolution; only add a discovery connection if the ordinary-start observation finds one missing. |
| Glade Warden | Confirmed mismatch. Warden's blueprint selects `Warden_1`; `Conversations/Wardens.json:285–325,360–370` describes a village, local elder/merchant and possible elder reward. The glade has none of these owners; OfferHelp only logs a message. | Give this actual glade owner truthful contextual dialogue. Preserve genuine village Wardens and do not fabricate a payable hunt. |
| Ellun's direction | Confirmed mismatch. `Content/Conversations/BMO_Quest.json:8,94` says the stump is east past the village; `VillagePopulationBuilder.cs:967–975` chooses an unconstrained open cell. | Derive the lead from the actual target or constrain placement consistently, with fallback/refusal coverage. No native seed-specific misdirection has been measured in this planning pass. |
| Hallun's task | Confirmed mismatch. `RootBeerGuy_Quest.json:8,11,37` presents notebook recovery; `Data/Storylets/RootBeerGuyCase.json:19–39` also requires the gremlin fact. | Make offer/status/report wording agree with remaining objectives and actual state. Do not remove the second objective to make the test easier. |
| Existing stories | Working. `VillagePopulationBuilder.cs:980–1000` gives six global village stories canonical hosts. | Do not duplicate shared IDs/facts or rename copies as new quests. |
| Early reward frequency | Unverified. The incomplete C12 run opened three nonempty containers but selected no weapon/armor before its audit path policy stopped. | This does not establish scarcity. Inspect actual nearby earned/reachable gear before changing reward sourcing. |
| Exceptional keepers | Working, deliberately narrow. `LegendaryLairEncounters.cs:62–64` requires tier 2–3 final lairs and one keeper family. Current 142 Spread surface coordinates are tier 1. | New tier 1 rare encounters need a separate scoped design; do not relax legendary gates or clone unique lore people. |
| Return/save ownership | Working cache/save contract with an extension risk. `SaveSystem.cs:897–902` serializes cached zones; `ZoneManager.cs:116–138` can discard a graph. `OverworldZoneManager.cs:1088` specially retains lair graphs; ordinary production invalidation at 1317 concerns changed settlements. | Prefer non-POI wilderness sites; prove the precise return/unload contract before adding persistence machinery. Manual unload alone is not evidence of an ordinary farming exploit. |
| Readability | Observed limitation. The latest ordinary-start and steam captures clip long sidebar text. The exact camera/layout cause has not been reproduced in this planning pass. | Measure the rendering boundary first; keep a layout hypothesis separate from a confirmed cause. |
| Complete descriptions | Confirmed path mismatch. World Examine writes multiline detail to the log; sidebar wrapping splits spaces but not explicit newlines. Inventory Examine already uses paginated AnnouncementUI. Focus/menu status summaries are capped. | Reuse the complete reader for world descriptions; a capped summary must have a route to the full information. |
| Equipment comparison | Extension. Current ItemExamineService reports factual item contributions; the inspected inventory paths do not provide candidate-versus-current comparison. | Add a read-only factual comparison, not a repair claim or speculative total-stat simulator. |
| Full campaign route | Unverified, explicitly deferred. C12 proves travel, purchase and filling, not the later joined reward/recovery/save loop. | Use short purpose-specific native checks; do not declare the whole campaign balanced when those checks pass. |

## 3. First-hour player journey

These are design checkpoints, not mandatory minute-by-minute instructions.

| Stage | Player experience | Existing support / proposed change | Acceptance |
|---|---|---|---|
| Arrival | Recognize self, nearby danger, supplies and a way to seek help. | Keep glade geometry and supplies. Inspect its actual sign/resident path before adding any new text. | A fresh player can find a truthful nearby lead without coordinates supplied by the audit. |
| Settlement | Choose preparation: water, light, rest, work or affordable equipment. | Sill is the primary current-map candidate; roles and stock must be checked when actually present. Keep Morrowfast as an existing later option. | Offered service belongs to the current living owner; absent/dead/hostile owners do not leave false service promises. |
| Small task | Follow a concrete local instruction and understand what remains. | Repair Ellun/Hallun guidance. Keep existing task IDs, facts, rewards and already-completed state. | The named target matches the lead; Hallun's two objectives and Ellun's single marker task are clear, and completed requirements are not requested again. |
| Expedition choice | Learn about an optional nearby encounter and its likely material reward. | Reuse an existing qualifying site if it already serves this purpose. Otherwise use the first approved rare-encounter site and a truthful sign/dialogue/note connection. | The lead resolves to a current site; no forced generation during every conversation, automatic quest acceptance or guaranteed surviving loot. |
| Encounter | Decide whether to separate enemies, use control, take cover, bypass or retreat. | Three concepts below reuse current actions and terrain. | Threat and retreat route are legible. Starting supplies remain ordinary; death and failure are real outcomes. |
| Reward | Recover the actual visible item or natural product, then compare/use/sell it. | Existing pickup, equipment, harvest and trade. Add only the missing comparison/readout needed to make the choice clear. | Exact source/item ownership, truthful values and normal transaction costs; no hidden bonus reward or duplicate item. |
| Return | See a depleted or changed site and retain the acquired item. | Current saved graph plus the bounded new ownership contract if necessary. | Same encounter/reward state after travel and save/load; no reroll on opening a menu, crossing an edge or loading. |

### Choosing a lead and reward without manufacturing a gap

Begin with three predeclared seeds (1, 64, 1729) and the actual fresh-start fallback
controls. Inventory the starting chunk, Sill and eligible nearby sources without
changing their contents. Record actual roles, offered directions, accessible
gear, prices, approach hazards and missing placements. A source inventory is not
proof of ordinary acquisition; pair it with one short native discovery route.

If a useful existing source and lead already work, mark that subtask complete
and spend the effort on encounter variety. If only a lead is missing, add the
lead. If no suitable earned equipment opportunity exists nearby, connect the
first rare site. Do not automatically add all three kinds of content.

An added equipment reward should be ordinary tier 1 gear with a meaningful
tradeoff, physically carried by a hostile combatant or stored at a legitimate
existing manufactured source. An existing task's cash reward followed by a real
shop purchase also supports an equipment choice, but is not an equipment find.
Food/reagents may be valuable alternative outcomes but do not by themselves
satisfy that choice. Friendly NPCs never become reward targets.

### Concrete first slice: use the content that already exists

1. Contextualize only the actual glade Warden. Offer a geographic direction to a
   real current-map settlement; for an ungenerated destination do not promise a
   particular living merchant, bed, stock item or safe route. Preserve the current
   signpost. A new startup hint or optional note is conditional on observed need.
2. Prefer dynamic Ellun text derived from the actual quest marker and current
   speaker/zone. Keep a truthful unavailable response for missing/moved/foreign
   markers. Constraining fresh placement east is the alternative only if it
   preserves semantic owners and also handles existing saves honestly.
3. Clarify both Hallun objectives in offer/status/report wording. Preserve
   pre-acceptance completion, kills by other actors, refusals and one-time rewards.
   The journal already displays the second objective; this is not a proven lockout.
4. Verify the smaller existing Ellun route first: glade lead → Sill → marker →
   actual reward → a real gear or preparation choice. Ellun currently gives 120 XP
   and 40 drams; Hallun gives 150 XP and 60 drams. Prices remain current whole-stack
   quotes. Mixed starting and earned money does not prove the quest alone funded
   a purchase. Rental items do not count as acquired saleable rewards.

Keep `WorldMap.StartingZoneID` at Sill 10.10: it identifies starter-town content,
not the new preferred player placement. Keep Farra's existing 2.6 → 3.6 expedition
and hospitality/refusal semantics. Neither coordinate should be mechanically
replaced with 11.10.

## 4. Rare encounter design

Three core concepts are proposed; names are working design labels, not new
canonical people. A fourth melee duelist is deferred because it adds little
beyond the coordinated pair. Their detailed source/kit contracts follow below.

### E1 — The ditch-cutters: coordinated raiders with readable gear

- **Purpose:** separate a coordinated pair, judge a two-cell reaching strike and
  choose between offensive and defensive equipment. This is the first content
  candidate if the existing first-hour route lacks an earned gear opportunity.
- **Source:** a fresh ordinary Spread field boundary/old-road ditch, away from
  glade, settlements, authored sites and reserved exits. Use an open side approach
  and retreat corridor; do not turn a compulsory exit into a toll fight.
- **Actors:** two original marlback role variants using the existing tier 1
  Scrabbler baseline, with no HP/AV multiplier. Leader visibly carries a tier 1
  ShortSword and uses the existing two-cell, 25-turn Lunge. Companion carries a
  Cudgel; exactly one actor wears a guaranteed tier 1 LeatherCap (AV 1, Commerce 8).
  Treat this as a replacement group within the existing population budget,
  not two extra enemies on top of a guaranteed group.
- **Decision:** isolate the leader, draw out its real cooldown, use ordinary
  control or approach from the open flank. Local assistance remains opt-in,
  same-faction and visibility/radius constrained. Preserve retreat behavior.
  Lunge attacks without moving; it is not a charge. Start with proposed eligible
  action chance 35%, assist radius 4 and retreat below 35% HP, then verify the
  interaction. Clear inherited random loadout picks so this authored kit cannot
  accumulate another weapon/armor bounty. Its listed gear value is 31 Commerce
  before ordinary death-table rolls, not guaranteed sale proceeds.
- **Reward:** the actual sword/cudgel/cap entities can drop through normal death
  spill or transfer. The cap supplies a clear head-slot choice; do not add a
  shield, armor bounty, enhancement, unique artifact or hidden completion grant.
- **Appearance:** use the approved low, broad marlback anatomy and current
  equipment sockets. Add distinct plate/rake marks and posture/material details;
  no canine/Qud silhouette, no color-only distinction, no duplicate baked weapon.
  Every new blueprint needs an explicit approved model binding.
- **Discovery:** a bounded description/lead names a real ditch site and visible
  carried gear. Describe an opportunity, not a promise that loot is still there.
- **Balance gate:** ordinary starting character must have a legible avoidance
  route and viable responses. Measure actual retaliation and any extra assistance;
  unchanged base stats alone do not establish unchanged difficulty.

**Sweep correction:** Shank was considered and rejected: its bonus requires a
negative status on the target (`ShortBlades_Shank.cs:118–130`). A new dagger
nickname without a supporting condition would not establish a distinct tactic.
Use the actual LongBlades Lunge eligibility, targeting and cooldown contract.

### E2 — The chalk-ring viper: a warned natural ambush

- **Purpose:** reward attention to a physical warning and route choice; contrast
  an ambush with the coordinated, visibly armed encounter.
- **Source:** a fresh hedge corner or overgrown boundary with a passable dry
  alternative. Keep the warning on the visible approach; no invisible required
  crossing, unique quest target, settlement, protected beast or loot chest.
- **Actor/mechanic:** a local Viper variant with current stats/bite, using the
  existing ambush goal only after its wake/save lifecycle is verified. Do not
  increase poison potency or add a new disease, tracking or stealth framework.
  Proposed sight radius 3 also limits its waking Brain sight; do not silently
  promise longer pursuit. The existing bite can apply 1d6 poison for eight turns
  at 75% chance. An 8 HP creature is not necessarily a gentle starter encounter.
- **Decision:** notice the warning, bypass, wake from a chosen position, retreat
  or fight with existing controls. The warning must not reveal hidden actors
  through fog or pretend that an unseen creature is currently alive.
- **Reward:** retain the actual natural corpse/harvest product and existing
  75% VenomGland chance. No guaranteed manufactured gear inside the animal. This
  encounter does not satisfy the finished-equipment objective by itself.
- **Appearance:** approved viper anatomy with a clear chalk-colored head/body
  marking and readable coiled silhouette. Verify sleeping/awake/moving/attacking
  state against actual available animation clips; do not invent an unsupported
  animation state and call its binding complete.
- **Persistence gate:** AIAmbush has private `_dormantPushed` state while the
  generic save path stores public fields. This is a lifecycle hypothesis needing
  an executed RED, not yet a confirmed wake-reset bug. Test asleep→save→load and
  awake→save→load, old ordinary vipers and goal-stack ownership before adopting it.

### E3 — The drain slime: an optional ranged environmental threat

- **Purpose:** line of sight, range and a dry bypass beside a wet ditch provide
  a different decision from the first two situations.
- **Source:** an optional deeper exploration branch within eligible Spread
  wilderness. Never guarantee it beside the fresh start, required village
  approach, quest marker or mandatory crossing.
- **Actor/mechanic:** use the existing slow CaveSlime body and actual AcidSpray
  eligibility/cooldown; no new ability or stat multiplier. Give the local variant
  a material/weathered appearance consistent with the Spread. Existing scenery
  provides line-of-sight interruption only where it actually blocks the ray.
  Proposed eligible-action chance is 60%, with existing range 4/cooldown 10.
  Require a real RiverMeadow wet bank, a dry unreserved body footprint and a dry
  bypass. Do not clear SpreadComposition's water or approach reservations.
- **Decision:** avoid the branch, use a verified sight break, attack from a
  favorable position, control it or withdraw before entering range. Do not
  promise that retreat or ordinary water removes an already-applied effect.
- **Reward:** existing natural BogSap harvest, with normal finite corpse
  ownership. No manufactured creature loot or guaranteed special equipment.
- **Appearance/readout:** use the approved slime anatomy with a distinct silt
  fringe and visible cast/hit state. Acid warning must be readable before exposure
  and distinguish the active effect from a harmless green visual.
- **Mandatory risk gate:** AcidSpray applies Acidic(0.8), whose initial tick is
  4 typed Acid damage with 0.05 decay (`AcidicEffect.cs:56–75`), in addition to the
  attack. Calling it merely a small 1d4 projectile would understate the risk to a
  40 HP starter. Measure full duration/counterplay in a short native encounter
  before approving its first-hour role. No silent global acid nerf to make this
  proposal pass. If the encounter is unsuitable or costly to repair, defer E3
  and ship E1/E2 without claiming all three complete.

### Shared source, rarity and persistence contract

These are authored design targets to validate, not measured final rates:

- Start with a small explicit definition list for E1/E2/E3, not arbitrary
  legendary templates. Keep `LegendaryLairEncounters` and its tier restrictions
  unchanged. These local variants are not new fixed lore people.
- Choose eligible addresses deterministically from the real map without generating
  every destination. Require actual Spread/tier 1/surface/no POI; exclude the
  glade, sinkhole mouths, authored/reserved sites and other owned content.
- Provisional first release: at most one site of each type, hence three selected
  encounter sites per world; no adjacent sites. A selected site can be absent if
  placement is invalid. Record refusal, do not force terrain/owner deletion or
  repeatedly reroll. Seeded source generation must not consume combat RNG.
- If M0 establishes a need for an early E1 opportunity, select its valid site
  within a proposed two-map-step neighborhood of the **actual** fresh start.
  This is one optional earned opportunity, not a guaranteed safe victory/item.
  Test candidate availability before accepting the radius; never silently expand
  the search or overwrite an existing special site to satisfy a quota.
- E2/E3 remain optional regional discoveries. The three concepts must not become
  a required checklist for every campaign or an encounter in every chunk.
- Replace the exact ordinary `SpreadTier1Encounter` group only after a complete
  rare placement preflight succeeds; keep separate ambient rows. Current table
  rolls return blueprint strings and cannot express this spatial mixed pair.
  A narrow generation-only handoff is needed; never delete already live enemies
  or mutate shared tables to fake replacement. A refusal restores the ordinary
  group. Inspect final population after all builders. This new placement seam's
  ordering must respect reservations, actual body footprints, routes and final
  generation acceptance. No double population on failed/retried generation.
- Cached non-POI graphs plus ordinary save serialization support normal return.
  Freeze the selected addresses/version for a new world before exploration;
  revalidate eligibility on generation without selecting replacement addresses
  after map mutations. Use existing serializable world/entity primitives for
  this bounded metadata, without a general encounter ledger. Preserve old cached
  zones; do not backfill a missing plan into an old save as an implicit migration.
  A versioned empty new-world selection is an initialized result, distinct from
  an old save with no plan; neither may initialize or reroll on later load/entry.
  Examinations, renders, conversations and repeated entry
  must never initialize rewards, reroll a site or recreate a dead owner.
- The default retention promise covers ordinary travel and save/load. Explicit
  administrative `UnloadZone` still discards/regenerates an ordinary graph; these
  variants are not globally unique lore owners. Do not add a retention hook now.
  If M0 finds a real player-facing wilderness unload path, revisit this decision
  with a reproduced case and at most three retained graphs. Never retain all
  Spread chunks or change settlement regeneration to address a hypothetical path.
- Validate actor IDs, current graph, inventory/body/Physics backlinks and actual
  reward IDs. A disarmed, looted, removed or dead owner must not advertise a
  reward it no longer carries. No new equipment entity is manufactured on return.

The cap and near-start radius are deliberately provisional. M0's source census
must report eligible/selected/placed/refused counts and travel/discovery distances.
Low encounter visibility is a possible design problem; a passing source test is
not proof that three sites across 142 surface chunks feel frequent enough. Adjust
the bounded contract explicitly if evidence warrants it, preserving reward value
and hostile budgets.

## 5. Readability plan

### R1 — Reproduce and repair actual text fit

The sidebar uses 34 columns, 31 content columns and a 0.5×1 text grid
(`ZoneRenderer.cs:53,466`; `SidebarRenderer.cs:157–175`). Its text sprites are
8×16/PPU 16 (`CP437TilesetGenerator.cs:684–685,752–780`), so a global font-size
replacement is not justified. `GameplayViewportLayout.cs:114–176` and the
dedicated camera in `CameraFollow.cs:539–578` determine final bounds.

Record final camera pixel rectangle, aspect, position/orthographic size, grid
transform, actual rendered strings and projected sprite corners. Compare these
with the retained 1920×1080 frame. Distinguish a true outside-camera glyph from
normal wrapping or intentional log history omission. Then change only the
reproduced padding/layout/invalidation defect; preserve world framing and style.

Existing `SidebarRendererTests.cs:37` tests the left edge only. Add paired
right/top/bottom extent and transition checks; a screenshot alone cannot identify
the faulty formula. Investigate the cache's aspect/size-only invalidation as a
hypothesis, not an established cause.

Display contract: desktop 1920×1080 is the configured and observed baseline;
web 960×600 is a configuration value, not verified UI support. Do not promise new
minimum/DPI/portrait/ultrawide support. Use smaller 16:9 and narrow/wide aspects as
diagnostic probes; any new supported floor requires explicit design and native
acceptance. Keep the independent 80×45 popup coordinate system intact.

### R2 — Complete, recoverable world examination

`ExaminablePart.cs:55–75,94–145` writes newline-separated detail to MessageLog.
`SidebarTextFormatter.cs:233–285` does not split explicit newlines, while
`AnnouncementUI.cs:53–115,194–230` already handles paragraphs and pagination
for inventory Examine. Reuse that reader for the currently validated world
target. Preserve useful paragraph boundaries rather than adding another renderer.
Owner effects alone are insufficient: separately snapshot and label the actual
`CellStatusReadout.GroundLine` for the selected cell so clipped ground hazards
are also recoverable. Use its current visibility/ownership rules, not a new scan
that exposes hidden terrain or hazards.

Keep a concise log summary if appropriate, but provide the complete authoritative
description in the reader. Define it as a read-only snapshot while open; reopening
uses fresh state. No action executes from a stale description. Preserve exact
owner selection, range/payment rules, free inspection, keyboard dismissal and
return to Look/gameplay. The solved terrain owner picker remains unchanged.

`WorldActionMenuUI` caps status at six rows and directs overflow to Look, whose
Focus is also capped. Provide a real full-detail route instead of that circular
fallback. Compact action labels may retain an omission marker if the full selected
label, reason and status can be recovered. Do not silently omit a scald warning.

### R3 — Factual equipment comparisons

This is a new convenience feature. Reuse `ItemExamineService.cs:20–147` and
the actual equipment/body identities in `InventoryScreenData.cs:292–335`.
Show candidate and currently equipped item names plus relevant contributions:
damage **per penetration**, hit/penetration modifiers, strength cap, item AV/DV,
weight, speed penalty, slots and conditional bonuses.

List actual displaced items, including two-hand conflicts. Distinguish an empty
worn slot from a hand with a real default natural attack. Unknown/conditional
effects remain labeled. Show the existing unavailable reason for an incompatible
slot or owner. Revalidate ownership and conflicts before a real equip action.

Do not temporarily equip the candidate, invoke damage, consume RNG or simulate
the character to calculate a preview. Do not claim aggregate DV/AV/DPS or a
universal “better” score from item contributions. Preserve existing confirmation
and transaction logic; the reader is informative, not an alternative equip path.

### R4 — Threat, hazard and availability wording

Keep actual stance (`hostile`, `friendly`, `neutral`, `pacified`), observable
mechanics and action availability separate. `LookQueryService.cs:197–215,257–276`
already handles pacification; retain its expiry refresh. Put consequential current
status and the encounter's truthful behavior cue before flavor, with full detail
through R2. Do not equate neutral/pacified with safe or invent a relative power
rating, hidden cooldown knowledge, future AI intention or unseen weakness.

World geometry, actual hot SteamEffect, visual-only steam cloud, liquid ownership
and finite resource state must stay distinguishable. Use the actual cell/status
readout and live effect description; no second hazard formula in UI code.

### Readability acceptance matrix

| Case | Required result |
|---|---|
| Normal | Vitals, selected owner, key warning and useful action fit with a visible margin. |
| Long | Long name/enhanced item/many statuses preserve identity and all consequential text through wrapping/pagination; first and last content are reachable. |
| Unavailable | Missing/removed/stale target, unavailable living service, wrong owner, lock, range or incompatible slot gives a truthful reason; never substitutes a same-name object. Valid corpses and remaining objects stay examinable. |
| Changed | Cooling source, Calm expiry, opened cache, depleted harvest, changed equipment and smaller content refresh without stale rows or stale actions. |
| Empty | No focus/log/actions/equipment slot remains readable and dismissible; no invented hazard or zero-filled unknown stat. |
| Transition | Open/close Look, reader, inventory, compare and world map restore the right camera/input state without changing game time or items. Loading restores the selected save's actual state, which may legitimately differ from the current session. |

## 6. Implementation order, dependencies and deliverables

The table defines milestone acceptance; execution status is in §10. Effort bands are engineering estimates,
not delivery promises: small = roughly half to one focused developer day;
medium = one to three; large = three to five. They include focused tests and
review, but not an unknown editor/import outage. A failed gate can reduce scope.

| Milestone | Deliverable and principal files | Depends on | Exit condition | Effort / priority |
|---|---|---|---|---|
| M0 — Establish the actual route | Record three fixed-seed nearby-source inventories, one ordinary native discovery attempt, actual service/reward ownership, and measured sidebar bounds. Use existing scenarios/readouts; keep evidence under `Docs/Verification/SpreadFirstHour/` when implemented. Record selected-source policy and a minimal save-metadata carrier before coding it. | Current source/evidence baseline | Existing useful opportunities, misleading directions and remaining unknowns are distinguished. A concrete gear/lead gap is either demonstrated or explicitly rejected. No failed audit policy is relabeled a game bug. | Small; first |
| M1 — Make existing work discoverable | Narrow glade Warden dialogue; actual-marker Ellun direction; Hallun's complete remaining objectives. Main surfaces: `LocalPeople.cs`, `RegionalGuidance.cs`, the three conversation JSON files and, only if required, the current storylet context/marker resolver. | M0 | Fresh and saved-context leads are truthful; rewards/IDs unchanged; genuine village Wardens and other canonical quests still work. Short Ellun route reaches a real preparation/equipment choice. | Medium; highest player impact |
| M2 — Make descriptions fully readable | R1 measured layout repair and R2 authoritative full world reader. Main surfaces: `GameplayViewportLayout.cs`, `SidebarRenderer.cs`, `SidebarTextFormatter.cs`, `ExaminablePart.cs`, `WorldActionMenuUI.cs`, `AnnouncementUI.cs` and their current input routing. Change `CameraFollow.cs` only if the reproduced defect requires it. | M0 measurement; may run beside M1 | No clipped consequential text at accepted resolution; complete current-owner detail reachable; free reading and modal/camera restoration proven. | Medium; high |
| M3 — Explain the equipment choice | Add a small pure comparison projection alongside `ItemExamineService.cs`/`InventoryScreenData.cs`, exposed through the existing inventory reader in `InventoryUI.cs`. Reuse real slot/conflict validation; do not rewrite equip. | M2 reader contract | Candidate/current/displaced items and conditional contributions are accurate, including empty/multiple hands and unavailable choices. Opening/reopening changes no game state. | Medium; high |
| M4 — Ship one complete rare encounter | New `SpreadRareEncounterPlan.cs` and `Builders/SpreadRareEncounterBuilder.cs`; narrow ordinary-Spread manager/population integration; E1's two exact blueprints, actual gear, current-style bindings/corpses and truthful lead. Source/reward metadata uses the existing save primitives selected in M0. | M0 source decision; M2/M3 before final player acceptance | Group replacement, safe optional placement, real tactics, real item acquisition/comparison, visual ownership, travel and save return pass. Unselected sources retain original population/RNG behavior. | Large; first new encounter |
| M5a — Add the warned viper | E2 template, one blueprint, authored appearance/explicit corpse mapping. Conditional minimal `AIAmbushPart.cs`/`DormantGoal.cs` compatibility repair only after a failing lifecycle test. | M4 common source; wake/save preflight | One-shot wake survives save/load; ordinary vipers remain unchanged; visible warning and bypass work; actual poison/finite natural harvest are truthful. | Medium with save risk; independently deferrable |
| M5b — Add the drain slime | E3 habitat template, one blueprint, authored appearance/explicit corpse mapping; existing AcidSpray executor. | M4 common source; short acid/counterplay gate | Dry placement/bypass and actual sight interruption work; full acid aftermath is acceptable for the optional role. No global acid rebalance hidden in the change. | Medium with high balance risk; independently deferrable |
| M6 — Integrate and publish evidence | Focused acceptance matrix, current-map selection census, inspected screenshots, exact-save mutation/restore, final cold review and living-doc status. Update this plan plus the existing completion/follow-up ledger. | Each milestone being shipped has met its own exit | Every shipped claim has current evidence; conditional omissions are named; appropriate integration sweep has no new failures. | Medium; required per shipped scope |

Recommended order is **M0 → M1/M2 → M3 → M4 → M5a/M5b → M6**. M1 and M2
may proceed independently with separate file ownership. M3 depends on the reader
contract. E1, E2 and E3 share the same blueprint/model allowlists and source builder,
so integrate them serially rather than allow competing broad JSON or registry
edits. E1 can ship while either beast is deferred; do not call that all three done.
Plan roughly 10–24 focused developer days for the full scope, with M5 uncertainty
called out rather than folded into a false precise schedule. Re-estimate after M0.

### Exact implementation boundaries

- **Dialogue:** prefer a narrowly bound glade context over altering every Warden.
  Resolve current task marker/owner state, not a second hardcoded direction table.
  Do not rewrite quest rewards, acceptance flags or the settlement identity.
- **Generation:** add the selector under `Assets/Scripts/Gameplay/World/Generation/`
  and its builder under `Builders/`. Integrate only the ordinary Spread/no-POI
  branch of `OverworldZoneManager.cs` and the smallest required handoff in
  `PopulationBuilder.cs`. Keep `PopulationTable.cs`'s global weights and other
  biomes intact. Stage complete fresh actors/gear, validate callbacks/footprints,
  then commit; failed required content leaves the ordinary population intact.
- **Content:** proposed exact child IDs are `SpreadHurdleCutter`,
  `SpreadDitchMate`, `SpreadLatchcoil` and `SpreadDrainSlime`. These are provisional
  identifiers until the source sweep confirms no collision. Use surgical additive
  blocks in `Assets/Resources/Content/Blueprints/Objects.json`; parse before/after
  and compare resolved objects. No whole-file reserialization or loot-table bump.
- **Art:** explicit living and corpse-family mappings are required. Inspect
  `SpreadBiomeActorLibrary`, `SpreadVisitorCreatureSource/Library`,
  `SpawnRing3DRecipes/Catalog` and `SpreadPortableRecipes`; extend exact IDs with
  approved source variants. Reuse actual Idle/Walk/Interact/Attack/Hit clips and
  equipment sockets. No invented death clip, generic fallback, duplicate baked
  weapon or borrowed-prefab mutation. Four blueprint IDs do not mean four new
  rigs. Budget real model generation, import and in-game inspection in M4/M5.
- **Equipment:** existing cap/sword/cudgel fits should require no new
  `Village3DEquipmentViews` fit behavior. Demonstrate current Body/Inventory/Physics
  ownership and exact adopted mesh; test held→dropped→equipped and corpse states.
- **Pins/docs:** update exact natural-weapon roster/model coverage pins when new
  child creatures are added. Keep positive controls; do not replace exact lists
  with permissive counts. Every new file under `Assets/` receives a fresh `.meta`.

## 7. Verification plan

### Test-first sequence for each implementation slice

Follow `CLAUDE.md`: write the small failing behavior test, execute and retain the
RED result, implement the minimum change, run the paired counterchecks, then do
the cross-feature sweep and cold review. A source hypothesis is not RED evidence.
For new content, the initial failure may be missing reachability, mapping or
ownership behavior; do not assert only that a new string exists in a JSON file.

Use current fixtures where they own the behavior; proposed new fixture families
are `SpreadFirstHourGuidanceTests`, `SpreadRareEncounterTests`, their adversarial
companions, and `EquipmentComparisonTests`. Extend the existing sidebar/camera,
examine-layout, RegionalGuidance, inventory/equipment and approved-art suites.
These names are planned, not files already created by this document.

| Feature | Positive witness | Counter/adversarial witness |
|---|---|---|
| Glade lead | Actual glade Warden gives current settlement direction without imaginary local services/reward. | Village Warden unchanged; wrong/foreign owner, absent destination, cached dead service, custom map and old save remain truthful. |
| Ellun/Hallun | Actual marker direction and all remaining objectives; one legitimate completion/reward. | Marker on another side/missing/moved/foreign; pre-accepted completion, gremlin killed by someone else, refusal, repeated reporting and already-rewarded save. |
| Full reader/layout | Measured glyph corners fit; paragraphs, owner effects, separately labeled visible ground status and first/last page survive long detail; actual chosen target opens, including a valid corpse. | Empty/shorter text, last page after content shrinks, resize/layout transition, unsupported glyph, removed/stale same-name target, hidden ground state, modal close/input/camera restoration; no turn, RNG or inventory mutation. |
| Comparison | Correct actual slot, candidate/current contributions and all displaced gear. | Empty hand versus natural attack; two-hand conflicts, wrong body/owner, nonwearable item, conditional/unknown contribution, removed item while reading; no temporary equip or simulation. |
| Rare selection | Stable selected addresses/version and real current-map eligibility independent of entry order; a versioned empty selection round-trips as empty. | Wrong biome/tier/POI/sinkhole/glade/authored source; static-map disagreement; zero eligible sites; adjacent/duplicate choices; map mutation; old saved zone and old save with no plan. No repeated-entry, load or read-only initialization/reroll. |
| Physical placement | Complete group replaces one ordinary group on reachable legal cells with visible approach and escape. | Reserved route/water/full footprint, blocked approach, foreign factory object, missing gear, callback mutation, partial placement. Refusal preserves ordinary group; unselected RNG/ambient controls match. |
| Pair tactics | Actual equipped sword permits Lunge; willing visible mate assists; genuinely injured owner retreats with possessions. | Disarmed/wrong-class weapon; wall/ally/invalid ray; cooldown; occluded/dead/foreign-faction/party/unwilling ally; Calm suppresses fighting until expiry; healthy owner does not falsely flee. |
| Viper lifecycle | Resting warning, hostile/damage wake, awake save/load, real bite/finite natural harvest. | Sleeping and pending-wake save states; old unmodified viper; no rearmed awake actor; hidden/removed owner; failed harvest stays failed; no manufactured item reward. |
| Slime/corrosion | Actual legal ray/cast and ongoing acid ticks; real dry bypass and finite BogSap harvest. | Sight blocker, range, cooldown, invalid source and no additional tick after actual effect expiry; no assumed water cure or invulnerable survival witness. |
| Ownership/save | Same actual actor/item IDs through death/drop/pickup/equip, flee, travel and save/load; depleted site stays depleted. | Disarm/theft, stale backlinks, consumed/foreign corpse, repeated load/entry, unrelated owner with same name; ordinary admin-unload limitation documented. |
| Graphics | Each exact new living/corpse ID has approved anatomy; current equipped item and actual attack/walk render correctly. | Fog, memory, removal, zone change, wrong backlink, carried-but-not-equipped item, dropped item and malformed mapping; no glyph fallback or stale duplicate view. |

If introducing a probability parameter, force its 0% and 100% boundary cases in
unit tests as well as a deterministic ordinary sample. Do not change production
chances, RNG or cooldowns merely to force a native witness. Validate data via the
existing blueprint/registry validators, adding `LootTableRegistry.Validate` only
if a later justified slice actually edits loot data.

### Bounded native checks

Run these only during implementation, after focused automated checks pass:

1. **Ordinary discovery and existing task:** use ordinary New Game and a recorded
   unmodified seed/kit. Find the sign/Warden lead, travel to the actual settlement,
   complete the smaller local task when available, and make a real service/gear
   choice. Record directions seen, owner IDs, starting/earned/spent money and any
   unavailable placement. This proves only the route actually completed.
2. **E1 and gear:** approach a naturally selected site using its in-world lead;
   inspect, fight/control/retreat under actual rules, and acquire/compare/equip or
   sell a real item if legitimately obtained. Observe Lunge/assistance with their
   actual chance/cooldown; a valid refusal is not an execution witness. A separate
   labeled direct setup can isolate a behavior but cannot prove discovery/pacing.
3. **UI state:** inspect normal and long owner/status/item descriptions, first
   and last pages, empty comparison slots and an unavailable action at 1920×1080.
   Capture actual bounds and screenshots; open/close/reopen to prove input and
   camera restoration. Inspect every retained image before claiming acceptance.
4. **Return and save:** save a changed/looted site and acquired item, perform one
   legitimate state mutation, load that exact save, then revisit. Compare actual
   graph/item/slot/depletion/cooldown identities. Restore the user's original save
   and editor configuration after evidence capture; no test artifacts as progress.
5. **Each accepted beast:** warning→inspect/bypass→one genuine attack/status
   sequence and aftermath, plus legitimate corpse/harvest if obtained. Do not
   stop the acid witness at projectile impact or grant resistance/HP. Failure is
   evidence for deferral, not permission to replace the native gate with a gallery.

Do not silently reseed, teleport, change reputation, grant items, force hits or
clone a reward to complete an ordinary-play claim. Preserve any necessary setup
shortcuts as explicitly narrower evidence. One successful route cannot establish
all-seed balance, global service availability, or first-hour pacing for everyone.

Use focused Unity EditMode suites for changed areas, then one integration sweep
after the integrated code/data/art settle. The inherited 20,068-pass result is a
baseline only. A standalone runner can support core logic/data checks but cannot
prove Unity import, renderer, camera, input or actual Play-mode behavior. Compare
its failures against an equivalent clean baseline; never use the raw failure
count as acceptance. Broaden/repeat tests only for new changes or unresolved risk.

### Evidence and living documentation

Record commit/source hash, Unity version, scene, seed, ordinary/custom start,
actual route, selected/placed/refused counts, precise assertions, screenshots,
test result paths and save/scene restoration. Keep native acquisition, isolated
mechanic checks, screenshots and deterministic source census distinct. Write
failures/limits beside successes. Update this plan and the relevant completion
ledger in the same future implementation commit; follow the §2.3 commit template.

## 8. Performance, risk and stop rules

Follow `Docs/PERF-FOUNDATION.md`. Address selection and placement belong in cold
world generation; no per-frame world scan, full-map generation, repeated blueprint
parsing or asset lookup in UI. Store only the bounded selection metadata needed
for the contract. Read-only directions must not generate distant zones.

Reuse current dirty-state/snapshot UI patterns. A comparison builds from the
current player/item/slot when requested; it does not simulate every inventory
item each frame. Snapshot inspection does not need live combat polling. New visual
variants follow current view ownership, cache invalidation and fog scopes. If a
change touches a hot path, retain a 60–90 second representative native capture
of movement, dense views and repeated open/close actions; compare allocations and
frame behavior to baseline. No performance claim from an empty gallery alone.

| Risk | Importance / response |
|---|---|
| Wrong lead or promised nonexistent reward | High: directly breaks the exploration loop. Fix before adding more leads. |
| Recreated gear/duplicate canonical quest or save damage | High: stop that slice, retain reproducer and fix ownership/compatibility before shipping it. |
| Unreadable danger, modal lock or old-style fallback for new content | High: blocks informed play or violates accepted art scope; isolate with a small reproduction. |
| Acid/poison too punishing for an optional early branch | High for that encounter, not proof of a global effect bug. Change its scoped role/placement or defer it; do not quietly nerf all combat. |
| AIAmbush needs broad goal-save repair | Medium feature, potentially high systemic cost. Keep the failing witness and defer E2 if a narrow compatibility repair is not enough. |
| Three sites too rare or radius has no valid habitat | Design uncertainty: inspect selection/refusal/discovery distances; revise one explicit cap/radius policy, not every loot table. |
| Hypothetical manual-unload farming | Low until an ordinary production path is shown. Preserve the stated boundary instead of adding global retention. |
| Dog fetch, exhaustive campaign bot or incidental art polish stalls | Low unless a new concrete blocker appears. Record repro, severity and next step; continue the current high-value milestone. |

After two unsuccessful repair approaches or roughly 20–30 minutes with no new
evidence, step back. Record the player impact, reproducibility, affected scope,
estimated remaining cost and next useful experiment. Continue only if it blocks
safe saves/ownership, core progress, truthful information or this slice's actual
acceptance. Otherwise defer it by name and move to an independent milestone.
This is a reassessment trigger, not permission to ship a known critical defect.

### Explicit deferrals

- Fourth rare melee encounter, new factions, general rarity/affix framework and
  broad loot/economy rebalance.
- Full C12 campaign automation, dog-fetch delivery demonstration and unrelated pet
  polish; current accepted core evidence remains separately recorded.
- New biome rollout, global art rework, unsupported-resolution guarantees, general
  font replacement, fire combustibility normalization and normal-play tinkering.
- Global acid/poison changes, all-AI save refactor or global zone retention merely
  to make one optional concept fit.
- Protected mystery answers, new divine actors, duplicate canonical quest hosts,
  and retroactive replacement/restocking of old serialized entities.

## 9. Source and evidence map

Short filenames above resolve to these current repository paths. Line anchors
refer to `0417e145` and will move during implementation; search named symbols then.

| Area | Primary paths / anchors |
|---|---|
| Rules and status | `CLAUDE.md` §2.3 and Q1–Q4; `ADVERSARIAL_TESTING.md`; `Docs/DENSITY-COMPLETION-PLAN.md`; `Docs/DENSITY-COMPLETENESS-FOLLOWUP.md`; `Docs/DENSITY-CAMPAIGN-ACCEPTANCE.md`; `Docs/Verification/DensityCompletion/Integration/PostContent/` |
| Start/route | `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs:25,839,1323`; `Assets/Scripts/Gameplay/Bootstrap/FreshGameStart.cs:12`; `Assets/Scripts/Gameplay/Bootstrap/FreshGamePlacement.cs:12`; `Assets/Scripts/Gameplay/Bootstrap/NewGameLoadout.cs:23`; `Assets/Scripts/Gameplay/World/Generation/ReferenceGladePlan.cs:11,49`; `Assets/Scripts/Gameplay/World/Generation/Builders/ReferenceGladeBuilder.cs:60`; `Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:212`; `Assets/Scripts/Gameplay/World/Map/WorldMap.cs:82`; `Docs/DENSITY-SPREAD-START.md`; `Docs/CHUNK-GAMEPLAY-IMPLEMENTATION.md` |
| Guidance/services | `Assets/Scripts/Gameplay/Conversations/RegionalGuidance.cs:38,60`; `Assets/Scripts/Gameplay/Conversations/LocalPeople.cs:167`; `Assets/Scripts/Gameplay/Entities/RegionalSignpostPart.cs:15`; `Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs:120,847,927,967,978`; `Assets/Resources/Content/Conversations/Wardens.json:285`; `Assets/Resources/Content/Conversations/BMO_Quest.json:8,37,94`; `Assets/Resources/Content/Conversations/RootBeerGuy_Quest.json:8,37,94`; `Assets/Resources/Content/Data/Storylets/RootBeerGuyCase.json:19`; `Docs/REGIONAL-GUIDANCE.md` |
| Content/rarity | `Assets/Scripts/Data/Tables/PopulationTable.cs:100,360`; `Assets/Scripts/Gameplay/World/Generation/Builders/PopulationBuilder.cs:43`; `Assets/Scripts/Gameplay/World/Generation/LegendaryLairEncounters.cs:54`; `Assets/Resources/Content/Blueprints/Objects.json:612,6243,7305,13287,14316`; `Assets/Resources/Content/Data/Loot/LootTables.json:519,577,765,931`; `Docs/LOOT-FINDS.md`; `Docs/DENSITY-LEGENDARY-LAIRS.md` |
| Combat/ambush | `Assets/Scripts/Gameplay/AI/CombatTacticsPart.cs:25,65,144,190`; `Assets/Scripts/Gameplay/AI/AIAmbushPart.cs:56,69`; `Assets/Scripts/Gameplay/AI/Goals/DormantGoal.cs:31,41`; `Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs:48`; `Assets/Scripts/Gameplay/Skills/LongBlades_Lunge.cs:46,114`; `Assets/Scripts/Gameplay/Skills/ShortBlades_Shank.cs:118`; `Assets/Scripts/Gameplay/Skills/Corrosion_AcidSpray.cs:18`; `Assets/Scripts/Gameplay/Effects/Concrete/AcidicEffect.cs:37,56`; `Assets/Scripts/Gameplay/Effects/Concrete/PoisonedEffect.cs:33` |
| Habitat/persistence | `Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs:26,114`; `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadCompositionBuilder.cs:32`; `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:126,1088,1304`; `Assets/Scripts/Gameplay/World/Map/ZoneManager.cs:44,116`; `Assets/Scripts/Gameplay/Save/SaveSystem.cs:897,1479,1857,1913` |
| Readability | `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs:53,466`; `Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs:114`; `Assets/Scripts/Presentation/Rendering/SidebarRenderer.cs:140,157,201`; `Assets/Scripts/Presentation/Rendering/SidebarTextFormatter.cs:233`; `Assets/Scripts/Presentation/Rendering/CP437TilesetGenerator.cs:684,752`; `Assets/Scripts/Presentation/Cameras/CameraFollow.cs:539`; `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs:55`; `Assets/Scripts/Presentation/UI/AnnouncementUI.cs:53`; `Assets/Scripts/Presentation/UI/WorldActionMenuUI.cs:40,341`; `Assets/Scripts/Gameplay/Items/ItemExamineService.cs:20`; `Assets/Scripts/Gameplay/Inventory/InventoryScreenData.cs:292`; `Assets/Scripts/Presentation/UI/InventoryUI.cs:1129,2696`; `Assets/Scripts/Gameplay/Look/LookQueryService.cs:197`; `Assets/Scripts/Gameplay/Look/CellStatusReadout.cs:61` |
| Art/performance/lore | `Assets/Scripts/Presentation/Rendering/SpreadBiomeActorLibrary.cs:54`; `Assets/Scripts/Presentation/Rendering/SpreadVisitorCreatureSource.cs`; `Assets/Scripts/Presentation/Rendering/SpreadVisitorCreatureLibrary.cs:85`; `Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs:592`; `Assets/Scripts/Presentation/Rendering/SpawnRing3DCatalog.cs:58`; `Assets/Scripts/Presentation/Rendering/SpreadPortableRecipes.cs:195,322`; `Assets/Scripts/Presentation/Rendering/SpreadEquipmentRecipes.cs:73`; `Assets/Scripts/Presentation/Rendering/Village3DEquipmentViews.cs`; `Docs/DENSITY-SPREAD-PRESENTATION-SCOPE.md`; `Docs/DENSITY-ORIGINAL-ENEMY-ART.md`; `Docs/ORIGINAL-ENEMY-REPLACEMENT-AUDIT.md`; `Docs/PERF-FOUNDATION.md`; `Lore/MYSTERY-LEDGER.md`; `Lore/Voices/VOICE-CARDS.md` |

## 10. Planning review and implementation status

- **Q1 — Symmetry:** leads versus missing/moved owners; sleeping versus awake
  saves; equipped versus disarmed gear; selected versus refused sites; long versus
  empty/shortened text; real hazard versus visual-only effect are paired above.
- **Q2 — Cross-feature consistency:** reuse canonical stories/services, ordinary
  gear/tactics, current map authority, existing save graphs and approved art. No
  global rarity, generation, UI or ownership substitute is proposed.
- **Q3 — Counter-check completeness:** every consequential claim has a negative
  or refusal case. Native discovery, isolated combat, graphical correctness and
  all-seed balance remain distinct claims with different evidence requirements.
- **Q4 — Doc versus implementation:** current mismatches, hypotheses, proposed
  additions and deferred work are labeled. This document claims no new test pass,
  repaired feature, generated asset, native route or completed encounter.

Planning review completed with independent route, combat/source and presentation
reads. Corrections incorporated: stationary Lunge, conditional encounter admission,
Hallun-specific two-objective wording, exact empty-plan save semantics, selected
versus retained sites, ground-status recovery, valid corpse inspection and
save restoration distinct from free reading. All full-path source references
resolve; the proposed evidence directory is intentionally not created yet.

The preceding review records the planning pass only. The implementation pass
below supersedes its not-started status.

## 11. Execution log and admission decisions

Execution started from `0417e145` under the saved implementation prompt. Evidence
lives in `Docs/Verification/SpreadFirstHour/`; the final integrated result and
acceptance bounds are recorded below and accompany the verified feature commit.

| Milestone | Current result | Still required |
|---|---|---|
| M0 | Three-seed nearby source census found existing Sill merchants and real early gear; no additional guaranteed cache is justified. Actual glyph measurement found zero top margin, no measured right overflow. | Complete census. The first held-key probe remains invalid for pacing; ordinary journey limits are in M1. |
| M1 | Truthful glade/actual-marker/Hallun guidance; 132 native focused tests pass (67 new, 65 neighbors). Ordinary keyboard route reaches Sill, accepts Ellun’s task and triggers the real stump stage. The journal no longer falsely locates it outside the village. | Returning to the wandering giver defeated the bounded test pathfinder. Quest reward/purchase/save route remains unverified; no claim that the game route is broken. |
| M2 | Current visible-owner full reader, separately labeled ground, full action-label recovery, preserved paragraphs and measured vertical margin implemented. 42 dedicated native cases and four paging-key cases pass. Staged native keyboard route completes with 11 inspected images; 527 actual glyphs have zero overflow at 1920×1080. | Integrated native GREEN; broader resolutions are not claimed. |
| M3 | Actual planner slot choices, conflicts and conditional item facts; 46 comparison cases and corrected neighbors pass. Paged reader inspected. Actual player-earned cap is compared after native combat and acquisition in M4. | Integrated native GREEN; the combat arena is staged, so natural discovery is not claimed. |
| M4 | Exact original pair and gear, physical preflight, frozen selection and ordinary-save metadata. Three generated selected zones each admit the pair. Final art29/29 and all18 frames reviewed. Staged native13/13: bypass, ordinary-kit kill, original cap/sword acquisition, comparison and exact save/mutate/load graph;32/40HP, no tonic. | Integrated native GREEN. Native Lunge was not observed; core lifecycle verifies it separately. |
| M5a | Ambush save repair passes actual Unity127 including real pre-fix native legacy bytes. Chalk-ring latchcoil and physical sign admitted once in each of three selected seeded zones; current source40 plus pair52 pass92/92 natively within the336-case repair selection. Art20/20 and all9 images reviewed; stronger contact count separately passes. | Native13/13 passes: warning, bypass, sleeping/awake/depleted save graphs, actual bite, defeat and real harvest;37/40HP, no tonic, six inspected frames. Integrated native GREEN. |
| M5b | **Deferred by admission gate.** One actual acid hit and full aftermath left a 40 HP Player at 1 HP in isolated Unity Play; a legal second cast killed the other target. | No drain-slime source or model ships. A later proposal needs ordinary-kit counterplay evidence. |
| M6 | Final20,406/20,406 native GREEN, zero failures/skips, zero recorded input drift, exact editor/save/input restoration, consolidated Q1–Q4 and inspected native frames. | Accepted scope verified for this commit; the explicit journey/balance/acid limits remain. |

Sweep and execution corrections:

- Source stock is not proof of legal/affordable offered trade or acquisition.
  M0 receipts explicitly distinguish those claims. The pair is a rare regional
  variation, not a forced nearby reward: seed 1 selects (5,15), seed 64 (10,2),
  and seed 1729 (6,12). Existing quest/service opportunities own the early loop.
- The shared saved selection uses bounded ordinary World properties. Missing
  old metadata is disabled; initialized empty metadata stays empty. Cached
  ordinary graphs retain their actor/item identity; administrative unload keeps
  its documented regeneration behavior.
- Source adversarial tests caught a sealed but locally clear hedge pocket,
  a foreign factory, and a creation callback closing the distant approach.
  Reachability and factory authority now revalidate before commitment.
- Initial creature prose promised a cap/sword/cudgel after disarm. Permanent
  anatomy and conditional training now stay factual; actual equipped items
  remain owned by the equipment projection.
- A held-key ad-hoc native probe repeated movement for 850 ticks. It is retained
  as isolated layout evidence only, never as a discovery or first-hour route.
- The first staged reader run reached the correct owner and paginated modal,
  but its PageDown injection could not pass the missing InputHelper mapping.
  The corrected driver observes the real page index and uses the displayed
  arrow control. The adapter gap is separately fixed and tested RED→GREEN.
- Two fixture premises needed correction: an invisible/unexplored steam cell
  must not be examinable, and an item retaining Equippable still has legitimate
  slot facts after its weapon part is removed. Positive and refusal controls
  remain paired; no visibility or ownership guard is weakened.
- Hedge has Physics solidity but does not opt into AI sight occlusion. The
  assistance countercheck uses an actual StoneWall; no global furniture/hedge
  sight rule is changed. A valid Lunge may miss, so its executed attack and
  real cooldown are checked without claiming every attempt deals damage.

The acid decision and its exact limits are in `M5b/notes.md` and
`M5b/acid-native-admission.json`. The probe used real commands/status lifecycle,
normal 40 HP/zero resistance, legal cooldown and actual movement out of range;
it did not establish ordinary discovery, AI frequency or universal player death.
No global acid tuning changed. Save isolation, bootstrap seed, root and original
ReferenceGlade scene were confirmed restored after the probe.

Additional bounded execution decisions:

- The ordinary M1 route retained40HP, reached Sill and triggered the exact real
  stump objective. A test predicate initially checked the completed stage after
  the story had already advanced; this is corrected without changing quest code.
  The wandering giver return defeated two bounded pathfinder attempts. Following
  the user’s priority rule, no third campaign bot is built for this slice.
- The marlback gallery exposed a floating decorative slate. The minor detail was
  removed after two contact approaches; the final model keeps one supported slate
  and two original rake markings. No broader original-body overhaul was needed.
- Viper optional saved metadata aliased to the pair could dispatch the wrong
  family. A failing overlap case led to family-specific selection revalidation,
  preserving the original pair’s saved payload and selection.
- The viper’s historical sign warns of bites and points south. It does not reveal
  current hidden occupants or promise that an already-awake enemy cannot pursue.

The full ordinary quest/service journey, all-seed encounter balancing and acid
slime remain explicit limits; staged success is not relabeled ordinary discovery.

Final source review reproduced one remaining pair-side callback gap: an actor’s
creation callback could change the selected biome or add a POI after initial
admission. Two failing cases plus two unchanged-source controls now pass with
repeated current pair authority checks before commitment. Only this pair branch
changed; the optional-viper branch, ranks, saved payload and successful diagnostic
remain unchanged. Private pair/source92/92 and both actual-reference compilers
pass; the final native integration includes the four new counterchecks.

The first frozen full run, `4b104d898a734a5ea3966bcd60dbb2c5`, finished
20,365 passed / 39 failed / 0 skipped out of20,404. It exposed34 nullable-manager
save regressions, one stale exact equipment allowlist, and four old examination
fixtures missing visible/renderable owners and the actual paged reader.
The save assignment now preserves supported managerless sessions; only the two
intentional rare kits extend the exact roster. Reader fixtures now exercise the
real guarded reader without weakening production visibility or ownership checks.
Focused native repairs pass336/336 plus64/64; the source subset includes all92
current pair/viper cases. The complete raw first run and repairs are retained.

Final verification: Actual unfiltered Unity EditMode **20,406/20,406 passed**, zero failures/skips, 1371.069s (job `b80eff45e56e4bdb90681af56ac80242`).
This adds338 discovered cases over the inherited20,068 baseline. All23,789
recorded code/data/art input hashes are unchanged across the rerun. Exact original
scene/start scene, play/compile state, seed, save-root override, last-game preference,
background behavior and input settings compare equal before/after. The complete
XML and machine-readable restoration/drift receipts are in `Integration/`.

This coherent feature commit contains the verified admitted scope.
The ordinary quest reward/purchase/save loop, all-seed encounter balancing and
rejected acid-slime proposal are explicitly outside the demonstrated acceptance.

## Current-source performance follow-up

The existing dense-glade native movement driver also passes12/12, zero errors,
with exact restoration and eight inspected frames. Its60.240s sample measures
frame mean5.112ms/p957.199ms and redraw-renderer p9553.865ms, versus the earlier
matching-settings sample4.252ms/4.014ms/43.007ms. This measured slowdown is retained
as a follow-up; the two noncontemporaneous Editor samples do not isolate a cause.
No broad optimization or performance improvement claim is made. The plan's
combined capture is explicitly narrowed: repeated-reader function is verified
separately in the11-frame M2 route; repeated-reader performance remains unmeasured.
See `Docs/Verification/SpreadFirstHour/Performance/README.md` for raw comparison,
importance assessment and the next matched experiment.
