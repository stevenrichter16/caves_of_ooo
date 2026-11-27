# Optional shower collection

W1a implementation contract, November23,2026 (America/Chicago). Bounded
Editor collection/manual-watering acceptance is recorded in the
[receipt](Verification/MachineSystems/WeatherCollection20261123/README.md).
Automatic crop rainfall and wholeW1 remain open. The
[parent weather design](WEATHER-AND-SEASONAL-TOOL-USES.md) remains the governing
bonus/comfort and seasonal-tool direction.

## Scope and source corrections

Split the first weather increment into W1a finite manual collection feeding
ordinary crop watering, then W1b automatic crop rain credits. Whole W1 requires
both; W2 still requires different jobs on the same physical frame in distinct
seasonal/material contexts. Independent bounded source/design critique found
no blockers to this split.

Zone.CurrentWindSpeed and CurrentWindDirection already affect gas dispersion.
W1 does not write them. Natural rain does not call Conjure Rain wholesale,
apply WetEffect, change fire/temperature, or add actor/creature exposure.
Dry-weather play, crop yields, ordinary water and services retain their baseline.

Use an explicitly finite repeating64window regional policy (32game days at
600ticks/window). Pure rain count/next queries examine at most one cycle plus
whole-cycle arithmetic. An independently hashed infinite history with capped
replay would lose legitimate unloaded benefits, so it is not this policy.
Policy1 supports ordinary surface regions only. Regional weights and eight-day
season phases are provisional tuning; weather does not disclose protected lore.

## Fixed shared authority

Morrowfast’s public cloths give a normal-map cue before any menu opens.
The frame shape shows dry, pooled and drained cloth. Both frames read
the same saved allowance and repaint after the final draw or a window
change. [Native cue and reload evidence](Verification/MachineSystems/PublicCatchClothCue20261127/README.md)

A fresh-only public allotment can provide a fixed catchment anchor and two
fixed collecting frames. No cached retrofit, automatic inventory grant,
portable entitlement or player-created new allowance in W1a. Each collector
holds the exact anchor Entity reference; a missing/moved/replaced/ambiguous
anchor refuses. Never elect or create a replacement authority from a collector.

Saved Part hygiene rejects raw zone/coordinate authority fields. Bind the
anchor and each frame to an exact existing ground Entity reference, with a
saved world key. The receiving manager, actual Zone membership, current owner
cell and bound ground remain authoritative. Plot key and bounded footprint
must be unique and nonoverlapping. Public access is explicit; private or
unqualified collection refuses. Clean cloth is a supported configuration,
not a recurring dirt/maintenance meter. Packing/replacing a collector cannot
reset the anchor's allowance. Entity references use existing graph serialization.

The anchor saves policy version/activation tick, world/plot binding and last
spent window/units. A qualifying window offers3water total across collectors.
Old unused allowances are not banked; read-only forecasts/menus never mutate
the ledger. A paid collection chooses an exact clean-water-compatible carried
flask or waterskin, up to its free capacity and the remaining allowance.

Quote exact collector, anchor, window/ledger and receiver contents/capacity.
Revalidate those identities, capabilities, world/cells, rain and ownership at
commit. Join the ordinary InventoryTransaction and claim participating owners.
Own only quantity/ledger deltas; unrelated injury, forced drops or independent
water additions must not be rewound by a blanket action snapshot.

## Real consumer baseline

Before implementation, actual GameBootstrap continued the inherited supplied
strong8ab6 branch at Sill12210/HP412/purse26. Normal map travel reached2.6;
the verified BeforeEntry archive12460 preserves its uncached entry opportunity.
Fresh entry12470 showed the existing public Morrowfast allotment, six standing
crops and prepared beds. Directed read-only path scouting aided navigation;
this does not establish natural discovery or ordinary acquisition/balance.

