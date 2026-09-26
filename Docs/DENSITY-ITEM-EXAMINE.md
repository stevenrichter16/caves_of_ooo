# Density Phase 1 — truthful item examination

> **Status:** implemented; standalone differential, focused native tests and
> live inspection GREEN. Full native sweep: **15,655/15,655** (2026-09-26).
> **Plan source:** `QUD-DENSITY-GAP-ANALYSIS.md` §6, Phase 1, item 5.
> **Scope:** make existing weapon, armor and tonic behavior legible through
> Examine. Preserve authored prose; add no item lore or gameplay mechanics.

## 1. User-visible invariants

- Examining a weapon reports that instance's damage dice, accuracy and
  penetration modifiers, damage attributes, effective equipment slots and
  authored on-hit effects. These are item contributions, not a prediction of
  the inspecting actor's final damage or hit chance.
- Examining armor reports its AV/DV contributions and real equipment speed
  penalty, including negative values and zero-valued protection where useful.
- A tonic on the ground has the same factual payload details as its inventory
  description. Inventory offers one useful Examine action, displayed above the
  still-open inventory; examination consumes no item, turn or random roll.
- Existing flavor, enhancement descriptions and current affliction lines
  remain visible exactly once. Material crafting guidance keeps its priority.
- Creatures' natural armor and natural weapons do not become item inspection
  blocks merely because they share the underlying parts.

## 2. Pre-implementation verification sweep

| Premise | Verified source / correction | Implementation consequence |
|---|---|---|
| Weapons, armor and tonics lack Examine altogether | `PhysicalObject` supplies Examinable; `BlueprintLoader.Bake` merges inherited parts and parameters | No new blueprint parts or JSON rewrite |
| All tonics lack useful descriptions | `TonicExamineService.TryDescribe` already constructs real effects and describes their actual fields in an inventory popup | Reuse that implementation; add a mechanics-only helper |
| Ground and inventory Examine share tonic details | `ExaminablePart.BuildExamineLine` only adds authored flavor, enhancements and live afflictions | Append the shared item details in this method |
| The tonic inventory menu has one Examine action | `InventoryUI.OpenItemActionPopup` adds `examine_tonic` and retains the inherited `Examine`; only materials suppress their generic entry | Suppress the redundant generic row for supported item descriptions |
| Weapon numbers are already fully inspectable | `InventoryUI.BuildSlotStats` shows a short equipped-slot line; it omits on-hit effects, attributes, stat caps and armor speed penalties | Add a coherent item description usable before equipping |
| PenBonus is the item's total PV | `CombatSystem.PerformSingleAttack` adds the actor's chosen stat modifier, skill modifiers and critical bonuses | Call it a penetration bonus, not total PV |
| Base damage is a final hit result | Combat rolls the base dice once per penetration, then applies other damage processing | Label the dice per penetration; make no final damage promise |
| An on-hit spec's duration/magnitude can be copied literally | `OnHitEffectFactory` ignores duration for burning/frozen state effects and constructors clamp fields such as acid corrosion | Construct previews through the existing factory, then `EffectDescriber` |
| On-hit probability guarantees an affliction | `OnHitWeaponEffects` requires actual damage > 0 and `ApplyEffect` may reject an effect | Label damaging-hit attempts; do not promise application against every target |
| Armor speed penalties are unused fields | `EquipBonusUtility.ApplyEquipBonuses` applies and removes `Armor.SpeedPenalty` | Report the actual penalty without changing equipment logic |
| Equipment always uses Equippable.Slot | `EquippablePart.GetEffectiveSlots` prefers UsesSlots, then handling-derived slots; `GetSlotArray` also supplies Hand when the legacy slot is null | Use the final slot-array API, including its fallback |
| Broad invented item backstories would be harmless | `Lore/MYSTERY-LEDGER.md` explicitly protects item descriptions, especially the Remnant and unterminated under-writing | Add no origin, chronology, explanatory cosmology or new lore |

The read-only census parses `Objects.json` and follows the same parent/child
parameter merge used by `BlueprintLoader`. Among Item-tagged entries it finds:

| Family | Resolved entries | Nonempty authored Examine text | Qualification |
|---|---:|---:|---|
| MeleeWeapon part | 33 | 1 | WarlordCleaver has prose; ForgedWeapon is a runtime output template |
| Armor part | 13 | 0 | Includes the abstract ArmorItem entry; 12 named armor pieces |
| Tonic part | 17 | 0 | Includes TonicItem and the runtime BrewedTonic output; 15 fixed named tonics |

