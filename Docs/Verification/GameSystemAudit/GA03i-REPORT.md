# GA03i — personal equipment and active natural weapons

Status: COMPLETE,2026-09-06. Baseline ab7809b1,9484GREEN →9599GREEN (+115).
115 new cases:34 initial equipment,40 dedicated equipment,12 natural activation,
3 saved-alias and26 dedicated natural adversarial cases. Test count is not a bug count.

## Player-visible result

Twenty-four explicitly selected creature and settlement roles receive personal
weapons, armor or work clothing through the normal equipment lifecycle. Hunters
carry spears, Warlords retain signature cleavers under their armor, and town
workers receive role-appropriate footwear/headwear/gloves. Exactly24 effective
blueprints receive kits; no base Creature/Villager or unrelated descendant is
changed. Optional thresholds and the Scavenger's two weapon choices remain
explicit. ENTITY-EQUIPMENT-PLAN retains the complete literal kit/attack ledger.

Automatic creation grants no longer flood the message log with equip-success
prose. Ordinary player equip and pickup remain audible, including independent
commands nested inside a quiet grant. Hooks, bonuses, refusal diagnostics and
retained refused grants use the existing path. This is per-command success
suppression, not a global mute or suppression of arbitrary callback prose.

The real content tests exposed a separate dead mechanic: humanoid natural
weapon recipes were assigned but their entities were never created by normal
factory/turn paths. Warlord and Sentry first attacks fell back to generic1d2.
Factory anatomy completion now creates defaults after the final actor override,
before ObjectCreated. All40 effective named declarations and default humanoid
fists are covered; handless opted-in and no-Body legacy paths remain intact.

Loading repairs missing recognized natural defaults on attached and detached
branches without replacing existing objects or flags. Shared saved aliases,
custom blank declarations and unknown missing recipes retain their saved state.
No kit backfill, save-version bump or new saved field is introduced. Fresh
anonymous natural objects use save reference tokens; ID-string stability is not
claimed. Natural objects remain outside personal inventory/death-drop ownership.

With natural defaults active, an adversarial test exposed a second-hand bug:
a hand already supporting a two-handed weapon added its fist. The gather guard
now excludes occupied non-first equipment slots. Free offhands still attack;
existing primary non-melee shield fallback remains deliberate CoO behavior.

## Scope and reference classification

This is CoO content/lifecycle integration, not exact Qud parity. Direct local
Qud reads cover GameObject.AutoEquip's Silent propagation, Inventory's normal
equipment hooks, Anatomy.ApplyTo/Body default maintenance, and Combat/BodyPart
weapon selection. CoO suppresses only success prose and retains strict ordinary
no-displacement rules; it does not port Qud's complete silence/replacement policy.
Its factory-end default materialization is finite, not per-turn maintenance.
Qud's weapon-scan fallback differs from CoO's retained shield/free-fist policy.

The original24-kit/quiet-message plan expanded after real REDs proved the
natural producer gap. This changes actual damage, classes, offhand opportunities
and existing riders; it is not cosmetic or balance-neutral. Existing gas ID
mismatches (A44), delayed source credit and dynamic recipe/anatomy mutation remain
separate recorded work. No universal natural-rider correctness or economy/
encounter-frequency/balance calibration follows from this wave.

JSON contains exactly408 inserted lines for24 Loadout Parts. Removing only
those appended Parts in memory reconstructs the entire original JSON semantics.
All three fields are explicit, including empty child overrides. The nine actual
Traders precede Loadout; kits are disjoint from recursive stock leaves. Maximum
stock and actual village loaners are tested at unchanged capacity150. Gear may
change actual loot class and seeded RNG consumption; neither is claimed identical.

## TDD, adversarial and review gates

All XML runs below had zero compiler errors. Historical failed checks are kept.

