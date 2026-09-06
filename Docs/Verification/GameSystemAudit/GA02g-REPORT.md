# GA02g — permanent paid upgrades through reforge

Status: COMPLETE. Baseline88409efc,8211/8211GREEN.

A45: rebuilding components overwrote Sharp's paid penetration while retaining the
ModSharp marker, preventing replacement of the lost benefit. It also erased paid
mineral adjectives, though their enhancement Parts/effects remained. Reforge now
rebuilds component contributions and reconstructs recognized permanent properties
from existing state. Temper still melts, including HP-maximum restoration.

## Verified references and scope

Root and independent reader checked actual Sharp/mineral implementations, real
recipes/content/components, ApplyComponentStats/TryRecomputeStats/TryReforge,
ClearTemper, component preview, equipment hooks, save round-trip and UI/station
routes. Local Qud ModSharp36–49 also applies direct penetration, but no Qud
reforge parity is claimed: this is CoO's component/modification contract.

- Actual Sharp recipe mod_sharp_melee costsBC, outputmodifiermod_sharp. Minerals
  pay one actual PaleSalt/ChoirIron/GlowQuartz, with no bit cost.
- Actual component IDs end in Component; shorter historical tests use fixture IDs.
- Legacy Sharp marker is separate from the2-enhancement-Part cap; duplicates and
  ordered Parts must all remain, with one visible label per recognized family.
- Combat Penetration uses damage channel. Combat derives DV rather than reading
  a fixture DV stat; fixed noncritical10 hit plus landed/single-record assertions
  correct the first consumer fixture without altering combat.
- Native actor-present station menus use CraftToggle:<ID> and CraftKit. Duplicate
  Sharp target exclusion is a menu observation, not a dispatched rejection.

## Implementation and divergence

Three runtime files: shared internal Sharp tag/adjective/+1 constants and mineral
adjective constants; ApplyComponentStats first rebuilds component base, then restores
Sharp's single marked contribution and stable known-family labels. It never replays
paid Apply, TierConfigure or equipment hooks. All Part occurrences, order, stored
fields, ModificationCount and equipment references remain intact. Family lookup
keeps existing assignable-type semantics; no authored subclass support claim.

Cold-eye discovered custom anonymous components could preserve an obsolete temper
name and repeat paid prefixes. Their stable fallback now matches the actual
ForgedWeapon default, 'forged weapon', before decoration. Actual shipped components
have names (existing content pin). No arbitrary old-name parsing or arbitrary old
penetration delta survives component rebuilding.

No new saved field, migration, output-quantity/target-identity/payment repair,
content/sprite, normal per-frame/per-turn hook or performance claim. Old damaged
saved weapons repair on their next successful reforge, not on load. A46/A47 remain
separate; native uses singleton outputs and actual resident replacement components.

## Evidence so far

| Artifact | Result | UTC |
|---|---|---|
| GA02g-red.xml.gz |24:7pass/17fail before minimum|01:10:38–39|
| GA02g-green.xml.gz |226/226 minimum plus neighbors|01:12:32–37|
| GA02g-adversarial-red.xml.gz |56:52pass/4fail;2real+2combatfixture|01:18:28–31|
| GA02g-expanded-red.xml.gz |63:54pass/9fail;2real+7absentnative|01:21:12–15|

All listed runs0C#errors. Initial24regression+32dedicatedadversarial+7staging.
Independent assertion improvements included exact mineral payment and actual
membership/Physics owner on singleton refusal; count1 alone survives removal.

## Review and honesty bounds

Independent actual-diff review found no further state/equipment callback hazard.
Dedicated probes cover marker versus arbitrary ModCount, old arbitrary stats/name,
all mineral pair orders/stack identity, duplicate Glow save/unequip/reequip,
stored custom tier/damage values, missing Render, actual melee consumer, service
ownership/station refusal, and preserved paid slot cap. Final native review cleared below.

Planned native: actual forge, SharpBC payment, PaleSalt payment, two station
reforges using each actual returned Oak, Sharp-popup exclusion versus freshDagger.
Can verify commands, identities, payments, modifiers and diagnostic outcomes.
Cannot verify visual feel/readability; temper, equipped Glow, save/load, combat,
anonymous content and refusal conservation remain EditMode evidence. No native
scheduler/performance claim. Temporary audit coroutine ends after its run.

## Native and focused completion

Expanded265/265GREEN,01:24:58–01:25:06UTC,zeroC#errors. Native first completed
run **6a4aafb74939437eb48f95731ee5c157**, **44/44PASS**,16.9175985s,exit0,
zeroC#errors. GA02g-native.json/log.gz retain the exact observations.

Actual keyboard flow forged from three selected component types, paidBC for
Sharp and one PaleSalt, then twice used the station's CraftKit verb to replace
Oak. The second replacement was the first reforge's actual returned entity;
the selected weapon remained marked. Penetration stayed base+1, same mineral
instance retained Tier2/BonusDamage4, ModificationCount stayed2, each label
appeared once, canonical name stayed stable, and remainingBC stayed1each.
Sharp's target popup excluded the reforged weapon and included the actual clean
Dagger; cancellation left resources unchanged. No direct command invocation or
payload injection in the driver. Only fixture staging sets initial resources.

## Self-review

- 🟡 A45 actual paid Sharp contribution and mineral label loss fixed after17REDs.
- 🟡 Anonymous components: stable base fallback removes obsolete temper names and
  prevents introduced duplicate-prefix behavior, after two corrected RED cases.
- 🔵 Counterchecks strengthened: actual mineral payment, singleton membership and
  Physics ownership, positive replacement installation/removal, repeated canonical
  native name. These are test improvements, not additional gameplay bugs.
- 🔵 Combat fixture corrected after reading derivedDV behavior; retain both raw
  failed/corrected runs and do not count those initial misses as combat defects.
- ⚪ Existing assignable mineral-family lookup retained. No saved schema or load
  repair. No A46 output/unit transformation or A47 payment/receipt claim.
- 🧪 Native proves actual controls and state, not visual/feel, save/Glow/temper,
  custom anonymous content or combat. Those have EditMode evidence; no75sperf
  requirement for these one-shot changes.

Precommit ownership/GUID audit and full-suite result are recorded at close-out.

Final independent runtime/test/native/launcher review:0must-fix. Raw native log
contains two already-recorded A31 destroyed-Camera teardown errors in
ZoneRenderer315→WorldFxCoordinator.CancelAll, after the successful audit/exit.
These remain queued rendering debt, not a clean-shutdown claim or a reforge
regression. The44observations and exit0 remain the measured result.
Initial ownership audit25paths,0protectedoverlap;2352uniqueGUIDs,0collisions.


## Final verification

Full **8274/8274GREEN**,01:28:16–01:29:57UTC,100.5721342s,zeroC#errors;
GA02g-full-green.xml.gz. Baseline8211→8274(+63), native44/44. Final independent
review0must-fix.26ownedpaths,0protectedoverlap;2352uniqueGUIDs,0collisions.
Three runtime files, six new test/native C# files with metadata, audit/daily docs,
this report and all GA02g raw evidence ship in one commit.
