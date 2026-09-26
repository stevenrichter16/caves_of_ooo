# C9 — scenery and corpse reachability audit

**Status:** bounded C9 implementation and core/adversarial verification complete.
The final shared-source focused group is **154/154 GREEN**: 52 new harvest/content
cases, 38 existing harvest/corpse/GroveLaw checks and 64 local-people cases.
Native keyboard harvest/replay acceptance and the final native suite remain due.
Root owns integration and blueprint surgery. The initial census predates the
user-requested removal of imported enemy names; its historical counts are not a
claim about final roster labels.

## Scope and corrections

The new bounded private census regenerates the same 150 wilderness/lair/depth
cells used for C1, five Morrowfast cells, and seven contrasting settlements in
each of five worlds: **190 zones, 492 resolved blueprints, 115 placed non-item,
non-creature blueprint types**. Of these, **54 advertise only Examine (or no
inventory action)** in the query context. This is a candidate list, not 54 inert
objects: liquids, traps, terrain, traversal and passive state have other seams.
The historical 91/200 figure cannot be reused as a current census result.

**17 of 106 Creature-tagged resolved blueprints** already configure effective
corpse harvest yields. The 106 denominator includes the abstract Creature base,
Player, people and protected fauna; it is not 106 candidates for butchery.
`CorpsePart` transfers family configuration into runtime `HarvestablePart`; new
corpse blueprints or a second butcher system are unnecessary. Constructs already
have deliberate mineral salvage (StoneGolem, ObsidianBrute, VaultSentinel), so
“no meat” must not be confused with “no salvage.”

Evidence: `Verification/DensityCompletion/Scenery/readonly-audit.json`, compressed
census/XML, zone list and exact private probe source in that directory. Probes
only read/generate disposable test worlds; they did not modify shared Assets.

## Confirmed seam defects before expanding yield coverage

- 🟡 **A spent target can yield twice.** Actual InventoryAction Harvest on a carried
  corpse grants one RawMeat and removes it. Sending the same action to the retained
  target reference grants another: expected total 1, observed 2.
- 🟡 **An unowned target can yield without consumption.** Acting on a corpse in a
  different actor's inventory grants the harvester one RawMeat while the corpse
  remains carried by the owner: expected 0, observed 1.

These are confirmed **core-event** failures (2/2 private hypotheses failed), not
claims of an ordinary keyboard exploit: routed UI may prevent some stale/foreign
requests. `HarvestablePart` has neither an ownership/reach preflight nor a durable
spent claim; target removal alone does not enforce its documented single use.
Before adding more yields, use the established inventory/zone ownership seams,
claim the action before callback-capable work, refuse stale/foreign/dead/distant
requests, and preserve valid overflow. Pair live carried/world successes with
same-state rejected controls and replay/reentrancy/save checks. Existing tests
that use unplaced synthetic actors must be reviewed for the intended contract,
not silently used to allow arbitrary remote harvesting.

## Bounded useful first pass

1. Secure the existing harvest seam with RED→GREEN and dedicated adversarial
   coverage; no second butcher action and no changes to visible equipment drops.
2. Add only grounded existing-product family rows: SariSnake → VenomGland is
   directly supported by authored pit-viper identity/venom attack; SunStriker →
   RawMeat follows its authored large-lizard identity; SporeShambler →
   ShamblerSporeSac follows the existing fungal relative's material yield.
   Confirm roster replacements and product consumption/economy before choosing
   exact counts. Do not change plain Shambler's attack or add a disease vector.
3. `Bones` → `Bone` is a clear scenery harvest candidate with existing product and
   natural Beating/ruin/settlement sources. Keep BogTakenBody and named/quest
   remains as counter-cases. Removing a target must preserve unrelated cell
   occupants and native footprint/presentation cleanup.
4. A repaired village oven currently has dynamic repair state but CookingService
   searches Campfire only. Review this as a small cross-feature Cook extension,
   with broken/cold/foreign/dead/distant controls; do not pretend every decorative
   oven is hot. BedPart likewise serves NPC rest but has no player action here.
   Both are proposals, not yet defects proved by a player-flow test.

No yield is proposed merely because a creature is hostile. Humanoid people,
children, pets, named Choir individuals, uncertain gin-frog/remnant identities,
protected birds and constructs need explicit family disposition. GlassScorpion's
neutral faction is deliberate. No corpse drop chance or equipment bounty changes
are needed for this first pass.

## Every raw Examine-only candidate

Counts are placed instances within the bounded sample; fields/ground dominate
these counts and should not distort interaction priorities.