| Gate | Total | Pass | Fail | Start UTC | Seconds |
|---|---:|---:|---:|---|---:|
| red | 34 | 7 | 27 | 2026-09-06 14:14:46Z | 1.1302191 |
| initial-green | 34 | 34 | 0 | 2026-09-06 14:26:33Z | 1.094426 |
| adversarial-first | 74 | 66 | 8 | 2026-09-06 14:30:54Z | 2.1217038 |
| natural-public-red | 8 | 4 | 4 | 2026-09-06 14:38:35Z | 0.5377693 |
| natural-detached-red | 10 | 5 | 5 | 2026-09-06 14:39:36Z | 0.4908045 |
| natural-minimum | 84 | 84 | 0 | 2026-09-06 14:40:38Z | 2.2392075 |
| first-full | 9568 | 9567 | 1 | 2026-09-06 14:41:40Z | 149.1372592 |
| natural-alias-red | 4 | 2 | 2 | 2026-09-06 14:45:29Z | 0.3147495 |
| natural-unknown-red | 5 | 3 | 2 | 2026-09-06 14:48:29Z | 0.4419706 |
| natural-alias-minimum | 95 | 95 | 0 | 2026-09-06 14:50:40Z | 2.3306523 |
| natural-adversarial-first | 26 | 25 | 1 | 2026-09-06 14:53:14Z | 0.9582865 |
| final-focused | 121 | 121 | 0 | 2026-09-06 15:07:33Z | 3.1091598 |
| final-full | 9599 | 9599 | 0 | 2026-09-06 15:09:22Z | 149.7737048 |

The initial34 cases yielded24 missing-content REDs and three quiet/nested prose
REDs, with seven passing controls. The40-case equipment gate exposed eight
manifestations of missing defaults; the later26-case natural gate had25 passing
controls and one confirmed occupied-secondary-hand RED. Public first-melee
and old-save tests establish the producer failure without manual regeneration.
The3-case alias gate plus unchanged existing body-field pin caught overly broad
load repair; unknown-recipe controls narrowed repair without changing the public
factory fallback. Dedicated gates cover optional thresholds, inheritance,
maximal stock, actual producers, armor/weapon behavior, save/death/pickup,
creation ordering, callback nesting, exact payloads, alias identity and injury.

Two authoring errors are retained separately, not claimed as gameplay bugs:
GA03i-incomplete-edit-compile.log.gz records CS1739 from a guarded script's partial
edit being compiled before completion. GA03i-natural-rng-fixture-stall.log.gz
records a test RNG that returned exploding penetration10 indefinitely; the
owned process was stopped with no fresh XML trusted. Its corrected bounded RNG
uses legal non-exploding rolls. First full9568 had one real saved-flag failure;
existing expectations were preserved and the implementation narrowed.

Cold-eye review runs both taxonomy and Qud-reference angles. It found and fixed
saved custom flags, detached shared-natural duplication, unknown-recipe repair,
and the occupied supporting hand. Independent final reviews and raw runtime
artifacts accompany this report. Passing hypotheses are pinned controls, not
invented bug fixes. Final source and profile reviews found0additional must-fix findings. The121focused gate (115new plus6existing body pins) and final9599full passed; the final functional native rerun passed23. Two later assertions only strengthen hand-count nonvacuity; final full includes them.

## Native functional evidence

Initial run16a132ba899c4d77b953f458fb14774e:23PASS/0FAIL/0unexpected,5.804119s,
shutdown5.8239763s; private root held, saving unregistered and root removed.
Final post-guard/post-profile compatibility rund09ad6e27f4047efbf55756584560551:23PASS/0FAIL/0unexpected,5.6595585999999996s; shutdown5.6827005999999995s. Private root held, saving unregistered and owned root removed; raw JSON/log retained.

The23 groups use actual fresh24 content, village Elder/Merchant/Quartermaster/
Scribe/Warden, cached Wellmeet Host/SaltMaster, Sumphold PeatCutters and Drowned
Ledger Scribe/Sorter. They check personal/shelf separation, exact ownership and
capacity, quiet success plus diagnostics, real natural landed damage/class,
ordinary re-equip/veto/nonmortal cut, F5/F6 graph restoration, credited/repeated
death and native movement/G/displayed-row pickup/auto-equip. Real Warlord XP
level-up UI is drained normally; rewards are not removed for convenience.

