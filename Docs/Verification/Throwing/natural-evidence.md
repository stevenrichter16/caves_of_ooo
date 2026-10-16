# Natural throwing acquisition and retained expeditions

This record reads completed native reports without rewriting failed checks. These are source-guided automated keyboard campaigns in the actual editor game, using original builds, generated sources, real purchases, finite supplies and normal enemy turns. They are not unaided player discovery, physical Steam Deck tests, or controlled comparison fixtures. The observer's existing pathfinder can inspect wider authoritative terrain/threats; throw eligibility itself requires a known visible owner and known clear trajectory.

Terminal JSON reports are losslessly compressed; `natural-archive-manifest.json` preserves their original byte counts and SHA-256 values verified after round-trip decompression. Live files are never compressed.

## Campaign ledger

| Campaign | Report | Outcome | Counted paid inputs | Completed owner turns | Acquisition / use |
| --- | --- | --- | ---: | ---: | --- |
| Bombardier, seed 29 | `../SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/report.json.gz` | Partial: first search exhausted; later real throws; unsafe approach stopped continuation | 663 | 664 | First 1 SP earned and Sure Grip purchased at input 427; four actual later throws while still level 2 |
| Breaker, seed 1729 | `../SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/report.json.gz` | Partial: initial search failed; later actual cudgel throw; stale recovery route stopped continuation | 541 | 541 | First 1 SP earned and Sure Grip purchased at input 465; one later throw while still level 2 |
| Bombardier, seed 64 | `../SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/report.json.gz` | Partial: source circuit lost its current safe district route | 410 | 410 | Finite source accessed; no earned point, purchase or throw |
| Warden, seed 64 | `../SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/report.json.gz` | Partial: source-circuit safe route unavailable in cellar | 317 | 317 | No earned point, purchase or throw |
| Duelist, seed 64 | `../SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/report.json.gz` | Partial: completed source circuit and checkpoint, no useful throw found at selected site | 480 | 480 | Sure Grip purchased with first earned point at input 467; no actual throw |

Initial campaigns share a 2,000 paid-input budget, with at most 700 per attempt and at most five attempts; optional held-out 201/202 are permitted only after an absent/ambiguous initial opportunity. Warden/Duelist controls have a separate 1,200-input/two-attempt budget. A counted input can contain more than one actual owner closure (for example a stun-blocked action); both metrics are retained. No unseen extra attempts may be substituted for the listed receipts.

## Bombardier 29: what was actually observed

The completed report is **not green**: `complete=false`, reported failures 3, unexpectedErrors 1, six of eight named checks passed. `throwing_natural_opportunity` and `throwing_natural_bound` remain unmet. The explicit failed initial search and later `fresh native movement footprint` precondition are distinct observations; the logged harness exception contributes to reporting rather than proving a third independent gameplay defect. Runtime 295.29 seconds, final HP 11/42, level 2, current-level XP 50, banked SP 0. The actor was alive when the observer stopped in `Overworld.15.10.0`.

The original source circuit succeeded: finite cellar goods, two FireClay spent on the real kitchen repair, introduction/permission, 2-dram meal commission, actual waterings and harvest. Its four one-time awards totaled 120 XP. At 427 counted inputs/tick 4380 the player was level 2, XP 5, HP 42, then bought Sure Grip through Field Manual with the earned single point. Original dagger ID 2700 was retained. No child skill or mature 4/6 SP checkpoint was reached. The subsequently observed three kills added 45 XP; this route does not establish fast child-skill progression.

### Initial failed search versus subsequent use

The first WetCrossing visit (`Overworld.13.12.0`, admitted family `WetCrossing`) consumed all 80 allowed approach actions, from counted 431 to 511. The observed path went from approximately (40,12), around the east/south perimeter, ending (14,21). It briefly reversed near (37,12), then followed a long safe route. This proves **no eligible shot was found by that bounded observer**, not that the chunk contained no possible legal shot. The receipt does not capture every rejected candidate's exact geometry, so an exhaustive absence claim is unsupported.

Read-only inspection found no concrete incorrect eligibility rule: the helper excludes adjacent targets, requires a currently known visible owner, exact range and a non-uncertain entity trajectory. The same helper found valid opportunities later, including a return to that same WetCrossing. The broad LOS approach predicate is only a routing heuristic; it is not a proof that the selected destination must immediately yield a legal throw.

Four later native throws used the original dagger against actual hostile MarlbackScrabblers, all before another skill point was earned:

