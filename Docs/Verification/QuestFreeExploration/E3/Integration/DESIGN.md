# E3 F5/F7 source audit and bounded proposal

Status: design only, 2026-09-27. No E3 production or tests written or executed. Existing E2 flow pack remains private during the root native run. Root owns family enum/version, population/receipt/composer and JSON; this agent proposes only placement/assist seams and paired tests. These are original CoO situations, not imported Qud content.

## Corrections from actual current sources

| Plan premise | Actual source | Consequence for the first slice |
|---|---|---|
| Existing snake plus forage | `PopulationTable.cs:360–384` has exactly one weighted hostile group: Viper **or** MarlbackScrabbler, count1–2; independent BerryBush1–3, Beehive0–2, HollowStump0–1. | F5 uses the actual Viper group and one actually generated BerryBush/Beehive. No group reroll, added snake or food. HollowStump is gold salvage, not food. |
| Forage can be provenance-authorized now | `Builders/PopulationBuilder.cs:20–23,69–74,133–151` exposes group, loose and ambient receipts only. `SpreadWildernessSituationPlan.cs:57–65` explicitly enumerates those receipt identities. | Root must add a separate unambiguous ordinary forage receipt and extend SourceCurrent; a nearby matching bush is not source authority. Preserve exact rolled list/order/RNG and expected count, including refused placements. |
| Ordinary Marlbacks already cooperate | `Objects.json:612–730` has Loadout/Armor/Corpse/Examinable, not CombatTactics; Creature at426–609 also has none. | F7 needs explicit, scoped assist opt-in on the two existing actors. Geometry alone cannot establish cooperation. Do not imply current ordinary Scrabblers already alert allies. |
| Reach weapon/skill variant is free reuse | Scrabbler Loadout at663–679 picks one Dagger/Hatchet/Cudgel, plus independent LeatherCap35%/LeatherGloves20%. There is no sword/spear/Lunge skill. | Keep those exact generated kits. First F7 variants are mutual sight vs broken sight. Reach-weapon/skill variant stays a later explicit content/budget change; do not steal the unique pair's ShortSword/cap. |
| Controlled receiver refuses assist | `CombatTacticsPart.AlertAllies:190–226` checks opt-in/passive/alive/faction/party/currentTarget/range/two LOS rays, but not NoFight. `NoFightGoal.cs:32–69` prevents actual combat while topmost. | Current Calm stops actions but can still receive a personal-hostility alert. Plan's stronger admission promise needs a paired current-API RED and a narrow deliberate guard, not a claim existing code already does it. |
| Snake HP8 means low danger | Viper at14316–14415: HP8, Speed130, inherited SightRadius10. `NaturalWeaponFactory.cs:53–54`: 1d3, penetration1, Poisoned75%,1d6,8 turns. | Opening/bypass must respect actual sight; keep poison roll, speed and stats unchanged. No forced poison result in native acceptance. |
| Harvest remains finite | BerryBush at25213–25320 yields WildBerries1–3; Beehive at25323–25382 yields Honeycomb1–2. `HarvestablePart.cs:27–37,40–77,98–109,141–176` validates reachable source, rolls once, removes exact source and packs/overflows transactionally. | Reuse current Harvest action; no new food/effect/harvest rules. No tool requirement. Spent source disappears; save checks source absence and actual product count/identity, not a spent world prop. |
| Snake death implies guaranteed venom | Viper inherits CreatureCorpse100%; Viper Corpse override14323–14344 yields VenomGland1 at75%. | Actual harvest may give zero. No chance alteration; killing/venom is optional and separate from food. |

Paths above are under `Assets/Scripts/Data/Tables`, `Assets/Scripts/Gameplay`, or `Assets/Resources/Content/Blueprints` as named. Exact current source hashes accompany this note.

## Proposed first implementation boundary

F5 uses one or two exact existing awake Vipers plus one exact unspent BerryBush or Beehive. The helper leaves the food owner anchored and moves only the receipt-authorized snake owners to a useful nearby hedge/cover pocket. It does not attach ambush, change sight/stats/faction, equip anything, consume food or create props. Sleeper variants remain unimplemented until separately authored; unique SpreadLatchcoil is excluded.

F7 requires exactly two actually rolled ordinary MarlbackScrabbler owners. A one-owner roll refuses this family; it never expands to two or swaps a Viper roll. It places the same pair near existing Hedge/Tree/Signpost features, without treating their originally random cold positions as a live travel leash, with distinct cells3–6 apart and a real alternative route. It adds only owned `CombatTacticsPart { SkillClasses="", AbilityChance=0, AssistAllies=true, AssistRadius=6 }` after all placement preflights. No ObjectCreated replay, skills/abilities, loadout events, extra RNG, HP/stat edits or faction changes. Existing tactics/role/party/busy owners refuse rather than being rewritten. Actual randomized gear can coincide; do not promise visually distinct weapons in every roll.

To satisfy the explicit controlled-assistance promise, propose the smallest additional `CombatTacticsPart.AlertAllies` receiver guard for an actual currently active top NoFight goal, paired with an expired/removed goal positive and unchanged ordinary assistance. This is an explicit expansion beyond placement-only scope requiring root coordination. No generic priority rewrite. An alternative is to preserve existing admission and weaken the design claim to action suppression; this note recommends the narrow tested guard because the plan explicitly says refusal.

## Proposed public API (for root agreement before code)

Extend the existing helper as a partial class so its private bounded geometry/owned-graph snapshot can be reused without a duplicate snapshot implementation:

