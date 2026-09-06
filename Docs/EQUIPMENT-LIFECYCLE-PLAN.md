# Equipment lifecycle and forced removal — A10 / GA03f

Status: COMPLETE, 2026-09-06. Baseline39e11374,9322GREEN →9369GREEN (+47). Focused456GREEN; native111PASS;0compilererrors. Historical source findings/proposals below are reconciled by the implementation log and final scope section; Loadout is the next separate GA03g wave.

## Findings and reach

### 1. Forced subtree removal omits equipment lifecycle (confirmed ordinary-reachable)

`Assets/Scripts/Gameplay/Anatomy/Body.cs:221` Dismember passes BeforeDismember, then calls UnequipSubtree before detaching the limb (:237). CombatSystem.CheckCombatDismemberment calls it (:1645); Axe_Dismember calls it (:88). Therefore this is not limited to the debug dismember command.

UnequipSubtree (:822–879) clears the first slot and all other attached slots sharing the item, clears Physics, and drops or returns the item. It omits:

- EquipBonusUtility.ApplyEquipBonuses(..., false), which reverses Equippable.EquipBonuses and Armor.SpeedPenalty (Commands/Equipment/EquipBonusUtility.cs:5–53).
- Matching InventoryPart.EquippedItems entries. Dismember does not subsequently clear that cache.
- AfterUnequip and ItemEnhancementDispatch.DispatchOnUnequip (ordinary command ordering: UnequipCommand.cs:94–115).
- EquipmentChangeBus notification.

Observable consequences include retained Agility or armor SpeedPenalty, retained Engraved reputation, Lacquered AppliedBonus/AV remaining on dropped armor, GlowQuartz radius remaining on dropped equipment, ghost inventory weight/equipment entries, and equipped light still projected from the actor. Combat AV/DV from ordinary armor is computed from attached Body slots and already disappears; do not claim every defense bonus survives.

The cache error also survives saves: SaveSystem.cs:1507–1526 serializes/restores EquippedItems, and InventoryPart.RefreshLoadedBackReferences (:401–420) then overwrites the dropped item's Physics.Equipped with the old owner. A save control should assert the loaded ground entity remains unequipped, not only compare pre-save lists.

### 2. DropAllEquipment erases equipment it did not process (confirmed compatibility/API failure)

Body.DropAllEquipment (:811–819) visits the body tree, then clears the entire EquippedItems dictionary. CombatSystem.HandleDeath (:1308–1313) calls that before DropInventoryOnDeath; the latter explicitly intends to drop remaining legacy/map-only entries (:1478–1492). The blanket clear makes that fallback unable to see them.

Ordinary command equip on an actor with Body uses body slots, so no normal authored hybrid-body producer was established. A protected existing SpellFxShowcase.cs:224 uses raw InventoryPart.Equip("ShowcaseWard") as a scenario-specific hybrid example; do not edit that work or claim ordinary-world reach. Legacy saved/API states are meaningful controls.

Smallest fix is to remove only cache entries matching each item actually detached by the body. Leave unrelated map-only entries for the existing death fallback. If the chosen public DropAllEquipment contract is expanded to include map-only entries itself, make that explicit and deduplicate the union; do not silently equate the two contracts.

### 3. Loadout equip skips lifecycle (confirmed authoring/API omission; currently no authored spawn reach)

Actual path is LoadoutPart.GrantOne (Gameplay/Entities/LoadoutPart.cs:157–196), not Body.Equip. At :192 it calls InventoryPart.EquipToBodyParts after adding the item and verifying a valid plan with no displacement.

InventoryPart.EquipToBodyParts (:243–267) already updates body slots, cache, Physics, handling refresh and EquipmentChangeBus. It intentionally does not apply bonuses or dispatch actor/enhancement hooks; EquipCommand owns those (:134–197).

