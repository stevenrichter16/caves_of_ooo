# Merchant staples, special consignments and an oven project

November23,2026. Bounded Editor qualification of FarmerStock and ArmorerStock.
This adds dependable base goods and persistent limited special lots, plus one
existing local project that expands future farmer supply. Routes, unique relic
issuance, other merchant profiles and all twelve complete machines remain open.
The [current Mac artifact/startup receipt](../MacCandidate73FB59DA4/README.md)
is separate; this receipt claims no packaged gameplay, controller or performance
qualification.

## Policy and independent source corrections

See [the contract](../../../MERCHANT-STAPLES-AND-SPECIAL-STOCK.md). Farmer base:
3gladroot seeds and2emberwheat seeds. Armorer base: one leather armor, cap and
boots. Replenish only deficits after strictly more than300 game ticks. Matching
usable base payloads count; modified/untradeable/unsupported/foreign/empty goods
remain intact. Canonical new staple units stack through the existing routine;
incompatible recipients cause separate exact insertion.

Each special lot holds two existing ordinary goods. Farmer choices are four
seeds; armorer choices are five pieces of protective equipment. Stable ordinal
selection and saved generation advance one position through each catalogue.
The current lot gets at least1200 game ticks from its actual installation. A
late visit installs at most one next lot and grants it a full new window.
Menu reads, reload, cancellation and travel do not reroll a current lot. Open
views, live offers and active trades defer reconciliation.

An existing local VillageOven at StableRepair or ImprovedWithCaretaker adds a
third good, roasted hearthbulb, to the farmer's next lot. Its caretaker-only
condition is not required for stable manual repair. Other PlaceConsequences
supplies retain their own authority. This is abstract merchant-supply eligibility,
not NPC recipe execution or material-free machine production.

The saved Part holds exact consignment members, audited bounded payload keys,
generation, install/staple ticks and selection. Both dram and barter transfers
release membership transactionally, with separate metadata rollback. Special
units cannot absorb ordinary/player-sold stacks. Rotation returns only untouched
single units still owned by the merchant; changed, escaped or unauditable goods
stay intact. Purchased and resold goods are ordinary stock thereafter.

Independent review identified and verified corrections for pre-staging foreign
equipment/custody, requested blueprint/identity, factory reentry, new reservations
opened by callbacks, changed oven eligibility, stale staple deficits, missing
purses, filter-hood fingerprints, and clipped/misleading project feedback.
GasMaskPart.Power has an exact merchant-only fingerprint path; ordinary grouping
and stacking permission were not broadened. A trade-count lease preserves the
existing contract for nested independent-item trades to the same merchant.
Source review of the final implementation and play polish:0remainingP1/P2.

## Native verification

[Run receipts](native-runs.json) record failures, controls and exact external
XML/log paths. Initial RED a did not compile because the test used an unavailable
SetProperty API; it is excluded. Compiling stub RED b:24total/22fail/2controls.
Further compiling RED batches reproduced the concrete review findings and two
actual-play polish issues before correction.

Final `merchant_consignments_play_polish_green_20261123a`:234total,233passes,
zero failures, one explicitly skipped golden-regeneration utility, zero compiler
errors. Includes64 new feature/countercase executions plus existing trade,
barter, restock, inventory grouping/transaction and saved-Part contracts. Both
complete authored choice cycles were exercised. The full inherited regression
was not rerun and remains failed; do not infer a corrected full-suite count.
[Source pins](source-pins.json) identify the verified runtime/test files.

## Actual GameBootstrap play

Used real native input, ordinary movement/menus, F1 readers, cancellation,
confirmation, F5, stopped Editor play and fresh Continue in disposable save
roots. No direct stock, clock, project, wallet, inventory or actor-stat mutation
was used to produce these outcomes.

The final branch starts from the retained3540 regional-clay checkpoint. That
prior [processing receipt](../RegionalClay20261122/README.md) documents creation
and merging of clay14786; this pilot reuses that evidence and does not requalify
acquisition. The inherited fixture has HP400, Strength70, carry1050 and extensive
provided goods. It cannot establish economy, difficulty or ordinary acquisition.