| Blueprint | Instances | Zones | Disposition |
|---|---:|---:|---|
| AcidPool | 26 | 9 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| AshBed | 40 | 13 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| BearTrap | 8 | 7 | Existing movement trigger; do not count as inert because menu only offers Examine. |
| Bed | 109 | 35 | NPC rest reservation already works; player Rest remains a possible bounded integration, not yet verified. |
| BogTakenBody | 9 | 7 | Preserve authored human remains; no generic meat/butchery reward. |
| Bones | 13 | 10 | Grounded candidate: single-use Harvest → Bone, after ownership/replay safeguards. |
| BrinePool | 1639 | 53 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| CampfireGroundMarker | 188 | 35 | Keep visual or runtime anchor; inspect owner, not an independent action target. |
| ConvalescencePool | 2 | 1 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| CopperPipe | 155 | 35 | Conductor network/material behavior is meaningful; preserve topology, no generic pipe scavenging assumption. |
| CropRow | 903 | 9 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| DescentLedge | 256 | 20 | Existing traversal/ledge control; global keys, not a new harvested prop. |
| FireTrap | 8 | 8 | Existing movement trigger; do not count as inert because menu only offers Examine. |
| Floor | 21088 | 30 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| FlowerField | 12 | 2 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| FrostVent | 16 | 5 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| Grass | 150857 | 85 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| GroveSign | 10 | 10 | Already authored reading via Examine; avoid duplicate Read with identical content. |
| GuestClothPole | 12 | 12 | Intentional place/culture dressing; keep. Trading belongs to actual owner. |
| HiddenShrineMarker | 5 | 5 | Keep visual or runtime anchor; inspect owner, not an independent action target. |
| IceSheet | 18 | 5 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| LanternGroundMarker | 35 | 10 | Keep visual or runtime anchor; inspect owner, not an independent action target. |
| LimestoneFloor | 6581 | 5 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| MarketStall | 39 | 12 | Intentional place/culture dressing; keep. Trading belongs to actual owner. |
| MemoryBathPool | 11 | 3 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| MillStone | 4 | 4 | Handling exists; declaration can depend on strength/state. Preserve until valid haul context is tested. |
| MirrorMucilagePool | 14 | 5 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| OilSeep | 4 | 2 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| OldStump | 5 | 5 | Keep visual or runtime anchor; inspect owner, not an independent action target. |
| Oven | 35 | 35 | Live oven has repair-state part; investigate real repaired-oven Cook support (current cooking accepts Campfire only). |
| OvenGroundMarker | 38 | 10 | Keep visual or runtime anchor; inspect owner, not an independent action target. |
| OverwritGround | 40000 | 20 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| PressurePlate | 8 | 7 | Existing movement trigger; do not count as inert because menu only offers Examine. |
| Reeds | 1891 | 25 | Keep vegetation; no existing authored reed product was established, so do not invent yield merely for count. |
| RoadStone | 6281 | 26 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| Rock | 239 | 10 | Stone material/thermal behavior; no new harvest product established. |
| Rubble | 2230 | 20 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| SaltCrust | 1040 | 10 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| Sand | 46689 | 30 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| SandstoneFloor | 7928 | 10 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| ShaleFloor | 5778 | 5 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| Signpost | 3 | 3 | Already authored reading via Examine; avoid duplicate Read with identical content. |
| SpikeTrap | 6 | 6 | Existing movement trigger; do not count as inert because menu only offers Examine. |
| SprayPool | 5704 | 20 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| StairsDown | 92 | 92 | Existing traversal/ledge control; global keys, not a new harvested prop. |
| StairsUp | 12 | 12 | Existing traversal/ledge control; global keys, not a new harvested prop. |
| Stalagmite | 51 | 15 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| SteamVent | 100 | 31 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| StoneFloor | 8014 | 35 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| TarSeep | 45 | 20 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| TepuiStone | 49665 | 25 | Keep terrain/structural dressing; no free product or extra verb assumed. |
| WatchLantern | 35 | 35 | Existing environmental/light/fuel effect; preserve unless a separate practical interaction is justified. |
| WaterPuddle | 5585 | 20 | Existing liquid/contact behavior; C3 vessel interaction occurs from carried vessel. |
| WellGroundMarker | 35 | 10 | Keep visual or runtime anchor; inspect owner, not an independent action target. |

## What this audit can and cannot prove

Can verify: resolved inheritance/configuration, current bounded core generation,
actual declared action commands in the supplied context, direct corpse-harvest
replay/ownership failures, and exact source/product definitions. Complete census
rows preserve all 492 blueprint records for later family classification.

Cannot verify: all 400 surface cells in any one world, natural player routes,
keyboard reachability or visual/footprint correctness, food/sale balance, and
precise Unity maps. The action query places a Player at 2,2; declaration-time
reach/strength gates may hide a valid action. A native acquisition/harvest/cook
example and matching refusal controls remain required before C9 can ship.