Current Assets/Resources has zero case-insensitive Loadout matches; no runtime source constructs LoadoutPart. Bootstrap wires its Factory (:216, :886), and EntityFactory constructs authored Parts and sends ObjectCreated. Docs/LOOT-OVERHAUL.md:309 claiming 16 shipped humanoid loadouts contradicts current content. Existing LoadoutPartTests supplies its own miniature blueprints.

Repair the grant call site if included, retaining no-displacement/carry-on-refusal semantics and reusing the command lifecycle. Do not move lifecycle into raw InventoryPart methods: ordinary commands and transaction restores already apply it separately and would double-apply. Base armor AV/DV already work via attached slots; only specific lifecycle effects are missing.

## Verified content and fixture corrections

- Real IronshodBoots has Armor.SpeedPenalty=5 (Objects.json:7027–7086). ChainMail is not the speed-penalty fixture.
- Dismembering Feet legitimately changes Body.MobilityPenalty (:508–522). Assert final Speed.Penalty equals unrelated penalties plus new mobility penalty, excluding the removed armor contribution; do not expect pre-injury Speed.
- Actual DuelistCutTinkerModification.Apply writes Agility:2 into EquipBonuses and updates an already-equipped actor (DuelistCutTinkerModification.cs:29–43). The shipped recipe is mod_duelist_cut_armor (Recipes_V1.json:59+), with schematic loot entries. HardenedShell adds Armor.SpeedPenalty=10 and updates an equipped actor; use a non-mobile armor slot to isolate this contribution.
- GlowQuartz has a paid mineral-infusion recipe and authored world sources. Engraved/Lacquered are implemented enhancement API controls; this sweep did not establish an ordinary world producer for those two.
- Configure enhancement payload fields explicitly when authoring a test blueprint: assigning Tier does not automatically call TierConfigure. Alternatively use the real Apply/ApplyTier APIs before command equip.
- Existing BodyPartSystemTests.Dismember_UnequipsSubtree (:669) and ground/Physics tests (:1451–1519) use raw InventoryPart.EquipToBodyPart. New bonus tests must begin with successful InventorySystem.Equip(actor, item, targetPart), and assert the positive bonus before injury.
- Do not assert actor AV/DV by a fabricated AV/DV Stat; use CombatSystem.GetAV/GetDV where appropriate. Lacquered also needs direct item Armor.AV and AppliedBonus checks after drop.

## Minimal implementation boundaries

1. Add/reuse one narrow forced-detachment lifecycle at Body.UnequipSubtree. Process each distinct item once, clear all exact matching body slots/cache entries, preserve unrelated items, reverse applied equip contributions, settle Physics/destination, notify equipment, then publish AfterUnequip and enhancement cleanup consistently with the normal path.
2. Forced limb/death cleanup must not be cancelled by ordinary BeforeUnequip veto. Preserve the existing BeforeDismember veto as the outer gate. Reusing normal UnequipCommand unchanged would violate this distinction.
3. Preserve existing destination semantics: provided valid zone drops at actor cell; no-zone removal returns to carried inventory. CheckUnsupportedPartLoss calls the no-zone route (:408), even when triggered from Dismember; do not change cascade destination accidentally.
4. Handle failed ground placement/missing actor position explicitly rather than clearing all ownership and reporting a fall. Actual armor drops are ordinary-reachable; barren-vegetation equipped targets or wrong-zone arguments are robustness controls. A forced carried fallback must not merge away the exact equipped entity or silently fail on capacity.
5. Remove blanket cache clear so map-only leftovers reach the death fallback, or explicitly expand the helper contract and test that expanded behavior.
6. Loadout call-site repair can be a separate bounded slice because current ordinary content reach is absent. No source reason to change raw storage APIs or save format.

## Qud reference checked directly

`/Users/steven/qud-decompiled-project/XRL.World.Parts/Body.cs:2490–2505` calls Part.UnequipPartAndChildren during Dismember. `XRL.World.Anatomy/BodyPart.cs:3545–3609` sends CommandForceUnequipObject with NoTake, then uses a forced drop path, excludes natural equipment, and snapshots child traversal. The death guard is Body.cs:3161–3167. These support forced lifecycle semantics; CoO's enhancement effects and world destination choices remain its own implementation.

