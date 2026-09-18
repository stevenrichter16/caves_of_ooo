# Chunk gameplay implementation — discovery and first expedition

Status: implemented and verified, 2026-09-16. Both authorized recommendations
are complete. CG17: 356/356 focused; CG18: 14,728 passed with the exact 32
pre-existing failures; CGN06: 28/28 live checks and 60-second profiling passed.

## Authorized scope

Implement recommendations 1 and 2 from the audit's final response: fix discovery
and misleading interactions, and connect a complete early expedition between
Grovelands spawn (2.6.0) and Morrowfast (3.6.0). This also implements the matching
first two sections of CHUNK-GAMEPLAY-AUDIT.md, whose order is reversed.
Do not expand into Stillleaf, regional state cycles, new biomes or general
construction. Preserve the requested camera, voxel style and full-zone reveal.
Save migrations are explicitly out of scope; normal new-state persistence is not.

Qud reference: none. These are CoO-original content and integration improvements.

## Readiness and verification sweep

| Surface | Readiness | Verified constraint / correction |
|---|---|---|
| Native world content | 🟢 | Prior 412-zone census matches 1,161 source files; CG01 baseline completed before production changes: 14,544 passed / 32 pre-existing failed |
| Generic quests | 🟡 | Six IDs also share facts, item identities and recipients. Scoping IDs alone is unsafe. Give each existing story one canonical host rather than claiming independent copies |
| Guidance | 🟡 | Scribe RegionOverview is empty; ordinary rumors are generic. Resolve actual world POIs/known generated destinations; no invented coordinates or compulsory quest auto-accept |
| Morrowfast shops | 🟡 | Stillcord merchants have initial goods but empty tables; declared stock tables must opt them into the existing bounded refill policy |
| Quest cues | 🟡 | Legacy ColorString does not establish voxel visibility. Use actual quest availability/progress with readable native 3D cues and explicit lifecycle/visibility checks |
| Crop rows | 🟡 | Most are descriptive terrain, distinct from CropPart. Author sparse ripe rows and spent/uncultivated distinctions rather than making every field cell unlimited food |
| Summit water | 🟢 | Existing WellPart provides drinking; existing LiquidPoolPart projects water/contact. Reuse these, retaining native mutable owners |
| Early expedition | 🟡 | Spawn is a compost field, not a guaranteed red-fruit grove; grove-red has toxic/lore implications. Do not promise a nearby harvest source that generation does not place |
| Art | 🟢 | Existing voxel/sprite assets cover baskets, wrapped supplies, crops, water and residents. Reuse truthful forms; no glyph-only new objects |
| Workspace | ⚪ | Pre-existing edits captured in Verification/ChunkGameplayImplementation/prechange.json and external byte backup. Stage only owned hunks/files |

## Milestones

1. **Discovery integrity:** canonical quest hosts, truthful regional directions,
   Morrowfast stock renewal, actual voxel quest cues. Keep existing rewards and
   one-shot quest semantics; avoid cloning six stories into fake new adventures.
2. **Truthful use:** sparse real field harvests, drinkable bromeliads and real
   spray-water contact. Align examination, available commands and visual state.
3. **The early expedition:** an optional Morrowfast request leads to an actual
   recoverable supply cache in the western spawn chunk. Inspect/open and carry
   supplies (hauling is local to the field); return to the real giver, resolve once,
   change a persistent local object/service and receive useful supplies. Give
   directions on arrival and in the journal; lead onward to existing bell work.
   Contents and placement must be real, reachable, seed-safe and persist after
   looting/destruction. Refusal/loss must never trap progression or gate hospitality.
4. **Acceptance:** combined regression suite, dedicated adversarial tests,
   independent cold-eye review, native player-flow audit and camera inspection.

## Performance

Resolve geographic leads when opening dialogue, not per frame. Quest cues reuse
existing entity visual refresh/invalidation where possible; pool/reuse marker
objects and shared materials. Avoid per-frame world scans and repeated mesh
creation. Cache invalidation must account for quest completion, zone changes,
visibility and owner death/removal. Read PERF-FOUNDATION.md before implementation
of rendering hooks. Procedural placement runs only at fresh generation; no
re-entry refill or invisible replacement of destroyed expedition content.

## Verification contract

- Tests precede production; record actual RED before implementation. Root alone
  runs Unity. Every success invariant gets a same-setup negative control.
