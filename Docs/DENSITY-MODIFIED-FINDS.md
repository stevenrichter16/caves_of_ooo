# C11.2 — modified deep finds

Published follow-up after foundation6b3f8939; native acceptance pending.
The existing C11.1 deep reliquary has one actual T4 equipment find. Add a bounded
25% chance of one compatible tier-1 enhancement to that piece, using the existing
ItemEnhancing API. Earlier tables, ordinary loot, shops and natural caches remain
unchanged. This is an original sourcing choice, not a global tinkering unlock.

## Verified corrections before implementation

- LootStocker.StockContainer rolls names, creates the real factory item, then
  ContainerPart.AddItem establishes ownership. A successful AddItem can represent
  a full stack merge, so the decorator must verify the exact retained owner/item.
- ItemEnhancing.Apply already checks compatibility and the two-slot cap and
  attaches serializable Parts. It does not alter Commerce value. Do not invent a
  sale-value multiplier or modify the global enhancement contracts.
- EnhancementSerrated requires Cutting; PaleSalt/ChoirIron require a melee weapon;
  Lacquered requires Armor; GlowQuartz requires Equippable. Engraved grants faction
  reputation and needs authored faction semantics, so it is excluded.
- Item tier and enhancement tier are different. T4 armor with a tier4 Lacquered
  enhancement would gain4AV; this first find source deliberately uses tier1 only.
- DeepReliquaryT4 is already restricted to ordinary Spread/Sodden/Beating columns
  depth9+ by StampCatalog. Do not extend manufactured finds to Choir containers.
- Existing old-source roll order must consume no new RNG draws. The new source
  may consume a chance/choice draw after its normal contents are determined.

## Contract and tests

Only four named ordinary T4 items in an actual LockedChest stocked by the exact
DeepReliquaryT4 table are eligible. Require Item, Tier4, Equippable, ordinary
unmodified single-unit state and correct native ownership. Apply at most one
allowlisted compatible enhancement, mark the chance as spent on success or miss,
and preserve locked state, identity, value, ordinary inspection and saved Parts.
Existing enhancements, unknown/custom content, portable/fake owners, depleted
stack merges, malformed item shapes and repeated calls fail closed.

First run real stocking against unchanged production: positive modified-find
assertion must fail while old-source/RNG/replay controls pass. Then implement the
narrow helper/hook. Add20–40 adversarial cases for ownership, compatibility,
zero/100 chance, save/idempotence, registry refusal, stacking and live equip/remove.
Re-run existing higher-tier/stocking/enhancement/save tests, compare distributions,
and obtain native generated-source inspection before claiming this complete.

## Scope boundaries

This closes only modified findings. Legendary templates, additional variants,
general liquid carrying, beds/doors and global thermal tuning remain separate.
No new blueprint, global shop/death roll, player BitLocker, or lore claim.

## C11.2 review

Source published after private verification. Initial real-stocking RED:6cases,5controls passed,1positive failed
with0enhancements in128deep finds. First implementation6/6GREEN.
Dedicated adversarial sweep:42total including36adversarial; initial8failures.
Two stack cases initially added a duplicate Stacker instead of editing the
existing inherited part; corrected setup leaves6real failures (four portable/
foreign chest states and rentable/rented items). All6refusals fixed,42/42GREEN.
Broader private higher-tier/enhancement/stocking/save selection passes319/319. Native checks remain.

Independent render_completion review identified the portable-owner gap before
its RED probe. The source publication is bounded to the recorded seven-file manifest.

Q1: chance is marked before the draw, including misses; save/load cannot reroll;
equip/unequip uses native enhancement hooks. No clock, shop or death-path edits.
Q2: actual ContainerPart and Physics ownership agree before mutation; table,
blueprint and exact allowed registry types all gate sourcing. Enhancement tier1
is independent of equipment tier4. Existing Commerce is unchanged deliberately.
Q3:36dedicated cases cover changed owner/source/shape, rental flags,0/100chance,
full compatible choices, save replay, registry absence and actual equip reversal.
Q4: these are standalone logic checks, not Unity, natural acquisition or balance.
The marker is instance metadata, not a source of repeated respawning stock.

Exact existing-source differential: 277/277 before, 277/277 after; zero newly failing or passing. Production candidate restored and hashes refreshed after the baseline isolation.

## Published files

- `Assets/Scripts/Gameplay/World/Generation/LootStocker.cs`
- `Assets/Scripts/Gameplay/World/Generation/FoundEquipmentEnhancements.cs`
- `Assets/Scripts/Gameplay/World/Generation/FoundEquipmentEnhancements.cs.meta`
- `Assets/Tests/EditMode/Gameplay/World/DensityModifiedFindsTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/DensityModifiedFindsTests.cs.meta`
- `Assets/Tests/EditMode/Gameplay/World/DensityModifiedFindsAdversarialTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/DensityModifiedFindsAdversarialTests.cs.meta`