## Bounded RED/control matrix (32 cases)

1. Command-equipped Duelist Cut armor on a severed non-mobile limb loses exactly Agility:2.
2. Same mod on an untargeted limb retains its bonus and exact link.
3. BeforeDismember veto preserves all slots, cache, stats, Physics, enhancement flags and notifications.
4. BeforeUnequip veto does not prevent forced cleanup after accepted dismemberment; normal Unequip still vetoes.
5. Actual IronshodBoots removal reverses 5 while retaining the newly computed mobility penalty.
6. Hardened Shell speed contribution reverses on a non-mobile armor slot; unrelated speed penalty remains.
7. No-zone removal returns exact item once, clears equipment/cache and adds only legitimate carried handling penalty.
8. Valid-zone removal puts exact item on ground once, not carried/equipped.
9. Multi-slot weapon spanning targeted and surviving limbs clears all its aliases once.
10. Another weapon on an untargeted limb remains equipped.
11. Repeated dismember/DropAll does not reverse bonuses twice.
12. Two distinct items in a removed subtree each clean up once.
13. Natural _DefaultBehavior is never manufactured as loot.
14. Engraved player reputation returns to prior baseline and AppliedBonus=false.
15. Engraved NPC/no-prior-application control does not subtract player reputation.
16. Lacquered drop restores item base AV and flag; later pickup/re-equip reapplies once.
17. GlowQuartz removal restores radius/flag; intrinsic light radius remains.
18. EquipmentChangeBus changes on actual removal, not veto/no-op.
19. No-zone light removal recomputes LightMap without needing a zone move/version bump.
20. AfterUnequip observer sees cleared body/cache/Physics and correct destination; multi-slot fires once.
21. Save after dismember preserves ground identity and null Equipped owner, including shared token alias.
22. Healthy equipped save followed by forced removal reverses persisted enhancement state exactly once.
23. Real lethal combat drops body equipment once; carried inventory still drops.
24. Body plus map-only equipment death preserves and drops both exact entities.
25. No-Body legacy death still drops its equipment.
26. Temporary/NoDropOnDeath preserves existing suppressed-drop behavior.
27. Explicit Loadout Equip IronshodBoots applies 5; Carry control applies zero.
28. Loadout equipment then normal Unequip returns penalty to baseline, not below it.
29. Explicit Loadout enhanced armor invokes OnEquipped once; carry leaves flag unchanged.
30. Loadout occupied slots/missing Body remain carried without displacing existing gear.
31. Initial proposal assumed Loadout duplicate ObjectCreated idempotence; source sweep disproved it. Preserve repeated grants; see final scope reconciliation.
32. Failed ground placement or wrong-zone owner argument retains a concrete destination; label extension robustness.

## Limits and additional source seams

No generic callback-exception rollback claim. Snapshot item/limb sets if newly added AfterUnequip or enhancement hooks can mutate them. Do not double-reverse fresh callback-created equipment. No new per-frame work is needed.

Raw equipment APIs have no generic applied-bonus ledger. A historical save or direct raw API setup whose bonuses were never applied cannot be inferred reliably from current stat totals; do not claim automatic migration without a separate contract.

RemovePartsByManager and excess implied-part removal bypass UnequipSubtree, but no current ordinary runtime caller for managed-part removal was found. Keep that API extension debt distinct unless the wave deliberately includes it. Body.Dismember also lacks a foreign-limb ownership gate; malformed cross-body calls should not be presented as normal player reach.


## Implementation order and gates

1. GA03f: forced equipment detachment and death cache preservation, with real command-equipped items, failing regressions, minimal implementation, dedicated20–40taxonomy cases, cold-eye/Qud comparison, native keyboard scenario, full suite, same-commit living docs.
2. Treat Loadout lifecycle separately: current authored reach is absent. Repair the supported authoring/API path after the ordinary forced-removal wave, and correct stale content claims without inventing new NPC equipment.
3. Native gameplay controls must exercise an actual equipped item and accepted/vetoed dismemberment or death through existing scenario/input controls. Explicit injury fixtures are labelled; script observability does not prove limb-loss feel, sprite placement or lighting appearance.