- Check C# errors before parsing fresh XML. Compare full-suite failures to the
  recorded baseline; never claim existing failures disappeared without evidence.
- Add dedicated adversarial coverage for replay/duplicate rewards, wrong actor,
  wrong zone/owner, removal, malformed context, full inventory, save/load and
  generation-order variation. Test actual conversation/world-action wiring.
- Native acceptance uses an isolated save scope and actual bootstrap/content.
  Record successful and rejected actions with diagnostic evidence. Headless
  numbers establish mechanics; rendered captures establish visible cues only.
- Restore the normal editor after verification. Do not alter the player's save.

## Self-review and implementation log

- Read CLAUDE.md, Unity orchestration skill and adversarial workflow. Baseline
  and source preservation started before production edits.
- Divided verification among bounded quest identity, affordance, merchant/cue
  reviews. Root owns the early expedition and integration; the canonical-quest agent also owns regional guidance.
- CG01 baseline: 14,544 passed / 32 failed / zero C# errors. Compared exact failed
  names with VA07-full: unchanged. No baseline regressions attributed to this work.
- CG02: canonical quest-host regression RED, 9/9 failed as intended; implemented
  six one-home stories rather than independent clones of shared global facts.
- CG03: compile-prerequisite RED for absent expedition/harvest APIs. No assertions
  executed; not counted as behavioral evidence.
- CG04: implementation compile failure (incorrect MovablePart assumption). Fixed
  using shipped HandlingPart and verified DragRules; no results accepted.
- CG05: zero C# errors, 92 cases / 56 passed / 36 failed. All nine expedition,
  canonical and harvest logic tests pass. Remaining intended RED: 25 missing
  native quest-cue cases, nine missing merchant-stock cases, two missing stubble
  art cases. Those implementations are now authorized by real assertion evidence.
- CG06: compile-prerequisite RED for regional guidance and journal seams; no
  assertions executed. Guidance implementation follows.
- Verification correction: ordinary starting materials are modest. The 13-reagent
  plus six-component grant is developer-only. Corrected the audit instead of
  designing around a false incentive problem. The expedition uses actual Sack
  art for wrapped cloth and a WovenBasket owner, with existing Handling/Container/
  Destructible/CompleteObjectiveOnTaken mechanics rather than new parallel systems.
- Early cold-eye found malformed-part preflight gaps in the expedition; dedicated
  adversarial tests now cover those before the fix. Actual hauling and native
  player-input acceptance remain required gates, not inferred from inventory tests.

- CG07: zero C# errors, 167 cases / 150 passed / 17 failed. Dedicated expedition
  adversarial cases exposed ten real preflight/context/onboarding gaps and one
  full-inventory fixture mistake (weight one still fit the clay; corrected to
  zero capacity). Merchant control wrongly assumed modern Villager lacks Trader;
  corrected the legacy-faction stimulus. Five failures were still missing art.
- CG08 generated four coarse stubble variants. Zero C# errors / successful build.
  All 16 previous mesh/prefab families and their metas are byte-identical; only
  the library index changes and four new mesh/prefab pairs are appended.
- CG09: zero C# errors, 218 cases / 211 passed / 7 failed. Expedition adversarial,
  merchants, harvest/art, canonical quests and initial guidance all pass. New
  RED findings: undefined quest cues, scale-dependent cue size, dead/hostile
  guidance contexts and truncated long objectives. Fixes follow their tests.
- Verification correction: existing DragSystem intentionally releases at zone
  boundaries. Removed the false whole-basket delivery promise and unreachable
  drag-turn-in branch. Native hauling still works locally; carry the actual
  cloth across the border. Dedicated boundary test pins this distinction.
- Cross-feature cold-eye: damaged or malformed parcels must fail before payment
  or transfer; missing table examination must not half-complete a handover;
  detached same-address zones cannot own conversations. All are now covered.

- CG10: zero C# errors, 221 cases / 218 passed / 3 failures. The seven CG09
  findings pass. Two integration controls reproduce transplanted Morrowfast cues.
  The Vennit guidance fixture failed setup before reaching the topic assertion;
  its corrected mutation check is recorded below, not claimed as initial RED.
- Independent cold-eye: Bohr reviewed expedition after its fixes; Carson reviewed
  malformed content and native offering identity; Faraday reviewed shared cue
  ownership, fog and cleanup. No additional blocking defect after the identified
  fixes. Known bound: cached supply loss is real, not silently repaired. Farra's
  wording recalls where supplies were left; refusal/release preserves hospitality.

