# C11.1 — bounded deep-reliquary T4 finds

**Status:** the four authored items, bounded source/context hook and confirmed key-lock authority repair are published; private and shared-source660-case sweeps GREEN. Native acceptance is in progress. This is partial T4 reachability, not completion of C11. Random spawn enhancements, general T4 container/death tiers, legendary templates and new creature variants remain separate. User direction keeps BitLocker out of regular play.

## Sweep and corrections

| Premise | Source finding | Decision |
|---|---|---|
| Existing items merely need T4 table access | Baked Item-tagged blueprint counts are T0=20, T1=106, T2=35, T3=15; none above3. Counts include templates, not actual find frequency. | Author four concrete mundane variants with actual equipment fields. |
| Raise the global table clamp | ContainerPlacementService and LootDropSystem clamp to T3; many natural/settlement tables have no T4 versions. | Leave both clamps and ordinary sources unchanged. |
| A new vault has to be generated | Underground's existing Reliquary is MinTier4 (depth9+), Chance20, with VaultSentinel, a locked chest and IronKey. | Reuse its existing difficulty/location/geometry; one extra weighted T4 item alongside its prior T3 roll. |
| Replacing the shared stamp marker is safe | Generic underground applies the same catalog below every surface biome. | New context overload only permits depth9+, non-POI Spread/Sodden/Beating. Choir, Overwrit, Stump and authored routes retain the old table. Clone the selected stamp rather than mutating shared catalog data. |
| Loot enhancements are already sourced | LootTableRegistry rolls blueprint strings; LootStocker creates them. ItemEnhancing.Apply exists but is not invoked here. | No enhancement metadata, auto-modification or premium-value rule in this slice. |
| A locked chest uses ContainerPart.Locked | The real LockedChest defines LockPart.IsLocked and iron KeyId; ContainerPart keeps its legacy flag false | Verify the LockPart authority and real key action. |
| Two or three old vault picks means two or three item units | A selected GoldCoin row expands10–20 units and the stocker counts successful merged units | Compare deterministic roll units and actual distinct stored stacks separately. |
| Tier means unconditional superiority | Existing T3 Claymore, PlateArmor and unique PalimpsestBlade have different hand/weight/dodge/hit tradeoffs. | Keep two-hand and armor penalties visible. Do not obsolete unique sources or turn every T4 into a universal best choice. |

## Exact proposed content

All four inherit existing ordinary equipment, are takeable, Tier4, with no unique/crafted/rental/faction provenance. Parent handling, slots and category remain inherited. Descriptions state visible construction only and do not explain a protected mystery.

| Blueprint / display | Parent | Authored mechanics | Weight / Commerce | Description |
|---|---|---|---|---|
| TemperedLongSword / tempered long sword | LongSword | 1d10+1, penetration4, Cutting LongBlades; one hand | 9 / 90 | A narrow line follows the hardened edge. The tang runs through a plain grip to a peened pommel. |
| CounterweightMaul / counterweight maul | Warhammer | 2d6+2, penetration6, Bludgeoning Cudgel; two hands | 22 / 110 | A heavy striking head is balanced by a smaller weight below the grip. Both ends are scarred by use. |
| FineRingMail / fine-ring mail | ChainMail | armor6, dodge−1; Body | 24 / 125 | Closely fitted rings lie in shallow folds. Small overlapping links cover the seams beneath each arm. |
| RivetedPlate / riveted plate | PlateArmor | armor9, dodge−4; Body | 46 / 140 | Thick overlapping plates are held by broad rivets. Deep straps carry their weight across the shoulders. |

The sword is heavier and lacks the unique PalimpsestBlade's +1 hit bonus. The maul keeps two occupied hands and rises from Warhammer weight16 to22. Fine-ring mail trades PlateArmor's higher armor for mobility and lower weight; riveted plate gains one armor over plate but costs one dodge and six weight. CounterweightMaul and RivetedPlate receive explicit Steel Material data because their existing parents do not define it; no new material rules are introduced.

Inherited glyphs/colors/categories give honest fallback coverage (`/`, `T`, `[`); set distinctive existing palette colors, preserve ASCII validation and inspection truth. No bespoke 3D asset is claimed. Root's native acceptance must verify all four dropped/held or inspected items remain visible; authored family art may be added only if the existing rendering path actually needs it.

## Source and implementation contract

`FindDeepEquipmentT4` is pick-one, exactly one pick, four equal positive weights, one item each. `DeepReliquaryT4` is independent: one `SealedVaultT3` reference and one `FindDeepEquipmentT4` reference, each100%. Preserve the old2–3 picks, including their existing vault provenance, while adding exactly one bounded T4 piece. The new pool itself contains only the four authored ordinary items. No general pool, hostile loadout, friendly/service NPC, shop or natural container changes.

