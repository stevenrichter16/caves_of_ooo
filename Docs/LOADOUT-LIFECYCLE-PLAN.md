# GA03g — Loadout equipment lifecycle: verified source and bounded TDD plan

Status: COMPLETE, 2026-09-06. Baseline4a4b8972,9369GREEN →9417GREEN (+48). Focused406GREEN; native24PASS;0compilererrors. Source preparation and proposals below are historical; final implementation and actual scope are reconciled in GA03g-REPORT.md.

## Goal and finite scope

An entity created with a Loadout `Equip` or `Pick` entry should receive the same applicable equipment bonuses, actor events and enhancement hooks as an ordinary successful equip command. A refused auto-equip should leave successfully granted gear carried. Existing equipment must not be displaced. Preserve the loadout's no-Body behavior, chance/count parsing, recursion guard, independent grants and save fields.

Primary implementation should be in LoadoutPart. Reuse InventorySystem.AutoEquip; do not relocate lifecycle effects into raw InventoryPart storage methods. A source-confirmed stale-plan seam in AutoEquip needs a RED if the wave promises no displacement when newly enabled BeforeEquip hooks alter a destination. Keep any resulting command repair narrow. Generic callback exception atomicity, parser redesign, new world content and repeated-ObjectCreated idempotence are separate contracts.

## Source-corrections table

| Premise | Current source and consequence |
| --- | --- |
| Sixteen humanoids currently have authored loadouts | `Assets/Resources` contains zero case-insensitive `loadout` matches. Scripts have only LoadoutPart itself, GameBootstrap Factory wiring (:216/:886) and LootDropSystem classification (:126), with no AddPart/new/runtime blueprint producer. Docs/LOOT-OVERHAUL.md:311–319 is stale. NewGameLoadout is a separate starter-kit helper. Do not claim an ordinary enemy-spawn bug or silently add content. |
| Loadout already goes through equip commands | LoadoutPart.GrantOne (:157–198) creates, AddObjects (:184), plans (:188), then calls raw `inventory.EquipToBodyParts` (:192). This skips EquipBonusUtility, BeforeEquip, AfterEquip and enhancement dispatch. Raw storage already handles slot/cache/Physics, carry refresh and EquipmentChangeBus. |
| The minimal replacement can call AutoEquip unconditionally | Existing EquipPlanner rejects missing Body (:43–47); Loadout therefore carries gear on no-Body actors. AutoEquipCommand has a legacy slot fallback (:117–130). Preserve an explicit Body gate or retain the existing body-plan gate around command delegation. |
| Repeated ObjectCreated is idempotent | LoadoutPart has only static `_depth`/MaxDepth=3 (:75–76), increment/decrement in a finally (:102–122). There is no per-entity applied flag. Replaying ObjectCreated grants again. Do not add a saved flag or write an idempotence assertion in this wave. Ordinary factory creation sends it once. |
| Equip entries are always 100% | The comment at LoadoutPart:105 says always, but Equip uses ParseCarry (:107) and GrantOne chance gating (:161–162), so explicit chance/count syntax works there too. Preserve this behavior and correct the wording if touched. |
| Chance text includes a percent sign | Valid text is `35`, not `35%`. ParseCarry's int.TryParse fallback repairs malformed text to 100 (:251). `%` in documentation is explanatory notation, not accepted syntax. |
| Body Params Anatomy=Humanoid configures anatomy | Body has no such public field/property. Existing LoadoutPartTests JSON at :38 works because EntityFactory defaults missing entity property Anatomy to Humanoid (:458–462). A nonhumanoid test must set entity `Props["Anatomy"]`, e.g. Quadruped. |
| Add refusal aborts the entire loadout | It returns from the current GrantOne/count loop (:184); Apply continues later independent Equip/Pick/Carry specifications. Preserve successful prior grants and later affordable specs. |
| Save/load should rerun the grant | SaveGraphSerializer.LoadPart (:1435–1459) constructs/deserializes the existing Loadout Part; it does not send ObjectCreated. The three public strings round-trip; private/static fields do not. Save/load should preserve exact items without a second grant. |
| Existing tests establish lifecycle correctness | LoadoutPartTests' 11 cases mostly assert synthetic item names/cache counts; actors lack relevant stats. LootOverhaulAdversarialTests:169–205 uses only refused Boulder900 against cap150, so its “partial loadout” test has no successful grant to preserve. Add actual positive preconditions. |
| AutoEquip's no-displacement plan stays current through hooks | AutoEquipCommand builds a plan before BeforeEquip (:92–114). EquipCommand reuses it after the event (:134–153, :214) and subsequently UnequipCommands current claimed-slot occupants (:229–236). A callback can fill an initially empty slot and still be displaced. This is an existing command seam newly reached by this loadout lifecycle reuse. |