These are description counts, not a new loot-reachability census. The generic
MeleeWeapon base has no MeleeWeapon part and is not counted. Runtime brews and
forged items must read their live fields, which favors runtime details over
static numbers in prose. WarlordCleaver's existing description is a preservation
control, not a content gap. Sporeblade has no spore-emission payload; its name
must not produce an invented disease claim.

## 3. Bounded implementation

`ItemExamineService.TryDescribeDetails(Entity item, out string details)` is a
read-only, mechanics-only API: no name, flavor, enhancement or affliction block.
It accepts Item-tagged entities with weapon, armor or supported tonic payloads.
`ExaminablePart.BuildExamineLine()` becomes a public read-only composer, adds
those details once, then retains its existing enhancement and affliction paths.
Inventory reuses that full composition in its existing announcement modal.

Initial fields:

1. Weapon base dice per penetration; signed hit and penetration bonuses; damage
   attributes; the stat used for penetration and its actual cap. The legacy
   uncapped sentinel maps to combat's effective cap of 50; inspection shows
   that constant instead of claiming mathematical infinity or a negative cap.
2. Existing class on-hit attempts from `OnHitClassEffects` constants, plus
   per-weapon attempts from parsed specs and `OnHitEffectFactory`. Target
   defenses may reject effects; unknown effect names are honestly labeled.
3. Armor AV, DV and signed speed change from the armor penalty; effective
   slots and any explicit equipment stat bonuses for these supported items.
4. Existing tonic healing, stat surge, status/cure/brew payloads and delivery
   text, via `TonicExamineService.TryDescribeDetails`. The original
   `TryDescribe` public method preserves its output contract for other callers.

No inspection-side `ApplyEffect`, combat roll, mutation of stats, inventory or
world state, blueprint creation, recipe unlock or tinkering enablement is
permitted. The description does not attempt full character-build simulation,
all material physics, gas emission, or every possible custom item part.
Enhancement details already have their own authoritative path and are not
duplicated. All shipped item weapons currently have empty gas-emission specs.

**Ownership:** root owns the shared `ExaminablePart` composition and signpost
hook. The item work owns the new service, tonic helper, inventory routing and
focused tests. No Objects.json edits are planned for this slice.

## 4. RED-first and counter-check plan

Core cases use real factory blueprints where possible:

- Dagger versus a plain nonweapon item; mutation of an instance's dice/hit/pen
  fields; negative modifiers; capped Dagger versus uncapped LongSword.
- FlamingSword versus LongSword; ThunderHammer's class stun plus per-weapon
  electrification; DissolutionMaul's clamped acid preview; VenomDagger's real
  poison dice/duration; Sporeblade must advertise no invented spores.
- LeatherArmor versus Buckler for signed DV; IronshodBoots versus LeatherBoots
  for speed; DissolutionMaul versus a one-handed weapon for effective slots.
- PoisonTonic and the four restored offensive tonics through world Examine;
  healing/cure/stat/brew forms; empty/unrecognized payload controls.
- Existing WarlordCleaver prose, one enhancement and one live affliction
  remain once each; a creature with Armor/MeleeWeapon parts stays on the
  existing non-item examination path.
- Unknown/invalid/zero-chance/>100 on-hit specs, several independent effects,
  null input, changed runtime fields and repeated examination; state snapshots
  verify no inventory, health, effect, clock or RNG changes.

Native inventory cases verify exactly one Examine for tonics, weapons and
armor, visible full text, unchanged inventory/turn state, retained equip/drop
actions, authored prose and material guidance priority. Existing
`MaterialGuidanceInventoryTests` deliberately pin ordinary Dagger examination
to the gameplay log; expanding weapon examination makes that old bounded-scope
expectation obsolete, while its unrelated-material and guidance controls must
remain. `GameAuditActionFeedbackAdversarialTests` relies on the existing
`examine_tonic` command; retain that command as a compatible route.

Run `ExaminablePartTests`, `TonicExamineServiceTests`,
`EnhancementDescriptionTests`, the new core suite and native inventory suites.
Record intentional RED cases separately from already-correct controls, then
run a dedicated adversarial pass, cold-eye review and the final differential
and native verification gates appropriate to the shipped change.

## 5. Qud reference and deliberate divergence

The existing universal Examinable part mirrors Qud's Description/Look concept;
this slice is CoO-specific inspection of already-shipped mechanics. No new Qud
parity claim is made. The original plan requested a first pass of item prose;
this implementation instead prioritizes accurate runtime details because
tonics already have a shared effect describer and forged/brewed/enhanced items
can change after blueprint creation. Flavor prose remains authored separately.

## 6. Implementation / review / verification

The initial sweep was recorded before Assets edits. The existing Examine-event
suite then ran on the private standalone runner against unchanged production:
**44 cases, 41 expected failures and 3 passing non-item controls**. This is an
assertion RED, not an absent-service compile exclusion. Evidence is preserved
in `Verification/DensityPhase1/ItemExamine/core-red.json` and its compressed XML.