`StampCatalog.Underground(depth, surfaceBiome, ordinaryColumn)` clones only the eligible Reliquary and its legend, changing `lockedchest:SealedVaultT3` to `lockedchest:DeepReliquaryT4`. The one-argument API retains the original catalog. C10's plan passes surface biome and `poi == null`; authored fallback calls supply no eligibility. Geometry, chance, minimum tier, lock, key, sentinel, existing drop behavior and maximum landmark count remain intact.

Private source ownership: LootTables.json and the narrow StampCatalog overload in LandmarkBuilder.cs. C10 owner integrates the manager call; root applies exactly four additive Objects blocks after reviewing parsed before/after evidence. No shared Assets publication while the native window is active.

## RED, counter-check and adversarial gates

Before production: missing four baked blueprints/stats, pool/source and context overload fail; existing T3 and natural-source controls pass. Then pin exact stats/slot/value/description and meaningful parent tradeoffs; actual StockContainer returns one T4 plus its old roll, capacity refusal cannot clone it, 0/100 controls and zero-weight controls work, deterministic seeds reach all four, and inspect/equip/save/unequip preserve identity and fields.

Test source eligibility at depth8/9, each allowed/excluded biome, POI/non-POI and old API; mutate returned catalogs to prove no leak into subsequent calls. Force the actual stamped Reliquary to validate locked chest, key, sentinel and item, then verify ordinary generated depth9+ source reachability without forcing a new population or chest. Check a legal approach to both key and lock, and real open/unlock action where supported. Multi-seed receipts record occurrence and additional item/value distribution separately from the C1 baseline census; no map-count promise follows from the stamp's20% roll.

Adversarial checks include unknown blueprint validation, nested cycles, source leakage into T1–3/natural/death/shop paths, full containers, reload/duplicate stock ownership, missing factory content and silent failure. Existing valid caller contracts remain unchanged. Narrow findings are repaired after a confirming RED.

Native acceptance remains root-owned: actual deep-source approach/acquisition and readable truthful item inspection, followed by integration tests. Standalone proves content inheritance, table math, core actions, deterministic generation and save graph contracts; it cannot prove native rendering, controls, Unity-identical maps or play balance.

## Review

- 🟡 Shared catalog mutation and lost surface-biome context are concrete risks; the overload clones and explicitly gates context.
- 🟡 New value is one90–140 item per actually placed eligible deep reliquary, not per zone or general container. Measure generated frequency before calling tuning acceptable.
- 🔵 Existing lock/key/sentinel, rare stamp chance, old vault roll and item machinery are reused.
- ⚪ This slice supplies four T4 finds at one real deep source. It does not finish general higher tiers, enhancements, legendary ownership or C11.

## Implementation log and evidence

The initial24-case content/source RED produced23 intended failures and one
existing-source control. Two fixture corrections were preserved separately: the
actual table is `DeathHumanoidT3`; the real lock is `LockPart.IsLocked`; and old
vault coin rows expand units beyond their2–3 pick count. No production changes
were made to satisfy those mistaken fixture assumptions. The corrected core
fixture passed24/24 after four additive blueprints, two additive tables and the
context-aware catalog overload. Parsed comparison: **494 existing blueprints
unchanged, four added;96 existing loot tables unchanged, two added.** Root applied
the shared Objects blocks and preserved its own parsed receipt.

A subsequent actual source-wire counter-check reverted only the manager's call
to the original one-argument catalog. The generated60-zone test failed because
all new finds disappeared, while the new items/pools still existed. Restoring
the context call restored its passing result. This distinguishes a working
ordinary-depth route from an orphan content table.

The same sweep reproduced a preexisting lock bypass:20 matched authority cases
produced **six failures and14 controls**. A real LockedChest's key lock did not
block OpenContainer events, Take, TakeAll or Put; menu collection also offered
Open while key-locked and duplicated Unlock when both lock fields were true.
The bounded repair adds a read-only `ContainerPart.IsLocked` property combining
its legacy saved field and the live LockPart. Existing open/transfer gates read
that property; menus defer the key lock's Unlock action to LockPart. Neither
saved field is removed, cleared or rewritten, and generation's AddItem can still
stock locked containers. Root owns the corresponding native InputHandler stale
Open-menu gate and its RED test before that UI change.

The private final suite passed **660/660**:61 new content, gate, lock, adversarial
and generated-source cases plus599 existing selected lock/container/loot/landmark/
item-inspection/equipment cases. Additional tests cover actual equip/unequip,
complete save graph identity, full capacity, invalid tables/cycles, stale menus,
legacy/real/both lock states, and the real stamped key's successful reusable
unlock versus the absent-key refusal. The historical C1 census's “opened” count
was corrected to handled opening attempts; see its living doc and C12 review.

Unforced generated sample: five seeds, four surface biomes, depths8/9/12,
**60 zones =30 eligible deep contexts +30 excluded controls**. One eligible
source appeared: seed729490642, Sodden `Overworld.13.0.12`, one RivetedPlate worth
140 Commerce. Its key, original VaultSentinel and lock remained present, with a
terrain/prop route to key and chest. No excluded context gained T4. That is
140 added Commerce across30 eligible zones (4.67 per selected zone), not a global
balance estimate; this sample observed only one variant naturally. All four
variants have independent real-stock/equipment/save proof. Current stamp chance,
placement space and the maximum landmark count bound frequency; no guarantee
of a reliquary on every deep floor is introduced.