All relative paths resolve under /Users/steven/caves-of-ooo.

## Producer-to-consumer path and API shapes

`EntityFactory.CreateEntity(string)` / `CreateEntity(Blueprint)` in Data/Factories/EntityFactory.cs attaches all blueprint Parts after parameters are applied (:202–219), initializes anatomy (:222), then sends ObjectCreated (:225). Actor stats exist before Parts are attached (:186–199). Loadout.HandleEvent (:87) calls private Apply; Apply requires ParentEntity, Factory, Inventory and available recursion depth. Equip list runs first, Pick second, Carry last.

Public authoring fields are exactly `string Equip`, `string Carry`, `string Pick`. The XML-like description in Qud is not CoO JSON. Valid CoO example:

```json
{"Name":"Loadout","Params":[
  {"Key":"Equip","Value":"IronshodBoots"},
  {"Key":"Carry","Value":"HealingTonic:35;GoldCoin:80x3-9"},
  {"Key":"Pick","Value":"1;Dagger;ShortSword"}
]}
```

`Pick` parses `N;a;b;c`, trims entries, samples without replacement from list positions, clamps N to the pool size, and grants each chosen entry at 100% once. Duplicate blueprint strings remain distinct pool entries; “without replacement” does not mean unique blueprint names. Preserve the supplied `LoadoutPart.Rng`; use a deterministic test RNG rather than probabilistic expected results.

The reusable facade is `public static bool InventorySystem.AutoEquip(Entity actor, Entity item)` (InventorySystem.cs:167). It runs AutoEquipCommand through InventoryCommandExecutor and a local InventoryTransaction. Validation failure returns a result; execution failure rolls back that equipment command. No zone is required for initial equip. It does not spend TurnManager energy.

AutoEquip accepts owned equippable singletons, refuses StackCount>1, requires no displacement, and delegates to EquipCommand.ExecuteInternal. That command runs BeforeEquip, performs storage, registers equipment/stat undo, applies EquipBonuses and Armor.SpeedPenalty, sends AfterEquip and dispatches enhancement OnEquipped (:134–197). Ordinary Unequip reverses the same contributions. Do not use regular InventorySystem.Equip as a substitute: it allows displacement and can split stacks.

Raw methods remain storage primitives: InventoryPart.EquipToBodyPart (:218), EquipToBodyParts (:243), Equip (:315). Moving lifecycle into them would double-apply normal commands and interfere with transaction restores. InventoryPart.EquippedItems uses body-part ID strings, and Physics.InInventory is null while equipped.

## Minimal implementation proposal

Smallest reviewable one-hunk version: keep the current successful/no-displacement body-plan preflight and replace only the raw storage call with `InventorySystem.AutoEquip(ParentEntity, item)`. This preserves the missing-Body gate and makes command refusal leave the item carried, at the cost of one extra finite plan build per grant.

Equally bounded cleanup: remove LoadoutPart's Planner field/import and use `if (tryEquip && ParentEntity.GetPart<Body>() != null) InventorySystem.AutoEquip(ParentEntity, item);`. AutoEquip supplies the ownership/equippable/plan gates. Document the no-Body gate so a future refactor does not restore the legacy fallback accidentally. Do not remove the successfully granted item when AutoEquip returns false.

The grant's AddObject is outside the equipment command transaction on purpose. A veto/failed equip should retain the granted carried item, and a later grant failure should not undo a prior successful item. This is per-item equipment atomicity, not all-or-nothing loadout generation. The command now emits its ordinary equip prose; this is a predictable newly reached success signal, not grounds to create a broad silent-command API.