## Approved implementation contract

Only HarvestablePart and new focused/adversarial tests are agent-owned. Root
applies four blueprint changes: SariSnake yields 1 VenomGland; SunStriker 1–2
RawMeat; SporeShambler 1 ShamblerSporeSac; Bones 1–2 Bone. All yield chances are
100%; existing creature corpse chances and unrelated fields stay unchanged.
No attacks, factions, human-remains rewards or global fire behavior change.

The harvest action must validate actual carried membership or same-zone spatial
reach (including footprint distance), a living actor and finite valid yield
configuration. A durable saved Harvested field plus a shared inventory transaction
claim prevents stale or reentrant target use. Stage products, recheck the source,
and join an outer action transaction when present. Rollback must restore the
source, exact inventory changes and any overflow drops; failed capacity without
a ground destination must preserve the source. Publish message, diagnostics and
GroveLaw consequence only after commit. Missing yield infrastructure refuses
rather than destroying the source; this corrects the old documented consume-on-
missing-factory behavior. Zero-chance valid harvest still spends the source.

Legacy tests with a detached actor harvesting a world target are invalid under
the intended reach rule and will receive explicit placement in their fixture;
that fixture correction must not weaken their existing yield/overflow/law checks.
No unrelated scenery verbs are implemented in this slice. Repaired-oven/player-
bed proposals remain separate follow-up work.

## Implementation, adversarial review and evidence

HarvestablePart now verifies a living actor, real carried membership or same-zone
footprint reach, consistent ownership, valid finite configuration and actual
portable non-creature products. It claims a saved spent flag and the action's
inventory transaction before callback-capable work, stages yield, rechecks source
and recipe, and records success only on commit. Overflow is placed at the valid
source cell; no destination causes a full rollback. An outer rollback restores
the source, exact changed inventory entries and overflow. Chance zero deliberately
spends a valid source; missing factory/product infrastructure refuses without
destroying it. That last behavior is a documented change from the old comment.

Root's content diff changes only the four approved Corpse/Harvestable parameter
sets; inherited corpse chances remain. Receipt: `Scenery/content-parsed-diff.json`.
The initial 32-case RED had 23 expected failures and nine passing controls. Twenty
additional adversarial cases found two malformed-yield failures (Player and Floor
were accepted as products); the bounded portable-product gate fixes both. The
remaining 18 adversarial cases already passed. Receipts preserve the original
source, hypotheses, 32-case RED and 90-case adversarial RED in
`Verification/DensityCompletion/Scenery/`.

Three legacy fixtures previously harvested with detached or remote synthetic
actors. They now place the actor legally adjacent while preserving their original
yield, overflow and GroveLaw assertions: BiomeHarvestTests,
BiomePhaseAAdversarialTests and GroveLawTests. These are explicit fixture repairs,
not relaxed ownership rules. `final-combined-green.json` and compressed XML record
154/154 from current shared source.

- 🟡 Replay/foreign ownership and callback reentry: fixed, with same-world success,
  foreign/detached/dead/distant refusal, and retained-reference replay controls.
- 🟡 Malformed creature/non-takeable yields: RED then fixed. No creature or floor
  can be smuggled into inventory by a malformed harvest recipe.
- 🔵 Transaction symmetry: claim, inventory deltas and world overflow all roll
  back together; message/diagnostic/GroveLaw effects wait for commit.
- ⚪ Scope: no generic human/pet/Choir bounty, no new fire scale, cooking or bed
  action. Those candidate ideas remain proposals, not implemented features.

Q1/Q2 compare carried/world and owned/joined transactions; both use the same
preflight and commit-only receipts. Q3 covers chance 0/100, diagnostic on/off,
minimum/maximum yields, footprint reach, missing destination, stale ownership,
save roundtrip, reentrant factory/message callbacks and outer rollback. Q4 matches
this bounded contract and distinguishes core events from a keyboard exploit.

Owned files: HarvestablePart; new DensityHarvestSecurityTests,
DensityCorpseCoverageTests and DensityHarvestAdversarialTests (+metas); the three
legacy fixture placements above; this living doc and evidence. Generic part
serialization saves Harvested; no C9 SaveSystem edit was needed.

**Can verify now:** actual death-to-corpse yield, factory materialization, legal
core action dispatch, transaction/save/replay safeguards, overflow conservation
and channel-gated diagnostic records under the standalone runtime.
**Cannot verify yet:** native keyboard discovery/harvest/replay, rendered source
removal, food/sale balance or Unity-identical maps. Root owns those native checks.