- CG11: zero C# errors, 225 cases / 223 passed / 2 failed. The newly reproduced
  zero-capacity basket bug is fixed by honoring ContainerPart.AddItem and
  Zone.AddEntity success before latching installation. Three alternate world
  seeds pass placement/uniqueness controls. The Vennit test failed a fixture
  ownership precondition (actor still in another zone); its earlier CG10 result
  therefore did not prove the missing-topic assertion. Corrected the fixture and
  CG12 temporarily removed only the new topic and reached the intended
  missing-topic assertion; restoring the exact JSON yielded a passing CG13 case. This is a post-implementation
  mutation check, not retroactively claimed TDD evidence.

- CG12: valid missing-topic mutation failure, one case / one failed / zero C#
  errors. The corrected Vennit fixture reaches the actual dialogue assertion.
  Restored the complete conversation file byte-for-byte after this check.
- CG13 full run: 14,713 passed / 41 failed / zero C# errors. The exact original
  32 failed names remain; nine new failures were investigated. Three old tests
  still asserted decorative-only water/crops and were updated to the intended
  interactive contracts with negative controls. One actual terrain fallback gap
  and five coverage failures for Wellmeet's canonical Warren actors were fixed.
  Actor model exceptions still require the exact quest/conversation/kill-fact
  contracts; arbitrary reskins remain rejected.
- CG14: all 354 focused feature, adversarial and affected integration cases
  passed, zero C# errors. Includes the nine CG13 failures plus two new native
  Warren-owner counterchecks and two explicit crop-ground/sprite controls.
  The repeat full suite and rendered acceptance are recorded below.

- CG15 full verification: 14,726 passed / 32 failed / zero C# errors.
  Compared exact failed test names with CG01: unchanged, no new failures.
  Total 14,758 cases, up 182 from the starting baseline. The full suite is not
  claimed green; the pre-existing failures remain outside this task.
- Native-harness cold-eye caught three evidence gaps before first execution:
  require the actual open Farra conversation for the missing-repeat check,
  inspect the rendered notes model as well as the selected pane, and require
  the presenter's current zone to be the active native graph. Tightened the
  acceptance harness only; production sources remained those verified by CG15 for CGN01–03.

- Native CGN02 exercised the complete expedition and Vennit notes, including
  F5/F6 restoration. Final acceptance remained failed because the harness
  incorrectly compared wilderness framing against the town's distinct fit.
  CGN03 then exposed an audit assumption about stunned movement; the game
  correctly consumed a turn without moving. These bounded harness corrections
  do not bypass simulation. See CHUNK-GAMEPLAY-NATIVE-AUDIT.md for receipts.
- Actual camera review found an implementation issue headless state checks had
  missed: quest-cue UVs assumed the wilderness's 16×8 atlas in Morrowfast, whose
  palette is 8×8. The available marker sampled a dark teal instead of its
  intended bright face and blended into the ground. A real-palette contrast
  regression precedes the fix, followed by a fresh native run.

- CG16: 42 quest-cue cases / 40 passed / 2 intended contrast failures / zero
  C# errors. Implemented correct 8-column town versus 16-column regional atlas
  sampling and a bounded pale/dark outlined exclamation using shipped swatches.
- CG17: all 356 focused feature/adversarial/integration cases pass, zero C# errors.
- CGN04: all 27 native checks pass, zero unexpected/compile errors, eight real
  1080p captures. Actual walking, stun recovery, three borders, dialogue, parcel
  retrieval/delivery, reward, directions, Q/Tab and F5/F6 were exercised. Source
  freeze and complete save/input/scene/view cleanup verified. Independent camera
  review confirms readable available cues and correct active/completed states.
  CG18 follows this last visual correction and leaves the exact baseline
  failures unchanged: 14,728 passed / 32 failed / 14,760 total, zero C# errors.
  This adds 184 cases to the starting suite, with no new failures.

## Player-facing changes

- Fresh-game arrival points east to Morrowfast. Farra offers **A Dry Place at
  Supper**; recover the actual western-field cloth, return it once, receive
  15 drams and one fire clay, and leave visible supplies at the common table.
  Nemm's existing bell work is the next optional use for the clay.
- Vennit, ordinary scribes and innkeepers can record nearby settlements from the
  actual map. **Q → Tab** opens saved travel notes; PageUp/PageDown changes pages.
  Directions retain their original departure point. Long quest objectives wrap.