## Performance

Read PERF-FOUNDATION before touching the forced-removal turn path. No new per-frame listener/cache; bounded equipment traversal only on actual injury/death. Snapshot collections only where callback mutation demands it. Preserve equipment notification and cell-dirty ownership. No optimization or speedup claim; if adding per-turn hot-path work, collect matching60–90s native before/after workload.

## Next source checks

Root must read the rest of this sweep's references and exact API signatures before writing fixtures. Verify BodyPart creation/targeting, command Equip return semantics, DuelistCut and GlowQuartz APIs, observer and fixture static-state isolation, drop placement refusal and death fallback ordering. Preserve protected CombatSystem/Entity/Save/FX work with explicit attribution.

## Root source cross-check before initial tests

Read normal Equip/Unequip and EquipBonusUtility against Body.UnequipSubtree: the
ordinary command applies/removes bonuses, then emits AfterUnequip and enhancement
cleanup; forced removal currently does none of that. GetAllEquipped is a distinct
snapshot of dictionary values only. Inventory.FinalizeLoad refreshes cached
Physics.Equipped ownership after body resolution, confirming the stale-save alias.
Qud Body2490–2508 calls forced Part.UnequipPartAndChildren after BeforeDismember;
CoO keeps its own enhancement and destination semantics.

Actual Objects.json fixtures validated read-only: Buckler has Hand slot, AV1/DV1;
LeatherGloves has root-level Handwear, so it is a countercheck for arm loss;
IronshodBoots has Feet slot and SpeedPenalty5. DuelistCut.Apply accepts armor with
Equippable and supplies Agility2; modify the actual Buckler before command Equip.
GlowQuartz TierConfigure supplies RadiusBonus=Tier; OnEquipped sets AppliedBonus
and item LightSource, so apply/configure before normal Equip and assert positive
radius/flag before removal. EquipmentChangeBus is a monotonic global version,
not a subscriber event. Tests compare delta and restore borrowed state explicitly.
Body.UpdateMobilityPenalty stores its contribution in actor MobilityPenalty; do
not subtract legitimate injury cost when asserting removed armor penalty.

## Contract decisions before RED authoring (GA03f)

- DropAllEquipment will process the distinct union of body equipment and remaining
  equipment-cache entries, rather than erase unprocessed cache entries. Its public
  promise is all equipment, so the expanded compatibility behavior is explicit.
  Ordinary body-aware items and legacy/map-only API states get separate tests.
- On missing/incorrect drop zone placement, retain the exact item carried rather
  than clear every owner and lose it. Forced fallback bypasses ordinary capacity
  refusal and stack merging; accepted limb loss cannot silently delete equipment.
- Forced cleanup reverses normal command-applied bonuses and dispatches settled
  AfterUnequip then enhancement cleanup in the ordinary sequence. BeforeUnequip
  cannot veto injury/death cleanup; BeforeDismember remains authoritative.
- Add event/ForcedUnequipped diagnostics for successful forced item cleanup with
  actor/item, blueprint and final destination. Test records per distinct item and
  paired rejected injury/no equipment controls. EquipmentChangeBus invalidates
  caches once per item cleanup; no frame listener or new content is introduced.
- GA03f baseline39e11374,9322GREEN. Body/Loadout/Combat beforecopies captured;
  Body and Loadout are clean, Combat is protected. Root implementation should
  prefer Body only. Loadout bonus/hook repair remains the next separate API wave.

## Initial regression authoring

Authored14 initial forced-equipment regressions/controls using actual Objects.json
items and normal command equip. First compile attempt missed CavesOfOoo.Data for
EntityFactory; root checked its actual declaration, corrected the import, retained
compile log and reran before any production edit. No runtime RED is claimed for
that failed compilation. The native verification driver is being drafted under/tmp
independently; root will review/apply it after source and test gates.

