# GA02j — truthful crafting availability and stale-pick recovery

Status: COMPLETE. Baseline71dd3189,8886GREEN; final8980/8980GREEN. A47positive-selection slice;
exact crafting output receipts remain the next repair. CoO-original consistency,
no new art, recipe, resource cost, saved field or general transaction redesign.

## Implemented behavior

Every explicitly selected brew reagent must have a real carried unit before the mix
resolves or pays. An invalid member refuses the entire selection. Batch quantity is
the minimum actual stock, with missing Stacker meaning1 and nonpositive quantity
meaning0. Duplicate physical references cannot produce a valid preview/batch promise.
Actorless previews remain ownership-independent. Positive Mishap experimentation
still executes through the existing command/service; preview validity still means
an actual Brew, preserving the existing pack-versus-experimentation behavior.

Automatic tinkering ingredients skip empty matches and select the first positive
carried match. The UI uses the same selector. Modification/disassembly queries refuse
nonpositive targets without requiring ownership in the actorless query. Equipped
singleton modifications remain valid; disassembly still requires carried payment.
Every payment helper rechecks actual positive carriage. Existing rollback records
remain; this is not the exact output-receipt or general callback-atomicity repair.

Unmarked empty crafting entries are hidden. Already marked empty entries remain
visible, clearly labelled '(empty; remove pick)', and removable in panel/station
menus. They stay in the explicit selected mix until removed, preventing silent
recipe changes. Inventory popup and command availability agree; removing a mark is
allowed despite empty quantity. A cached attempt to add an empty pick revalidates
before exclusive-group changes, so another item's exact valid mark remains intact.
Existing C clear still removes all carried picks across crafting modes.

Empty resident records are malformed-state robustness, not an established ordinary
producer. Normal positive stacks, no-Stacker items, equipped modifications, same-name
siblings and ordinary selection controls are counterchecks. Unshipped comma-separated
ingredient syntax and exact output/rollback references remain recorded separately.

## Verification gates

Every Unity run restarts/settles MCP first and checks compiler errors before XML.

| Raw archive | Total/pass/fail | UTC / duration | Meaning |
|---|---|---|---|
|GA02j-red.xml.gz|46/21/25|06:25:08–06:25:10;2.4872045s|Reproduced payment, preview, target and selection failures; positive controls pass |
|GA02j-minimum.xml.gz|125/125/0|06:26:58–06:27:01;3.4949581s|Minimum repair with brew/tinker/mark neighbors |
|GA02j-adversarial.xml.gz|120/120/0|06:29:41–06:29:47;6.0586105s|29dedicated attempted-break cases plus neighbors |
|GA02j-staging-red.xml.gz|9/0/9|06:30:52–06:30:53;.3288175s|Scenario absent before authoring |
|GA02j-focused.xml.gz|84/84/0|06:32:56–06:33:00;4.1737926s|All initial new tests and staging |
|GA02j-label-red.xml.gz|3/1/2|06:34:47;.4756s|Zero/negative cleanup labels missing; positive control passes |

All listed0compilererrors. Dedicated suite now32cases after3label controls.
Initial regression46+dedicated32+staging9=87newtests. Counts are tests, not bug totals.

## Review and hypotheses

Independent review found the 🟡 same-name empty/singleton ambiguity: ordinary item
names suppress counts<=1, so a generic empty-preview reason did not identify which
pick to remove. Actual same-name row-text tests failed RED; panel/station cleanup
suffix added afterward. No other current-slice runtime blocker found.

Hypotheses cover actual BlastcapSpore Mishap/no-damage refusal, Dagger/LeatherArmor
equipped mods, no-Stacker payment, another actor's items, invalid batch order, cached
exclusive marks, repeated pure queries, popup/station cleanup, clear-all, refill
recovery and first-positive later ingredient. Station unit tests gather real rows
and execute the command; they do not claim InputHandler dispatch. Native evidence
below follows the pack recovery and tinkering route.

## Native plan and honesty bounds

Disposable arena uses existing content: GlimmerBrine2 and SparkRoot0 both marked,
empty PaleSalt before a distinct positive PaleSalt2, Dagger1 and adjacent actual
AlchemyStill. Synthetic empty records are intentional audit setup. Queued keyboard
controls open crafting, refuse the whole invalid mix, remove only the stale pick,
recheck the exact remaining preview, brew one, then open Mods and infuse the exact
Dagger using the positive later mineral. Reflection observes only.