The new service and tonic detail extraction are now written. Eight native
inventory cases use the actual action menu and announcement/modal handoff;
their native RED run produced **8/8 expected failures** before changing the
inventory route (three duplicate-Examine failures and five missing-popup
failures). Job `e2ec002d40ee4bdeaa80ef454dd2a1fa` is preserved at
`Verification/DensityPhase1/ItemExamine/native-ui-red.json`.

With the root's shared composition hook, the new core suite and existing
ExaminablePart, TonicExamineService and EnhancementDescription suites pass
**93/93** in the standalone runner. The GREEN receipt and XML are beside the
RED evidence. This is core-logic evidence; it does not establish native UI.

The root coordinates the editor with the parallel rendering audit. This item
slice makes no Objects.json edits. Assets changes pause during each native capture
window so the test runner is not interrupted by script imports.

### Adversarial review

The dedicated 25-case `DensityItemExamineAdversarialTests` suite first produced
**two confirmed inspection defects and 23 already-correct controls** after the
initial implementation. Both are repaired, and the combined core suites pass
**118/118**:

- 🟡 The effect preview originally added only authored weapon attributes;
  combat also adds the chosen stat name. Both now build the same attribute
  collection before applying class predicates. A Cutting-stat/Strength-stat
  pair pins the otherwise unusual but supported branch.
- 🟡 A null legacy equipment slot displayed blank even though equipment's
  `GetSlotArray` falls back to Hand. The description now uses that final API.
- 🔵 Oversized effect probabilities display their effective 100% roll gate;
  invalid and zero-chance specs remain absent, and unknown effects are labeled
  inert. Preview construction respects effect clamps and indefinite states.
- ⚪ The description reports item contributions and attempted effects. Actor
  stats, skills, resistance, target conditions and final combat results remain
  outside its scope. Long-description native layout is reviewed independently.

No gameplay dispatcher, equipment rule, effect or RNG policy changed. The
adversarial RED and final focused GREEN XML/receipts are in the same ItemExamine
verification directory. The full comparison against pre-continuation
`670e3966` uses an identical 10,060-case corpus on both sides: old production
9,672 passed / 388 failed, current production 9,765 passed / 295 failed.
**Zero newly failing, 93 newly passing** across both this item work and the
parallel signpost work. All 295 remaining failures also fail in the baseline.
Both sides select 672 files and exclude 286; native UI/input/rendering is among
the exclusions. Exact names, runtime limits, hashes and compressed XML are
preserved in `Verification/DensityPhase1/Continuation/standalone-final.json`.

### Inventory routing and stale-selection guards

After the initial eight-case native RED, inventory now offers one rich Examine
route, uses the shared full text, preserves `examine_tonic` compatibility, and
gives material guidance priority. Existing material tests deliberately expand
their former Dagger-log-only expectation to a weapon popup while retaining
their ordinary-material and misleading-name controls.

Root's symmetry review identified the direct route's missing observability and
stale-selection handling. Native tests now cover success records, actual
equipped items, removal after opening the menu, and loss of the payload before
selection. The follow-up native RED executed **13 cases: 10 expected failures
and 3 passing controls**, job `6dc63d1e1abe476a9904b674398c09a0`, preserved in
`ItemExamine/native-ui-followup-red.json`.

Both `examine_item` and the compatible `examine_tonic` route now recheck
`InventoryPart.Contains`, which includes carried and equipped items. A removed
item produces `ItemExamineRejected` with `unavailable-inventory-item`; a lost
payload produces `no-supported-details`. Both paths retain the exact menu and
show a visible failure without opening a popup. Successful inspection emits
`ItemExamined` with the blueprint and queues the full shared description.
Actor/target are recorded in their standard diagnostic fields. These changes
do not consume the item or spend a turn. The native GREEN request includes the
new inventory fixture, material guidance and action-feedback regression suites.
The focused editor job `0625b069c36e415285971400651ccecc` passed **83/83**:
13 item inventory, 18 material guidance, 39 action feedback, 4 layout and
9 capture cases. Its receipt is
`Verification/DensityPhase1/CaptureProbe/capture-item-layout-green.json`.

One subsequent root-requested compatibility control removes only the Item tag
from an actually carried HealingTonic. It requires the exact legacy popup,
unchanged stack/state, one Examine row and a success diagnostic. This is an
already-correct branch pin, not a newly discovered defect. It brings the native
item fixture to 14 cases; all are included in the successful final native sweep.
The added case is not included in the 83-case receipt.

### Native layout counter-check

