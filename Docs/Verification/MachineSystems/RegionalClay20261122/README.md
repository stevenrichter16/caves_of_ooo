# Regional mire, atomic clay baking and well repair — November 22, 2026

The existing baking action could create two clay from only eight mire when
ObjectCreated synchronously re-entered it. The corrected action leases one actor
action before callbacks, binds exact source/output identities, pays once, and
undoes only owned quantity deltas. [Policy](../../../ATOMIC-CLAY-BAKING.md).
No new recipe, saved Part fields, item or mandatory upkeep. Ordinary settling
water/oil and their separate callback limits are unchanged.

## Native evidence

Verbatim XML, mutants.json and native-results.json are retained. Native EditMode
runs used idle warm clone3 through Tools/CloneVerification, never the open main
Editor and never overlapping actual play. Overlapping runs are not additive.

| Run | Result | Meaning |
| --- | --- | --- |
| red_a | Compile invalid, no XML | Missing Data using; no RED credit. |
| red_b | 18 total, 16 fail | Initial defect countercases; the original late-fuel fixture was invalid and is excluded from that claim. |
| red_c | 2/2 fail | Both re-entry variants print nested=True/clay=2/mire=4/fuel=5 from12mire/25fuel. |
| green_d | 63 total, 1 fail | Late-fuel fixture started at MaxFuel25 yet expected27; production bounded at25. Fixture corrected to20, expecting22. |
| red_e | 1/1 fail | Corrected late-fuel fixture on original source: expected22, actual20. Independent contribution was rewound. |
| green_f | 121/121 pass | Before final six controls. |
| green_g | 127/127 pass | 25 AtomicClay +43 FiniteHearthCooling +15 MaterialRepair +13 NaturalHeatSettling +31 StillSettling. |
| factory_budget mutant | 25 total, 3 fail | Post-factory source guard removed. |
| output_snapshot mutant | 25 total, 5 fail | Absolute later output snapshot reintroduced. |
| actor_snapshot mutant | 25 total, 3 fail | Blanket actor snapshot reintroduced. |

Final GREEN and mutants have zero C# compiler errors in their private logs. Cases
cover distinct/shared transaction re-entry, changed feed/volume/fire/anchors,
owned/malformed outputs, compatible merge, exact fuel exhaustion on both still
variants, factory throw/released lease, late injury/fuel/drop and replaced source
or output Parts. Independent additions/custody survive rollback. Raw provisional
credit consumption and arbitrary identity replacement cannot receive stronger
restoration promises than bounded diagnostics. Full corrected-head regression
is still owed; these127tests are a targeted run.

## Actual GameBootstrap play

One game8ab6a6384c2a44deb8b3a68303809d8f; actual native InputSystem keys through
normal menus, GameBootstrap movement and F5/Continue. Observations are read-only.
See observations.json and13actual GameView PNGs (2315×1189; vertical flip only).
The first well-before-menu PNG targets grass9440 and is explicitly excluded;
well-actual-before-menu and well-before-reader target9998 correctly.

1. From prior earned cooling2760, put all fixture clay2243×20 into campbox2150
   through Inventory. Move to Gleaners11.10, stand40,9 by well9998 at41,8.
   Reader shows split lining/two-clay bill. Actual zero-clay Repair refuses freely
   at2850/HP385, source13151 still48. Archive before Stop.
2. After new code imports, fresh2850 matches the entire measured before snapshot.
   Travel to Works17.7; stand dry34,9 beside real finite mire13151 at34,8.
   Select receiver2238 explicitly: fill12 at3150, source48->36; receiver2239
   fills12 at3160, source36->24. Fixture mire2231=11 and2235=1 untouched.
3. Return to camp10.11. A viper blocks approach; actual fist combat kills it,
   earns9XP and changes existing reputations; poison/combat reduce HP to360.
   No reset. Reach40,9 at3490. Feed hearth2147 twice: timber2267 17->15,
   fuel2.75->22.75. Relight at3520: striker2297 19->18, fuel22.5/temp441.5.
4. Still2148 at41,8 preview quotes8of12mire/one clay/10fuel. Cancel is free
   at3520. Execute exact receiver2238 at3530:12->4, new clay14786×1,
   fire12.25/temp433.17. Execute2239 at3540:12->4, same clay14786 merges
   physically to×2, fire2/temp425.006622. Source24, boxed fixture20 unchanged.
   The two batches spend20process fuel; relight/batches spend0.75ordinary burn.
5. Normal F5/archive-before-Stop3540. Separate normal fresh Play/Continue3540
   matches the entire measured snapshot, including source, leftovers, stack,
   HP360, striker, timber, fire and boxed fixture stock.
6. Carry only produced14786×2 to the actual Gleaners well. At3630 it remains
   unrepaired/unusable. Normal Repair commits3640, consumes exactly2clay,
   makes well usable, cause60f2083250504eeabe87ba22e35f5604. Boxed20 intact.
   Menu now offers DrawWaterAtWell and no Repair. Full reader shows fresh seam.
   Actual Draw water says: You drink. The well water is cool. Tick/HP unchanged.
   Existing connected-restoration reward adds30XP once (9->39, level1).
7. Normal F5/archive-before-Stop3640. Separate fresh Play/Continue3640 matches
   the entire measured final snapshot and XP39. Fresh menu again lacks Repair;
   actual DrawWaterAtWell succeeds freely with the same measured snapshot.

The three earned-save archives contain the four files named by each archive-pins
manifest, verified before packaging. Before source pins intentionally name the
old committed baseline; source-pins.json names the final six runtime/test/meta
files. Fresh3540 and3640 are distinct executed persistence checks, not inferred
from archive copying. No static receipt survives a fresh session.

## Failures, preservation and scope

A scripts-only refresh compiled the modified consumer without importing the new
helper. The console reported CS0103 and missing metadata; the operator incorrectly
continued into scene/start orchestration. Play refused and no native gameplay
input was posted. Force-all asset refresh then imported the helper. Metadata,
idle state and zero console errors were gated before retry. One transient
isolation Begin returned unsuccessful during scene/domain transition; no root
or play remained. Stable retry resumed exact2850. These attempts earn no play.

Latest idle console also contains the existing Input Manager deprecation and an
MCP WebSocket connection error; neither is a gameplay/compile result. Final main
is stopped/rootnull/sceneclean/profileroff, original7built-in presets/custom0/
selected0/no owned view. Fresh preservation.json verifies84foreign and861
selected normal file hashes unchanged; analytics and TestResults.xml excluded.
This is selected-file evidence, not whole-directory or byte-exact preferences.
All builds/copies remain retained. No geometry/FPS/controller/physical keyboard
claim. Source/artifact/gameplay-evidence review is read-only independent review,
not a second native operator. Publication/current Mac delivery are recorded in
the continuation; this receipt alone is not a full delivery claim.

The well retains legacy Stock=-1 endless service; no measured hydration benefit
or finite tariff. Pool-less well filling supports Waterskins in source but not
generic LiquidFlasks despite broad reader wording; no flask-fill claim. Cellar
clay and mender stock are source-supported alternatives, not played purchases or
price comparisons. Inherited HPmax400, carry1050, kits, timber and striker are
fixtures. Unloaded fire remains2/temp425.006622 during travel: chronology is
still open. No ordinary-acquisition/economic balance or NPC survival acceptance.
No new protected lore. All twelve complete-machine gates remain open; graphics
cancelled, capstan/cultures parked, Deck deferred.