Raw private receipts and source hashes: `Verification/DensityCompletion/HigherTiers/ModifiedFinds`. Native generated-source/play inspection remains a gate.

## Native focused gate

Both modified-find fixtures passed within the actual Unity422/422 combined selection (Integration/native-c10-c11-fifth-focused). Generated-source play inspection is still pending.


## Diagnostic rejection and miss review closure

Independent source review found that only successful modifiers emitted an outcome; valid misses and registry/ownership/context refusals were silent. The first native acquisition run (`afe6159bf2bd4b8fb89f6478a39c2ab8`) demonstrated why that distinction matters: a test-contaminated registry yielded unmarked items, not ordinary chance misses. The original invalid source-census receipt remains preserved.

The bounded repair retains success records and adds named rejection and marked-miss outcomes. Zero chance has no invented roll; chance25 captures the existing roll; chance100 does not add a draw. Every existing guard retains its prior order, marker timing and refusal semantics. The stock dispatcher calls this optional decorator only for its existing DeepReliquaryT4 source, avoiding irrelevant refusal noise from unrelated tables. Direct wrong-table calls still diagnose that rejection. No source data, rate, price, content, save schema or gameplay RNG changed.

Paired evidence in `HigherTiers/ModifiedFinds/Diagnostics/`:44 diagnostic cases recorded37 intended REDs and7 controls, then44 GREEN;98 affected checks passed before the fixture-isolation follow-up. Reverting only the dispatch guard reproduces two expected noise failures. Four source tables across32 seeds produce128 byte-identical ordered stock/stat/modifier/marker/next-RNG signatures before and after. Independent review found no concrete ordering or ownership blocker. Runtime and separate actual Unity/NUnit-reference compiles have zero errors; native execution is still pending.


### Exact fixture-state restoration

The concrete no-domain-reload leak is also repaired in ItemEnhancingTests. Its setup previously reset the registry to four stubs without teardown. The fixture now preserves both borrowed maps and the exact initialized flag, then restores their original dictionary objects in place, including initially false and deliberately partial states. It does not globally rebuild production state or hide preexisting contamination. Six independent lifecycle cases produced four restoration failures before this change; existing12 plus6 now pass18/18, and the combined affected set passes104/104. Failed-body and teardown-without-setup controls are included. Independent review and the separate actual-NUnit compile are clear. Native execution remains pending. The acquisition preflight still rejects an already-contaminated domain; this prevents the known fixture from creating fresh contamination after the next test suite.


Native retry `6e90df1471fa47f28f38f8b226946824` preserved6 successful checks through real modified-maul acquisition, then failed a harness assumption that exactly one Tab always selects inventory. The run remains a failure with full key timeline and exact editor restoration. The UI supports pointer-driven panel changes; the report previously omitted panel identity, so the precise triggering panel is unknown. The bounded harness now records it and navigates the actual panel through ordinary keys, at most5 transitions, without assigning UI state or retrying a failed fight. This is a harness correction grounded in the real failed route, not a loot/gameplay change or a completed acquisition claim.

### Native pile-selection correction

The real retry `835efc984b3a4d5390a9e4a61902aa45` advances through both ordinary
fights: the acquired modified maul is equipped/inspected, the actual guardian
and legendary keeper die, and the keeper's same enhanced armor spills with its
other equipment. Eleven checks pass before the harness's single-owner menu
assumption fails on that real loot pile. HP42/44 follows earned experience,
not a stat grant. Full report/key sequence/log and exact restoration are retained.
The harness now follows the visible pile's everything-here row and exact owner
picker with actual keys, then chooses Take; it does not directly collect or
select the reward. Complete replay is still required before acceptance.

### Complete native acquisition acceptance

Replay `2863b2680c074bc9bd7aece4fddc6381` passes19/19 with zero unexpected
errors in23.648 seconds. It uses fixed seed1, two labelled transfers to actual
content entrances, native keys, original starting controls and earned equipment.
The actual modified counterweight maul and keeper's lacquered leather armor are
collected, equipped and inspected; both real enemies die; F5, mutation and F6
restore the same acquired identities and depleted source graph. The player ends
42/44HP with naturally earned levels. Full key/damage traces, all candidates,
images, editor log and exact scene/save/preference/input restoration are retained.
Root viewed the restored gear/result frame: native descriptions and equipment
are legible; this foreign-biome source remains the existing sprite presentation.
It does not establish Spread voxel coverage, natural discovery, progression pace,
combat balance, or full animation quality. Earlier failed routes remain evidence.