Required stale-plan countercheck: use a one-slot Feet target. In the granted boots' BeforeEquip event, a one-shot test Part creates/carries and command-equips a different boots entity into Feet, then returns true. The outer AutoEquip must preserve the independently committed boots and leave its own grant carried. The current cached plan can displace the inner item. Pair with a callback that leaves the slot unchanged, which must succeed. If reproduced, rebuild/revalidate the no-displacement plan after BeforeEquip or reject any now-occupied claim before any UnequipCommand. A fresh post-hook plan also avoids equipping to a limb detached by the callback. Do not infer that this solves arbitrary payload effects or observer exceptions.

Stack/merge boundary: AddObject may merge a new entity completely into a carried resident. Its incoming reference is then unowned with zero quantity; AutoEquip's ownership validation should refuse it. Assert resident quantity/mass and no orphan equipped reference. Do not force-equip that dead incoming reference or silently choose an arbitrary resident. AutoEquip's documented stacked-item refusal keeps an authored multi-unit stack carried; explicitly note this difference from the old raw bypass if a synthetic authored StackCount>1 control is included. Authored nonpositive quantities and huge parser counts remain separate robustness debt, not a claim of this lifecycle repair.

## Actual-content fixture blueprint

Use a fresh EntityFactory for each fixture group, loaded from actual Resources/Content/Blueprints/Objects.json, then mutate only that fixture factory's baked blueprint dictionaries. `Blueprint` has `Parts`, `Stats`, `Props`, `Tags`, `IntProps`; `StatBlueprint` has Name/Value/Min/Max/Boost. Example:

```csharp
var factory = new EntityFactory();
factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
    "Resources/Content/Blueprints/Objects.json")));
var actorBlueprint = factory.Blueprints["Player"];
actorBlueprint.Stats["Speed"] = new StatBlueprint { Name="Speed", Value=100, Min=0, Max=100 };
actorBlueprint.Parts["Loadout"] = new Dictionary<string,string> {
    ["Equip"]="IronshodBoots", ["Carry"]="", ["Pick"]=""
};
LoadoutPart.Factory = factory;
var actor = factory.CreateEntity("Player"); // True constructor/ObjectCreated path, no replay.
```

Save and restore old LoadoutPart.Factory/Rng, rather than nulling them unconditionally. Save old `_depth` by reflection only if deliberately probing recursion; assert it returns to its prior value after the fixture, without normalizing away a failure. Restore any altered MessageLog, diagnostics, reputation, TurnManager.Active and enhancement registry state. A local fresh factory prevents changes to shared bootstrap blueprints.

For actual Duelist Cut payload, register a public test Part through `factory.RegisterPartType<T>()` and attach it to the fixture factory's Buckler blueprint. On that item's ObjectCreated it invokes the real `DuelistCutTinkerModification.Apply`; the actor's outer Loadout then receives the configured item. Similarly invoke actual GlowQuartzTinkerModification.Apply before the granted item is returned. Assert the setup succeeded. This proves actual mod producers plus loadout equip; simply changing blueprint EquipBonuses remains a valid payload control but must be labeled synthetic configuration.

For direct enhancement blueprint controls, set all actual payload fields explicitly (e.g. GlowQuartz Tier=2, RadiusBonus=2, AppliedBonus=false); loading a Tier field does not call TierConfigure. Register test observers before creating the actor so BeforeEquip/AfterEquip are present during the first loadout. Actor Anatomy belongs in Props; do not repeat the old ineffective Body parameter.

## Initial RED/control set (8)

1. Actual IronshodBoots Equip grants exactly one equipped unit and Speed.Penalty=5; assert full body/cache/Physics links.
2. Identical Carry-only loadout retains the exact boots carried with zero equipment penalty/hook calls.
3. Loadout boots then ordinary Unequip returns Speed.Penalty to baseline, never -5; re-equip adds 5 once.
4. Actual Glow-Quartz item producer then Loadout Equip sets AppliedBonus and adds +2 light radius once.
5. BeforeEquip veto sees the exact granted item and keeps it carried with zero bonuses/AfterEquip/enhancement effects.
6. Real two-hand weapon with an explicit test EquipBonuses payload occupies two aliases but adds the contribution and emits AfterEquip once.
7. No-Body actor with Equip keeps the exact item carried and creates no legacy slot.
8. Already occupied Feet keeps the existing exact equipped item and carries new boots without displacement or bonus change.