```csharp
public static bool TrySnakeForage(Zone zone, IReadOnlyList<Entity> snakes,
    Entity forage, Func<bool> authority, out Func<bool> finalState);
public static bool TryWorkGang(Zone zone, IReadOnlyList<Entity> pair,
    bool breakAssistanceSight, Func<bool> authority, out Func<bool> finalState);
```

`finalState` is null on refusal and a transient, read-only final-callback validator on success. No callback is stored in any saved part. Root validates/consumes the exact group/forage receipts before entry, then supplies current plan/token/sourcebuilder-reference authority; it separately pins all unselected receipt owners including their original positions. The delegate pins all selected owners' graph/positions/new exact opt-in part and baseline critical geometry. Root passes it through strict four-argument plan commitment and final zone acceptance.

Helpers must preflight bounded candidates (same32 source/256 candidate caps as current helper), capture original whole actor/gear/body/goal graph and exact source fields, recheck authority/current owners before and after each move/part callback, and roll back only still-owned changes. No restoration of callback-owned replacements or independent mutations. No factories or caller RNG in either helper. Refusal does not mint compensation or try another family.

F5 geometry: interior pocket near actual existing cover; at least one physically reachable harvest-adjacent cell, and an alternative opposite-border route that avoids each snake's actual visible acquisition radius. Traversable border arrivals must have at least four cells of standoff; distant visible pressure is allowed while a separate actual sight-free crossing remains mandatory. This supersedes the original private all-border-unseen rule after real cold-corpus RED and root review. The risky optional harvest approach has actual snake sight; safe bypass excludes the source route rather than pretending all harvesting is safe. Preserve existing reserved/stair/border component connections. Reject impossible pockets instead of reducing snake SightRadius. Both existing snakes, if rolled, stay in the source packet and count toward danger.

F7 geometry: mutual-sight variant has reciprocal current LOS within assist radius and a reachable threat/contact witness cell visible to both. Broken-sight variant has existing cover blocking at least the receiver-to-caller ray and a physically reachable approach visible to one actor but hidden from the other; no artificial walls. Both variants retain a route avoiding both acquisition regions and no mandatory corridor pressure. These are cold placement facts, not a guarantee wandering actors remain stationed forever; ordinary Brain movement is unchanged. If stable work posts become a required behavior, that must be a separately reviewed bounded role rather than silently toggling Brain flags.

## RED-first sequence and concrete counterchecks

1. Current-source premise tests using actual factory content and existing APIs: exact ordinary kits/no tactics; actual finite food; active NoFight assistance admission versus ordinary and expired/removed goal. Preserve source hash/RED before any guard implementation.
2. Compile-compatible helper tests first (reflection until API exists): meaningful open/covered geometry positives; no-forage, wrong roll, one-Scrabbler roll, foreign/duplicate/source-ref replacement, spent food, changed quantity/gear/stats/goal, party/busy/role owner, insufficient bypass/mandatory arrival pressure controls. Actual opposing sight rays must be asserted before outcome assertions.
3. Minimal private implementation. Source/actor conservation and untouched caller RNG measured before/after, two generated actors remain exactly two, source stays original cell, no factory/loot calls. Callback changes after first move, after opt-in append and at final-validator time must refuse without removing foreign owners; off-route harmless change stays positive.
4. Real AI controls on placed pair: actual proactive acquisition triggers existing nonrelaying assistance; cover/foreign faction/opt-out/passive/dead/engaged/party/active Calm refuse as applicable; no double action and no skill/cooldown grant. Save/load preserves only existing CombatTactics public fields and actual current goals/gear. No claim of Lunge/cooldown variety from this assist-only slice.
5. Food transaction/save control: same source actual Harvest produces authored count, retry fails, overflow preserves total and source absence survives full graph roundtrip. Pair retained references/gear survive save; no helper replay on restore. Reuse existing Harvest tests where already authoritative rather than duplicating all engine branches.

Root's short native gates after source integration: two naturally generated F5/F7 pockets, optional bypass and actual reachable source; one real food harvest with real quantity/depletion/save; visible pair assistance and occluded/control counter via native movement/Calm and exact event witnesses. Body/gear/source images must be viewed. A native test may record a genuine refusal or failed survival; it cannot grant food, suppress foes, force poison/hits or reroll the source. Whole cold census reports realization rate, original rolls/stock/value and time deltas; no claim every selected site succeeds.

## Exact prospective files and handoff

Agent scope: one-line partial declaration in `Gameplay/AI/SpreadExplorationActorPlacement.cs`; new `SpreadExplorationActorPlacement.Encounters.cs`+meta; focused new placement/assist fixtures+metas. `CombatTacticsPart.cs` only if root approves the controlled-admission correction after its real RED. No new role save schema, JSON, models, skills or broad AI changes.

Root dependencies: forage receipt on PopulationBuilder and SourceCurrent, two family registrations/version/selected entry variants, composer invocation+final validator, cold census/manifest/persistence and native acceptance wiring. These must remain root-owned. No production is authorized by this note itself; send findings first and agree APIs/scope before tests/code.

## Executed source correction before implementation

Initial30 current-API/reflection cases:6PASS/24RED. Twenty-two were missing helper; active NoFight admission reproduced the authorized defect. The remaining RED exposed a fixture/design premise: actual Hedge has only Physics.Solid, while AIHelpers.HasLineOfSight checks Cell.IsSolid (tag-based). Tree has an actual Solid tag and is existing AI sight cover. First F7 occlusion must use actual Tree/other native sight-blocking geometry, not assume a visible hedge blocks AI. The test fixture was corrected to actual Tree; global LOS/Hedge gameplay is unchanged. Original failing source/fixture/XML retained. Root informed.
