# Density completion C0/C1 — armor finds and visible hostile gear

**Status:** C1 data and focused core GREEN complete; adversarial GREEN complete; native acceptance pending.
**Baseline:** `50ef23d2`, archived scripts/content in a private runner checkout.
**Plan:** `DENSITY-COMPLETION-PLAN.md` C0/C1 and `LOOT-FINDS.md`.
This is CoO-original content tuning, not a Qud parity port.

## Verification sweep

| Premise | Verified correction | Consequence |
|---|---|---|
| Armor pools already exist | Six weapon/offense pools exist, no armor pools | Add exactly three armor pools and 15 live container references |
| All armor tiers have many pieces | Six concrete T1, five T2, one T3 armor blueprints | T3 pool includes lower-tier off-body pieces; no invented armor |
| Loadout changes are just rewards | Gear is equipped through AutoEquip and contributes real armor/weapon behavior | Restrict roster; retain early-tier body-armor rates; coordinate tactics weapon classes |
| Eleven hostile loadouts ship | Ten hostile holders and eighteen other holders resolve from current blueprints | Touch nine named hostiles; preserve SkeletalSentry and every friendly/service loadout |
| Pick happens first | Loadout applies Equip, then Pick, then Carry | Avoid shields or conflicting hand grants before the picked weapon |
| Gear needs a new death drop roll | Real equipment and inventory already spill; natural defaults do not | Assert exact instance identity through death; leave death tables unchanged |
| All weapon choices suit planned tactics | Shank/Lunge require compatible actual equipment | Scavenger retains Dagger/ShortSword; bandits retain ShortSword; chieftain gets LongSword |
| The warlord already drops its cleaver | WarlordCleaver is currently a natural fallback; equipped armor drops | Add a real visible WarlordCleaver while preserving its natural fallback |
| A higher table number makes higher-tier gear | Both selectors clamp to T3; no Item-tagged Tier4+ blueprint exists | C11 remains separate, requiring real variants and enhancement/source contracts |
| Expected table value is game economy | Capacity, stack merges, actual placements and sale-price rounding intervene | Record table expectation plus actual zone and loadout distributions separately |

## Bounded content

Three pick-one pools (blueprint:weight):

- T1: LeatherArmor:2, Buckler:1, LeatherBoots:2, LeatherGloves:2,
  LeatherCap:2, Cloak:1. Expected Commerce value 15.4.
- T2: ChainMail:3, IronHelmet:2, IronshodBoots:2, WardedCloak:1,
  IronBuckler:2. Expected Commerce value 41.6.
- T3: PlateArmor:4, ChainMail:1, IronHelmet:1, IronshodBoots:1,
  WardedCloak:2, IronBuckler:1. Expected Commerce value 63.8.

Add one independent armor reference per tier to Crate (20/25/30%), Urn
(10/15/20%), BoneCache (20/25/30%), StrongBox (25/30/35%) and Reliquary
(15/20/25%). All legacy rows remain. Sacks, baskets, hollow logs, death loot,
shops and special-provenance pools remain unchanged. Rentals, crafted outputs,
natural weapons and unique artifacts never enter the new pools.

Root owns surgical Objects.json edits after RED. Target Loadout fields only:

| Blueprint | Equip | Pick |
|---|---|---|
| Snapjaw | LeatherCap:35;LeatherGloves:20 | 1;Dagger;Hatchet;Cudgel |
| SnapjawScavenger | LeatherGloves:50;LeatherArmor:25 | 1;Dagger;ShortSword |
| SnapjawHunter | Spear;LeatherBoots:50;LeatherCap:25 | empty |
| DesertBandit | ShortSword;LeatherCap:40;LeatherBoots:20 | empty |
| RuinScavenger | LeatherGloves:50;LeatherCap:25 | 1;Dagger;Cudgel;OldWorldPipe |
| SnapjawChieftain | LongSword;LeatherArmor;LeatherCap;LeatherBoots:35 | empty |
| SnapjawWarlord | WarlordCleaver;LeatherArmor;IronHelmet;IronshodBoots | empty |
| AmbushBandit | ShortSword;LeatherArmor:35;LeatherBoots:25 | empty |
| RuneCultist | Dagger;LeatherGloves:50;Cloak:30 | empty |

Carry stays empty. Existing natural weapons, stats, tags and all other parts are
preserved by this slice. Changes from the first proposal preserve the explicit
C2 tactics requirements; C2 changes are separately reviewed.

## Measurement and acceptance

Before content edits, archive baseline production and run the same census code
against it: five fixed seeds; four non-POI cells in each of six live surface
biomes; ordinary underground depths 1/3/5; first available lair of each eligible
biome; actual designed starter grant and actual Morrowfast shop stock. Record
absent lair coverage instead of inventing zones. Count actual container contents,
successful core Open actions, locked/unapproachable exclusions, categories,
blueprints, units, weight, Commerce value and neutral sale quotes. Do not call
core action dispatch native UI or full new-game bootstrap.