Run this RED before the minimum. Capture compilation failures separately from behavioral failures. Existing synthetic LoadoutPartTests and InventorySystem auto-equip neighbors are controls, not proof of the new effects.

## Dedicated adversarial matrix (28 cases)

These are proposed tests, not executed results. Use exact deltas/references and pair each refusal with a valid control from the same setup.

1. Pick `1;IronshodBoots` reaches the same lifecycle as Equip; zero/invalid Pick count grants nothing.
2. Actual Duelist Cut Buckler grant adds Agility:2; plain Buckler control does not.
3. Two independent bonus-bearing grants on different slots sum their contributions; removing one preserves the other.
4. Missing target stat is tolerated without manufacturing a Stat or subtracting an unrelated one on unequip.
5. Synthetic negative EquipBonuses apply and reverse symmetrically; zero-value payload does not change the total.
6. Glow-Quartz on an intrinsic light source restores its original radius on unequip, retaining the Part and configured RadiusBonus.
7. Lacquered API/blueprint payload applies item AV once, then ordinary unequip restores AV and AppliedBonus. Assert actual CombatSystem.GetAV where needed, not a fabricated AV Stat.
8. Engraved on a Player-tagged fixture applies and restores faction reputation; non-player control does not subtract reputation it never added. Snapshot/restore the reputation globals.
9. Multi-slot enhancement invokes once per item, not once per claimed body slot; exact alias counts are two with one FirstSlotForEquipped.
10. One unavailable hand refuses a two-hand auto-equip and preserves the equipped single-hand item; both-free control succeeds.
11. Incompatible Quadruped anatomy keeps Hand gear carried; an actual compatible slot control succeeds. Set entity Props correctly.
12. Missing Inventory or null Factory remains a graceful no-op, while otherwise identical valid actor receives gear.
13. Null/empty Equip and Carry do not dispatch BeforeEquip/AfterEquip or change equipment version.
14. BeforeEquip false retains the exact owned grant, normal stats and unrelated inventory; the same probe toggled true succeeds on a fresh actor.
15. BeforeEquip exception is converted by the command executor into failure; grant stays carried, no equipment contribution remains, and a later independent specification still runs. Do not assert unrelated callback payload rollback.
16. AfterEquip exception rolls back the current command's storage and generic stats, retaining its grant carried and prior successful grants. This covers the built-in command undo boundary only; enhancement-hook exception-after-mutation is separate A41 debt.
17. One-shot BeforeEquip independently command-equips another item into Feet: outer grant refuses, inner success remains. This is the source-confirmed stale-plan RED described above.
18. BeforeEquip leaves destination unchanged: outer auto-equip succeeds, guarding against an unconditional callback refusal.
19. BeforeEquip detaches the sole planned Feet node: outer boots grant must not appear equipped on the detached node; keep it carried. Use a one-slot anatomy requirement so a fresh plan cannot legitimately select a different free slot. Revalidate after the hook if the current prebuilt plan proves stale.
20. First small grant succeeds, second overweight grant refuses: exact first equipment, contribution and ownership remain. Follow with another affordable independent spec to verify per-entry continuation.
21. Unknown blueprint first or middle produces the factory's expected error but does not suppress later valid independent specs. Expect the actual logged error explicitly.
22. Same authored item repeated across count entries: fill legitimate free slots, then carry excess; conserve exact positive units and mass. Do not assume every repetition remains separately carried when stacking is allowed.
23. AddObject merges incoming grant into a preexisting compatible carried stack: resident gains the units, no zero-count incoming entity becomes equipped, and no lifecycle bonus fires.
24. Authored StackCount>1 grant remains carried under the normal AutoEquip contract; singleton control equips. Record the old raw-bypass behavioral difference, not full quantity-validation coverage.
25. Explicit Equip chance0 grants nothing and chance100 grants/equips; this protects source behavior despite the old “always” comment.
26. Nested/self-referential loadout stops at MaxDepth and leaves `_depth` restored; subsequent independent creation still works. Use small counts, not huge malformed count loops.
27. Save/load an actually loadout-equipped actor preserves exact body/cache/item-token aliases, generic stat values and enhancement AppliedBonus; normal unequip of the loaded item reverses once. No new grant on load.
28. GA03f composition: actual command-equipped loadout boots/Glow gear removed by injury or death refund their contributions exactly once; untouched carried/suppressed-drop control remains valid. This proves the repaired producer composes with forced cleanup rather than merely asserting cache placement.