Executed initial13 (not14) tests:3PASS/10FAIL,0CS,10:21:34Z,.3831101s.
Failures reproduce accepted-injury bonus/cache leaks, missing post-event/enhancement
cleanup, lost compatibility-cache equipment, invalid ground destinations, armor
penalty residue and saved ownership restoration. Veto, other-arm and normal
unequip controls pass. Implemented minimum in Body only, then launched initial
plus anatomy/combat neighbors. No native or complete-feature claim yet.

Minimum initial/anatomy/combat neighbors134/134GREEN,0CS,10:24:21Z,.4842798s.
Dedicated adversarial authoring now probes multi-slot identity, surviving root
handwear, exact fallback/capacity, missing inventory, lighting invalidation without
membership changes, diagnostics, real combat death/NoDrop policy and callback
hypotheses. Independent review found same-limb reentry, re-equipping the removed
item, and installing a different item into the still-attached doomed hand.
Fresh explicit target plans already reject detached slots by exact membership
(TargetPartCompatibilityRule30–46); no planner change is needed for that flow.

Failed-add preservation is currently scoped to Body forced removal. The later
Combat.DropInventoryOnDeath caller can still discard a vegetation-tagged equipped
API fixture when a barren ground refuses it. No ordinary authored equipment with
that tag was established; record the complete-caller robustness issue with the
remaining AddEntity-failure queue, and do not claim it fixed by Body fallback alone.

Dedicated22 plus initial13:32/35PASS,3confirmed callback failures,0CS,
10:30:17Z,.457001s. New item could equip into the doomed hand; same item could
re-equip during its enhancement cleanup; same-limb reentry crashed outer removal.
Minimum fix settles anatomy/regeneration bookkeeping before publishing cleanup,
clears captured detached plus surviving shared slots, mirrors unsupported-part
removal, and guards the item through callbacks. InventoryTransaction gains one
internal guard factory; an existing outer claim remains owned by that command.
This is a documented two-file expansion from the preferred Body-only repair.
Removed the superseded private ClearEquipmentFromAllParts helper; the captured
slot pass now handles both attached and detached references. Focused run pending.

Focused156/156GREEN,0CS,10:32:57Z,.6681726s after the first callback repair.
Added six guard-lifetime/regeneration/saved-detached-alias/unsupported-hand pins;
all pass. The throwing-observer test asserts only eventual item-guard release,
not whole-observer rollback or enhancement settlement after an exception.

Independent review then proposed a second-item nested-drop hypothesis: an injury
has detached a subtree containing two command-equipped items, and item A's
AfterUnequip calls public DropAllEquipment. Cache-only nested removal of B fails
to clear its detached slot; the outer pass removes its bonus and emits again.
Paired explicit two-Hand-on-one-Arm API fixture confirmed: focused164,163PASS/
1FAIL,0CS,10:44:05–06Z,.7420474s. Six additional pins and the no-nested control
pass. Expanding the same hypothesis to Extrinsic limbs before the next fix:
DismemberedParts does not track those, so scanning regeneration records alone
would be incomplete. Also corrected the carry no-merge fixture to apply identical
DuelistCut payloads to both Bucklers and assert CanStackWith positively.

### Qud ordering comparison

| Surface | Qud reference | CoO GA03f | Classification |
|---|---|---|---|
| Accepted dismember order | Body2505 UnequipPartAndChildren; CutAndQueueForRegeneration later at2614 | Detach/register before forced AfterUnequip callbacks | Deliberate divergence: fresh plans cannot target doomed slots and recursive same-limb removal refuses |
| Normal unequip lifecycle | Reference-level forced part cleanup | CoO command bonus reversal, settled AfterUnequip, item enhancement dispatch | CoO-specific extension; no full Qud parity claim |
| Callback guard | Not a direct port | Borrow existing inventory claim or own a finally-released guard | CoO transaction integration |