Factory loadout samples use 256 seeds per named hostile and measure actual
equipped/carry state, item value, AV/DV and same-instance drops. Table expectations
are exact nested weighted/independent math, not a substitute for zone generation.
The runner's stable string hash differs from Unity; identical test seeds imply
replay within that runtime, not identical editor maps. C12 repeats the census.

RED tests start at live stocked containers and real factory actors. Counter-checks
disable the relevant chance/weight or use an unchanged natural/friendly source.
Every weapon/armor granted must actually equip before its same ID drops once on
ordinary death; Temporary/NoDropOnDeath controls suppress rewards. Registry
validation, tier/category bounds and unchanged legacy rows are required.
Dedicated adversarial coverage follows GREEN: capacity/stacking, save/reload,
inheritance, multiple instances, deterministic RNG, exclusions and diagnostics.

Native acceptance remains root-owned: open a naturally generated manufactured
container via keyboard, transfer and inspect armor; inspect a generated hostile's
real gear before death and recover the same instances; observe a natural Choir
container control. Ordinary-stat play evaluates threat/reward balance separately.

## Implementation log and review

- 2026-09-26: read CLAUDE.md, ADVERSARIAL_TESTING.md, LOOT-FINDS.md and live source;
  corrected counts, loadout order, natural fallback and higher-tier assumptions.
- Archived baseline census completed before content changes. The same final census
  corpus passes 16/16 against `50ef23d2`; content RED is 41 expected failures plus
  ten unchanged controls (51 cases). All 51 content cases pass in the C1 overlay.
- Added three pools and 15 independent references. Parsed comparison proves every
  old entry and unrelated table unchanged. Root applied precisely the nine
  Loadout edits above; the private after overlay contains only these C1 changes.
  Concurrent books, tactics and Waterskin changes are deliberately excluded from
  this comparison and require integrated verification.
- Measurement self-review found two report defects before accepting results:
  Commerce/sell APIs already price an entire stack (2-case RED, one failure), and
  StatusTonic also implements Water/Stoneskin utility effects (13-case RED, two
  failures). Corrected arithmetic and classification pass; rejected raw reports
  remain explicitly named in the receipt directory.
- The initial adversarial run had three fixture failures from guessing the table
  prefix `WovenBasketT`; live placement actually uses `BasketT`. Corrected the test
  mapping, with the failed receipt retained. This was not a production defect.
- ⚪ Gear value increases deliberately; balance is not inferred from table math.
- ⚪ Existing saves retain their serialized equipment and stocked contents.

## Files owned by this slice

- `Assets/Resources/Content/Data/Loot/LootTables.json`.
- New `Assets/Tests/EditMode/Gameplay/World/DensityLootCompletionTests.cs`,
  `DensityLootCensusTests.cs`, `DensityLootCompletionAdversarialTests.cs`, metadata.
- `Docs/DENSITY-LOOT-COMPLETION.md` and verification receipts.
- Root owns Objects.json, integrated plan, shared docs and native scenario wiring.

## Actual C1-only census

Same five seeds and same selected cells in the archived baseline and isolated
C1 overlay. The historical helper recorded 447 handled opening attempts and
counted the contents of those containers. Later lock-authority review found that
this did not prove every container opened: it checked only the legacy lock flag
and treated Handled as success. These rows are stock/value observations, not
player acquisition. The corrected C12 helper records combined locks, legal
adjacent placement, dispatched attempts and exact actor OpenContainer events
separately; raw historical receipts remain unchanged. Lairs existed in Spread, Sodden
and Beating in these worlds. None were found in Grovelands, Overwrit or Stump
for these seeds; this is coverage absence, not a claim they can never exist.

| Source | Zones / containers | Armor before → after | Commerce before → after | Actual stack sell quotes before → after |
|---|---:|---:|---:|---:|
| Wilderness | 120 / 330 | 13 → 37 | 9,172 → 10,842 | 2,984 → 3,571 |
| Lairs | 15 / 66 | 2 → 9 | 1,061 → 1,357 | 346 → 448 |
| Underground 1/3/5 | 15 / 51 | 4 → 13 | 1,473 → 1,814 | 479 → 596 |
| Total containers | 150 / 447 | 19 → 59 | 11,706 → 14,013 | 3,809 → 4,615 |

Overall container Commerce rises 19.7%, and neutral sell quotes 21.2%. New rolls
advance the shared loot RNG, so unchanged old rows can yield different individual
items after the addition; the parsed-row preservation check is separate from the
observed sample. The actual designed starter remains one dagger, two healing
tonics and two food units (Commerce 54 / sell 17). The two generated Morrowfast shop
inventories, sampled across five seeds, remain Commerce 1,710 / sell 575 in C1.
Those shop numbers do not include later C3/C5 source additions.

Across 256 factory seeds for each of ten hostile holders, every manufactured
weapon/armor is equipped, with zero carried fallbacks. Mean personal gear value
changes as follows; these are factory distributions, not encounter frequencies:

| Actor | Before | After |
|---|---:|---:|
| Snapjaw | 11.56 | 14.39 |
| SnapjawScavenger | 15.30 | 24.00 |
| SnapjawHunter | 22.17 | 26.00 |
| DesertBandit | 17.38 | 20.58 |
| RuinScavenger | 12.78 | 14.02 |
| SkeletalSentry | 25.00 | 25.00 |
| SnapjawChieftain | 53.00 | 67.17 |
| SnapjawWarlord | 83.00 | 118.00 |
| AmbushBandit | 25.43 | 28.43 |
| RuneCultist | 12.78 | 19.34 |

These increases also change hostile AV/DV and, for boots, speed. They must be
observed with ordinary combat stats before calling C1 balance accepted. The
runner proves deterministic content/equipment composition under its stable hash;
it does not prove editor map identity, native transfer UI, natural acquisition,
full new-game behavior, or threat/reward balance.

## Cold-eye Q1–Q4 and adversarial scope

- Q1: source/stock and equip/drop symmetry both use existing real paths; no new
  hidden death gear roll. Positive death checks retain exact item instances;
  Temporary and NoDropOnDeath suppress gear and hidden supplies.
- Q2: every source is independent, every armor child pool is pick-one, tier bounds
  use actual blueprint tags, and table selectors still clamp to authored T3.
  Natural Choir sources and all seventeen other loadouts are explicit controls.
- Q3: chance0/100 and actual threshold-minus-one/threshold branches, zero weights,
  absence of Pick, full containers, second spawns, and repeated death provide
  matched counter-checks. Save token graphs assert IDs, owning inventory,
  equipment, AV/DV and speed rather than merely non-null objects.
- Q4: no T4+ claim, no accidental merchant/unique/rental/crafted source claim,
  and no claim that standalone action dispatch is native UI. Corrected the early
  non-hostile-holder count from 18 to 17 after a fresh resolved-blueprint census.

The dedicated 23-case adversarial file exercises nine natural container/tier
controls, three chance/capacity cases, three stocked-armor save cases, two
actor-equipment save/death cases, two drop-suppression cases, two independent
spawn/seed cases, a natural Sentry control, and all seventeen friendly/service
loadouts together. Existing loot regression files are also included. Full
integration, editor acceptance and normal-stat pacing remain separate gates.

Evidence: `Docs/Verification/DensityCompletion/Loot/` contains content RED,
measurement RED and rejected drafts, canonical before/after JSON+XML gzip files,
C1 overlay hashes, parsed data differences, and concise census summaries.

Final focused standalone run: **138/138 passed**, including all 23 dedicated
adversarial cases, 51 content cases, 16 census/control cases, and 48 existing
loot regressions. Native and broader integration evidence remain pending.

## Final integrated equipment pin reconciliation

The legacy equipment-content fixtures predated C1's richer kits. The broader
combat regression exposed old exact probability, item-count, parent-choice and
armor-only breacher assumptions. They now pin the final literal kits, all 17
optional armor boundaries (present at chance−1, absent at chance), and the two
new explicit C13 loadout holders. The breacher must carry a real equipped cleaver
and retain both stronger natural hands after unequipping it; old saved actors
remain unmodified while a new actor has exactly four distinct gear instances.

Three remaining failures concerned C12's intentional player prose correction:
`you equips` is now `You equip`. Exact message assertions were updated; the
nested equip, death-drop identities, repeated-death idempotence and actual
pickup/equipment ownership checks remain. No production fix was needed.

Private focused result: **91/91 GREEN** across the two legacy fixtures, with
1,579 assertions. Receipts: `Loot/equipment-pins-before.xml.gz` (the original
broader regression, not a matching-corpus baseline) and
`Loot/equipment-pins-green.json` / `.xml.gz`. These checks do not replace native
EditMode or acquisition/economy acceptance.

## C11 follow-up correction to the historical C1 census

The deep-source verification found that real `LockedChest` uses
`LockPart.IsLocked`, while the census helper checked only the legacy
`ContainerPart.Locked` field. It also counted a handled OpenContainer action as
an opening. Preserve the raw historical C1 receipts, but interpret their447
“opened” figure as **handled opening attempts**, not proof that every key lock
was satisfied. Contents/value/category totals still describe actual generated
containers; the acquisition claim is narrower. The C12 integrated checkpoint
must count both lock authorities, successful actor OpenContainer events and
key acquisition separately. A matched private lock-authority probe confirmed
that the core itself allowed open/take/take-all/put through a real key lock; the
C11 bounded repair is tracked in `DENSITY-HIGHER-TIER-FINDS.md`.


The C12 correction is now implemented in `DENSITY-FINAL-CENSUS.md`: all150 original zone rows replay unchanged, and actual access is measured separately from stock. At the pre-C10.2 integrated checkpoint the445 containers yield39 lock skips and406 observed openings. These are core permitted opens from a legally placed measuring actor, not natural route/key acquisition. Historical receipts remain immutable.