Repeated ObjectCreated may be a documented compatibility control asserting a second grant (within available slots/capacity), but it is not an idempotence test. Do not pad the dedicated count by retesting parser cases already covered unless implementation touches their behavior.

## Qud analogue verified directly

- `/Users/steven/qud-decompiled-project/XRL.World/GameObjectFactory.cs:1584–1603` processes authored InventoryObject chance/count during creation. `ProcessAsInventory` (:1227–1261) applies NoEquip metadata and delivers through CommandTakeObject.
- `XRL.World.Parts/Brain.cs:2056–2059` performs initial reequip on entry when requested; `PerformEquip` (:3883–3885) delegates to PerformReequip with Initial=true.
- `XRL.World/GameObject.cs:16323` AutoEquip obtains/receives the item and sends CommandEquipObject for slot attempts (:16358, :16379, :16415). `XRL.World.Anatomy/BodyPart.cs:1658–1683` also routes its Equip API through CommandEquipObject.
- `XRL.World.Parts/Inventory.cs:1379–1482` handles PerformEquip, does actual slot placement, then emits EquippedEvent and EquipperEquippedEvent.

This supports routing authored starting gear through the normal lifecycle. CoO's Equip/Carry/Pick string grammar, strict no-displacement auto-equip, no-Body carry fallback and enhancement effects are CoO contracts. Qud AutoEquip can try replacements and splits stacks; do not claim exact parity or copy those policies into this bounded fix.

## Native/API honesty and final gates

No current authored world spawn uses Loadout. A native scenario must create an explicitly labeled fixture blueprint with Loadout using actual item blueprints and the real EntityFactory creation event; manual AddPart followed by replay on an already initialized actor should not be described as normal spawn coverage. Native inspection can verify initialized equipment/stats/hooks and then use normal player UI or a staged actor interaction to exercise unequip/injury/death/save, but adding an armed enemy to shipped content is a separate product decision.

The bounded feature is also meaningfully verifiable with actual-factory EditMode tests and an API self-auditing scenario. No allocation/speedup/75-second profile claim is needed for a spawn-only grant traversal; collect broader performance evidence only if the implementation adds a hot-path responsibility. Visual equipment appearance, loot economy balance and ordinary-world frequency remain unverified.

Before commit: initial RED -> minimum -> focused neighboring suites -> dedicated taxonomy RED/fixes -> independent review -> bounded native/API evidence with explicit fixture labels -> full suite. Update stale LOOT-OVERHAUL content/death-reliability wording and this wave's source corrections in root-owned Docs only when root implements. Do not edit protected concurrent work or add save fields.

## Root decisions before RED

Use the explicit Body gate plus ordinary AutoEquip; remove the redundant local
planner. No current authored producer will be invented. Refused equip retains the
granted carried item; ordinary AutoEquip stack/no-displacement contracts apply.
Keep no-Body carry, per-entry chance/count/replay behavior, and save fields. Test
the cached-plan callback occupancy/detachment seam before any command repair.

## Executed implementation log

Initial9:4PASS/5FAIL,0CS,11:03:57Z,.5428978s. Actual boots omitted5, actual
GlowQuartz stayed unapplied, BeforeEquip never fired, a two-hand configured bonus
stayed absent, and normal unequip produced Speed.Penalty=-5. Carry, occupied and
no-Body controls pass. Minimum replaces raw storage with Body-gated AutoEquip and
removes the duplicate planner; corrects obsolete all-creatures-empty/death-perfect
prose and the false Equip-always comment. Neighboring316/316GREEN,0CS,
11:05:17–18Z,.998989s.