| Counted input | Zone | Target ID | Tick window | Actual thrown damage | Evidence |
| ---: | --- | --- | --- | ---: | --- |
|550 |11.10 |2076 |5700→5710 |2 | Initial accuracy passed; later defeated through ordinary combat |
|611 |13.12 |13613 |6370→6380 |3 | Same WetCrossing on revisit; later defeated through ordinary combat |
|635 |15.10 |21274 |6670→6680 |3 | Later defeated through ordinary combat |
|659 |15.10 |21276 |6920→6930 |4 | Initial accuracy failed, then penetration/damage succeeded through the owned single retry |

Each throw window records one genuine completed player turn. The final retry follows the actual command's `RollThrowAccuracy || RetryPhysicalAccuracy` branch, not an invented forced outcome. Dagger recovery and explicit re-equipping happened three times between throws; it was no longer carried/equipped after the fourth throw when the cached recovery route became unsafe and stopped the observer. No Recovery skill was owned or credited.

Thus **natural acquisition and useful use before the second point are observed**, while the original designated-site search check remains failed. The report's final `outcome="no-useful-visible-throw"` retains the first search label; it must not be read as a summary saying there were zero throws throughout continuation. We retain that source receipt and explain the distinction here.

### Expedition, money and supply consequences

Three recorded completed continuation trips returned with actual losses: 36 inputs in the glade,6 to 11.11 (opponent observed but no safe combat approach), 40 to the WetCrossing. The next 15.10 trip stopped at HP 11. Both original HealingTonics were consumed through paid native use. Other supplies—including original offensive tonics/grenade, DriedMeat and the commissioned meal—remained; the policy did not optimize every available consumable. Therefore “no HealingTonic left” must not become “no possible healing/resource response exists.”

The actual apothecary ID 16417 in 10.10 was reached after 20 inputs of the optional detour. The trade page showed: HealingTonic stack 2 for 51 drams; BurnSalve 43; SpeedTonic 34; Panacea 170; MendleafSprig 11. Purse 48. **Water and Acid were absent**, so no purchase occurred and the purse stayed 48. The observer did not sell equipment, fabricate stock or call unavailable tonics affordable. A later revisit was attempted but stopped after 7 inputs because no current safe approach existed. No shelf replenishment, paid tonic replenishment or brewing cycle was observed. The remaining crop pulps are actual cure items, not assumed brewing reagents.

Four actual compressed campaign checkpoints were retained: first point and expeditions 0/1/2. Native F5/F6 preserved player identity, stats, gear, skills, tick and energy. The final incomplete trip was not substituted for those earlier retained saves.

### Native timing observation

The first 60.006 seconds sampled 17,956 frames after discarding two setup frames, during actual travel/UI/AI and report IO: mean 3.34 ms, p95 3.77 ms, p99 32.37 ms, maximum 1,661.76 ms. 1280×800, world camera 940×680, vSync 0, targetFrameRate −1, timeScale 1, lowDetail false. The window traversed glade and cellar; it was not padded with idle time.

These are end-to-end macOS Unity Editor frame deltas with automation/report overhead. The large maximum remains visible. They do not establish physical Deck performance, steady-game FPS, allocation behavior, or an optimized production build.

## Breaker 1729: acquisition, later use and the observer stop

This report also remains **not green**: `complete=false`, 3 reported failures, 1 unexpected error, the same initial opportunity/bound checks unmet. Runtime 219.68 seconds; 541 counted inputs and genuine owner turns. The actual opening circuit reached level 2 and Sure Grip was bought for the earned 1 SP at input 465 / tick 4760, with HP 42 and current-level XP 65. The HeavyFrame at 15.10 was present, including its finite Shattered Rime book cache, but the initial search found no admitted useful shot. No child was purchased.

Continuation trips 0 and 1 each used 6 inputs, encountered no selected hostile, and returned with HP 42 and both HealingTonics. On trip 2, the real original cudgel ID 2722 was thrown at MarlbackScrabbler 18167 in 13.12; tick 5370→5380 records exactly one paid owner closure, a successful accuracy roll and 3 penetrations. The subsequent 45-action combat ended with that enemy defeated and the player at HP 40. Thus the initial `no-useful-visible-throw` label again does not describe the full continuation. Final current-level XP was 80, SP 0; no 4/6 SP milestone or vendor acquisition was reached. Three actual checkpoints remain: first point and expeditions 0/1.

