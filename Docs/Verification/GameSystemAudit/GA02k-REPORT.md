# GA02k — exact crafted recipients and local failure receipts

Status: COMPLETE. Final9066/9066GREEN (+86tests), native25/25PASS.
Baseline fb1f6a87,8980tests. CoO-original integrity repair; no Qud parity claim,
new content/art, resource-cost change or saved field. A47 local brew/build scope;
A41 broader command/discovery/effect atomicity remains separate.

## Implemented behavior

TryBrew and TryCraft return actual carried recipients after append/merge. A batch
list counts produced units and can repeat a recipient; a boundary stack at98 gets
one unit to99 and the next output becomes a new singleton. Empty/negative resident
stacks cannot absorb new crafted units. Existing callers that discard service
outputs keep their UI flow. The former orphan return was an API defect; ordinary
visible item loss was not established from those callers.

A local transfer receipt restores exact membership/order, stack counts and Physics
owners after failed crafting. Bit refunds restore only this operation's normalized
cost. Separated ingredient singletons are restored as the original entries instead
of re-merging into a sibling. Wrong same-blueprint output guessing is removed, along
with the obsolete brew consumption ledger. Mod-only ingredient rollback helpers
remain used by TryApplyModification and are retained.

Prepare/configure outputs before spending, then recheck the actor's exact inventory/
locker, source eligibility, known recipe and affordability. Freeze explicit reagent
references for both single/batch brewing and blueprint/count/ingredient/cost for
builds. A same-actor claim bounds recursive preparation calls across these two
services; unrelated actors and independently completed item drops remain available.
All local claims release on refusal, exception and commit. Already-adopted prepared
outputs refuse without being duplicated or reclaimed from another inventory.

Ordinary creation retains strict hard capacity, checking incoming mass and actual
post-merge mass. Existing weapon transformation policy is unchanged. Snapshots enroll
before mutation, including failed/throwing paths. Success commits before discovery/
prose. Tinker emits event/CraftRejected for refusal and event/CraftCompleted immediately
after commit, before prose; BrewResolved retains its established post-discovery/prose
position and targets the actual recipient. Brew prose names one produced unit even
when the recipient now holds two. Payment/handling diagnostics describe attempts and
can precede local rollback; they are not success receipts.

## Verification gates

Every run restarts/settles MCP first, checks compiler errors before XML, and retains
raw failed evidence. Two test fixture compile failures were corrected before counting
runtime RED: internal TryClaim needed reflection; a callback `_` parameter shadowed
out discards. Both complete compiler logs are retained.

| Archive | Total/pass/fail | UTC and duration |
|---|---|---|
|GA02k-red.xml.gz|26/9/17|2026-09-06 06:57:33Z – 2026-09-06 06:57:35Z; 1.4957107s|
|GA02k-guard-red.xml.gz|34/13/21|2026-09-06 07:05:37Z – 2026-09-06 07:05:39Z; 1.8395607s|
|GA02k-minimum-failure.xml.gz|125/124/1|2026-09-06 07:07:47Z – 2026-09-06 07:07:52Z; 4.5727458s|
|GA02k-minimum-green.xml.gz|125/125/0|2026-09-06 07:08:57Z – 2026-09-06 07:09:01Z; 4.8214444s|
|GA02k-adversarial-red.xml.gz|77/71/6|2026-09-06 07:11:58Z – 2026-09-06 07:12:02Z; 3.9043347s|
|GA02k-staging-eligibility-red.xml.gz|9/1/8|2026-09-06 07:13:17Z – 2026-09-06 07:13:18Z; 0.4609725s|
|GA02k-expanded-green.xml.gz|211/211/0|2026-09-06 07:15:53Z – 2026-09-06 07:15:59Z; 5.7783999s|

All tabulated runs have0compilererrors. Initial34cases include8bounded preparation
re-entry/release controls; dedicated45adversarial cases;7scenario staging cases:
86newtests in total. Five existing A03 parameterized merge assertions now assert the
new real-recipient return while retaining quantities/parts/healing payload evidence.
Direct AddObject tests retain their incoming-orphan semantics.