- Six generic village stories each have one canonical home. Existing shared
  progress/rewards stay one-shot instead of being advertised as separate copies.
- Morrowfast's mender and provisioner retain their exact initial stock and use the
  existing refill interval after stock runs low. No immortal/replaced merchants.
- Voxel **!** indicates available work; a hollow diamond indicates active journal
  work, not readiness for payment. Completed, invalid, hidden or dead owners do
  not keep a cue. The native owner/collision graph and camera remain unchanged.
- Field strips contain a few harvestable emberwheat rows and spent stubble;
  harvesting is finite and leaves the same destructible owner. Tank bromeliads
  offer drinking and spray pools expose actual native water contact.

## Scope and repository preservation

Current-state saving is tested; migration of previously generated chunks is
explicitly excluded. Begin a new game for the complete newly authored expedition
and merchant/content setup. No new biomes, settlement generation, camera changes,
reveal changes or cross-zone hauling mechanics are included.

The starting workspace includes pre-existing untracked production dependencies
(including Morrowfast and the 3D presenters) as well as modified tracked files.
Do not stage their entire contents to make this patch appear standalone. Preserve
an exact change manifest/patch against the starting workspace and leave the code
in the working tree unless a genuinely isolated commit can represent it without
absorbing the user's pre-existing work. This is a repository-preservation limit,
not a claim that native gameplay verification is complete.

## Final review and bounds

- 🟡 Fixed during review: required-part and insertion preflights, exact giver/zone
  ownership, rejected/repeated handover, canonical Warren model coverage, field
  ground fallback, long objective wrapping and both native palette layouts.
  Every fix has actual failing evidence before the correction, except Vennit's
  initial missing-topic fixture: its valid later mutation RED is explicitly
  distinguished above.
- 🟢 Symmetry and lifecycle: one-time ownership/reward, ready/spent persistence,
  native owner removal, marker state/mesh release, save graph replacement and
  private-audit teardown have positive and negative coverage. Dedicated
  adversarial fixtures cover the expedition, guidance, canonical quest homes,
  affordances and quest cues. Independent cold-eye and actual camera passes ran.
- ⚪ CoO-original work; no Qud parity claim. No save migration or general hauling,
  farming simulation, new biome or town system is included.
- 🧪 Live acceptance covers one seed and the western-field/Morrowfast journey.
  Harvest/water mechanics and crop mesh transitions have native EditMode tests;
  this run does not certify stubble readability, summit-water feel, all-biome cue
  readability, first-hour balance or runtime performance.

## Files and review packet

World/content changes are in MorrowfastExpedition, WorldLocationContext,
OverworldZoneManager, MorrowfastContent, VillagePopulationBuilder and surgical
conversation/storylet/loot/blueprint additions. Discovery uses RegionalGuidance,
ConversationManager/Actions, QuestLogUI, QuestCueStateQuery and NativeQuestCueViews
with hooks in both 3D presenters. Affordances use FieldHarvestPart, Spread
composition/model selection, EnvironmentSpriteRenderer and four appended stubble
variants. New/adapted tests and the executable native acceptance accompany them.

The complete task-specific file list and original/current hashes are in
[changes.json](Verification/ChunkGameplayImplementation/changes.json);
[changes.patch](Verification/ChunkGameplayImplementation/changes.patch) is against
the preserved starting workspace, not a clean HEAD checkout. No files are staged
or committed. This avoids absorbing the existing untracked production files.

## Completion evidence

CG18 retains the exact CG01 baseline failure names, with no new failures.
CGN06 repeats the real journey with all 28 checks passing and complete cleanup.
After CG18, only the native audit harness and its evidence validator changed
(to add required performance sampling); game production is byte-identical to
the full-suite version. The final native run compiled and exercised that harness.

- [Final baseline comparison](Verification/ChunkGameplayImplementation/final-baseline-comparison.json)
- [Final native receipt](Verification/ChunkGameplayImplementation/CGN06-profiled-native-journey/receipt.json)
- [Native and performance report](CHUNK-GAMEPLAY-NATIVE-AUDIT.md)
- [Content/GUID integrity](Verification/ChunkGameplayImplementation/content-integrity.json)

Start a new game for fresh content, travel one chunk east from Grovelands to
Morrowfast and speak with Farra. Vennit supplies regional directions; Q → Tab
opens saved travel notes. User saves were never used by acceptance.
