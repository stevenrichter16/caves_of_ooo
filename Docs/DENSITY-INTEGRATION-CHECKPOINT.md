# Density completion — first integration checkpoint

Status: verified foundation ready for commit. This checkpoint starts from `50ef23d2`
and follows the previously committed completion plan. It does not close every
milestone in that plan. The status ledger in DENSITY-COMPLETION-PLAN.md remains
authoritative.

## Result under review

The first integration connects existing armor, tactical abilities, readable texts,
water/cooking/rest/light actions, local people, travellers, finite harvests and
smoke to ordinary content. Recognizable imported enemy identities are replaced
by the original Marlbacks and grove lantern-moth, with independent quest creatures
and narrow saved-identity compatibility. Ordinary deep caves gain an alternate
room layout and a bounded four-item T4 reliquary source. BitLocker stays dev-only.

The image-request addition is a real authored world zone and saved ReferenceGlade
scene. Its original prop kit, scoped rendering, ordinary input, finite stock,
harvest depletion, saves and world exits share native game systems. The third native route passes 12/12 with zero errors; comparison still calls for
more upright reeds, fuller grass and larger figures. A fourth art pass stays
private until this checkpoint is committed; rejected screenshots remain evidence.

## Integration corrections

- C13 rendering roster pins were expanded to the actual original bodies, preserving
  mesh/skin/bone/palette assertions. Quest gnomes use exact DirtGnome identity.
- Harvest tests now first reject remote actors, then place them in actual reach.
  Neighbor/depletion/overflow controls remain intact.
- Message observers follow the deliberately changed player-subject grammar while
  preserving transaction callback/rollback assertions.
- Locked container access uses both native lock authorities, including stale UI
  selections. A native four-case RED caught the final UI consumer.
- The reserved reference address has an explicit positive glade route and negative
  generic Spread route; other POI/wilderness controls remain.
- Historical loot reports measured stocked contents and handled attempts, not
  necessarily successful opens. The corrected census replays exact baseline zone
  IDs and records actual matching actor OpenContainer events separately.

## Evidence and limits

The initial unfiltered Unity run completed 16,794 tests: 16,742 passed and 52 failed.
The subsequent 580-case affected selection passes all cases. Fifty failures pass
under the exact same test names; two roster-count test names changed to reflect
32 skins and 396 native objects while preserving their semantic checks. The additional 239-case glade/census selection also passes after the intentional
grass-height contract was updated from the old hedge-mat silhouette. The final unfiltered Unity 6000.3.4f1 run passes **16,996/16,996**, zero failures
or skips, in 921.086 seconds. The preceding full run exposed ten stale quest
fixtures, repaired to require DirtGnome while rejecting a disguised Marlback;
103/103 affected cases passed before the final complete rerun. The saved XML,
receipt and exact source snapshot are in
`Verification/DensityCompletion/Integration/native-full-foundation-green.*`
and `native-foundation-asset-snapshot.json`.

The exact 672-file standalone baseline selection compares 10,060 baseline cases
with 10,075 after C10.1/C11.1. Both have the same 295 environment failures and zero
newly failing tests. New fixtures outside this selection have separate focused
coverage; count differences alone are not used as a regression verdict.

The second native glade route passed 12/12 with zero errors and eight captures.
A 60.163-second movement sample measured mean 6.321 ms / p95 7.617 ms in this editor
session. It proves input/state/persistence and records real frames. It does not
prove natural discovery, solo combat balance, player-build performance or visual
fidelity; the inspected second pass was rejected for further art improvement. The third
route also passed 12/12 with zero errors; 60.208 seconds / 9,842 frames measured
mean 6.117 ms / p95 7.010 ms. Its gameplay is accepted; its visual proportions need
a fourth pass, recorded as follow-up rather than called complete.

## Scope boundaries and commit shape

The original plan intended independently revertible milestone commits. Concurrent
implementation converged in shared blueprint, save, input, generation and renderer
seams, and the user subsequently added the complete enemy-identity and reference
scene migrations. This first checkpoint must preserve those tested integrated
contracts together; splitting shared files into speculative intermediate versions
would create unverified combinations. C10.2 lair persistence and later thermal,
liquid, furniture and exceptional-find work remain separate follow-up milestones.

This is a recorded workflow divergence, not a claim that the larger plan is done.
No unrelated pre-existing untracked files or MCP logs belong in the checkpoint.
Root uses explicit ownership manifests and inspects staged paths before commit.

## Q1–Q4 review

Q1: paired creation/removal, save/load, FOV invalidation, acquisition/depletion and
entry/return paths have matching ownership checks. Scoped art loses and regains
actual world authority through bind, refresh and frame paths.

Q2: existing inventory transactions, body footprints, source registries and native
world ownership remain authoritative. The new lock property resolves both existing
lock forms without changing saved fields or allowing stocking to unlock a chest.

Q3: each specialist document records flipped controls and dedicated adversarial
cases; native integration added stale-menu, scene selection, visibility and profile
ownership regressions rather than replacing failures with count-only checks.

Q4: generated placements and seeded maps can change; existing saves retain their
serialized parts except exact C13 identity aliases. The runner uses different
string hashing than Unity. Numerical/functional checks do not certify balance or
art quality. Remaining gates stay explicitly open in the living plan.
