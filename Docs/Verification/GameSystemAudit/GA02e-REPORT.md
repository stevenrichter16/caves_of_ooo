# GA02e — preserve crafted item payloads when stacking

Status: COMPLETE. Full8122/8122 GREEN, zero C# errors. Baseline1ad88572,8040/8040 GREEN.

## Outcome and scope

A03 changes Stacker.CanStackWith to compare mutable crafted configuration after
its existing blueprint/name/charged-book gates. Brewed potions with charge2/3,
healing1d4/2d4, once/twice PaleSalt infusion and different temper payloads no longer
replace each other during insertion. Equivalent actual recipes and upgrades still
merge. Existing content/recipes/art are reused; there is no public Part field,
serialized fingerprint, cached identity or save-version change.

StackPayloadIdentity compares ordered tonic/BrewItem/StatusTonic/CureTonic
responders and a separate ordered enhancement subsequence. Melee configuration
and temper state are independent of that dispatch order. All eight melee fields
and every occurrence of all six known enhancements are included. Comparisons
use exact known runtime types; unknown subclasses in these families stay separate.
Raw payload strings are conservative configuration identity, preserving effect
order. Counts/IDs/ownership/private parser caches/unrelated parts are excluded.

ClearTemper leaves an empty marker; a sole default-empty exact WeaponTemper is
normalized to absence. Multiple markers retain their full order so first-marker
capacity cannot be lost. A harmless zero-radius LightSource left by unequip does
not prevent this bounded merge. This is not universal entity/stat/thermal/material
or arbitrary custom Part equivalence. A45 Sharp/reforge's retained veto tag and
A41 arbitrary effect rollback remain separate accepted repairs.

StackPayloadMismatch explains same-name crafted-payload refusal on the event
channel. Earlier blueprint/display/charge gates retain behavior; disabled
channel checks avoid payload allocation. MergeFrom remains the quantity primitive
called after CanStackWith by both production insertion consumers.

## Reference sweep and methodology

Root read actual producers, blueprints, consumer fields, paid mod dispatch,
clone/public-field save and the Qud reference. Qud GameObject.SameAs10696–10785
compares broad part/stat/effect identity; Stacker.SameAs111–114 ignores quantity;
ModSerrated21–27 and IModification71–77 compare chance/tier. CoO's selective
payload groups preserve its duplicate-part model and charged-book exclusion;
no full Qud receive/equivalence parity is claimed.

Preimplementation corrections: names omit potency, healing-only output has no
BrewItem, mineral payment is one actual mineral with zero bit cost, modification
does not restack, and any effect-bearing brew can temper (old coating-only prose
is stale). Independent review identified harmless crafting-history order and
empty-marker differences before implementation. It then found one introduced
normalization bug: filtering every empty marker made [empty,active] equivalent
to [active]. Two explicit REDs preceded limiting the exception to sole markers.

Native preparation initially suggested an unavailable AddObject noStack overload;
source verification rejected it before authoring. Three real ground daggers are
picked sequentially after prior blades have been modified. An actual two-handed
Warhammer fills both hands to preserve carried pickup and test ordinary transfers.
All effect/upgrade outputs are produced through native UI, not preconfigured.

## Evidence so far

Every XML result follows a zero-compiler-error check.

| Gate | Raw archive | Result (September6UTC, September5 local) |
|---|---|---|
| Actual-content RED | GA02e-red.xml.gz |21:10fail/11controls,00:12:25–26 |
| Minimum+neighbors | GA02e-green.xml.gz |537/537,00:14:59–00:15:03 |
| Dedicated adversarial | GA02e-adversarial.xml.gz |68/68,00:18:26–29 |
| Cold-eye+staging RED | GA02e-cold-eye-red.xml.gz |78:2normalization+7missing-bench fail/69pass,00:20:48–52 |

New tests:21 regression+54 dedicated adversarial+7 native staging=82.
The field matrix independently mutates/restores42 configured fields across12
families; these assertions are not42 separate test cases or42 found bugs. Actual
recipes, material payment, carried consumption and mineral damage are checked.
Extension fixtures cover duplicate parts, unknown subclasses and inconsistent
states; these are not claims of ordinary UI exploits. Actual forge/temper/mod/
reforge histories provide counterchecks for benign identity differences.