Can verify: queued keyboard dispatch, exact row/selection/popup references, labels,
quantities, discoveries, enhancement tier, ingredient/bit payment, and turn/energy.
Cannot verify: rendered pixels, physical keyboard/mouse ingestion or subjective
smoothness. No performance-speedup claim. Changes affect on-demand queries/rebuilds
and crafting execution; no new per-frame renderer work. Launcher restores the old
save preference, unregisters synthetic runtime and deletes only its disposable slot.
Known A31shutdown camera exceptions, if present, are recorded separately.

## Native failure and expanded scope

Expanded124GREEN,06:35:52–06:35:56UTC,4.3577687s,0CS.
First native20ed69b731f4418494d0c33a66b824e5: **11PASS/1FAIL**,4.300388709s,
exit1,0CS. Pack recovery/brew checks passed; the exact infusion target check failed.
Actual UI reported no compatible target. Raw failed JSON/log retained; this run had
0MissingReferenceExceptions. It is not counted as a successful native gate.

Native exposed a separate ordinary first-use bug: the mineral modification shim
calls EnhancementFactory.TryGet before lazy initialization. TryGet/Create only read
the registry. ItemEnhancing.Apply initializes it, but lies behind the failing
compatibility gate. Factory documentation says the shims initialize first; actual
code does not. Older tests/bench setup explicitly initialized it and masked the gap.
The new bench intentionally does not initialize that registry.

Scope extends to the actual shim's first-use initialization, with7newtests:
query/execution for all3minerals plus suppressed-test-registry refusal. Root and
independent reviewer verified the source chain. Fixing scenario setup would hide
this ordinary gameplay bug, so the scenario remains unchanged.

First-use RED7:6failed exact missing-registration reasons,1suppression control
passed,06:40:06–06:40:07UTC,.6242687s,0CS. Added EnsureInitialized in
MineralInfusionTinkerModification.CanApply immediately before TryGet. It honors
ResetForTests suppression and repairs all three recipes through their shared shim.
This is A50, discovered by the fresh native run. Total new94=46regression+
39dedicated adversarial+9staging. Previously stated87was before this scope extension.

First-use expanded151/151GREEN,06:41:16–06:41:21UTC,5.5795577s,0CS.
Unchanged native scenario rerun dca91794c64a4eea8607f10182b62182:
**15/15PASS**,4.673590416s,exit0,0CS. No registry initialization was added to the
scenario. First-use infusion now finds the actual Dagger, applies PaleSalt tier2,
consumes one from the later positive salt and leaves the empty entry intact.

Final native log contains2known A31camera exceptions at shutdown after the successful
report. Failed first-native log had0such exceptions. Both raws remain.2414unique
GUIDs/0collisions. Full suite pending. Independent source confirmation agrees this
is ordinary first-use initialization, and the suppressed-registry control remains.

## Close-out

Full **8980/8980GREEN**,06:43:30–06:45:42UTC,131.7028706s,
0compilererrors/failed/skipped/inconclusive.94newcases (46regression,
39dedicated adversarial,9staging), native15PASS,2414uniqueGUIDs/0collisions.

Self-review: 🟡 empty-payment/selection issues reproduced and fixed; same-name
cleanup ambiguity caught by review, RED-tested and fixed; A50first-use mineral
initialization caught by native failure, independently source-confirmed, RED-tested
for all3recipes and fixed in production. 🔵 automatic ingredient selection now
shares its pure helper with UI, removing duplicated lookup/payment logic.
⚪ exact receipt/command rollback and unshipped ingredient alternatives remain next;
existing suppressed registries retain their contract. 🧪 no pixels/input-device/
subjective-feel claim. Known A31shutdown exceptions reported separately.

Files: seven production sources (brew/tinker/shim, mark command/rows and two UI
files), four test files, native bench/player/launcher, raw evidence and living docs.
No blueprint JSON/art/save schema changes. Owned whole paths are checked against
all1027protected paths; no overlap or protected spell/art staging.