Ordinary approach reached59,12 at12670. Existing mosshulk combat caused26+24
damage and one actual bump attack dealt14; HP362, no injected healing/items.
Crop33015's full reader said seed/dry/paused and named carried-water or Conjure
Rain alternatives. Actual C-menu chose WaterCrop|2240: flask1water became the
same empty flask; at12680 crop progress1/moisture39/remainder0 reflected the
existing40moisture top-up and one paid ten-tick interval. Other supplies were
not consumed. F5/four-file archive12680 verified before isolation finish.

Private evidence: /private/tmp/coo-weather-baseline-20261123 and earned roots
/private/tmp/coo-weather-before-entry-earned-20261123 and
/private/tmp/coo-weather-manual-water-earned-20261123. Observers initially used
wrong State/Stop helper names; those compile-invalid observer calls posted no
gameplay action and earn no product/test evidence. Correct Finish(owner,token,0)
followed verified archival; original mice/input settings restored and main later
observed stopped/rootnull/clean/original7views. No weather output demonstrated.

## W1a evidence and remaining gates

Compiling native RED and countercases precede implementation. Cover finite
shared allowance, two collectors, exact/partial receivers, menu purity, stale
window, missing/moved/duplicate/overlapping anchors, public/private/cleanliness,
invalid payloads, bounded calendar arithmetic, transaction callbacks/reentry,
legacy activation and graph save/reload. Run appropriate retained regressions.
Actual reader/paid collection/manual watering, dry-weather refusal/ordinary
manual alternative, travel and fresh save/reload passed with independent
source/gameplay-evidence review0P1/P2. Final106total/105pass/0fail/1utilityskip
includes52weather feature cases. A second distinct water consumer was NOT
demonstrated; bounded W1a is narrower than the earlier wholeW1 plan. Native
partial receiver/next-window cases are not hand-played. Natural wandering and
wholeW1 remain open. See the receipt for RED failures, callback fixes, directed
strong-fixture limits, helper exclusions, exact pins and preservation.
No offline-only foundation acceptance.

## Local rain guide at Sella’s shelter

Roadmap 34 now has a second kind of optional shower work. The dressing
shelter already has a dry outdoor sumpsieve bed, a separate ripe crop and an
ordinary dressing service that uses pads. The public guide directs one
finite local rain portion into the dry bed through one action. It saves a
unit of carried water and gives the same moisture as hand watering. It makes
no pool or inventory item. Sumpsieve needs 48 moist rounds and one watering
provides 40. The player can tend the remaining growth later, use the ripe
crop, buy supplies or continue along the ordinary road.

Use the existing Sodden weather observations and crop moisture rules. Admit
one fitting on clean exterior ground beside the exact bed after the district
is accepted, only in the next fresh eligible generation version. Leave the
existing footing and paths clear. The saved fitting, prepared soil and exact
crop define the access. The current mild shower allows one portion per weather
window. Nothing accumulates while absent. Selection binds both owners and
the current window. Reconcile prior crop time before the action. A valid
receipt commits the crop top-up and finite rain together. A spent, flooded,
ripe, moved, already watered or replaced bed uses no supply. Reuse the same
ordinary moisture change for flask and guide so adding this source preserves
both growth and the readback. Track the selected source and saved use in the
existing diagnostic stream.

Actual play reached the shelter from an ordinary new start, used the local
rain portion, returned to ordinary spell watering for the remaining growth,
and harvested the exact crop. The native comparisons cover carried water,
fair and stale windows, claims and late failure. This establishes one bounded
directed-watering site. Automatic rain credit remains a separate design.
[Evidence](Verification/MachineSystems/SoddenRainGuide20261125/README.md)

W1b must settle the earlier crop interval before each rain credit/exposure
change, retain wet-tick remainder and handle arbitrary existing moisture above40.
Credit at rainy-window start only; later planting waits for the next eligible
credit. Forty moisture lasts at most400world ticks, versus600between starts.
Choose exact bounded interval integration before W1b RED; preserve CropSystem
growth transitions and qualify factory/callback behavior separately.

All twelve complete-machine gates and the original failed full regression stay
open. Graphics cancelled; capstan/cultures parked; Deck deferred. Preserve all
foreign work, normal saves/preferences, every app and earned checkpoint.