## Review, corrections and divergences

🟡 Initial source/RED reproduced orphan returns, wrong sibling rollback, singleton
re-merging and early payment on factory failure. Review caught preparation recursion
before production implementation; four same-actor cross-service tests failed RED,
while different-actor and retry controls passed.

🟡 Cold-eye found batch selection mutation between iterations, changed ingredient
blueprint before payment, and removed ReagentPart before payment. Each failed a
concrete test before its narrow fix, paired with preserved caller mutation/case-only
name/unrelated display change controls. Four tinker outcome-record tests also failed
before success/refusal diagnostics were added.

🔵 Old missing-blueprint test caught a less actionable error message in the first
minimum implementation. Restored the named blueprint before the125GREEN run.
Independent review requested a nonzero carry penalty setup to make handling symmetry
nonvacuous; final strengthened result follows below. Native source preflight corrected
an observation field name before launch; no UI mutation/reflection action was used.

Scope divergences: all outputs of supported NumberMade>1 builds now prepare before
payment, changing extension callback timing. Every currently authored V1 build makes1;
wrong-sibling multioutput rollback is a supported extension defect, not a claimed
normal V1 producer. Malformed counts and differently weighted compatible residents
are robustness controls. Factory callbacks are not globally rewound, and arbitrary
payload/effect/outer-command/discovery/Mishap HP rollback or extreme bit-refund integer
overflow is not claimed. Output service guards cover brew/build preparation, not all
crafting services. General A41 remains queued.

## Native verification and honesty bounds

Run88e108a8a2b54022becf67b9950baeee:25/25PASS,7.811696042s,exit0,
0compilererrors,0MissingReferenceException log lines. Separate6service checks prove
returned append/merged recipients with the actual factory. The19keyboard checks use
queued InputSystem keyboard states through InputHandler: two brews, two dagger builds,
a third capacity refusal, exact tonic stack Drop through its popup, successful build
retry into the original dagger, and return to Normal.

Arena uses Dagger1, marked GlimmerBrine2, B3/C3, MaxWeight16 and authored craft_dagger
BC/NumberMade1. Observed mass6→10→14, refusal unchanged, drop→12, retry→16; original
dagger1→2→3→4, tonic resident1→2 then ground, B/C3→2→1→0. Refusal retains exact
inventory refs/order/counts/owners/IDs and payment; crafting/refusal preserve time.

Can verify: queued keyboard route, actual menu selection/status, quantities, ownership,
recipient identity, local payment, carry mass and successful recovery. Direct-service
checks are labelled separately because the UI discards return values.
Cannot verify: rendered pixels, physical keyboard/mouse delivery, subjective feel,
all arbitrary callbacks, or a performance speedup. No new frame renderer work; these
changes run on crafting/menu actions, so no75second frame-performance claim applies.
Launcher restores save preferences and removes only its disposable arena slot.

2420unique Asset GUIDs,0collisions. No new sprite or blueprint JSON changes.

## Final verification

Full9066/9066GREEN,07:17:25–07:19:42UTC,136.4970163s,
0compilererrors/failed/skipped/inconclusive. After that run, four existing test cases
were strengthened to use handling3per unit plus unrelated7Speed penalty; runtime
unchanged. Final all86GA02kcases GREEN,07:20:27–31UTC,4.0280713s,0CS,
raw GA02k-strengthened-green.xml.gz. Refusal restores10; successful merge becomes13.

Independent final cold-eye source review found no further production must-fix
findings. Ten player-flow hypotheses are covered by the dedicated attempted-break
matrix above, including positive controls. No Qud-reference implementation is claimed.

Files: three production sources, one updated A03test file, three new test files,
native bench/player/launcher and metadata, raw reports and three living docs.
Whole owned paths have no overlap with the1027protected spell/art paths.
Next repair: recorded A31camera-shutdown stale Unity reference, then remaining
whole-game queue. A41 broader transactional payload/discovery behavior remains open.