## Self-review and remaining verification

- 🟡 Fixed: same-name loss of brewed strength, healing, mineral occurrences and temper effects.
- 🟡 Fixed: introduced empty-marker filtering hid first-marker temper capacity.
- 🔵 Prevented: false incompatibility from non-dispatch crafting-history order.
- ⚪ Kept: charged books never stack, exact configuration strings and bounded payload scope.
- 🧪 Native visual/feel and broader crafting unit-target/rollback boundaries remain explicit below.

No ordinary per-frame/per-turn production hook was added. Comparisons occur in
inventory/container insertion; no performance speedup or75-second runtime capture
is claimed. Native scripts are temporary audit drivers, not gameplay loops.


## Native evidence and final review

GA02e-native.json: run9b7331519681442e82b2b03230b569dd,89/89 PASS,0 failures,
32.876371625seconds, batch exit0. Matching GA02e-native-unity.log.gz has zero
compiler errors. The input driver used six real Brew commands: three status
mixtures produced weak1/strong2; three healing mixtures produced1d4×1/2d4×2,
including an equivalent recipe. Every reagent payment had a fresh BrewResolved.

Three plain ground daggers were picked sequentially and infused1/2/1 times with
four actual PaleSalt payments. Warhammer occupied both body hands throughout
setup, so auto-equip could not remove the comparison items from carried storage.
Drop/Pickup preserved the twice-infused item separately and merged the equivalent
once-infused item. The carried strong healing potion spent one unit and healed
within2–8HP; the carried strong status potion spent one and applied charge3.
Both required fresh TonicApplied with consumed=true and matching item identity.

**Can verify:** real keyboard/modal routing, actual ingredients/recipe/payment,
carried payload identity/counts, different-versus-equal transfer behavior, healing
bounds and charge, fresh diagnostic records and isolated audit completion.
**Cannot verify:** rendered pixels/feel, native enhancement combat damage,
save/clone/split behavior (those have EditMode evidence), or exact RNG heal amount.
Manual-stop cleanup is source-reviewed. The launcher uses only its unique save
marker and restores the prior slot preference; driver restores diagnostic/input/
background settings. The separately queued A06 new-game save repair must preserve
these audit launchers' isolation. Existing A31 camera teardown errors are retained
in the native log and are not claimed fixed here.

Final independent production/native reviews found no remaining must-fix issue.
The rollback test was strengthened to prove the inner acquisition succeeded and
observe its merged/separate intermediate state before exact injected refusal;
then assert exact restored quantities and enhancement occurrences. Four optional
controls pin unknown empty subclasses and diagnostic silence at earlier gates.
GA02e-counterchecks.xml.gz:82/82 GREEN00:28:01–05UTC,zero C# errors. Two final
restored-enhancement-count assertions were added after that focused run and are
included in the final full suite. No production behavior changed after that run.
GUID audit:2340 unique,0 collisions; explicit task paths have no overlap with the
1027-path protected manifest. Final full/ownership commit gate follows below.


## Full-suite and ownership close-out

First full GA02e-full-flaky.xml.gz:8121/8122 passed,00:30:11–00:31:42UTC; only the
previously recorded fungal self-cloud test flaked. All new cases passed. Unchanged
repeat GA02e-full-green.xml.gz:8122/8122 GREEN,00:33:01–00:34:32UTC,zero C# errors.
Both runs occurred September6UTC/September5 America/Chicago. Test increase82;
final restored-enhancement assertions are included. No Assets edits during runs.

While waiting, read-only preparation recorded A45 Sharp/reforge and A46 stacked
weapon target/merged forge output issues, plus A47 malformed-unit and crafting
rollback controls. These are separate finite repair plans, not completed claims
from A03's stack comparator. Next A05 handles derived carry-penalty refresh only.

Ownership audit: 28 explicit task-owned paths, zero overlap with the
1027-path protected manifest. Staged only this list; preexisting/concurrent work
remains unstaged. Metadata copied from existing assets with only GUID changed.