Both Bombardier 29 and Breaker 1729 stops expose the same **observer route bug**, not a demonstrated gameplay movement defect. `NaturalRecoverOriginal` computed a whole path once, then replayed it across paid turns in which enemies and terrain could change. `StepTo` correctly refused a now-unsafe later step. Breaker had already taken four post-combat recovery steps; Bombardier one. A narrow observer-only correction is now installed to replan after each paid step, retain the original 12-step/total-action bound and record abandonment when no current safe route remains. These failed receipts will not be rerun or rewritten. Until the correction has its own native evidence, source review/compile do not count as a successful native recovery demonstration.

Breaker's first 60.030 seconds sampled 19,621 frames: mean 3.06 ms, p95 3.58 ms, p99 31.36 ms, maximum 2,288.39 ms. Settings and honesty limits match the Bombardier observation; this is actual macOS editor travel/UI/AI work, not a Deck benchmark.

## Held-out Bombardier 64: blocked natural source circuit

Report `7e313c0c8ba543ac888fded6fe8a1412` stopped after 410 inputs / owner turns, 139.85 seconds, with HP 40, level 1, XP 90 and SP 0. Ordinary start, starting budget and finite original source checks passed; earned point, purchase, opportunity, checkpoint and bound did not. The active district route no longer had a safe approach (`DistrictReach`), so no point or throw is credited. This differs from the cached recovery-path bug: it occurred during the original source circuit, before Throwing was owned. No campaign checkpoint was manufactured.

The first 60.005 seconds retained the sampler's maximum 19,998 frames and explicitly reported overflow. Its stored samples have mean 2.79 ms, p95 3.15 ms, p99 28.23 ms, maximum 2,320.24 ms. Because overflow is true, these stored samples do not cover every frame in the whole window; they cannot be advertised as a complete-window performance distribution.

The three initial runs used 1,614 of the declared shared 2,000 counted-input budget. The remaining 386 is less than either observed completed first-point circuit (427 / 465). There is no additional seed retry solely to produce a passing result; optional held-out seeds remain unrun. Warden/Duelist controls use their separately declared budget.

## Warden 64 control: source-circuit refusal

The Warden control stopped after 317 counted inputs and owner turns, 95.35 seconds, at HP 40, level 1, XP 60, SP 0. Original finite source access passed; the current safe district route inside the original cellar was unavailable before the first point. The report retains 2 failures and 1 unexpected observer error. There was no Throwing purchase, actual throw, or saved first-point checkpoint. The first 60.037 seconds filled the 19,998-frame buffer (`overflow=true`): retained-sample mean 2.86 ms, p95 3.49 ms, p99 29.95 ms, maximum 1,374.02 ms. As for Bombardier 64, this overflow prevents a claim of complete-window frame coverage.

## Duelist 64 control: purchase and checkpoint, absent selected opportunity

The Duelist control completed its finite source circuit and native save/reload checkpoint with no fatal or unexpected error. It remains **partial**, because its one failed check is the absence of a selected useful throw: 7/8 named checks passed, including the budget bound. At input 467 / tick 4780 it bought Sure Grip with its first earned point, at level 2, HP 42, current-level XP 35. The source-guided visit observed the actual TrappersStore at 11.11, but the bounded search found no admitted useful shot. Final count: 480 paid inputs / owner turns, 181.71 seconds, unchanged HP 42 and XP 35. No child, vendor replenishment or actual throw is credited.

The first 60.023 seconds sampled 19,995 frames with no overflow: mean 3.00 ms, p95 3.26 ms, p99 29.72 ms, maximum 1,450.84 ms. This retains the same editor/automation limits as the earlier timing windows.

The final ledger is **three initial attempts (1,614 / 2,000 inputs) and two controls (797 / 1,200 inputs)**. None of the five reports is fully green. Three cohorts earned and bought Sure Grip; two later produced genuine throws before another point. The two ongoing-campaign stops were traced to the observer's cached recovery route, which is now corrected but not replayed. The two earlier safe-route refusals and all absent-opportunity results remain evidence. No retries were added to obtain favorable results.

## Remaining acceptance

Naturally earned children and 4/6 SP pacing, naturally sourced Water/Acid, renewable brewing/restock economy and physical Deck behavior remain unverified until actual receipts demonstrate them. Controlled mechanics/comparison tests cannot fill those natural evidence gaps. Final screenshots require separate visual review; this document does not convert a completed script into aesthetic acceptance.