- At3740 the armorer showed ordinary leather staples and starred coldward cloak
  and kilnfelt apron. F1 showed AV and DV on separate lines and explained cold
  resistance/slots/cost. The95dram cloak was unaffordable with50; confirmation
  preserved the exact item, purse and tick. See [shop](armorer-first-lot.png) and
  [reader](armorer-special-reader.png).
- At3960 the farmer's gladroot base top-up was one x3 stack. Cancelling the
 11dram seamleaf purchase kept50drams/current stock/time. Confirming transferred
  exact17713, reduced50→39 and removed its consignment membership. This is the
  final branch; the earlier3900purchase/fresh records are a separate pre-polish
  branch and are not relabeled3960. See [purchased stock](final-farmer-bought.png).
- The elder supplied guide17744 through conversation. The farmer's existing
  rebuild service consumed one of two carried FireClay and kept the guide.
  At4630 the saved oven was StableRepair and the old current lot still held
 17714, generation0/install3570/bonusfalse. The screen explicitly explained
  that baking joins the next consignment. See [waiting status](final-repaired-waiting-shop.png).
- A verified F5 checkpoint was archived before stopping. Fresh Continue4630
  preserved stage, clay1, guide, purchased17713 and exact old current17714.
 14native wait actions advanced4630→4770; no view/arrival meant no automatic
  refresh. Walking to the farmer and opening at4860 installed only generation1,
  with a fresh full window from4860:17750pitchpod,17751knotflax and the explicit
  third17752roasted hearthbulb. Old untouched17714 left the shelf and purchased
 17713 stayed carried. See [expanded lot](final-oven-bonus-lot.png).
- Bought17752 for19drams (39→20), left the shop, selected the exact item in
  the dense pack and ate through its ordinary action. At4870 it was consumed,
  HP360→367 and ColdResistance0→20. The world HUD/Focus/log showed the prepared
  meal and its100-turn duration. This is useful outside the shop and a concrete
  project→supply→food→protection outcome. See [actual effect](final-meal-effect.png).
- F5, archived verified4870 files, stopped, fresh Continue: the meal stayed
  consumed, HP367/ColdResistance20/purse20 persisted, seed17713 remained carried,
  and generation1/install4860/bonustrue/current17750+17751 were exact. See
  [fresh frame](final-fresh-4870.png) and [observations](observations.json).

An earlier4310repair was observed but its checkpoint copy failed; isolation was
then closed, so that checkpoint is excluded. The repaired state was replayed and
actually verified fresh at4630/4870. The operational correction is to resolve
the post-repair announcement, confirm the normal input state, press F5, verify
the expected saved tick and successful archival, then finish isolation. Never
clean up after a failed checkpoint guard.

Some townspeople fought soot gremlins; an earlier branch lost its armorer. This
slice does not establish the cause/control or qualify settlement survival. The
final branch's armorer was alive when its stock/readers/refusal were exercised.
The purchased seamleaf was retained, not planted here. Other stock/project
combinations, remote routes, unique issuance and long-run population/economy
behavior remain unverified.

Independent source and gameplay-evidence review found0remainingP1/P2, inspecting
the actual stock/reader/refusal, waiting/bonus and meal/fresh frames plus exact
checkpoint receipts. This is an independent review of recorded play, not a
second native operator. Buyback, barter, depleted-staple refill and adversarial
conditions have native-test evidence only; do not promote them to hand-played.

## Preservation and limits

Final check16:28:04UTC: all84foreign tracked hashes and861selected normal-state
hashes matched. No byte-exact preference-file claim. Main Editor stopped,
SaveRootOverride null, scene clean, profiler off, original7built-in views/index0;
owned input device removed and original settings restored. Prior apps, private
exports, checkpoints and foreign files were preserved. No foreign .meta was
staged. An additive MerchantStockPart golden entry preserves all old contracts.

Hardware: Mac17,2,32GiB,10CPU,arm64,macOS26.0; Unity6000.3.4f1. Native screenshots
show actual Editor frames. No FPS or production-hardware budget was measured;
performance qualification remains open. This bounded supply/project slice does
not close any of the twelve full-machine acceptance gates.