Observability source correction before the dedicated gate: the reused AutoEquip
facade/executor has no generic success/refusal diagnostic. Add a producer-level
event/LoadoutEquipResult with exact actor/item/blueprint, equipped boolean, and
reason equipped/auto_equip_refused/missing_body. Test these three branches before
adding the receipt; do not claim broad instrumentation of unchanged parsing/grants.

Dedicated tests will exercise the cached-plan before-hook seam, exact grant
conservation/stacking, per-command exception rollback, subsequent independent
specs, serialized aliases/no-regrant, actual mod activation and forced-cleanup
composition. Test-only callback configuration will be restored per fixture.

Dedicated initial43:38PASS/5FAIL,0CS,11:10:34–36Z,1.2875198s. Three missing
receipt branches and detached cached slot reproduced. The occupied callback
fixture initially created identical IronshodBoots while the outer grant was
carried; AddObject merged that new item, so it did not establish independent slot
occupancy. Corrected to actual LeatherBoots and assert successful inner equip
outside the callback (the command executor catches callback assertion exceptions).
Corrected43 remains38PASS/5FAIL,0CS,11:12:04–06Z,1.1697948s; now both stale-plan
failures are meaningful. The independent boots have SpeedPenalty0; preserve that
instead of expecting the outer IronshodBoots5.

Further hypothesis46:39PASS/7FAIL,0CS,11:14:51–52Z,1.3151118s. AfterEquip can
force-remove its own item; the old command then reactivates GlowQuartz on the
carried item. The disabled callback preserves the applied positive control.
Add a fourth receipt outcome removed_during_equip, distinct from failed planning.
Do not return a late failure and blindly roll back: forced cleanup already
reversed generic bonuses. Preserve the completed independent removal and skip
late enhancement activation. Added other-free-hand and Carry-no-attempt controls;
final RED is executing before the three-file callback/receipt repair.

Final48 RED:40PASS/8FAIL,0CS,11:16:37–38Z,1.3822002s. The other-free-hand
case confirms a fresh plan must select another valid slot rather than displace
independent gear or refuse every changed plan. Carry-only receipt control passes.
Applied three-file repair: Loadout normal lifecycle plus final-outcome receipt;
AutoEquip discards pre-hook plan reuse; Equip replans after BeforeEquip and skips
late enhancement when a completed AfterEquip callback already removed the item.
The unused internal prebuilt-plan parameter plumbing is removed. Success here
means the equip operation completed before independent removal; the producer
receipt separately states current ownership. Arbitrary callback exception rollback
and later manual-displacement callback mutation remain outside this repair.

Focused406/406GREEN,0CS,11:18:11–14Z,2.6363779s. Native generator independently
prepared and fully read by root, then applied; all three native sources compiled
in this run. Review found one test-validity improvement: capture successful Feet
detachment and assert it outside BeforeEquip, because the executor converts a
callback assertion exception to refusal. Added success, null ParentPart and
attached-tree exclusion checks; the affected three rows are executing. No further
production must-fix was found. Native/API and full execution remain required.

Updated LOOT-OVERHAUL's current-source notice and historical SM2 heading so its
reported16humanoid entry cannot be mistaken for current authored reach. Clarified
that null Factory skips grants while null Rng falls back to a local Random.

The strengthened3fixture rows passed,0CS. Native24PASS/0unexpected; complete
teardown/root removal verified. Full9417GREEN,0CS,2026-09-06 11:28:42Z–2026-09-06 11:30:59Z,137.4359663s.
Final39dedicated+9initial tests, source review, finite scope and proposal
reconciliation are in GA03g-REPORT.md. GUID2453/0collisions.
Files: LoadoutPart/AutoEquipCommand/EquipCommand; two Loadout test classes;
LoadoutLifecycleBench, driver and editor launcher plus metadata; this plan,
LOOT-OVERHAUL current-source correction, audit/smoothing/daily docs and GA03g proof.
Next: A11 mortal dismember death/source/UI consistency.