The initial overflow suspicion confused the gameplay's 25 rows with the popup
overlay's actual 45. Root's four native layout probes passed without changing
AnnouncementUI: all **61 supported blueprint entries** fit (largest base popup
17 rows, FlamingSword), real WarlordCleaver and FlamingSword descriptions with
two valid enhancements plus two afflictions remain inside the viewport, and
long-to-short/close transitions leave no old foreground/background tiles.
No speculative scrolling or silent truncation was introduced. These are native
tile-bound checks; the finite Play scenario separately verifies readable
rendering and actual user input.

### Live native inspection

The finite Play run `9c5fa9a75a35496d9b85716e6f5b7b28` passed **16/16 checks**
with zero unexpected `Application.logMessageReceived` errors. Its report and
seven screenshots are in
`Verification/DensityFollowup/NativeExamine/9c5fa9a75a35496d9b85716e6f5b7b28/`.
The run used the native new-game bootstrap and actual keyboard paths through
world Look/actions, the inventory action menu and the announcement modal.

The three item action screenshots each show one Examine. The three description
screenshots visibly retain WarlordCleaver's authored prose and Serrated tier-2
enhancement beside its live mechanics, IronshodBoots' AV/DV and speed penalty,
and the two-unit HealingTonic stack's healing and delivery details. Independent
visual inspection of all seven saved PNGs found upright, legible text; the
item descriptions and their dismissal prompts fit inside the screen. The
signpost screenshot also shows the actual contextual directions in the world
log. The scripted checks confirm return to the same inventory row, restored
normal input and world-log scroll, unchanged turn/HP/effects/item quantities,
and no destination generation or travel-note creation.

**Boundary:** the factory Signpost, WarlordCleaver with Serrated tier 2 and
IronshodBoots were explicitly staged in an owned disposable game. HealingTonic
was the real designed starter stack. This proves those native inspection and
return paths; it does not prove natural acquisition, placement frequency,
ordinary progression or balance. Reflection only observed UI state; input
traversed the real keyboard handlers. The error count covers the managed log
callback and does not substitute for a separate native-backend console audit.
The broader native suite, job `d6a671a95dda48b78b9aa670b39b003b`, passed
**15,655/15,655 with zero failures/skips** in 392.82 seconds;
the live run is not a substitute for that gate or the final legacy-tonic test.

### Final cold-eye checks for this slice

- **Symmetry:** both the preferred item popup and compatible tonic command
  share the same membership, success and rejection paths; material guidance
  keeps its existing route and priority.
- **Public shape:** details omit the header/flavor/affliction/enhancement
  blocks; the public Examinable composer adds each once. Diagnostic actor and
  target are top-level fields, with blueprint or rejection reason payloads.
- **Counter-checks:** signed bonuses, absent payloads, class aliases, factory
  defaults/clamps, null slots, stale items, equipped membership and unchanged
  state have matched controls. The added legacy-tonic case covers the explicit
  compatibility fallback in the final native sweep.
- **Doc alignment:** this is runtime mechanics inspection, not authored lore,
  loot reachability, final damage prediction or new combat behavior. No new
  answer to a protected lore mystery is supplied. Visual verification is
  recorded separately above rather than inferred from GREEN logic tests.

The final source/doc Q1–Q4 reread is complete with **zero new findings** after
the documented attribute, slot-fallback, stale-selection and diagnostic repairs.
The full native gate is complete; its raw receipt is
`Verification/DensityPhase1/Continuation/native-editmode-final.json`.

### Files in this item slice

- NEW `Assets/Scripts/Gameplay/Items/ItemExamineService.cs` + `.meta`.
- MOD `Assets/Scripts/Gameplay/Items/TonicExamineService.cs`.
- MOD `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs` (root-owned shared
  public composer and details hook).
- MOD `Assets/Scripts/Presentation/UI/InventoryUI.cs`.
- NEW `Assets/Tests/EditMode/Gameplay/Items/DensityItemExamineTests.cs` + `.meta`.
- NEW `Assets/Tests/EditMode/Gameplay/Items/DensityItemExamineAdversarialTests.cs`
  + `.meta`.
- NEW `Assets/Tests/EditMode/Presentation/UI/DensityItemExamineInventoryTests.cs`
  + `.meta`.
- MOD `Assets/Tests/EditMode/Presentation/UI/MaterialGuidanceInventoryTests.cs`
  for the documented UI expansion.
- NEW `Docs/DENSITY-ITEM-EXAMINE.md` and the receipts under
  `Docs/Verification/DensityPhase1/ItemExamine/` and
  `Docs/Verification/DensityPhase1/Continuation/`.
- The shared native scenario and its live evidence are owned by the parallel
  native-review work; this doc links them without claiming independent runs.