Can verify: these script-observable actual-content/save/death/equipment paths
and real native input bindings used by the scenario. Cannot verify: every
random world occurrence, autonomous NPC combat, physical keyboard combat, all
riders, sprite quality or subjective feel. AI removal, fixed arena, controlled
HP/AV/agility and finite RNG are explicit fixtures. No positive fixture forces
RegenerateDefaultEquipment/UpdateBodyParts to manufacture the tested connection.

## 75-second bounded combat profiles

Both sources already include natural activation and all equipment/save fixes.
True BEFORE ran before the occupied-secondary-hand guard; AFTER followed it.
Eleven-file hashes and the exact owned patch prove only that guard differed
between captures. Later Loadout docstring-only corrections are distinguished
from capture source; no behavior changed after timing.

Before aa9ab41e94194f5eab0c61ce5a973c0e:75.1736779s,72287frames,492strikes.
After ea9e69e041ea4069b14e0452a56a2648:75.1522081s,71654frames,491strikes.
Each has25s idle/one-hand/two-hand phases, at least200 accepted attacks per active
phase, no overflow/failed call/unexpected error, and verified private teardown.
One-hand uses249before/242after calls, two miss swings and one offhand roll per
call in both. Two-hand uses243before/249after calls: before has two swings/one
offhand roll; after has one swing/zero offhand rolls. Health, coordinates,
registration, equipment aliases, energy and turn clock remain unchanged.

Full public melee timings in milliseconds:

| Variant | Phase | Mean | p99 | Maximum |
|---|---|---:|---:|---:|
| before | one_hand | 0.319485 | 0.465800 | 1.732400 |
| before | two_hands | 0.341687 | 0.588500 | 4.006100 |
| after | one_hand | 0.340583 | 0.612300 | 3.378200 |
| after | two_hands | 0.242509 | 0.380800 | 2.760300 |

These are descriptive whole-method observations including RNG/events/messages/
diagnostics/FX. Work intentionally differs when the forbidden swing disappears.
Single captures do not prove isolated guard cost, equivalence, no regression,
speedup, zero allocation, real-build FPS or user-perceived smoothness. Sparse
frame-marker percentiles of0 do not mean attacks are free. The one-hand control mean/p99 rose in this pair. Wall-frame maxima include589.401ms before/two-hand and648.300ms after/one-hand. Whole-editor GC and
wall-frame outliers remain visible in retained raw CSV.gz/JSON; wall frame is
not CPU, and recorders can shift a completed frame at phase edges. Both profile logs include the same nonfatal Unity Licensing startup message;0CS/0unexpected runtime errors does not mean no error-labelled text. No landed-hit,
rider/death, scheduler/input-combat or factory-creation performance claim.

## Attribution and remaining work

Nine production files: Objects.json, EntityFactory, Body, NaturalWeaponFactory,
LoadoutPart, InventorySystem, AutoEquipCommand, EquipCommand and the narrow
CombatSystem supporting-hand guard. Five new test classes plus one historical
fixture comment; three new native scenario/driver/batch classes and their
metadata; same-commit plans, audit/flow/day ledgers and verification evidence.
CombatSystem's unrelated existing spell hunks stay outside the index. Existing
sprites cover the kits; this wave adds no raster or renderer assets.

Next is the user's distinct ground-equipment sprite request, then the recorded
whole-game queue beginning with A12 turn scheduling after lethal callbacks.
A generated dagger concept failed16×16/binary-alpha checks and was rejected
as a production asset. No completed sprite improvement is claimed here.

Final asset metadata audit:2466uniqueGUIDs,0collisions,0missingGUIDs. This wave adds8 C# metadata entries, each copied from a shipped template with only its GUID changed. Index attribution proves only the new Combat guard is adopted; protected source/art remains outside this wave.