Independent render source review found truthful existing coverage: held sword
uses the LongBlades equipment-blade family; held maul uses the Cudgel/Bludgeoning
equipment-club family. Dropped sword uses its generic blade fallback, dropped
maul retains CP437 T, and the two dropped armors retain their armor glyphs. There
is no body-armor3D overlay; native equipment state still applies, while unsupported
body slots use the existing fallback contract. No bespoke dropped models or
visible body-armor3D are claimed. A native visibility/inspection observation
remains necessary; this paragraph reports source review only.

Receipts live under `Verification/DensityCompletion/HigherTiers/`: original and
corrected RED, fixture-correction XML, lock-authority RED, source-wire negative
control, private660 GREEN, generated sample, exact additive Objects/table
proposals, parsed diffs and publication hashes. The source snapshot came from
the completed integration checkpoint; C10's private plan was combined for
source proof, then the context hook was applied narrowly to its final shared
manager after its188-case GREEN. No whole shared manager was replaced.

### Changed files and current acceptance boundary

- Root: four additive Objects.json blocks; native InputHandler lock read after UI RED.
- LootTables.json: FindDeepEquipmentT4 and DeepReliquaryT4 only.
- LandmarkBuilder.cs: explicit context overload and isolated changed stamp.
- OverworldZoneManager.cs: one context-aware catalog call coordinated with C10.
- ContainerPart.cs, InventorySystem.cs, TakeFromContainerCommand.cs, PutInContainerCommand.cs: shared computed access authority.
- Four new fixtures/metas: DensityHigherTierFindsTests, DensityHigherTierFindsAdversarialTests, DensityHigherTierGeneratedSourceTests and DensityContainerLockAuthorityTests.

Q1: both saved/current lock authorities deny all generic core access directions;
stocking remains a separate creation operation. Q2: old catalog callers and saved
fields retain their contracts; only explicit ordinary context opts into T4. Q3:
source wire, depth/biome/POI gates, chance/weights, capacity, keys, save identity,
metadata and real equipment have matched controls. Q4: the four new names and
prose make no canon claim; all reports distinguish this one-source partial slice
from general higher-tier loot and preserve failed runs.

**Can verify:** inherited mechanics, table validation/replay, contextual source
wiring, actual core lock/key/inventory actions, ownership, save and sampled
generated terrain/prop reachability. **Cannot verify from standalone:** native
rendering/input, Unity-identical maps, balance/feel or prevalence outside this
bounded sample. Native and final integrated regressions are separate gates.

### Shared-source confirmation

After root applied the four Objects additions, the same private runner compiled
shared Assets directly with no production overrides: **660/660 GREEN**. The
unforced60-zone census repeated the same single eligible source and zero
excluded sources. `shared-nearby-green.json` and compressed XML pin exact counts
and content hashes; `shared-generated-census-summary.json` pins the natural
source and valuation. This does not subsume root's native UI/rendering checks.

### Full baseline-corpus regression after publication

The frozen post-C10/C11/core-lock snapshot retained the original672-file
selection. Baseline10,060 cases:9,765 passed,295 known standalone-environment
failures. Current10,075 cases:9,780 passed, the same295 failures; **NEWLY FAILING0,
NEWLY PASSING0**. Test-name changes are explicit:58 added names,43 removed,
reflecting renamed content cases and added cases inside selected existing files.
New fixtures remain separate focused coverage. The first attempt omitted the
old non-source Assets/Scenes link; that DirectoryNotFound failure was archived,
link coverage restored and the complete run repeated without any gameplay or
assertion edit. `Integration/standalone-final-after-c11.json`, compressed XML,
name diff and snapshot hashes preserve this checkpoint. Native UI changes are
outside the standalone runner.


### Native integration and stale-menu closure

The affected Unity selection passes 580/580, including the higher-tier source,
lock and equipment cases. The actual 60-zone native cohort produced three source
caches: CounterweightMaul (seed1729, Beating depth9), TemperedLongSword (seed2026,
Spread depth9), and RivetedPlate (seed2026, Beating depth9). Each had a reachable
key and chest approach; the 30 excluded contexts remained empty of these items.
This runtime differs from the runner's stable string hashing. The receipt is
`HigherTiers/native-generated-source-census.json`; its historical embedded
standalone boundary text is inaccurate for this copied native output and is
corrected in the test's next publication. It does not prove player acquisition.

Root found one remaining UI consumer checking only ContainerPart.Locked after a
previously gathered Open action. Native RED: one failed part-lock case, three
passing legacy/both/unlocked controls. InputHandler now uses computed IsLocked;
all four cases pass in the 580-case native receipt. The test keeps original item
identity, actor inventory, flags and input state assertions, so merely hiding a
popup while transferring the item cannot pass.