Method summary updated to remove its stale unequip-before-detach ordering and
redundant summary block. No new per-frame or per-turn listener was introduced.

Expanded regular/extrinsic nested-drop gate32:30PASS/2FAIL,0CS,
10:46:05–06Z,.4569399s. Both no-nested controls and corrected CanStackWith fixture
pass. Repaired by a private transient removal-scope stack: nested public cleanup
can discover and clear exact equipment aliases even on unrecorded extrinsic
subtrees. Scope pops in finally; no public/saved field added. Final focused166/
166GREEN,0CS,10:47:15–16Z,.8207214s.

First native run43d3cb546d034a13b524277d74a22de7:99 checks passed before its
combined death-screen recovery assertion failed. Raw final JSON/log retained;
final report has one bench failure plus one unexpected assertion error, with
shutdown root held and saving unregistered. Existing F6 controller adds "Game
loaded."; DeathScreenController succeeds without adding that message. The shared
driver assertion incorrectly required it for both routes. Split graph/clock checks
from the F6-only message receipt; final native rerun remains required before
classifying recovery as verified.

Independent review added one adjacent pending-item hypothesis before completion:
ordinary UnequipItem of the second item awaiting forced subtree cleanup can bypass
the active detached-slot view and remove its bonus twice. Paired regular/extrinsic
cases are now executing before any corresponding implementation.

Pending-item ordinary-transfer RED34:32PASS/2FAIL,0CS,10:50:21–22Z,.5351344s.
Both regular/extrinsic pending items accepted an independent ordinary Unequip.
Added complete-set claims before the first callback; newly acquired guards release
in reverse order in finally, preserving any existing outer claim. Unrelated spare
items remain outside the set. Expanded focused456/456GREEN,0CS,
10:52:25–27Z,1.4085828s. Independent final source pass reports no outstanding
must-fix in this bounded wave.

Final native b400878318fc4156a9465bbb91ce3fe2:111PASS/0FAIL/0unexpected errors,
12.4065528seconds; shutdown12.4267235 with runtime saving unregistered and isolated
root held through teardown, then removed. Death-screen L restored the fresh graph,
healthy equipped bonuses, trap and saved clock. Corrected F6-only message receipt
is separately asserted. Full suite is running.

### Final scope reconciliation

The original32-row matrix was a proposal, not an executed coverage claim. This wave
ships13initial plus34dedicated cases, strengthened by existing anatomy/inventory/
enhancement neighbors. GlowQuartz and actual DuelistCut/HardenedShell/boots have
new direct producer/cleanup checks. Engraved/Lacquered and natural/default/Temporary
policies retain their existing dispatch/policy coverage; no new dedicated checks
for those rows are claimed here. The shared enhancement dispatcher and death
suppression policy are reused unchanged. Loadout rows move to GA03g because no
current authored world producer exists. Original row31's once-only premise was
false: Loadout has a recursion-depth guard, not an applied flag, and repeated
ObjectCreated currently grants again. Preserve that compatibility contract in
GA03g; do not add an unplanned saved flag.

Implementation core: snapshot distinct participating items; register detached
subtrees through a finally-scoped private list; guard the complete item set;
reverse ordinary contributions; clear exact attached/detached/cache aliases;
settle a real ground or exact carried destination; refresh and diagnose; dispatch
AfterUnequip then enhancement cleanup. Repeated/nested forced cleanup skips items
already cleared. No hot-path polling or save fields added.

Full9369/9369GREEN,0CS,2026-09-06 10:55:03Z–2026-09-06 10:57:21Z,137.99489s.
Final evidence in GA03f-REPORT.md; GUID2448valid/unique,0collisions.
Files: Body.cs, InventoryTransaction.cs; two GameAuditEquipment test classes and
metadata; EquipmentLifecycleBench/scenario player/editor launcher and metadata;
phase plan, audit/smoothing/daily docs, raw GA03f verification and independent review.
